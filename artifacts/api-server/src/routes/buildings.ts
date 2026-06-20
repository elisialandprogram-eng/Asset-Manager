import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import {
  buildingRepository,
  upgradeQueueRepository,
  resourceRepository,
  kingdomRepository,
} from "@workspace/db";
import {
  validateUpgrade,
  computeUpgradeCost,
  computeUpgradeDuration,
  checkAffordability,
  type BuildingType,
} from "@workspace/game-engine";

const router = Router();

router.post("/:buildingId/upgrade", requireAuth, async (req: AuthRequest, res) => {
  const buildingId = parseInt(String(req.params["buildingId"] ?? ""));
  if (isNaN(buildingId)) {
    res.status(400).json({ error: "Invalid building ID" });
    return;
  }

  try {
    const building = await buildingRepository.findById(buildingId);

    if (!building) {
      res.status(404).json({ error: "Building not found" });
      return;
    }

    const kingdom = await kingdomRepository.findByIdAndUserId(building.kingdomId, req.user!.userId);

    if (!kingdom) {
      res.status(403).json({ error: "You do not own this building" });
      return;
    }

    const validation = validateUpgrade(building.level, building.isConstructing);
    if (!validation.valid) {
      res.status(409).json({ error: validation.reason });
      return;
    }

    const cost = computeUpgradeCost(building.buildingType as BuildingType, building.level);

    const resources = await resourceRepository.findByKingdomId(building.kingdomId);

    if (!resources) {
      res.status(500).json({ error: "Kingdom resources not found" });
      return;
    }

    const affordability = checkAffordability(
      { food: resources.food, wood: resources.wood, stone: resources.stone, iron: resources.iron, gold: resources.gold },
      cost,
    );

    if (!affordability.canAfford) {
      res.status(402).json({
        error: "Insufficient resources",
        shortfall: affordability.shortfall,
        required: cost,
      });
      return;
    }

    await resourceRepository.deduct(building.kingdomId, resources, cost);

    const durationSeconds = computeUpgradeDuration(building.buildingType as BuildingType, building.level);
    const startsAt = new Date();
    const endsAt = new Date(startsAt.getTime() + durationSeconds * 1000);

    await buildingRepository.markConstructing(buildingId, endsAt);

    const queueItem = await upgradeQueueRepository.insert({
      kingdomId: building.kingdomId,
      buildingId: building.id,
      fromLevel: building.level,
      toLevel: building.level + 1,
      foodCost: cost.food,
      woodCost: cost.wood,
      stoneCost: cost.stone,
      ironCost: cost.iron,
      goldCost: cost.gold,
      startsAt,
      endsAt,
      status: "in_progress",
    });

    res.status(202).json({
      message: "Upgrade started",
      upgrade: queueItem,
      durationSeconds,
      endsAt,
      costDeducted: cost,
    });
  } catch (err) {
    req.log.error({ err }, "UpgradeBuilding error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:buildingId/upgrade-preview", requireAuth, async (req: AuthRequest, res) => {
  const buildingId = parseInt(String(req.params["buildingId"] ?? ""));
  if (isNaN(buildingId)) {
    res.status(400).json({ error: "Invalid building ID" });
    return;
  }

  try {
    const building = await buildingRepository.findById(buildingId);

    if (!building) {
      res.status(404).json({ error: "Building not found" });
      return;
    }

    const cost = computeUpgradeCost(building.buildingType as BuildingType, building.level);
    const durationSeconds = computeUpgradeDuration(building.buildingType as BuildingType, building.level);
    const validation = validateUpgrade(building.level, building.isConstructing);

    const resources = await resourceRepository.findByKingdomId(building.kingdomId);

    const affordability = resources
      ? checkAffordability(
          { food: resources.food, wood: resources.wood, stone: resources.stone, iron: resources.iron, gold: resources.gold },
          cost,
        )
      : { canAfford: false, shortfall: cost };

    res.json({
      buildingId: building.id,
      buildingType: building.buildingType,
      currentLevel: building.level,
      targetLevel: building.level + 1,
      canUpgrade: validation.valid,
      reason: validation.reason,
      cost,
      durationSeconds,
      canAfford: affordability.canAfford,
      shortfall: affordability.shortfall,
    });
  } catch (err) {
    req.log.error({ err }, "UpgradePreview error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
