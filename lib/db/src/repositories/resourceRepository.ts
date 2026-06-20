import { db } from "../index";
import { resourcesTable } from "../schema/resources";
import { eq } from "drizzle-orm";

export type ResourceInsert = typeof resourcesTable.$inferInsert;
export type Resource = typeof resourcesTable.$inferSelect;

export interface ResourceCosts {
  food: number;
  wood: number;
  stone: number;
  iron: number;
  gold: number;
}

export const resourceRepository = {
  async findByKingdomId(kingdomId: number): Promise<Resource | undefined> {
    const [row] = await db
      .select()
      .from(resourcesTable)
      .where(eq(resourcesTable.kingdomId, kingdomId))
      .limit(1);
    return row;
  },

  async insert(values: ResourceInsert): Promise<Resource> {
    const [row] = await db.insert(resourcesTable).values(values).returning();
    return row!;
  },

  async update(kingdomId: number, values: Partial<ResourceCosts>): Promise<void> {
    await db
      .update(resourcesTable)
      .set({ ...values, updatedAt: new Date() })
      .where(eq(resourcesTable.kingdomId, kingdomId));
  },

  async deduct(kingdomId: number, current: Resource, costs: ResourceCosts): Promise<void> {
    await db
      .update(resourcesTable)
      .set({
        food: current.food - costs.food,
        wood: current.wood - costs.wood,
        stone: current.stone - costs.stone,
        iron: current.iron - costs.iron,
        gold: current.gold - costs.gold,
        updatedAt: new Date(),
      })
      .where(eq(resourcesTable.kingdomId, kingdomId));
  },

  async applyTick(
    kingdomId: number,
    next: ResourceCosts,
  ): Promise<void> {
    await db
      .update(resourcesTable)
      .set({
        food: next.food,
        wood: next.wood,
        stone: next.stone,
        iron: next.iron,
        gold: next.gold,
        updatedAt: new Date(),
      })
      .where(eq(resourcesTable.kingdomId, kingdomId));
  },
};
