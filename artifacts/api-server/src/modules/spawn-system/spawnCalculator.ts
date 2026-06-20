/**
 * spawnCalculator.ts — Pure computation helpers for spawn generation.
 *
 * This module replicates the fBm terrain noise from lib/game-engine's
 * terrainGenerator so the spawn system can evaluate biomes and elevation
 * server-side without a client/browser dependency.
 *
 * All functions here are pure (no DB calls, no side effects).
 */

import type { Biome, SpawnTypeConfig } from "./spawnTypes";

// ---------------------------------------------------------------------------
// Terrain noise (matches noiseUtils.ts and terrainGenerator.ts exactly)
// ---------------------------------------------------------------------------

function hashNoise(seed: number, x: number, y: number): number {
  let h = ((seed * 1000003 + x * 374761393 + y * 668265263) | 0);
  h = ((h ^ (h >>> 13)) * 1540483477) | 0;
  h = h ^ (h >>> 15);
  return Math.abs(h) / 0x7fffffff;
}

function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

function smoothstep(t: number): number {
  return t * t * (3 - 2 * t);
}

function valueNoise(x: number, y: number, seed: number): number {
  const xi = Math.floor(x);
  const yi = Math.floor(y);
  const u = smoothstep(x - xi);
  const v = smoothstep(y - yi);
  return lerp(
    lerp(hashNoise(seed, xi, yi), hashNoise(seed, xi + 1, yi), u),
    lerp(hashNoise(seed, xi, yi + 1), hashNoise(seed, xi + 1, yi + 1), u),
    v,
  );
}

function fbm(x: number, y: number, seed: number, octaves = 6): number {
  let val = 0, amp = 0.5, freq = 1, max = 0;
  for (let i = 0; i < octaves; i++) {
    val += valueNoise(x * freq, y * freq, seed + i * 7919) * amp;
    max += amp;
    amp *= 0.5;
    freq *= 2;
  }
  return val / max;
}

/**
 * Returns terrain elevation [0, 1] at world coordinates (worldX, worldY).
 * Matches the client-side noiseUtils.getElevation() exactly.
 */
export function getElevationAt(worldX: number, worldY: number, seed: number): number {
  const nx = worldX / 2400;
  const ny = worldY / 2400;
  return fbm(nx, ny, seed, 7) * 0.7 + fbm(nx * 1.2, ny * 1.2, seed + 33333, 4) * 0.3;
}

/**
 * Returns terrain moisture [0, 1] at world coordinates.
 * Matches noiseUtils.getMoisture() exactly.
 */
export function getMoistureAt(worldX: number, worldY: number, seed: number): number {
  const nx = worldX / 2400;
  const ny = worldY / 2400;
  return fbm(nx * 0.9, ny * 0.9, seed + 55555, 5);
}

/**
 * Returns the crystal noise value at a position.
 * High crystal noise (> 0.83) indicates a crystal zone biome.
 */
export function getCrystalNoise(worldX: number, worldY: number, seed: number): number {
  const nx = worldX / 2400;
  const ny = worldY / 2400;
  return fbm(nx * 3, ny * 3, seed + 11111, 4);
}

/**
 * Derives the biome name from elevation, moisture, and crystal noise.
 * Matches the biome logic from terrainGenerator.ts and noiseUtils.ts.
 */
export function getBiomeAt(worldX: number, worldY: number, seed: number): Biome {
  const e = getElevationAt(worldX, worldY, seed);
  const m = getMoistureAt(worldX, worldY, seed);
  const c = getCrystalNoise(worldX, worldY, seed);

  if (e < 0.20) return "water";
  if (e > 0.88) return "peaks";
  if (e > 0.80) return "mountains";
  if (e > 0.67) return "mountains";
  if (e > 0.54) return "hills";

  // Mid-elevation crystal zone
  if (e >= 0.38 && e <= 0.67 && c > 0.83) return "crystal";

  if (e > 0.38) {
    return m > 0.40 ? "forest" : "hills";
  }
  // Lower elevation (plains band)
  return m > 0.58 ? "forest" : "plains";
}

// ---------------------------------------------------------------------------
// Level calculation
// ---------------------------------------------------------------------------

/**
 * Calculates the spawn level for a new spawn.
 *
 * Base level is randomised within the config range.
 * When land development is present (future feature), it scales up.
 */
export function calculateSpawnLevel(
  config: SpawnTypeConfig,
  rng: number,               // value in [0, 1) from seeded RNG
  landDevelopmentLevel?: number,
): number {
  const { min_level, max_level } = config;
  const range = max_level - min_level;
  let level = min_level + Math.floor(rng * (range + 1));

  // Future land development scaling
  // Land level 1-2 → no boost
  // Land level 3-4 → +1
  // Land level 5-6 → +2
  // Land level 7-8 → +3
  // Land level 9-10 → +4 (elite/boss tier)
  if (landDevelopmentLevel && landDevelopmentLevel >= 3) {
    level += Math.floor((landDevelopmentLevel - 1) / 2);
  }

  return Math.min(level, 20);
}

/**
 * Calculates the expiry timestamp for a new spawn based on config.
 */
export function calculateExpiryDate(config: SpawnTypeConfig, now = new Date()): Date {
  const expiryMs = config.expiry_hours * 60 * 60 * 1000;
  return new Date(now.getTime() + expiryMs);
}

// ---------------------------------------------------------------------------
// Position sampling
// ---------------------------------------------------------------------------

/**
 * A simple seeded pseudo-random number generator.
 * Produces a deterministic value in [0, 1) for a given seed + index.
 */
export function seededRng(seed: number, index: number): number {
  return hashNoise(seed, index, index * 7 + 1);
}

/**
 * Generates a candidate world position (posX, posY) using a seeded RNG.
 * Positions are scattered across the full 500–9500 range to avoid edges.
 *
 * @param seed      World seed for reproducibility
 * @param attempt   Attempt index (increment to get a different position)
 * @param subtype   Spawn subtype — used as entropy source
 */
export function candidatePosition(
  seed: number,
  attempt: number,
  subtype: string,
): { posX: number; posY: number } {
  // Mix subtype string into the seed to get spatially different positions
  // for different subtypes in the same world.
  const subtypeSeed = subtype
    .split("")
    .reduce((acc, ch, i) => acc + ch.charCodeAt(0) * (i + 1), seed);

  const posX = Math.floor(seededRng(subtypeSeed, attempt * 2)     * 9000 + 500);
  const posY = Math.floor(seededRng(subtypeSeed, attempt * 2 + 1) * 9000 + 500);
  return { posX, posY };
}
