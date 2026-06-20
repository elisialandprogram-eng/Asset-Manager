import { drizzle } from "drizzle-orm/node-postgres";
import pg from "pg";
import * as schema from "./schema";

const { Pool } = pg;

if (!process.env.DATABASE_URL) {
  throw new Error(
    "DATABASE_URL must be set. Did you forget to provision a database?",
  );
}

export const pool = new Pool({ connectionString: process.env.DATABASE_URL });
export const db = drizzle(pool, { schema });

export * from "./schema";
export {
  kingdomRepository,
  resourceRepository,
  buildingRepository,
  upgradeQueueRepository,
  constructionRepository,
  worldRepository,
  userRepository,
  monsterRepository,
  monsterSpawnRepository,
  crystalNodeRepository,
  spawnRepository,
  marchRepository,
  heroRepository,
  actionPointRepository,
  battleReportRepository,
  hospitalRepository,
  inventoryRepository,
  troopInventoryRepository,
} from "./repositories";
export type {
  KingdomInsert,
  ResourceInsert,
  ResourceCosts,
  BuildingInsert,
  UpgradeQueueInsert,
  ConstructionQueueInsert,
  WorldInsert,
  MonsterInsert,
  CrystalNodeInsert,
  UserInsert,
  WorldSpawn,
  InsertWorldSpawn,
  March,
  InsertMarch,
  Hero,
  InsertHero,
  ActionPoint,
  InsertActionPoint,
  BattleReport,
  InsertBattleReport,
  Hospital,
  InsertHospital,
  Inventory,
  InsertInventory,
  TroopInventory,
  InsertTroopInventory,
} from "./repositories";

export {
  getDatabaseProviderInstance,
  resetDatabaseProvider,
  DrizzleProvider,
  SupabaseProvider,
} from "./providers";
export type { DatabaseProvider, DatabaseHealth } from "./providers";

export { getSupabaseClient, resetSupabaseClient } from "./supabase/client";
export { getSupabaseAdminClient, resetSupabaseAdminClient } from "./supabase/adminClient";
export { ensureStorageBuckets, STORAGE_BUCKETS } from "./supabase/storage";
export type { SupabaseStorageBucket } from "./supabase/types";
