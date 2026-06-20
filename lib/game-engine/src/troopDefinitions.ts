/**
 * troopDefinitions.ts — Complete T1-T5 troop stat tables.
 *
 * Source of truth: COMBAT_ENGINE_BIBLE.md §7
 *
 * Classes: infantry, cavalry, archer, siege
 * Tiers:   1-5
 *
 * Key format: "{class}_t{tier}"  (e.g. "infantry_t1", "cavalry_t3")
 *
 * Rules:
 *   - No DB calls. Pure data.
 *   - All buffs (research, hero, alliance) applied as multipliers at runtime.
 *   - RPS triangle: Infantry > Cavalry, Cavalry > Archer, Archer > Infantry, Siege neutral.
 *   - AP cost per monster attack scales by monster tier, not troop tier.
 */

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export type TroopClass = "infantry" | "cavalry" | "archer" | "siege";
export type TroopTier = 1 | 2 | 3 | 4 | 5;
export type TroopKey =
  | "infantry_t1" | "infantry_t2" | "infantry_t3" | "infantry_t4" | "infantry_t5"
  | "cavalry_t1"  | "cavalry_t2"  | "cavalry_t3"  | "cavalry_t4"  | "cavalry_t5"
  | "archer_t1"   | "archer_t2"   | "archer_t3"   | "archer_t4"   | "archer_t5"
  | "siege_t1"    | "siege_t2"    | "siege_t3"    | "siege_t4"    | "siege_t5";

export interface TroopDefinition {
  key: TroopKey;
  class: TroopClass;
  tier: TroopTier;
  name: string;
  baseAttack: number;
  baseDefense: number;
  baseHp: number;
  baseSpeed: number;
  loadCapacity: number;
  trainingTimeSec: number;
  trainingCost: {
    food: number;
    wood: number;
    stone: number;
    iron: number;
    gold: number;
  };
}

// ---------------------------------------------------------------------------
// Base stats — indexed from COMBAT_ENGINE_BIBLE.md §7
// ---------------------------------------------------------------------------

