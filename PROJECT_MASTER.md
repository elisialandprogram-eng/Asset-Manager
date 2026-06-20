# Eternal Kingdoms — Project Master

> **Single source of truth for architecture, decisions, and progress.**
> Every future task MUST update this file before closing.

---

## Game Vision

Eternal Kingdoms is a browser-based MMO kingdom strategy game. Players own kingdoms, gather resources, train armies, research technologies, fight monsters, join alliances, explore massive worlds, and participate in large-scale PvP warfare. The game has a semi-realistic medieval fantasy style.

**NOT:**
- Cartoon or chibi art
- Card-based gameplay
- Clash of Clans style
- Idle game mechanics
- Tile-based card systems

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Portal Frontend | React, TypeScript, Tailwind CSS (account/portal only) |
| Game Client | **Unity 6 LTS + URP** (WebGL, Android, iOS) |
| Backend | Node.js, Express 5 |
| Database | PostgreSQL + Drizzle ORM → Supabase (planned) |
| Realtime | Socket.IO (planned Phase 8) |
| Auth | JWT (stateless tokens — shared by portal and Unity client) |
| Build | Vite (portal), esbuild (backend), Unity Build Pipeline (game) |
| Monorepo | pnpm workspaces |

---

## Workspace Structure

```
/
├── artifacts/
│   ├── eternal-kingdoms/        # React + Vite PORTAL ONLY (no game rendering)
│   └── api-server/              # Express backend
├── lib/
│   ├── api-spec/                # OpenAPI spec (source of truth)
│   ├── api-client-react/        # Generated React Query hooks
│   ├── api-zod/                 # Generated Zod validation schemas
│   └── db/                      # Drizzle ORM schema + connection
├── unity-client/                # Unity 6 LTS game client root
│   ├── README.md
│   ├── UNITY_ARCHITECTURE.md
│   ├── UNITY_NETWORKING.md
│   ├── UNITY_WORLD_SYSTEM.md
│   ├── UNITY_KINGDOM_SYSTEM.md
│   ├── UNITY_ART_PIPELINE.md
│   ├── UNITY_BUILD_PIPELINE.md
│   └── UNITY_API_CONTRACT.md
├── assets/
│   ├── buildings/               # Building placeholder SVGs
│   ├── troops/                  # Troop placeholder SVGs
│   ├── monsters/                # Monster placeholder SVGs
│   ├── dragons/                 # Dragon placeholder SVGs
│   ├── skins/                   # Skin placeholder assets
│   ├── terrain/                 # Terrain tiles
│   └── ui/                      # UI icons and elements
├── PROJECT_MASTER.md            # This file
├── ROADMAP.md                   # Feature roadmap
├── ARCHITECTURE_STATE.md        # Current architecture status
├── SUPABASE_MIGRATION_PLAN.md   # Supabase migration planning doc
├── GAME_DESIGN_BIBLE.md         # ⭐ Spec Freeze: full gameplay design (5000+ words)
├── WORLD_ARCHITECTURE_BIBLE.md  # ⭐ Spec Freeze: 2048×2048 world system (4000+ words)
├── BLOCKCHAIN_ARCHITECTURE_BIBLE.md # ⭐ Spec Freeze: NFT + on-chain architecture (6000+ words)
├── COMBAT_ENGINE_BIBLE.md       # ⭐ Spec Freeze: combat math + simulation (5000+ words)
├── ALLIANCE_AND_SOVEREIGNTY_BIBLE.md # ⭐ Spec Freeze: alliance + congress (4000+ words)
└── MONETIZATION_BIBLE.md        # ⭐ Spec Freeze: F2P + NFT economy (3000+ words)
```

---

## Database Entities

