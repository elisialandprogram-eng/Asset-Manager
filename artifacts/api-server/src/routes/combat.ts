/**
 * combat.ts — PvE monster attack endpoint.
 *
 * POST /api/monsters/:spawnId/attack — Initiate a monster attack march
 *
 * Flow:
 *   1. Validate kingdom ownership
 *   2. Validate monster spawn exists and is active
 *   3. Validate hero (optional)
 *   4. Validate troops exist in troop_inventory
 *   5. Deduct AP (based on monster tier)
 *   6. Deduct troops from inventory
 *   7. Create attack_monster march
 *   8. March processor resolves combat on arrival and sets march → returning
 *
 * GET /api/monsters/:spawnId — Get monster info + AP cost
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import {
  marchRepository,
  kingdomRepository,
  heroRepository,
  actionPointRepository,
  troopInventoryRepository,
  monsterSpawnRepository,
  monsterRepository,
} from "@workspace/db";
import {
  calculateDistance,
  calculateMarchSpeed,
  calculateTravelSeconds,
  getMonsterApCost,
  type TroopComposition,
  TROOP_DEFINITIONS,
  type TroopKey,
} from "@workspace/game-engine";
import type { TroopCount } from "@workspace/db";

const router = Router();

// ---------------------------------------------------------------------------
// GET /api/monsters/:spawnId — Monster info + AP cost
// ---------------------------------------------------------------------------

router.get("/:spawnId", requireAuth, async (req: AuthRequest, res) => {
  const spawnId = Number(String(req.params["spawnId"]));
  if (isNaN(spawnId)) {
    res.status(400).json({ error: "Invalid spawnId" });
    return;
  }

  try {
    const spawn = await monsterSpawnRepository.findById(spawnId);
    if (!spawn) {
      res.status(404).json({ error: "Monster spawn not found" });
      return;
    }

    const monster = await monsterRepository.findById(spawn.monsterId);
    if (!monster) {
      res.status(404).json({ error: "Monster definition not found" });
      return;
    }

    const apCost = getMonsterApCost(monster.tier);

    res.json({
      spawn: {
        id: spawn.id,
        monsterId: spawn.monsterId,
        worldId: spawn.worldId,
        x: spawn.x,
        y: spawn.y,
        currentHp: spawn.currentHp,
        respawnAt: spawn.respawnAt,
        defeatedByKingdomId: spawn.defeatedByKingdomId,
      },
      monster: {
        id: monster.id,
        name: monster.name,
        tier: monster.tier,
        power: monster.power,
        hp: monster.hp,
        attack: monster.attack,
        defense: monster.defense,
        assetId: monster.assetId,
      },
      apCost,
    });
  } catch (err) {
    req.log.error({ err }, "GetMonsterSpawn error");
    res.status(500).json({ error: "Internal server error" });
  }
});

// ---------------------------------------------------------------------------
// POST /api/monsters/:spawnId/attack — Send an attack march
// ---------------------------------------------------------------------------

router.post("/:spawnId/attack", requireAuth, async (req: AuthRequest, res) => {
  const spawnId = Number(String(req.params["spawnId"]));
  if (isNaN(spawnId)) {
    res.status(400).json({ error: "Invalid spawnId" });
    return;
  }

  const body = req.body as {
    kingdomId?: unknown;
    heroId?: unknown;
    troops?: Record<string, unknown>;
  };

  const kingdomId = Number(body.kingdomId);
  if (isNaN(kingdomId) || kingdomId <= 0) {
    res.status(400).json({ error: "Invalid kingdomId" });
    return;
  }

  // ── 1. Validate kingdom ownership ────────────────────────────────────────
  const kingdom = await kingdomRepository.findById(kingdomId);
  if (!kingdom) {
    res.status(404).json({ error: "Kingdom not found" });
    return;
  }
  if (kingdom.userId !== req.user!.userId) {
    res.status(403).json({ error: "You do not own this kingdom" });
    return;
  }
  if (kingdom.mapX === null || kingdom.mapY === null) {
    res.status(400).json({ error: "Kingdom has no map position" });
    return;
  }

  // ── 2. Validate monster spawn ─────────────────────────────────────────────
  const spawn = await monsterSpawnRepository.findById(spawnId);
  if (!spawn) {
    res.status(404).json({ error: "Monster spawn not found" });
    return;
  }
  if (spawn.respawnAt && spawn.respawnAt > new Date()) {
    res.status(409).json({ error: "Monster is not yet available (respawning)" });
    return;
  }
  if (spawn.defeatedByKingdomId !== null && !spawn.respawnAt) {
    res.status(409).json({ error: "Monster has already been defeated" });
    return;
  }

  const monster = await monsterRepository.findById(spawn.monsterId);
  if (!monster) {
    res.status(404).json({ error: "Monster definition not found" });
    return;
  }

  // ── 3. Validate hero (optional) ───────────────────────────────────────────
  const heroId = body.heroId != null ? Number(body.heroId) : null;
  if (heroId !== null) {
    const hero = await heroRepository.findById(heroId);
    if (!hero || hero.kingdomId !== kingdomId) {
      res.status(400).json({ error: "Hero not found or does not belong to this kingdom" });
      return;
    }
  }

  // ── 4. Validate troops ───────────────────────────────────────────────────
  const rawTroops = (body.troops ?? {}) as Record<string, unknown>;
  const troops: TroopCount = {};
  for (const [key, val] of Object.entries(rawTroops)) {
    const count = Number(val);
    if (TROOP_DEFINITIONS[key as TroopKey] && count > 0) {
      troops[key] = count;
    }
  }

  const totalTroops = Object.values(troops).reduce((s, v) => s + v, 0);
  if (totalTroops <= 0) {
    res.status(400).json({ error: "At least one troop must be sent" });
    return;
  }

  const inventory = await troopInventoryRepository.findByKingdomId(kingdomId);
  if (!inventory) {
    res.status(400).json({ error: "No troop inventory found for this kingdom" });
    return;
  }

  const inv = inventory.troops as TroopCount;
  for (const [key, needed] of Object.entries(troops)) {
    const have = inv[key] ?? 0;
    if (have < needed) {
      res.status(400).json({
        error: `Insufficient troops: need ${needed} ${key}, have ${have}`,
      });
      return;
    }
  }

  // ── 5. Check and deduct AP ────────────────────────────────────────────────
  const apCost = getMonsterApCost(monster.tier);
  try {
    await actionPointRepository.deduct(kingdomId, apCost);
  } catch (err) {
    const msg = err instanceof Error ? err.message : "Insufficient AP";
    res.status(400).json({ error: msg });
    return;
  }

  try {
    // ── 6. Deduct troops from inventory ────────────────────────────────────
    await troopInventoryRepository.deductTroops(kingdomId, troops);

    // ── 7. Calculate march timings ─────────────────────────────────────────
    const toTile = (v: number) => Math.floor(v * 0.2048);
    const originX = toTile(kingdom.mapX!);
    const originY = toTile(kingdom.mapY!);
    const destX   = toTile(spawn.x * 10); // spawn coords are in world space
    const destY   = toTile(spawn.y * 10);

    const troopComp: TroopComposition = {};
    for (const [key, count] of Object.entries(troops)) {
      const def = TROOP_DEFINITIONS[key as TroopKey];
      if (def) {
        const legacyKey = `${def.class}` as keyof TroopComposition;
        troopComp[legacyKey] = (troopComp[legacyKey] ?? 0) + count;
      }
    }

    const speedTpm = calculateMarchSpeed(troopComp);
    const distanceTiles = calculateDistance(originX, originY, destX, destY);
    const travelSeconds = calculateTravelSeconds(
      Math.max(distanceTiles, 1),
      speedTpm,
    );

    const startedAt = new Date();
    const arrivesAt = new Date(startedAt.getTime() + travelSeconds * 1000);
    const returnsAt = new Date(arrivesAt.getTime() + travelSeconds * 1000);

    // ── 8. Create march record ─────────────────────────────────────────────
    const march = await marchRepository.insert({
      worldId:         spawn.worldId,
      kingdomId,
      marchType:       "attack_monster",
      status:          "outbound",
      originX,
      originY,
      destX,
      destY,
      spawnId,
      targetKingdomId: null,
      troops,
      speedTpm,
      distanceTiles:   Math.max(distanceTiles, 1),
      startedAt,
      arrivesAt,
      gatherEndsAt:    null,
      returnStartedAt: null,
      returnsAt,
      completedAt:     null,
      resourcesGathered: null,
      heroId,
      battleReportId:  null,
    });

    res.status(201).json({
      march: {
        id:            march.id,
        worldId:       march.worldId,
        kingdomId:     march.kingdomId,
        marchType:     march.marchType,
        status:        march.status,
        spawnId:       march.spawnId,
        troops,
        heroId:        march.heroId,
        speedTpm:      march.speedTpm,
        distanceTiles: march.distanceTiles,
        startedAt:     march.startedAt,
        arrivesAt:     march.arrivesAt,
        returnsAt:     march.returnsAt,
        apCost,
        monsterName:   monster.name,
        monsterTier:   monster.tier,
      },
    });
  } catch (err) {
    // Rollback AP deduction on error
    try {
      await actionPointRepository.add(kingdomId, apCost);
    } catch {
      // best-effort rollback
    }
    req.log.error({ err }, "MonsterAttack error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
