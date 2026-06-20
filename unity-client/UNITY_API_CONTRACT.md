# Unity Client — API Contract

> Full audit of all existing backend API endpoints.
> Documents endpoint, method, request schema, response schema, auth requirement, and intended Unity usage.
> Missing APIs are identified — implementation deferred to their respective feature phases.

---

## Authentication

All protected endpoints require:
```
Authorization: Bearer <jwt_token>
```

JWT is issued by `POST /api/auth/login` and stored by the Unity `AuthManager`.

---

## Endpoints

### Health Check

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/healthz` |
| Auth | None |
| Request | — |
| Response | `{ "status": "ok" }` |
| Unity Usage | Connection check on startup, reconnect polling |

---

### Auth — Register

| Field | Value |
|-------|-------|
| Method | `POST` |
| Path | `/api/auth/register` |
| Auth | None |
| Request | `{ username, email, password, kingdomName }` |
| Response | `{ token, user: { id, username, email, role }, kingdom: { id, name, worldId, mapX, mapY } }` |
| Unity Usage | Registration flow in `Login.unity` |

---

### Auth — Login

| Field | Value |
|-------|-------|
| Method | `POST` |
| Path | `/api/auth/login` |
| Auth | None |
| Request | `{ email, password }` |
| Response | `{ token, user: { id, username, email, role } }` |
| Unity Usage | `AuthManager.Login()` — stores token in PlayerPrefs |

---

### Auth — Current User

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/auth/me` |
| Auth | Required |
| Request | — |
| Response | `{ id, username, email, role, worldId }` |
| Unity Usage | `AuthManager.ValidateToken()` — called on scene boot to confirm token still valid |

---

### Kingdoms — My Kingdom

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/mine` |
| Auth | Required |
| Request | — |
| Response | `{ id, name, worldId, mapX, mapY, power, allianceId }` |
| Unity Usage | `KingdomManager` — get own kingdom ID for subsequent requests |

---

### Kingdoms — Get by ID

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/:id` |
| Auth | Required |
| Request | — |
| Response | `{ id, name, worldId, mapX, mapY, power, allianceId }` |
| Unity Usage | World map — load enemy kingdom info on click |

---

### Kingdoms — Full State

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/:id/state` |
| Auth | Required |
| Request | — |
| Response | `{ kingdom, buildings[], resources, productionRates, resourceCaps, power, palaceTier, upgradeQueue[] }` |
| Unity Usage | `KingdomManager.LoadState()` — primary data source for kingdom scene; polled every 8s |

---

### Kingdoms — Buildings

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/:id/buildings` |
| Auth | Required |
| Request | — |
| Response | `Building[]` — `{ id, buildingType, level, assetId, isConstructing, constructionEndsAt }` |
| Unity Usage | Supplemental building list; prefer `/state` for full context |

---

### Kingdoms — Resources

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/:id/resources` |
| Auth | Required |
| Request | — |
| Response | `{ food, wood, stone, iron, gold, updatedAt }` |
| Unity Usage | Resource HUD quick-refresh |

---

### Kingdoms — Upgrade Queue

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/:id/queue` |
| Auth | Required |
| Request | — |
| Response | `UpgradeQueueItem[]` — `{ id, buildingId, fromLevel, toLevel, endsAt, status }` |
| Unity Usage | Activity feed, upgrade timer overlays on building slots |

---

### Kingdoms — Construction Queue

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/:id/construction-queue` |
| Auth | Required |
| Request | — |
| Response | `ConstructionQueueItem[]` — `{ id, buildingId, buildingType, endsAt, status }` |
| Unity Usage | Construction timer overlays on empty building slots |

---

### Kingdoms — Construction Options

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/kingdoms/:id/construction-options` |
| Auth | Required |
| Request | — |
| Response | `ConstructionOption[]` — `{ buildingType, cost, duration, canAfford, slotsAvailable }` |
| Unity Usage | Populate "Construct" dialog with available building types + costs |

---

### Kingdoms — Construct Building

