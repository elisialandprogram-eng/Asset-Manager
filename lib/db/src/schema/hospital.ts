import {
  pgTable,
  serial,
  integer,
  real,
  timestamp,
  jsonb,
  uniqueIndex,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const woundedTroopsSchema = z.record(z.string(), z.number());
export type WoundedTroops = z.infer<typeof woundedTroopsSchema>;

/**
 * hospital — Per-kingdom wound tracking.
 *
 * Rules (GAME_DESIGN_BIBLE.md §10):
 *   - Field operations: 50% dead, 50% wounded
 *   - Home defense: 100% wounded
 *   - Hospital capacity enforced; overflow = permanent death
 *   - Healing rate: 5 troops/min per Hospital level
 *   - Priority: T5 > T4 > T3 > T2 > T1
 *
 * woundedTroops: JSONB map of troopKey → count (e.g. "infantry_t1" → 50)
 * capacity: max troops that can be in hospital at once
 * lastHealAt: timestamp for lazy heal calculation
 */
export const hospitalTable = pgTable(
  "hospital",
  {
    id: serial("id").primaryKey(),

    kingdomId: integer("kingdom_id").notNull(),

    woundedTroops: jsonb("wounded_troops")
      .notNull()
      .$type<WoundedTroops>()
      .default({}),

    capacity: integer("capacity").notNull().default(500),

    healRatePerMinute: real("heal_rate_per_minute").notNull().default(5),

    lastHealAt: timestamp("last_heal_at").notNull().defaultNow(),

    createdAt: timestamp("created_at").notNull().defaultNow(),
    updatedAt: timestamp("updated_at").notNull().defaultNow(),
  },
  (t) => [
    uniqueIndex("hospital_kingdom_unique").on(t.kingdomId),
  ],
);

export const insertHospitalSchema = createInsertSchema(hospitalTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});
export type InsertHospital = z.infer<typeof insertHospitalSchema>;
export type Hospital = typeof hospitalTable.$inferSelect;
