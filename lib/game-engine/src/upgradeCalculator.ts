import {
  UPGRADE_BASE_COST,
  UPGRADE_COST_SCALE,
  UPGRADE_BASE_SECONDS,
  UPGRADE_TIME_SCALE,
  MAX_BUILDING_LEVEL,
  type BuildingType,
  type ResourceType,
  ALL_RESOURCE_TYPES,
} from "./gameBalance";

import type { ResourceSnapshot } from "./resourceCalculator";

// ─── Upgrade Cost ─────────────────────────────────────────────────────────────

/**
 * Compute the resource cost to upgrade a building from `currentLevel` to `currentLevel + 1`.
 *
 * Formula: floor(baseCost[resource] × UPGRADE_COST_SCALE^(targetLevel − 1))
 */
export function computeUpgradeCost(
  buildingType: BuildingType,
  currentLevel: number
): Record<ResourceType, number> {
  const targetLevel = currentLevel + 1;
  const base = UPGRADE_BASE_COST[buildingType];
  const result = {} as Record<ResourceType, number>;

  for (const resource of ALL_RESOURCE_TYPES) {
    result[resource] = Math.floor(
      base[resource] * Math.pow(UPGRADE_COST_SCALE, targetLevel - 1)
    );
  }

  return result;
}

// ─── Upgrade Duration ─────────────────────────────────────────────────────────

/**
 * Compute how many seconds an upgrade from `currentLevel` to `currentLevel + 1` takes.
 *
 * Formula: floor(baseSeconds × UPGRADE_TIME_SCALE^(targetLevel − 1))
 */
export function computeUpgradeDuration(
  buildingType: BuildingType,
  currentLevel: number
): number {
  const targetLevel = currentLevel + 1;
  return Math.floor(
    UPGRADE_BASE_SECONDS[buildingType] *
      Math.pow(UPGRADE_TIME_SCALE, targetLevel - 1)
  );
}

// ─── Affordability Check ──────────────────────────────────────────────────────

export interface UpgradeAffordabilityResult {
  canAfford: boolean;
  /** Resources missing (0 if already sufficient for that resource). */
  shortfall: Record<ResourceType, number>;
}

/**
 * Check whether a kingdom's current resources cover the upgrade cost.
 */
export function checkAffordability(
  resources: ResourceSnapshot,
  cost: Record<ResourceType, number>
): UpgradeAffordabilityResult {
  const shortfall = {} as Record<ResourceType, number>;
  let canAfford = true;

  for (const resource of ALL_RESOURCE_TYPES) {
    const deficit = cost[resource] - resources[resource];
    shortfall[resource] = Math.max(0, deficit);
    if (deficit > 0) canAfford = false;
  }

  return { canAfford, shortfall };
}

// ─── Validation ───────────────────────────────────────────────────────────────

export interface UpgradeValidation {
  valid: boolean;
  reason?: string;
}

/**
 * Validate whether an upgrade can be queued (ignoring resources — that's a
 * separate check via checkAffordability).
 */
export function validateUpgrade(
  currentLevel: number,
  isConstructing: boolean
): UpgradeValidation {
  if (isConstructing) {
    return { valid: false, reason: "Building is already upgrading" };
  }
  if (currentLevel >= MAX_BUILDING_LEVEL) {
    return {
      valid: false,
      reason: `Building is already at maximum level (${MAX_BUILDING_LEVEL})`,
    };
  }
  return { valid: true };
}

// ─── Power Calculation ────────────────────────────────────────────────────────

/**
 * Compute total kingdom power from all buildings.
 * Power = sum of (building level × power weight per type).
 */
const POWER_WEIGHT: Record<BuildingType, number> = {
  palace: 50,
  farm: 5,
  lumber_mill: 5,
  quarry: 5,
  iron_mine: 8,
  gold_mine: 10,
  barracks: 20,
  wall: 15,
  watch_tower: 12,
};

export interface BuildingLevelSnapshot {
  buildingType: BuildingType;
  level: number;
}

export function computeKingdomPower(
  buildings: BuildingLevelSnapshot[]
): number {
  return buildings.reduce(
    (total, b) => total + b.level * (POWER_WEIGHT[b.buildingType] ?? 1),
    0
  );
}
