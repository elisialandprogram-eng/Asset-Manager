import { Router } from "express";
import { count } from "drizzle-orm";
import { db, troopsTable } from "@workspace/db";
import { eq } from "drizzle-orm";
import {
  kingdomRepository,
  buildingRepository,
  resourceRepository,
} from "@workspace/db";
import { requireAuth, type AuthRequest } from "../middlewares/auth";

const router = Router();

router.get("/mine", requireAuth, async (req: AuthRequest, res) => {
  try {
    const kingdom = await kingdomRepository.findByUserId(req.user!.userId);

    if (!kingdom) {
      res.status(404).json({ error: "Kingdom not found" });
      return;
    }

    res.json(kingdom);
  } catch (err) {
    req.log.error({ err }, "GetMyKingdom error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id", async (req, res) => {
  const id = parseInt(req.params["id"] ?? "");
  if (isNaN(id)) {
    res.status(400).json({ error: "Invalid kingdom ID" });
    return;
  }

  try {
    const kingdom = await kingdomRepository.findById(id);

    if (!kingdom) {
      res.status(404).json({ error: "Kingdom not found" });
      return;
    }

    res.json(kingdom);
  } catch (err) {
    req.log.error({ err }, "GetKingdom error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/summary", async (req, res) => {
  const id = parseInt(req.params["id"] ?? "");
  if (isNaN(id)) {
    res.status(400).json({ error: "Invalid kingdom ID" });
    return;
  }

  try {
    const kingdom = await kingdomRepository.findById(id);

    if (!kingdom) {
      res.status(404).json({ error: "Kingdom not found" });
      return;
    }

    const [resources, [buildingCountResult], [troopCountResult]] = await Promise.all([
      resourceRepository.findByKingdomId(id),
      db.select({ count: count() }).from(troopsTable).where(eq(troopsTable.kingdomId, id)),
      db.select({ count: count() }).from(troopsTable).where(eq(troopsTable.kingdomId, id)),
    ]);

    const buildings = await buildingRepository.findByKingdomId(id);

    res.json({
      kingdom,
      resources: resources ?? { kingdomId: id, food: 0, wood: 0, stone: 0, iron: 0, gold: 0, updatedAt: new Date() },
      buildingCount: buildings.length,
      troopCount: troopCountResult?.count ?? 0,
      power: kingdom.power,
    });
  } catch (err) {
    req.log.error({ err }, "GetKingdomSummary error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/buildings", async (req, res) => {
  const id = parseInt(req.params["id"] ?? "");
  if (isNaN(id)) {
    res.status(400).json({ error: "Invalid kingdom ID" });
    return;
  }

  try {
    const buildings = await buildingRepository.findByKingdomId(id);
    res.json(buildings);
  } catch (err) {
    req.log.error({ err }, "GetKingdomBuildings error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/resources", async (req, res) => {
  const id = parseInt(req.params["id"] ?? "");
  if (isNaN(id)) {
    res.status(400).json({ error: "Invalid kingdom ID" });
    return;
  }

  try {
    const resources = await resourceRepository.findByKingdomId(id);

    if (!resources) {
      res.status(404).json({ error: "Resources not found" });
      return;
    }

    res.json(resources);
  } catch (err) {
    req.log.error({ err }, "GetKingdomResources error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
