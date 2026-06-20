import { db } from "../index";
import { buildingsTable } from "../schema/buildings";
import { upgradeQueueTable } from "../schema/upgradeQueue";
import { eq, and } from "drizzle-orm";

export type BuildingInsert = typeof buildingsTable.$inferInsert;
export type Building = typeof buildingsTable.$inferSelect;
export type UpgradeQueueInsert = typeof upgradeQueueTable.$inferInsert;
export type UpgradeQueueItem = typeof upgradeQueueTable.$inferSelect;

export const buildingRepository = {
  async findById(id: number): Promise<Building | undefined> {
    const [row] = await db
      .select()
      .from(buildingsTable)
      .where(eq(buildingsTable.id, id))
      .limit(1);
    return row;
  },

  async findByKingdomId(kingdomId: number): Promise<Building[]> {
    return db.select().from(buildingsTable).where(eq(buildingsTable.kingdomId, kingdomId));
  },

  async findSnapshotsByKingdomId(kingdomId: number) {
    return db
      .select({
        buildingType: buildingsTable.buildingType,
        level: buildingsTable.level,
        isConstructing: buildingsTable.isConstructing,
      })
      .from(buildingsTable)
      .where(eq(buildingsTable.kingdomId, kingdomId));
  },

  async findTypeSnapshotsByKingdomId(kingdomId: number) {
    return db
      .select({ buildingType: buildingsTable.buildingType, isConstructing: buildingsTable.isConstructing })
      .from(buildingsTable)
      .where(eq(buildingsTable.kingdomId, kingdomId));
  },

  async findLevelSnapshotsByKingdomId(kingdomId: number) {
    return db
      .select({ buildingType: buildingsTable.buildingType, level: buildingsTable.level })
      .from(buildingsTable)
      .where(eq(buildingsTable.kingdomId, kingdomId));
  },

  async findActiveLevelSnapshotsByKingdomId(kingdomId: number) {
    return db
      .select({ buildingType: buildingsTable.buildingType, level: buildingsTable.level })
      .from(buildingsTable)
      .where(and(eq(buildingsTable.kingdomId, kingdomId), eq(buildingsTable.isConstructing, false)));
  },

  async insert(values: BuildingInsert): Promise<Building> {
    const [row] = await db.insert(buildingsTable).values(values).returning();
    return row!;
  },

  async markConstructing(id: number, endsAt: Date): Promise<void> {
    await db
      .update(buildingsTable)
      .set({ isConstructing: true, constructionEndsAt: endsAt, updatedAt: new Date() })
      .where(eq(buildingsTable.id, id));
  },

  async completeUpgrade(id: number, toLevel: number): Promise<void> {
    await db
      .update(buildingsTable)
      .set({ level: toLevel, isConstructing: false, constructionEndsAt: null, updatedAt: new Date() })
      .where(eq(buildingsTable.id, id));
  },

  async completeConstruction(id: number): Promise<void> {
    await db
      .update(buildingsTable)
      .set({ level: 1, isConstructing: false, constructionEndsAt: null, updatedAt: new Date() })
      .where(eq(buildingsTable.id, id));
  },
};

export const upgradeQueueRepository = {
  async findInProgressByKingdomId(kingdomId: number): Promise<UpgradeQueueItem[]> {
    return db
      .select()
      .from(upgradeQueueTable)
      .where(and(eq(upgradeQueueTable.kingdomId, kingdomId), eq(upgradeQueueTable.status, "in_progress")));
  },

  async findCompletedNow(now: Date): Promise<UpgradeQueueItem[]> {
    const { lte } = await import("drizzle-orm");
    return db
      .select()
      .from(upgradeQueueTable)
      .where(and(eq(upgradeQueueTable.status, "in_progress"), lte(upgradeQueueTable.endsAt, now)));
  },

  async insert(values: UpgradeQueueInsert): Promise<UpgradeQueueItem> {
    const [row] = await db.insert(upgradeQueueTable).values(values).returning();
    return row!;
  },

  async markCompleted(id: number): Promise<void> {
    await db
      .update(upgradeQueueTable)
      .set({ status: "completed" })
      .where(eq(upgradeQueueTable.id, id));
  },
};
