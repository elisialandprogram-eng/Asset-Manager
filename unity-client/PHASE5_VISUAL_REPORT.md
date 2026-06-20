# Eternal Kingdoms — Phase 5 Visual Production Report

> **Phase:** Unity Phase 5 — Visual Production Alpha
> **Date:** 2026-06-20
> **Target quality:** League of Kingdoms / Rise of Kingdoms / Call of Dragons

---

## 1. Visual Architecture Report (U5.1)

### VisualSettingsManager

**Location:** `Assets/Scripts/Visual/VisualSettingsManager.cs`

Four quality tiers implemented with full runtime switching:

| Tier | Shadow Distance | Cascades | MSAA | HDR | Bloom | SSAO | Fog |
|------|----------------|----------|------|-----|-------|------|-----|
| Low | 50u | 1 | ×1 | ❌ | Off | Off | ❌ |
| Medium | 100u | 2 | ×2 | ✅ | 0.4 | 0.5 | ❌ |
| High | 150u | 4 | ×4 | ✅ | 0.7 | 0.8 | ✅ |
| Ultra | 250u | 4 | ×8 | ✅ | 1.0 | 1.0 | ✅ |

**Auto-detection logic:**
- WebGL → Medium (browser memory limits)
- Mobile GPU < 2 GB VRAM → Low
- Mobile GPU 2-4 GB → Medium
- Mobile GPU ≥ 4 GB → High
- Desktop GPU ≥ 8 GB VRAM → Ultra

**URP Post Processing Stack (High/Ultra):**
- Bloom (Physically Based, Scatter = 0.7)
- Screen Space Ambient Occlusion (SSAO, Intensity 0.8)
- Color Adjustments (Contrast +5, Saturation +8, Brightness 0)
- Tonemapping (ACES)
- Vignette (Intensity 0.3, Rounded)
- Chromatic Aberration (Intensity 0.15 — subtle, not distracting)
- Depth of Field (Bokeh, focal length 120mm on kingdom view)
- Film Grain (Intensity 0.05)

**Inspector wiring required:**
- Assign `VolumeProfile_Low/Medium/High/Ultra` assets
- Assign `UniversalRenderPipelineAsset_Low/Medium/High/Ultra` assets
- Assign `globalVolume` reference

---

## 2. Terrain Report (U5.2)

### BiomeTerrainController + TerrainSplatMapper

**Files:**
- `Assets/Scripts/Terrain/BiomeTerrainController.cs`
- `Assets/Scripts/Terrain/TerrainSplatMapper.cs`

### 7 Biome Definitions

Each biome is a `BiomeData` ScriptableObject with full art specification:

#### Grasslands
- Ground: lush green grass, dirt path detail, wildflower scatter
- Features: gentle rolling hills, stone fences, scattered haystacks
- Ambient: warm yellow-green tint, light fog (density 0.001), bird sounds
- Fog: disabled (clear skies)

#### Forest
- Ground: dark loam, moss, leaf scatter, mushroom detail
- Features: dense canopy trees (oak, pine, birch), fallen logs, streams
- Ambient: deep shadow tint, volumetric fog (density 0.02), owl + wind sounds
- Fog: enabled (density 0.018, color: dark blue-green)

#### Snow
- Ground: packed snow, ice crack detail, frozen ground showing through
- Features: pine trees with snow caps, frozen rivers, icicle rocks
- Ambient: cold blue-white tint, near-silence, howling wind occasional stinger
- Fog: heavy (density 0.03, color: pale blue-white)

#### Desert
- Ground: sand ripple texture, cracked earth detail, dune grass scatter
- Features: sandstone ruins, dead cacti, bleached bones, mirages
- Ambient: warm orange tint, heat shimmer shader, rare desert bird
- Fog: disabled (high visibility)

#### Highlands
- Ground: highland scrub grass, heather, exposed bedrock detail
- Features: dramatic cliffs (cliff layer active ≥ 40°), highland sheep, ancient stone circles
- Ambient: strong wind audio, eagle screech stinger, cool grey-green tint
- Fog: light (density 0.008, color: grey-white)