export const TROOP_DEFINITIONS: Record<TroopKey, TroopDefinition> = {
  // ── Infantry ──────────────────────────────────────────────────────────────
  infantry_t1: {
    key: "infantry_t1", class: "infantry", tier: 1, name: "Militia",
    baseAttack: 150, baseDefense: 200, baseHp: 600, baseSpeed: 2.0, loadCapacity: 4,
    trainingTimeSec: 30,
    trainingCost: { food: 50, wood: 30, stone: 20, iron: 10, gold: 0 },
  },
  infantry_t2: {
    key: "infantry_t2", class: "infantry", tier: 2, name: "Footman",
    baseAttack: 230, baseDefense: 310, baseHp: 950, baseSpeed: 2.1, loadCapacity: 4,
    trainingTimeSec: 90,
    trainingCost: { food: 120, wood: 80, stone: 50, iron: 30, gold: 5 },
  },
  infantry_t3: {
    key: "infantry_t3", class: "infantry", tier: 3, name: "Spearman",
    baseAttack: 360, baseDefense: 480, baseHp: 1500, baseSpeed: 2.2, loadCapacity: 4,
    trainingTimeSec: 240,
    trainingCost: { food: 300, wood: 200, stone: 130, iron: 80, gold: 20 },
  },
  infantry_t4: {
    key: "infantry_t4", class: "infantry", tier: 4, name: "Halberdier",
    baseAttack: 560, baseDefense: 740, baseHp: 2400, baseSpeed: 2.3, loadCapacity: 4,
    trainingTimeSec: 600,
    trainingCost: { food: 750, wood: 500, stone: 320, iron: 200, gold: 60 },
  },
  infantry_t5: {
    key: "infantry_t5", class: "infantry", tier: 5, name: "Imperial Guard",
    baseAttack: 880, baseDefense: 1160, baseHp: 3800, baseSpeed: 2.4, loadCapacity: 4,
    trainingTimeSec: 1800,
    trainingCost: { food: 2000, wood: 1400, stone: 900, iron: 600, gold: 200 },
  },

  // ── Cavalry ───────────────────────────────────────────────────────────────
  cavalry_t1: {
    key: "cavalry_t1", class: "cavalry", tier: 1, name: "Scout Rider",
    baseAttack: 200, baseDefense: 180, baseHp: 500, baseSpeed: 3.2, loadCapacity: 10,
    trainingTimeSec: 60,
    trainingCost: { food: 80, wood: 40, stone: 20, iron: 20, gold: 5 },
  },
  cavalry_t2: {
    key: "cavalry_t2", class: "cavalry", tier: 2, name: "Horseman",
    baseAttack: 310, baseDefense: 280, baseHp: 790, baseSpeed: 3.4, loadCapacity: 10,
    trainingTimeSec: 180,
    trainingCost: { food: 200, wood: 100, stone: 50, iron: 60, gold: 15 },
  },
  cavalry_t3: {
    key: "cavalry_t3", class: "cavalry", tier: 3, name: "Knight",
    baseAttack: 480, baseDefense: 430, baseHp: 1250, baseSpeed: 3.6, loadCapacity: 10,
    trainingTimeSec: 480,
    trainingCost: { food: 500, wood: 250, stone: 130, iron: 150, gold: 50 },
  },
  cavalry_t4: {
    key: "cavalry_t4", class: "cavalry", tier: 4, name: "Heavy Knight",
    baseAttack: 740, baseDefense: 670, baseHp: 2000, baseSpeed: 3.8, loadCapacity: 10,
    trainingTimeSec: 1200,
    trainingCost: { food: 1200, wood: 600, stone: 320, iron: 380, gold: 120 },
  },
  cavalry_t5: {
    key: "cavalry_t5", class: "cavalry", tier: 5, name: "Dragoon",
    baseAttack: 1160, baseDefense: 1050, baseHp: 3200, baseSpeed: 4.0, loadCapacity: 10,
    trainingTimeSec: 3600,
    trainingCost: { food: 3000, wood: 1500, stone: 800, iron: 1000, gold: 350 },
  },

  // ── Archer ────────────────────────────────────────────────────────────────
  archer_t1: {
    key: "archer_t1", class: "archer", tier: 1, name: "Bowman",
    baseAttack: 180, baseDefense: 120, baseHp: 400, baseSpeed: 1.8, loadCapacity: 6,
    trainingTimeSec: 45,
    trainingCost: { food: 60, wood: 60, stone: 10, iron: 15, gold: 0 },
  },
  archer_t2: {
    key: "archer_t2", class: "archer", tier: 2, name: "Longbowman",
    baseAttack: 270, baseDefense: 190, baseHp: 620, baseSpeed: 1.9, loadCapacity: 6,
    trainingTimeSec: 120,
    trainingCost: { food: 150, wood: 150, stone: 25, iron: 40, gold: 10 },
  },
  archer_t3: {
    key: "archer_t3", class: "archer", tier: 3, name: "Crossbowman",
    baseAttack: 420, baseDefense: 290, baseHp: 980, baseSpeed: 2.0, loadCapacity: 6,
    trainingTimeSec: 360,
    trainingCost: { food: 380, wood: 380, stone: 60, iron: 100, gold: 30 },
  },
  archer_t4: {
    key: "archer_t4", class: "archer", tier: 4, name: "Marksman",
    baseAttack: 650, baseDefense: 450, baseHp: 1560, baseSpeed: 2.1, loadCapacity: 6,
    trainingTimeSec: 900,
    trainingCost: { food: 950, wood: 950, stone: 150, iron: 250, gold: 80 },
  },
  archer_t5: {
    key: "archer_t5", class: "archer", tier: 5, name: "Elven Ranger",
    baseAttack: 1020, baseDefense: 710, baseHp: 2500, baseSpeed: 2.2, loadCapacity: 6,
    trainingTimeSec: 2700,
    trainingCost: { food: 2500, wood: 2500, stone: 400, iron: 700, gold: 280 },
  },

  // ── Siege ─────────────────────────────────────────────────────────────────
  siege_t1: {
    key: "siege_t1", class: "siege", tier: 1, name: "Ballista",
    baseAttack: 500, baseDefense: 100, baseHp: 1200, baseSpeed: 1.0, loadCapacity: 2,
    trainingTimeSec: 300,
    trainingCost: { food: 100, wood: 300, stone: 200, iron: 150, gold: 20 },
  },
  siege_t2: {
    key: "siege_t2", class: "siege", tier: 2, name: "Catapult",
    baseAttack: 800, baseDefense: 160, baseHp: 1900, baseSpeed: 1.0, loadCapacity: 2,
    trainingTimeSec: 720,
    trainingCost: { food: 250, wood: 750, stone: 500, iron: 380, gold: 60 },
  },
  siege_t3: {
    key: "siege_t3", class: "siege", tier: 3, name: "Trebuchet",
    baseAttack: 1300, baseDefense: 250, baseHp: 3000, baseSpeed: 1.0, loadCapacity: 2,
    trainingTimeSec: 1800,
    trainingCost: { food: 600, wood: 1800, stone: 1200, iron: 950, gold: 180 },
  },
  siege_t4: {
    key: "siege_t4", class: "siege", tier: 4, name: "Bombard",
    baseAttack: 2100, baseDefense: 400, baseHp: 4800, baseSpeed: 1.0, loadCapacity: 2,
    trainingTimeSec: 4500,
    trainingCost: { food: 1500, wood: 4500, stone: 3000, iron: 2400, gold: 500 },
  },
  siege_t5: {
    key: "siege_t5", class: "siege", tier: 5, name: "Dragon Cannon",
    baseAttack: 3400, baseDefense: 640, baseHp: 7800, baseSpeed: 1.0, loadCapacity: 2,
    trainingTimeSec: 12000,
    trainingCost: { food: 4000, wood: 12000, stone: 8000, iron: 6500, gold: 1500 },
  },
};

