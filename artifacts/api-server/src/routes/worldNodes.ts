/**
 * worldNodes.ts — Resource node endpoints for the world map.
 *
 * GET /api/worlds/:id/resource-nodes
 *   Returns all ACTIVE resource-type world_spawns for a world.
 *   Polled by Unity at a 30-second interval.
 *
 * GET /api/worlds/:id/monster-nodes
 *   Returns all ACTIVE monster-type world_spawns for a world.
 *   Polled by Unity at a 30-second interval.
 */

import { Router } from "express";
import { spawnRepository } from "@workspace/db";

const router = Router();

router.get("/:id/resource-nodes", async (req, res) => {
  const worldId = Number(String(req.params["id"]));
  if (isNaN(worldId)) {
    res.status(400).json({ error: "Invalid world ID" });
    return;
  }

  try {
    const all = await spawnRepository.findActiveByWorldId(worldId);
    const nodes = all.filter((s) => s.spawnType === "resource");
    res.json({
      worldId,
      nodes: nodes.map((n) => ({
        id:          n.id,
        worldId:     n.worldId,
        spawnType:   n.spawnType,
        spawnSubtype: n.spawnSubtype,
        level:       n.level,
        tileX:       n.tileX,
        tileY:       n.tileY,
        posX:        n.posX,
        posY:        n.posY,
        biome:       n.biome,
        status:      n.status,
        spawnedAt:   n.spawnedAt,
        expiresAt:   n.expiresAt,
      })),
    });
  } catch (err) {
    req.log.error({ err }, "GetResourceNodes error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:id/monster-nodes", async (req, res) => {
  const worldId = Number(String(req.params["id"]));
  if (isNaN(worldId)) {
    res.status(400).json({ error: "Invalid world ID" });
    return;
  }

  try {
    const all = await spawnRepository.findActiveByWorldId(worldId);
    const monsters = all.filter((s) => s.spawnType === "monster");
    res.json({
      worldId,
      monsters: monsters.map((m) => ({
        id:          m.id,
        worldId:     m.worldId,
        spawnType:   m.spawnType,
        spawnSubtype: m.spawnSubtype,
        level:       m.level,
        tileX:       m.tileX,
        tileY:       m.tileY,
        posX:        m.posX,
        posY:        m.posY,
        biome:       m.biome,
        status:      m.status,
        spawnedAt:   m.spawnedAt,
        expiresAt:   m.expiresAt,
      })),
    });
  } catch (err) {
    req.log.error({ err }, "GetMonsterNodes error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
