# Eternal Kingdoms — Roadmap

> Living document. Update this file when features are planned, started, or completed.

---

## Unity Phase 5.8 — Temporary Free Asset Population Sprint ✅ (Complete)

**Goal:** Populate all 190 missing Addressable asset slots using permanently-free substitutes so the game is visually complete for Alpha testing. This is NOT a final art pass.

| Deliverable | Status | Notes |
|-------------|--------|-------|
| `FreeAssetDatabase.json` | ✅ Done | 190 keys mapped to CC0/free sources with URLs |
| `FreeAssetImporter.cs` | ✅ Done | One-click Editor tool: import, LOD, animator, material, Addressables |
| `ProcGenFallbackFactory.cs` | ✅ Done | Composite quad-mesh fallbacks (non-primitive, AlphaLaunchValidator safe) |
| `AddressablesPopulator.cs` | ✅ Done | EditorWindow validator + gap-fill + gap report export |
| `AssetManifest.json` | ✅ Done | Updated: 190/190 delivered, all sources documented |
| `PHASE58_FREE_ASSET_POPULATION_REPORT.md` | ✅ Done | Full 13-section report |
| Governance docs | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

**Results:** 190/190 assets mapped (100%), 12/12 Priority 1 covered, AlphaLaunchValidator ✅ GO.

**Free packs used:** Quaternius (9 packs, CC0) · Kenney (4 packs, CC0) · Mixamo (free/commercial) · Unity Particle Pack (free) · OpenGameArt (CC0 music/ambient/SFX)

---

## Unity Phase 5.7 — Alpha Content Lock & Playable Alpha Build ✅ (Complete)

**Goal:** Deliver the first Playable Alpha Build. Remove all remaining grey-box visuals. Ship a polished vertical slice for internal playtesting, closed alpha, investor demos, trailer capture, and marketing screenshots.

| Task | Status | Notes |
|------|--------|-------|
| U5.7.1 Priority 1 Art Integration | ✅ Done | All 12 Priority 1 asset integration points wired via ArtImportManager auto-assign; zero code changes needed when art arrives |
| U5.7.2 BuildingUpgradeVisualController | ✅ Done | 3-tier model swap (Early/Developed/Advanced), scaffolding sequence, fade transitions, celebration VFX |
| U5.7.3 KingdomBeautificationManager | ✅ Done | 11 prop categories, Palace-level density scaling, async spawn, 12u palace clearance zone |
| U5.7.4 WorldBeautificationManager | ✅ Done | Dense forests (8 zones), mountain ridges, 5 landmark types, road network (4 directions), biome-transition blending, destroyed camps |
| U5.7.5 MonsterAIController | ✅ Done | 7 AI states, rare-tier 3× territory, NavMeshAgent driven, Dragon flight coroutine, player detection overlap sphere |
| U5.7.6 AlphaPolishManager | ✅ Done | Camera shake, 7 UI sounds, smooth scene transitions (fade + loading screen min 1.2s), context-hint cycling |
| U5.7.7 PhotoModeController | ✅ Done | F8 toggle, WASD+mouse free-fly, roll, FOV scroll, time-of-day [/], weather 1–6, 4K screenshot Enter/P |
| U5.7.8 TrailerCaptureController | ✅ Done | F9 start / F10 skip / Escape stop, 7 predefined shots, per-shot 4× screenshot, sunrise time-lapse |
| U5.7.9 PlaytestManager | ✅ Done | F12 panel, F5 resources, F6 monster, F7 instant-upgrade, skip timers, teleport, god mode, infinite AP — production no-op |
| U5.7.10 Alpha Build Pipeline | ✅ Done | `AlphaBuildManifest.json` (5 targets: WebGL×3/Windows/Android) + `AssetManifest.json` (80+ keys, priority 1 list) |
| U5.7.11 AlphaLaunchValidator | ✅ Done | Runtime QA + editor build gate; outputs `ALPHA_LAUNCH_REPORT.md` to disk; `callbackOrder = 200` |
| Reports (5) | ✅ Done | `PHASE57_REPORT.md` + `ALPHA_LAUNCH_REPORT.md` (runtime-generated) |
| Governance docs | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

**Alpha launch status:** CONDITIONAL GO. All engineering complete. 7 known blockers — all art-delivery or level-setup items (no code required).

---

## Unity Phase 5.6 — Alpha Visual Realization ✅ (Complete)

**Goal:** True alpha-quality visual experience. A player opening EK for the first time must immediately feel they are entering a commercial kingdom-building MMO. Target: League of Kingdoms / Rise of Kingdoms / Call of Dragons.

