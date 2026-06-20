/**
 * spawnRules.ts — Biome restrictions and placement validation rules.
 *
 * Each spawn subtype maps to allowed biomes and optional elevation ranges.
 * This file is the single place to change which monsters appear where.
 *
 * Biome reference (elevation e, moisture m):
 *   water:    e < 0.20
 *   plains:   e 0.20–0.38, m < 0.58
 *   forest:   e 0.26–0.67, m > 0.40
 *   hills:    e 0.38–0.67
 *   mountains:e 0.54–0.88
 *   peaks:    e > 0.80
 *   crystal:  e 0.38–0.67, crystalNoise > 0.83
 */

import type { Biome, SpawnBiomeRule, OccupiedPosition } from "./spawnTypes";

// ---------------------------------------------------------------------------
// Biome restriction map
// ---------------------------------------------------------------------------

/** Biome rules for monster spawns */
const MONSTER_RULES: Record<string, SpawnBiomeRule> = {
  bandit:    { allowedBiomes: ["plains"],               elevationRange: [0.20, 0.38] },
  dire_wolf: { allowedBiomes: ["forest"],               elevationRange: [0.26, 0.67] },
  ogre:      { allowedBiomes: ["hills", "forest"],      elevationRange: [0.38, 0.67] },
  guardian:  { allowedBiomes: ["mountains", "hills"],   elevationRange: [0.54, 0.80] },
  dragon:    { allowedBiomes: ["mountains", "peaks"],   elevationRange: [0.67, 1.00] },
};

/** Biome rules for resource spawns */
const RESOURCE_RULES: Record<string, SpawnBiomeRule> = {
  farm:    { allowedBiomes: ["plains"],               elevationRange: [0.20, 0.38] },
  lumber:  { allowedBiomes: ["forest"],               elevationRange: [0.26, 0.67] },
  iron:    { allowedBiomes: ["hills"],                elevationRange: [0.38, 0.54] },
  gold:    { allowedBiomes: ["hills", "mountains"],   elevationRange: [0.50, 0.67] },
  crystal: { allowedBiomes: ["crystal", "hills"],     elevationRange: [0.38, 0.67] },
};

/** Minimum distance (world units) between any two spawns */
const MIN_SPAWN_DISTANCE = 250;

/** Minimum distance (world units) between a spawn and a kingdom centre */
const MIN_KINGDOM_DISTANCE = 600;

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/**
 * Returns the biome rule for a given spawn subtype.
 * Falls back to a permissive default if the subtype is unknown.
 */
export function getSpawnRule(subtype: string): SpawnBiomeRule {
  return (
    MONSTER_RULES[subtype] ??
    RESOURCE_RULES[subtype] ??
    { allowedBiomes: ["plains", "forest", "hills"] }
  );
}

/**
 * Returns the primary (preferred) biome for a spawn subtype.
 * Used when picking a candidate position.
 */
export function getPrimaryBiome(subtype: string): Biome {
  const rule = getSpawnRule(subtype);
  return rule.allowedBiomes[0] ?? "plains";
}

/**
 * Returns true if the proposed position is valid:
 *   - Not too close to existing spawns
 *   - Not too close to kingdom positions
 *   - Elevation is within the allowed range for this subtype
 */
export function isValidPlacement(
  posX: number,
  posY: number,
  elevation: number,
  subtype: string,
  existingSpawns: OccupiedPosition[],
  kingdoms: OccupiedPosition[],
): boolean {
  const rule = getSpawnRule(subtype);

  // Check elevation range
  if (rule.elevationRange) {
    const [minE, maxE] = rule.elevationRange;
    if (elevation < minE || elevation > maxE) return false;
  }

  // Reject water
  if (elevation < 0.20) return false;

  // Reject if too close to an existing spawn
  for (const s of existingSpawns) {
    if (distance(posX, posY, s.posX, s.posY) < MIN_SPAWN_DISTANCE) return false;
  }

  // Reject if too close to a kingdom
  for (const k of kingdoms) {
    if (distance(posX, posY, k.posX, k.posY) < MIN_KINGDOM_DISTANCE) return false;
  }

  return true;
}

function distance(x1: number, y1: number, x2: number, y2: number): number {
  const dx = x1 - x2;
  const dy = y1 - y2;
  return Math.sqrt(dx * dx + dy * dy);
}
