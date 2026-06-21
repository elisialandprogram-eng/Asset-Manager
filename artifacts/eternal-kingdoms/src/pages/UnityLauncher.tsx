import { useEffect, useRef, useState, useCallback } from "react";
import { useLocation } from "wouter";
import { useAuth } from "@/hooks/use-auth";
import { UNITY_BUILD_URL } from "@/config/unityConfig";
import { motion, AnimatePresence } from "framer-motion";

const TOKEN_KEY = "ek_token";
const READY_TIMEOUT_MS = 90_000;

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

type GameMode = "world_active" | "fallback_world" | null;

interface DebugState {
  iframeLoaded: boolean;
  readyReceived: boolean;
  gameReady: boolean;
  gameMode: GameMode;
  tokenSent: boolean;
  retryCount: number;
  timedOut: boolean;
}

export default function UnityLauncher() {
  const [, setLocation] = useLocation();
  const { user, isLoadingUser } = useAuth();
  const iframeRef = useRef<HTMLIFrameElement>(null);

  const [unityReady, setUnityReady] = useState(false);
  const [gameReady, setGameReady] = useState(false);
  const [gameMode, setGameMode] = useState<GameMode>(null);
  const [timedOut, setTimedOut] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [progress, setProgress] = useState(0);
  const [showDebug, setShowDebug] = useState(false);
  const [webGLAbsent] = useState(() => !checkWebGL());

  const [debug, setDebug] = useState<DebugState>({
    iframeLoaded: false,
    readyReceived: false,
    gameReady: false,
    gameMode: null,
    tokenSent: false,
    retryCount: 0,
    timedOut: false,
  });

  const unityReadyRef = useRef(false);
  const gameReadyRef = useRef(false);
  const retryCountRef = useRef(0);
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
      retryCountRef.current += 1;
      setDebug((d) => ({ ...d, tokenSent: true, retryCount: retryCountRef.current }));
    }
  }, []);

  // Listen for messages from Unity iframe
  useEffect(() => {
    if (webGLAbsent) return;

    function handleMessage(event: MessageEvent) {
      const msg = event.data;
      if (!msg) return;

      switch (msg.type) {
        case "UNITY_READY":
          // Engine loaded — send auth token immediately
          unityReadyRef.current = true;
          setUnityReady(true);
          setProgress(1);
          setDebug((d) => ({ ...d, readyReceived: true }));
          sendAuth();
          break;

        case "UNITY_GAME_READY":
          // RuntimeBootstrap confirmed a camera is rendering
          gameReadyRef.current = true;
          const mode = (msg.mode ?? null) as GameMode;
          setGameReady(true);
          setGameMode(mode);
          setDebug((d) => ({ ...d, gameReady: true, gameMode: mode }));
          break;

        case "UNITY_PROGRESS":
          setProgress(msg.value ?? 0);
          break;

        case "UNITY_LOAD_ERROR":
          console.error("[UnityLauncher] Unity reported load error:", msg.message);
          setLoadError(String(msg.message ?? "Unknown Unity load error"));
          break;
      }
    }

    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, [sendAuth, webGLAbsent]);

  // Periodic auth token retry (every 3 s, up to 5 times)
  useEffect(() => {
    if (webGLAbsent) return;
    const intervals: ReturnType<typeof setInterval>[] = [];
    const t = setInterval(() => {
      if (retryCountRef.current >= 5 || gameReadyRef.current) {
        intervals.forEach(clearInterval);
        return;
      }
      sendAuth();
    }, 3_000);
    intervals.push(t);
    return () => intervals.forEach(clearInterval);
  }, [sendAuth, webGLAbsent]);

  // Debug overlay toggle (backtick)
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

    // If UNITY_READY hasn't fired within the timeout, surface an error
    setTimeout(() => {
      if (!unityReadyRef.current) {
        setTimedOut(true);
        setDebug((d) => ({ ...d, timedOut: true }));
      }
    }, READY_TIMEOUT_MS);
  }

  // Loading / auth spinner
  if (isLoadingUser || !user) {
    return (
      <div
        className="min-h-screen flex items-center justify-center"
        style={{ background: "radial-gradient(ellipse at center, #1a1200 0%, #0a0a0a 100%)" }}
      >
        <div className="flex flex-col items-center gap-4">
          <div className="text-amber-400 font-serif text-4xl font-bold tracking-wide">
            Eternal Kingdoms
          </div>
          <div className="flex items-center gap-3">
            <motion.div
              animate={{ rotate: 360 }}
              transition={{ repeat: Infinity, duration: 1.2, ease: "linear" }}
              className="w-4 h-4 border-2 border-amber-600/40 border-t-amber-400 rounded-full"
            />
            <span className="text-amber-200/70 text-sm font-serif">Authenticating…</span>
          </div>
        </div>
      </div>
    );
  }

  // WebGL unavailable (sandbox, old browser)
  const noGPU = webGLAbsent || (loadError !== null && /webgl/i.test(loadError));
  if (noGPU) {
    const fullUrl = window.location.href;
    return (
      <div
        className="min-h-screen flex flex-col items-center justify-center gap-8 p-8"
        style={{ background: "radial-gradient(ellipse at center, #1a1200 0%, #0a0a0a 100%)" }}
      >
        <div className="text-amber-400 font-serif text-4xl font-bold">Eternal Kingdoms</div>
        <div className="max-w-md w-full bg-zinc-900/80 border border-amber-800/40 rounded-2xl p-8 flex flex-col items-center gap-6 text-center">
          <div className="w-14 h-14 rounded-2xl bg-amber-900/30 border border-amber-700/40 flex items-center justify-center text-3xl">
            🏰
          </div>
          <div className="space-y-2">
            <h2 className="text-white font-semibold text-xl">Open in a Full Browser Tab</h2>
            <p className="text-zinc-400 text-sm leading-relaxed">
              The game requires hardware WebGL which isn't available inside
              Replit's embedded preview. Open the link below in Chrome, Firefox,
              or Safari to play.
            </p>
          </div>
          <a
            href={fullUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="w-full py-3 bg-amber-500 hover:bg-amber-400 text-black font-bold text-sm rounded-xl transition-colors flex items-center justify-center gap-2"
          >
            Open Game in Browser ↗
          </a>
          <div className="w-full bg-zinc-800 rounded-lg px-4 py-3 text-left">
            <div className="text-zinc-500 text-xs mb-1">Game URL</div>
            <div className="text-zinc-200 text-xs font-mono break-all">{fullUrl}</div>
          </div>
        </div>
        <button
          onClick={() => setLocation("/")}
          className="text-xs text-zinc-600 hover:text-zinc-400 transition-colors"
        >
          ← Back to login
        </button>
      </div>
    );
  }

  const showError = (loadError !== null && !noGPU) || timedOut;
  const pct = Math.round(progress * 100);

  function loadingLabel(): string {
    if (pct === 0) return "Connecting…";
    if (pct < 90) return `Downloading game client… ${pct}%`;
    if (pct < 100) return `Compiling shaders… ${pct}%`;
    if (!gameReady) return "Initialising world…";
    return gameMode === "fallback_world" ? "Fallback world ready" : "World loaded";
  }

  function gameModeLabel(): string {
    if (!gameReady) return "";
    if (gameMode === "fallback_world")
      return "Preview world active — awaiting live game scene";
    if (gameMode === "world_active")
      return "World scene live";
    return "";
  }

  return (
    <div className="fixed inset-0 overflow-hidden" style={{ background: "#0a0a0a" }}>

      {/* Loading overlay — shown until Unity engine is ready */}
      <AnimatePresence>
        {!unityReady && !showError && (
          <motion.div
            key="loading"
            initial={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.8 }}
            className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-10"
            style={{ background: "radial-gradient(ellipse at 50% 40%, #1f1000 0%, #0a0802 60%, #000 100%)" }}
          >
            <div className="flex flex-col items-center gap-2">
              <motion.h1
                animate={{ opacity: [0.7, 1, 0.7] }}
                transition={{ repeat: Infinity, duration: 3, ease: "easeInOut" }}
                className="font-serif text-5xl font-bold text-amber-400 tracking-widest"
              >
                ETERNAL KINGDOMS
              </motion.h1>
              <p className="text-amber-200/50 text-sm tracking-widest uppercase font-serif">
                Preparing your realm
              </p>
            </div>

            <div className="w-80 flex flex-col items-center gap-3">
              <div className="w-full h-1.5 bg-zinc-800 rounded-full overflow-hidden">
                <motion.div
                  className="h-full bg-gradient-to-r from-amber-600 to-amber-400 rounded-full"
                  style={{ width: `${pct}%` }}
                  transition={{ duration: 0.4, ease: "easeOut" }}
                />
              </div>
              <div className="flex items-center gap-3">
                <motion.div
                  animate={{ rotate: 360 }}
                  transition={{ repeat: Infinity, duration: 1.4, ease: "linear" }}
                  className="w-3.5 h-3.5 border-2 border-amber-800 border-t-amber-400 rounded-full flex-shrink-0"
                />
                <span className="text-amber-300/70 text-xs font-mono tabular-nums">
                  {loadingLabel()}
                </span>
              </div>
              {pct < 10 && (
                <p className="text-zinc-600 text-xs text-center leading-relaxed max-w-xs">
                  First load downloads ~14 MB of game data.<br />
                  This takes 10–30 seconds depending on your connection.
                </p>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Game-mode status badge — shown after UNITY_GAME_READY */}
      <AnimatePresence>
        {gameReady && gameModeLabel() && (
          <motion.div
            key="game-mode-badge"
            initial={{ opacity: 0, y: -8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            transition={{ duration: 0.6 }}
            className="absolute top-3 left-1/2 -translate-x-1/2 z-30 px-4 py-1.5 rounded-full
                       bg-black/60 border border-amber-700/40 backdrop-blur-sm"
          >
            <span className="text-amber-400/80 text-xs font-mono tracking-wide">
              {gameMode === "fallback_world" ? "⚠ " : "⚔ "}
              {gameModeLabel()}
            </span>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Error state */}
      {showError && (
        <div
          className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-6 p-8"
          style={{ background: "radial-gradient(ellipse at center, #1a0800 0%, #0a0a0a 100%)" }}
        >
          <div className="text-amber-400 font-serif text-4xl font-bold">Eternal Kingdoms</div>
          <div className="max-w-sm w-full bg-zinc-900/80 border border-zinc-700 rounded-2xl p-6 flex flex-col items-center gap-4 text-center">
            <span className="text-3xl">⚔️</span>
            <h2 className="text-white font-semibold text-lg">
              {timedOut ? "Connection Timeout" : "Failed to Load"}
            </h2>
            <p className="text-zinc-400 text-sm leading-relaxed">
              {timedOut
                ? "Unity engine didn't signal ready in time. Try opening the game in a full browser tab."
                : (loadError ?? "An unknown error occurred.")}
            </p>
            <div className="flex gap-3 w-full">
              <button
                onClick={() => {
                  setTimedOut(false);
                  setLoadError(null);
                  setProgress(0);
                  setUnityReady(false);
                  setGameReady(false);
                  setGameMode(null);
                  unityReadyRef.current = false;
                  gameReadyRef.current = false;
                  retryCountRef.current = 0;
                  setDebug({
                    iframeLoaded: false, readyReceived: false, gameReady: false,
                    gameMode: null, tokenSent: false, retryCount: 0, timedOut: false,
                  });
                  if (iframeRef.current) iframeRef.current.src = UNITY_BUILD_URL;
                }}
                className="flex-1 py-2 border border-amber-700/50 text-amber-400 text-sm rounded-lg hover:bg-amber-900/20 transition-colors"
              >
                Retry
              </button>
              <button
                onClick={() => setLocation("/")}
                className="flex-1 py-2 border border-zinc-700 text-zinc-400 text-sm rounded-lg hover:bg-zinc-800 transition-colors"
              >
                Sign out
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Unity iframe — always mounted so download starts immediately */}
      <iframe
        ref={iframeRef}
        src={UNITY_BUILD_URL}
        title="Eternal Kingdoms"
        className="absolute inset-0 w-full h-full border-0"
        allow="fullscreen; autoplay; clipboard-write"
        onLoad={handleIframeLoad}
        onError={() => setLoadError("The Unity build could not be loaded.")}
      />

      {/* Debug overlay — toggle with ` */}
      {showDebug && (
        <div className="absolute bottom-4 left-4 z-50 bg-black/90 border border-zinc-700 rounded-lg p-3 text-xs font-mono text-zinc-300 space-y-1 pointer-events-none">
          <div className="text-amber-400 font-semibold mb-1">
            Unity Debug <span className="text-zinc-500">(` to toggle)</span>
          </div>
          <DebugRow label="webgl absent"    value={webGLAbsent}      warn />
          <DebugRow label="iframe loaded"   value={debug.iframeLoaded} />
          <DebugRow label="UNITY_READY"     value={debug.readyReceived} />
          <DebugRow label="UNITY_GAME_READY" value={debug.gameReady} />
          <DebugRow label="token sent"      value={debug.tokenSent} />
          <DebugRow label="timeout hit"     value={debug.timedOut}   warn />
          <div className="text-zinc-500 pt-1 border-t border-zinc-700">
            progress: {pct}% · retries: {debug.retryCount}
          </div>
          {debug.gameMode && (
            <div className="text-amber-400/70">mode: {debug.gameMode}</div>
          )}
          <div className="text-zinc-600 text-[10px] break-all max-w-[220px]">
            {UNITY_BUILD_URL}
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
  const color = value ? (warn ? "text-amber-400" : "text-emerald-400") : "text-zinc-600";
  return (
    <div className="flex items-center gap-2">
      <span className={color}>{value ? "✓" : "✗"}</span>
      <span className={value ? "text-zinc-200" : "text-zinc-500"}>{label}</span>
    </div>
  );
}