#### Swamp
- Ground: dark mud, moss-covered ground, lily pad scatter near water
- Features: gnarled cypress trees, murky pools, broken docks, fireflies VFX
- Ambient: constant bubbling/croaking loop, green-brown tint, heavy fog
- Fog: enabled (density 0.04, color: sickly green-grey)

#### Volcanic
- Ground: black basalt, lava rock detail, ash powder scatter
- Features: lava rivers (emissive material), steam vents (particle VFX), scorched ruins
- Ambient: low rumble + hissing loop, red-orange tint, near-zero fog (smoke instead)
- Fog: light (density 0.006, color: dark orange-grey)

### Terrain Splat Mapping Algorithm
- Ground layer: base weight = 1.0 − noiseVariation
- Detail layers: Perlin noise blend (scale 0.05, strength 0.25)
- Cliff layer: active when terrain normal angle > 45° from vertical
- Cliff blending: smooth lerp between 45° and 90° slope
- Noise seed: per-chunk deterministic (hash of chunk coordinates)

### Additional Terrain Features
- **Roads:** separate mesh overlay with `roadMaterial` (cobblestone normal map)
- **Rivers:** rendered as separate water plane mesh with flow shader
- **Small lakes:** same water shader, still reflection
- **Cliffs:** handled by splat cliff layer + vertical terrain geometry
- **Terrain decals:** world decal system for battle scars, camp fire marks, symbols

**Art assets required:**
- 7 × 4 terrain layers (ground, detail1, detail2, cliff per biome) = 28 TerrainLayer assets
- Road material (cobblestone, URP Lit, tiled)
- River material (URP water shader with flow + caustics)

---

## 3. Kingdom Visuals Report (U5.4)

### KingdomVisualController + BuildingVisualState

**Files:**
- `Assets/Scripts/Kingdom/KingdomVisualController.cs`
- `Assets/Scripts/Kingdom/BuildingVisualState.cs`

### Kingdom Layout Architecture

```
                    ╔══════════╗
                    ║  PALACE  ║  ← Dominates skyline, tallest structure
                    ╚══════════╝
              ╔═══╗   ╔═══╗   ╔═══╗
              ║ I ║   ║ I ║   ║ I ║  ← Inner Ring (economic)
              ╚═══╝   ╚═══╝   ╚═══╝
         ╔══╗   ╔══╗        ╔══╗   ╔══╗
         ║O ║   ║O ║        ║O ║   ║O ║  ← Outer Ring (production)
         ╚══╝   ╚══╝        ╚══╝   ╚══╝
    ╔═╗              ╔═══╗              ╔═╗
    ║M║              ║   ║ ← GATE      ║M║  ← Military Ring (barracks/stables)
    ╚═╝              ╚═══╝              ╚═╝
    ════════ WALLS (unlocked at Palace level 5) ════════
```

### Building Visual State Machine

| State | Visual | Duration |
|-------|--------|----------|
| EmptyLot | Bare earth, grass, foundation stakes | Permanent until construction |
| Foundation | Stone foundation slab, surveying poles | Frame |
| Constructing | Scaffolding mesh, animated workers (shader), dust VFX | Build timer |
| Complete(N) | Level-N building mesh, idle animations | Permanent |

**Transitions fire:**
- `construction_dust` ParticleSystem during Constructing state
- `building_complete` VFX (fireworks + flash) on Complete transition

### Environmental Kingdom Details
- **Flags:** 8 cloth-simulation flag positions, tinted by kingdom color
- **Banners:** 4 hanging banner positions at palace entrance
- **Torches:** 16 torch positions, all with flicker PointLight + smoke ParticleSystem
- **Roads:** cobblestone procedural mesh connecting all building slots
- **Walls:** modular wall sections (10 prefabs), gate (1 prefab), 4 corner towers
  - Wall unlock: Palace level 5
  - Tower upgrade: additional cannon/archer meshes at Palace level 10+
- **Smoke:** chimneys on forge/barracks buildings, gentle smoke ParticleSystem

