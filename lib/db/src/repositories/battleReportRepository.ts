/**
 * battleReportRepository.ts — All DB operations for the battle_reports table.
 *
 * Reports are permanent — never deleted.
 * Supports future PvP reports (Phase 5+).
 */

import { db } from "../index";
import { battleReportsTable } from "../schema/battleReports";
import { eq, desc } from "drizzle-orm";
import type { BattleReport, InsertBattleReport } from "../schema/battleReports";

export type { BattleReport, InsertBattleReport };

export const battleReportRepository = {
  async findById(id: number): Promise<BattleReport | null> {
    const rows = await db
      .select()
      .from(battleReportsTable)
      .where(eq(battleReportsTable.id, id))
      .limit(1);
    return rows[0] ?? null;
  },

  /** Latest reports for a kingdom (most recent first). */
  async findByKingdomId(
    kingdomId: number,
    limit = 20,
    offset = 0,
  ): Promise<BattleReport[]> {
    return db
      .select()
      .from(battleReportsTable)
      .where(eq(battleReportsTable.attackerKingdomId, kingdomId))
      .orderBy(desc(battleReportsTable.createdAt))
      .limit(limit)
      .offset(offset);
  },

  async insert(values: InsertBattleReport): Promise<BattleReport> {
    const rows = await db.insert(battleReportsTable).values(values).returning();
    if (!rows[0]) throw new Error("Battle report insert returned no rows");
    return rows[0];
  },

  /** Link a battle report to a march (called when march is resolved). */
  async countByKingdomId(kingdomId: number): Promise<number> {
    const rows = await db
      .select({ id: battleReportsTable.id })
      .from(battleReportsTable)
      .where(eq(battleReportsTable.attackerKingdomId, kingdomId));
    return rows.length;
  },
};
