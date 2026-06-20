import { pgTable, serial, integer, timestamp, text, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const crystalTypeEnum = pgEnum("crystal_type", [
  "fire",
  "ice",
  "earth",
  "lightning",
  "void",
  "holy",
]);

export const crystalNodesTable = pgTable("crystal_nodes", {
  id: serial("id").primaryKey(),
  worldId: integer("world_id").notNull(),
  x: integer("x").notNull(),
  y: integer("y").notNull(),
  crystalType: crystalTypeEnum("crystal_type").notNull(),
  crystalYield: integer("crystal_yield").notNull().default(100),
  harvestedByKingdomId: integer("harvested_by_kingdom_id"),
  harvestExpiresAt: timestamp("harvest_expires_at"),
  assetId: text("asset_id"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertCrystalNodeSchema = createInsertSchema(crystalNodesTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertCrystalNode = z.infer<typeof insertCrystalNodeSchema>;
export type CrystalNode = typeof crystalNodesTable.$inferSelect;
