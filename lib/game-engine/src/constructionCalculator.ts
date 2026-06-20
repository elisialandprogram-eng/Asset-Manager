/**
 * constructionCalculator.ts — Logic for constructing brand-new buildings.
 *
 * A "construction" means placing a building for the first time (level 0 → 1).
 * Costs and durations reuse the existing upgrade formulas at level 0 so that
 * balance stays in one place (gameBalance.ts).
 */

import {
  UPGRADE_BASE_COST,
  UPGRADE_BASE_SECONDS,
  type BuildingType,
  type ResourceType,
  ALL_RESOURCE_TYPES,
  RESOURCE_BUILDING_TYPES,
} from "./gameBalance";
import {
  checkCanConstructBuilding,
  type BuildingCountSnapshot,
} from "./palaceRules";
import { checkAffordability, type UpgradeAffordabilityResult } from "./upgradeCalculator";
import type { ResourceSnapshot } from "./resourceCalculator";

// ─── Construction Cost ─────────────────────────────────────────────────────────

/**
 * Compute the resource cost to construct a brand-new building (level 0 → 1).
 * Uses the base upgrade cost (scale exponent = 0, so cost = base).
 */
export function computeConstructionCost(
  buildingType: BuildingType
): Record<ResourceType, number> {
  const base = UPGRADE_BASE_COST[buildingType];
  const result = {} as Record<ResourceType, number>;
  for (const resource of ALL_RESOURCE_TYPES) {
    result[resource] = base[resource];
  }
  return result;
}

// ─── Construction Duration ─────────────────────────────────────────────────────

/**
 * Compute how many seconds it takes to construct a brand-new building.
 * Uses the base upgrade seconds (scale exponent = 0).
 */
export function computeConstructionDuration(buildingType: BuildingType): number {
  return UPGRADE_BASE_SECONDS[buildingType];
}

// ─── Constructable Types ───────────────────────────────────────────────────────

/**
 * All building types a player can explicitly construct (palace is created on
 * kingdom registration and is excluded from the "build new" flow).
 */
export const CONSTRUCTABLE_BUILDING_TYPES: BuildingType[] = [
  "farm",
  "lumber_mill",
  "quarry",
  "iron_mine",
  "gold_mine",
  "barracks",
  "wall",
  "watch_tower",
];

// ─── Construction Option ───────────────────────────────────────────────────────

export interface ConstructionOption {
  buildingType: BuildingType;
  label: string;
  cost: Record<ResourceType, number>;
  durationSeconds: number;
  /** Whether palace tier + slot limits allow this building to be constructed. */
  slotAvailable: boolean;
  slotReason?: string;
  /** Whether the kingdom currently has enough resources. */
  canAfford: boolean;
  shortfall: Record<ResourceType, number>;
}

export const BUILDING_LABELS: Record<BuildingType, string> = {
  palace: "Palace",
  farm: "Farm",
  lumber_mill: "Lumber Mill",
  quarry: "Quarry",
  iron_mine: "Iron Mine",
  gold_mine: "Gold Mine",
  barracks: "Barracks",
  wall: "Wall",
  watch_tower: "Watch Tower",
};

/**
 * Compute all construction options available for a kingdom given its palace
 * level, existing building counts, and current resources.
 */
export function computeConstructionOptions(
  palaceLevel: number,
  existingCounts: BuildingCountSnapshot[],
  resources: ResourceSnapshot
): ConstructionOption[] {
  return CONSTRUCTABLE_BUILDING_TYPES.map((buildingType) => {
    const slotCheck = checkCanConstructBuilding(buildingType, palaceLevel, existingCounts);
    const cost = computeConstructionCost(buildingType);
    const durationSeconds = computeConstructionDuration(buildingType);
    const affordability = checkAffordability(resources, cost);

    return {
      buildingType,
      label: BUILDING_LABELS[buildingType],
      cost,
      durationSeconds,
      slotAvailable: slotCheck.allowed,
      slotReason: slotCheck.reason,
      canAfford: affordability.canAfford,
      shortfall: affordability.shortfall,
    };
  });
}
