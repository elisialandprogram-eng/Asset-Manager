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
 * Item keys follow a consistent naming convention:
 *
 * Speedups:    speedup_universal_1m, speedup_universal_1h, speedup_universal_3h,
 *              speedup_universal_8h, speedup_universal_24h
 *              speedup_training_1h, speedup_research_1h, speedup_medical_1h
 *
 * Resources:   resource_food_10k, resource_wood_10k, resource_stone_10k,
 *              resource_iron_5k, resource_gold_2500, resource_crystal_1k
 *
 * AP Potions:  ap_potion_small, ap_potion_medium, ap_potion_full
 *
 * Hero XP:     hero_xp_small, hero_xp_medium, hero_xp_large
 *
 * Future ERC1155: nftContractAddress + nftTokenId bridged via itemKey pattern.
 */
export const itemCountSchema = z.record(z.string(), z.number().int().min(0));
export type ItemCount = z.infer<typeof itemCountSchema>;

/**
 * inventory — Per-kingdom item bag (JSONB for flexibility).
 *
 * Supports future ERC1155 migration.
 * All item keys are registered in the asset registry for visual lookup.
 */
export const inventoryTable = pgTable(
  "inventory",
  {
    id: serial("id").primaryKey(),

    kingdomId: integer("kingdom_id").notNull(),

    items: jsonb("items")
      .notNull()
      .$type<ItemCount>()
      .default({}),

    createdAt: timestamp("created_at").notNull().defaultNow(),
    updatedAt: timestamp("updated_at").notNull().defaultNow(),
  },
  (t) => [
    uniqueIndex("inventory_kingdom_unique").on(t.kingdomId),
  ],
);

export const insertInventorySchema = createInsertSchema(inventoryTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});
export type InsertInventory = z.infer<typeof insertInventorySchema>;
export type Inventory = typeof inventoryTable.$inferSelect;