**Art prefabs required per building (6-10 per building type × 15 building types):**
- 1 × EmptyLot prefab
- 1 × Foundation prefab
- 3 × ConstructionStage prefabs (early/mid/late scaffolding)
- Up to 10 × CompletedLevel prefabs (Level 1–10 appearance)

---

## 4. Resource Node Visuals Report (U5.5)

### ResourceNodeVisual

**File:** `Assets/Scripts/World/ResourceNodeVisual.cs`

### Node Type Visual Specifications

| Node | Idle VFX | Harvest VFX | Depleted |
|------|----------|-------------|---------|
| Farm | Gentle grass sway, butterfly particles | Wheat chaff flying, harvester animation | Barren field, crow |
| Lumber Camp | Wind tree sway | Sawdust particles, axe animation | Stumps only, no trees |
| Stone Quarry | Dust motes floating | Rock chip particles, chisel impact | Flat quarry, no boulders |
| Iron Mine | Torch flicker at entrance | Sparks inside shaft, cart animation | Sealed shaft entrance |
| Gold Deposit | Gold glint particles (rare burst) | Gold dust emissive shimmer | Dark, cracked rock |
| Crystal Cluster | Animated emissive pulse (sin-wave) | Crystal shard floating VFX | Dim glow, cracked shards |

### Crystal Cluster Special Shader
The crystal uses a custom URP shader (or Shader Graph) with:
- `_EmissionColor` driven by `ResourceNodeVisual.SetCrystalEmissive()`
- `_DissolveAmount` for the depletion state (cracks appear progressively)
- Emission range: 0.5–2.0 × base color (sin-wave, period ~5s idle, ~2s harvesting)
- Tier colors: Common=`#4D7FFF`, Rare=`#B34DFF`, Epic=`#FFD21A`

### LOD Configuration
| LOD | Distance | Detail |
|-----|----------|--------|
| LOD0 | < 30u | Full mesh + all particles |
| LOD1 | < 80u | Simplified mesh, billboard particles |
| LOD2 | < 200u | Impostor sprite + 1 particle |
| Culled | > 200u | Hidden |

---

## 5. Monster Visuals Report (U5.6)

### MonsterVisualController

**File:** `Assets/Scripts/Monsters/MonsterVisualController.cs`

### Monster Categories

| Category | Examples | Scale multiplier (T5) | Idle Anim | LOD0 Distance |
|----------|---------|----------------------|-----------|---------------|
| Bandits | Bandit Captain, Marauder | ×1.0 | Patrol loop | 60u |
| Dire Wolves | Alpha Wolf, Pack Wolf | ×1.2 | Prowl crouch | 80u |
| Ogres | Stone Ogre, Frost Ogre | ×1.5 | Stomp idle | 100u |
| Ancient Guardians | Stone Colossus, Runic Sentinel | ×2.0 | Slow rotation | 150u |
| Dragons | Drake, Elder Dragon | ×3.0 | Wing flap, head sway | 200u |

### Tier Visual Tinting
| Tier | Color | Ambient Light | Aura VFX |
|------|-------|--------------|----------|
| T1 (common) | Grey `#B3B3B3` | None | None |
| T2 (uncommon) | Green `#66E666` | Subtle green 0.2 | Small green spark |
| T3 (rare) | Blue `#4D99FF` | Blue 0.3 | Blue energy wisps |
| T4 (elite) | Purple `#B34DFF` | Purple 0.4 | Purple crackle |
| T5 (boss) | Red-gold `#FF6619` | Red 0.5 | Fire/ember particles |

### Animation Events
- **Spawn:** Emerge from ground with dirt/rubble VFX (spawn trigger on Animator)
- **Idle loop:** Subtle breathing, random head turns (Animator with random idle clips)
- **Selected:** Ring expands and pulses at `selectionPulseSpeed = 2.0 Hz`
- **Death:** `DieTrigger` → 1.8s dissolve shader fade → `gameObject.SetActive(false)`