| Field | Value |
|-------|-------|
| Method | `POST` |
| Path | `/api/kingdoms/:id/construct` |
| Auth | Required |
| Request | `{ buildingType: string }` |
| Response | `{ building, constructionQueue }` |
| Unity Usage | Player confirms build → deducts resources → starts timer |

---

### Buildings — Upgrade

| Field | Value |
|-------|-------|
| Method | `POST` |
| Path | `/api/buildings/:id/upgrade` |
| Auth | Required |
| Request | — |
| Response | `{ upgradeQueue }` |
| Unity Usage | Player clicks "Upgrade" on a building slot and confirms |

---

### Buildings — Upgrade Preview

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/buildings/:id/upgrade-preview` |
| Auth | Required |
| Request | — |
| Response | `{ fromLevel, toLevel, cost, duration, canAfford }` |
| Unity Usage | Show cost/time preview in upgrade dialog before confirmation |

---

### Assets — List

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/assets` |
| Auth | None |
| Request | — |
| Response | `AssetRegistryEntry[]` — `{ assetId, category, name, nftContractAddress, nftTokenId }` |
| Unity Usage | `AssetManager` — map assetId to Addressable bundle key on startup |

---

### Assets — Get by ID

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/assets/:assetId` |
| Auth | None |
| Request | — |
| Response | `AssetRegistryEntry` |
| Unity Usage | On-demand asset metadata lookup |

---

### Worlds — List

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/worlds` |
| Auth | None |
| Request | — |
| Response | `World[]` — `{ id, name, status, maxKingdoms, season, seed }` |
| Unity Usage | World selection screen (future multi-world support) |

---

### Worlds — Full Map

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/worlds/:id/map` |
| Auth | Required |
| Request | — |
| Response | `{ world, kingdoms: KingdomMapEntry[], spawns: WorldSpawn[], crystals: CrystalNode[] }` |
| Unity Usage | `WorldManager` — initial world load + 60s refresh |

---

### Worlds — Kingdoms

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/worlds/:id/kingdoms` |
| Auth | Required |
| Request | — |
| Response | `KingdomMapEntry[]` — `{ id, name, mapX, mapY, power, isActive }` |
| Unity Usage | Kingdom entity placement on world map |

---

### Worlds — Spawns

| Field | Value |
|-------|-------|
| Method | `GET` |
| Path | `/api/worlds/:id/spawns` |
| Auth | Required |
| Request | — |
| Response | `WorldSpawn[]` — `{ id, spawnType, spawnSubtype, x, y, monster?, crystal? }` |
| Unity Usage | Monster and crystal entity placement on world map |

---

### Worlds — Place Kingdom (DEV ONLY)

| Field | Value |
|-------|-------|
| Method | `POST` |
| Path | `/api/worlds/:id/place-kingdom` |
| Auth | Required (admin role) |
| Request | `{ kingdomId, x, y }` |
| Response | Updated kingdom |
| Unity Usage | Dev tooling only — not used in production client |

---

## Missing APIs (Identified — Not Yet Implemented)

| Feature | Endpoint Needed | Phase |
|---------|----------------|-------|
| Troop training | `POST /api/kingdoms/:id/train` | Phase 3 |
| Troop list | `GET /api/kingdoms/:id/troops` | Phase 3 |
| Combat — attack | `POST /api/combat/attack` | Phase 7 |
| Combat — scout | `POST /api/combat/scout` | Phase 7 |
| Battle reports | `GET /api/kingdoms/:id/battles` | Phase 7 |
| Research — start | `POST /api/kingdoms/:id/research` | Phase 4 |
| Research — state | `GET /api/kingdoms/:id/research` | Phase 4 |
| Alliance — create | `POST /api/alliances` | Phase 6 |
| Alliance — join | `POST /api/alliances/:id/join` | Phase 6 |
| Alliance — members | `GET /api/alliances/:id/members` | Phase 6 |
| Socket.IO events | WebSocket on same port | Phase 8 |
| Kingdom march | `POST /api/kingdoms/:id/march` | Phase 7 |
| Crystal harvest | `POST /api/crystals/:id/harvest` | Phase 5 |
