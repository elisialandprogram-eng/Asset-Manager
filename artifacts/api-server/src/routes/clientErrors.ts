import { Router, type IRouter } from "express";
import type { Request, Response } from "express";

const router: IRouter = Router();

/**
 * POST /api/client-error
 * Receives browser-side unhandled errors and promise rejections.
 * Logs them to the server console so they appear in workflow logs.
 * Used by main.tsx window.onerror / unhandledrejection listeners.
 */
router.post("/client-error", (req: Request, res: Response) => {
  const body = req.body ?? {};
  // pino logger attached to app — fall back to console
  const log = (req.app as unknown as { log?: { warn: (...a: unknown[]) => void } }).log;
  const entry = {
    type:     body.type ?? "unknown",
    message:  body.message ?? body.reason ?? "(no message)",
    filename: body.filename,
    lineno:   body.lineno,
    colno:    body.colno,
    stack:    typeof body.stack === "string" ? body.stack.slice(0, 1200) : undefined,
  };
  if (log?.warn) {
    log.warn(entry, "[browser] client-side error reported");
  } else {
    console.warn("[browser] client-side error:", JSON.stringify(entry, null, 2));
  }
  res.status(204).end();
});

export default router;
