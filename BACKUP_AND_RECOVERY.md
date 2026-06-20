# Eternal Kingdoms — Backup and Recovery Strategy

> **OPERATIONAL DOCUMENT**
> Version 1.0 — June 2026

---

## 1. Backup Architecture

### Provider-Specific Backup Layers

| Layer | Provider | Frequency | Retention | Managed By |
|-------|----------|-----------|-----------|-----------|
| Supabase automatic backup | Supabase Pro | Daily | 7 days | Supabase |
| Supabase PITR (Point-in-Time Recovery) | Supabase Pro+ | Continuous | Up to 30 days | Supabase |
| Application-level export | pg_dump | Daily (via cron) | 30 days | EK DevOps |
| Pre-migration snapshot | pg_dump | Before any migration | Permanent | EK DevOps |
| Pre-deployment snapshot | pg_dump | Before each deploy | 14 days | EK DevOps |

---

## 2. Daily Backup Procedure

### Automated Daily Backup (Application Level)

Run via cron at 02:00 UTC daily:

```bash
# Cron entry (on deployment server or CI runner)
0 2 * * * cd /app && pnpm db:export >> /var/log/ek-backup.log 2>&1
```

This runs `scripts/migration/db-export.ts` which:
1. Calls `pg_dump` against `DATABASE_URL`
2. Saves output to `backups/ek_export_TIMESTAMP.sql`
3. Saves a JSON report to `backups/ek_export_report_TIMESTAMP.json`

### Backup Rotation

Keep the last 30 daily exports. Older files are deleted by the cleanup job:

```bash
# Cleanup exports older than 30 days
find /app/backups -name "ek_export_*.sql" -mtime +30 -delete
find /app/backups -name "ek_export_report_*.json" -mtime +30 -delete
```

---

## 3. Weekly Snapshot Procedure

Every Sunday at 01:00 UTC, a "weekly snapshot" is tagged and uploaded to cold storage:

```bash
# Weekly snapshot cron
0 1 * * 0 cd /app && pnpm db:export && cp backups/$(ls backups/*.sql | sort | tail -1) /cold-storage/weekly/ek_week_$(date +%Y%W).sql
```

Weekly snapshots are retained for 12 weeks (3 months).

---

## 4. Supabase PITR (Point-in-Time Recovery)

On Supabase Pro+, PITR allows recovery to any second within the retention window.

**To use PITR via Supabase Dashboard:**
1. Navigate to: Project Settings → Database → Point-in-Time Recovery
2. Select the recovery timestamp
3. Click "Restore" — Supabase creates a new project instance from that point
4. Update `DATABASE_URL` and `SUPABASE_URL` in environment secrets to point to restored instance
5. Run `pnpm db:verify` to confirm data integrity

**PITR is the recommended recovery path for:**
- Accidental data deletion
- Corrupted data from a bad deploy
- Recovery from a security incident

---

## 5. Restore Procedure (Application-Level Export)

Use when restoring from an application-level `pg_dump` export:

### Step 1: Identify the target backup

```bash
ls backups/*.sql | sort | tail -10
```

### Step 2: Set rollback target

```bash
export ROLLBACK_DB_URL="postgresql://..."  # Target DB connection string
```

### Step 3: Run rollback script

```bash
pnpm db:rollback
```

This script:
1. Waits 10 seconds (abort window with Ctrl+C)
2. Drops and recreates the public schema
3. Restores from the most recent export
4. Verifies the `users` table row count
5. Saves a rollback report to `backups/`

### Step 4: Verify

```bash
pnpm db:verify
```

All checks must pass before declaring recovery complete.

---

## 6. Rollback Procedure (Post-Deploy)

If a deployment introduces a broken migration:

1. **Stop the API server** (to prevent further writes to the broken schema).
2. **Identify the pre-deploy snapshot** (named with the deploy timestamp).
3. **Run rollback:**
   ```bash
   ROLLBACK_DB_URL=$DATABASE_URL pnpm db:rollback
   ```
4. **Redeploy the previous version** of the application code.
5. **Run `pnpm db:verify`** to confirm.
6. **Restart the API server.**

---

## 7. Disaster Recovery

### Scenario: Complete Database Loss

1. **Supabase project destroyed:** Create a new Supabase project. Restore from the latest weekly cold storage snapshot using `pnpm db:rollback`.
2. **Supabase account compromised:** Rotate `SUPABASE_SERVICE_ROLE_KEY` and `SUPABASE_JWT_SECRET` immediately. Revoke all active sessions. Run `pnpm db:verify` on restored instance.
3. **Application server destroyed:** Application is stateless (all state in DB). Redeploy to a new server. Point `DATABASE_URL` to the unchanged Supabase instance. Application is immediately operational.

### RTO / RPO Targets

| Scenario | RTO (Recovery Time) | RPO (Data Loss) |
|---------|--------------------|-----------------|
| Single table corruption | < 1 hour (PITR) | 0 seconds (PITR) |
| Full DB corruption | < 2 hours | ≤ 24 hours (daily backup) |
| Accidental data deletion | < 30 minutes (PITR) | 0 seconds |
| Server failure | < 15 minutes (stateless redeploy) | 0 seconds |
| Complete account loss | < 4 hours | ≤ 7 days (cold storage) |

---

## 8. Backup Verification

Monthly backup drill (first Monday of each month):

1. Export current database: `pnpm db:export`
2. Restore to a staging instance: `ROLLBACK_DB_URL=$STAGING_URL pnpm db:rollback`
3. Run full verification: `DATABASE_URL=$STAGING_URL pnpm db:verify`
4. Confirm all checks pass
5. Log drill results in `backups/drill_report_MONTH.json`

---

## 9. Environment Secret Backup

All environment secrets (DATABASE_URL, SESSION_SECRET, SUPABASE_* keys) are:
- Stored in Replit Secrets (development)
- Stored in deployment platform secrets (production)
- **Never committed to the repository**
- Documented (without values) in `replit.md` under "Required env"

In the event of secret loss:
- `SESSION_SECRET`: Generate a new one. **All existing JWT tokens are invalidated** — users must re-login.
- `DATABASE_URL`: Retrieve from Supabase dashboard → Settings → Database → Connection string.
- `SUPABASE_SERVICE_ROLE_KEY`: Retrieve from Supabase dashboard → Settings → API.
- `SUPABASE_ANON_KEY`: Same as above.

---

*This document defines the operational backup and recovery strategy for Eternal Kingdoms. Update whenever backup frequency, retention, or procedure changes.*
