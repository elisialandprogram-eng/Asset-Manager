#!/usr/bin/env tsx
import { execSync } from "child_process";
import { mkdirSync, writeFileSync } from "fs";
import { join } from "path";

const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
const backupDir = join(process.cwd(), "backups");
const backupFile = join(backupDir, `ek_export_${timestamp}.sql`);

mkdirSync(backupDir, { recursive: true });

const databaseUrl = process.env.DATABASE_URL;
if (!databaseUrl) {
  console.error("ERROR: DATABASE_URL not set.");
  process.exit(1);
}

console.log("Exporting database...");
console.log(`Output: ${backupFile}`);

try {
  execSync(`pg_dump "${databaseUrl}" --no-owner --no-acl > "${backupFile}"`, {
    stdio: "inherit",
    shell: true,
  });

  const report = {
    timestamp,
    databaseUrl: databaseUrl.replace(/:[^:@]+@/, ":****@"),
    backupFile,
    status: "success",
  };

  const reportFile = join(backupDir, `ek_export_report_${timestamp}.json`);
  writeFileSync(reportFile, JSON.stringify(report, null, 2));

  console.log("Export complete.");
  console.log(`Report: ${reportFile}`);
} catch (err) {
  console.error("Export failed:", err);
  process.exit(1);
}
