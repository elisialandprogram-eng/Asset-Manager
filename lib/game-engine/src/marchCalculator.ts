/**
 * marchCalculator.ts — Pure march travel and gather math.
 *
 * Rules:
 * - No DB calls, no side effects. Pure computation only.
 * - Every formula is derived from COMBAT_ENGINE_BIBLE.md.
 * - All speeds are in tiles per minute.
 * - Future modifier slots are already present (research, hero, alliance, terrain, dragoon).
 */

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

/** Base march speed — infantry reference, tiles per minute. */
export const BASE_MARCH_SPEED_TPM = 2.0;

/** Type speed multipliers (relative to infantry baseline). */
export const TROOP_SPEED_MODIFIER: Record<string, number> = {
  militia:      1.0,   // infantry
  spearman:     1.0,   // infantry
  archer:       0.9,   // ranged
  scout:        1.6,   // cavalry-class
  knight:       1.6,   // cavalry
  catapult:     0.5,   // siege
  dragon_rider: 1.4,   // special
};

/** Tier speed multipliers — indexed by tier (1–5). */
export const TIER_SPEED_MODIFIER: Record<number, number> = {
  1: 1.0,
  2: 1.1,
  3: 1.2,
  4: 1.3,
  5: 1.4,
};

/** Gather duration per node level (seconds). Level 1 = 5min, scales up. */
export const GATHER_DURATION_SECONDS: Record<number, number> = {
  1: 300,    // 5 min
  2: 480,    // 8 min
  3: 720,    // 12 min
  4: 960,    // 16 min
  5: 1200,   // 20 min
  6: 1500,   // 25 min
  7: 1800,   // 30 min
  8: 2400,   // 40 min
  9: 3000,   // 50 min
  10: 3600,  // 60 min
};

/** Resource capacity per troop type (how much one troop can carry). */
export const TROOP_CARRY_CAPACITY: Record<string, number> = {
  militia:      20,
  spearman:     20,
  archer:       25,
  scout:        35,
  knight:       40,
  catapult:     60,
  dragon_rider: 80,
};

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface TroopComposition {
  militia?:      number;
  spearman?:     number;
  archer?:       number;
  scout?:        number;
  knight?:       number;
  catapult?:     number;
  dragon_rider?: number;
}

export interface SpeedModifiers {
  /** Fractional research bonus (0.10 = +10%). Default 0. */
  researchBonus?: number;
  /** Hero speed stat out of 10000 (heroSpeedStat / 10000 = fraction). Default 0. */
  heroSpeedStat?: number;
  /** Fractional alliance bonus (0.05 = +5%). Default 0. */
  allianceBonus?: number;
  /** Terrain multiplier (1.0 = normal, 0.8 = forest, etc.). Default 1. */
  terrainModifier?: number;
  /** Dragoon agility bonus fraction. Default 0. */
  dragoonBonus?: number;
}

export interface MarchTimings {
  /** Effective speed in tiles per minute */
  speedTpm: number;
  /** Euclidean distance in tiles */
  distanceTiles: number;
  /** Travel time one way (seconds) */
  travelSeconds: number;
  /** When the march arrives at destination */
  arrivesAt: Date;
  /** Duration of the gather phase (seconds) — null if not a gather march */
  gatherDurationSeconds: number | null;
  /** When gather phase ends (null for non-gather) */
  gatherEndsAt: Date | null;
  /** When the return march arrives home */
  returnsAt: Date;
  /** Total round-trip duration (seconds) */
  totalDurationSeconds: number;
}

// ---------------------------------------------------------------------------
// Core functions
// ---------------------------------------------------------------------------

/**
 * Weighted-average march speed for a mixed troop composition.
 * Applies research / hero / alliance / terrain / dragoon modifiers.
 *
 * Formula from COMBAT_ENGINE_BIBLE.md §2:
 *   effective_speed = sum(count × type_modifier) / total_troops
 *   then multiplied by all additive/multiplicative modifiers.
 */
