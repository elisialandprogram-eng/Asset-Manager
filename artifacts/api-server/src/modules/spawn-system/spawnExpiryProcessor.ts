/**
 * spawnExpiryProcessor.ts — Handles expiry and cleanup of stale spawn rows.
 *
 * Two responsibilities:
 *   1. Mark active spawns as EXPIRED when their expiresAt has passed
 *   2. Hard-delete rows that have been expired/depleted for more than 1 hour
 *      (keeps the table lean and avoids full-table scans over time)
 */

import { spawnRepository } from "./spawnRepository";

/** How long an expired/depleted row sits before being permanently deleted */
const CLEANUP_AFTER_MS = 60 * 60 * 1000; // 1 hour

export interface ExpiryResult {
  markedExpired: number;
  deleted: number;
}

/**
 * Processes all expired spawns for a world:
 *   - Finds active rows past their expiresAt and marks them EXPIRED
 *   - Deletes rows that have been expired/depleted for over 1 hour
 *
 * @param worldId  The world to process
 * @returns Counts of rows affected for logging
 */
export async function processExpiredSpawns(worldId: number): Promise<ExpiryResult> {
  const now = new Date();
  const cleanupBefore = new Date(now.getTime() - CLEANUP_AFTER_MS);

  // Step 1: Find active spawns whose lifetime has ended
  const expired = await spawnRepository.findExpiredByWorldId(worldId, now);

  let markedExpired = 0;
  if (expired.length > 0) {
    await spawnRepository.markExpiredBatch(expired.map((s) => s.id));
    markedExpired = expired.length;
  }

  // Step 2: Hard-delete stale expired/depleted rows
  const deleted = await spawnRepository.deleteStaleByWorldId(worldId, cleanupBefore);

  return { markedExpired, deleted };
}
