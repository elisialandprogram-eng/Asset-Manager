/**
 * spawn-system/index.ts — Public API for the spawn system module.
 *
 * Import from here, not from individual files, to keep coupling minimal.
 */

export { startSpawnScheduler, stopSpawnScheduler } from "./spawnScheduler";
export { runSpawnCycle, resetWorldSpawns, cleanupWorldSpawns } from "./spawnProcessor";
export { spawnRepository } from "./spawnRepository";
export type { CycleResult } from "./spawnProcessor";
