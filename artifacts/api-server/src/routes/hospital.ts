/**
 * hospital.ts — Hospital management endpoints.
 *
 * GET  /api/hospital?kingdomId=X   — Get hospital state (with lazy healing)
 * POST /api/hospital/heal          — Trigger immediate heal tick (debugging / speedup)
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { hospitalRepository, kingdomRepository } from "@workspace/db";

const router = Router();

// ---------------------------------------------------------------------------
// GET /api/hospital?kingdomId=X
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
    let hospital = await hospitalRepository.heal(kingdomId);
    if (!hospital) {
      hospital = await hospitalRepository.upsert({
        kingdomId,
        woundedTroops: {},
        capacity: 500,
        healRatePerMinute: 5,
        lastHealAt: new Date(),
      });
    }

    const totalWounded = Object.values(
      hospital.woundedTroops as Record<string, number>,
    ).reduce((s, v) => s + v, 0);

    res.json({
      hospital: {
        kingdomId,
        woundedTroops: hospital.woundedTroops,
        totalWounded,
        capacity: hospital.capacity,
        healRatePerMinute: hospital.healRatePerMinute,
        lastHealAt: hospital.lastHealAt,
      },
    });
  } catch (err) {
    req.log.error({ err }, "GetHospital error");
    res.status(500).json({ error: "Internal server error" });
  }
});

// ---------------------------------------------------------------------------
// POST /api/hospital/heal — Apply heal tick and return updated state
// ---------------------------------------------------------------------------

router.post("/heal", requireAuth, async (req: AuthRequest, res) => {
  const { kingdomId } = req.body as { kingdomId?: unknown };
  const id = Number(kingdomId);
  if (isNaN(id) || id <= 0) {
    res.status(400).json({ error: "Invalid kingdomId" });
    return;
  }

  const kingdom = await kingdomRepository.findById(id);
  if (!kingdom) {
    res.status(404).json({ error: "Kingdom not found" });
    return;
  }
  if (kingdom.userId !== req.user!.userId) {
    res.status(403).json({ error: "You do not own this kingdom" });
    return;
  }

  try {
    const hospital = await hospitalRepository.heal(id);
    if (!hospital) {
      res.status(404).json({ error: "Hospital not initialized for this kingdom" });
      return;
    }
    const totalWounded = Object.values(
      hospital.woundedTroops as Record<string, number>,
    ).reduce((s, v) => s + v, 0);
    res.json({ woundedTroops: hospital.woundedTroops, totalWounded });
  } catch (err) {
    req.log.error({ err }, "HospitalHeal error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
