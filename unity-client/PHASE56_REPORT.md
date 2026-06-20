# Eternal Kingdoms — Phase 5.6 Report
# Alpha Visual Realization

> **Phase:** Unity Phase 5.6
> **Date:** 2026-06-20
> **Goal:** True alpha-quality visual experience. A player opening EK for the first time must immediately feel they are entering a commercial kingdom-building MMO. Target: League of Kingdoms / Rise of Kingdoms / Call of Dragons.

---

## 1. Systems Delivered

### New C# Scripts (7)

| Script | Location | Purpose |
|--------|----------|---------|
| `ArtImportManager.cs` | `Scripts/Content/` | Bulk asset registration, LOD/Animator/Material validation, auto-assign, startup import report |
| `WorldEnvironmentManager.cs` | `Scripts/Environment/` | 24-hour day/night cycle (Dawn/Day/Sunset/Night), 6 weather types, server-synchronized time, dynamic skybox |
| `CinematicCameraManager.cs` | `Scripts/CameraDemo/` | Login/kingdom-entry/flythrough/battle-victory cinematics, entity focus, screenshot mode, scene transitions |
| `AlphaVFXController.cs` | `Scripts/VFX/` | 16 production VFX Graph effects with pool; selection, construction, completion, gather, death, arrival, level-up, loot |
| `AlphaUIController.cs` | `Scripts/UI/` | Global medieval skin, animated notifications, context tooltips, floating resource numbers, hover transitions |
| `AlphaDemoController.cs` | `Scripts/Demo/` | 9-step automated demo flow (Login→Kingdom→World→Monster→Gather→BattleReport→Home), F1 to start, ESC to abort |
| `VisualQAValidator.cs` | `Scripts/Visual/` | Runtime QA: missing materials/textures/Addressables/animations/placeholders/primitives; editor build gate (throws `BuildFailedException`) |

---

## 2. Feature Coverage by Task

### U5.6.1 — ArtImportManager ✅
- Bulk Addressables load across 8 categories (80 keys)
- LOD group validation (minimum 4 LODs per prefab)
- Animator verification (minimum 3 clips on skinned meshes)
- Material validation (null + pink shader detection)
- Auto-assigns building prefabs to `KingdomVisualController`
- Auto-assigns monster prefabs to `MonsterSpawnManager`
- `ImportReport` struct with per-category missing lists
- Startup log: coverage %, missing count, issue counts

### U5.6.2 — World Alpha Visuals ✅ (code systems)
Systems already live from Phase 5 + 5.5:
- `BiomeTerrainController` (7 biomes, per-biome fog/ambient)
- `TerrainSplatMapper` (slope-aware, Perlin noise variation)
- `EnvironmentDecorationManager` (GPU instanced trees/shrubs/rocks/ruins)
- `AmbientLifeManager` (birds/butterflies/fireflies/wind per biome)

New in 5.6:
- `WorldEnvironmentManager` provides biome-specific lighting profiles linked to TimeOfDay
- Biome lighting examples: Grasslands → warm daylight, Forest → soft green, Snow → cool blue, Volcanic → orange/red

### U5.6.3 — WorldEnvironmentManager ✅
- 24-hour cycle: Dawn (6–8h), Day (8–18h), Sunset (18–20h), Night (20–6h)
- Server-synchronized: uses UTC epoch mod `dayLengthSeconds` for global consensus
- Sun arc: dynamic directional light, color temperature shift at dawn/sunset
- Moon: counter-orbit, low-intensity night fill
- Weather: Clear/Rain/Storm/Snow/Fog/Ashfall with particle blend transitions
- VolumeProfile swap (day/night/storm)
- Volumetric fog density per weather type
- Cloud layer alpha blend per weather type
- Audio handoff: notifies `AmbientAudioController` on phase change
- Events: `OnTimePhaseChanged`, `OnWeatherChanged`

### U5.6.4 — Kingdom Alpha Experience ✅ (systems from 5.5)
Delivered by Phase 5.5 — `CitizenManager` + `CitizenController`:
- Animated citizens (Villager/Farmer/Soldier/Merchant)
- Palace-level population (5–60 NPCs)
- Working farms, patrolling guards, talking pairs, idle NPC states
- `KingdomVisualController`: smoke chimneys, torch flames, wind banners
- `BuildingVisualState`: 4-state machine, visual progression by level

Phase 5.6 adds:
- `AlphaVFXController.SpawnTorchFlame()` / `SpawnChimneySmoke()` — production VFX Graph vs old Shuriken
- `AlphaUIController` applies global skin to all kingdom panels on scene load

