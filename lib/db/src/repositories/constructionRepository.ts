import { db } from "../index";
import { constructionQueueTable } from "../schema/constructionQueue";
import { eq, and, lte } from "drizzle-orm";

export type ConstructionQueueInsert = typeof constructionQueueTable.$inferInsert;
export type ConstructionQueueItem = typeof constructionQueueTable.$inferSelect;

export const constructionRepository = {
  async findInProgressByKingdomId(kingdomId: number): Promise<ConstructionQueueItem[]> {
    return db
      .select()
      .from(constructionQueueTable)
      .where(
        and(
          eq(constructionQueueTable.kingdomId, kingdomId),
          eq(constructionQueueTable.status, "in_progress"),
        ),
      );
  },

  async findCompletedNow(now: Date): Promise<ConstructionQueueItem[]> {
    return db
      .select()
      .from(constructionQueueTable)
      .where(
        and(
          eq(constructionQueueTable.status, "in_progress"),
          lte(constructionQueueTable.endsAt, now),
        ),
      );
  },

  async insert(values: ConstructionQueueInsert): Promise<ConstructionQueueItem> {
    const [row] = await db.insert(constructionQueueTable).values(values).returning();
    return row!;
  },

  async markCompleted(id: number): Promise<void> {
    await db
      .update(constructionQueueTable)
      .set({ status: "completed" })
      .where(eq(constructionQueueTable.id, id));
  },
};
