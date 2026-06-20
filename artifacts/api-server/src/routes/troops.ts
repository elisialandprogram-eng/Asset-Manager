/**
 * troops.ts — Troop inventory endpoints.
 *
 * GET  /api/troops?kingdomId=X        — Get T1-T5 troop inventory
 * GET  /api/troops/definitions        — Get all troop definitions (stats)
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { troopInventoryRepository, kingdomRepository } from "@workspace/db";
import { TROOP_DEFINITIONS, ALL_TROOP_KEYS } from "@workspace/game-engine";

const router = Router();

// ---------------------------------------------------------------------------
// GET /api/troops/definitions — Static troop stat table
// ---------------------------------------------------------------------------

router.get("/definitions", requireAuth, async (_req, res) => {
  const definitions = ALL_TROOP_KEYS.map((key) => TROOP_DEFINITIONS[key]);
  res.json({ definitions });
});

// ---------------------------------------------------------------------------
// GET /api/troops?kingdomId=X
// ---------------------------------------------------------------------------

router.get("/", requireAuth, async (req: AuthRequest, res) => {
  const kingdomId = Number(String(req.query["kingdomId"] ?? ""));
  if (isNaN(kingdomId) || kingdomId <= 0) {
    res.status(400).json({ error: "kingdomId query parameter is required" });
    return;
  }

  const kingdom = await kingdomRepository.findById(kingdomId);
  if (!kingdom) {
    res.status(404).json({ error: "Kingdom not found" });
    return;
  }
  if (kingdom.userId !== req.user!.userId) {
    res.status(403).json({ error: "You do not own this kingdom" });
    return;
  }

  try {
    let inventory = await troopInventoryRepository.findByKingdomId(kingdomId);
    if (!inventory) {
      inventory = await troopInventoryRepository.upsert({ kingdomId, troops: {} });
    }
    res.json({ troops: inventory.troops, kingdomId });
  } catch (err) {
    req.log.error({ err }, "GetTroops error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