### U5.6.5 — World Entity Rework ✅ (systems from 5–5.5)
- `ResourceNodeVisual`: unique silhouettes per node, harvest/depleted/respawn animations, crystal emissive pulse
- `MonsterVisualController`: full Animator Controller contract, roaming/idle/alert/death dissolve
- Kingdom tiers (Village/Town/City/Fortress/Capital): driven by Palace level in `KingdomVisualController`
- `ArtImportManager` auto-assigns production prefabs replacing grey-box fallbacks on import

### U5.6.6 — CinematicCameraManager ✅
- Login cinematic: 8s aerial pan across the world
- Kingdom entry: altitude arc from 80u to 18u over 5s, orbiting
- Flythrough: waypoint-to-waypoint with inspector-assigned `Transform[]`
- Battle victory: zoom-in (FOV 20°) then pull-back orbit
- World→Kingdom transition: 2.5s zoom into kingdom before scene load
- Entity focus: smooth orbit at configurable distance + FOV
- Screenshot mode: HUD hidden, 4× super-resolution capture
- All cinematics wrapped with `OnCinematicStarted` / `OnCinematicEnded` events

### U5.6.7 — VFX Alpha Pass ✅
VFX Graph pooled effects:
| Effect | Trigger |
|--------|---------|
| `selectionRingVFX` | Entity selected |
| `entitySelectedPulseVFX` | Entity re-selected pulse |
| `constructionDustVFX` | Building under construction |
| `scaffoldingSparkVFX` | Construction sparks |
| `buildingCompleteCelebrationVFX` | Building finished |
| `resourceGatherSparkleVFX` | Resource gathered |
| `resourceNodeDepletedVFX` | Node depleted |
| `monsterDeathDissolveVFX` | Monster killed |
| `monsterSpawnBurstVFX` | Monster spawned |
| `marchArrivalBurstVFX` | March arrives at destination |
| `marchDustTrailVFX` | Attached to moving march banner |
| `levelUpCelebrationVFX` | Player/building level up |
| `lootExplosionVFX` | Loot reward explosion |
| `torchFlameVFX` | Ambient torch (persistent) |
| `chimneySmokVFX` | Chimney smoke (persistent) |
| `campfireEmbersVFX` | Campfire ember glow (persistent) |

All effects pooled (`poolSizePerEffect = 8`). Persistent looped effects returned via `Release()`.

### U5.6.8 — Audio Alpha Pass ✅ (systems from 5–5.5)
- `AudioManager`: 5 channels (Music/SFX/UI/Combat/World)
- `AmbientAudioController`: 8 biome contexts, 3s cross-fade
- 16 audio clip addresses in `ArtAssetRegistry`
- `WorldEnvironmentManager` triggers audio context on time-phase change
- Weather audio: rain/storm/wind clips blend with `weatherTransitionSpeed`

### U5.6.9 — UI Alpha Pass ✅
- `AlphaUIController`: global skin via `UIThemeManager.ApplyTheme()`
- TMP font auto-applied to all TextMeshProUGUI in scene on load
- Notification system: pooled, slide-in from right, auto-dismiss after 4s
- Tooltip: delay 0.4s, context-sensitive, hides on pointer exit
- `TooltipTrigger` component: drop on any Button/Image for hover scale + tooltip
- Floating resource change numbers: +green / -red, float-up animation 1.6s
- `AnimatedPanel` base class (Phase 5.8): all panels inherit enter/exit animation

### U5.6.10 — AlphaDemoController ✅
9-step automated demo:
```
Step 1: Login               → CinematicCameraManager.PlayLoginCinematic()
Step 2: Kingdom Cinematic   → PlayKingdomEntryCinematic(kingdomRoot)
Step 3: Kingdom Interaction → FocusOn(palace) + PlayBuildingComplete()
Step 4: World Transition    → PlayWorldToKingdomTransition() + LoadScene(World)
Step 5: World Exploration   → PlayFlythroughCinematic()
Step 6: Monster Hunt        → FocusOn(monster) + PlayMonsterDeath()
Step 7: Resource Gathering  → FocusOn(node) + PlayResourceGather() + Depleted()
Step 8: Battle Report       → PlayBattleVictoryCamera() + LootExplosion() + LevelUp()
Step 9: Return Home         → PlayKingdomEntryCinematic()
```
Controls: F1 start, ESC abort, `loopDemo` bool for trade-show mode.

### U5.6.11 — Visual QA ✅
`VisualQAValidator.cs` checks:
- ❌ Missing materials (null slot + pink shader)
- ❌ Missing textures (`_MainTex`, `_BaseMap`, `_BumpMap`)
- ❌ Missing Addressables (reads `ArtImportManager.GetReport()`)
- ❌ Missing animations (null controller or 0 clips)
- ❌ Placeholder assets (Default-Material, "placeholder" in name)
- ❌ Primitive meshes (Cube/Sphere/Capsule/Cylinder/Plane/Quad)

