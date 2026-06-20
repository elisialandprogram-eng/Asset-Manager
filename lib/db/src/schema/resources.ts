import { pgTable, serial, integer, timestamp } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const resourcesTable = pgTable("resources", {
  id: serial("id").primaryKey(),
  kingdomId: integer("kingdom_id").notNull().unique(),
  food: integer("food").notNull().default(500),
  wood: integer("wood").notNull().default(500),
  stone: integer("stone").notNull().default(500),
  iron: integer("iron").notNull().default(200),
  gold: integer("gold").notNull().default(100),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertResourceSchema = createInsertSchema(resourcesTable).omit({ id: true });
export type InsertResource = z.infer<typeof insertResourceSchema>;
export type Resource = typeof resourcesTable.$inferSelect;
