import { pgTable, serial, text, timestamp, integer, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const worldStatusEnum = pgEnum("world_status", ["active", "inactive", "full", "maintenance"]);

export const worldsTable = pgTable("worlds", {
  id: serial("id").primaryKey(),
  name: text("name").notNull().unique(),
  description: text("description"),
  status: worldStatusEnum("status").notNull().default("active"),
  maxKingdoms: integer("max_kingdoms").notNull().default(1000),
  currentKingdoms: integer("current_kingdoms").notNull().default(0),
  season: integer("season").default(1),
  seed: integer("seed"),
  startedAt: timestamp("started_at").defaultNow(),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertWorldSchema = createInsertSchema(worldsTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertWorld = z.infer<typeof insertWorldSchema>;
export type World = typeof worldsTable.$inferSelect;
