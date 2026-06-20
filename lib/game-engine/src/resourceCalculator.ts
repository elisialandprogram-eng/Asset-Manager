import {
  PRODUCTION_RATE,
  BASE_RESOURCE_CAP,
  CAP_PER_BUILDING_LEVEL,
  type BuildingType,
  type ResourceType,
  ALL_RESOURCE_TYPES,
} from "./gameBalance";

// ─── Minimal types (no @workspace/db dependency) ─────────────────────────────

export interface BuildingSnapshot {
  buildingType: BuildingType;
  level: number;
  isConstructing: boolean;
}

export interface ResourceSnapshot {
  food: number;
  wood: number;
  stone: number;
  iron: number;
  gold: number;
}

export interface ProductionRates extends ResourceSnapshot {}
export interface ResourceCaps extends ResourceSnapshot {}

// ─── Production ───────────────────────────────────────────────────────────────

/**
 * Compute total per-tick resource production for a kingdom's buildings.
 * Buildings currently under construction contribute nothing.
 */
export function computeProductionRates(
  buildings: BuildingSnapshot[]
): ProductionRates {
  const rates: ResourceSnapshot = {
    food: 0,
    wood: 0,
    stone: 0,
    iron: 0,
    gold: 0,
  };

  for (const b of buildings) {
    if (b.isConstructing) continue;
    const typeRates = PRODUCTION_RATE[b.buildingType];
    for (const resource of ALL_RESOURCE_TYPES) {
      const baseRate = typeRates[resource] ?? 0;
      rates[resource] += baseRate * b.level;
    }
  }

  return rates;
}

// ─── Caps ─────────────────────────────────────────────────────────────────────

/**
 * Compute per-resource storage caps for a kingdom.
 * Starts from BASE_RESOURCE_CAP and adds bonuses from building levels.
 */
export function computeResourceCaps(
  buildings: BuildingSnapshot[]
): ResourceCaps {
  const caps: ResourceSnapshot = { ...BASE_RESOURCE_CAP };

  for (const b of buildings) {
    if (b.isConstructing) continue;
    const capBonuses = CAP_PER_BUILDING_LEVEL[b.buildingType];
    if (!capBonuses) continue;
    for (const resource of ALL_RESOURCE_TYPES) {
      const bonus = capBonuses[resource] ?? 0;
      caps[resource] += bonus * b.level;
    }
  }

  return caps;
}

// ─── Tick Application ─────────────────────────────────────────────────────────

/**
 * Apply one tick of resource production to the current resource totals.
 * Resources are capped at their computed maximums.
 *
 * Returns the new resource values (does NOT mutate input).
 */
export function applyTick(
  current: ResourceSnapshot,
  buildings: BuildingSnapshot[]
): ResourceSnapshot {
  const rates = computeProductionRates(buildings);
  const caps = computeResourceCaps(buildings);

  const next: ResourceSnapshot = { ...current };
  for (const resource of ALL_RESOURCE_TYPES) {
    next[resource] = Math.min(current[resource] + rates[resource], caps[resource]);
  }
  return next;
}

/**
 * Compute estimated resources at a future time, given production rates and caps.
 * Used to display real-time resource values on the client without polling every second.
 *
 * @param current    Resources at the reference timestamp
 * @param rates      Per-tick production rates
 * @param caps       Storage caps
 * @param elapsedMs  Milliseconds since the reference timestamp
 * @param tickMs     Tick interval in milliseconds (default 60_000)
 */
export function projectResources(
  current: ResourceSnapshot,
  rates: ProductionRates,
  caps: ResourceCaps,
  elapsedMs: number,
  tickMs = 60_000
): ResourceSnapshot {
  const ticksElapsed = elapsedMs / tickMs;
  const result: ResourceSnapshot = { ...current };
  for (const resource of ALL_RESOURCE_TYPES) {
    result[resource] = Math.min(
      current[resource] + rates[resource] * ticksElapsed,
      caps[resource]
    );
  }
  return result;
}
