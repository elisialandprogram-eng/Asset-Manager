import { db } from "../index";
import { kingdomsTable } from "../schema/kingdoms";
import { eq, and, isNull } from "drizzle-orm";

export type KingdomInsert = typeof kingdomsTable.$inferInsert;
export type Kingdom = typeof kingdomsTable.$inferSelect;

export const kingdomRepository = {
  async findByUserId(userId: number): Promise<Kingdom | undefined> {
    const [row] = await db
      .select()
      .from(kingdomsTable)
      .where(eq(kingdomsTable.userId, userId))
      .limit(1);
    return row;
  },

  async findById(id: number): Promise<Kingdom | undefined> {
    const [row] = await db
      .select()
      .from(kingdomsTable)
      .where(eq(kingdomsTable.id, id))
      .limit(1);
    return row;
  },

  async findByIdAndUserId(id: number, userId: number): Promise<Kingdom | undefined> {
    const [row] = await db
      .select()
      .from(kingdomsTable)
      .where(and(eq(kingdomsTable.id, id), eq(kingdomsTable.userId, userId)))
      .limit(1);
    return row;
  },

  async findIdByIdAndUserId(id: number, userId: number): Promise<{ id: number } | undefined> {
    const [row] = await db
      .select({ id: kingdomsTable.id })
      .from(kingdomsTable)
      .where(and(eq(kingdomsTable.id, id), eq(kingdomsTable.userId, userId)))
      .limit(1);
    return row;
  },

  async findAllActive(): Promise<{ id: number }[]> {
    return db
      .select({ id: kingdomsTable.id })
      .from(kingdomsTable)
      .where(eq(kingdomsTable.isActive, true));
  },

  async findByWorldId(worldId: number) {
    return db
      .select({
        id: kingdomsTable.id,
        name: kingdomsTable.name,
        mapX: kingdomsTable.mapX,
        mapY: kingdomsTable.mapY,
        power: kingdomsTable.power,
        worldId: kingdomsTable.worldId,
        isActive: kingdomsTable.isActive,
      })
      .from(kingdomsTable)
      .where(eq(kingdomsTable.worldId, worldId));
  },

  async findUnplacedByWorldId(worldId: number): Promise<Kingdom[]> {
    return db
      .select()
      .from(kingdomsTable)
      .where(and(eq(kingdomsTable.worldId, worldId), isNull(kingdomsTable.mapX)));
  },

  async findPlacedByWorldId(worldId: number) {
    return db
      .select({ id: kingdomsTable.id, x: kingdomsTable.mapX, y: kingdomsTable.mapY })
      .from(kingdomsTable)
      .where(eq(kingdomsTable.worldId, worldId));
  },

  async findExistingPositionsByWorldId(worldId: number) {
    return db
      .select({ x: kingdomsTable.mapX, y: kingdomsTable.mapY })
      .from(kingdomsTable)
      .where(eq(kingdomsTable.worldId, worldId));
  },

  async insert(values: KingdomInsert): Promise<Kingdom> {
    const [row] = await db.insert(kingdomsTable).values(values).returning();
    return row!;
  },

  async updatePower(id: number, power: number): Promise<void> {
    await db
      .update(kingdomsTable)
      .set({ power, updatedAt: new Date() })
      .where(eq(kingdomsTable.id, id));
  },

  async updatePosition(id: number, x: number, y: number): Promise<Kingdom | undefined> {
    const [row] = await db
      .update(kingdomsTable)
      .set({ mapX: x, mapY: y })
      .where(eq(kingdomsTable.id, id))
      .returning();
    return row;
  },
};