| Entity | Table | Status |
|--------|-------|--------|
| Users | `users` | ✅ Schema defined + live |
| Kingdoms | `kingdoms` | ✅ Schema defined + live |
| Buildings | `buildings` | ✅ Schema defined + live |
| Resources | `resources` | ✅ Schema defined + live |
| Troops | `troops` (legacy) | ✅ Schema defined |
| Research | `research` | ✅ Schema defined |
| Worlds | `worlds` | ✅ Schema defined + live |
| Maps | `map_tiles` | ✅ Schema defined |
| Monsters | `monsters` | ✅ Schema defined + live |
| Monster Spawns | `monster_spawns` | ✅ Schema defined + live |
| Crystal Nodes | `crystal_nodes` | ✅ Schema defined + live |
| Alliances | `alliances` | ✅ Schema defined |
| Asset Registry | `asset_registry` | ✅ Schema defined + live |
| Marches | `marches` | ✅ Phase 3+4: gather + attack_monster |
| Heroes | `heroes` | ✅ Phase 4: inventory, rarity, XP, skills |
| Action Points | `action_points` | ✅ Phase 4: lazy regen |
| Battle Reports | `battle_reports` | ✅ Phase 4: permanent combat history |
| Hospital | `hospital` | ✅ Phase 4: wounded tracking, lazy heal |
| Troop Inventory | `troop_inventory` | ✅ Phase 4: T1-T5 counts by TroopKey |
| Inventory | `inventory` | ✅ Phase 4: item bag by itemKey |

---

## API Endpoints (v0.1)

| Method | Path | Purpose | Status |
|--------|------|---------|--------|
| GET | /api/healthz | Health check | ✅ |
| POST | /api/auth/register | Register | ✅ |
| POST | /api/auth/login | Login | ✅ |
| GET | /api/auth/me | Current user | ✅ |
| GET | /api/kingdoms/mine | My kingdom | ✅ |
| GET | /api/kingdoms/:id | Kingdom by ID | ✅ |
| GET | /api/kingdoms/:id/summary | Dashboard summary | ✅ |
| GET | /api/kingdoms/:id/state | Full game state | ✅ |
| GET | /api/kingdoms/:id/queue | Upgrade queue | ✅ |
| GET | /api/kingdoms/:id/construction-queue | Construction queue | ✅ |
| GET | /api/kingdoms/:id/construction-options | Build options | ✅ |
| POST | /api/kingdoms/:id/construct | Start construction | ✅ |
| GET | /api/kingdoms/:id/buildings | Kingdom buildings | ✅ |
| GET | /api/kingdoms/:id/resources | Kingdom resources | ✅ |
| POST | /api/buildings/:id/upgrade | Upgrade building | ✅ |
| GET | /api/buildings/:id/upgrade-preview | Upgrade preview | ✅ |
| GET | /api/assets | List assets | ✅ |
| GET | /api/assets/:assetId | Get asset | ✅ |
| GET | /api/worlds | List worlds | ✅ |
| GET | /api/worlds/:id/map | World map data | ✅ |
| GET | /api/worlds/:id/kingdoms | World kingdoms | ✅ |
| GET | /api/worlds/:id/spawns | Monster spawns | ✅ |
| POST | /api/worlds/:id/place-kingdom | Place kingdom (DEV) | ✅ |
| POST | /api/marches | Create gather march | ✅ Phase 3 |
| GET | /api/marches | List active marches | ✅ Phase 3 |
| DELETE | /api/marches/:id | Cancel march | ✅ Phase 3 |
| GET | /api/worlds/:id/resource-nodes | Active resource nodes | ✅ Phase 3 |
| GET | /api/worlds/:id/monster-nodes | Active monster spawns | ✅ Phase 3 |
| GET | /api/heroes | List heroes | ✅ Phase 4 |
| GET | /api/heroes/:id | Hero detail | ✅ Phase 4 |
| GET | /api/troops | Troop inventory | ✅ Phase 4 |
| GET | /api/troops/definitions | T1-T5 stat definitions | ✅ Phase 4 |
| GET | /api/hospital | Wounded counts (lazy heal) | ✅ Phase 4 |
| GET | /api/reports | Battle report list | ✅ Phase 4 |
| GET | /api/reports/:id | Battle report detail | ✅ Phase 4 |
| GET | /api/monsters/:spawnId | Monster spawn + AP cost | ✅ Phase 4 |
| POST | /api/monsters/:spawnId/attack | Launch attack march | ✅ Phase 4 |
| GET | /api/kingdoms/:id/ap | Current action points | ✅ Phase 4 |
| GET | /api/inventory | Item inventory | ✅ Phase 4 |

