import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import {
  worldRepository,
  kingdomRepository,
  monsterSpawnRepository,
  crystalNodeRepository,
  spawnRepository,
} from "@workspace/db";

const router = Router();

router.get("/:id/map", async (req, res) => {
  const worldId = Number(String(req.params["id"]));
  if (isNaN(worldId)) {
    res.status(400).json({ error: "Invalid world ID" });
    return;
  }

  try {
    const world = await worldRepository.findById(worldId);

    if (!world) {
      res.status(404).json({ error: "World not found" });
      return;
    }

    const [kingdoms, spawns, crystals] = await Promise.all([
      kingdomRepository.findByWorldId(worldId),
      monsterSpawnRepository.findByWorldIdWithMonster(worldId),
      crystalNodeRepository.findByWorldId(worldId),
    ]);

    res.json({
      world: {
        id: world.id,
        name: world.name,
        description: world.description,
        status: world.status,
        maxKingdoms: world.maxKingdoms,
        currentKingdoms: world.currentKingdoms,
        season: world.season,
        seed: world.seed ?? 42937,
      },
      kingdoms,
      spawns: spawns.map((s) => ({
        id: s.id,
        worldId: s.worldId,
        x: s.x,
        y: s.y,
        currentHp: s.currentHp,
        respawnAt: s.respawnAt,
        monster: {
          id: s.monsterId,
          name: s.monsterName,
          tier: s.monsterTier,
          power: s.monsterPower,
          hp: s.monsterHp,
          assetId: s.monsterAssetId,
        },
      })),
      crystals,
    });
  } catch (err) {
    req.log.error({ err }, "GetWorldMap error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/kingdoms", async (req, res) => {
  const worldId = Number(String(req.params["id"]));
  if (isNaN(worldId)) {
    res.status(400).json({ error: "Invalid world ID" });
    return;
  }

  try {
    const kingdoms = await kingdomRepository.findByWorldId(worldId);
    res.json(kingdoms);
  } catch (err) {
    req.log.error({ err }, "GetWorldKingdoms error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/spawns", async (req, res) => {
  const worldId = Number(String(req.params["id"]));
  if (isNaN(worldId)) {
    res.status(400).json({ error: "Invalid world ID" });
    return;
  }

  try {
    const spawns = await monsterSpawnRepository.findByWorldIdWithMonster(worldId);
    res.json(
      spawns.map((s) => ({
        id: s.id,
        worldId: s.worldId,
        x: s.x,
        y: s.y,
        currentHp: s.currentHp,
        respawnAt: s.respawnAt,
        monster: {
          id: s.monsterId,
          name: s.monsterName,
          tier: s.monsterTier,
          power: s.monsterPower,
          hp: s.monsterHp,
          assetId: s.monsterAssetId,
        },
      })),
    );
  } catch (err) {
    req.log.error({ err }, "GetWorldSpawns error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/active-spawns", async (req, res) => {
  const worldId = Number(String(req.params["id"]));
  if (isNaN(worldId)) {
    res.status(400).json({ error: "Invalid world ID" });
    return;
  }

  try {
    const spawns = await spawnRepository.findActiveByWorldId(worldId);
    res.json({ worldId, spawns });
  } catch (err) {
    req.log.error({ err }, "GetActiveSpawns error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.post("/:id/place-kingdom", requireAuth, async (req: AuthRequest, res) => {
  if (req.user?.role !== "admin") {
    res.status(403).json({ error: "DEV ONLY endpoint" });
    return;
  }

  const worldId = Number(String(req.params["id"]));
  const { kingdomId, x, y } = req.body as { kingdomId: number; x: number; y: number };

  if (isNaN(worldId) || !kingdomId || x === undefined || y === undefined) {
    res.status(400).json({ error: "Invalid input: required kingdomId, x, y" });
    return;
  }

  if (x < 0 || x > 10000 || y < 0 || y > 10000) {
    res.status(400).json({ error: "Coordinates must be between 0 and 10000" });
    return;
  }

  try {
    const kingdom = await kingdomRepository.updatePosition(kingdomId, x, y);

    if (!kingdom) {
      res.status(404).json({ error: "Kingdom not found" });
      return;
    }

    res.json({ message: "Kingdom placed", kingdom: { id: kingdom.id, mapX: kingdom.mapX, mapY: kingdom.mapY } });
  } catch (err) {
    req.log.error({ err }, "PlaceKingdom error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
