# Eternal Kingdoms

A browser-based MMO kingdom strategy game with a semi-realistic medieval fantasy style. Players own kingdoms, gather resources, train armies, research technologies, fight monsters, join alliances, and participate in large-scale PvP warfare.

## Run & Operate

- `pnpm --filter @workspace/api-server run dev` — run the API server (port 5000)
- `pnpm --filter @workspace/eternal-kingdoms run dev` — run the frontend (port auto)
- `pnpm run typecheck` — full typecheck across all packages
- `pnpm run build` — typecheck + build all packages
- `pnpm --filter @workspace/api-spec run codegen` — regenerate API hooks and Zod schemas from the OpenAPI spec
- `pnpm --filter @workspace/db run push` — push DB schema changes (dev only)
- Required env: `DATABASE_URL` — Postgres connection string
- Required env: `SESSION_SECRET` — JWT signing secret

## Stack

- pnpm workspaces, Node.js 24, TypeScript 5.9
- Frontend: React + Vite, Tailwind CSS, Framer Motion, Wouter router
- API: Express 5
- DB: PostgreSQL + Drizzle ORM
- Validation: Zod (`zod/v4`), `drizzle-zod`
- Auth: JWT (stateless, stored in localStorage as `ek_token`)
- API codegen: Orval (from OpenAPI spec)
- Build: esbuild (CJS bundle)

## Where things live

- `lib/api-spec/openapi.yaml` — OpenAPI spec (source of truth for all API contracts)
- `lib/db/src/schema/` — Drizzle ORM table definitions (13 entity schemas)
- `artifacts/eternal-kingdoms/src/` — React frontend (Login, Register, Dashboard)
- `artifacts/api-server/src/routes/` — Express route handlers
- `artifacts/api-server/src/lib/jwt.ts` — JWT token creation/verification
- `assets/` — SVG placeholder assets organized by category
- `PROJECT_MASTER.md` — Full project governance (source of truth)
- `ROADMAP.md` — Feature roadmap by phase
- `ARCHITECTURE_STATE.md` — Component-level status tracker

## Architecture decisions

- **OpenAPI-first** — All types generated from spec. Never hand-write types that codegen already produces.
- **Asset Registry as NFT bridge** — Every visual object has a unique ID (e.g. `building_palace_001`). Future NFTs connect through these IDs via `nft_contract_address` / `nft_token_id` fields.
- **World-scoped data** — Every kingdom, tile, monster, and crystal node belongs to a world. Schema supports 20+ concurrent worlds.
- **JWT auth with `SESSION_SECRET`** — Stateless tokens signed with HMAC-SHA256. Token stored in localStorage as `ek_token`, passed as Bearer header on all API requests.
- **Placeholder-first assets** — All SVGs live in `assets/` with canonical registry IDs. Swappable without code changes.

## Product

Phase 0 complete — foundation is in place:
- Login and Register pages with kingdom creation
- Dashboard / War Room showing kingdom info, resources, buildings
- Full PostgreSQL schema for 13 game entities
- Asset Registry with 18 placeholder SVG assets
- JWT authentication
- Governance docs (PROJECT_MASTER.md, ROADMAP.md, ARCHITECTURE_STATE.md)

## User preferences

_Populate as you build — explicit user instructions worth remembering across sessions._

## Gotchas

- Always run `pnpm --filter @workspace/api-spec run codegen` after changing `openapi.yaml`
- Always run `pnpm --filter @workspace/db run push` after changing Drizzle schemas
- Asset registry IDs are permanent — never rename them once assigned
- `drizzle-kit push` uses `DATABASE_URL` from environment secrets
- JWT secret lives in `SESSION_SECRET` env var (already provisioned)

## Pointers

- See `PROJECT_MASTER.md` for full architecture governance
- See `ROADMAP.md` for the 10-phase feature roadmap
- See `ARCHITECTURE_STATE.md` for component-level status
- See the `pnpm-workspace` skill for workspace structure, TypeScript setup, and package details