Full Unity-facing audit: `unity-client/UNITY_API_CONTRACT.md`

---

## Asset Registry Convention

All visual assets follow this naming pattern:

```
{category}_{name}_{version_padded}
```

Examples:
- `building_palace_001`
- `building_farm_001`
- `troop_militia_001`
- `monster_wolf_001`
- `dragon_fire_001`
- `skin_palace_001`

Future NFT tokens connect through these IDs. Asset registry IDs are **permanent** — never rename once assigned.

---

## Phase 5.5 Art Asset Integration

| System | File | Purpose |
|--------|------|---------|
| ArtAssetRegistry | Scripts/Content/ | 80 Addressable addresses, startup validation, fallback |
| CitizenManager | Scripts/Population/ | Palace-level NPC population (5–60 citizens) |
| CitizenController | Scripts/Population/ | 5-state NPC behaviour, 80u LOD pause |
| AmbientLifeManager | Scripts/Environment/ | Biome-specific birds/butterflies/wind/leaves |
| DemoSceneController | Scripts/Demo/ | Full demo scene: cinematic, debug overlay, screenshot |
| VerticalSliceController | Scripts/Demo/ | 11-step Login→BattleReport flow tracker |
| PerformanceValidator | Scripts/Performance/ | 500-entity stress test, FPS P5 metric |

See `unity-client/PHASE55_REPORT.md` for missing asset spec + demo scene hierarchy.

---

## Visual Architecture (Phase 5)

| System | File | Purpose |
|--------|------|---------|
| VisualSettingsManager | Scripts/Visual/ | 4-tier quality (Low/Med/High/Ultra), URP swap |
| BiomeTerrainController | Scripts/Terrain/ | 7 biomes, fog/ambient per biome |
| TerrainSplatMapper | Scripts/Terrain/ | Slope-aware splat, Perlin variation |
| EnvironmentDecorationManager | Scripts/Environment/ | Procedural decoration, biome density, pool |
| KingdomVisualController | Scripts/Kingdom/ | Palace+rings+walls+flags+torches |
| BuildingVisualState | Scripts/Kingdom/ | 4-state building slot state machine |
| ResourceNodeVisual | Scripts/World/ | 6 node types, crystal emissive pulse |
| MonsterVisualController | Scripts/Monsters/ | 5 categories, dissolve death, tier tint |
| MarchBannerEntity | Scripts/World/ | Banner+formation+hero portrait+dust trail |
| UIThemeManager | Scripts/UI/ | Dark medieval fantasy theme |
| AnimatedPanel | Scripts/UI/ | Base class: 4 animation modes, eased |
| AudioManager (ext.) | Scripts/Managers/ | +UI/Combat/World channels |
| AmbientAudioController | Scripts/Audio/ | 8 biome contexts, 3s cross-fade |
| VFXLibrary | Scripts/VFX/ | 9 effects, pool, screen shake |
| AssetCatalogManager | Scripts/Content/ | Addressables, ref-count, hot-swap, NFT |
| PerformanceManager | Scripts/Performance/ | FPS sampling, adaptive quality |

See `unity-client/PHASE5_VISUAL_REPORT.md` for full art specs and inspector wiring guide.

---

## Architecture Decisions

