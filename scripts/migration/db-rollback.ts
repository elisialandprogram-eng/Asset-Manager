#!/usr/bin/env tsx
import { execSync } from "child_process";
import { existsSync, readdirSync, writeFileSync } from "fs";
import { join } from "path";

const targetUrl = process.env.ROLLBACK_DB_URL ?? process.env.DATABASE_URL;
if (!targetUrl) { console.error("ERROR: ROLLBACK_DB_URL or DATABASE_URL not set."); process.exit(1); }

const backupDir = join(process.cwd(), "backups");
if (!existsSync(backupDir)) { console.error("ERROR: No backups/ directory. Nothing to rollback to."); process.exit(1); }

const exports = readdirSync(backupDir)
  .filter((f) => f.startsWith("ek_export_") && f.endsWith(".sql"))
  .sort()
  .reverse();

if (exports.length === 0) { console.error("ERROR: No export files in backups/. Cannot rollback."); process.exit(1); }

const latestExport = join(backupDir, exports[0]);
console.log("=== ROLLBACK PROCEDURE ===");
console.log(`Target DB: ${targetUrl.replace(/:[^:@]+@/, ":****@")}`);
console.log(`Restore from: ${latestExport}`);
console.log("This will DROP and RECREATE all tables from the backup.");
console.log("Press Ctrl+C within 10 seconds to abort...");

await new Promise((resolve) => setTimeout(resolve, 10000));

const timestamp = new Date().toISOString().replace(/[:.]/g, "-");

try {
  console.log("Step 1: Dropping existing public schema...");
  execSync(`psql "${targetUrl}" -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"`, { stdio: "inherit", shell: true });

  console.log("Step 2: Restoring from backup...");
  execSync(`psql "${targetUrl}" < "${latestExport}"`, { stdio: "inherit", shell: true });

  console.log("Step 3: Verifying restore...");
  const count = execSync(`psql "${targetUrl}" -t -c "SELECT COUNT(*) FROM users;"`, { shell: true }).toString().trim();
  console.log(`  users table: ${count} rows`);

  const report = { timestamp, target: targetUrl.replace(/:[^:@]+@/, ":****@"), restoredFrom: latestExport, status: "success" };
  writeFileSync(join(backupDir, `rollback_report_${timestamp}.json`), JSON.stringify(report, null, 2));
  console.log("Rollback complete.");
} catch (err) {
  console.error("Rollback failed:", err);
  process.exit(1);
}
