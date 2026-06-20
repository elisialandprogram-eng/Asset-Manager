/**
 * spawnGenerator.ts — Generates new spawn rows to fill world population gaps.
 *
 * Algorithm per enabled subtype:
 *   1. Count active spawns in the DB
 *   2. Calculate deficit = max_world_spawns - activeCount
 *   3. For each deficit slot, try up to MAX_ATTEMPTS candidate positions
 *   4. Accept positions that pass biome + placement validation
 *   5. Return array of NewSpawnRow objects ready for DB insertion
 *
 * No DB writes happen here — the caller (spawnProcessor.ts) handles writes.
 */

import { spawnRepository } from "./spawnRepository";
import { getAllEnabledSubtypes } from "./spawnConfig";
import { getBiomeAt, getElevationAt, calculateSpawnLevel, calculateExpiryDate, candidatePosition, seededRng } from "./spawnCalculator";
import { isValidPlacement } from "./spawnRules";
import type { NewSpawnRow, OccupiedPosition } from "./spawnTypes";

/** Maximum position attempts per spawn before skipping */
const MAX_ATTEMPTS = 40;

/**
 * Generates all missing spawns for a world.
 *
 * @param worldId    The world to populate
 * @param worldSeed  Deterministic seed for terrain + position generation
 * @param kingdoms   Placed kingdom positions to avoid overlapping
 * @returns Array of new spawn rows ready for bulk insert
 */
export async function generateMissingSpawns(
  worldId: number,
  worldSeed: number,
  kingdoms: OccupiedPosition[],
): Promise<NewSpawnRow[]> {
  const enabledTypes = getAllEnabledSubtypes();
  const now = new Date();

  // Snapshot of current active spawn positions (for collision avoidance)
  const existingActive = await spawnRepository.findActiveByWorldId(worldId);
  const existingPositions: OccupiedPosition[] = existingActive.map((s) => ({
    posX: s.posX,
    posY: s.posY,
  }));

  // Count active spawns per subtype
  const activeCounts = await spawnRepository.countActiveBySubtype(worldId);

  const toInsert: NewSpawnRow[] = [];

  // Use a time-based seed component to ensure different positions each cycle
  const timeSeed = Math.floor(now.getTime() / 60_000); // changes every minute

  for (const { spawnType, subtype, config } of enabledTypes) {
    if (!config.enabled) continue;

    const activeCount = activeCounts[subtype] ?? 0;
    const deficit = config.max_world_spawns - activeCount;
    if (deficit <= 0) continue;

    let generated = 0;

    for (let i = 0; i < deficit; i++) {
      let placed = false;

      for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
        // Unique attempt index: mixes world, subtype, time, deficit slot, and attempt
        const attemptIdx = (timeSeed * 1000 + i * MAX_ATTEMPTS + attempt) % 999_999;

        const { posX, posY } = candidatePosition(worldSeed + timeSeed, attemptIdx, subtype);
        const elevation = getElevationAt(posX, posY, worldSeed);
        const biome = getBiomeAt(posX, posY, worldSeed);

        // Combine existing DB positions with newly staged positions
        const allOccupied = [
          ...existingPositions,
          ...toInsert.map((r) => ({ posX: r.posX, posY: r.posY })),
        ];

        if (!isValidPlacement(posX, posY, elevation, subtype, allOccupied, kingdoms)) {
          continue;
        }

        const levelRng = seededRng(worldSeed + timeSeed, i * 10 + attempt);
        const level = calculateSpawnLevel(config, levelRng);
        const expiresAt = calculateExpiryDate(config, now);

        toInsert.push({
          worldId,
          spawnType,
          spawnSubtype: subtype,
          level,
          tileX: Math.floor(posX / 100),
          tileY: Math.floor(posY / 100),
          posX,
          posY,
          biome,
          status: "active",
          spawnedAt: now,
          expiresAt,
          depletedAt: null,
          landId: null,
          landDevelopmentLevel: null,
        });

        placed = true;
        generated++;
        break;
      }

      if (!placed) {
        // Couldn't find a valid position for this slot — skip it this cycle
      }
    }
  }

  return toInsert;
}
