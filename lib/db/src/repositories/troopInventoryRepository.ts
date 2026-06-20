/**
 * troopInventoryRepository.ts — All DB operations for the troop_inventory table.
 *
 * Phase 4 troop system: T1-T5 Infantry/Cavalry/Archer/Siege.
 * JSONB-based storage for flexibility and future NFT bridging.
 *
 * Key format: "{class}_t{tier}" (e.g. "infantry_t1", "cavalry_t3")
 */

import { db } from "../index";
import { troopInventoryTable } from "../schema/troopInventory";
import { eq } from "drizzle-orm";
import type { TroopInventory, InsertTroopInventory, TroopCount } from "../schema/troopInventory";

export type { TroopInventory, InsertTroopInventory };

export const troopInventoryRepository = {
  async findByKingdomId(kingdomId: number): Promise<TroopInventory | null> {
    const rows = await db
      .select()
      .from(troopInventoryTable)
      .where(eq(troopInventoryTable.kingdomId, kingdomId))
      .limit(1);
    return rows[0] ?? null;
  },

  async upsert(values: InsertTroopInventory): Promise<TroopInventory> {
    const existing = await this.findByKingdomId(values.kingdomId);
    if (existing) return existing;
    const rows = await db.insert(troopInventoryTable).values(values).returning();
    if (!rows[0]) throw new Error("TroopInventory insert returned no rows");
    return rows[0];
  },

  /** Add troops to inventory (additive). Creates record if missing. */
  async addTroops(kingdomId: number, newTroops: TroopCount): Promise<TroopInventory> {
    let inv = await this.findByKingdomId(kingdomId);
    if (!inv) {
      inv = await this.upsert({ kingdomId, troops: {} });
    }
    const current = inv.troops as TroopCount;
    const updated: TroopCount = { ...current };
    for (const [key, count] of Object.entries(newTroops)) {
      if (count > 0) updated[key] = (updated[key] ?? 0) + count;
    }
    const rows = await db
      .update(troopInventoryTable)
      .set({ troops: updated, updatedAt: new Date() })
      .where(eq(troopInventoryTable.kingdomId, kingdomId))
      .returning();
    if (!rows[0]) throw new Error("TroopInventory update failed");
    return rows[0];
  },

  /**
   * Deduct troops for a march. Throws if insufficient.
   * Returns snapshot BEFORE deduction for the march record.
   */
  async deductTroops(kingdomId: number, deduct: TroopCount): Promise<TroopInventory> {
    const inv = await this.findByKingdomId(kingdomId);
    if (!inv) throw new Error("Troop inventory not found for kingdom");
    const current = inv.troops as TroopCount;
    const updated: TroopCount = { ...current };
    for (const [key, count] of Object.entries(deduct)) {
      if (count <= 0) continue;
      const have = updated[key] ?? 0;
      if (have < count) {
        throw new Error(`Insufficient troops: need ${count} ${key}, have ${have}`);
      }
      updated[key] = have - count;
      if (updated[key] === 0) delete updated[key];
    }
    const rows = await db
      .update(troopInventoryTable)
      .set({ troops: updated, updatedAt: new Date() })
      .where(eq(troopInventoryTable.kingdomId, kingdomId))
      .returning();
    if (!rows[0]) throw new Error("TroopInventory deduct failed");
    return rows[0];
  },

  /** Return surviving troops after a march. */
  async returnTroops(kingdomId: number, returning: TroopCount): Promise<TroopInventory> {
    return this.addTroops(kingdomId, returning);
  },
};
