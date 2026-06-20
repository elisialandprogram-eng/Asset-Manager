import {
  pgTable,
  serial,
  integer,
  text,
  real,
  boolean,
  timestamp,
  jsonb,
  pgEnum,
  index,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const heroRarityEnum = pgEnum("hero_rarity", [
  "common",
  "uncommon",
  "rare",
  "epic",
  "legendary",
]);

export const heroSkillSchema = z.object({
  skillId: z.string(),
  name: z.string(),
  description: z.string(),
  triggerRound: z.number().int().optional(),
  effectType: z.string(),
  effectValue: z.number(),
});
export type HeroSkill = z.infer<typeof heroSkillSchema>;

export const heroStatsSchema = z.object({
  command: z.number().int(),
  attack: z.number().int(),
  defense: z.number().int(),
  speed: z.number().int(),
  gathering: z.number().int(),
});
export type HeroStats = z.infer<typeof heroStatsSchema>;

/**
 * heroes — One row per hero owned by a kingdom.
 *
 * Supports future ERC721 migration via nftTokenId.
 * Each hero is unique (rarity + level + skills).
 * A march may have 0 or 1 hero assigned (heroId on marches table).
 */
export const heroesTable = pgTable(
  "heroes",
  {
    id: serial("id").primaryKey(),

    kingdomId: integer("kingdom_id").notNull(),

    assetId: text("asset_id").notNull(),

    name: text("name").notNull(),

    rarity: heroRarityEnum("rarity").notNull().default("common"),

    level: integer("level").notNull().default(1),

    experience: integer("experience").notNull().default(0),

    experienceToNext: integer("experience_to_next").notNull().default(100),

    leadershipCapacity: integer("leadership_capacity").notNull().default(5000),

    troopAffinity: text("troop_affinity"),

    stats: jsonb("stats").notNull().$type<HeroStats>(),

    skills: jsonb("skills").notNull().$type<HeroSkill[]>().default([]),

    isLeading: boolean("is_leading").notNull().default(false),

    nftContractAddress: text("nft_contract_address"),
    nftTokenId: text("nft_token_id"),

    createdAt: timestamp("created_at").notNull().defaultNow(),
    updatedAt: timestamp("updated_at").notNull().defaultNow(),
  },
  (t) => [
    index("hero_kingdom_idx").on(t.kingdomId),
    index("hero_leading_idx").on(t.kingdomId, t.isLeading),
  ],
);

export const insertHeroSchema = createInsertSchema(heroesTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});
export type InsertHero = z.infer<typeof insertHeroSchema>;
export type Hero = typeof heroesTable.$inferSelect;
