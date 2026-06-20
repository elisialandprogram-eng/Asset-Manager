/**
 * spawnScheduler.ts — Automated recurring spawn cycle.
 *
 * Starts a setInterval on server startup that runs the spawn lifecycle
 * for every active world every N minutes (configured in world-spawns.json).
 *
 * Design goals:
 *   - Zero manual intervention required
 *   - Errors are logged but do NOT crash the server
 *   - Configurable interval from world-spawns.json (no code change needed)
 *   - First cycle runs immediately on startup to populate an empty DB
 */

import { worldRepository } from "@workspace/db";
import { runSpawnCycle } from "./spawnProcessor";
import { spawnConfig } from "./spawnConfig";
import { logger } from "../../lib/logger";

let schedulerHandle: ReturnType<typeof setInterval> | null = null;

/**
 * Runs a single spawn cycle across all active worlds.
 * Errors per-world are caught so one failing world doesn't block others.
 */
async function runAllWorlds(): Promise<void> {
  try {
    const worlds = await worldRepository.findAll();
    const activeWorlds = worlds.filter((w) => w.status === "active");

    for (const world of activeWorlds) {
      try {
        await runSpawnCycle(world.id);
      } catch (err) {
        logger.error({ err, worldId: world.id }, "Spawn cycle error for world");
      }
    }
  } catch (err) {
    logger.error({ err }, "Spawn scheduler: failed to load worlds");
  }
}

/**
 * Starts the spawn scheduler.
 *
 * Call once on server startup (in index.ts) alongside startResourceTick().
 * Calling this more than once is safe — a second call is a no-op.
 */
export function startSpawnScheduler(): void {
  if (schedulerHandle) {
    logger.warn("Spawn scheduler already running — ignoring duplicate start");
    return;
  }

  const intervalMs = spawnConfig.scheduler_interval_minutes * 60 * 1000;

  logger.info(
    { intervalMinutes: spawnConfig.scheduler_interval_minutes },
    "Starting spawn scheduler",
  );

  // Run immediately on startup so the world_spawns table is populated
  // before any frontend request arrives
  void runAllWorlds();

  schedulerHandle = setInterval(() => void runAllWorlds(), intervalMs);
}

/** Stops the scheduler (useful for tests or graceful shutdown) */
export function stopSpawnScheduler(): void {
  if (schedulerHandle) {
    clearInterval(schedulerHandle);
    schedulerHandle = null;
    logger.info("Spawn scheduler stopped");
  }
}
