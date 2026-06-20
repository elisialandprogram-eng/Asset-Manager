import { pgTable, serial, integer, timestamp, text, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const researchTypeEnum = pgEnum("research_type", [
  "agriculture",
  "forestry",
  "mining",
  "metallurgy",
  "archery",
  "swordsmanship",
  "cavalry",
  "fortification",
  "alchemy",
  "dragon_taming",
]);

export const researchTable = pgTable("research", {
  id: serial("id").primaryKey(),
  kingdomId: integer("kingdom_id").notNull(),
  researchType: researchTypeEnum("research_type").notNull(),
  level: integer("level").notNull().default(0),
  isResearching: integer("is_researching").notNull().default(0),
  researchEndsAt: timestamp("research_ends_at"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertResearchSchema = createInsertSchema(researchTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertResearch = z.infer<typeof insertResearchSchema>;
export type Research = typeof researchTable.$inferSelect;
