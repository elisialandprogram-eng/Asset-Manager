---
name: Unity migration
description: React portal is account/portal-only; all game rendering moved to Unity 6 LTS + URP; browser engine permanently deleted
---

## Rule
The React app (`artifacts/eternal-kingdoms/`) is a **portal and account frontend only**. It must never contain game rendering code (PixiJS, Three.js, Canvas engines, etc.).

## What was deleted
- `src/game/` — entire PixiJS engine directory (GameEngine, AssetManager, CameraController, WorldScene, KingdomScene, IsoMath, etc.)
- `src/components/world3d/` — World3DCanvas, KingdomScene3D, spriteAssets.ts, noiseUtils.ts
- `src/components/game/WorldCanvas.tsx` — legacy 2D canvas world map
- `src/components/game/WorldInfoPanel.tsx` — entity detail panel (depended on WorldCanvas types)
- `pixi.js` dependency removed from package.json

## What replaced them
- `/kingdom` route → `pages/kingdom.tsx` — "Kingdom gameplay is available in the Unity Client."
- `/world` route → `pages/world-map.tsx` — "World exploration is available in the Unity Client."
- Dashboard viewport → Unity Client placeholder with "Launch Unity Client" button
- `unity-client/` directory — full Unity 6 LTS architecture documentation (8 files)
- `SUPABASE_MIGRATION_PLAN.md` — database migration plan

## Why
GPU is disabled in Replit sandbox (GL_VENDOR=Disabled). Browser rendering was a dead end. Unity 6 LTS + URP is the production game client targeting WebGL, Android, iOS.

## How to apply
- Never add rendering libraries (PixiJS, Three.js, Babylon.js, etc.) to artifacts/eternal-kingdoms
- The portal keeps: Login, Register, Dashboard (ResourceHud, KingdomOverview, BuildModal, UpgradeModal, ActivityFeed), Account pages
- All new visual gameplay goes in unity-client/
- unity-client/UNITY_API_CONTRACT.md must be updated when new API endpoints are added
