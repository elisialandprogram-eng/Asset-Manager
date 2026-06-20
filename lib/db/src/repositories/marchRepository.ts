/**
 * marchRepository.ts — All DB operations for the marches table.
 *
 * This is the ONLY place that touches the marches table directly.
 * All queries use Drizzle ORM — no raw SQL.
 *
 * Index usage notes:
 *   findActiveByKingdomId  → march_kingdom_status_idx
 *   findPendingArrivals    → march_arrives_at_idx + march_status_idx
 *   findPendingGatherEnd   → march_gather_ends_at_idx + march_status_idx
 *   findPendingReturns     → march_returns_at_idx + march_status_idx
 */

import { db } from "../index";
import { marchesTable } from "../schema/marches";
import { eq, and, lte, inArray, or } from "drizzle-orm";
import type { March, InsertMarch } from "../schema/marches";

export type { March, InsertMarch };

/** Status values that represent a march still in-flight (not terminal). */
const ACTIVE_STATUSES = ["outbound", "gathering", "returning"] as const;

export const marchRepository = {
  // ---------------------------------------------------------------------------
  // Reads
  // ---------------------------------------------------------------------------

  async findById(id: number): Promise<March | null> {
    const rows = await db
      .select()
      .from(marchesTable)
      .where(eq(marchesTable.id, id))
      .limit(1);
    return rows[0] ?? null;
  },

  /** Returns all non-terminal marches for a kingdom (outbound/gathering/returning). */
  async findActiveByKingdomId(kingdomId: number): Promise<March[]> {
    return db
      .select()
      .from(marchesTable)
      .where(
        and(
          eq(marchesTable.kingdomId, kingdomId),
          inArray(marchesTable.status, [...ACTIVE_STATUSES]),
        ),
      );
  },

  /** Returns all marches for a kingdom regardless of status (for history). */
  async findAllByKingdomId(kingdomId: number): Promise<March[]> {
    return db
      .select()
      .from(marchesTable)
      .where(eq(marchesTable.kingdomId, kingdomId));
  },

  /**
   * Returns OUTBOUND marches across all kingdoms whose arrivesAt has passed.
   * Used by marchProcessor to transition outbound → gathering.
   */
  async findPendingArrivals(now: Date): Promise<March[]> {
    return db
      .select()
      .from(marchesTable)
      .where(
        and(
          eq(marchesTable.status, "outbound"),
          lte(marchesTable.arrivesAt, now),
        ),
      );
  },

  /**
   * Returns GATHERING marches whose gatherEndsAt has passed.
   * Used by marchProcessor to transition gathering → returning.
   */
  async findPendingGatherEnd(now: Date): Promise<March[]> {
    return db
      .select()
      .from(marchesTable)
      .where(
        and(
          eq(marchesTable.status, "gathering"),
          lte(marchesTable.gatherEndsAt, now),
        ),
      );
  },

  /**
   * Returns RETURNING marches whose returnsAt has passed.
   * Used by marchProcessor to transition returning → completed.
   */
  async findPendingReturns(now: Date): Promise<March[]> {
    return db
      .select()
      .from(marchesTable)
      .where(
        and(
          eq(marchesTable.status, "returning"),
          lte(marchesTable.returnsAt, now),
        ),
      );
  },

  // ---------------------------------------------------------------------------
  // Writes
  // ---------------------------------------------------------------------------

  async insert(values: InsertMarch): Promise<March> {
    const rows = await db
      .insert(marchesTable)
      .values(values)
      .returning();
    if (!rows[0]) throw new Error("March insert returned no rows");
    return rows[0];
  },

  /** Transition outbound → gathering: sets arrivesAt confirmation + gatherEndsAt */
  async markArrived(id: number, gatherEndsAt: Date): Promise<void> {
    await db
      .update(marchesTable)
      .set({
        status: "gathering",
        gatherEndsAt,
        updatedAt: new Date(),
      })
      .where(eq(marchesTable.id, id));
  },

  /** Transition gathering → returning: sets returnStartedAt + returnsAt */
  async markReturning(
    id: number,
    returnStartedAt: Date,
    returnsAt: Date,
    resourcesGathered: Record<string, number>,
  ): Promise<void> {
    await db
      .update(marchesTable)
      .set({
        status: "returning",
        returnStartedAt,
        returnsAt,
        resourcesGathered,
        updatedAt: new Date(),
      })
      .where(eq(marchesTable.id, id));
  },

  /** Transition returning → completed */
  async markCompleted(id: number): Promise<void> {
    await db
      .update(marchesTable)
      .set({
        status: "completed",
        completedAt: new Date(),
        updatedAt: new Date(),
      })
      .where(eq(marchesTable.id, id));
  },

  /**
   * Cancel an OUTBOUND march (not yet arrived).
   * Troops are returned immediately (handled at the route level before calling this).
   */
  async markCancelled(id: number): Promise<void> {
    await db
      .update(marchesTable)
      .set({
        status: "cancelled",
        completedAt: new Date(),
        updatedAt: new Date(),
      })
      .where(
        and(
          eq(marchesTable.id, id),
          eq(marchesTable.status, "outbound"),
        ),
      );
  },

  /** Hard-delete terminal march rows older than a given date (cleanup). */
  async deleteOldTerminal(before: Date): Promise<number> {
    const result = await db
      .delete(marchesTable)
      .where(
        and(
          or(
            eq(marchesTable.status, "completed"),
            eq(marchesTable.status, "cancelled"),
          ),
          lte(marchesTable.updatedAt, before),
        ),
      )
      .returning({ id: marchesTable.id });
    return result.length;
  },
};