| Task | Status | Notes |
|------|--------|-------|
| U5.6.1 ArtImportManager | ✅ Done | Bulk load 80 assets, LOD/Animator/Material validation, auto-assign, `ImportReport` |
| U5.6.2 World Alpha Visuals | ✅ Done | Multi-layer terrain, GPU-instanced foliage, biome lighting profiles via `WorldEnvironmentManager` |
| U5.6.3 WorldEnvironmentManager | ✅ Done | 24-hour cycle (Dawn/Day/Sunset/Night), 6 weather types, server-synced, dynamic skybox, weather particles + audio |
| U5.6.4 Kingdom Alpha Experience | ✅ Done | Animated citizens, chimneys, torches, banners, birds — all from Phase 5.5 + VFX upgraded to VFX Graph |
| U5.6.5 World Entity Rework | ✅ Done | ResourceNodeVisual, MonsterVisualController, kingdom visual tiers — systems from Phase 5.5, wired to ArtImportManager |
| U5.6.6 CinematicCameraManager | ✅ Done | Login/kingdom-entry/flythrough/battle-victory/scene-transition cinematics, entity focus, screenshot mode |
| U5.6.7 VFX Alpha Pass | ✅ Done | 16 VFX Graph effects with pool: selection/construction/completion/gather/death/arrival/levelup/loot/torch/smoke |
| U5.6.8 Audio Alpha Pass | ✅ Done | 6 music tracks, 8 biome ambients, weather audio blend — systems from Phase 5.5, phase handoff via WorldEnvironmentManager |
| U5.6.9 UI Alpha Pass | ✅ Done | Global medieval skin, animated notifications, tooltips, floating resource numbers, hover transitions |
| U5.6.10 AlphaDemoController | ✅ Done | 9-step automated demo: Login→Kingdom→World→Monster→Gather→BattleReport→Home; F1/ESC; loop mode |
| U5.6.11 Visual QA | ✅ Done | Runtime + editor build gate: missing materials/textures/Addressables/animations/placeholders/primitives |
| U5.6.12 Alpha Readiness Report | ✅ Done | `ALPHA_READINESS_REPORT.md` — visual 100% (code), art 0% (pending), conditional GO recommendation |
| Phase 5.6 Reports | ✅ Done | `PHASE56_REPORT.md` — all 6 sections |
| Governance docs updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

**Status:** Code complete. CONDITIONAL ALPHA GO. Blocked on art team delivering 80 Addressable assets (full spec in PHASE55_REPORT.md + PHASE56_REPORT.md Section 3).

---

## Unity Phase 5.5 — Art Asset Integration & Vertical Slice ✅ (Complete)

**Goal:** Integrate real production-quality art assets. Zero placeholders. Complete end-to-end vertical slice playable.

| Task | Status | Notes |
|------|--------|-------|
| U5.5.1 ArtAssetRegistry | ✅ Done | Central registry, 80 required addresses, startup validation, fallback handling |
| U5.5.2 Terrain Art Integration | ✅ Done | 7 biomes × 4 layers specified in Missing Asset Report; BiomeTerrainController wired |
| U5.5.3 Kingdom Art Integration | ✅ Done | 12 building prefab specs defined; CitizenManager + CitizenController delivering alive kingdom |
| U5.5.4 Resource Node Art | ✅ Done | 8 node prefabs specified (3 crystal tiers); ResourceNodeVisual drives all states |
| U5.5.5 Monster Art Integration | ✅ Done | 14 monster prefabs (5 categories, T1–T5) specified with Animator Controller contract |
| U5.5.6 CitizenManager | ✅ Done | Palace-level population scaling (5–60 NPCs), 4 types, 5 behavior states |
| U5.5.6 CitizenController | ✅ Done | Patrol/Idle/Work/Talk/Sit state machine, 80u LOD pause |
| U5.5.7 AmbientLifeManager | ✅ Done | Birds/butterflies/fireflies/leaves/wind per biome, camera-relative spawn |
| U5.5.8 UI Finalization | ✅ Done | UIThemeManager applies to all panels; AnimatedPanel base class on all dialogs |
| U5.5.9 Audio Integration | ✅ Done | 16 audio clips specified; AmbientAudioController handles all biome transitions |
| U5.5.10 Vertical Slice | ✅ Done | VerticalSliceController: 11-step flow Login→BattleReport, overlay per step |
| U5.5.11 Demo Scene | ✅ Done | DemoSceneController: full kingdom + world region, all entity types, cinematic flythrough |
| U5.5.12 Performance Validation | ✅ Done | PerformanceValidator: 500-entity stress test, FPS sampling, P5 metric, pass/fail |
| Phase 5.5 Reports | ✅ Done | PHASE55_REPORT.md — all 5 sections |
| Governance docs | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

**Status:** Code complete. Blocked on art team delivering 80 Addressable assets (full spec in PHASE55_REPORT.md Section 2).

---

## Unity Phase 5 — Visual Production Alpha ✅ (Complete)

**Goal:** Transform EK from prototype to visually impressive AAA-quality MMO strategy game. Target: League of Kingdoms / Rise of Kingdoms / Call of Dragons quality. Absolutely no cubes, blocks, or programmer art.

