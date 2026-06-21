import { useEffect, useRef, useState, useCallback } from "react";
import { useLocation } from "wouter";
import { useAuth } from "@/hooks/use-auth";
import { UNITY_BUILD_URL } from "@/config/unityConfig";
import { motion, AnimatePresence } from "framer-motion";

const TOKEN_KEY = "ek_token";
const READY_TIMEOUT_MS = 120_000;

function checkWebGL(): boolean {
  try {
    const canvas = document.createElement("canvas");
    return !!(
      canvas.getContext("webgl2") ||
      canvas.getContext("webgl") ||
      canvas.getContext("experimental-webgl")
    );
  } catch {
    return false;
  }
}

interface DebugState {
  iframeLoaded: boolean;
  readyReceived: boolean;
  tokenSent: boolean;
  retryFired: boolean;
  timedOut: boolean;
}

export default function UnityLauncher() {
  const [, setLocation] = useLocation();
  const { user, isLoadingUser } = useAuth();
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [unityReady, setUnityReady] = useState(false);
  const [timedOut, setTimedOut] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [showDebug, setShowDebug] = useState(false);
  // True when WebGL API is absent entirely (very restrictive sandbox).
  // Unity may also report a WebGL error even when the API is present (no GPU),
  // so we also catch that case via loadError message check below.
  const [webGLAbsent] = useState(() => !checkWebGL());
  const [debug, setDebug] = useState<DebugState>({
    iframeLoaded: false,
    readyReceived: false,
    tokenSent: false,
    retryFired: false,
    timedOut: false,
  });

  const unityReadyRef = useRef(false);
  const userRef = useRef(user);
  useEffect(() => { userRef.current = user; }, [user]);

  // Auth guard
  useEffect(() => {
    if (!isLoadingUser && !user) setLocation("/");
  }, [user, isLoadingUser, setLocation]);

  const sendAuth = useCallback(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    const u = userRef.current;
    if (token && iframeRef.current?.contentWindow && u) {
      iframeRef.current.contentWindow.postMessage(
        { type: "UNITY_AUTH", token, userId: u.id },
        "*"
      );
      setDebug((d) => ({ ...d, tokenSent: true }));
    }
  }, []);

  // Listen for UNITY_READY / UNITY_LOAD_ERROR
  useEffect(() => {
    if (webGLAbsent) return;
    function handleMessage(event: MessageEvent) {
      if (event.data?.type === "UNITY_READY") {
        unityReadyRef.current = true;
        setUnityReady(true);
        setDebug((d) => ({ ...d, readyReceived: true }));
        sendAuth();
      } else if (event.data?.type === "UNITY_LOAD_ERROR") {
        const msg = String(event.data.message ?? "Unknown error");
        console.error("[UnityLauncher] Unity reported a load error:", msg);
        setLoadError(msg);
      }
    }
    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, [sendAuth, webGLAbsent]);

  // 3-second auth retry
  useEffect(() => {
    if (webGLAbsent) return;
    const t = setTimeout(() => {
      if (!unityReadyRef.current) {
        setDebug((d) => ({ ...d, retryFired: true }));
        sendAuth();
      }
    }, 3_000);
    return () => clearTimeout(t);
  }, [sendAuth, webGLAbsent]);

  // Debug toggle via backtick
  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "`") setShowDebug((v) => !v);
    }
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, []);

  function handleIframeLoad() {
    setDebug((d) => ({ ...d, iframeLoaded: true }));
    sendAuth();
    setTimeout(() => {
      if (!unityReadyRef.current) {
        setTimedOut(true);
        setDebug((d) => ({ ...d, timedOut: true }));
      }
    }, READY_TIMEOUT_MS);
  }

  if (isLoadingUser || !user) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-amber-400/60 text-sm animate-pulse font-serif">
          Entering the realm…
        </div>
      </div>
    );
  }

  // Show "open in new tab" when:
  //  a) WebGL API is entirely absent (webGLAbsent), OR
  //  b) Unity itself reports no WebGL support (sandboxed iframe with software GL)
  const noGPU =
    webGLAbsent ||
    (loadError !== null && /webgl/i.test(loadError));

  if (noGPU) {
    const fullUrl = window.location.href;
    return (
      <div className="min-h-screen bg-black flex flex-col items-center justify-center gap-6 p-6">
        <div className="w-16 h-16 rounded-2xl bg-amber-900/20 border border-amber-700/30 flex items-center justify-center">
          <span className="text-3xl">🏰</span>
        </div>
        <div className="text-center space-y-2 max-w-sm">
          <h2 className="font-serif text-2xl text-amber-400">
            Open in Your Browser
          </h2>
          <p className="text-sm text-zinc-400 leading-relaxed">
            The Eternal Kingdoms client requires WebGL, which isn't available
            inside Replit's preview pane. Open the game in a full browser tab
            to play.
          </p>
        </div>
        <a
          href={fullUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="px-6 py-3 bg-amber-600 hover:bg-amber-500 text-black font-semibold text-sm rounded-lg transition-colors"
        >
          Open Game in New Tab ↗
        </a>
        <button
          onClick={() => setLocation("/")}
          className="text-xs text-zinc-600 hover:text-zinc-400 transition-colors"
        >
          Back to login
        </button>
      </div>
    );
  }

  const showError = loadError !== null || timedOut;

  return (
    <div className="fixed inset-0 bg-black overflow-hidden">
      {/* Loading spinner */}
      <AnimatePresence>
        {!unityReady && !showError && (
          <motion.div
            key="loading"
            initial={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.6 }}
            className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-black gap-6 pointer-events-none"
          >
            <motion.div
              animate={{ opacity: [0.5, 1, 0.5] }}
              transition={{ repeat: Infinity, duration: 2 }}
              className="font-serif text-3xl text-amber-400"
            >
              Eternal Kingdoms
            </motion.div>
            <div className="flex items-center gap-2">
              <motion.div
                animate={{ rotate: 360 }}
                transition={{ repeat: Infinity, duration: 1, ease: "linear" }}
                className="w-5 h-5 border-2 border-amber-400/30 border-t-amber-400 rounded-full"
              />
              <span className="text-sm text-amber-400/60 font-serif">
                Loading world…
              </span>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Error state */}
      {showError && (
        <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-black gap-5 p-6">
          <div className="w-16 h-16 rounded-2xl bg-amber-900/20 border border-amber-700/30 flex items-center justify-center">
            <span className="text-3xl">⚔️</span>
          </div>
          <div className="text-center space-y-2">
            <h2 className="font-serif text-xl text-amber-400">
              {timedOut ? "Connection Timeout" : "Failed to Load"}
            </h2>
            <p className="text-sm text-zinc-400 max-w-sm leading-relaxed">
              {timedOut
                ? "Unity loaded but did not send a ready signal in time."
                : loadError ?? "An unknown error occurred."}
            </p>
          </div>
          <div className="flex gap-3">
            <button
              onClick={() => {
                setTimedOut(false);
                setLoadError(null);
                setUnityReady(false);
                unityReadyRef.current = false;
                setDebug({ iframeLoaded: false, readyReceived: false, tokenSent: false, retryFired: false, timedOut: false });
                if (iframeRef.current) iframeRef.current.src = UNITY_BUILD_URL;
              }}
              className="px-4 py-2 border border-amber-700/50 text-amber-400 text-sm rounded hover:bg-amber-900/30 transition-colors"
            >
              Retry
            </button>
            <button
              onClick={() => setLocation("/")}
              className="px-4 py-2 border border-zinc-700 text-zinc-400 text-sm rounded hover:bg-zinc-800 transition-colors"
            >
              Sign out
            </button>
          </div>
        </div>
      )}

      {/* Unity iframe */}
      <iframe
        ref={iframeRef}
        src={UNITY_BUILD_URL}
        title="Eternal Kingdoms"
        className="w-full h-full border-0"
        allow="fullscreen; autoplay; clipboard-write"
        onLoad={handleIframeLoad}
        onError={() => setLoadError("The Unity build could not be loaded.")}
      />

      {/* Debug overlay — toggle with ` */}
      {showDebug && (
        <div className="absolute bottom-4 left-4 z-50 bg-black/85 border border-zinc-700 rounded-lg p-3 text-xs font-mono text-zinc-300 space-y-1 pointer-events-none">
          <div className="text-amber-400 font-semibold mb-1">
            Unity Debug <span className="text-zinc-500">(` to toggle)</span>
          </div>
          <DebugRow label="webgl absent" value={webGLAbsent} warn />
          <DebugRow label="iframe loaded" value={debug.iframeLoaded} />
          <DebugRow label="UNITY_READY" value={debug.readyReceived} />
          <DebugRow label="token sent" value={debug.tokenSent} />
          <DebugRow label="3s retry fired" value={debug.retryFired} />
          <DebugRow label="timeout hit" value={debug.timedOut} warn />
          <div className="text-zinc-500 pt-1 border-t border-zinc-700">
            url: {UNITY_BUILD_URL}
          </div>
        </div>
      )}
    </div>
  );
}

function DebugRow({
  label,
  value,
  warn = false,
}: {
  label: string;
  value: boolean;
  warn?: boolean;
}) {
  const color = value
    ? warn
      ? "text-amber-400"
      : "text-emerald-400"
    : "text-zinc-600";
  return (
    <div className="flex items-center gap-2">
      <span className={color}>{value ? "✓" : "✗"}</span>
      <span className={value ? "text-zinc-200" : "text-zinc-500"}>{label}</span>
    </div>
  );
}
