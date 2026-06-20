export type DatabaseProvider = "drizzle" | "supabase";
export type AppEnvironment = "development" | "staging" | "production";

export interface DatabaseConfig {
  provider: DatabaseProvider;
  environment: AppEnvironment;
  databaseUrl: string;
  supabaseUrl?: string;
  supabaseAnonKey?: string;
  supabaseServiceRoleKey?: string;
  supabaseJwtSecret?: string;
}

export function getAppEnvironment(): AppEnvironment {
  const env = process.env.APP_ENV ?? process.env.NODE_ENV ?? "development";
  if (env === "production") return "production";
  if (env === "staging") return "staging";
  return "development";
}

export function getDatabaseProvider(): DatabaseProvider {
  const supabaseUrl = process.env.SUPABASE_URL;
  const supabaseKey = process.env.SUPABASE_SERVICE_ROLE_KEY;
  const appEnv = getAppEnvironment();
  if (appEnv !== "development" && supabaseUrl && supabaseKey) {
    return "supabase";
  }
  if (supabaseUrl && supabaseKey && process.env.USE_SUPABASE === "true") {
    return "supabase";
  }
  return "drizzle";
}

export function getDatabaseConfig(): DatabaseConfig {
  const databaseUrl = process.env.DATABASE_URL;
  if (!databaseUrl) {
    throw new Error("DATABASE_URL must be set.");
  }
  return {
    provider: getDatabaseProvider(),
    environment: getAppEnvironment(),
    databaseUrl,
    supabaseUrl: process.env.SUPABASE_URL,
    supabaseAnonKey: process.env.SUPABASE_ANON_KEY,
    supabaseServiceRoleKey: process.env.SUPABASE_SERVICE_ROLE_KEY,
    supabaseJwtSecret: process.env.SUPABASE_JWT_SECRET,
  };
}
