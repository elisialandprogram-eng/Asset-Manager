# World.unity — Scene Setup Guide

> **Unity Developer Reference — Phase U2.1–U2.12**
> Mirror of Kingdom.unity structure; all manager singletons carried over from Bootstrap.

---

## Build Settings Entry

Add `World` scene to Build Settings at index 3 (after Bootstrap=0, Login=1, Kingdom=2).

---

## Full Scene Hierarchy

```
World
├── [Bootstrap Managers]          ← Carried from Bootstrap.unity via DontDestroyOnLoad
│   ├── GameManager
│   ├── SceneController
│   ├── SaveManager
│   ├── ConfigManager
│   ├── NetworkManager
│   ├── AuthManager
│   ├── AudioManager
│   ├── SettingsManager
│   └── AddressablesManager
│
├── WorldSceneRoot                ← New scene-local objects begin here
│   ├── WorldSceneController      ← [WorldSceneController.cs]
│   ├── WorldSelectionManager     ← [WorldSelectionManager.cs]
│   └── FogOfWarManager           ← [FogOfWarManager.cs]
│
├── Environment
│   ├── DirectionalLight          ← Sun; X=50°, Y=145°; Color (1,0.97,0.87); Intensity=1.2
│   ├── AmbientSkybox             ← URP Volume with Skybox + Ambient Occlusion
│   └── GlobalVolume              ← Post-processing: Bloom(0.4), ColorGrading(warm tones)
│
├── World Grid
│   ├── ChunkManager              ← [ChunkManager.cs]; Pool parent for all chunk GameObjects
│   │   └── ChunkPool/            ← 60 pre-allocated inactive Chunk GameObjects
│   └── WorldStreamingManager     ← [WorldStreamingManager.cs]
│
├── Entities
│   ├── KingdomsParent            ← Parent Transform for KingdomEntity pool
│   ├── MonstersParent            ← Parent Transform for MonsterEntity pool
│   ├── CrystalsParent            ← Parent Transform for CrystalEntity pool
│   └── WorldEntitySpawner        ← [WorldEntitySpawner.cs]
│
├── Camera Rig
│   └── WorldCameraRig            ← Empty pivot object; rotated (60°, -45°, 0°)
│       └── MainCamera            ← [Camera]; Orthographic; OrthoSize=50; CullingMask=All
│           └── WorldCameraController (on WorldCameraRig) ← [WorldCameraController.cs]
│
└── UI
    ├── WorldHUDCanvas            ← Screen Space — Overlay; sortOrder=0
    │   ├── WorldTopBar           ← [WorldTopBar.cs]; anchored top-left→top-right
    │   │   ├── KingdomNameLabel  ← TMP; top-left corner
    │   │   ├── WorldNameLabel    ← TMP; top-center
    │   │   ├── MyKingdomButton
    │   │   ├── SearchButton
    │   │   ├── BookmarksButton
    │   │   └── CenterButton
    │   ├── WorldBottomBar        ← [WorldBottomBar.cs]; anchored bottom
    │   │   ├── MarchesButton     (disabled, Phase 7)
    │   │   ├── AllianceButton    (disabled, Phase 6)
    │   │   ├── EventsButton      (disabled, Phase 9)
    │   │   ├── RankingsButton    (disabled, Phase 5+)
    │   │   └── MailButton        (disabled, Phase 6+)
    │   └── WorldHUD              ← [WorldHUD.cs]; bottom-left HUD cluster
    │       ├── CoordLabel        ← "(1024, 1024)"
    │       ├── ChunkLabel        ← "Chunk (16, 16)"
    │       ├── BiomeLabel        ← "Ancient Forest"
    │       ├── ZoneLabel         ← "Mid Reaches"
    │       └── ZoomLabel         ← "Zoom: 50"
    │
    ├── WorldPopupCanvas          ← Screen Space — Overlay; sortOrder=10
    │   ├── WorldInfoPanel        ← [WorldInfoPanel.cs]; right-side slide panel
    │   │   ├── CloseButton
    │   │   ├── KingdomSubPanel   ← activated when kingdom selected
    │   │   │   ├── KingdomNameLabel
    │   │   │   ├── KingdomPowerLabel
    │   │   │   ├── KingdomCoordsLabel
    │   │   │   └── EnterKingdomButton
    │   │   ├── MonsterSubPanel   ← activated when monster selected
    │   │   │   ├── MonsterNameLabel
    │   │   │   ├── MonsterTierLabel
    │   │   │   ├── MonsterHpLabel
    │   │   │   └── MonsterHpBar (Slider)
    │   │   └── CrystalSubPanel   ← activated when crystal selected
    │   │       ├── CrystalTypeLabel
    │   │       ├── CrystalYieldLabel
    │   │       └── CrystalStatusLabel
    │   ├── CoordinateNavigator   ← [CoordinateNavigator.cs]; center modal
    │   │   ├── XField (TMP_InputField, integer)
    │   │   ├── YField (TMP_InputField, integer)
    │   │   ├── GoButton
    │   │   ├── ErrorLabel (TMP)
    │   │   └── PreviewLabel (TMP)
    │   ├── NetworkErrorPopup     ← [NetworkErrorPopup.cs]
    │   ├── ReconnectPopup        ← [ReconnectPopup.cs]
    │   └── NotificationManager   ← [NotificationManager.cs]
    │
    └── LoadingOverlay            ← Fullscreen black panel; active during bootstrap
        └── LoadingSpinner        ← [LoadingSpinner.cs]
```

