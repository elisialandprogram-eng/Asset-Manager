import {
  pgTable,
  serial,
  integer,
  real,
  timestamp,
  uniqueIndex,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

/**
 * action_points — Per-kingdom AP pool.
 *
 * Rules (from GAME_DESIGN_BIBLE.md §14):
 *   - Base max: 200 AP  (300 with research)
 *   - Regen: 1 AP every 6 minutes (10 AP/hr)
 *   - AP costs: T1 monster = 6, T2 = 12, T3 = 20, T4 = 30, T5 = 40
 *
 * lastRegenAt tracks when AP was last recalculated.
 * Lazy regen: recalculate on every AP-gated request.
 */
export const actionPointsTable = pgTable(
  "action_points",
  {
    id: serial("id").primaryKey(),

    kingdomId: integer("kingdom_id").notNull(),

    currentAP: real("current_ap").notNull().default(200),

    maxAP: real("max_ap").notNull().default(200),

    regenRatePerMinute: real("regen_rate_per_minute").notNull().default(1.0 / 6.0),

    lastRegenAt: timestamp("last_regen_at").notNull().defaultNow(),

    createdAt: timestamp("created_at").notNull().defaultNow(),
    updatedAt: timestamp("updated_at").notNull().defaultNow(),
  },
  (t) => [
    uniqueIndex("ap_kingdom_unique").on(t.kingdomId),
  ],
);

export const insertActionPointSchema = createInsertSchema(actionPointsTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});
export type InsertActionPoint = z.infer<typeof insertActionPointSchema>;
export type ActionPoint = typeof actionPointsTable.$inferSelect;