1. **OpenAPI-first API contract** — All types and hooks are generated from `lib/api-spec/openapi.yaml`. No hand-writing types.
2. **Unity as primary game client** — The React portal (`artifacts/eternal-kingdoms/`) is account + portal only. All game rendering runs in Unity 6 LTS + URP.
3. **Asset Registry as NFT bridge** — Every visual object has a unique registry ID. NFTs will reference these IDs. The `nftContractAddress` and `nftTokenId` fields are reserved but null until Polygon integration.
4. **Placeholder-first assets** — All game assets start as SVG placeholders with standardized IDs. They are swappable via the asset registry without code changes.
5. **World-scoped data** — Every kingdom belongs to a world. The schema supports 20+ persistent worlds and a dedicated battle world.
6. **JWT auth** — Stateless JWT tokens are stored client-side (localStorage in portal, PlayerPrefs in Unity). Shared secret `SESSION_SECRET`. The `role` field supports player/moderator/admin roles.
7. **Future-ready schemas** — Tables include `metadata` JSONB columns and `nft_*` fields to avoid schema migrations when NFT/marketplace features ship.
8. **Supabase migration** — Infrastructure is in place. `lib/db/src/providers/` abstracts the connection. Switching providers requires only setting `USE_SUPABASE=true` + `SUPABASE_*` env vars. Repository interfaces are unchanged. See `SUPABASE_MIGRATION_PLAN.md`, `SUPABASE_RLS_PLAN.md`, `BACKUP_AND_RECOVERY.md`.

---

## Governance Rules

- This file MUST be updated when any schema, endpoint, or architecture decision changes.
- New features require a ROADMAP.md entry before implementation.
- ARCHITECTURE_STATE.md tracks completion status of each system.
- The `assets/` directory is the canonical location for all game art assets.
- Asset registry IDs are permanent once assigned — they cannot be renamed.
- `unity-client/UNITY_API_CONTRACT.md` must be updated when new API endpoints are added.

---

## Specification Documents

All design and engineering specifications are frozen. These documents are the **immutable reference architecture** — all future implementation must conform to them.

| Document | Topic | Words |
|----------|-------|-------|
| `GAME_DESIGN_BIBLE.md` | Vision, gameplay loops, all systems (Palace, Research, March, Combat, Hero, Dragoon, Congress, Seasons) | 5000+ |
| `WORLD_ARCHITECTURE_BIBLE.md` | 2048×2048 grid, zones 0–7, chunk system, tile occupancy, NFT land boundaries, fog of war, node lifecycle, biomes, multi-world | 4000+ |
| `BLOCKCHAIN_ARCHITECTURE_BIBLE.md` | ERC-721 Land NFTs (4096), ERC-1155 resources, oracle, heroes/dragoon NFTs, breeding, marketplace, wallet abstraction, Polygon | 6000+ |
| `COMBAT_ENGINE_BIBLE.md` | March equations, RPS modifiers, armor curves, attack/defense formulas, buff stacking, casualty rules, plunder, rally, shrine, Congress wars, anti-cheat | 5000+ |
| `ALLIANCE_AND_SOVEREIGNTY_BIBLE.md` | Alliance hierarchy, treasury, research, fortresses, territory taxation, shrines, Congress, King powers, NAP, diplomacy, cross-world warfare | 4000+ |
| `MONETIZATION_BIBLE.md` | F2P philosophy, premium currency, speedup caps, cosmetics, skins, NFTs, marketplace fees, battle pass, anti-P2W, whale controls, retention loops | 3000+ |

---

## Current Phase

**Specification Freeze — Complete**

- ✅ All browser-based game rendering code permanently deleted (`src/game/`, `src/components/world3d/`)
- ✅ PixiJS removed from dependencies
- ✅ React portal is now account + portal frontend only
- ✅ `/kingdom` and `/world` pages show Unity Client placeholder
- ✅ `/unity-client/` directory created with full architecture documentation (8 files)
- ✅ `SUPABASE_MIGRATION_PLAN.md` created
- ✅ `GAME_DESIGN_BIBLE.md` — 21-section gameplay design specification
- ✅ `WORLD_ARCHITECTURE_BIBLE.md` — full world spatial system specification
- ✅ `BLOCKCHAIN_ARCHITECTURE_BIBLE.md` — Polygon NFT + oracle architecture
- ✅ `COMBAT_ENGINE_BIBLE.md` — deterministic combat simulation specification
- ✅ `ALLIANCE_AND_SOVEREIGNTY_BIBLE.md` — alliance governance and Congress system
- ✅ `MONETIZATION_BIBLE.md` — F2P philosophy, anti-P2W policies, NFT economy
- ✅ Supabase provider infrastructure complete (providers, clients, storage, health endpoint, realtime foundation)
- ✅ Migration scripts: `pnpm db:export`, `pnpm db:migrate:supabase`, `pnpm db:verify`, `pnpm db:rollback`
- ✅ `SUPABASE_RLS_PLAN.md` — RLS policies designed (not yet enabled)
- ✅ `BACKUP_AND_RECOVERY.md` — daily/weekly/PITR/DR strategy
- ✅ `GET /health/database` endpoint operational
- ✅ Backend operational, all APIs unchanged
- ✅ Dev account provisioned (dev@eternalkingdoms.com)

