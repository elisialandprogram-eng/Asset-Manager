export const WORLD_SIZE = 10_000;

function hash(seed: number, x: number, y: number): number {
  let h = (seed + x * 374761393 + y * 668265263) | 0;
  h = ((h ^ (h >>> 13)) * 1540483477) | 0;
  h = h ^ (h >>> 15);
  return Math.abs(h) / 0x7fffffff;
}

function lerp(a: number, b: number, t: number): number {
  return a + t * (b - a);
}

function smoothstep(t: number): number {
  return t * t * (3 - 2 * t);
}

function valueNoise(x: number, y: number, seed: number): number {
  const xi = Math.floor(x);
  const yi = Math.floor(y);
  const xf = x - xi;
  const yf = y - yi;
  const u = smoothstep(xf);
  const v = smoothstep(yf);
  const aa = hash(seed, xi, yi);
  const ba = hash(seed, xi + 1, yi);
  const ab = hash(seed, xi, yi + 1);
  const bb = hash(seed, xi + 1, yi + 1);
  return lerp(lerp(aa, ba, u), lerp(ab, bb, u), v);
}

export function fbm(x: number, y: number, seed: number, octaves = 6): number {
  let value = 0;
  let amplitude = 0.5;
  let frequency = 1;
  let max = 0;
  for (let i = 0; i < octaves; i++) {
    value += valueNoise(x * frequency, y * frequency, seed + i * 7919) * amplitude;
    max += amplitude;
    amplitude *= 0.5;
    frequency *= 2;
  }
  return value / max;
}

export type Biome = "plains" | "forest" | "hills" | "mountains" | "peaks" | "snow" | "crystal";

export function getElevation(worldX: number, worldY: number, seed: number): number {
  const nx = worldX / 2000;
  const ny = worldY / 2000;
  return fbm(nx, ny, seed);
}

export function getBiome(worldX: number, worldY: number, seed: number): Biome {
  const elevation = getElevation(worldX, worldY, seed);
  const crystalNoise = fbm(worldX / 1500, worldY / 1500, seed + 99999, 4);
  if (crystalNoise > 0.82 && elevation > 0.3 && elevation < 0.6) return "crystal";
  if (elevation < 0.22) return "plains";
  if (elevation < 0.38) return "forest";
  if (elevation < 0.54) return "hills";
  if (elevation < 0.68) return "mountains";
  if (elevation < 0.82) return "peaks";
  return "snow";
}

export function seededRandom(seed: number, index: number): number {
  let h = ((seed + index * 6364136223) ^ (index >>> 16)) | 0;
  h = ((h ^ (h >>> 13)) * 1540483477) | 0;
  h = h ^ (h >>> 15);
  return Math.abs(h) / 0x7fffffff;
}

export function generateKingdomPosition(
  kingdomId: number,
  worldSeed: number,
  existingPositions: Array<{ x: number; y: number }>,
  minDistance = 400,
): { x: number; y: number } {
  const margin = 800;
  const range = WORLD_SIZE - margin * 2;

  for (let attempt = 0; attempt < 40; attempt++) {
    const x = Math.floor(seededRandom(kingdomId * 997 + attempt, worldSeed) * range + margin);
    const y = Math.floor(seededRandom(kingdomId * 997 + attempt, worldSeed + 1) * range + margin);
    const tooClose = existingPositions.some((p) => {
      const dx = p.x - x;
      const dy = p.y - y;
      return Math.sqrt(dx * dx + dy * dy) < minDistance;
    });
    if (!tooClose) return { x, y };
  }

  const x = Math.floor(seededRandom(kingdomId * 997, worldSeed) * range + margin);
  const y = Math.floor(seededRandom(kingdomId * 997, worldSeed + 1) * range + margin);
  return { x, y };
}
