import { pgTable, serial, text, integer, timestamp, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const monsterTierEnum = pgEnum("monster_tier", ["common", "uncommon", "rare", "elite", "boss", "ancient"]);

export const monstersTable = pgTable("monsters", {
  id: serial("id").primaryKey(),
  assetId: text("asset_id").notNull(),
  name: text("name").notNull(),
  tier: monsterTierEnum("tier").notNull().default("common"),
  power: integer("power").notNull().default(100),
  hp: integer("hp").notNull().default(1000),
  attack: integer("attack").notNull().default(50),
  defense: integer("defense").notNull().default(30),
  createdAt: timestamp("created_at").notNull().defaultNow(),
});

export const monsterSpawnsTable = pgTable("monster_spawns", {
  id: serial("id").primaryKey(),
  monsterId: integer("monster_id").notNull(),
  worldId: integer("world_id").notNull(),
  x: integer("x").notNull(),
  y: integer("y").notNull(),
  currentHp: integer("current_hp").notNull(),
  respawnAt: timestamp("respawn_at"),
  defeatedByKingdomId: integer("defeated_by_kingdom_id"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertMonsterSchema = createInsertSchema(monstersTable).omit({ id: true, createdAt: true });
export const insertMonsterSpawnSchema = createInsertSchema(monsterSpawnsTable).omit({ id: true, createdAt: true, updatedAt: true });
export type InsertMonster = z.infer<typeof insertMonsterSchema>;
export type Monster = typeof monstersTable.$inferSelect;
export type MonsterSpawn = typeof monsterSpawnsTable.$inferSelect;
