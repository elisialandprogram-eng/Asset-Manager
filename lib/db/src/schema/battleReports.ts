import {
  pgTable,
  serial,
  integer,
  text,
  boolean,
  timestamp,
  jsonb,
  index,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const combatRoundSchema = z.object({
  round: z.number().int(),
  attackerDamageDealt: z.number(),
  defenderDamageDealt: z.number(),
  attackerHpAfter: z.number(),
  defenderHpAfter: z.number(),
  attackerTroopsLostThisRound: z.record(z.string(), z.number()),
});
export type CombatRound = z.infer<typeof combatRoundSchema>;

export const casualtyMapSchema = z.record(z.string(), z.number());
export type CasualtiesMap = z.infer<typeof casualtyMapSchema>;

export const rewardsSchema = z.object({
  food:    z.number().optional(),
  wood:    z.number().optional(),
  stone:   z.number().optional(),
  iron:    z.number().optional(),
  gold:    z.number().optional(),
  crystal: z.number().optional(),
  items:   z.record(z.string(), z.number()).optional(),
  heroXp:  z.number().optional(),
});
export type Rewards = z.infer<typeof rewardsSchema>;

/**
 * battle_reports — Permanent record of every PvE combat.
 *
 * Never deleted. Referenced by Unity BattleReportPanel.
 * Phase 5 will add PvP report fields (attackerKingdomId vs defenderKingdomId).
 */
export const battleReportsTable = pgTable(
  "battle_reports",
  {
    id: serial("id").primaryKey(),

    attackerKingdomId: integer("attacker_kingdom_id").notNull(),

    defenderMonsterSpawnId: integer("defender_monster_spawn_id"),

    monsterName: text("monster_name").notNull(),

    monsterTier: text("monster_tier").notNull(),

    attackerWon: boolean("attacker_won").notNull(),

    roundsFought: integer("rounds_fought").notNull(),

    rounds: jsonb("rounds").notNull().$type<CombatRound[]>().default([]),

    attackerTroopsSent: jsonb("attacker_troops_sent")
      .notNull()
      .$type<Record<string, number>>()
      .default({}),

    attackerTroopsKilled: jsonb("attacker_troops_killed")
      .notNull()
      .$type<CasualtiesMap>()
      .default({}),

    attackerTroopsWounded: jsonb("attacker_troops_wounded")
      .notNull()
      .$type<CasualtiesMap>()
      .default({}),

    attackerTroopsSurvived: jsonb("attacker_troops_survived")
      .notNull()
      .$type<Record<string, number>>()
      .default({}),

    rewardsGranted: jsonb("rewards_granted")
      .notNull()
      .$type<Rewards>()
      .default({}),

    heroId: integer("hero_id"),

    marchId: integer("march_id"),

    createdAt: timestamp("created_at").notNull().defaultNow(),
  },
  (t) => [
    index("report_kingdom_idx").on(t.attackerKingdomId),
    index("report_created_idx").on(t.createdAt),
    index("report_march_idx").on(t.marchId),
  ],
);

export const insertBattleReportSchema = createInsertSchema(battleReportsTable).omit({
  id: true,
  createdAt: true,
});
export type InsertBattleReport = z.infer<typeof insertBattleReportSchema>;
export type BattleReport = typeof battleReportsTable.$inferSelect;
