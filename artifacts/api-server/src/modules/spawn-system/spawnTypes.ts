/**
 * spawnTypes.ts — TypeScript interfaces for the spawn system.
 *
 * These types represent configuration, runtime state, and placement rules.
 * They are intentionally separate from Drizzle ORM types so the spawn
 * system can be tested or adapted without a DB dependency.
 */

// ---------------------------------------------------------------------------
// Configuration (mirrors config/world-spawns.json)
// ---------------------------------------------------------------------------

/** Per-subtype configuration loaded from world-spawns.json */
export interface SpawnTypeConfig {
  enabled: boolean;
  /** Hours before a depleted/expired slot gets a replacement */
  respawn_hours: number;
  /** How many hours an active spawn lives before auto-expiring */
  expiry_hours: number;
  /** Maximum concurrent ACTIVE spawns of this type in a world */
  max_world_spawns: number;
  /** Minimum spawn level (1–20) */
  min_level: number;
  /** Maximum spawn level (1–20) */
  max_level: number;
}

/** Full world-spawns.json structure */
export interface SpawnConfig {
  /** How often (minutes) the spawn scheduler runs */
  scheduler_interval_minutes: number;
  /** UTC hour for daily resets (0 = midnight) */
  daily_reset_hour_utc: number;
  resource_spawns: Record<string, SpawnTypeConfig>;
  monster_spawns: Record<string, SpawnTypeConfig>;
}

// ---------------------------------------------------------------------------
// Biome and placement rules
// ---------------------------------------------------------------------------

/** Terrain biome names used in the world_spawns table */
export type Biome =
  | "plains"
  | "forest"
  | "hills"
  | "mountains"
  | "peaks"
  | "crystal"
  | "water";

/** A rule that restricts a spawn subtype to specific biomes */
export interface SpawnBiomeRule {
  /** Which biomes are valid for this spawn subtype */
  allowedBiomes: Biome[];
  /**
   * Additional elevation range [min, max] (0–1 scale).
   * Applied on top of the biome check for finer control.
   */
  elevationRange?: [number, number];
}

// ---------------------------------------------------------------------------
// Runtime generation types
// ---------------------------------------------------------------------------

/** An existing spawn or kingdom occupying a world position */
export interface OccupiedPosition {
  posX: number;
  posY: number;
}

/**
 * The data needed to insert a new spawn row.
 * Matches InsertWorldSpawn from the DB schema (minus auto-generated fields).
 */
export interface NewSpawnRow {
  worldId: number;
  spawnType: "resource" | "monster";
  spawnSubtype: string;
  level: number;
  tileX: number;
  tileY: number;
  posX: number;
  posY: number;
  biome: Biome;
  status: "active";
  spawnedAt: Date;
  expiresAt: Date;
  depletedAt: null;
  landId: null;
  landDevelopmentLevel: null;
}
