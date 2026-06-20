import {
  pgTable,
  serial,
  integer,
  timestamp,
  jsonb,
  uniqueIndex,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

/**
 * Troop keys follow the pattern: {class}_t{tier}
 * Classes: infantry, cavalry, archer, siege
 * Tiers: 1-5
 *
 * Example: infantry_t1, cavalry_t3, siege_t2
 *
 * This JSONB map stores how many of each type the kingdom currently has
 * (available, not on march). Troops on march are subtracted on send,
 * returned on march completion.
 */
export const troopCountSchema = z.record(z.string(), z.number().int().min(0));
export type TroopCount = z.infer<typeof troopCountSchema>;

/**
 * troop_inventory — Per-kingdom T1-T5 tier-based troop storage.
 *
 * Phase 4 troop system. The legacy `troops` table (Phase 3 enum-based)
 * is kept for backward compatibility with gather marches.
 * Monster attack marches use this table exclusively.
 */
export const troopInventoryTable = pgTable(
  "troop_inventory",
  {
    id: serial("id").primaryKey(),

    kingdomId: integer("kingdom_id").notNull(),

    troops: jsonb("troops")
      .notNull()
      .$type<TroopCount>()
      .default({}),

    createdAt: timestamp("created_at").notNull().defaultNow(),
    updatedAt: timestamp("updated_at").notNull().defaultNow(),
  },
  (t) => [
    uniqueIndex("troop_inv_kingdom_unique").on(t.kingdomId),
  ],
);

export const insertTroopInventorySchema = createInsertSchema(troopInventoryTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});
export type InsertTroopInventory = z.infer<typeof insertTroopInventorySchema>;
export type TroopInventory = typeof troopInventoryTable.$inferSelect;
