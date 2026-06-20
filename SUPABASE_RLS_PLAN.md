# Eternal Kingdoms — Supabase Row Level Security Plan

> **PLANNING DOCUMENT — RLS NOT YET ENABLED**
> This document defines the intended RLS policies for each table.
> RLS will be enabled in a future phase once the Supabase migration is stable and validated.
> The current architecture uses a backend-first approach: all game writes flow through the Express API server, which connects with the service role key (bypasses RLS).

---

## Architecture Approach: Backend-First

The Eternal Kingdoms API server is the single authoritative gatekeeper for all data mutations. The Supabase client in the frontend (portal / Unity) is used only for:
- Realtime subscriptions (read-only channels)
- Storage file uploads (scoped by bucket policy)
- Future: Direct auth token exchange

This means:
- All INSERT / UPDATE / DELETE flows through the Express API (service role — bypasses RLS).
- RLS policies are a defense-in-depth layer, not the primary security mechanism.
- Direct client database access is not supported in Phase 1.

---

## Table Policies

### `users`

```sql
-- Users can read their own row
CREATE POLICY "users_select_own" ON users
  FOR SELECT USING (auth.uid()::text = id::text);

-- Service role manages all inserts (registration)
-- No direct client insert allowed
```

**Notes:** User registration, login, and profile changes all go through `/api/auth/*`. No direct Supabase Auth used — JWT is custom (see `lib/jwt.ts`). Future migration path: adopt Supabase Auth as the JWT provider; current JWT secret maps to `SUPABASE_JWT_SECRET`.

---

### `kingdoms`

```sql
-- Kingdom owner can read their own kingdom
CREATE POLICY "kingdoms_select_own" ON kingdoms
  FOR SELECT USING (
    auth.uid()::text = (SELECT user_id::text FROM kingdoms WHERE id = kingdoms.id)
  );

-- Any authenticated user can read basic kingdom info (for world map)
CREATE POLICY "kingdoms_select_world" ON kingdoms
  FOR SELECT USING (auth.role() = 'authenticated');

-- Only service role modifies kingdoms
```

**Notes:** World map queries need to return all kingdoms (fog of war is client-side in current design). The "read all for world map" policy will be tightened to chunk-level in Phase 2.

---

### `buildings`

```sql
-- Kingdom owner can read their buildings
CREATE POLICY "buildings_select_own" ON buildings
  FOR SELECT USING (
    auth.uid()::text = (
      SELECT u.id::text FROM users u
      JOIN kingdoms k ON k.user_id = u.id
      WHERE k.id = buildings.kingdom_id
    )
  );
```

**Notes:** No direct client writes — all construction/upgrade flows through API.

---

### `resources`

```sql
-- Kingdom owner can read their resources
CREATE POLICY "resources_select_own" ON resources
  FOR SELECT USING (
    auth.uid()::text = (
      SELECT u.id::text FROM users u
      JOIN kingdoms k ON k.user_id = u.id
      WHERE k.id = resources.kingdom_id
    )
  );
```

**Notes:** Resources are updated by the server tick engine — no client writes ever.

---

### `alliances`

```sql
-- Any authenticated user can read alliance basic info
CREATE POLICY "alliances_select_public" ON alliances
  FOR SELECT USING (auth.role() = 'authenticated');

-- Alliance leader (R5) can update via service role only
```

**Notes:** Alliance CRUD is complex (hierarchy, permissions). All alliance actions go through API endpoints. RLS here is a read-only public layer.

---

### `worlds`

```sql
-- Public read — world info is not secret
CREATE POLICY "worlds_select_public" ON worlds
  FOR SELECT USING (true);
```

**Notes:** World info (name, seed, season) is public knowledge.

---

### `monsters` / `monster_spawns` / `crystal_nodes`

```sql
-- Public read — world map entities are visible to all players (subject to fog of war server-side)
CREATE POLICY "spawns_select_public" ON monster_spawns
  FOR SELECT USING (auth.role() = 'authenticated');

CREATE POLICY "crystals_select_public" ON crystal_nodes
  FOR SELECT USING (auth.role() = 'authenticated');
```

**Notes:** Fog of war is applied server-side in the API response — not via RLS. RLS here prevents unauthenticated scraping.

---

### `upgrade_queue` / `construction_queue`

```sql
-- Kingdom owner can read their own queues
CREATE POLICY "upgrade_queue_select_own" ON upgrade_queue
  FOR SELECT USING (
    auth.uid()::text = (
      SELECT u.id::text FROM users u
      JOIN kingdoms k ON k.user_id = u.id
      WHERE k.id = upgrade_queue.kingdom_id
    )
  );
```

---

### `asset_registry`

```sql
-- Public read — assets are public data
CREATE POLICY "assets_select_public" ON asset_registry
  FOR SELECT USING (true);
```

---

### Future Tables (battle_reports, messages)

```sql
-- battle_reports: Only participants can read their own reports
-- messages: Sender and recipient only
-- These tables do not exist yet — policies defined here for planning
```

---

## Enabling RLS: Step-by-Step

When ready to enable RLS (future phase):

1. **Enable RLS on each table:**
   ```sql
   ALTER TABLE users ENABLE ROW LEVEL SECURITY;
   ALTER TABLE kingdoms ENABLE ROW LEVEL SECURITY;
   -- ... all tables
   ```

2. **Apply policies** (copy from sections above).

3. **Verify service role bypass:** The API server connects with `SUPABASE_SERVICE_ROLE_KEY` — this automatically bypasses all RLS policies. No API behavior changes.

4. **Test direct client access** with an anon key and verify policies reject unauthorized access.

5. **Enable in staging first**, then production.

---

## Future Migration Path: Supabase Auth

The current auth system uses a custom JWT signed with `SESSION_SECRET`. Future migration to Supabase Auth:

1. Set `SUPABASE_JWT_SECRET = SESSION_SECRET` — Supabase will validate existing tokens.
2. Update registration to call `supabase.auth.admin.createUser()` in addition to inserting into `users` table.
3. Gradually migrate sessions to Supabase-issued JWTs.
4. Once all sessions use Supabase JWTs, `auth.uid()` will resolve correctly in RLS policies.

---

*RLS is NOT enabled. This document is planning only. Enable per the step-by-step procedure above when migration is stable.*
