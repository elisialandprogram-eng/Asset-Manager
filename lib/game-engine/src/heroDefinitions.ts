/**
 * heroDefinitions.ts — Hero rarity configs and starter hero templates.
 *
 * Source of truth: GAME_DESIGN_BIBLE.md §18
 *
 * Heroes are unique entities (future ERC721). Stats scale with rarity + level.
 * Each march supports 0 or 1 hero.
 */

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export type HeroRarity = "common" | "uncommon" | "rare" | "epic" | "legendary";

export interface HeroRarityConfig {
  rarity: HeroRarity;
  leadershipBase: number;
  leadershipPerLevel: number;
  statMultiplier: number;
  maxLevel: number;
  xpToNextLevel: (level: number) => number;
  skillSlots: number;
}

export interface StarterHeroTemplate {
  assetId: string;
  name: string;
  rarity: HeroRarity;
  troopAffinity: "infantry" | "cavalry" | "archer" | "siege" | null;
  baseStats: {
    command: number;
    attack: number;
    defense: number;
    speed: number;
    gathering: number;
  };
  starterSkills: StarterSkill[];
}

export interface StarterSkill {
  skillId: string;
  name: string;
  description: string;
  triggerRound: number | undefined;
  effectType: string;
  effectValue: number;
}

// ---------------------------------------------------------------------------
// Rarity Configuration
// ---------------------------------------------------------------------------

export const HERO_RARITY_CONFIG: Record<HeroRarity, HeroRarityConfig> = {
  common: {
    rarity: "common",
    leadershipBase: 5_000,
    leadershipPerLevel: 100,
    statMultiplier: 1.0,
    maxLevel: 30,
    xpToNextLevel: (lvl) => lvl * 100,
    skillSlots: 2,
  },
  uncommon: {
    rarity: "uncommon",
    leadershipBase: 7_000,
    leadershipPerLevel: 150,
    statMultiplier: 1.15,
    maxLevel: 40,
    xpToNextLevel: (lvl) => lvl * 120,
    skillSlots: 3,
  },
  rare: {
    rarity: "rare",
    leadershipBase: 10_000,
    leadershipPerLevel: 200,
    statMultiplier: 1.35,
    maxLevel: 50,
    xpToNextLevel: (lvl) => lvl * 150,
    skillSlots: 4,
  },
  epic: {
    rarity: "epic",
    leadershipBase: 15_000,
    leadershipPerLevel: 300,
    statMultiplier: 1.60,
    maxLevel: 60,
    xpToNextLevel: (lvl) => lvl * 200,
    skillSlots: 5,
  },
  legendary: {
    rarity: "legendary",
    leadershipBase: 25_000,
    leadershipPerLevel: 500,
    statMultiplier: 2.0,
    maxLevel: 80,
    xpToNextLevel: (lvl) => lvl * 300,
    skillSlots: 6,
  },
};

// ---------------------------------------------------------------------------
// AP Costs by Monster Tier
// ---------------------------------------------------------------------------

/** Maps monster tier name to AP cost. */
export const MONSTER_AP_COST: Record<string, number> = {
  common:   6,
  uncommon: 12,
  rare:     20,
  elite:    30,
  boss:     40,
  ancient:  60,
};

// ---------------------------------------------------------------------------
// Starter Heroes — seeded into new kingdoms at creation
// ---------------------------------------------------------------------------

export const STARTER_HEROES: StarterHeroTemplate[] = [
  {
    assetId: "hero_commander_001",
    name: "Commander Aldric",
    rarity: "common",
    troopAffinity: "infantry",
    baseStats: { command: 100, attack: 80, defense: 60, speed: 50, gathering: 40 },
    starterSkills: [
      {
        skillId: "skill_shield_wall",
        name: "Shield Wall",
        description: "Increases infantry defense by 10% in round 1.",
        triggerRound: 1,
        effectType: "infantry_defense_pct",
        effectValue: 10,
      },
    ],
  },
  {
    assetId: "hero_ranger_001",
    name: "Ranger Sylva",
    rarity: "common",
    troopAffinity: "archer",
    baseStats: { command: 80, attack: 100, defense: 40, speed: 70, gathering: 60 },
    starterSkills: [
      {
        skillId: "skill_volley",
        name: "Volley",
        description: "Archers deal +15% damage in round 2.",
        triggerRound: 2,
        effectType: "archer_attack_pct",
        effectValue: 15,
      },
    ],
  },
];

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Calculate hero leadership capacity at a given level. */
export function calculateLeadership(rarity: HeroRarity, level: number): number {
  const cfg = HERO_RARITY_CONFIG[rarity];
  return cfg.leadershipBase + cfg.leadershipPerLevel * (level - 1);
}

/** XP required to reach next level. */
export function xpToNextLevel(rarity: HeroRarity, level: number): number {
  return HERO_RARITY_CONFIG[rarity].xpToNextLevel(level);
}

/** Hero attack stat contribution (out of 10000 scale, used in buff formula). */
export function heroAttackBuff(heroAttackStat: number): number {
  return heroAttackStat / 10_000;
}
