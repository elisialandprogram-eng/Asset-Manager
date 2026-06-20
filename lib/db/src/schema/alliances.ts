import { pgTable, serial, text, integer, timestamp, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const allianceRoleEnum = pgEnum("alliance_role", ["leader", "officer", "member"]);
export const allianceDiplomacyEnum = pgEnum("alliance_diplomacy", ["allied", "neutral", "at_war"]);

export const alliancesTable = pgTable("alliances", {
  id: serial("id").primaryKey(),
  worldId: integer("world_id").notNull(),
  name: text("name").notNull(),
  tag: text("tag").notNull(),
  leaderId: integer("leader_id").notNull(),
  power: integer("power").notNull().default(0),
  memberCount: integer("member_count").notNull().default(1),
  description: text("description"),
  emblemAssetId: text("emblem_asset_id"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const allianceMembersTable = pgTable("alliance_members", {
  id: serial("id").primaryKey(),
  allianceId: integer("alliance_id").notNull(),
  kingdomId: integer("kingdom_id").notNull(),
  role: allianceRoleEnum("role").notNull().default("member"),
  joinedAt: timestamp("joined_at").notNull().defaultNow(),
});

export const insertAllianceSchema = createInsertSchema(alliancesTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertAlliance = z.infer<typeof insertAllianceSchema>;
export type Alliance = typeof alliancesTable.$inferSelect;
export type AllianceMember = typeof allianceMembersTable.$inferSelect;
