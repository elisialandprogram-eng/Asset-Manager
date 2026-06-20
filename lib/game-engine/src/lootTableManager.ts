/**
 * lootTableManager.ts — Monster loot tables and reward generation.
 *
 * Source of truth: GAME_DESIGN_BIBLE.md §12 + Phase 4 spec (U4.8)
 *
 * Rules:
 *   - NO mock data. All rewards derived from tables.
 *   - Backend authoritative — no client-side reward calculation.
 *   - Rarity table controls drop probability.
 *   - Hero XP always granted on victory.
 *   - Resources always granted on victory.
 *   - Items are probabilistic by tier.
 *   - Deterministic: seed from (reportId XOR monsterSpawnId) for reproducibility.
 *     Phase 4: uses Math.random() — Phase 5+ will use seeded PRNG.
 */

export interface LootTable {
  tier: string;
  resources: {
    food:    [min: number, max: number];
    wood:    [min: number, max: number];
    stone:   [min: number, max: number];
    iron:    [min: number, max: number];
    gold:    [min: number, max: number];
    crystal: [min: number, max: number];
  };
  heroXp: [min: number, max: number];
  itemDrops: ItemDropChance[];
}

export interface ItemDropChance {
  itemKey: string;
  quantity: [min: number, max: number];
  /** Drop probability 0.0-1.0 */
  chance: number;
}

export interface LootResult {
  food:    number;
  wood:    number;
  stone:   number;
  iron:    number;
  gold:    number;
  crystal: number;
  heroXp:  number;
  items:   Record<string, number>;
}

// ---------------------------------------------------------------------------
// Loot Tables by Monster Tier
// ---------------------------------------------------------------------------

