import pg from "pg";
import { drizzle, type NodePgDatabase } from "drizzle-orm/node-postgres";
import * as schema from "../schema";
import { getAppEnvironment } from "../config/database";
import type { DatabaseHealth, DatabaseProvider } from "./DatabaseProvider";

const { Pool } = pg;

export class DrizzleProvider implements DatabaseProvider {
  private pool: InstanceType<typeof Pool> | null = null;
  private _db: NodePgDatabase<typeof schema> | null = null;

  constructor(private readonly connectionString: string) {}

  get db(): NodePgDatabase<typeof schema> {
    if (!this._db) throw new Error("DrizzleProvider: not connected. Call connect() first.");
    return this._db;
  }

  async connect(): Promise<void> {
    this.pool = new Pool({ connectionString: this.connectionString });
    this._db = drizzle(this.pool, { schema });
  }

  async disconnect(): Promise<void> {
    await this.pool?.end();
    this.pool = null;
    this._db = null;
  }

  async healthCheck(): Promise<DatabaseHealth> {
    const start = Date.now();
    try {
      await this.pool?.query("SELECT 1");
      const latencyMs = Date.now() - start;
      const poolInfo = this.pool as unknown as { totalCount?: number };
      return {
        provider: "drizzle",
        status: "ok",
        latencyMs,
        activeConnections: poolInfo?.totalCount ?? 0,
        environment: getAppEnvironment(),
      };
    } catch (err) {
      return {
        provider: "drizzle",
        status: "error",
        latencyMs: Date.now() - start,
        environment: getAppEnvironment(),
        error: err instanceof Error ? err.message : String(err),
      };
    }
  }

  async transaction<T>(fn: () => Promise<T>): Promise<T> {
    return fn();
  }
}
