/**
 * marches.ts — March CRUD endpoints.
 *
 * POST   /api/marches          — Create a gather march
 * GET    /api/marches          — List active marches for the authenticated kingdom
 * DELETE /api/marches/:id      — Cancel an outbound march (recall)
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import {
  marchRepository,
  kingdomRepository,
  spawnRepository,
} from "@workspace/db";
import {
  calculateGatherMarchTimings,
  calculateCarryCapacity,
  calculateGatherYield,
  type TroopComposition,
} from "@workspace/game-engine";
import type { TroopLoad } from "@workspace/db";

const router = Router();

// ---------------------------------------------------------------------------
// POST /api/marches — Create gather march
// ---------------------------------------------------------------------------

router.post("/", requireAuth, async (req: AuthRequest, res) => {
  const body = req.body as {
    kingdomId?: unknown;
    worldId?: unknown;
    spawnId?: unknown;
    troops?: Record<string, unknown>;
  };

  const kingdomId = Number(body.kingdomId);
  const worldId   = Number(body.worldId);
  const spawnId   = Number(body.spawnId);

  if (isNaN(kingdomId) || kingdomId <= 0) {
    res.status(400).json({ error: "Invalid kingdomId" });
    return;
  }
  if (isNaN(worldId) || worldId <= 0) {
    res.status(400).json({ error: "Invalid worldId" });
    return;
  }
  if (isNaN(spawnId) || spawnId <= 0) {
    res.status(400).json({ error: "Invalid spawnId" });
    return;
  }

  const rawTroops = (body.troops ?? {}) as Record<string, unknown>;
  const troops: TroopLoad = {
    militia:      Number(rawTroops["militia"]      ?? 0),
    spearman:     Number(rawTroops["spearman"]     ?? 0),
    archer:       Number(rawTroops["archer"]       ?? 0),
    scout:        Number(rawTroops["scout"]        ?? 0),
    knight:       Number(rawTroops["knight"]       ?? 0),
    catapult:     Number(rawTroops["catapult"]     ?? 0),
    dragon_rider: Number(rawTroops["dragon_rider"] ?? 0),
  };

  const totalTroops =
    (troops.militia      ?? 0) +
    (troops.spearman     ?? 0) +
    (troops.archer       ?? 0) +
    (troops.scout        ?? 0) +
    (troops.knight       ?? 0) +
    (troops.catapult     ?? 0) +
    (troops.dragon_rider ?? 0);

  if (totalTroops <= 0) {
    res.status(400).json({ error: "At least one troop must be sent" });
    return;
  }

  const userId = req.user!.userId;

  // Validate kingdom ownership
  const kingdom = await kingdomRepository.findById(kingdomId);
  if (!kingdom) {
    res.status(404).json({ error: "Kingdom not found" });
    return;
  }
  if (kingdom.userId !== userId) {
    res.status(403).json({ error: "You do not own this kingdom" });
    return;
  }

  // Validate the spawn exists and is active
  const activeSpawns = await spawnRepository.findActiveByWorldId(worldId);
  const spawn = activeSpawns.find((s) => s.id === spawnId);
  if (!spawn) {
    res.status(404).json({ error: "Resource node not found or no longer active" });
    return;
  }
  if (spawn.spawnType !== "resource") {
    res.status(400).json({ error: "Target spawn is not a resource node" });
    return;
  }

  // Kingdom must have a map position
  if (kingdom.mapX === null || kingdom.mapY === null) {
    res.status(400).json({ error: "Kingdom has no map position" });
    return;
  }

  // Convert backend coords (0–10000) to tile coords (0–2047)
  const toTile = (v: number) => Math.floor(v * 0.2048);
  const originX = toTile(kingdom.mapX);
  const originY = toTile(kingdom.mapY);
  const destX   = toTile(spawn.posX);
  const destY   = toTile(spawn.posY);

  const startedAt = new Date();

  const troopComp: TroopComposition = {
    militia:      troops.militia      ?? 0,
    spearman:     troops.spearman     ?? 0,
    archer:       troops.archer       ?? 0,
    scout:        troops.scout        ?? 0,
    knight:       troops.knight       ?? 0,
    catapult:     troops.catapult     ?? 0,
    dragon_rider: troops.dragon_rider ?? 0,
  };

  const timings = calculateGatherMarchTimings({
    originX, originY, destX, destY,
    troops: troopComp,
    nodeLevel: spawn.level,
    startedAt,
  });

  try {
    const march = await marchRepository.insert({
      worldId,
      kingdomId,
      marchType: "gather",
      status: "outbound",
      originX,
      originY,
      destX,
      destY,
      spawnId,
      targetKingdomId: null,
      troops,
      speedTpm: timings.speedTpm,
      distanceTiles: timings.distanceTiles,
      startedAt,
      arrivesAt: timings.arrivesAt,
      gatherEndsAt: timings.gatherEndsAt,
      returnStartedAt: null,
      returnsAt: timings.returnsAt,
      completedAt: null,
      resourcesGathered: null,
    });

    res.status(201).json({
      march: {
        id:             march.id,
        worldId:        march.worldId,
        kingdomId:      march.kingdomId,
        marchType:      march.marchType,
        status:         march.status,
        originX:        march.originX,
        originY:        march.originY,
        destX:          march.destX,
        destY:          march.destY,
        spawnId:        march.spawnId,
        troops:         march.troops,
        speedTpm:       march.speedTpm,
        distanceTiles:  march.distanceTiles,
        startedAt:      march.startedAt,
        arrivesAt:      march.arrivesAt,
        gatherEndsAt:   march.gatherEndsAt,
        returnsAt:      march.returnsAt,
        carryCapacity:  calculateCarryCapacity(troopComp),
        estimatedYield: calculateGatherYield(spawn.level, troopComp),
      },
    });
  } catch (err) {
    req.log.error({ err }, "CreateMarch error");
    res.status(500).json({ error: "Internal server error" });
  }
});

// ---------------------------------------------------------------------------
// GET /api/marches?kingdomId=X
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
    const marches = await marchRepository.findActiveByKingdomId(kingdomId);
    res.json({ marches });
  } catch (err) {
    req.log.error({ err }, "ListMarches error");
    res.status(500).json({ error: "Internal server error" });
  }
});

// ---------------------------------------------------------------------------
// DELETE /api/marches/:id — Cancel (recall) an outbound march
// ---------------------------------------------------------------------------

router.delete("/:id", requireAuth, async (req: AuthRequest, res) => {
  const marchId = Number(String(req.params["id"]));
  if (isNaN(marchId)) {
    res.status(400).json({ error: "Invalid march ID" });
    return;
  }

  const march = await marchRepository.findById(marchId);
  if (!march) {
    res.status(404).json({ error: "March not found" });
    return;
  }

  const kingdom = await kingdomRepository.findById(march.kingdomId);
  if (!kingdom || kingdom.userId !== req.user!.userId) {
    res.status(403).json({ error: "You do not own this kingdom" });
    return;
  }

  if (march.status !== "outbound") {
    res.status(409).json({
      error: "March cannot be cancelled",
      detail: `March is in state '${march.status}' — only outbound marches can be recalled`,
    });
    return;
  }

  try {
    await marchRepository.markCancelled(marchId);
    res.json({ message: "March recalled successfully", marchId });
  } catch (err) {
    req.log.error({ err }, "CancelMarch error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