---

## Inspector Wiring Tables

### WorldSceneController

| Field | Assign |
|-------|--------|
| streamingManager | WorldStreamingManager (in World Grid) |
| entitySpawner | WorldEntitySpawner (in Entities) |
| fogOfWar | FogOfWarManager (in WorldSceneRoot) |
| worldCamera | WorldCameraController (on WorldCameraRig) |
| worldHUD | WorldHUD (in WorldHUDCanvas) |
| worldTopBar | WorldTopBar (in WorldHUDCanvas) |
| worldBottomBar | WorldBottomBar (in WorldHUDCanvas) |
| loadingOverlay | LoadingOverlay |
| hudCanvas | WorldHUDCanvas |
| popupCanvas | WorldPopupCanvas |
| mapPollIntervalSeconds | 60 |
| spawnPollIntervalSeconds | 30 |

### WorldCameraController (on WorldCameraRig)

| Field | Value |
|-------|-------|
| boundsXMin / Max | −5120 / 5120 |
| boundsZMin / Max | −5120 / 5120 |
| minOrthoSize | 10 |
| maxOrthoSize | 120 |
| initialOrthoSize | 50 |
| scrollSensitivity | 6 |
| dragSensitivity | 1.0 |
| keyboardPanSpeed | 80 |
| edgeScrollSpeed | 60 |
| flyToSpeed | 200 |

### ChunkManager

| Field | Value |
|-------|-------|
| chunkPrefab | Assets/Prefabs/World/Chunk.prefab |
| initialPoolSize | 60 |
| maxPoolSize | 121 |
| chunkParent | ChunkPool (child of ChunkManager) |

### WorldStreamingManager

| Field | Value |
|-------|-------|
| loadRadius | 5 |
| unloadRadius | 7 |
| scanInterval | 0.5 |
| maxLoadsPerFrame | 2 |
| maxUnloadsPerFrame | 4 |
| worldCamera | MainCamera |

### WorldEntitySpawner

| Field | Value |
|-------|-------|
| kingdomEntityPrefab | Assets/Prefabs/World/KingdomEntity.prefab |
| monsterEntityPrefab | Assets/Prefabs/World/MonsterEntity.prefab |
| crystalEntityPrefab | Assets/Prefabs/World/CrystalEntity.prefab |
| kingdomPoolSize | 100 |
| monsterPoolSize | 300 |
| crystalPoolSize | 200 |
| kingdomParent | KingdomsParent |
| monsterParent | MonstersParent |
| crystalParent | CrystalsParent |

### WorldSelectionManager

| Field | Assign |
|-------|--------|
| infoPanel | WorldInfoPanel |

### WorldInfoPanel

| Field | Assign |
|-------|--------|
| panelRoot | WorldInfoPanel root RectTransform |
| closeButton | CloseButton |
| kingdomPanel | KingdomSubPanel |
| monsterPanel | MonsterSubPanel |
| crystalPanel | CrystalSubPanel |
| All label fields | Assign matching TMP labels by name |
| enterKingdomButton | EnterKingdomButton |

### CoordinateNavigator

| Field | Assign |
|-------|--------|
| panelRoot | CoordinateNavigator root |
| xField | XField (TMP_InputField) |
| yField | YField (TMP_InputField) |
| goButton | GoButton |
| errorLabel | ErrorLabel |
| previewLabel | PreviewLabel |
| worldCamera | WorldCameraController |

### WorldTopBar

| Field | Assign |
|-------|--------|
| myKingdomButton | MyKingdomButton |
| searchButton | SearchButton |
| centerButton | CenterButton |
| bookmarksButton | BookmarksButton |
| coordinateNavigator | CoordinateNavigator |
| worldCamera | WorldCameraController |
| sceneController | WorldSceneController |

### WorldHUD

| Field | Assign |
|-------|--------|
| coordLabel | CoordLabel |
| chunkLabel | ChunkLabel |
| biomeLabel | BiomeLabel |
| zoneLabel | ZoneLabel |
| zoomLabel | ZoomLabel |
| worldCamera | WorldCameraController |

