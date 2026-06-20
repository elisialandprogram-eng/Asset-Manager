/**
 * actionPoints.ts — Action points endpoint.
 *
 * GET  /api/kingdoms/:id/ap   — Get current AP (with lazy regen applied)
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { actionPointRepository, kingdomRepository } from "@workspace/db";

const router = Router();

router.get("/:id/ap", requireAuth, async (req: AuthRequest, res) => {
  const kingdomId = Number(String(req.params["id"]));
  if (isNaN(kingdomId) || kingdomId <= 0) {
    res.status(400).json({ error: "Invalid kingdom ID" });
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
    let ap = await actionPointRepository.getCurrentAp(kingdomId);
    if (!ap) {
      await actionPointRepository.upsert({
        kingdomId,
        currentAP: 200,
        maxAP: 200,
        regenRatePerMinute: 1.0 / 6.0,
        lastRegenAt: new Date(),
      });
      ap = { currentAP: 200, maxAP: 200 };
    }
    res.json({ currentAP: ap.currentAP, maxAP: ap.maxAP });
  } catch (err) {
    req.log.error({ err }, "GetActionPoints error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