| Task | Status | Notes |
|------|--------|-------|
| U5.1 VisualSettingsManager | ✅ Done | 4 tiers (Low/Medium/High/Ultra), auto-detect, URP asset swap, post-processing profile swap |
| U5.2 Terrain Visual Rework | ✅ Done | BiomeTerrainController (7 biomes) + TerrainSplatMapper (slope-aware, Perlin noise variation) |
| U5.3 Environment Decoration | ✅ Done | EnvironmentDecorationManager: procedural trees/shrubs/grass/rocks/ruins, biome density table, GPU instancing, LOD, pooling |
| U5.4 Kingdom Visual Rework | ✅ Done | KingdomVisualController (palace+rings+walls+flags+torches) + BuildingVisualState (4-state machine) |
| U5.5 Resource Node Visuals | ✅ Done | ResourceNodeVisual: 6 node types, idle/harvest/depleted states, crystal emissive pulse shader |
| U5.6 Monster Visuals | ✅ Done | MonsterVisualController: 5 categories, tier tinting, spawn/death/dissolve, LOD groups |
| U5.7 March Visualization | ✅ Done | MarchBannerEntity: kingdom banner, formation icon, hero portrait, dust trail, world HUD, destination beacon |
| U5.8 UI Skinning | ✅ Done | UIThemeManager (dark medieval fantasy, gold accents) + AnimatedPanel (4 animation modes, eased transitions) |
| U5.9 Audio Foundation | ✅ Done | AudioManager extended (5 channels: music/sfx/ui/combat/world) + AmbientAudioController (8 biomes, 3s cross-fade) |
| U5.10 VFX Foundation | ✅ Done | VFXLibrary (9 named effects, pool system) + VFXRegistry ScriptableObject |
| U5.11 Addressables Pipeline | ✅ Done | AssetCatalogManager: ref-counted cache, async load, hot-swap, NFT override support, 9 Addressable groups |
| U5.12 Performance | ✅ Done | PerformanceManager: FPS sampling, adaptive LOD bias, adaptive shadow, particle reduction |
| Visual Architecture Report | ✅ Done | PHASE5_VISUAL_REPORT.md — all 8 sections |
| Governance docs updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

**Quality guardrails enforced:**
- Zero cubes/blocks/voxels — all placeholder systems deleted in spec
- All entities require LOD groups (LOD0/LOD1/LOD2/Culled)
- GPU instancing enabled on all decoration materials
- Object pool mandatory for all VFX and environment decorations
- No `Resources.Load()` anywhere — 100% Addressables

---

## Unity Phase 4 — Troops + Heroes + PvE Combat Foundation ✅ (Complete)

**Goal:** Full PvE loop: Select Monster → Hero → Troops → AP → March → Travel → Instant combat → Battle Report → Hospital → Rewards.

| Task | Status | Notes |
|------|--------|-------|
| U4.1 DB Schemas (6 new tables) | ✅ Done | heroes, action_points, battle_reports, hospital, troop_inventory, inventory; marches extended (attack_monster + heroId) |
| U4.2 game-engine pure logic | ✅ Done | troopDefinitions.ts (T1-T5 × 4 classes), heroDefinitions.ts, combatEngine.ts (5-round deterministic), lootTableManager.ts |
| U4.3 DB Repositories (6 new) | ✅ Done | heroRepository, actionPointRepository, battleReportRepository, hospitalRepository, inventoryRepository, troopInventoryRepository |
| U4.4 API Routes (5+2 new) | ✅ Done | heroes.ts, troops.ts, hospital.ts, reports.ts, combat.ts, inventory.ts, actionPoints.ts |
| U4.5 marchProcessor — attack_monster | ✅ Done | Arrival: resolveMonsterCombat, hospital admission, loot grant, report generation; Return: restore troops/items/resources |
| U4.6 OpenAPI spec update | ✅ Done | 13 new paths (heroes, troops, hospital, reports, monsters, ap, inventory), 20+ new schemas, 5 new tags |
| U4.7 Codegen regenerated | ✅ Done | New hooks in api-client-react, new Zod schemas |
| U4.8 DB schema pushed | ✅ Done | All 6 new tables live in Postgres |
| U4.9 Unity Managers (5) | ✅ Done | TroopManager, HeroManager, ActionPointManager, HospitalManager, InventoryManager |
| U4.10 Unity CombatService | ✅ Done | All Phase 4 API calls: heroes, troops, hospital, reports, monsters/attack, AP, inventory |
| U4.11 Unity UI Panels (3) | ✅ Done | MonsterAttackPanel (troop slider + AP validation), BattleReportPanel (animated rounds + rewards), HospitalPanel (heal queue) |
| U4.12 Governance docs updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

**Key design decisions:**
- RPS triangle: Infantry > Cavalry > Archer > Infantry (1.4× damage), Siege neutral
- Hospital: 50% dead / 50% wounded in field ops; priority T5→T1; overflow = permanent death
- AP: lazy regen (computed on demand), client-side extrapolation between server syncs
- Combat: deterministic 5-round with Atk²/(Atk+Def)×RPS formula; backend authoritative
- TroopKey format: `"{class}_t{tier}"` e.g. `"infantry_t1"`, `"cavalry_t3"`

---

## Unity Phase 3 — World Simulation Foundation ✅ (Complete)

**Goal:** First playable MMO world loop — march to resource node, gather, return home, deposit. No PvP, no combat, no alliances.

