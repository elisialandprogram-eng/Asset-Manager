---
name: Game Engine Architecture
description: Where game logic lives and the boundary between pure computation and DB access
---

All game logic (formulas, balance, production, upgrade costs, palace tiers) lives in `lib/game-engine/` — pure TypeScript with no DB or Express imports.

The `api-server/src/engine/` layer owns the DB read/write and the 60-second setInterval tick.

**Why:** Keeps game balance testable and importable without DB overhead. All future balancing changes go in `gameBalance.ts`.

**How to apply:** Never put game math in route handlers or DB queries. Route handlers call engine functions and then do DB writes.

Key constants: tick=60s, max building level=20, upgrade cost scale=1.5x/level, upgrade time scale=1.3x/level.
Palace tiers: L1-4=tier1 (2 resource bldgs), L5-9=tier2 (+barracks), L10-14=tier3 (+militia), L15-19=tier4 (+spearman/archer), L20=tier5 (+scout/research).