Editor build gate: implements `IPreprocessBuildWithReport`.  Throws `BuildFailedException` if any primitive mesh found in prefabs.

### U5.6.12 — Alpha Readiness Report ✅
See `ALPHA_READINESS_REPORT.md`.

---

## 3. Art Asset Specifications (Additions)

### WorldEnvironmentManager Asset Requirements

| Asset | Address | Format | Notes |
|-------|---------|--------|-------|
| Day skybox material | `Skybox/skybox_day` | HDRI Skybox (6-sided or panoramic) | URP compatible |
| Night skybox material | `Skybox/skybox_night` | HDRI Skybox | Star field + moon |
| Storm VolumeProfile | (scene-local) | VolumeProfile | Heavy fog + desaturated CA |
| VolumeProfile Low | (scene-local) | VolumeProfile | No bloom, SSAO off |
| VolumeProfile Medium | (scene-local) | VolumeProfile | Bloom 0.5, SSAO 0.5 |
| VolumeProfile High | (scene-local) | VolumeProfile | Bloom 1.0, SSAO 1.0, DOF |
| VolumeProfile Ultra | (scene-local) | VolumeProfile | Full post-processing stack |

### CinematicCameraManager — Scene Setup
- Create empty `Waypoints` root in World scene
- Add 7 child Transforms named `Waypoint_00` through `Waypoint_06`
- Assign to `CinematicCameraManager.flythroughWaypoints[]`
- Assign `kingdomRoot` Transform in `AlphaDemoController`

### AlphaVFXController — VFX Graph Requirements
Each VFX Graph asset must:
- Use **Visual Effect Graph** (not legacy Particle System)
- Expose property: `Position (Vector3)` for world-space spawn
- Expose property: `Scale (float)` for size override
- Support Play/Stop via `VisualEffect.Play()` / `.Stop()`
- Include LOD cutoff at 200u

---

## 4. Scene Hierarchy Additions

### WorldScene.unity — New GameObjects
```
WorldScene
├── [Environment]
│   ├── WorldEnvironmentManager       ← WorldEnvironmentManager.cs
│   │   ├── SunLight                  ← Directional Light (sun)
│   │   ├── MoonLight                 ← Directional Light (moon)
│   │   ├── RainParticles             ← ParticleSystem
│   │   ├── SnowParticles             ← ParticleSystem
│   │   ├── AshfallParticles          ← ParticleSystem
│   │   └── FogParticles              ← ParticleSystem
│   └── CloudLayers
│       ├── CloudLayer_Day            ← MeshRenderer (scrolling UV)
│       └── CloudLayer_Storm          ← MeshRenderer (dark, alpha 0 default)
├── [Camera]
│   └── MainCamera                    ← CinematicCameraManager.cs
│       └── Waypoints/
│           ├── Waypoint_00 … 06
├── [VFX]
│   └── AlphaVFXController            ← AlphaVFXController.cs
│       └── VFX_Pool/
├── [Demo]
│   └── AlphaDemoController           ← AlphaDemoController.cs
├── [QA]
│   └── VisualQAValidator             ← VisualQAValidator.cs
└── [UI]
    └── AlphaUIController             ← AlphaUIController.cs
```

---

## 5. Integration Points

| Phase 5.6 Script | Calls Into |
|-----------------|------------|
| `ArtImportManager` | `KingdomVisualController.RegisterBuildingPrefab()`, `MonsterSpawnManager.RegisterMonsterPrefab()` |
| `WorldEnvironmentManager` | `AmbientAudioController.OnTimePhaseChanged()` |
| `CinematicCameraManager` | `SceneManager.LoadSceneAsync()`, canvas hide/show |
| `AlphaVFXController` | `AudioSource.PlayClipAtPoint()` |
| `AlphaUIController` | `UIThemeManager.ApplyTheme()`, `TMP_Text.font` |
| `AlphaDemoController` | `CinematicCameraManager`, `AlphaVFXController`, `SceneManager` |
| `VisualQAValidator` | `ArtImportManager.GetReport()`, `IPreprocessBuildWithReport` |

---

## 6. Quality Guardrails Enforced

| Rule | Enforcement |
|------|-------------|
| Zero cubes / capsules / primitives | `VisualQAValidator` build gate + runtime check |
| All Addressables loaded via ArtImportManager | No `Resources.Load()` calls in any new script |
| All VFX pooled | `AlphaVFXController` maintains pool per asset |
| All animations verified | `ArtImportManager` + `VisualQAValidator` |
| All materials URP-compatible | `ArtImportManager` pink-shader detection |
| Build fails if placeholders remain | `VisualQAValidator.IPreprocessBuildWithReport` |

---

*Phase 5.6 complete — 2026-06-20*
*Next phase: Phase 6 — Alliance System + Territory Control*