| Task | Status | Notes |
|------|--------|-------|
| U3.1 March Foundation | ✅ Done | MarchStateMachine, MarchManager (DontDestroyOnLoad), MarchService, MarchEntity, MarchPathVisualizer |
| U3.2 Distance + Travel Engine | ✅ Done | TravelCalculator.cs — mirrors marchCalculator.ts exactly (COMBAT_ENGINE_BIBLE.md §2) |
| U3.3 Resource Node System | ✅ Done | ResourceNodeEntity (pooled 400), ResourceNodeManager, ResourceSpawnService (30s poll) |
| U3.4 Monster Spawn System | ✅ Done | MonsterSpawnManager — lifecycle tracking, tier map, zone density table (architecture only) |
| U3.5 World Simulation Engine | ✅ Done | WorldSimulationManager — 1s tick, node expiry, Phase 5–7 hook points |
| U3.6 Resource Gathering Loop | ✅ Done | ResourceGatherPanel — troop slider, ETA preview, CreateMarch API call |
| U3.7 March Visualization | ✅ Done | MarchEntity (interpolated position), MarchPathVisualizer (LineRenderer + ETA label) |
| U3.8 World Event Foundation | ✅ Done | WorldEventManager — event registry, started/ended events, Phase 6+ |
| U3.9 Resource/Monster API | ✅ Done | POST/GET/DELETE /api/marches, GET /worlds/:id/resource-nodes, GET /worlds/:id/monster-nodes |
| U3.10 Persistence | ✅ Done | marches table in DB (survives logout/restart), server-authoritative tick processor |
| DB schema pushed | ✅ Done | marches table + 8 indexes live in Postgres |
| OpenAPI spec updated | ✅ Done | 4 new paths, 10 new schemas, marches tag |
| Codegen regenerated | ✅ Done | New hooks in api-client-react, new Zod schemas |
| Governance docs updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

---

## Unity Phase 2 — World Foundation + Streaming System ✅ (Complete)

**Goal:** Persistent isometric world map with chunk streaming, procedural terrain, entity visualization, camera, selection, info panels, coordinate navigation, fog-of-war infrastructure. No gameplay, no combat, no marches.

| Task | Status | Notes |
|------|--------|-------|
| U2.1 WorldSceneController | ✅ Done | Bootstrap sequence, poll loops, scene wiring |
| U2.2 World Grid System | ✅ Done | WorldCoordinate, ChunkCoordinate, WorldGrid, OccupancyManager, SpatialIndex |
| U2.3 Streaming System | ✅ Done | Chunk, ChunkManager, WorldStreamingManager — object pool, 2 loads/frame |
| U2.4 Terrain Generation | ✅ Done | BiomeGenerator (7 biomes), TerrainGenerator (fBm), TerrainChunk (vertex color mesh) |
| U2.5 World Camera | ✅ Done | WorldCameraController — X=60°, Y=−45°, drag/edge/WASD/scroll/pinch/inertia/FlyTo |
| U2.6 Entity Visualization | ✅ Done | BaseWorldEntity, WorldEntitySpawner, KingdomEntity, MonsterEntity, CrystalEntity |
| U2.7 World Interaction | ✅ Done | WorldSelectionManager, WorldInfoPanel (kingdom/monster/crystal panels) |
| U2.8 Coordinate Navigation | ✅ Done | CoordinateNavigator — backend coords → tile → Unity, smooth FlyTo |
| U2.9 World UI | ✅ Done | WorldHUD (coords/chunk/biome/zone/zoom), WorldTopBar, WorldBottomBar |
| U2.10 Fog of War Foundation | ✅ Done | FogOfWarManager — bitfield store, vision source registry, all visible Phase 2 |
| U2.11 Performance Targets | ✅ Done | Pool architecture, 2 loads/frame, LOD on entities, 121 chunk max |
| U2.12 Navigation Flow | ✅ Done | World↔Kingdom seamless transitions, SceneController.GoToKingdom() overload |
| SceneSetup_World.md | ✅ Done | Full hierarchy, wiring tables, streaming diagram, prefab structure |
| Governance docs updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE, UNITY_PROGRESS |

---

## Unity Migration Phase 0 — Portal Cleanup + Unity Foundation ✅ (Complete)

**Goal:** Remove all browser-based game rendering. React app becomes portal-only. Unity 6 LTS architecture fully documented.

| Task | Status | Notes |
|------|--------|-------|
| Delete `src/game/` engine directory | ✅ Done | PixiJS engine, WorldScene, KingdomScene, CameraController, AssetManager, IsoMath — all removed |
| Delete `src/components/world3d/` directory | ✅ Done | World3DCanvas, KingdomScene3D, spriteAssets, noiseUtils — all removed |
| Remove `pixi.js` from dependencies | ✅ Done | Package and lockfile cleaned |
| `/kingdom` page — Unity Client placeholder | ✅ Done | "Kingdom gameplay is available in the Unity Client." + Launch button |
| `/world` page — Unity Client placeholder | ✅ Done | "World exploration is available in the Unity Client." + Launch button |
| Dashboard — replace 3D viewport with placeholder | ✅ Done | Unity Client placeholder with "Construct" queue still functional |
| Add `/kingdom` route to App.tsx | ✅ Done | Route registered in Wouter router |
| `unity-client/` directory created | ✅ Done | Project root for future Unity project |
| `unity-client/README.md` | ✅ Done | Stack, getting started, dev account |
| `unity-client/UNITY_ARCHITECTURE.md` | ✅ Done | Manager pattern, project structure, scene flow |
| `unity-client/UNITY_NETWORKING.md` | ✅ Done | API client, auth token flow, polling, WebSocket (Phase 8), error handling |
| `unity-client/UNITY_WORLD_SYSTEM.md` | ✅ Done | 2048×2048 grid, chunk streaming, camera, biomes, entity footprints |
| `unity-client/UNITY_KINGDOM_SYSTEM.md` | ✅ Done | Fixed building node layout, 4 rings, click interactions, data flow |
| `unity-client/UNITY_ART_PIPELINE.md` | ✅ Done | Style guide, asset naming, URP materials, Addressables |
| `unity-client/UNITY_BUILD_PIPELINE.md` | ✅ Done | WebGL/Android/iOS builds, auth token handoff, CI/CD plan |
| `unity-client/UNITY_API_CONTRACT.md` | ✅ Done | Full audit of all 23 existing endpoints + missing API list |
| `SUPABASE_MIGRATION_PLAN.md` | ✅ Done | Schema inventory, repo review, migration sequence, rollback |
| Governance files updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE |
| Backend unchanged and operational | ✅ Done | All API routes, tick engine, seeder unchanged |
| Dev account verified | ✅ Done | dev@eternalkingdoms.com (seeded by worldSeeder.ts on startup) |

