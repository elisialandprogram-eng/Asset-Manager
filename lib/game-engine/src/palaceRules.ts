import {
  PALACE_TIERS,
  RESOURCE_BUILDING_TYPES,
  type BuildingType,
  type PalaceTierConfig,
} from "./gameBalance";

// ─── Palace Tier Resolution ────────────────────────────────────────────────────

/**
 * Get the palace tier config that applies at a given palace level.
 * Each tier covers a range of 5 palace levels:
 *   - Tier 1: levels 1–4
 *   - Tier 2: levels 5–9
 *   - Tier 3: levels 10–14
 *   - Tier 4: levels 15–19
 *   - Tier 5: level 20
 */
export function getPalaceTier(palaceLevel: number): PalaceTierConfig {
  if (palaceLevel >= 20) return PALACE_TIERS[5];
  if (palaceLevel >= 15) return PALACE_TIERS[4];
  if (palaceLevel >= 10) return PALACE_TIERS[3];
  if (palaceLevel >= 5)  return PALACE_TIERS[2];
  return PALACE_TIERS[1];
}

// ─── Building Construction Checks ─────────────────────────────────────────────

export interface BuildingCountSnapshot {
  buildingType: BuildingType;
  count: number;
}

export interface ConstructionCheckResult {
  allowed: boolean;
  reason?: string;
}

/**
 * Check whether a new building of `type` can be constructed given the
 * palace level and existing building counts.
 */
export function checkCanConstructBuilding(
  buildingType: BuildingType,
  palaceLevel: number,
  existingCounts: BuildingCountSnapshot[]
): ConstructionCheckResult {
  const tier = getPalaceTier(palaceLevel);
  const counts = Object.fromEntries(existingCounts.map((c) => [c.buildingType, c.count]));

  // Palace itself — only one palace allowed
  if (buildingType === "palace") {
    if ((counts["palace"] ?? 0) > 0) {
      return { allowed: false, reason: "A kingdom can only have one Palace" };
    }
    return { allowed: true };
  }

  // Resource buildings
  if (RESOURCE_BUILDING_TYPES.includes(buildingType)) {
    const currentResourceCount = RESOURCE_BUILDING_TYPES.reduce(
      (sum, t) => sum + (counts[t] ?? 0),
      0
    );
    if (currentResourceCount >= tier.maxResourceBuildings) {
      return {
        allowed: false,
        reason: `Palace level ${palaceLevel} allows at most ${tier.maxResourceBuildings} resource buildings. Upgrade your Palace to unlock more.`,
      };
    }
    return { allowed: true };
  }

  // Barracks
  if (buildingType === "barracks") {
    if (!tier.unlocksFeatures.includes("barracks")) {
      return {
        allowed: false,
        reason: `Barracks requires Palace level 5 or higher.`,
      };
    }
    if ((counts["barracks"] ?? 0) >= tier.maxBarracks) {
      return {
        allowed: false,
        reason: `Palace level ${palaceLevel} allows at most ${tier.maxBarracks} Barracks.`,
      };
    }
    return { allowed: true };
  }

  // Walls
  if (buildingType === "wall") {
    if ((counts["wall"] ?? 0) >= tier.maxWalls) {
      return {
        allowed: false,
        reason: `Palace level ${palaceLevel} allows at most ${tier.maxWalls} Wall segments.`,
      };
    }
    return { allowed: true };
  }

  // Watch Towers
  if (buildingType === "watch_tower") {
    if ((counts["watch_tower"] ?? 0) >= tier.maxWatchTowers) {
      return {
        allowed: false,
        reason: `Palace level ${palaceLevel} allows at most ${tier.maxWatchTowers} Watch Towers.`,
      };
    }
    return { allowed: true };
  }

  return { allowed: true };
}

// ─── Feature Gate ─────────────────────────────────────────────────────────────

/**
 * Check whether a specific gameplay feature is unlocked at the given palace level.
 */
export function isFeatureUnlocked(
  feature: string,
  palaceLevel: number
): boolean {
  return getPalaceTier(palaceLevel).unlocksFeatures.includes(feature);
}
