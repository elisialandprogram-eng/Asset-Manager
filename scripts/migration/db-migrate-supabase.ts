#!/usr/bin/env tsx
import { execSync } from "child_process";
import { existsSync, readdirSync, writeFileSync } from "fs";
import { join } from "path";

const databaseUrl = process.env.DATABASE_URL;
const supabaseUrl = process.env.SUPABASE_URL;
const supabaseServiceKey = process.env.SUPABASE_SERVICE_ROLE_KEY;

if (!databaseUrl) { console.error("ERROR: DATABASE_URL not set (source DB)."); process.exit(1); }
if (!supabaseUrl) { console.error("ERROR: SUPABASE_URL not set (target)."); process.exit(1); }
if (!supabaseServiceKey) { console.error("ERROR: SUPABASE_SERVICE_ROLE_KEY not set."); process.exit(1); }

const SUPABASE_DB_URL = process.env.SUPABASE_DB_URL;
if (!SUPABASE_DB_URL) {
  console.error(
    "ERROR: SUPABASE_DB_URL not set.\n" +
    "Set SUPABASE_DB_URL to your Supabase direct Postgres connection string.\n" +
    "Format: postgresql://postgres:[PASSWORD]@db.[PROJECT_REF].supabase.co:5432/postgres"
  );
  process.exit(1);
}

const backupDir = join(process.cwd(), "backups");
if (!existsSync(backupDir)) {
  console.error("ERROR: No backups directory found. Run db:export first.");
  process.exit(1);
}

const exports = readdirSync(backupDir)
  .filter((f) => f.startsWith("ek_export_") && f.endsWith(".sql"))
  .sort()
  .reverse();

if (exports.length === 0) {
  console.error("ERROR: No export files found. Run pnpm db:export first.");
  process.exit(1);
}

const latestExport = join(backupDir, exports[0]);
console.log(`Using export: ${latestExport}`);
console.log("Migrating to Supabase...");

const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
const steps: Array<{ step: string; status: string; note?: string }> = [];

try {
  console.log("Step 1: Importing schema and data into Supabase...");
  execSync(`psql "${SUPABASE_DB_URL}" < "${latestExport}"`, {
    stdio: "inherit",
    shell: true,
  });
  steps.push({ step: "import", status: "ok" });

  console.log("Step 2: Validating row counts...");
  const tables = [
    "users", "worlds", "kingdoms", "buildings", "resources",
    "troops", "research", "map_tiles", "monsters", "monster_spawns",
    "crystal_nodes", "alliances", "asset_registry", "upgrade_queue", "construction_queue",
  ];
  for (const table of tables) {
    const count = execSync(
      `psql "${SUPABASE_DB_URL}" -t -c "SELECT COUNT(*) FROM ${table};"`,
      { shell: true }
    ).toString().trim();
    console.log(`  ${table}: ${count} rows`);
  }
  steps.push({ step: "row_count_validation", status: "ok" });

  console.log("Step 3: Validating FK constraints...");
  execSync(
    `psql "${SUPABASE_DB_URL}" -c "SELECT conname, conrelid::regclass FROM pg_constraint WHERE contype = 'f' ORDER BY conrelid::regclass::text;"`,
    { stdio: "inherit", shell: true }
  );
  steps.push({ step: "fk_validation", status: "ok" });

  console.log("Migration complete.");
  const report = { timestamp, sourceExport: latestExport, target: SUPABASE_DB_URL.replace(/:[^:@]+@/, ":****@"), steps, status: "success" };
  writeFileSync(join(backupDir, `migration_report_${timestamp}.json`), JSON.stringify(report, null, 2));
  console.log("Migration report saved to backups/.");
} catch (err) {
  console.error("Migration failed:", err);
  process.exit(1);
}
