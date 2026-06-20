/**
 * spawnProcessor.ts — Main orchestrator for a single spawn cycle.
 *
 * A "cycle" runs the full lifecycle sequence for one world:
 *   1. Expire stale spawns + delete cleaned-up rows
 *   2. Generate new spawns to fill population gaps
 *   3. Bulk-insert new spawn rows
 *
 * Called by the scheduler (every N minutes) and by admin endpoints.
 */

import { kingdomRepository } from "@workspace/db";
import { worldRepository } from "@workspace/db";
import { processExpiredSpawns } from "./spawnExpiryProcessor";
import { generateMissingSpawns } from "./spawnGenerator";
import { spawnRepository } from "./spawnRepository";
import { logger } from "../../lib/logger";

export interface CycleResult {
  worldId: number;
  markedExpired: number;
  deleted: number;
  generated: number;
}

/**
 * Runs one complete spawn cycle for a world.
 * Safe to call at any frequency — idempotent if nothing needs changing.
 */
export async function runSpawnCycle(worldId: number): Promise<CycleResult> {
  // Get world seed for terrain noise calculations
  const world = await worldRepository.findById(worldId);
  const worldSeed = world?.seed ?? 42937;

  // Get all kingdom positions to avoid overlapping spawns with kingdoms
  const kingdoms = await kingdomRepository.findPlacedByWorldId(worldId);
  const kingdomPositions = kingdoms
    .filter((k) => k.x !== null && k.y !== null)
    .map((k) => ({ posX: k.x as number, posY: k.y as number }));

  // Step 1: Expire + cleanup stale rows
  const { markedExpired, deleted } = await processExpiredSpawns(worldId);

  // Step 2: Generate replacement spawns
  const newSpawns = await generateMissingSpawns(worldId, worldSeed, kingdomPositions);

  // Step 3: Persist new spawns
  let generated = 0;
  if (newSpawns.length > 0) {
    await spawnRepository.insertMany(newSpawns);
    generated = newSpawns.length;
  }

  logger.info(
    { worldId, markedExpired, deleted, generated },
    "Spawn cycle complete",
  );

  return { worldId, markedExpired, deleted, generated };
}

/**
 * Resets all spawns for a world:
 *   1. Deletes every row in world_spawns for this worldId
 *   2. Runs a fresh generation cycle
 */
export async function resetWorldSpawns(worldId: number): Promise<{ deleted: number; generated: number }> {
  const deleted = await spawnRepository.deleteAllByWorldId(worldId);

  const world = await worldRepository.findById(worldId);
  const worldSeed = world?.seed ?? 42937;
  const kingdoms = await kingdomRepository.findPlacedByWorldId(worldId);
  const kingdomPositions = kingdoms
    .filter((k) => k.x !== null && k.y !== null)
    .map((k) => ({ posX: k.x as number, posY: k.y as number }));

  const newSpawns = await generateMissingSpawns(worldId, worldSeed, kingdomPositions);
  if (newSpawns.length > 0) {
    await spawnRepository.insertMany(newSpawns);
  }

  logger.info({ worldId, deleted, generated: newSpawns.length }, "World spawns reset");
  return { deleted, generated: newSpawns.length };
}

/**
 * Cleanup-only pass: deletes expired/depleted rows older than 1 hour.
 * Does NOT expire currently active spawns or generate new ones.
 */
export async function cleanupWorldSpawns(worldId: number): Promise<{ removed: number }> {
  const cleanupBefore = new Date(Date.now() - 60 * 60 * 1000);
  const removed = await spawnRepository.deleteStaleByWorldId(worldId, cleanupBefore);
  logger.info({ worldId, removed }, "Spawn cleanup complete");
  return { removed };
}
