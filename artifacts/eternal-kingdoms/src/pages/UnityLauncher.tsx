import { useEffect, useRef, useState } from "react";
import { useLocation } from "wouter";
import { useAuth } from "@/hooks/use-auth";
import { UNITY_BUILD_URL } from "@/config/unityConfig";
import { motion, AnimatePresence } from "framer-motion";

const TOKEN_KEY = "ek_token";

export default function UnityLauncher() {
  const [, setLocation] = useLocation();
  const { user, isLoadingUser } = useAuth();
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [unityReady, setUnityReady] = useState(false);
  const [loadError, setLoadError] = useState(false);

  useEffect(() => {
    if (!isLoadingUser && !user) setLocation("/");
  }, [user, isLoadingUser, setLocation]);

  useEffect(() => {
    function handleMessage(event: MessageEvent) {
      if (event.data?.type === "UNITY_READY") {
        setUnityReady(true);
        const token = localStorage.getItem(TOKEN_KEY);
        if (token && iframeRef.current?.contentWindow && user) {
          iframeRef.current.contentWindow.postMessage(
            { type: "UNITY_AUTH", token, userId: user.id },
            "*"
          );
        }
      }
    }
    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, [user]);

  function handleIframeLoad() {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token && iframeRef.current?.contentWindow && user) {
      iframeRef.current.contentWindow.postMessage(
        { type: "UNITY_AUTH", token, userId: user.id },
        "*"
      );
    }
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
            className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-black gap-6"
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
          <p className="text-sm text-muted-foreground max-w-sm text-center">
            The Unity build could not be loaded at{" "}
            <code className="text-amber-400/80">{UNITY_BUILD_URL}</code>. Place
            the WebGL build in <code className="text-amber-400/80">public/unity/</code>.
          </p>
          <button
            onClick={() => setLocation("/dashboard")}
            className="mt-2 px-4 py-2 border border-amber-700/50 text-amber-400 text-sm rounded hover:bg-amber-900/30 transition-colors"
          >
            Back to Dashboard
          </button>
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
    </div>
  );
}
