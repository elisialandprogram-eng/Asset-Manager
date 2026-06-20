/**
 * spawnRepository.ts — All DB operations for the world_spawns table.
 *
 * This repository is the ONLY place that touches the world_spawns table.
 * All queries are parameterised via Drizzle ORM (no raw SQL injection risk).
 *
 * Performance notes:
 *   - findActiveByWorldId uses the compound (world_id, status) index
 *   - countActiveBySubtype uses the same compound index + groupBy
 *   - deleteStaleByWorldId uses the status + updated_at indexes
 */

import { db } from "../index";
import { worldSpawnsTable } from "../schema/worldSpawns";
import { eq, and, lt, count, inArray, lte, ne } from "drizzle-orm";
import type { WorldSpawn, InsertWorldSpawn } from "../schema/worldSpawns";

export type { WorldSpawn, InsertWorldSpawn };

export const spawnRepository = {
  // ---------------------------------------------------------------------------
  // Reads
  // ---------------------------------------------------------------------------

  /**
   * Returns all ACTIVE spawns for a world.
   * Used by the frontend GET /worlds/:id/active-spawns endpoint.
   */
  async findActiveByWorldId(worldId: number): Promise<WorldSpawn[]> {
    return db
      .select()
      .from(worldSpawnsTable)
      .where(
        and(
          eq(worldSpawnsTable.worldId, worldId),
          eq(worldSpawnsTable.status, "active"),
        ),
      );
  },

  /**
   * Returns active rows whose expiresAt is in the past.
   * Used by processExpiredSpawns to mark them EXPIRED.
   */
  async findExpiredByWorldId(worldId: number, now: Date): Promise<WorldSpawn[]> {
    return db
      .select()
      .from(worldSpawnsTable)
      .where(
        and(
          eq(worldSpawnsTable.worldId, worldId),
          eq(worldSpawnsTable.status, "active"),
          lte(worldSpawnsTable.expiresAt, now),
        ),
      );
  },

  /**
   * Returns a count of active spawns grouped by subtype.
   * Used by spawnGenerator to compute per-subtype deficits.
   */
  async countActiveBySubtype(worldId: number): Promise<Record<string, number>> {
    const rows = await db
      .select({
        subtype: worldSpawnsTable.spawnSubtype,
        cnt: count(),
      })
      .from(worldSpawnsTable)
      .where(
        and(
          eq(worldSpawnsTable.worldId, worldId),
          eq(worldSpawnsTable.status, "active"),
        ),
      )
      .groupBy(worldSpawnsTable.spawnSubtype);

    const result: Record<string, number> = {};
    for (const row of rows) {
      result[row.subtype] = Number(row.cnt);
    }
    return result;
  },

  /**
   * Returns aggregate stats for the admin dashboard.
   */
  async getStats(worldId: number): Promise<{
    totalActive: number;
    totalExpired: number;
    totalDepleted: number;
    bySubtype: Record<string, number>;
    byBiome: Record<string, number>;
  }> {
    const rows = await db
      .select({
        status: worldSpawnsTable.status,
        subtype: worldSpawnsTable.spawnSubtype,
        biome: worldSpawnsTable.biome,
        cnt: count(),
      })
      .from(worldSpawnsTable)
      .where(eq(worldSpawnsTable.worldId, worldId))
      .groupBy(
        worldSpawnsTable.status,
        worldSpawnsTable.spawnSubtype,
        worldSpawnsTable.biome,
      );

    let totalActive = 0, totalExpired = 0, totalDepleted = 0;
    const bySubtype: Record<string, number> = {};
    const byBiome: Record<string, number> = {};

    for (const row of rows) {
      const n = Number(row.cnt);
      if (row.status === "active")   totalActive   += n;
      if (row.status === "expired")  totalExpired  += n;
      if (row.status === "depleted") totalDepleted += n;

      if (row.status === "active") {
        bySubtype[row.subtype] = (bySubtype[row.subtype] ?? 0) + n;
        byBiome[row.biome]     = (byBiome[row.biome]     ?? 0) + n;
      }
    }

    return { totalActive, totalExpired, totalDepleted, bySubtype, byBiome };
  },

  // ---------------------------------------------------------------------------
  // Writes
  // ---------------------------------------------------------------------------

  /** Bulk-inserts new spawn rows. Uses batches of 200 for large inserts. */
  async insertMany(values: InsertWorldSpawn[]): Promise<void> {
    if (values.length === 0) return;
    const BATCH = 200;
    for (let i = 0; i < values.length; i += BATCH) {
      await db.insert(worldSpawnsTable).values(values.slice(i, i + BATCH));
    }
  },

  /** Marks a list of spawn IDs as EXPIRED */
  async markExpiredBatch(ids: number[]): Promise<void> {
    if (ids.length === 0) return;
    await db
      .update(worldSpawnsTable)
      .set({ status: "expired", updatedAt: new Date() })
      .where(inArray(worldSpawnsTable.id, ids));
  },

  /** Marks a single spawn as DEPLETED (gathered/defeated by a player) */
  async markDepleted(id: number): Promise<void> {
    await db
      .update(worldSpawnsTable)
      .set({ status: "depleted", depletedAt: new Date(), updatedAt: new Date() })
      .where(eq(worldSpawnsTable.id, id));
  },

  /**
   * Hard-deletes rows that have been in a non-active state for over 1 hour.
   * Returns the number of rows deleted.
   */
  async deleteStaleByWorldId(worldId: number, before: Date): Promise<number> {
    const result = await db
      .delete(worldSpawnsTable)
      .where(
        and(
          eq(worldSpawnsTable.worldId, worldId),
          ne(worldSpawnsTable.status, "active"),
          lt(worldSpawnsTable.updatedAt, before),
        ),
      )
      .returning({ id: worldSpawnsTable.id });
    return result.length;
  },

  /**
   * Hard-deletes ALL spawn rows for a world (used by admin reset endpoint).
   * Returns the number of rows deleted.
   */
  async deleteAllByWorldId(worldId: number): Promise<number> {
    const result = await db
      .delete(worldSpawnsTable)
      .where(eq(worldSpawnsTable.worldId, worldId))
      .returning({ id: worldSpawnsTable.id });
    return result.length;
  },
};
