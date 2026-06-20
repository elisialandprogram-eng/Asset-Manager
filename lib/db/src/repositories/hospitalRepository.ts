/**
 * hospitalRepository.ts — All DB operations for the hospital table.
 *
 * Hospital uses lazy healing: calculated at request time from lastHealAt.
 * Priority: T5 > T4 > T3 > T2 > T1 (higher tiers healed first).
 */

import { db } from "../index";
import { hospitalTable } from "../schema/hospital";
import { eq } from "drizzle-orm";
import type { Hospital, InsertHospital, WoundedTroops } from "../schema/hospital";
/** Extract tier from troop key (e.g. "infantry_t3" → 3). Returns 0 on failure. */
function getTroopTier(key: string): number {
  const match = /_t([1-5])$/.exec(key);
  return match && match[1] ? Number(match[1]) : 0;
}

export type { Hospital, InsertHospital };

/** Apply lazy healing based on elapsed time. Returns updated wounded map. */
function computeHealing(
  woundedTroops: WoundedTroops,
  healRatePerMinute: number,
  lastHealAt: Date,
  now: Date,
): WoundedTroops {
  const elapsedMs = now.getTime() - lastHealAt.getTime();
  const elapsedMinutes = elapsedMs / 60_000;
  let troopsToHeal = Math.floor(elapsedMinutes * healRatePerMinute);

  if (troopsToHeal <= 0) return woundedTroops;

  const updated = { ...woundedTroops };
  const tierOrder = [5, 4, 3, 2, 1];

  for (const tier of tierOrder) {
    for (const key of Object.keys(updated)) {
        if (getTroopTier(key) !== tier) continue;
      const available = updated[key] ?? 0;
      const heal = Math.min(available, troopsToHeal);
      updated[key] = available - heal;
      troopsToHeal -= heal;
      if (updated[key] === 0) delete updated[key];
      if (troopsToHeal <= 0) return updated;
    }
  }

  return updated;
}

export const hospitalRepository = {
  async findByKingdomId(kingdomId: number): Promise<Hospital | null> {
    const rows = await db
      .select()
      .from(hospitalTable)
      .where(eq(hospitalTable.kingdomId, kingdomId))
      .limit(1);
    return rows[0] ?? null;
  },

  async upsert(values: InsertHospital): Promise<Hospital> {
    const existing = await this.findByKingdomId(values.kingdomId);
    if (existing) return existing;
    const rows = await db.insert(hospitalTable).values(values).returning();
    if (!rows[0]) throw new Error("Hospital insert returned no rows");
    return rows[0];
  },

  /** Apply lazy healing and persist. Returns updated hospital record. */
  async heal(kingdomId: number): Promise<Hospital | null> {
    const hospital = await this.findByKingdomId(kingdomId);
    if (!hospital) return null;
    const now = new Date();
    const healed = computeHealing(
      hospital.woundedTroops as WoundedTroops,
      hospital.healRatePerMinute,
      hospital.lastHealAt,
      now,
    );
    const rows = await db
      .update(hospitalTable)
      .set({ woundedTroops: healed, lastHealAt: now, updatedAt: now })
      .where(eq(hospitalTable.kingdomId, kingdomId))
      .returning();
    return rows[0] ?? null;
  },

  /** Admit new wounded troops. Caller must pre-check capacity. */
  async admitWounded(
    kingdomId: number,
    newWounded: WoundedTroops,
  ): Promise<Hospital | null> {
    const hospital = await this.heal(kingdomId);
    if (!hospital) return null;

    const current = hospital.woundedTroops as WoundedTroops;
    const merged: WoundedTroops = { ...current };
    for (const [key, count] of Object.entries(newWounded)) {
      merged[key] = (merged[key] ?? 0) + count;
    }

    const rows = await db
      .update(hospitalTable)
      .set({ woundedTroops: merged, updatedAt: new Date() })
      .where(eq(hospitalTable.kingdomId, kingdomId))
      .returning();
    return rows[0] ?? null;
  },

  /** Get total wounded count (after lazy healing). */
  async getTotalWounded(kingdomId: number): Promise<number> {
    const hospital = await this.heal(kingdomId);
    if (!hospital) return 0;
    return Object.values(hospital.woundedTroops as WoundedTroops).reduce(
      (s, v) => s + v,
      0,
    );
  },

  async updateCapacity(kingdomId: number, capacity: number): Promise<void> {
    await db
      .update(hospitalTable)
      .set({ capacity, updatedAt: new Date() })
      .where(eq(hospitalTable.kingdomId, kingdomId));
  },
};
