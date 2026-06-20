/**
 * inventoryRepository.ts — All DB operations for the inventory table.
 *
 * Items stored as JSONB map: itemKey → quantity.
 * Future ERC1155 bridge: itemKey maps to nft_contract_address + token_id
 * via the asset_registry.
 */

import { db } from "../index";
import { inventoryTable } from "../schema/inventory";
import { eq } from "drizzle-orm";
import type { Inventory, InsertInventory, ItemCount } from "../schema/inventory";

export type { Inventory, InsertInventory };

export const inventoryRepository = {
  async findByKingdomId(kingdomId: number): Promise<Inventory | null> {
    const rows = await db
      .select()
      .from(inventoryTable)
      .where(eq(inventoryTable.kingdomId, kingdomId))
      .limit(1);
    return rows[0] ?? null;
  },

  async upsert(values: InsertInventory): Promise<Inventory> {
    const existing = await this.findByKingdomId(values.kingdomId);
    if (existing) return existing;
    const rows = await db.insert(inventoryTable).values(values).returning();
    if (!rows[0]) throw new Error("Inventory insert returned no rows");
    return rows[0];
  },

  /** Add items to inventory (additive). Creates record if missing. */
  async addItems(kingdomId: number, newItems: ItemCount): Promise<Inventory> {
    let inventory = await this.findByKingdomId(kingdomId);
    if (!inventory) {
      inventory = await this.upsert({ kingdomId, items: {} });
    }
    const current = inventory.items as ItemCount;
    const updated: ItemCount = { ...current };
    for (const [key, qty] of Object.entries(newItems)) {
      if (qty > 0) updated[key] = (updated[key] ?? 0) + qty;
    }
    const rows = await db
      .update(inventoryTable)
      .set({ items: updated, updatedAt: new Date() })
      .where(eq(inventoryTable.kingdomId, kingdomId))
      .returning();
    if (!rows[0]) throw new Error("Inventory update failed");
    return rows[0];
  },

  /** Consume items. Throws if insufficient quantity. */
  async consumeItems(kingdomId: number, consume: ItemCount): Promise<Inventory> {
    const inventory = await this.findByKingdomId(kingdomId);
    if (!inventory) throw new Error("Inventory not found for kingdom");
    const current = inventory.items as ItemCount;
    const updated: ItemCount = { ...current };
    for (const [key, qty] of Object.entries(consume)) {
      const have = updated[key] ?? 0;
      if (have < qty) {
        throw new Error(`Insufficient item: need ${qty} ${key}, have ${have}`);
      }
      updated[key] = have - qty;
      if (updated[key] === 0) delete updated[key];
    }
    const rows = await db
      .update(inventoryTable)
      .set({ items: updated, updatedAt: new Date() })
      .where(eq(inventoryTable.kingdomId, kingdomId))
      .returning();
    if (!rows[0]) throw new Error("Inventory consume failed");
    return rows[0];
  },
};