---

## Chunk Prefab Setup (Assets/Prefabs/World/Chunk.prefab)

```
Chunk (empty GameObject)
├── MeshFilter
├── MeshRenderer         ← Material: TerrainVertexColor (URP Lit, vertex color enabled)
├── MeshCollider         ← for raycast selection
├── Chunk.cs
└── TerrainChunk.cs
```

**TerrainVertexColor Material:**
- Shader: Universal Render Pipeline/Lit
- Source: Vertex Color
- Enable "Vertex Colors" in shader property block

---

## Entity Prefab Structure

### KingdomEntity.prefab

```
KingdomEntity
├── SelectableEntity.cs
├── KingdomEntity.cs
├── SelectionRing        ← Flat disc mesh, inactive by default
├── HoverRing            ← Flat ring mesh, inactive by default
├── OwnKingdomIndicator  ← Crown icon / gold glow particle
├── VillageMesh          ← Activated at power < 100
├── TownMesh             ← Activated at power 100–499
├── CastleMesh           ← Activated at power 500–1999
├── CapitalMesh          ← Activated at power ≥ 2000
└── WorldSpaceCanvas
    └── NameLabel (TMP)
```

### MonsterEntity.prefab

```
MonsterEntity
├── SelectableEntity.cs
├── MonsterEntity.cs
├── SelectionRing
├── HoverRing
├── AliveMesh            ← Lair geometry, active when HP > 0
├── DeadMarker           ← Skull marker, active when HP ≤ 0
├── TierRenderer         ← Colored by monster tier
└── WorldSpaceCanvas
    ├── NameLabel (TMP)
    └── HpBar (Slider)
```

### CrystalEntity.prefab

```
CrystalEntity
├── SelectableEntity.cs
├── CrystalEntity.cs
├── SelectionRing
├── HoverRing
├── FullCrystal          ← Full crystal mesh + emission
├── DepletedCrystal      ← Grey stub mesh
├── CrystalLight         ← Point light, intensity=2, color=crystalType
└── WorldSpaceCanvas
    └── NameLabel (TMP)
```

---

## Performance Notes (U2.11)

| Target | Value | How Achieved |
|--------|-------|-------------|
| WebGL FPS | 60 | Object pools, 2 loads/frame cap, LOD on entities |
| Mobile FPS | 30 | Streaming radius tunable; reduce to 3 for mobile |
| Visible entities | 500+ | Frustum culling via Unity camera; LOD on entity meshes |
| Frame spike budget | < 20ms | ChunkManager spreads loads over frames |
| Streaming memory | ~1 MB FoW | Bitfield array in FogOfWarManager |
| Active chunk count | ≤ 121 | (2×5+1)² max loaded simultaneously |

---

## World → Kingdom Transition (U2.12)

```
Player clicks own kingdom entity on world map
  └── KingdomEntity.OnSelected()
       └── WorldSelectionManager.SelectKingdom(entity)
            └── WorldInfoPanel.ShowKingdom(data)
                 └── EnterKingdomButton shown (own kingdom only)
                      └── Player clicks "Enter Kingdom"
                           └── WorldSelectionManager.EnterSelectedKingdom()
                                └── SaveManager.KEY_KINGDOM_ID = kingdomId
                                     └── SceneController.GoToKingdom()
                                          └── Kingdom.unity loads
                                               └── KingdomSceneController reads KEY_KINGDOM_ID
```

Kingdom → World transition:
```
Kingdom scene "Back to World" button
  └── SceneController.GoToWorld()
       └── World.unity loads
            └── WorldSceneController.Bootstrap() centers camera on own kingdom
```

---

## Streaming Flow Diagram

```
Start: WorldStreamingManager.Initialize(grid, seed, camera)
  │
  └─ PrewarmPool() — 60 Chunk GameObjects allocated
       │
       └─ StreamLoop() coroutine (every 0.5s):
            1. Get camera's ChunkCoordinate
            2. If chunk changed: ScheduleLoadUnload(chunk)
               ├── Desired = all chunks within 5 chunk radius
               ├── Enqueue unloaded desired chunks → _loadQueue
               └── Enqueue active chunks outside 7 radius → _unloadQueue
            3. Pop up to 2 loads from _loadQueue
               └── ChunkManager.LoadChunk(coord, seed)
                    └── Chunk.Initialize(coord, seed)
                         └── TerrainChunk.Generate(coord, seed)
                              └── TerrainGenerator.Generate() → mesh
            4. Pop up to 4 unloads from _unloadQueue
               └── ChunkManager.UnloadChunk(coord)
                    └── Chunk.Recycle() → pool
```