### Death Dissolve Shader
Custom URP shader (Shader Graph):
- `_DissolveAmount` 0→1 over `dissolveDuration = 1.8s`
- Dissolve edge emits bright color (tier color) as it retreats
- Noise texture drives dissolve pattern (not linear wipe)

---

## 6. UI Skinning Report (U5.8)

### UIThemeManager + AnimatedPanel

**Files:**
- `Assets/Scripts/UI/UIThemeManager.cs`
- `Assets/Scripts/UI/AnimatedPanel.cs`

### Visual Language

**Dark Medieval Fantasy** — inspired by Diablo 4 / Rise of Kingdoms UI:

| Element | Style |
|---------|-------|
| Panel background | Dark stone texture, 9-slice, slight vignette edges |
| Panel border | Gold ornament trim, 9-slice (`panelBorderGold` sprite) |
| Buttons | Aged leather/wood background, gold border highlight on hover |
| Hover glow | Gold bloom on button hover (gold glow material) |
| Font | Serif fantasy font (TMP) — gold for headings, cream for body, red for alerts |
| Icons | Framed in rarity-color borders (grey/green/blue/purple/gold) |
| Scrollbars | Stone-groove track, aged gold handle |
| Tooltips | Parchment texture background, sepia text |

### Component Architecture

```
ThemedButton   — Button + background Image + optional border Image
ThemedPanel    — Panel Image (dark/mid/light variant) + border Image
ThemedLabel    — TMP text with role: Primary/Secondary/Gold/Alert
AnimatedPanel  — Base class all panels: Show()/Hide() with eased animation
```

### AnimatedPanel Animation Types

| Mode | Enter | Exit |
|------|-------|------|
| ScaleIn | Scale 0.88→1.0 + EaseOutBack + fade in | Scale 1.0→0.95 + fade out |
| SlideUp | Slide from −60px with EaseOutBack | Slide to −30px + fade |
| SlideRight | Slide from +60px with EaseOutBack | Slide to +30px + fade |
| Fade | Alpha 0→1 | Alpha 1→0 |

**Duration:** Show 220ms / Hide 160ms (unscaled time — works during pause)

### Panels Using AnimatedPanel

All Phase 4 + 5 panels inherit AnimatedPanel:
- MonsterAttackPanel (SlideUp)
- BattleReportPanel (ScaleIn)
- HospitalPanel (SlideRight)
- InventoryPanel (SlideRight)
- BuildingUpgradePanel (ScaleIn)
- HUDNotificationBanner (SlideUp)

**Inspector wiring required:**
- Create `UIThemeData` ScriptableObject, populate all color/sprite/font fields
- Assign `rootCanvases` array in UIThemeManager
- Add `ThemedButton/ThemedPanel/ThemedLabel` components to all UI objects

---

## 7. Audio Report (U5.9)

### AudioManager (Phase 5 Extension) + AmbientAudioController

**Files:**
- `Assets/Scripts/Managers/AudioManager.cs` (extended with UI/Combat/World channels)
- `Assets/Scripts/Audio/AmbientAudioController.cs`

### Audio Channel Architecture

| Channel | Source | Volume Control | Use |
|---------|--------|---------------|-----|
| Music | `musicSource` | `MusicVolume` | Background BGM, cross-fades between tracks |
| SFX | `sfxSource` | `SfxVolume` | Generic one-shot SFX |
| UI | `_uiSource` | `UIVolume` | Button click, panel open/close, notification |
| Combat | `_combatSource` | `CombatVolume` | Sword clash, spell impact, roar, explosion |
| World | `_worldSource` | `WorldVolume` | March arrival, building complete, crystal chime |
| Ambient A/B | Cross-fade pair | `AmbientAudioController` | Biome loops |
| Ambient Stinger | `stingerSource` | `AmbientAudioController` | Biome-specific one-shots |

### Ambient Context Library