Required env for Supabase (optional — game runs without them on drizzle):
- `SUPABASE_URL` — Supabase project URL
- `SUPABASE_ANON_KEY` — public anon key
- `SUPABASE_SERVICE_ROLE_KEY` — server-side admin key
- `SUPABASE_JWT_SECRET` — future Supabase Auth migration
- `USE_SUPABASE=true` — opt-in Supabase provider in development

---

## Unity Phase 5.8 Summary (2026-06-20)

Temporary Free Asset Population Sprint — all 190 Addressable slots mapped to permanently-free substitutes.

**Delivered:**
- `Assets/Data/FreeAssetDatabase.json` — 20 free packs, 190 key→source mappings, URLs, licenses, LOD/animator/scale/color flags
- `Assets/Scripts/Editor/FreeAssetImporter.cs` — one-click import: LOD groups, AnimatorControllers, URP materials, Addressables group `EK_FreeAssets_Phase58`, AssetManifest update
- `Assets/Scripts/Editor/ProcGenFallbackFactory.cs` — composite quad-box fallback meshes (non-primitive), category colour coding, `FallbackAssetTag` runtime warning
- `Assets/Scripts/Editor/AddressablesPopulator.cs` — EditorWindow validator: 190-key scan, P1 highlighting, gap-fill, gap report export
- `BuildConfigs/AssetManifest.json` v1.1 — 190/190 delivered, full pack summary, import instructions
- `PHASE58_FREE_ASSET_POPULATION_REPORT.md` — 13-section report

**Results:** 100% Addressable coverage · 12/12 Priority 1 · AlphaLaunchValidator GO · 100% visual coverage

**Free packs (all CC0 / permanently free):**
- Quaternius: Medieval Buildings, Modular Medieval, Monsters, Characters, Nature, Farm, Survival Props, Dungeon, Crystals
- Kenney: Medieval RTS, Nature Kit, Road Textures, Platformer Kit 3D
- Mixamo (free for commercial use), Unity Particle Pack (free), OpenGameArt (CC0)

---

## Unity Phase 5.7 Summary (2026-06-20)

Alpha Content Lock & Playable Alpha Build — 9 new C# scripts, 2 build configs, 2 reports.

**Delivered:**
- `BuildingUpgradeVisualController.cs` — 3-tier building visuals (Early/Developed/Advanced), scaffolding upgrade sequence, fade transitions, celebration VFX
- `KingdomBeautificationManager.cs` — 11 prop categories, Palace-level density, async async spawn, 12u clearance zone, refresh-on-level-up
- `WorldBeautificationManager.cs` — 8 dense forest zones, mountain ridges, 5 landmark types, 4-road network, biome-transition blending, destroyed camps
- `MonsterAIController.cs` — 7-state AI (NavMeshAgent), rare-tier 3× territory, Dragon flight coroutine, player detection
- `AlphaPolishManager.cs` — camera shake, 7 UI sounds, smooth scene load (fade+bar+min 1.2s), context hints
- `PhotoModeController.cs` — F8 free-fly, WASD+mouse+roll+FOV, time/weather controls, 4K screenshot
- `TrailerCaptureController.cs` — F9 start, 7 shots (kingdom/world/march/monster/sunrise/night/storm), per-shot screenshot
- `PlaytestManager.cs` — F12 panel, F5/F6/F7, resource/monster/upgrade cheats, production safeguard
- `AlphaLaunchValidator.cs` — runtime QA + editor build gate, disk report output
- `BuildConfigs/AlphaBuildManifest.json` — 5 targets, Addressables CDN, quality gates
- `BuildConfigs/AssetManifest.json` — 80+ keys, priority 1 list
- `PHASE57_REPORT.md` + `ALPHA_LAUNCH_REPORT.md`

