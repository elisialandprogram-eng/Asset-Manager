# Unity Client — Architecture

## Design Principles

1. **API-first** — All game state lives in the PostgreSQL backend. The Unity client is a stateless view layer. It reads from the API and sends actions; it never owns authoritative state.
2. **Manager pattern** — Top-level singleton managers (`GameManager`, `NetworkManager`, `AuthManager`, `WorldManager`, `KingdomManager`) own their subsystem lifecycle.
3. **ScriptableObject config** — All game balance values (`gameBalance`) are driven by ScriptableObjects populated from the API `/kingdoms/:id/state` response. No hardcoded values.
4. **Addressable assets** — All art is loaded via Addressables for platform-agnostic streaming on WebGL, Android, and iOS.
5. **URP throughout** — All materials use URP Lit/Unlit shaders. No built-in pipeline materials.

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs           — App lifecycle, scene bootstrap
│   │   ├── Config/
│   │   │   └── ApiConfig.cs         — Backend URL, timeout, retry config
│   │   └── Extensions/              — General C# utilities
│   ├── Networking/
│   │   ├── ApiClient.cs             — HTTP request wrapper (UnityWebRequest)
│   │   ├── WebSocketClient.cs       — Socket.IO realtime bridge
│   │   └── DTOs/                    — C# mirror of OpenAPI response schemas
│   ├── Authentication/
│   │   ├── AuthManager.cs           — JWT storage, refresh, logout
│   │   └── LoginController.cs       — Login form ↔ API bridge
│   ├── World/
│   │   ├── WorldManager.cs          — World state, chunk lifecycle
│   │   ├── ChunkLoader.cs           — Async chunk load/unload
│   │   ├── TerrainChunk.cs          — Chunk mesh generation from noise
│   │   ├── EntitySpawner.cs         — Kingdom/monster/crystal entity placement
│   │   └── WorldCamera.cs           — Isometric camera controller
│   ├── Kingdom/
│   │   ├── KingdomManager.cs        — Kingdom state, building slots, queues
│   │   ├── BuildingSlot.cs          — Individual building node logic
│   │   └── KingdomCamera.cs         — Kingdom interior camera
│   ├── Buildings/
│   │   ├── BuildingData.cs          — ScriptableObject: building type config
│   │   └── BuildingController.cs    — Per-building behaviour + click handling
│   ├── Troops/
│   │   ├── TroopData.cs             — ScriptableObject: troop type config
│   │   └── TroopTrainingQueue.cs    — Training queue state + timer
│   ├── Monsters/
│   │   ├── MonsterData.cs           — ScriptableObject: monster config
│   │   └── MonsterSpawnController.cs — Spawn entity behaviour
│   ├── Resources/
│   │   └── ResourceManager.cs       — Local resource state + rate display
│   ├── Alliances/
│   │   └── AllianceManager.cs       — Alliance membership + diplomacy
│   ├── NFT/
│   │   └── NFTBridge.cs             — Asset registry ID ↔ NFT contract bridge
│   ├── UI/
│   │   ├── HUD/                     — Resource bar, timers, notifications
│   │   ├── Kingdom/                 — Building upgrade/construct dialogs
│   │   ├── World/                   — Entity info panels, threat badges
│   │   └── Common/                  — Shared buttons, modals, tooltips
│   ├── Camera/
│   │   ├── CameraController.cs      — Shared drag/zoom/inertia base
│   │   └── IsometricHelpers.cs      — World↔screen coordinate math
│   ├── Managers/
│   │   ├── AudioManager.cs          — SFX + ambient music
│   │   └── SettingsManager.cs       — Graphics quality, audio prefs
│   └── Utilities/
│       ├── NoiseUtils.cs            — fBm terrain noise (mirrors lib/game-engine)
│       └── MathExtensions.cs        — Lerp helpers, angle utils
│
├── Art/
│   ├── Terrain/                     — Ground tiles, biome detail textures
│   ├── Buildings/                   — Isometric building prefab art
│   ├── Units/                       — Troop and hero character art
│   ├── Monsters/                    — Monster models + rigs
│   ├── Resources/                   — Resource node art (farm, mine, etc.)
│   ├── Effects/                     — Particle systems (smoke, fire, sparks)
│   └── UI/                          — UI atlas, icon sprites, fonts
│
├── Prefabs/
│   ├── World/                       — Chunk, entity, marker prefabs
│   ├── Kingdom/                     — Building slot, wall, gate prefabs
│   └── UI/                          — Panel, card, button prefabs
│
├── Scenes/
│   ├── Bootstrap.unity              — Entry: auth check → route to Login or World
│   ├── Login.unity                  — Login / register flow
│   ├── World.unity                  — World map — main multiplayer view
│   └── Kingdom.unity                — Kingdom interior — owned kingdom view
│
├── ScriptableObjects/
│   ├── Buildings/                   — One SO per building type
│   ├── Troops/                      — One SO per troop type
│   └── Monsters/                    — One SO per monster type
│
└── Addressables/
    ├── Buildings/                   — Per-platform building art bundles
    ├── Terrain/                     — Biome terrain bundles
    └── UI/                          — UI atlas bundles
```

## Scene Flow

```
Bootstrap
  ├── AuthManager.HasToken() == true  →  World scene
  └── AuthManager.HasToken() == false →  Login scene

Login scene
  └── POST /auth/login → store JWT → World scene

World scene
  ├── WorldManager loads chunk 0,0 around player's kingdom
  ├── EntitySpawner places kingdoms / monsters / crystals from API
  └── Click kingdom → Kingdom scene (if own kingdom)

Kingdom scene
  ├── KingdomManager loads full kingdom state from GET /kingdoms/:id/state
  ├── BuildingSlots render buildings from state.buildings
  └── Click slot → Upgrade/Construct dialog → POST to API
```
