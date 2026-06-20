import { useEffect, useRef, useState, useCallback } from "react";
import { useLocation } from "wouter";
import { useAuth } from "@/hooks/use-auth";
import { UNITY_BUILD_URL } from "@/config/unityConfig";
import { motion, AnimatePresence } from "framer-motion";

const TOKEN_KEY = "ek_token";

interface DebugState {
  iframeLoaded: boolean;
  readyReceived: boolean;
  tokenSent: boolean;
  retryFired: boolean;
}

export default function UnityLauncher() {
  const [, setLocation] = useLocation();
  const { user, isLoadingUser } = useAuth();
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [unityReady, setUnityReady] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [showDebug, setShowDebug] = useState(false);
  const [debug, setDebug] = useState<DebugState>({
    iframeLoaded: false,
    readyReceived: false,
    tokenSent: false,
    retryFired: false,
  });

  const unityReadyRef = useRef(false);
  const userRef = useRef(user);
  useEffect(() => { userRef.current = user; }, [user]);

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

  useEffect(() => {
    function handleMessage(event: MessageEvent) {
      if (event.data?.type === "UNITY_READY") {
        unityReadyRef.current = true;
        setUnityReady(true);
        setDebug((d) => ({ ...d, readyReceived: true }));
        sendAuth();
      }
    }
    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, [sendAuth]);

  useEffect(() => {
    const timer = setTimeout(() => {
      if (!unityReadyRef.current) {
        setDebug((d) => ({ ...d, retryFired: true }));
        sendAuth();
      }
    }, 3000);
    return () => clearTimeout(timer);
  }, [sendAuth]);

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

  return (
    <div className="fixed inset-0 bg-black overflow-hidden">
      <AnimatePresence>
        {!unityReady && !loadError && (
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

      {loadError && (
        <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-black gap-4">
          <div className="font-serif text-xl text-red-400">
            Unity client unavailable
          </div>
          <p className="text-sm text-zinc-400 max-w-sm text-center">
            Build not found at{" "}
            <code className="text-amber-400/80">{UNITY_BUILD_URL}</code>.
            Place the WebGL output in{" "}
            <code className="text-amber-400/80">public/unity/</code>.
          </p>
        </div>
      )}

      <iframe
        ref={iframeRef}
        src={UNITY_BUILD_URL}
        title="Eternal Kingdoms"
        className="w-full h-full border-0"
        allow="fullscreen; autoplay; clipboard-write"
        onLoad={handleIframeLoad}
        onError={() => setLoadError(true)}
      />

      {showDebug && (
        <div className="absolute bottom-4 left-4 z-50 bg-black/80 border border-zinc-700 rounded-lg p-3 text-xs font-mono text-zinc-300 space-y-1 pointer-events-none">
          <div className="text-amber-400 font-semibold mb-1">Unity Debug  <span className="text-zinc-500">(` to toggle)</span></div>
          <DebugRow label="iframe loaded" value={debug.iframeLoaded} />
          <DebugRow label="UNITY_READY" value={debug.readyReceived} />
          <DebugRow label="token sent" value={debug.tokenSent} />
          <DebugRow label="3s retry fired" value={debug.retryFired} />
          <div className="text-zinc-500 pt-1 border-t border-zinc-700">
            url: {UNITY_BUILD_URL}
          </div>
        </div>
      )}
    </div>
  );
}

function DebugRow({ label, value }: { label: string; value: boolean }) {
  return (
    <div className="flex items-center gap-2">
      <span className={value ? "text-emerald-400" : "text-zinc-600"}>
        {value ? "✓" : "✗"}
      </span>
      <span className={value ? "text-zinc-200" : "text-zinc-500"}>{label}</span>
    </div>
  );
}
