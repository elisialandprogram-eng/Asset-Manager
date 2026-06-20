import {
  pgTable,
  serial,
  integer,
  text,
  timestamp,
  pgEnum,
  real,
  jsonb,
  index,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

// --- Enums ---

export const marchTypeEnum = pgEnum("march_type", [
  "gather",          // harvest a resource node
  "attack_monster",  // PvE attack on a monster spawn (Phase 4)
  "attack",          // PvP attack (Phase 5+)
  "reinforce",       // send troops to ally (Phase 5+)
  "scout",           // reconnaissance (Phase 5+)
  "rally",           // multi-kingdom rally (Phase 6+)
]);

export const marchStatusEnum = pgEnum("march_status", [
  "idle",        // (reserved — not persisted)
  "outbound",    // marching toward destination
  "gathering",   // arrived; harvest timer running
  "returning",   // marching back home
  "completed",   // resources deposited; terminal state
  "cancelled",   // recalled before arrival; terminal state
]);

// --- TroopLoad sub-type (stored in troops JSONB column) ---

export const troopLoadSchema = z.object({
  militia:      z.number().int().min(0).optional(),
  spearman:     z.number().int().min(0).optional(),
  archer:       z.number().int().min(0).optional(),
  scout:        z.number().int().min(0).optional(),
  knight:       z.number().int().min(0).optional(),
  catapult:     z.number().int().min(0).optional(),
  dragon_rider: z.number().int().min(0).optional(),
});
export type TroopLoad = z.infer<typeof troopLoadSchema>;

// --- ResourceGathered sub-type (stored in resourcesGathered JSONB column) ---

export const resourceGatheredSchema = z.object({
  food:    z.number().min(0).optional(),
  wood:    z.number().min(0).optional(),
  stone:   z.number().min(0).optional(),
  iron:    z.number().min(0).optional(),
  gold:    z.number().min(0).optional(),
  crystal: z.number().min(0).optional(),
});
export type ResourceGathered = z.infer<typeof resourceGatheredSchema>;

// --- Table ---

/**
 * marches — One row per march event from creation to completion.
 *
 * Lifecycle:
 *   OUTBOUND → (arrives_at reached) → GATHERING → (gather_ends_at reached) →
 *   RETURNING → (returns_at reached) → COMPLETED
 *
 *   OUTBOUND → (player recalls) → CANCELLED
 *
 * Phase 3 scope: gather-type only. attack/reinforce/scout/rally fields are
 * nullable and reserved for Phase 5+.
 */
export const marchesTable = pgTable(
  "marches",
  {
    id: serial("id").primaryKey(),

    /** World the march takes place in */
    worldId: integer("world_id").notNull(),

    /** Kingdom that owns this march */
    kingdomId: integer("kingdom_id").notNull(),

    /** March category */
    marchType: marchTypeEnum("march_type").notNull().default("gather"),

    /** Current lifecycle state */
    status: marchStatusEnum("status").notNull().default("outbound"),

    // --- Origin (kingdom tile position at march creation) ---
    originX: integer("origin_x").notNull(),
    originY: integer("origin_y").notNull(),

    // --- Destination (target tile) ---
    destX: integer("dest_x").notNull(),
    destY: integer("dest_y").notNull(),

    /** world_spawns.id being targeted (null for PvP marches, Phase 5+) */
    spawnId: integer("spawn_id"),

    /** Target kingdom (null for PvP not yet implemented) */
    targetKingdomId: integer("target_kingdom_id"),

    /** Troops committed to this march (JSONB TroopLoad) */
    troops: jsonb("troops").notNull().$type<TroopLoad>(),

    /** Effective march speed in tiles/minute at creation time */
    speedTpm: real("speed_tpm").notNull(),

    /** Distance in tiles (Euclidean, computed at creation) */
    distanceTiles: real("distance_tiles").notNull(),

    /** When the march departed (= created_at for immediate departure) */
    startedAt: timestamp("started_at").notNull().defaultNow(),

    /** When the march arrives at destination */
    arrivesAt: timestamp("arrives_at").notNull(),

    /** When gathering phase ends and return march begins */
    gatherEndsAt: timestamp("gather_ends_at"),

    /** When the return march begins (= gatherEndsAt for gather type) */
    returnStartedAt: timestamp("return_started_at"),

    /** When the march returns home */
    returnsAt: timestamp("returns_at"),

    /** When the march was fully resolved (COMPLETED or CANCELLED) */
    completedAt: timestamp("completed_at"),

    /** Resources collected during the gathering phase (deposited on return) */
    resourcesGathered: jsonb("resources_gathered").$type<ResourceGathered>(),

    /** Hero leading this march (null = no hero). Phase 4 PvE. */
    heroId: integer("hero_id"),

    /** Battle report generated on monster combat completion. Phase 4. */
    battleReportId: integer("battle_report_id"),

    createdAt: timestamp("created_at").notNull().defaultNow(),
    updatedAt: timestamp("updated_at").notNull().defaultNow(),
  },
  (table) => [
    index("march_kingdom_idx").on(table.kingdomId),
    index("march_world_idx").on(table.worldId),
    index("march_status_idx").on(table.status),
    index("march_arrives_at_idx").on(table.arrivesAt),
    index("march_returns_at_idx").on(table.returnsAt),
    index("march_gather_ends_at_idx").on(table.gatherEndsAt),
    // Fast lookup: active marches in a kingdom
    index("march_kingdom_status_idx").on(table.kingdomId, table.status),
  ],
);

// --- Drizzle-Zod schemas ---

export const insertMarchSchema = createInsertSchema(marchesTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});

export type InsertMarch = z.infer<typeof insertMarchSchema>;
export type March = typeof marchesTable.$inferSelect;
