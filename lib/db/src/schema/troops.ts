import { pgTable, serial, integer, timestamp, text, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const troopTypeEnum = pgEnum("troop_type", [
  "militia",
  "spearman",
  "archer",
  "scout",
  "knight",
  "catapult",
  "dragon_rider",
]);

export const troopsTable = pgTable("troops", {
  id: serial("id").primaryKey(),
  kingdomId: integer("kingdom_id").notNull(),
  troopType: troopTypeEnum("troop_type").notNull(),
  assetId: text("asset_id").notNull(),
  count: integer("count").notNull().default(0),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertTroopSchema = createInsertSchema(troopsTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertTroop = z.infer<typeof insertTroopSchema>;
export type Troop = typeof troopsTable.$inferSelect;
