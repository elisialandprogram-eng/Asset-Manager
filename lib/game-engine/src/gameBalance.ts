/**
 * gameBalance.ts — Single source of truth for ALL game simulation formulas.
 *
 * Rules:
 * - Every numeric constant that affects gameplay lives here.
 * - Never hard-code production rates, costs, or times inside route handlers.
 * - Change a number here and it propagates to the entire engine.
 */

// ─── Types ────────────────────────────────────────────────────────────────────

export type BuildingType =
  | "palace"
  | "farm"
  | "lumber_mill"
  | "quarry"
  | "iron_mine"
  | "gold_mine"
  | "barracks"
  | "wall"
  | "watch_tower";

export type ResourceType = "food" | "wood" | "stone" | "iron" | "gold";

export const ALL_BUILDING_TYPES: BuildingType[] = [
  "palace",
  "farm",
  "lumber_mill",
  "quarry",
  "iron_mine",
  "gold_mine",
  "barracks",
  "wall",
  "watch_tower",
];

export const ALL_RESOURCE_TYPES: ResourceType[] = [
  "food",
  "wood",
  "stone",
  "iron",
  "gold",
];

// ─── Tick ─────────────────────────────────────────────────────────────────────

/** How often the resource tick fires (milliseconds). */
export const TICK_INTERVAL_MS = 60_000; // 1 minute

/** Maximum building level any building can reach. */
export const MAX_BUILDING_LEVEL = 20;

// ─── Resource Production ──────────────────────────────────────────────────────

/**
 * Per-tick production per level.
 * A Farm at level 3 produces: 3 × 10 = 30 food per tick.
 */
export const PRODUCTION_RATE: Record<
  BuildingType,
  Partial<Record<ResourceType, number>>
> = {
  palace: {},
  farm: { food: 10 },
  lumber_mill: { wood: 10 },
  quarry: { stone: 10 },
  iron_mine: { iron: 8 },
  gold_mine: { gold: 5 },
  barracks: {},
  wall: {},
  watch_tower: {},
};

// ─── Resource Caps ────────────────────────────────────────────────────────────

/** Base storage cap for each resource type (applies to every kingdom). */
export const BASE_RESOURCE_CAP: Record<ResourceType, number> = {
  food: 10_000,
  wood: 10_000,
  stone: 10_000,
  iron: 5_000,
  gold: 2_500,
};

/**
 * Extra cap added per building level.
 * A Farm at level 5 adds 5 × 500 = 2,500 to the food cap.
 */
export const CAP_PER_BUILDING_LEVEL: Partial<
  Record<BuildingType, Partial<Record<ResourceType, number>>>
> = {
  farm: { food: 500 },
  lumber_mill: { wood: 500 },
  quarry: { stone: 500 },
  iron_mine: { iron: 300 },
  gold_mine: { gold: 200 },
};

// ─── Upgrade Costs ────────────────────────────────────────────────────────────

/**
 * Base upgrade cost at level 1 for each building type.
 * Actual cost = floor(baseCost[resource] × UPGRADE_COST_SCALE^(targetLevel - 1))
 */
export const UPGRADE_BASE_COST: Record<
  BuildingType,
  Record<ResourceType, number>
> = {
  palace: { food: 0, wood: 1500, stone: 2000, iron: 1000, gold: 500 },
  farm: { food: 0, wood: 150, stone: 80, iron: 30, gold: 20 },
  lumber_mill: { food: 0, wood: 0, stone: 120, iron: 50, gold: 30 },
  quarry: { food: 0, wood: 80, stone: 0, iron: 60, gold: 40 },
  iron_mine: { food: 0, wood: 100, stone: 150, iron: 0, gold: 50 },
  gold_mine: { food: 0, wood: 120, stone: 200, iron: 100, gold: 0 },
  barracks: { food: 100, wood: 200, stone: 150, iron: 200, gold: 100 },
  wall: { food: 0, wood: 100, stone: 400, iron: 200, gold: 50 },
  watch_tower: { food: 0, wood: 150, stone: 300, iron: 100, gold: 50 },
};

/**
 * Exponential cost multiplier per level.
 * Level 2 costs baseCost × 1.5; level 3 costs baseCost × 1.5², etc.
 */
export const UPGRADE_COST_SCALE = 1.5;

// ─── Upgrade Times ────────────────────────────────────────────────────────────

/**
 * Base upgrade duration in seconds at level 1.
 * Actual duration = floor(baseSeconds × UPGRADE_TIME_SCALE^(targetLevel - 1))
 */
export const UPGRADE_BASE_SECONDS: Record<BuildingType, number> = {
  palace: 7200,      // 2 hours
  farm: 120,         // 2 minutes
  lumber_mill: 120,
  quarry: 180,       // 3 minutes
  iron_mine: 240,    // 4 minutes
  gold_mine: 300,    // 5 minutes
  barracks: 600,     // 10 minutes
  wall: 480,         // 8 minutes
  watch_tower: 420,  // 7 minutes
};

/**
 * Exponential time multiplier per level.
 * Level 2 upgrade takes baseSeconds × 1.3; level 3 takes × 1.3², etc.
 */
export const UPGRADE_TIME_SCALE = 1.3;

// ─── Palace Progression ───────────────────────────────────────────────────────

export interface PalaceTierConfig {
  /** Maximum distinct resource-producing buildings (farm, lumber_mill, quarry, iron_mine, gold_mine). */
  maxResourceBuildings: number;
  /** Maximum barracks buildings allowed. */
  maxBarracks: number;
  /** Maximum wall segments allowed. */
  maxWalls: number;
  /** Maximum watch towers allowed. */
  maxWatchTowers: number;
  /** Feature identifiers unlocked at this tier. */
  unlocksFeatures: string[];
}

/**
 * Palace level → tier configuration.
 * Each 5 palace levels constitute one progression tier.
 *
 * Tier 1  (palace 1–4):  starter kingdom, 2 resource buildings
 * Tier 2  (palace 5–9):  barracks unlocked, 4 resource buildings
 * Tier 3  (palace 10–14): militia available, 6 resource buildings
 * Tier 4  (palace 15–19): spearman + archer, 8 resource buildings
 * Tier 5  (palace 20):   full unlock, 10 resource buildings
 */
export const PALACE_TIERS: Record<number, PalaceTierConfig> = {
  1: {
    maxResourceBuildings: 2,
    maxBarracks: 0,
    maxWalls: 1,
    maxWatchTowers: 0,
    unlocksFeatures: [],
  },
  2: {
    maxResourceBuildings: 4,
    maxBarracks: 1,
    maxWalls: 2,
    maxWatchTowers: 1,
    unlocksFeatures: ["barracks"],
  },
  3: {
    maxResourceBuildings: 6,
    maxBarracks: 2,
    maxWalls: 3,
    maxWatchTowers: 2,
    unlocksFeatures: ["barracks", "militia"],
  },
  4: {
    maxResourceBuildings: 8,
    maxBarracks: 3,
    maxWalls: 4,
    maxWatchTowers: 3,
    unlocksFeatures: ["barracks", "militia", "spearman", "archer"],
  },
  5: {
    maxResourceBuildings: 10,
    maxBarracks: 4,
    maxWalls: 5,
    maxWatchTowers: 4,
    unlocksFeatures: [
      "barracks",
      "militia",
      "spearman",
      "archer",
      "scout",
      "research",
    ],
  },
};

/** RESOURCE_BUILDING_TYPES for palace rule checks. */
export const RESOURCE_BUILDING_TYPES: BuildingType[] = [
  "farm",
  "lumber_mill",
  "quarry",
  "iron_mine",
  "gold_mine",
];
