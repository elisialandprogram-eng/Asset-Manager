import { createRoot } from "react-dom/client";
import { setAuthTokenGetter } from "@workspace/api-client-react";
import App from "./App";
import "./index.css";

// Setup API auth token
setAuthTokenGetter(() => localStorage.getItem("ek_token"));

// ─── Window-level error reporting ────────────────────────────────────────────
// Captures unhandled JS errors and unhandled promise rejections (including
// Three.js WebGL context creation failures that bypass React Error Boundaries)
// and reports them to the Express server so they appear in workflow logs.

function reportClientError(payload: Record<string, unknown>) {
  try {
    navigator.sendBeacon(
      "/api/client-error",
      new Blob([JSON.stringify(payload)], { type: "application/json" })
    );
  } catch {
    // sendBeacon may not be available in all environments; fail silently
  }
}

window.addEventListener("error", (e) => {
  reportClientError({
    type:     "error",
    message:  e.message,
    filename: e.filename,
    lineno:   e.lineno,
    colno:    e.colno,
    stack:    e.error instanceof Error ? e.error.stack : undefined,
  });
});

window.addEventListener("unhandledrejection", (e) => {
  const reason = e.reason;
  reportClientError({
    type:    "unhandledrejection",
    message: reason instanceof Error ? reason.message : String(reason),
    stack:   reason instanceof Error ? reason.stack   : undefined,
  });
});
// ─────────────────────────────────────────────────────────────────────────────

createRoot(document.getElementById("root")!).render(<App />);
