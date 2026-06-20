/**
 * heroRepository.ts — All DB operations for the heroes table.
 *
 * No raw SQL. Drizzle ORM only.
 * Supports future ERC721 migration (nftContractAddress / nftTokenId fields).
 */

import { db } from "../index";
import { heroesTable } from "../schema/heroes";
import { eq, and } from "drizzle-orm";
import type { Hero, InsertHero } from "../schema/heroes";

export type { Hero, InsertHero };

export const heroRepository = {
  async findById(id: number): Promise<Hero | null> {
    const rows = await db.select().from(heroesTable).where(eq(heroesTable.id, id)).limit(1);
    return rows[0] ?? null;
  },

  async findByKingdomId(kingdomId: number): Promise<Hero[]> {
    return db.select().from(heroesTable).where(eq(heroesTable.kingdomId, kingdomId));
  },

  async findLeadingHero(kingdomId: number): Promise<Hero | null> {
    const rows = await db
      .select()
      .from(heroesTable)
      .where(and(eq(heroesTable.kingdomId, kingdomId), eq(heroesTable.isLeading, true)))
      .limit(1);
    return rows[0] ?? null;
  },

  async insert(values: InsertHero): Promise<Hero> {
    const rows = await db.insert(heroesTable).values(values).returning();
    if (!rows[0]) throw new Error("Hero insert returned no rows");
    return rows[0];
  },

  async addExperience(id: number, xp: number): Promise<Hero | null> {
    const hero = await this.findById(id);
    if (!hero) return null;

    const newXp = hero.experience + xp;
    const newXpToNext = hero.experienceToNext;

    let newLevel = hero.level;
    let remainingXp = newXp;
    let xpToNext = newXpToNext;

    while (remainingXp >= xpToNext && newLevel < 80) {
      remainingXp -= xpToNext;
      newLevel++;
      xpToNext = Math.floor(newLevel * 150);
    }

    const rows = await db
      .update(heroesTable)
      .set({
        level: newLevel,
        experience: remainingXp,
        experienceToNext: xpToNext,
        leadershipCapacity: 5000 + newLevel * 200,
        updatedAt: new Date(),
      })
      .where(eq(heroesTable.id, id))
      .returning();
    return rows[0] ?? null;
  },

  async setLeading(heroId: number, kingdomId: number, isLeading: boolean): Promise<void> {
    if (isLeading) {
      await db
        .update(heroesTable)
        .set({ isLeading: false, updatedAt: new Date() })
        .where(eq(heroesTable.kingdomId, kingdomId));
    }
    await db
      .update(heroesTable)
      .set({ isLeading, updatedAt: new Date() })
      .where(eq(heroesTable.id, heroId));
  },

  async delete(id: number): Promise<void> {
    await db.delete(heroesTable).where(eq(heroesTable.id, id));
  },
};