export const ALL_TROOP_KEYS = Object.keys(TROOP_DEFINITIONS) as TroopKey[];

// ---------------------------------------------------------------------------
// RPS Triangle — Phase 4 spec
// Infantry > Cavalry (1.4x), Cavalry > Archer (1.4x), Archer > Infantry (1.4x)
// Siege: neutral vs troops (1.0x)
// ---------------------------------------------------------------------------

export const RPS_MODIFIER: Record<TroopClass, Record<TroopClass, number>> = {
  infantry: { infantry: 1.0, cavalry: 1.4, archer: 0.7, siege: 1.0 },
  cavalry:  { infantry: 0.7, cavalry: 1.0, archer: 1.4, siege: 1.0 },
  archer:   { infantry: 1.4, cavalry: 0.7, archer: 1.0, siege: 1.0 },
  siege:    { infantry: 1.0, cavalry: 1.0, archer: 1.0, siege: 1.0 },
};

/** Get RPS modifier for attacker class vs defender class. */
export function getRpsModifier(
  attackerClass: TroopClass,
  defenderClass: TroopClass,
): number {
  return RPS_MODIFIER[attackerClass]?.[defenderClass] ?? 1.0;
}

/** Parse a troop key into class + tier. */
export function parseTroopKey(key: string): { class: TroopClass; tier: TroopTier } | null {
  const match = /^(infantry|cavalry|archer|siege)_t([1-5])$/.exec(key);
  if (!match || !match[1] || !match[2]) return null;
  return {
    class: match[1] as TroopClass,
    tier: Number(match[2]) as TroopTier,
  };
}

/** Total attack power for a troop composition (before buffs). */
export function calculateTotalAttack(
  troops: Record<string, number>,
  researchBonus = 0,
  heroAttackStat = 0,
): number {
  let total = 0;
  for (const [key, count] of Object.entries(troops)) {
    const def = TROOP_DEFINITIONS[key as TroopKey];
    if (!def || count <= 0) continue;
    const unitAtk =
      def.baseAttack *
      (1 + researchBonus) *
      (1 + heroAttackStat / 10_000);
    total += unitAtk * count;
  }
  return total;
}

/** Total HP for a troop composition. */
export function calculateTotalHp(troops: Record<string, number>): number {
  let total = 0;
  for (const [key, count] of Object.entries(troops)) {
    const def = TROOP_DEFINITIONS[key as TroopKey];
    if (!def || count <= 0) continue;
    total += def.baseHp * count;
  }
  return total;
}

/** Total defense for a troop composition. */
export function calculateTotalDefense(
  troops: Record<string, number>,
  researchBonus = 0,
): number {
  let total = 0;
  for (const [key, count] of Object.entries(troops)) {
    const def = TROOP_DEFINITIONS[key as TroopKey];
    if (!def || count <= 0) continue;
    total += def.baseDefense * (1 + researchBonus) * count;
  }
  return total;
}

/** Dominant troop class (by count). Used for RPS calculation. */
export function getDominantClass(troops: Record<string, number>): TroopClass {
  const classCounts: Record<TroopClass, number> = {
    infantry: 0, cavalry: 0, archer: 0, siege: 0,
  };
  for (const [key, count] of Object.entries(troops)) {
    const parsed = parseTroopKey(key);
    if (parsed) classCounts[parsed.class] += count;
  }
  let dominant: TroopClass = "infantry";
  let max = 0;
  for (const [cls, cnt] of Object.entries(classCounts)) {
    if (cnt > max) { max = cnt; dominant = cls as TroopClass; }
  }
  return dominant;
}