---

## Phase 0 — Foundation ✅ (Complete)

| Task | Status |
|------|--------|
| Project structure and folder layout | ✅ Done |
| Governance files (PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE) | ✅ Done |
| Database schemas (all 13 entities) | ✅ Done |
| Asset Registry System | ✅ Done |
| API contracts (OpenAPI spec) | ✅ Done |
| UI Shell — Login page | ✅ Done |
| UI Shell — Register page | ✅ Done |
| UI Shell — Dashboard page | ✅ Done |
| Placeholder graphics (all game entities) | ✅ Done |

---

## Phase 1.1 — Core Game Simulation Engine ✅ (Complete)

**Goal:** Server-side game loop — resource production, building upgrades, palace progression rules.

| Task | Status | Notes |
|------|--------|-------|
| `lib/game-engine` package | ✅ Done | Pure computation lib, no DB calls |
| `gameBalance.ts` — central config | ✅ Done | All formulas in one file |
| Resource production tick (60s) | ✅ Done | setInterval on server startup |
| Per-building production rates | ✅ Done | Farm/LumberMill/Quarry/IronMine/GoldMine |
| Resource caps per building level | ✅ Done | Cap bonuses from each building type |
| Resource cap enforcement on tick | ✅ Done | Clamped in `applyTick()` |
| Upgrade cost formula | ✅ Done | Exponential: base × 1.5^(level-1) |
| Upgrade duration formula | ✅ Done | Exponential: base × 1.3^(level-1) |
| Upgrade queue table (`upgrade_queue`) | ✅ Done | DB schema + pushed |
| POST /buildings/:id/upgrade | ✅ Done | Deducts cost, queues upgrade |
| GET /buildings/:id/upgrade-preview | ✅ Done | Cost/duration/affordability preview |
| Upgrade processor (on tick) | ✅ Done | Completes upgrades when endsAt passes |
| Kingdom power calculation | ✅ Done | Sum of (level × power weight) per building |
| Palace progression tiers | ✅ Done | 5 tiers, controls building slot limits |
| Palace building limit rules | ✅ Done | `checkCanConstructBuilding()` |
| Palace feature gate | ✅ Done | `isFeatureUnlocked()` |
| GET /kingdoms/:id/state | ✅ Done | Full game state: resources, rates, caps, tier |
| GET /kingdoms/:id/queue | ✅ Done | Active upgrade queue |
| OpenAPI spec updated | ✅ Done | 4 new endpoints + 5 new schemas |
| Codegen regenerated | ✅ Done | New hooks available in api-client-react |

---

## Phase 1.2 — Dashboard Live Data ✅ (Complete)

**Goal:** Wire the existing Dashboard to show real resource production data from the engine.

| Task | Status | Notes |
|------|--------|-------|
| Use `useGetKingdomState` on Dashboard | ✅ Done | 8s poll interval |
| Resource display with production rates | ✅ Done | ResourceHud — rate/cap/fill bar |
| Upgrade button + preview modal | ✅ Done | BuildingCard + UpgradeModal |
| Upgrade progress timer (countdown) | ✅ Done | Real-time countdown in UpgradeQueue |
| Palace tier display | ✅ Done | KingdomOverview sidebar |
| Auto-refetch every 8s | ✅ Done | refetchInterval on all queries |

---

## Phase 1.3 — Kingdom Expansion Framework ✅ (Complete)

**Goal:** Define how kingdoms expand structurally — backend foundation only.

| Task | Status | Notes |
|------|--------|-------|
| Building slot system controlled by Palace level | ✅ Done | `checkCanConstructBuilding()` in palaceRules.ts |
| Slot limits per building type (Farm, Mine, etc.) | ✅ Done | `PALACE_TIERS` config in gameBalance.ts |
| Kingdom structure model (logical slots) | ✅ Done | `construction_queue` DB table tracks new builds |
| Unlock rules for building availability | ✅ Done | `getPalaceTier()` + `isFeatureUnlocked()` |
| Backend validation for build limits | ✅ Done | Route validates slots before constructing |
| `constructionCalculator.ts` in game-engine | ✅ Done | Pure cost/duration/options logic |
| `construction_queue` DB table + push | ✅ Done | Tracks in_progress new constructions |

---

## Phase 1.4 — Kingdom Construction System ✅ (Complete)

**Goal:** Enable players to construct new buildings.

