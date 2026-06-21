import { useEffect, useRef, useState, useCallback } from "react";
import { useLocation } from "wouter";
import { useAuth } from "@/hooks/use-auth";
import { UNITY_BUILD_URL } from "@/config/unityConfig";
import { motion, AnimatePresence } from "framer-motion";

const TOKEN_KEY = "ek_token";
// If Unity doesn't signal READY within this many ms after iframe load, show the
// "build not deployed" screen instead of spinning forever.
// 120 s — Unity WebGL WASM is ~14 MB compressed / 83 MB uncompressed and
// needs time to download + compile before the READY signal fires.
const READY_TIMEOUT_MS = 120_000;

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
  const [loadError, setLoadError] = useState(false);
  const [showDebug, setShowDebug] = useState(false);
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

  // Listen for UNITY_READY
  useEffect(() => {
    function handleMessage(event: MessageEvent) {
      if (event.data?.type === "UNITY_READY") {
        unityReadyRef.current = true;
        setUnityReady(true);
        setDebug((d) => ({ ...d, readyReceived: true }));
        sendAuth();
      } else if (event.data?.type === "UNITY_LOAD_ERROR") {
        console.error("[UnityLauncher] Unity reported a load error:", event.data.message);
        setLoadError(true);
      }
    }
    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, [sendAuth]);

  // 3-second auth retry (in case READY fires before listener is set)
  useEffect(() => {
    const t = setTimeout(() => {
      if (!unityReadyRef.current) {
        setDebug((d) => ({ ...d, retryFired: true }));
        sendAuth();
      }
    }, 3_000);
    return () => clearTimeout(t);
  }, [sendAuth]);

  // Debug toggle via backtick
  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "`") setShowDebug((v) => !v);
    }
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, []);

  // After iframe loads, give Unity READY_TIMEOUT_MS to signal — then bail out
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

  const showError = loadError || timedOut;

  return (
    <div className="fixed inset-0 bg-black overflow-hidden">
      {/* Loading spinner — hidden once Unity is ready OR we've timed out */}
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

      {/* Build-not-found / timeout state */}
      {showError && (
        <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-black gap-5 p-6">
          <div className="w-16 h-16 rounded-2xl bg-amber-900/20 border border-amber-700/30 flex items-center justify-center">
            <span className="text-3xl">⚔️</span>
          </div>
          <div className="text-center space-y-2">
            <h2 className="font-serif text-xl text-amber-400">
              Unity Build Not Deployed
            </h2>
            <p className="text-sm text-zinc-400 max-w-sm leading-relaxed">
              {timedOut
                ? "The Unity client loaded but did not send a READY signal within 10 seconds."
                : "The Unity build file could not be fetched."}
            </p>
          </div>
          <div className="bg-zinc-900 border border-zinc-700 rounded-lg p-4 text-xs font-mono text-zinc-300 max-w-sm w-full space-y-1">
            <div className="text-zinc-500 mb-2">To fix — add the WebGL build:</div>
            <div className="text-emerald-400">
              artifacts/eternal-kingdoms/public/
            </div>
            <div className="text-emerald-400 pl-4">unity/</div>
            <div className="text-zinc-300 pl-8">index.html</div>
            <div className="text-zinc-300 pl-8">Build/</div>
            <div className="text-zinc-500 pl-10">*.wasm  *.js  *.data</div>
          </div>
          <div className="flex gap-3">
            <button
              onClick={() => {
                setTimedOut(false);
                setLoadError(false);
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

      {/* Unity iframe — always mounted so it can initialise in background */}
      <iframe
        ref={iframeRef}
        src={UNITY_BUILD_URL}
        title="Eternal Kingdoms"
        className="w-full h-full border-0"
        allow="fullscreen; autoplay; clipboard-write"
        onLoad={handleIframeLoad}
        onError={() => setLoadError(true)}
      />

      {/* Debug overlay — toggle with ` */}
      {showDebug && (
        <div className="absolute bottom-4 left-4 z-50 bg-black/85 border border-zinc-700 rounded-lg p-3 text-xs font-mono text-zinc-300 space-y-1 pointer-events-none">
          <div className="text-amber-400 font-semibold mb-1">
            Unity Debug{" "}
            <span className="text-zinc-500">(` to toggle)</span>
          </div>
          <DebugRow label="iframe loaded" value={debug.iframeLoaded} />
          <DebugRow label="UNITY_READY" value={debug.readyReceived} />
          <DebugRow label="token sent" value={debug.tokenSent} />
          <DebugRow label="3s retry fired" value={debug.retryFired} />
          <DebugRow label="10s timeout hit" value={debug.timedOut} warn />
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
