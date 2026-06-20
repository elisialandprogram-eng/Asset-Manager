import {
  pgTable,
  serial,
  integer,
  text,
  timestamp,
  pgEnum,
  index,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

// --- Enums ---

/** Whether this spawn is a resource node or a monster camp */
export const spawnTypeEnum = pgEnum("spawn_type", ["resource", "monster"]);

/** Lifecycle state of every spawn row */
export const spawnStatusEnum = pgEnum("spawn_status", [
  "active",    // visible and interactable on the world map
  "expired",   // lifetime ran out — invisible; will be deleted by cleanup
  "depleted",  // gathered/defeated by a player — invisible; will be deleted by cleanup
]);

// --- Table ---

/**
 * world_spawns — Unified dynamic spawn table.
 *
 * Replaces the static monster_spawns + crystal_nodes approach with a
 * server-driven lifecycle:  SPAWNED → ACTIVE → (EXPIRED | DEPLETED) → REMOVED → RESPAWNED
 *
 * Future NFT/land integration: land_id and land_development_level drive
 * spawn level scaling when Land NFTs ship (Phase 10).
 */
export const worldSpawnsTable = pgTable(
  "world_spawns",
  {
    id:                   serial("id").primaryKey(),

    /** Which world this spawn belongs to */
    worldId:              integer("world_id").notNull(),

    /** Top-level category: resource node or monster camp */
    spawnType:            spawnTypeEnum("spawn_type").notNull(),

    /**
     * Specific variety within the category.
     * Resources: farm | lumber | iron | gold | crystal
     * Monsters:  bandit | dire_wolf | ogre | guardian | dragon
     */
    spawnSubtype:         text("spawn_subtype").notNull(),

    /** Gameplay power level 1–20, scales with world progression */
    level:                integer("level").notNull().default(1),

    /** World-space tile coordinates (0–9999) — used for spatial queries */
    tileX:                integer("tile_x").notNull(),
    tileY:                integer("tile_y").notNull(),

    /** World-space sub-tile position (0–10000) — used for rendering */
    posX:                 integer("pos_x").notNull(),
    posY:                 integer("pos_y").notNull(),

    /**
     * Terrain biome at spawn location.
     * Values: plains | forest | hills | mountains | peaks | water | crystal
     */
    biome:                text("biome").notNull(),

    /** Current lifecycle state */
    status:               spawnStatusEnum("status").notNull().default("active"),

    /** When this spawn was created / entered ACTIVE state */
    spawnedAt:            timestamp("spawned_at").notNull().defaultNow(),

    /** When this spawn automatically transitions to EXPIRED */
    expiresAt:            timestamp("expires_at").notNull(),

    /** When this spawn was depleted (gathered or defeated) */
    depletedAt:           timestamp("depleted_at"),

    // --- Future Land NFT fields (nullable until Phase 10) ---

    /** ID of the Land NFT tile occupying this location */
    landId:               integer("land_id"),

    /** Development level of the land tile (drives spawn level scaling) */
    landDevelopmentLevel: integer("land_development_level"),

    createdAt:            timestamp("created_at").notNull().defaultNow(),
    updatedAt:            timestamp("updated_at").notNull().defaultNow(),
  },
  (table) => [
    // Performance indexes for MMO-scale queries
    index("ws_world_id_idx").on(table.worldId),
    index("ws_spawn_type_idx").on(table.spawnType),
    index("ws_spawn_subtype_idx").on(table.spawnSubtype),
    index("ws_status_idx").on(table.status),
    index("ws_expires_at_idx").on(table.expiresAt),
    index("ws_tile_x_idx").on(table.tileX),
    index("ws_tile_y_idx").on(table.tileY),
    // Compound index for the most common query: active spawns in a world
    index("ws_world_status_idx").on(table.worldId, table.status),
  ],
);

// --- Drizzle-Zod schemas ---

export const insertWorldSpawnSchema = createInsertSchema(worldSpawnsTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});

export type InsertWorldSpawn = z.infer<typeof insertWorldSpawnSchema>;
export type WorldSpawn = typeof worldSpawnsTable.$inferSelect;
