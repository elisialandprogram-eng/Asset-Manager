export interface DatabaseHealth {
  provider: "drizzle" | "supabase";
  status: "ok" | "error";
  latencyMs: number;
  activeConnections?: number;
  environment: string;
  error?: string;
}

export interface DatabaseProvider {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  healthCheck(): Promise<DatabaseHealth>;
  transaction<T>(fn: () => Promise<T>): Promise<T>;
}
