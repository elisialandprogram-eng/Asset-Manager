import { pgTable, serial, integer, timestamp, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const upgradeStatusEnum = pgEnum("upgrade_status", [
  "queued",
  "in_progress",
  "completed",
  "cancelled",
]);

export const upgradeQueueTable = pgTable("upgrade_queue", {
  id: serial("id").primaryKey(),
  kingdomId: integer("kingdom_id").notNull(),
  buildingId: integer("building_id").notNull(),
  fromLevel: integer("from_level").notNull(),
  toLevel: integer("to_level").notNull(),
  foodCost: integer("food_cost").notNull().default(0),
  woodCost: integer("wood_cost").notNull().default(0),
  stoneCost: integer("stone_cost").notNull().default(0),
  ironCost: integer("iron_cost").notNull().default(0),
  goldCost: integer("gold_cost").notNull().default(0),
  startsAt: timestamp("starts_at").notNull().defaultNow(),
  endsAt: timestamp("ends_at").notNull(),
  status: upgradeStatusEnum("status").notNull().default("in_progress"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
});

export const insertUpgradeQueueSchema = createInsertSchema(upgradeQueueTable).omit({
  id: true,
  createdAt: true,
});
export type InsertUpgradeQueue = z.infer<typeof insertUpgradeQueueSchema>;
export type UpgradeQueueItem = typeof upgradeQueueTable.$inferSelect;
