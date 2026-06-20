import { db } from "../index";
import { worldsTable } from "../schema/worlds";
import { usersTable } from "../schema/users";
import { monstersTable } from "../schema/monsters";
import { monsterSpawnsTable } from "../schema/monsters";
import { crystalNodesTable } from "../schema/crystals";
import { eq } from "drizzle-orm";

export type WorldInsert = typeof worldsTable.$inferInsert;
export type World = typeof worldsTable.$inferSelect;
export type MonsterInsert = typeof monstersTable.$inferInsert;
export type Monster = typeof monstersTable.$inferSelect;
export type MonsterSpawnInsert = typeof monsterSpawnsTable.$inferInsert;
export type CrystalNodeInsert = typeof crystalNodesTable.$inferInsert;
export type CrystalNode = typeof crystalNodesTable.$inferSelect;
export type UserInsert = typeof usersTable.$inferInsert;
export type User = typeof usersTable.$inferSelect;

export const worldRepository = {
  async findByName(name: string): Promise<World | undefined> {
    const [row] = await db
      .select()
      .from(worldsTable)
      .where(eq(worldsTable.name, name))
      .limit(1);
    return row;
  },

  async findById(id: number): Promise<World | undefined> {
    const [row] = await db
      .select()
      .from(worldsTable)
      .where(eq(worldsTable.id, id))
      .limit(1);
    return row;
  },

  async findActive(): Promise<World | undefined> {
    const [row] = await db
      .select()
      .from(worldsTable)
      .where(eq(worldsTable.status, "active"))
      .limit(1);
    return row;
  },

  async findAll(): Promise<World[]> {
    return db.select().from(worldsTable);
  },

  async insert(values: WorldInsert): Promise<World> {
    const [row] = await db.insert(worldsTable).values(values).returning();
    return row!;
  },

  async update(id: number, values: Partial<WorldInsert>): Promise<World> {
    const [row] = await db
      .update(worldsTable)
      .set(values)
      .where(eq(worldsTable.id, id))
      .returning();
    return row!;
  },
};

export const userRepository = {
  async findByEmail(email: string): Promise<User | undefined> {
    const [row] = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.email, email))
      .limit(1);
    return row;
  },

  async findByUsername(username: string): Promise<User | undefined> {
    const [row] = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.username, username))
      .limit(1);
    return row;
  },

  async findById(id: number): Promise<User | undefined> {
    const [row] = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.id, id))
      .limit(1);
    return row;
  },

  async insert(values: UserInsert): Promise<User> {
    const [row] = await db.insert(usersTable).values(values).returning();
    return row!;
  },

  async updateLastLogin(id: number): Promise<void> {
    await db
      .update(usersTable)
      .set({ lastLoginAt: new Date() })
      .where(eq(usersTable.id, id));
  },

  async updatePassword(id: number, passwordHash: string): Promise<void> {
    await db
      .update(usersTable)
      .set({ passwordHash })
      .where(eq(usersTable.id, id));
  },
};

export const monsterRepository = {
  async findAll(): Promise<Monster[]> {
    return db.select().from(monstersTable);
  },

  async findById(id: number): Promise<Monster | undefined> {
    const [row] = await db
      .select()
      .from(monstersTable)
      .where(eq(monstersTable.id, id))
      .limit(1);
    return row;
  },

  async findFirstOne(): Promise<Monster | undefined> {
    const [row] = await db.select().from(monstersTable).limit(1);
    return row;
  },

  async insertMany(values: MonsterInsert[]): Promise<void> {
    await db.insert(monstersTable).values(values);
  },
};

export const monsterSpawnRepository = {
  async findById(id: number) {
    const [row] = await db
      .select()
      .from(monsterSpawnsTable)
      .where(eq(monsterSpawnsTable.id, id))
      .limit(1);
    return row;
  },

  async findFirstByWorldId(worldId: number) {
    const [row] = await db
      .select()
      .from(monsterSpawnsTable)
      .where(eq(monsterSpawnsTable.worldId, worldId))
      .limit(1);
    return row;
  },

  async findByWorldIdWithMonster(worldId: number) {
    return db
      .select({
        id: monsterSpawnsTable.id,
        worldId: monsterSpawnsTable.worldId,
        x: monsterSpawnsTable.x,
        y: monsterSpawnsTable.y,
        currentHp: monsterSpawnsTable.currentHp,
        respawnAt: monsterSpawnsTable.respawnAt,
        monsterId: monstersTable.id,
        monsterName: monstersTable.name,
        monsterTier: monstersTable.tier,
        monsterPower: monstersTable.power,
        monsterHp: monstersTable.hp,
        monsterAssetId: monstersTable.assetId,
      })
      .from(monsterSpawnsTable)
      .innerJoin(monstersTable, eq(monsterSpawnsTable.monsterId, monstersTable.id))
      .where(eq(monsterSpawnsTable.worldId, worldId));
  },

  async insertMany(values: MonsterSpawnInsert[]): Promise<void> {
    await db.insert(monsterSpawnsTable).values(values);
  },
};

export const crystalNodeRepository = {
  async findFirstByWorldId(worldId: number): Promise<CrystalNode | undefined> {
    const [row] = await db
      .select()
      .from(crystalNodesTable)
      .where(eq(crystalNodesTable.worldId, worldId))
      .limit(1);
    return row;
  },

  async findByWorldId(worldId: number): Promise<CrystalNode[]> {
    return db.select().from(crystalNodesTable).where(eq(crystalNodesTable.worldId, worldId));
  },

  async insertMany(values: CrystalNodeInsert[]): Promise<void> {
    await db.insert(crystalNodesTable).values(values);
  },
};