**Alpha launch status:** CONDITIONAL GO. All engineering complete. Blocked only on art-team delivery (B1–B7 in `ALPHA_LAUNCH_REPORT.md`).

---

## Unity Phase 5.6 Summary (2026-06-20)

Alpha Visual Realization — production visual systems complete. 7 new C# scripts.

**Delivered:**
- `ArtImportManager.cs` — bulk Addressables import (80 keys, 8 categories), LOD/Animator/Material validation, auto-assign prefabs to KingdomVisualController + MonsterSpawnManager, startup ImportReport
- `WorldEnvironmentManager.cs` — 24-hour cycle (Dawn/Day/Sunset/Night), server-synced UTC time, 6 weather types (Clear/Rain/Storm/Snow/Fog/Ashfall), dynamic skybox, weather particles + audio crossfade, VolumeProfile swap
- `CinematicCameraManager.cs` — login/kingdom-entry/flythrough/battle-victory/scene-transition cinematics, entity focus, screenshot mode (4× super), `OnCinematicStarted/Ended` events
- `AlphaVFXController.cs` — 16 VFX Graph effects with pool (8/effect): selection, construction, completion, gather, death, arrival, levelup, loot, torch, smoke, campfire
- `AlphaUIController.cs` — global medieval skin, animated notifications, `TooltipTrigger` component, floating resource numbers, hover scale transitions
- `AlphaDemoController.cs` — 9-step automated demo (Login→BattleReport→Home), F1 start, ESC abort, loop mode for trade shows
- `VisualQAValidator.cs` — runtime QA (missing materials/textures/Addressables/animations/placeholders/primitives) + editor `IPreprocessBuildWithReport` build gate
- `PHASE56_REPORT.md` + `ALPHA_READINESS_REPORT.md`

**Alpha launch status:** CONDITIONAL GO. All code systems complete. Blocked on art team delivering 80 Addressable assets.

---

## Unity Phase 3 Summary (2026-06-20)

World Simulation Foundation — first playable MMO loop. No PvP, no combat.

**Delivered (backend):**
- `lib/db/src/schema/marches.ts` + pushed to DB
- `lib/game-engine/src/marchCalculator.ts` — COMBAT_ENGINE_BIBLE.md §2 math
- `lib/db/src/repositories/marchRepository.ts`
- `artifacts/api-server/src/routes/marches.ts` — POST/GET/DELETE /api/marches
- `artifacts/api-server/src/routes/worldNodes.ts` — GET /worlds/:id/resource-nodes + monster-nodes
- `artifacts/api-server/src/engine/marchProcessor.ts` — tick-driven lifecycle
- OpenAPI: 4 new paths, 10 new schemas, marches tag; codegen regenerated

**Delivered (Unity):**
- MarchDTOs, MarchStateMachine, TravelCalculator, MarchService, MarchManager
- MarchEntity, MarchPathVisualizer, ResourceNodeEntity, ResourceNodeManager
- ResourceSpawnService, MonsterSpawnManager, WorldSimulationManager
- ResourceGatherPanel, WorldEventManager

**Player flow complete:**
Enter World → Select Resource Node → ResourceGatherPanel → Send March →
MarchEntity travels world → Arrives → Gathers → Returns → Resources deposited

Next: **Unity Phase 4 — Kingdom UI (Upgrade dialog, Construct dialog, building timers, Research system)**

---

## Unity Phase 2 Summary (2026-06-20)

World Foundation + Streaming System. 24 new C# scripts. No gameplay.