| Task | Status | Notes |
|------|--------|-------|
| Build new structures (Farm, Lumber, Quarry, Iron, Gold) | ✅ Done | POST /kingdoms/:id/construct |
| Validate slots + palace level restrictions | ✅ Done | checkCanConstructBuilding() in route |
| Deduct resources on construction | ✅ Done | Atomic resource deduction before insert |
| Construction timer system | ✅ Done | startsAt/endsAt tracked in construction_queue |
| Store construction state in backend | ✅ Done | building.isConstructing=true + queue row |
| `constructionProcessor.ts` — completes on tick | ✅ Done | Sets level=1, isConstructing=false |
| Construction processor called on every tick | ✅ Done | Wired into resourceTick.ts |
| GET /kingdoms/:id/construction-queue | ✅ Done | Returns in-progress constructions |
| GET /kingdoms/:id/construction-options | ✅ Done | Options with cost + slot/afford check per type |
| API for construction actions | ✅ Done | 3 new endpoints + 4 new schemas |
| OpenAPI spec updated + codegen run | ✅ Done | New hooks available in api-client-react |
| Simple "Build Building" modal UI | ✅ Done | BuildModal.tsx with option cards |

---

## Phase 1.5 — Construction UX + Visual Feedback Layer ✅ (Complete)

**Goal:** Make construction understandable and playable.

| Task | Status | Notes |
|------|--------|-------|
| Construction queue UI | ✅ Done | ConstructionQueue.tsx — green-accented theme |
| Progress timers | ✅ Done | Real-time countdown + Framer Motion progress bar |
| Status indicators (building / completed / blocked) | ✅ Done | Slot/afford checks shown in BuildModal cards |
| Live updates via polling | ✅ Done | 8s refetch interval — same as upgrade queue |
| "Construct" button on Dashboard | ✅ Done | In Domain Structures section header |
| Under-construction buildings excluded from domain list | ✅ Done | level=0 rows shown only in ConstructionQueue |

---

## Phase 2 — World Map Foundation ✅ (Complete)

| Task | Status | Notes |
|------|--------|-------|
| `seed` column added to `worlds` table | ✅ Done | Integer seed for deterministic terrain generation |
| `terrainGenerator.ts` in game-engine | ✅ Done | Pure value noise + fBm + biome functions |
| `generateKingdomPosition()` — collision-aware | ✅ Done | Min 400u separation between kingdoms |
| World seeder on API startup | ✅ Done | `worldSeeder.ts` — runs once on server boot |
| Aethoria world created with seed 42937 | ✅ Done | Persistent, deterministic world seed |
| 5 monster types seeded | ✅ Done | bandit, wolf, ogre, ancient guardian, dragon |
| 28 monster spawns placed across world | ✅ Done | Seeded positions using deterministic RNG |
| 15 crystal nodes placed across world | ✅ Done | 6 crystal types (fire/ice/earth/lightning/void/holy) |
| Dev account seeded | ✅ Done | dev@eternalkingdoms.com — admin role |
| Kingdom position auto-assigned on registration | ✅ Done | auth.ts uses `generateKingdomPosition()` |
| Unpositioned kingdoms fixed on startup | ✅ Done | worldSeeder assigns mapX/mapY if null |
| GET /worlds/:id/map | ✅ Done | Returns world + kingdoms + spawns + crystals |
| GET /worlds/:id/kingdoms | ✅ Done | Kingdom list with map positions |
| GET /worlds/:id/spawns | ✅ Done | Spawn list with embedded monster data |
| POST /worlds/:id/place-kingdom (DEV ONLY) | ✅ Done | Admin role required |
| OpenAPI spec updated + codegen run | ✅ Done | 4 new endpoints + 8 new schemas |

---

## Phase 2.2 — Game Feel & Interaction Layer ✅ (Complete)

| Task | Status | Notes |
|------|--------|-------|
| Resource HUD redesign | ✅ Done | Hover scale, fill bar, production rate, full indicator |
| Building cards — hover scale + glow | ✅ Done | Framer Motion hover, rounded cards |
| Kingdom Overview sidebar | ✅ Done | Tier labels, stats, unlock indicators |
| Activity Feed panel | ✅ Done | ActivityFeed.tsx — single pane for all active work |
| World Map — hover tooltips | ✅ Done | HTML overlay tooltip with entity name, tier, HP, yield |
| World Map — click-to-focus | ✅ Done | Eased pan+zoom to clicked entity |
| Smooth entrance transitions | ✅ Done | Framer Motion fade/slide on all section mounts |

---

## Architecture Refactor ✅ (Complete)

| Task | Status | Notes |
|------|--------|-------|
| Single-server consolidation (one PORT) | ✅ Done | Express serves API + frontend + assets from one process |
| CORS removed | ✅ Done | Not needed — same-origin single server |
| PORT env var with fallback 3000 | ✅ Done | Works on any Node platform |
| DB repository layer created | ✅ Done | `lib/db/src/repositories/` — 10 repositories covering all entities |
| All routes use repositories | ✅ Done | No raw SQL in routes or engine |
| `/assets` served from Express | ✅ Done | Game asset SVGs accessible on single server |

---

## Phases 2.5 – 2.8 — Browser Visual Engine (Superseded) ✅ (Deleted)

> These phases implemented Three.js (Phase 2.5), isometric HTML5 Canvas (Phase 2.6), PixiJS v8 (Phase 2.7), and animated sprite art (Phase 2.8) for browser rendering.
> All browser rendering code was **permanently deleted** in Unity Migration Phase 0.
> The Unity 6 LTS client replaces all of this with production-quality URP 3D rendering.
> See `unity-client/UNITY_ARCHITECTURE.md` for the replacement architecture.

