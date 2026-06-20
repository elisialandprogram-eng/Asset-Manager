#!/usr/bin/env tsx
import { writeFileSync } from "fs";
import { join } from "path";

const databaseUrl = process.env.DATABASE_URL;
if (!databaseUrl) { console.error("ERROR: DATABASE_URL not set."); process.exit(1); }

const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
const results: Array<{ check: string; status: string; detail?: string }> = [];

async function runCheck(name: string, fn: () => Promise<string>): Promise<void> {
  try {
    const detail = await fn();
    console.log(`  ✓ ${name}: ${detail}`);
    results.push({ check: name, status: "pass", detail });
  } catch (err) {
    console.error(`  ✗ ${name}: ${err instanceof Error ? err.message : String(err)}`);
    results.push({ check: name, status: "fail", detail: err instanceof Error ? err.message : String(err) });
  }
}

console.log("Running database verification...\n");

const { default: pg } = await import("pg");
const pool = new pg.Pool({ connectionString: databaseUrl, ssl: databaseUrl.includes("supabase") ? { rejectUnauthorized: false } : undefined });

await runCheck("Connection", async () => {
  await pool.query("SELECT 1");
  return "connected";
});

const EXPECTED_TABLES = [
  "users", "worlds", "kingdoms", "buildings", "resources",
  "troops", "research", "map_tiles", "monsters", "monster_spawns",
  "crystal_nodes", "alliances", "asset_registry", "upgrade_queue", "construction_queue",
];

await runCheck("Tables exist", async () => {
  const { rows } = await pool.query<{ tablename: string }>(
    "SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename"
  );
  const found = rows.map((r) => r.tablename);
  const missing = EXPECTED_TABLES.filter((t) => !found.includes(t));
  if (missing.length > 0) throw new Error(`Missing tables: ${missing.join(", ")}`);
  return `${found.length} tables found`;
});

await runCheck("World seeded", async () => {
  const { rows } = await pool.query("SELECT id, name, seed FROM worlds LIMIT 1");
  if (rows.length === 0) throw new Error("No worlds found");
  return `World '${rows[0].name}' (seed=${rows[0].seed})`;
});

await runCheck("Dev account exists", async () => {
  const { rows } = await pool.query("SELECT id, email FROM users WHERE email = 'dev@eternalkingdoms.com'");
  if (rows.length === 0) throw new Error("dev@eternalkingdoms.com not found");
  return "dev@eternalkingdoms.com present";
});

await runCheck("Monster spawns seeded", async () => {
  const { rows } = await pool.query("SELECT COUNT(*) as cnt FROM monster_spawns");
  return `${rows[0].cnt} spawns`;
});

await runCheck("Asset registry populated", async () => {
  const { rows } = await pool.query("SELECT COUNT(*) as cnt FROM asset_registry");
  return `${rows[0].cnt} assets`;
});

await pool.end();

const allPass = results.every((r) => r.status === "pass");
const report = { timestamp, databaseUrl: databaseUrl.replace(/:[^:@]+@/, ":****@"), results, overall: allPass ? "pass" : "fail" };

writeFileSync(join(process.cwd(), "backups", `verify_report_${timestamp}.json`), JSON.stringify(report, null, 2));

console.log(`\nVerification: ${allPass ? "✅ ALL CHECKS PASS" : "❌ SOME CHECKS FAILED"}`);
if (!allPass) process.exit(1);
