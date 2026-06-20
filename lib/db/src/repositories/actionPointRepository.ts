/**
 * actionPointRepository.ts — All DB operations for the action_points table.
 *
 * AP uses lazy regen: recalculated at request time from lastRegenAt.
 * No background ticker needed — regen is computed on demand.
 */

import { db } from "../index";
import { actionPointsTable } from "../schema/actionPoints";
import { eq } from "drizzle-orm";
import type { ActionPoint, InsertActionPoint } from "../schema/actionPoints";

export type { ActionPoint, InsertActionPoint };

/** Compute AP regenerated since lastRegenAt. */
function computeRegen(ap: ActionPoint, now: Date): number {
  const elapsedMs = now.getTime() - ap.lastRegenAt.getTime();
  const elapsedMinutes = elapsedMs / 60_000;
  return elapsedMinutes * ap.regenRatePerMinute;
}

export const actionPointRepository = {
  async findByKingdomId(kingdomId: number): Promise<ActionPoint | null> {
    const rows = await db
      .select()
      .from(actionPointsTable)
      .where(eq(actionPointsTable.kingdomId, kingdomId))
      .limit(1);
    return rows[0] ?? null;
  },

  /**
   * Get current AP with lazy regen applied (does NOT persist — use regenerate() to save).
   */
  async getCurrentAp(kingdomId: number): Promise<{ currentAP: number; maxAP: number } | null> {
    const ap = await this.findByKingdomId(kingdomId);
    if (!ap) return null;
    const regen = computeRegen(ap, new Date());
    const current = Math.min(ap.currentAP + regen, ap.maxAP);
    return { currentAP: current, maxAP: ap.maxAP };
  },

  /** Upsert initial AP record for a new kingdom. */
  async upsert(values: InsertActionPoint): Promise<ActionPoint> {
    const existing = await this.findByKingdomId(values.kingdomId);
    if (existing) return existing;
    const rows = await db.insert(actionPointsTable).values(values).returning();
    if (!rows[0]) throw new Error("AP insert returned no rows");
    return rows[0];
  },

  /**
   * Apply regen and persist. Call before any AP-consuming operation.
   * Returns the post-regen snapshot.
   */
  async regenerate(kingdomId: number): Promise<ActionPoint | null> {
    const ap = await this.findByKingdomId(kingdomId);
    if (!ap) return null;
    const now = new Date();
    const regen = computeRegen(ap, now);
    const newAP = Math.min(ap.currentAP + regen, ap.maxAP);
    const rows = await db
      .update(actionPointsTable)
      .set({ currentAP: newAP, lastRegenAt: now, updatedAt: now })
      .where(eq(actionPointsTable.kingdomId, kingdomId))
      .returning();
    return rows[0] ?? null;
  },

  /** Deduct AP. Call after regenerate() — throws if insufficient. */
  async deduct(kingdomId: number, amount: number): Promise<ActionPoint> {
    const ap = await this.regenerate(kingdomId);
    if (!ap) throw new Error("Action points record not found for kingdom");
    if (ap.currentAP < amount) {
      throw new Error(
        `Insufficient AP: need ${amount}, have ${Math.floor(ap.currentAP)}`,
      );
    }
    const rows = await db
      .update(actionPointsTable)
      .set({ currentAP: ap.currentAP - amount, updatedAt: new Date() })
      .where(eq(actionPointsTable.kingdomId, kingdomId))
      .returning();
    if (!rows[0]) throw new Error("AP deduct failed");
    return rows[0];
  },

  /** Add AP (from potion use). Capped at maxAP. */
  async add(kingdomId: number, amount: number): Promise<ActionPoint | null> {
    const ap = await this.regenerate(kingdomId);
    if (!ap) return null;
    const newAP = Math.min(ap.currentAP + amount, ap.maxAP);
    const rows = await db
      .update(actionPointsTable)
      .set({ currentAP: newAP, updatedAt: new Date() })
      .where(eq(actionPointsTable.kingdomId, kingdomId))
      .returning();
    return rows[0] ?? null;
  },
};
