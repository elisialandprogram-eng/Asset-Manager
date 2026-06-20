export type { DatabaseProvider, DatabaseHealth } from "./DatabaseProvider";
export { DrizzleProvider } from "./DrizzleProvider";
export { SupabaseProvider } from "./SupabaseProvider";

import { getDatabaseConfig } from "../config/database";
import { DrizzleProvider } from "./DrizzleProvider";
import { SupabaseProvider } from "./SupabaseProvider";
import type { DatabaseProvider } from "./DatabaseProvider";

let _provider: DatabaseProvider | null = null;

export function getDatabaseProviderInstance(): DatabaseProvider {
  if (_provider) return _provider;
  const config = getDatabaseConfig();
  if (config.provider === "supabase") {
    _provider = new SupabaseProvider(config.databaseUrl);
  } else {
    _provider = new DrizzleProvider(config.databaseUrl);
  }
  return _provider;
}

export function resetDatabaseProvider(): void {
  _provider = null;
}
