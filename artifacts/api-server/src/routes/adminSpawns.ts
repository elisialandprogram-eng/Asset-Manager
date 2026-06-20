/**
 * adminSpawns.ts — Admin-only spawn management endpoints.
 *
 * All routes require the user to be authenticated with role === "admin".
 *
 * Routes (mounted under /api/admin/spawns):
 *   POST /generate  — Trigger an immediate spawn cycle for a world
 *   POST /reset     — Delete all spawns for a world and regenerate from scratch
 *   POST /cleanup   — Delete stale expired/depleted rows older than 1 hour
 *   GET  /stats     — Aggregate spawn statistics for a world
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { runSpawnCycle, resetWorldSpawns, cleanupWorldSpawns, spawnRepository } from "../modules/spawn-system";

const router = Router();

/** Shared admin guard — rejects non-admin users with 403 */
function requireAdmin(req: AuthRequest, res: Parameters<typeof router.use>[0] extends (...args: infer A) => unknown ? A[1] : never): boolean {
  if (req.user?.role !== "admin") {
    (res as { status: (n: number) => { json: (b: unknown) => void } })
      .status(403)
      .json({ error: "Admin access required" });
    return false;
  }
  return true;
}

/**
 * POST /api/admin/spawns/generate
 * Triggers an immediate spawn cycle for a world.
 * Body: { worldId: number }
 */
router.post("/generate", requireAuth, async (req: AuthRequest, res) => {
  if (req.user?.role !== "admin") {
    res.status(403).json({ error: "Admin access required" });
    return;
  }

  const worldId = Number(req.body?.worldId);
  if (!worldId || isNaN(worldId)) {
    res.status(400).json({ error: "worldId is required" });
    return;
  }

  try {
    const result = await runSpawnCycle(worldId);
    res.json({ generated: result.generated, markedExpired: result.markedExpired, deleted: result.deleted });
  } catch (err) {
    req.log.error({ err }, "AdminSpawns generate error");
    res.status(500).json({ error: "Internal server error" });
  }
});

/**
 * POST /api/admin/spawns/reset
 * Deletes ALL spawns for a world and runs a fresh generation cycle.
 * Body: { worldId: number }
 */
router.post("/reset", requireAuth, async (req: AuthRequest, res) => {
  if (req.user?.role !== "admin") {
    res.status(403).json({ error: "Admin access required" });
    return;
  }

  const worldId = Number(req.body?.worldId);
  if (!worldId || isNaN(worldId)) {
    res.status(400).json({ error: "worldId is required" });
    return;
  }

  try {
    const result = await resetWorldSpawns(worldId);
    res.json(result);
  } catch (err) {
    req.log.error({ err }, "AdminSpawns reset error");
    res.status(500).json({ error: "Internal server error" });
  }
});

/**
 * POST /api/admin/spawns/cleanup
 * Deletes expired/depleted rows older than 1 hour (table maintenance).
 * Body: { worldId: number }
 */
router.post("/cleanup", requireAuth, async (req: AuthRequest, res) => {
  if (req.user?.role !== "admin") {
    res.status(403).json({ error: "Admin access required" });
    return;
  }

  const worldId = Number(req.body?.worldId);
  if (!worldId || isNaN(worldId)) {
    res.status(400).json({ error: "worldId is required" });
    return;
  }

  try {
    const result = await cleanupWorldSpawns(worldId);
    res.json(result);
  } catch (err) {
    req.log.error({ err }, "AdminSpawns cleanup error");
    res.status(500).json({ error: "Internal server error" });
  }
});

/**
 * GET /api/admin/spawns/stats?worldId=1
 * Returns aggregate spawn statistics (counts by status, subtype, biome).
 */
router.get("/stats", requireAuth, async (req: AuthRequest, res) => {
  if (req.user?.role !== "admin") {
    res.status(403).json({ error: "Admin access required" });
    return;
  }

  const worldId = Number(req.query["worldId"]);
  if (!worldId || isNaN(worldId)) {
    res.status(400).json({ error: "worldId query param is required" });
    return;
  }

  try {
    const stats = await spawnRepository.getStats(worldId);
    res.json(stats);
  } catch (err) {
    req.log.error({ err }, "AdminSpawns stats error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