| Context | Loop | Stinger | Notes |
|---------|------|---------|-------|
| Kingdom | Fireplace + crowd murmur | Bell tower chime | Plays in Kingdom scene |
| Grasslands | Wind + birds + insects | Shepherd's flute note | Light, pastoral |
| Forest | Deep wind + owl + rustling | Branch snap | Dark, mysterious |
| Snow | Howling wind + silence | Ice crack | Lonely, vast |
| Desert | Hot wind + sand | Desert bird | Dry, sparse |
| Highlands | Strong wind + eagle | Rock tumble | Epic, open |
| Swamp | Bubbling + frogs + drips | Frog burst | Eerie, wet |
| Volcanic | Rumble + hissing steam | Lava bubble pop | Oppressive, dangerous |

**Monster proximity ambient:**
- 3D AudioSource attached to monster GameObject
- Spatial blend = 1.0 (full 3D)
- Min distance = 5u, Max distance = 50u (logarithmic rolloff)
- Growl clip loops while monster is active

### Music Tracks Required
- `music_main_menu` — epic orchestral theme (2–3 min loop)
- `music_kingdom_peaceful` — ambient medieval, harps/lute (2 min loop)
- `music_kingdom_combat` — urgent strings + drums (90s loop, plays during active marches)
- `music_world_explore` — adventurous, moderate tempo (3 min loop)
- `music_boss_fight` — intense battle theme (2 min loop)
- `music_victory` — fanfare sting (15s, no loop)

### UI SFX Required
- `panel_open`, `panel_close` (whoosh + settle)
- `button_click`, `button_hover` (subtle tick)
- `notification_banner` (soft bell)
- `reward_appear` (coin shimmer)
- `march_sent` (horn fanfare)
- `error` (low thud)

---

## 8. Performance Report (U5.12)

### PerformanceManager

**File:** `Assets/Scripts/Performance/PerformanceManager.cs`

### Targets

| Platform | FPS Target | Entities | Draw Calls Budget |
|----------|-----------|----------|------------------|
| WebGL Desktop | 60 FPS | 500 | < 500 |
| Android (mid) | 30 FPS | 300 | < 200 |
| iOS (A14+) | 30 FPS | 400 | < 300 |

### Optimization Stack

**1. LOD Groups — all entities (EnvironmentDecorationManager)**
- LOD0 full detail < 15% screen height
- LOD1 simplified < 4% screen height
- Culled below billboard threshold

**2. GPU Instancing**
- Enabled on all decoration materials (`mat.enableInstancing = true`)
- All tree/rock/shrub prefabs share single mesh + material per type
- Expected batch reduction: 60–80% for decoration-heavy chunks

**3. Object Pooling**
- `DecorationPool` per prefab type (min 2, grows on demand)
- `VFXPool` per effect key (min 2-5 depending on VFX type)
- `MarchEntity` pool (max 50 simultaneous marches)

**4. Chunk Streaming Budget**
- Max 30 decoration objects spawned per frame (async coroutine)
- Max 2 chunk loads per frame (existing WorldStreamingManager budget)
- Decoration stripped immediately on chunk unload

**5. Occlusion Culling**
- Unity occlusion bake required for Kingdom scene
- World scene: dynamic occlusion via camera frustum + LOD culling

**6. Adaptive Quality (PerformanceManager)**
- FPS sampled every 1 second (5-sample rolling average)
- If FPS < 75% of target:
  - `QualitySettings.lodBias` reduced from 1.0 → 0.5
  - Shadow distance reduced by 50%
  - ParticleSystem.maxParticles halved
- If FPS recovers to > 95% target: settings restored
- Player override (VisualSettingsManager) disables adaptive mode

**7. Texture Budget**
- Terrain textures: 512×512 normal, 1024×1024 albedo
- Building textures: 512×512 per atlas (4 buildings per atlas)
- Character/monster: 256×256 on mobile, 512×512 desktop
- All textures: ASTC compression on mobile, DXT5 on desktop/WebGL

**8. Shadow Budget**
- All decoration trees: shadow casting disabled (only LOD0 within 40u)
- Monsters: receive + cast shadows
- Buildings: cast only, no self-shadow on mobile

---

## Environment Decoration System Report (U5.3)

### EnvironmentDecorationManager

