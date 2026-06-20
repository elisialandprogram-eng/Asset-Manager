# Eternal Kingdoms — Supabase Migration Plan

> This document plans the migration from the current self-managed PostgreSQL (via Drizzle ORM) to Supabase PostgreSQL.
> Migration execution is deferred — this is a planning document only.

---

## Current State

| Component | Current |
|-----------|---------|
| Database | PostgreSQL (self-managed, `DATABASE_URL` env var) |
| ORM | Drizzle ORM (`lib/db/`) |
| Connection | `drizzle-orm/node-postgres` (`pg` driver) |
| Auth | JWT (custom — `artifacts/api-server/src/lib/jwt.ts`) |
| Storage | No file storage currently |
| Realtime | Not yet implemented |

---

## Existing Schema Inventory

| Table | Rows (approx dev) | Notes |
|-------|------------------|-------|
| `users` | ~5 | dev account + test accounts |
| `kingdoms` | ~5 | one per user |
| `buildings` | ~55 | up to 11 per kingdom |
| `resources` | ~5 | one per kingdom |
| `troops` | 0 | schema exists, no data yet |
| `research` | 0 | schema exists, no data yet |
| `worlds` | 1 | Aethoria (id=1, seed=42937) |
| `map_tiles` | 0 | unused — terrain is procedural |
| `monsters` | 5 | seeded monster types |
| `monster_spawns` | 28 | seeded spawn positions |
| `crystal_nodes` | 15 | seeded node positions |
| `alliances` | 0 | schema exists, no data yet |
| `asset_registry` | 33 | SVG placeholder asset entries |
| `upgrade_queue` | varies | in-progress upgrades |
| `construction_queue` | varies | in-progress constructions |

---

## Repository Abstraction Review

The codebase uses a repository pattern (`lib/db/src/repositories/`) that fully abstracts the database layer. No raw SQL in routes or game engine — all access goes through repository functions.

**This is the key enabler for a clean migration.** Swapping the Drizzle connection config is the primary required change.

| Repository | Complexity to Migrate |
|------------|----------------------|
| `kingdomRepository` | Low — standard CRUD |
| `resourceRepository` | Low — standard CRUD + atomic deduct |
| `buildingRepository` | Low — standard CRUD |
| `upgradeQueueRepository` | Low — standard CRUD |
| `constructionRepository` | Low — standard CRUD |
| `worldRepository` | Low — standard CRUD |
| `userRepository` | Medium — password hashing |
| `monsterRepository` | Low — standard CRUD |
| `monsterSpawnRepository` | Low — standard CRUD |
| `crystalNodeRepository` | Low — standard CRUD |

---

## Migration Sequence

### Step 1 — Provision Supabase Project

