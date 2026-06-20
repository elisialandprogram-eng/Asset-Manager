import { pgTable, serial, text, timestamp, integer, boolean } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const kingdomsTable = pgTable("kingdoms", {
  id: serial("id").primaryKey(),
  userId: integer("user_id").notNull(),
  worldId: integer("world_id").notNull(),
  allianceId: integer("alliance_id"),
  name: text("name").notNull(),
  mapX: integer("map_x"),
  mapY: integer("map_y"),
  power: integer("power").notNull().default(0),
  isActive: boolean("is_active").notNull().default(true),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertKingdomSchema = createInsertSchema(kingdomsTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertKingdom = z.infer<typeof insertKingdomSchema>;
export type Kingdom = typeof kingdomsTable.$inferSelect;