**File:** `Assets/Scripts/Environment/EnvironmentDecorationManager.cs`

### Decoration Categories by Biome

| Biome | Trees/chunk | Shrubs/chunk | Grass/chunk | Rocks/chunk | Ruins/chunk |
|-------|------------|-------------|------------|------------|-------------|
| Grasslands | 15 | 20 | 30 | 8 | 1 |
| Forest | 40 | 30 | 20 | 5 | 2 |
| Snow | 8 | 5 | 3 | 12 | 1 |
| Desert | 2 | 8 | 5 | 15 | 3 |
| Highlands | 5 | 15 | 25 | 20 | 2 |
| Swamp | 20 | 25 | 15 | 5 | 3 |
| Volcanic | 0 | 3 | 0 | 25 | 4 |

### Exclusion Zones
- Kingdom tile center: 20u radius cleared
- Resource node center: 8u radius cleared
- Road paths: 3u width cleared
- Water bodies: objects below `waterExclusionHeight = 0.5u` excluded

### Procedural Variation
- Scale: ±20% random per instance
- Rotation: random Y axis per instance
- Position: Perlin-seeded offset from grid within placement radius
- Seed: deterministic per chunk coordinate (reproducible world)

---

## March Visualization Report (U5.7)

### MarchBannerEntity

**File:** `Assets/Scripts/World/MarchBannerEntity.cs`

Replaces the Phase 3 simple sphere marker with:

| Component | Description |
|-----------|-------------|
| Kingdom banner | Cloth-simulation mesh, tinted by kingdom primary color |
| Formation icon | Sprite billboard: attack/gather/reinforce/scout icons |
| Hero portrait | Sprite billboard, visible only when hero assigned to march |
| Dust trail | TrailRenderer + ParticleSystem follows march path |
| Destination beacon | Animated ring pulsing at target position |
| World HUD canvas | Billboard canvas showing ETA countdown + troop count |
| State color | Banner border: blue=outbound, green=gathering, amber=returning |

### HUD Layout (world-space canvas)
```
┌─────────────────────┐
│  [FormationIcon] [HeroPortrait]  │
│  [KingdomBanner mesh]            │
│  ETA: 4m 32s                     │
│  ⚔ 1,500 troops                  │
└─────────────────────┘
```

---

## Addressables Pipeline Report (U5.11)

### AssetCatalogManager

**File:** `Assets/Scripts/Content/AssetCatalogManager.cs`

### Addressable Groups

| Group | Labels | Preloaded? | Content |
|-------|--------|-----------|---------|
| UI | `UI` | ✅ On start | Theme sprites, fonts, panel backgrounds |
| VFX | `VFX` | ✅ On start | All particle prefabs |
| Audio/SFX | `Audio/SFX` | ✅ On start | UI + combat SFX |
| Buildings | `Buildings` | On demand | Building prefabs by registry ID |
| Monsters | `Monsters` | On demand | Monster prefabs by registry ID |
| Units | `Units` | On demand | Hero + troop prefabs |
| Environment | `Environment` | On chunk load | Decoration prefabs |
| Terrain | `Terrain` | On biome enter | Terrain layer assets |
| Audio/Music | `Audio/Music` | On scene | Music BGM tracks |

### Key Convention

All assets addressed by Asset Registry ID:
```
Buildings/building_palace_001
Buildings/building_farm_001
Monsters/monster_wolf_001
Units/hero_commander_001
VFX/monster_defeat
Audio/ambient_forest_loop
```

### NFT Override Flow
1. `inventoryRepository` / NFT bridge returns `nftContractAddress` + `nftContractTokenId`
2. `NFTBridge.GetAssetUrl(contractAddress, tokenId)` resolves to IPFS/Arweave URL
3. `AssetCatalogManager.Invalidate(address)` flushes cached default asset
4. Next `LoadAsync()` call fetches from remote URL instead

---

*All Phase 5 C# scripts are production-ready architecture. Art assets (meshes, textures, audio clips, particle prefabs) must be created by the art team and assigned via Inspector references or Addressable addresses matching the key conventions documented above.*
