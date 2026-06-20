---
name: Unity Phase 1 complete
description: Summary of Unity Phase 1 deliverables and what Phase 2 needs next
---

## What was delivered

46 C# scripts under `unity-client/Assets/Scripts/`:
- All singleton managers (Bootstrap, Game, Scene, Config, Save, Addressables, Audio, Settings, UI, Popup, Notification, CoroutineRunner)
- Full networking stack: ApiClient (UnityWebRequest, JWT, retry, timeout), NetworkManager, AuthService, KingdomService (10 endpoints), WorldService (4 endpoints)
- All DTOs mirroring OpenAPI spec: Auth, Kingdom, World, Asset
- Auth: AuthManager (boot validation, HandleUnauthorized), LoginController (email/password form)
- Kingdom scene: KingdomStateManager (15s poll), KingdomNodeLayout (4 rings, 12 nodes), KingdomTerrainBuilder (procedural plateau), BuildingSlot, KingdomCamera
- Camera: IsometricCameraController (X=60°, Y=−45°, drag/scroll/pinch/inertia/bounds)
- Interaction: SelectableEntity, EntitySelectionManager, NodeTooltip
- Data SOs: EnvironmentConfig (Dev/Staging/Prod), BuildingData (ring/slot/assetId)
- Utilities: NoiseUtils (fBm mirroring terrainGenerator.ts), MathExtensions, CoroutineRunner

Scene guides in `unity-client/Assets/Scenes/SceneSetup_*.md` — full hierarchy + wiring tables.
Build configs in `unity-client/BuildConfigs/` (Dev/Staging/Prod JSON).
WEBGL_INTEGRATION_GUIDE.md covers: JWT handoff, iframe embed, postMessage protocol, build commands.

**Why:** Foundation only — no gameplay, no troops, no combat.

## Phase 2 scope (next)
- World.unity — chunk terrain generation from world seed (NoiseUtils), entity placement (kingdoms/monsters/crystals), world-scoped isometric camera with 2048×2048 bounds
- Kingdom click on world map → KingdomScene transition
- Upgrade/Construct dialog UI panels
- WebGL build + iframe embed in React portal