export function calculateMarchSpeed(
  troops: TroopComposition,
  modifiers: SpeedModifiers = {},
): number {
  const {
    researchBonus    = 0,
    heroSpeedStat    = 0,
    allianceBonus    = 0,
    terrainModifier  = 1.0,
    dragoonBonus     = 0,
  } = modifiers;

  const entries = Object.entries(troops) as [string, number][];
  const totalTroops = entries.reduce((sum, [, count]) => sum + (count ?? 0), 0);

  if (totalTroops === 0) {
    return BASE_MARCH_SPEED_TPM;
  }

  const weightedSum = entries.reduce((sum, [type, count]) => {
    const modifier = TROOP_SPEED_MODIFIER[type] ?? 1.0;
    return sum + (count ?? 0) * modifier;
  }, 0);

  const baseSpeed = BASE_MARCH_SPEED_TPM * (weightedSum / totalTroops);

  return (
    baseSpeed
    * (1 + researchBonus)
    * (1 + heroSpeedStat / 10_000)
    * (1 + allianceBonus)
    * (1 + dragoonBonus)
    * terrainModifier
  );
}

/**
 * Euclidean distance between two tile coordinates.
 *
 * Formula from COMBAT_ENGINE_BIBLE.md §2:
 *   distance = sqrt((x2 - x1)^2 + (z2 - z1)^2)
 */
export function calculateDistance(
  x1: number,
  y1: number,
  x2: number,
  y2: number,
): number {
  return Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);
}

/**
 * One-way travel time in seconds.
 *
 * travelTime = distance / speed  (where speed is tiles/min → convert to tiles/sec)
 */
export function calculateTravelSeconds(
  distanceTiles: number,
  speedTpm: number,
): number {
  if (speedTpm <= 0) throw new Error("March speed must be positive");
  return (distanceTiles / speedTpm) * 60;
}

/**
 * Get gather duration in seconds for a given node level.
 * Falls back to max level duration for levels above the table.
 */
export function getGatherDurationSeconds(nodeLevel: number): number {
  return GATHER_DURATION_SECONDS[nodeLevel] ?? GATHER_DURATION_SECONDS[10]!;
}

/**
 * Calculate full march timing for a gather march.
 *
 * Returns all timestamps and derived values needed to persist the march row.
 */
export function calculateGatherMarchTimings(params: {
  originX: number;
  originY: number;
  destX: number;
  destY: number;
  troops: TroopComposition;
  nodeLevel: number;
  startedAt: Date;
  modifiers?: SpeedModifiers;
}): MarchTimings {
  const { originX, originY, destX, destY, troops, nodeLevel, startedAt, modifiers } = params;

  const speedTpm       = calculateMarchSpeed(troops, modifiers);
  const distanceTiles  = calculateDistance(originX, originY, destX, destY);
  const travelSeconds  = calculateTravelSeconds(distanceTiles, speedTpm);
  const gatherDurationSeconds = getGatherDurationSeconds(nodeLevel);

  const arrivesAt = new Date(startedAt.getTime() + travelSeconds * 1000);
  const gatherEndsAt = new Date(arrivesAt.getTime() + gatherDurationSeconds * 1000);
  const returnsAt = new Date(gatherEndsAt.getTime() + travelSeconds * 1000);

  return {
    speedTpm,
    distanceTiles,
    travelSeconds,
    arrivesAt,
    gatherDurationSeconds,
    gatherEndsAt,
    returnsAt,
    totalDurationSeconds: travelSeconds * 2 + gatherDurationSeconds,
  };
}

/**
 * Total carry capacity of a troop composition.
 * Used to cap resource gathering by load limit.
 */
export function calculateCarryCapacity(troops: TroopComposition): number {
  return Object.entries(troops).reduce((sum, [type, count]) => {
    const cap = TROOP_CARRY_CAPACITY[type] ?? 20;
    return sum + (count ?? 0) * cap;
  }, 0);
}

/**
 * How much of a given resource a node yields, capped by carry capacity.
 *
 * Phase 3: simple model — amount per level, capped by troop load.
 */
export function calculateGatherYield(
  nodeLevel: number,
  troops: TroopComposition,
  nodeRemainingAmount?: number,
): number {
  const YIELD_PER_LEVEL = 500;
  const rawYield = nodeLevel * YIELD_PER_LEVEL;
  const carryMax = calculateCarryCapacity(troops);
  const available = nodeRemainingAmount ?? rawYield;
  return Math.min(rawYield, carryMax, available);
}
