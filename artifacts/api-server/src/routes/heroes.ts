/**
 * heroes.ts — Hero management endpoints.
 *
 * GET  /api/heroes?kingdomId=X   — List heroes for a kingdom
 * GET  /api/heroes/:id           — Get a specific hero
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { heroRepository, kingdomRepository } from "@workspace/db";

const router = Router();

// ---------------------------------------------------------------------------
// GET /api/heroes?kingdomId=X
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
    const heroes = await heroRepository.findByKingdomId(kingdomId);
    res.json({ heroes });
  } catch (err) {
    req.log.error({ err }, "ListHeroes error");
    res.status(500).json({ error: "Internal server error" });
  }
});

// ---------------------------------------------------------------------------
// GET /api/heroes/:id
// ---------------------------------------------------------------------------

router.get("/:id", requireAuth, async (req: AuthRequest, res) => {
  const heroId = Number(String(req.params["id"]));
  if (isNaN(heroId)) {
    res.status(400).json({ error: "Invalid hero ID" });
    return;
  }

  try {
    const hero = await heroRepository.findById(heroId);
    if (!hero) {
      res.status(404).json({ error: "Hero not found" });
      return;
    }

    const kingdom = await kingdomRepository.findById(hero.kingdomId);
    if (!kingdom || kingdom.userId !== req.user!.userId) {
      res.status(403).json({ error: "Forbidden" });
      return;
    }

    res.json({ hero });
  } catch (err) {
    req.log.error({ err }, "GetHero error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
