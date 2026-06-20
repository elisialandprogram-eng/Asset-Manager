import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import {
  kingdomRepository,
  buildingRepository,
  resourceRepository,
  constructionRepository,
} from "@workspace/db";
import {
  checkCanConstructBuilding,
  checkAffordability,
  computeConstructionCost,
  computeConstructionDuration,
  computeConstructionOptions,
  CONSTRUCTABLE_BUILDING_TYPES,
  getPalaceTier,
  type BuildingType,
  type BuildingCountSnapshot,
} from "@workspace/game-engine";

const router = Router();

router.get("/:id/construction-queue", requireAuth, async (req: AuthRequest, res) => {
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

    const queue = await constructionRepository.findInProgressByKingdomId(id);
    res.json(queue);
  } catch (err) {
    req.log.error({ err }, "GetConstructionQueue error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/construction-options", requireAuth, async (req: AuthRequest, res) => {
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

    const [buildings, resources] = await Promise.all([
      buildingRepository.findByKingdomId(id),
      resourceRepository.findByKingdomId(id),
    ]);

    const palaceBuilding = buildings.find((b) => b.buildingType === "palace");
    const resolvedPalaceLevel = palaceBuilding?.level ?? 1;

    const existingCounts: BuildingCountSnapshot[] = CONSTRUCTABLE_BUILDING_TYPES.map((bt) => ({
      buildingType: bt,
      count: buildings.filter((b) => b.buildingType === bt).length,
    }));
    existingCounts.push({ buildingType: "palace", count: buildings.filter((b) => b.buildingType === "palace").length });

    const currentResources = resources ?? { food: 0, wood: 0, stone: 0, iron: 0, gold: 0 };
    const options = computeConstructionOptions(resolvedPalaceLevel, existingCounts, currentResources);

    res.json({
      palaceLevel: resolvedPalaceLevel,
      palaceTier: getPalaceTier(resolvedPalaceLevel),
      options,
    });
  } catch (err) {
    req.log.error({ err }, "GetConstructionOptions error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.post("/:id/construct", requireAuth, async (req: AuthRequest, res) => {
  const id = parseInt(String(req.params["id"] ?? ""));
  if (isNaN(id)) {
    res.status(400).json({ error: "Invalid kingdom ID" });
    return;
  }

  const { buildingType } = req.body as { buildingType?: string };
  if (!buildingType || !CONSTRUCTABLE_BUILDING_TYPES.includes(buildingType as BuildingType)) {
    res.status(400).json({
      error: "Invalid buildingType",
      valid: CONSTRUCTABLE_BUILDING_TYPES,
    });
    return;
  }

  const btype = buildingType as BuildingType;

  try {
    const kingdom = await kingdomRepository.findByIdAndUserId(id, req.user!.userId);

    if (!kingdom) {
      res.status(404).json({ error: "Kingdom not found" });
      return;
    }

    const [buildings, resources] = await Promise.all([
      buildingRepository.findSnapshotsByKingdomId(id),
      resourceRepository.findByKingdomId(id),
    ]);

    const palaceBuilding = buildings.find((b) => b.buildingType === "palace");
    const palaceLevel = palaceBuilding?.level ?? 1;

    const existingCounts: BuildingCountSnapshot[] = [
      ...CONSTRUCTABLE_BUILDING_TYPES,
      "palace" as BuildingType,
    ].map((bt) => ({
      buildingType: bt,
      count: buildings.filter((b) => b.buildingType === bt).length,
    }));

    const slotCheck = checkCanConstructBuilding(btype, palaceLevel, existingCounts);
    if (!slotCheck.allowed) {
      res.status(409).json({ error: slotCheck.reason });
      return;
    }

    if (!resources) {
      res.status(500).json({ error: "Kingdom resources not found" });
      return;
    }

    const cost = computeConstructionCost(btype);
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

    const durationSeconds = computeConstructionDuration(btype);
    const startsAt = new Date();
    const endsAt = new Date(startsAt.getTime() + durationSeconds * 1000);
    const assetId = `building_${btype}_001`;

    await resourceRepository.deduct(id, resources, cost);

    const newBuilding = await buildingRepository.insert({
      kingdomId: id,
      buildingType: btype,
      level: 0,
      assetId,
      isConstructing: true,
      constructionEndsAt: endsAt,
    });

    const queueItem = await constructionRepository.insert({
      kingdomId: id,
      buildingId: newBuilding.id,
      buildingType: btype,
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
      message: "Construction started",
      construction: queueItem,
      building: newBuilding,
      durationSeconds,
      endsAt,
      costDeducted: cost,
    });
  } catch (err) {
    req.log.error({ err }, "ConstructBuilding error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