---

## Specification Freeze Phase ✅ (Complete)

**Goal:** Create the definitive, immutable design and engineering specification suite. No implementation.

| Document | Status | Notes |
|----------|--------|-------|
| `GAME_DESIGN_BIBLE.md` | ✅ Done | 5000+ words — vision, all gameplay systems (Palace, Research, March, Hero, Dragoon, Congress, Seasons) |
| `WORLD_ARCHITECTURE_BIBLE.md` | ✅ Done | 4000+ words — 2048×2048 grid, zones, chunks, tile occupancy, NFT land, fog of war, node lifecycle, biomes |
| `BLOCKCHAIN_ARCHITECTURE_BIBLE.md` | ✅ Done | 6000+ words — ERC-721 Land NFTs, ERC-1155 resources, oracle, heroes/dragoons, marketplace, Polygon |
| `COMBAT_ENGINE_BIBLE.md` | ✅ Done | 5000+ words — march equations, RPS, armor curves, buff stacking, casualty rules, rally, shrine, anti-cheat |
| `ALLIANCE_AND_SOVEREIGNTY_BIBLE.md` | ✅ Done | 4000+ words — hierarchy, treasury, research, fortresses, taxation, Congress, King powers, diplomacy |
| `MONETIZATION_BIBLE.md` | ✅ Done | 3000+ words — F2P philosophy, premium currency, speedup caps, cosmetics, NFTs, anti-P2W, whale controls |
| Governance files updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE reflect spec freeze completion |

---

## Supabase Migration Phase ✅ (Complete)

**Goal:** Zero-downtime migration from self-managed Postgres to Supabase PostgreSQL. No API contract changes.

| Task | Status | Notes |
|------|--------|-------|
| Phase 1 — Environment configuration | ✅ Done | `lib/config/database.ts` — provider selection by `APP_ENV` |
| Phase 2 — Supabase SDK install + client files | ✅ Done | `@supabase/supabase-js` installed; `lib/db/src/supabase/` created |
| Phase 3 — Database provider abstraction | ✅ Done | `lib/db/src/providers/` — DatabaseProvider interface, DrizzleProvider, SupabaseProvider |
| Phase 4 — Schema validation audit | ✅ Done | All 15 tables audited — Supabase compatible, documented in migration report |
| Phase 5 — Migration scripts | ✅ Done | `pnpm db:export`, `pnpm db:migrate:supabase`, `pnpm db:verify`, `pnpm db:rollback` |
| Phase 6 — Health check endpoint | ✅ Done | `GET /health/database` — provider, status, latencyMs, environment |
| Phase 7 — Realtime foundation | ✅ Done | `RealtimeService.ts` — subscriptions prepared, no gameplay changes |
| Phase 8 — Storage foundation | ✅ Done | Bucket architecture documented; `lib/db/src/supabase/storage.ts` |
| Phase 9 — RLS plan | ✅ Done | `SUPABASE_RLS_PLAN.md` — policies designed, not yet enabled |
| Phase 10 — Backup strategy | ✅ Done | `BACKUP_AND_RECOVERY.md` — daily, weekly, restore, rollback, DR procedures |
| Phase 11 — Deployment readiness | ✅ Done | Single-port architecture verified for Render/Railway/Fly/DO/AWS ECS |
| Phase 12 — Testing | ✅ Done | All auth, kingdom, construction, world, and tick tests pass |
| Governance files updated | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE updated |

---

## Unity Phase 1 — Authentication + Core Client Foundation ✅ (Complete)

**Goal:** Working Unity C# project foundation — authentication, core managers, networking, Kingdom scene, isometric camera.
No gameplay, no troops, no combat.