**Delivered:**
- `World/Grid/` — WorldCoordinate, ChunkCoordinate, WorldGrid, OccupancyManager, SpatialIndex
- `World/Streaming/` — Chunk, ChunkManager (object pool), WorldStreamingManager (load radius=5, unload=7)
- `World/Terrain/` — BiomeGenerator (7 biomes, zone-aware), TerrainGenerator (fBm, vertex color), TerrainChunk
- `World/` — WorldSceneController (bootstrap sequence + poll loops), WorldCameraController (X=60°, Y=−45°, full controls), WorldSelectionManager
- `World/Entities/` — BaseWorldEntity, WorldEntitySpawner (pooled), KingdomEntity (4 power tiers), MonsterEntity (6 tier colors), CrystalEntity (6 crystal types)
- `World/UI/` — WorldInfoPanel (kingdom/monster/crystal panels), WorldHUD (coord/chunk/biome/zone/zoom), WorldTopBar, WorldBottomBar
- `World/Navigation/` — CoordinateNavigator (backend → tile → FlyTo with live preview)
- `World/FogOfWar/` — FogOfWarManager (bitfield, vision source registry — Phase 2: all visible)
- `Core/SceneController.cs` — GoToKingdom() no-arg overload for World→Kingdom flow
- `Assets/Scenes/SceneSetup_World.md` — full hierarchy, wiring tables, streaming diagram, entity prefab structure

Next: **Unity Phase 3 — Upgrade + Construct dialogs, building timers, research UI**

---

## Unity Phase 1 Summary (2026-06-20)

Complete C# script foundation for the Unity 6 LTS client. 45 scripts delivered across all subsystems.
No gameplay, no troops, no combat — foundation only per spec.

**Delivered:**
- Full `unity-client/Assets/` folder structure (Scripts, Scenes, Prefabs, Art, Materials, ScriptableObjects, Addressables)
- All 13 core + manager singletons with DontDestroyOnLoad persistence and controlled bootstrap order
- `ApiClient.cs` — UnityWebRequest HTTP wrapper with JWT, retry, timeout, 401 handler, env switching
- `AuthService`, `KingdomService`, `WorldService` — wrappers for all 23 live backend endpoints
- All DTOs mirroring the OpenAPI spec (Auth, Kingdom, World, Asset)
- `AuthManager.cs` — JWT PlayerPrefs storage, token validation on boot, HandleUnauthorized flow
- `LoginController.cs` — email/password/remember-me form, error label, loading overlay
- `IsometricCameraController.cs` — drag pan, scroll zoom, pinch zoom, inertia, bounds clamp (X=60°, Y=−45°)
- `KingdomCamera.cs` — kingdom-scoped camera subclass (±150 unit bounds)
- `KingdomStateManager.cs` — 15-second poll of GET /api/kingdoms/:id/state, OnStateRefreshed event
- `KingdomNodeLayout.cs` — 12 fixed building nodes in 4 rings (Palace, Inner ×4, Middle ×4, Outer ×4)
- `KingdomTerrainBuilder.cs` — procedural terrain plateau mesh (URP material-ready)
- `SelectableEntity.cs` + `EntitySelectionManager.cs` — single-selection system with hover scale
- `NodeTooltip.cs` — cursor-following tooltip
- `UIManager`, `PopupManager`, `NotificationManager` — full UI layer stack + toast system
- `NetworkErrorPopup`, `ReconnectPopup`, `LoadingSpinner` — connectivity UI
- `NoiseUtils.cs` — fBm terrain noise mirroring lib/game-engine/terrainGenerator.ts
- `MathExtensions.cs` — backend↔Unity coordinate math, tile/chunk helpers
- `EnvironmentConfig.cs` — ScriptableObject for Dev/Staging/Prod env config
- `BuildingData.cs` — ScriptableObject per building type (ring, slot, Addressables key)
- `NFTBridge.cs` — assetId↔NFT bridge (reserved, no-op until Phase 10)
- Scene hierarchy guides: Bootstrap, Login, Kingdom (SceneSetup_*.md)
- Build configs: Development, Staging, Production JSON
- `WEBGL_INTEGRATION_GUIDE.md` — JWT handoff, iframe embedding, postMessage protocol, build commands
- `UNITY_PROGRESS.md` — full phase tracking document
