import { pgTable, serial, text, timestamp, integer, boolean, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const buildingTypeEnum = pgEnum("building_type", [
  "palace",
  "farm",
  "lumber_mill",
  "quarry",
  "iron_mine",
  "gold_mine",
  "barracks",
  "wall",
  "watch_tower",
]);

export const buildingsTable = pgTable("buildings", {
  id: serial("id").primaryKey(),
  kingdomId: integer("kingdom_id").notNull(),
  buildingType: buildingTypeEnum("building_type").notNull(),
  level: integer("level").notNull().default(1),
  assetId: text("asset_id").notNull(),
  isConstructing: boolean("is_constructing").notNull().default(false),
  constructionEndsAt: timestamp("construction_ends_at"),
  positionX: integer("position_x"),
  positionY: integer("position_y"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertBuildingSchema = createInsertSchema(buildingsTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertBuilding = z.infer<typeof insertBuildingSchema>;
export type Building = typeof buildingsTable.$inferSelect;