export const LOOT_TABLES: Record<string, LootTable> = {
  common: {
    tier: "common",
    resources: {
      food:    [500,  2000],
      wood:    [400,  1500],
      stone:   [300,  1000],
      iron:    [100,   400],
      gold:    [50,    200],
      crystal: [0,       0],
    },
    heroXp: [20, 60],
    itemDrops: [
      { itemKey: "speedup_universal_1m",   quantity: [1, 2], chance: 0.30 },
      { itemKey: "ap_potion_small",        quantity: [1, 1], chance: 0.10 },
      { itemKey: "hero_xp_small",          quantity: [1, 2], chance: 0.25 },
      { itemKey: "resource_food_10k",      quantity: [1, 1], chance: 0.20 },
    ],
  },

  uncommon: {
    tier: "uncommon",
    resources: {
      food:    [2000,  6000],
      wood:    [1500,  4500],
      stone:   [1000,  3000],
      iron:    [400,   1200],
      gold:    [200,    600],
      crystal: [0,       50],
    },
    heroXp: [80, 200],
    itemDrops: [
      { itemKey: "speedup_universal_1h",   quantity: [1, 1], chance: 0.20 },
      { itemKey: "speedup_universal_1m",   quantity: [2, 4], chance: 0.40 },
      { itemKey: "ap_potion_small",        quantity: [1, 2], chance: 0.20 },
      { itemKey: "hero_xp_small",          quantity: [2, 4], chance: 0.30 },
      { itemKey: "resource_iron_5k",       quantity: [1, 1], chance: 0.15 },
    ],
  },

  rare: {
    tier: "rare",
    resources: {
      food:    [6000, 18000],
      wood:    [4500, 13500],
      stone:   [3000,  9000],
      iron:    [1200,  3600],
      gold:    [600,   1800],
      crystal: [50,     300],
    },
    heroXp: [250, 600],
    itemDrops: [
      { itemKey: "speedup_universal_3h",   quantity: [1, 1], chance: 0.25 },
      { itemKey: "speedup_universal_1h",   quantity: [1, 2], chance: 0.40 },
      { itemKey: "speedup_training_1h",    quantity: [1, 2], chance: 0.30 },
      { itemKey: "ap_potion_medium",       quantity: [1, 1], chance: 0.20 },
      { itemKey: "hero_xp_medium",         quantity: [1, 2], chance: 0.35 },
      { itemKey: "resource_crystal_1k",    quantity: [1, 1], chance: 0.20 },
    ],
  },

  elite: {
    tier: "elite",
    resources: {
      food:    [18000,  60000],
      wood:    [13500,  45000],
      stone:   [9000,   30000],
      iron:    [3600,   12000],
      gold:    [1800,    6000],
      crystal: [300,     1500],
    },
    heroXp: [800, 2000],
    itemDrops: [
      { itemKey: "speedup_universal_8h",   quantity: [1, 1], chance: 0.30 },
      { itemKey: "speedup_universal_3h",   quantity: [1, 3], chance: 0.50 },
      { itemKey: "speedup_research_1h",    quantity: [1, 2], chance: 0.35 },
      { itemKey: "ap_potion_medium",       quantity: [1, 2], chance: 0.30 },
      { itemKey: "hero_xp_large",          quantity: [1, 2], chance: 0.40 },
      { itemKey: "resource_crystal_1k",    quantity: [2, 5], chance: 0.35 },
    ],
  },

  boss: {
    tier: "boss",
    resources: {
      food:    [60000,  200000],
      wood:    [45000,  150000],
      stone:   [30000,  100000],
      iron:    [12000,   40000],
      gold:    [6000,    20000],
      crystal: [1500,     5000],
    },
    heroXp: [3000, 8000],
    itemDrops: [
      { itemKey: "speedup_universal_24h",  quantity: [1, 1], chance: 0.25 },
      { itemKey: "speedup_universal_8h",   quantity: [1, 2], chance: 0.50 },
      { itemKey: "speedup_training_1h",    quantity: [2, 5], chance: 0.60 },
      { itemKey: "ap_potion_full",         quantity: [1, 1], chance: 0.20 },
      { itemKey: "hero_xp_large",          quantity: [2, 5], chance: 0.55 },
      { itemKey: "resource_crystal_1k",    quantity: [5, 15], chance: 0.50 },
    ],
  },

  ancient: {
    tier: "ancient",
    resources: {
      food:    [200000, 800000],
      wood:    [150000, 600000],
      stone:   [100000, 400000],
      iron:    [40000,  160000],
      gold:    [20000,   80000],
      crystal: [5000,    20000],
    },
    heroXp: [15000, 50000],
    itemDrops: [
      { itemKey: "speedup_universal_24h",  quantity: [2, 4], chance: 0.70 },
      { itemKey: "speedup_universal_8h",   quantity: [3, 6], chance: 0.80 },
      { itemKey: "ap_potion_full",         quantity: [1, 2], chance: 0.40 },
      { itemKey: "hero_xp_large",          quantity: [5, 12], chance: 0.80 },
      { itemKey: "resource_crystal_1k",    quantity: [20, 60], chance: 0.90 },
    ],
  },
};

// ---------------------------------------------------------------------------
// Reward generation
// ---------------------------------------------------------------------------

function randInt(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

/**
 * Generate loot for a monster victory.
 *
 * Returns zero rewards on defeat (caller should check attackerWon first).
 */
export function generateLoot(monsterTier: string): LootResult {
  const table = LOOT_TABLES[monsterTier] ?? LOOT_TABLES["common"]!;

  const r = table.resources;
  const items: Record<string, number> = {};

  for (const drop of table.itemDrops) {
    if (Math.random() <= drop.chance) {
      const qty = randInt(drop.quantity[0], drop.quantity[1]);
      items[drop.itemKey] = (items[drop.itemKey] ?? 0) + qty;
    }
  }

  return {
    food:    randInt(r.food[0],    r.food[1]),
    wood:    randInt(r.wood[0],    r.wood[1]),
    stone:   randInt(r.stone[0],   r.stone[1]),
    iron:    randInt(r.iron[0],    r.iron[1]),
    gold:    randInt(r.gold[0],    r.gold[1]),
    crystal: randInt(r.crystal[0], r.crystal[1]),
    heroXp:  randInt(table.heroXp[0], table.heroXp[1]),
    items,
  };
}

/** AP cost for attacking a monster of a given tier. */
export function getMonsterApCost(tier: string): number {
  const costs: Record<string, number> = {
    common:   6,
    uncommon: 12,
    rare:     20,
    elite:    30,
    boss:     40,
    ancient:  60,
  };
  return costs[tier] ?? 6;
}
