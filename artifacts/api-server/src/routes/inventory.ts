/**
 * inventory.ts — Item inventory endpoints.
 *
 * GET  /api/inventory?kingdomId=X   — Get item inventory
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { inventoryRepository, kingdomRepository } from "@workspace/db";

const router = Router();

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
    let inventory = await inventoryRepository.findByKingdomId(kingdomId);
    if (!inventory) {
      inventory = await inventoryRepository.upsert({ kingdomId, items: {} });
    }
    res.json({ kingdomId, items: inventory.items });
  } catch (err) {
    req.log.error({ err }, "GetInventory error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
