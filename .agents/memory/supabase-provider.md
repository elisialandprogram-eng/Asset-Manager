---
name: Supabase provider pattern
description: Rules for the Supabase database provider infrastructure in lib/db/src/providers/
---

## Rule
All DB-related files must live inside `lib/db/src/` because `lib/db/tsconfig.json` has `rootDir: "src"`. Any file outside that boundary causes TS6059 / TS6305 errors.

**Why:** TypeScript composite project with strict rootDir enforcement. Files outside rootDir cannot be included in the build.

**How to apply:** When adding new config or utility files that `@workspace/db` needs, place them in `lib/db/src/<subdir>/` — not in `lib/config/` or any other top-level directory.

## Provider selection
- `USE_SUPABASE=true` + all `SUPABASE_*` vars present → `SupabaseProvider` (Postgres over SSL to Supabase)
- Otherwise → `DrizzleProvider` (local pg Pool, default for dev)
- Factory lives in `lib/db/src/providers/index.ts` — `getDatabaseProviderInstance()`

## SDK install rule
`@supabase/supabase-js` must be installed in **both**:
- `@workspace/db` — for Supabase clients + admin client
- `@workspace/api-server` — for `RealtimeService.ts` which imports `SupabaseClient` / `RealtimeChannel` types

Missing it from api-server causes TS2307 "Cannot find module '@supabase/supabase-js'".

## Migration scripts
In `scripts/migration/` — run via `tsx`, not compiled. Available as:
- `pnpm --filter @workspace/scripts run db:export`
- `pnpm --filter @workspace/scripts run db:migrate:supabase`
- `pnpm --filter @workspace/scripts run db:verify`
- `pnpm --filter @workspace/scripts run db:rollback`

## Health endpoint
`GET /api/health/database` — returns `{ provider, status, latencyMs, activeConnections, environment }`. Provider is "drizzle" or "supabase".