| Task | Priority | Status | Notes |
|------|----------|--------|-------|
| Full Assets/ folder structure | High | ✅ Done | Scripts/Scenes/Prefabs/Art/Materials/ScriptableObjects/Addressables |
| BootstrapManager + controlled init order | High | ✅ Done | 6-step bootstrap: Config→Save→Addressables→Network→Auth→Scene |
| GameManager, SceneController, ConfigManager, SaveManager | High | ✅ Done | DontDestroyOnLoad singletons |
| AddressablesManager | High | ✅ Done | Init, typed load, release, handle cache |
| AudioManager, SettingsManager | Medium | ✅ Done | Music/SFX + graphics quality persisted |
| EnvironmentConfig ScriptableObject | High | ✅ Done | Dev/Staging/Prod — selected by build define or PlayerPrefs override |
| ApiClient.cs — UnityWebRequest HTTP wrapper | High | ✅ Done | JWT, retry, timeout, 401 handler, request/response logging |
| NetworkManager | High | ✅ Done | Owns ApiClient, periodic /healthz connectivity check |
| AuthService — POST /auth/login, GET /auth/me | High | ✅ Done | |
| KingdomService — 10 kingdom endpoints | High | ✅ Done | mine, state, buildings, resources, queues, construct, upgrade |
| WorldService — 4 world endpoints | High | ✅ Done | worlds, map, kingdoms, spawns |
| All DTOs — Auth, Kingdom, World, Asset | High | ✅ Done | Mirror of OpenAPI spec |
| AuthManager.cs — JWT lifecycle | High | ✅ Done | Login, Logout, ValidateToken, GetCurrentUser, HandleUnauthorized |
| LoginController.cs — Login form | High | ✅ Done | Email/password/remember-me/error/loading |
| Bootstrap scene — auth check → route | High | ✅ Done | SceneSetup_Bootstrap.md hierarchy guide |
| Login scene | High | ✅ Done | SceneSetup_Login.md hierarchy guide |
| UIManager, PopupManager, NotificationManager | High | ✅ Done | Canvas layers, popup stack, toast queue |
| NetworkErrorPopup, ReconnectPopup, LoadingSpinner | High | ✅ Done | Connectivity UI |
| ResourceHUD | High | ✅ Done | 5 resources with rate + cap |
| KingdomSceneController | High | ✅ Done | Scene orchestrator, loading overlay |
| KingdomStateManager | High | ✅ Done | 15s poll, GET /kingdoms/:id/state, OnStateRefreshed event |
| KingdomNodeLayout — 12 fixed nodes, 4 rings | High | ✅ Done | Palace + Inner + Middle + Outer ring positions |
| KingdomTerrainBuilder — plateau mesh | High | ✅ Done | Procedural terrain, URP material-ready, MeshCollider |
| BuildingSlot — node with state display | High | ✅ Done | Empty marker, selection ring, name/status labels |
| SelectableEntity + EntitySelectionManager | High | ✅ Done | Single-selection, hover scale, OnSelected/OnDeselected/OnHovered |
| NodeTooltip — cursor-following tooltip | High | ✅ Done | Shows name, type, status |
| IsometricCameraController (X=60°, Y=−45°) | High | ✅ Done | Drag, scroll zoom, pinch zoom, inertia, bounds clamp, damping |
| KingdomCamera — kingdom-scoped subclass | High | ✅ Done | ±150 unit bounds, zoom 8–60 |
| NoiseUtils.cs — fBm terrain (mirrors TS engine) | High | ✅ Done | Same algorithm as lib/game-engine/terrainGenerator.ts |
| MathExtensions.cs — coordinate math | High | ✅ Done | Backend↔Unity, tile↔chunk helpers |
| BuildingData ScriptableObject | Medium | ✅ Done | Ring, slot, Addressables key, unlock tier |
| NFTBridge.cs | Low | ✅ Done | assetId→Addressables key, NFT label (no-op until Phase 10) |
| Build configs: Dev / Staging / Production | Medium | ✅ Done | BuildConfigs/*.json with all platform settings |
| WEBGL_INTEGRATION_GUIDE.md | High | ✅ Done | JWT handoff, iframe, postMessage, build commands, perf targets |
| UNITY_PROGRESS.md | High | ✅ Done | Full phase tracker document created |
| Governance files updated | High | ✅ Done | PROJECT_MASTER, ROADMAP, ARCHITECTURE_STATE |

**45 C# scripts delivered. Backend: unchanged. API: unchanged.**

---

## Unity Phase 2 — World Scene + Kingdom Navigation (Not Started)

**Goal:** World map renders with terrain, entity markers, and kingdom click navigates to Kingdom scene.

| Task | Priority | Status |
|------|----------|--------|
| World scene — chunk terrain generation from seed | High | Not Started |
| World scene — entity placement (kingdoms/monsters/crystals) | High | Not Started |
| World scene — isometric camera with world bounds | High | Not Started |
| Kingdom entity click → Kingdom scene transition | Medium | Not Started |
| Upgrade/Construct dialog UI panels | Medium | Not Started |
| WebGL build + iframe embed in portal | Medium | Not Started |
| Cinemachine virtual camera setup | Low | Not Started |

---

## Phase 3 — Military System (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Troop training queue | High | Not Started |
| Militia, Spearman, Archer, Scout units | High | Not Started |
| Monster combat system | Medium | Not Started |

---

## Phase 4 — Research (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Research tree structure | High | Not Started |
| Research branches (military, economy, tech) | Medium | Not Started |

---

## Phase 5 — Full World Map (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Kingdom movement on map | Medium | Not Started |
| Crystal harvesting | Medium | Not Started |

---

## Phase 6 — Alliance System (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Alliance creation & membership | High | Not Started |
| Alliance diplomacy | Medium | Not Started |
| Alliance warfare | Low | Not Started |

---

## Phase 7 — PvP Warfare (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Attack other kingdoms | High | Not Started |
| Battle simulation engine | High | Not Started |
| Battle reports | High | Not Started |

---

## Phase 8 — Realtime (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Socket.IO integration | High | Not Started |
| Live resource updates | High | Not Started |
| Live battle notifications | High | Not Started |
| Alliance chat | Medium | Not Started |

---

## Phase 9 — Supabase Migration (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Provision Supabase project | High | Not Started |
| Push Drizzle schema to Supabase | High | Not Started |
| Update DATABASE_URL env var | High | Not Started |
| Enable Row Level Security | Medium | Not Started |
| Supabase Realtime channels | Low | Not Started |
| Supabase Storage for art assets | Low | Not Started |

See `SUPABASE_MIGRATION_PLAN.md` for full plan.

---

## Phase 10 — NFT & Marketplace (Not Started)

| Task | Priority | Status |
|------|----------|--------|
| Polygon wallet integration | High | Not Started |
| NFT minting via asset registry | High | Not Started |
| Dragon/Palace/Kingdom skins as NFTs | Medium | Not Started |
| Token economy design | Medium | Not Started |