1. Create a Supabase project at [supabase.com](https://supabase.com).
2. Note the project URL and service role key.
3. Set environment variables:
   - `DATABASE_URL` → Supabase PostgreSQL connection string (pooler or direct)
   - `SUPABASE_URL` → `https://<project>.supabase.co`
   - `SUPABASE_ANON_KEY` → public anon key
   - `SUPABASE_SERVICE_ROLE_KEY` → service role key (backend only — never expose)

> `SUPABASE_URL` and `SUPABASE_ANON_KEY` are already reserved in `artifacts/api-server/src/index.ts`.

### Step 2 — Push Schema to Supabase

Run Drizzle's push against the Supabase `DATABASE_URL`:

```bash
pnpm --filter @workspace/db run push
```

This creates all 15 tables in Supabase's PostgreSQL instance.

Verify in Supabase Table Editor that all tables created correctly.

### Step 3 — Migrate Existing Data (Dev → Supabase)

For development environments, re-run the world seeder — it is idempotent:

```bash
# Start API server pointed at Supabase DATABASE_URL
# worldSeeder.ts runs automatically on startup and creates all seed data
```

For production environments (if any player data exists), use `pg_dump` / `pg_restore`:

```bash
pg_dump $OLD_DATABASE_URL | psql $SUPABASE_DATABASE_URL
```

### Step 4 — Update Drizzle Connection Config

Change `lib/db/src/connection.ts` from `node-postgres` to Supabase-compatible connection:

```typescript
// Current
import { drizzle } from 'drizzle-orm/node-postgres';
import { Pool } from 'pg';
const pool = new Pool({ connectionString: process.env.DATABASE_URL });

// After migration — no change needed if using Supabase's connection string
// Supabase provides a standard PostgreSQL connection string compatible with node-postgres
// Optionally switch to Supabase JS client for Realtime features:
import { createClient } from '@supabase/supabase-js';
const supabase = createClient(process.env.SUPABASE_URL, process.env.SUPABASE_SERVICE_ROLE_KEY);
```

The Drizzle ORM layer remains unchanged. Only the connection string changes.

### Step 5 — Enable Row Level Security (RLS)

Enable RLS on sensitive tables. Initial policies:

| Table | Policy |
|-------|--------|
| `users` | Users can only read their own row |
| `kingdoms` | Users can only read/write their own kingdom |
| `buildings` | Users can only read/write buildings in their kingdom |
| `resources` | Users can only read resources in their kingdom |
| `upgrade_queue` | Users can only read their own queue |
| `construction_queue` | Users can only read their own queue |
| `worlds` | Read-only for all authenticated users |
| `monsters` | Read-only for all authenticated users |
| `monster_spawns` | Read-only for all authenticated users |
| `crystal_nodes` | Read-only for all authenticated users |
| `asset_registry` | Read-only for all |

> Note: RLS bypassed by service role key — the Express API uses service role for all writes.

### Step 6 — Supabase Realtime (Phase 8)

When Socket.IO (Phase 8) is implemented, optionally replace polling with Supabase Realtime channels:

```typescript
supabase.channel('resource-updates')
  .on('postgres_changes', { event: 'UPDATE', schema: 'public', table: 'resources' }, (payload) => {
    // push to connected Unity client via WebSocket
  })
  .subscribe();
```

### Step 7 — Storage Migration

Currently no file storage is used (all game art is SVG placeholders stored in `assets/`).

When production art assets are created, they will be stored in **Supabase Storage**:

| Bucket | Contents | Access |
|--------|----------|--------|
| `game-assets` | Building, troop, monster images | Public |
| `user-avatars` | Player profile pictures | Private (RLS) |
| `nft-metadata` | NFT metadata JSONs | Public |

Asset registry IDs will map to Supabase Storage URLs:
```
https://<project>.supabase.co/storage/v1/object/public/game-assets/building_palace_001.webp
```

---

## Environment Variables

| Variable | Purpose | When Needed |
|----------|---------|-------------|
| `DATABASE_URL` | Drizzle ORM connection (Supabase pooler URL) | Immediately on migration |
| `SUPABASE_URL` | Supabase project URL | Phase 8 (Realtime) |
| `SUPABASE_ANON_KEY` | Client-side public key | Phase 8 |
| `SUPABASE_SERVICE_ROLE_KEY` | Backend full-access key | Phase 8 / Storage |
| `SESSION_SECRET` | JWT signing secret (unchanged) | Already in use |

---

## Rollback Strategy

If the Supabase migration causes issues:

1. Revert `DATABASE_URL` env var to the original PostgreSQL connection string.
2. The Drizzle ORM layer is unchanged — no code rollback needed.
3. The old database retains all data (no destructive operations on the source).

---

## Migration Risks

| Risk | Mitigation |
|------|-----------|
| Connection string format differences | Supabase provides standard PostgreSQL URL — compatible |
| RLS blocking backend writes | Use service role key for all server-side writes |
| Supabase connection pooling limits | Use Supabase connection pooler (PgBouncer) URL for API |
| Data loss during migration | Never drop the original DB until Supabase is confirmed stable |
| `pg_dump` schema conflicts (types, sequences) | Test with a staging Supabase project first |
