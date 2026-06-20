import { pgTable, serial, integer, timestamp, pgEnum } from "drizzle-orm/pg-core";
import { buildingTypeEnum } from "./buildings";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const constructionStatusEnum = pgEnum("construction_status", [
  "in_progress",
  "completed",
  "cancelled",
]);

export const constructionQueueTable = pgTable("construction_queue", {
  id: serial("id").primaryKey(),
  kingdomId: integer("kingdom_id").notNull(),
  buildingId: integer("building_id").notNull(),
  buildingType: buildingTypeEnum("building_type").notNull(),
  foodCost: integer("food_cost").notNull().default(0),
  woodCost: integer("wood_cost").notNull().default(0),
  stoneCost: integer("stone_cost").notNull().default(0),
  ironCost: integer("iron_cost").notNull().default(0),
  goldCost: integer("gold_cost").notNull().default(0),
  startsAt: timestamp("starts_at").notNull().defaultNow(),
  endsAt: timestamp("ends_at").notNull(),
  status: constructionStatusEnum("construction_status").notNull().default("in_progress"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
});

export const insertConstructionQueueSchema = createInsertSchema(constructionQueueTable).omit({
  id: true,
  createdAt: true,
});
export type InsertConstructionQueue = z.infer<typeof insertConstructionQueueSchema>;
export type ConstructionQueueItem = typeof constructionQueueTable.$inferSelect;
