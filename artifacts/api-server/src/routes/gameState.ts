import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import {
  kingdomRepository,
  buildingRepository,
  resourceRepository,
  upgradeQueueRepository,
} from "@workspace/db";
import {
  computeProductionRates,
  computeResourceCaps,
  computeKingdomPower,
  getPalaceTier,
  type BuildingSnapshot,
  type BuildingType,
} from "@workspace/game-engine";

const router = Router();

router.get("/:id/state", requireAuth, async (req: AuthRequest, res) => {
  const id = parseInt(String(req.params["id"] ?? ""));
  if (isNaN(id)) {
    res.status(400).json({ error: "Invalid kingdom ID" });
    return;
  }

  try {
    const kingdom = await kingdomRepository.findByIdAndUserId(id, req.user!.userId);

    if (!kingdom) {
      res.status(404).json({ error: "Kingdom not found" });
      return;
    }

    const [buildings, resources, activeQueue] = await Promise.all([
      buildingRepository.findByKingdomId(id),
      resourceRepository.findByKingdomId(id),
      upgradeQueueRepository.findInProgressByKingdomId(id),
    ]);

    const snapshots: BuildingSnapshot[] = buildings.map((b) => ({
      buildingType: b.buildingType as BuildingType,
      level: b.level,
      isConstructing: b.isConstructing,
    }));

    const productionRates = computeProductionRates(snapshots);
    const resourceCaps = computeResourceCaps(snapshots);
    const power = computeKingdomPower(
      snapshots.map((s) => ({ buildingType: s.buildingType, level: s.level })),
    );

    const palace = buildings.find((b) => b.buildingType === "palace");
    const palaceTier = getPalaceTier(palace?.level ?? 0);

    const currentResources = resources ?? {
      food: 0,
      wood: 0,
      stone: 0,
      iron: 0,
      gold: 0,
      updatedAt: new Date(),
    };

    res.json({
      kingdom,
      resources: currentResources,
      buildings,
      activeQueue,
      productionRates,
      resourceCaps,
      palaceTier,
      power,
      resourcesLastUpdated: currentResources.updatedAt,
    });
  } catch (err) {
    req.log.error({ err }, "GetKingdomState error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/queue", requireAuth, async (req: AuthRequest, res) => {
  const id = parseInt(String(req.params["id"] ?? ""));
  if (isNaN(id)) {
    res.status(400).json({ error: "Invalid kingdom ID" });
    return;
  }

  try {
    const kingdom = await kingdomRepository.findIdByIdAndUserId(id, req.user!.userId);

    if (!kingdom) {
      res.status(404).json({ error: "Kingdom not found" });
      return;
    }

    const queue = await upgradeQueueRepository.findInProgressByKingdomId(id);
    res.json(queue);
  } catch (err) {
    req.log.error({ err }, "GetKingdomQueue error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
