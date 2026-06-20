# Eternal Kingdoms — Unity 6 Setup Guide

## Required Unity Version

**Unity 6000.0.23f1** (Unity 6 LTS)

Install via **Unity Hub → Installs → Install Editor → Unity 6 (LTS)**.
Add the **WebGL Build Support** module when installing.

---

## Opening the Project

1. Open **Unity Hub**
2. Click **Open** → **Add project from disk**
3. Select the `unity-client/` folder (the folder containing `ProjectVersion.txt`)
4. Unity Hub will detect the correct editor version automatically

On first open Unity will:
- Import all packages from `Packages/manifest.json` (~5–10 min on first run)
- Compile all C# scripts
- Generate Library/ cache (not committed to git)

---

## Required Packages (auto-installed from manifest.json)

| Package | Version | Purpose |
|---|---|---|
| Universal Render Pipeline | 17.0.3 | Rendering pipeline |
| Addressables | 2.3.1 | Asset streaming |
| Input System | 1.11.2 | Keyboard/mouse/touch |
| TextMeshPro | 3.2.0-pre.10 | UI text |
| Visual Effect Graph | 17.0.3 | Particle VFX |
| Cinemachine | 3.1.2 | Camera systems |
| Unity UI | 2.0.0 | Canvas/UI |
| AI Navigation | 2.0.7 | NavMesh pathfinding |

---

## First-Time URP Setup (required once)

After opening the project, create the URP pipeline asset:

1. **Edit → Project Settings → Graphics**
2. Click **Create a Universal Render Pipeline Asset** (or use the wizard)
3. Save the generated assets into `Assets/Settings/`
4. The `GraphicsSettings.asset` will update automatically

Alternatively via menu:
```
Assets → Create → Rendering → URP Asset (with Universal Renderer)
```
Then assign it in **Edit → Project Settings → Graphics → Scriptable Render Pipeline Settings**.

---

## Scene Build Order

Scenes are registered in `ProjectSettings/EditorBuildSettings.asset`:

| Index | Scene | Purpose |
|---|---|---|
| 0 | Assets/Scenes/Bootstrap.unity | Boot / init / auth bridge |
| 1 | Assets/Scenes/Login.unity | Login UI |
| 2 | Assets/Scenes/Kingdom.unity | Kingdom 3D view |
| 3 | Assets/Scenes/World.unity | World map |

---

## Building for WebGL

### In Unity Editor

1. **File → Build Settings**
2. Select **WebGL** platform → **Switch Platform**
3. Set **Compression Format**: Gzip (or Brotli for production)
4. Set **Publishing Settings → Decompression Fallback**: ✓ enabled
5. Click **Build** → select output folder

### Output Structure

Unity produces:
```
<output>/
  index.html
  Build/
    <name>.loader.js
    <name>.data.gz
    <name>.framework.js.gz
    <name>.wasm.gz
```

### Deploying to the Web App

Copy the entire output into:
```
artifacts/eternal-kingdoms/public/unity/
```

The web launcher at `/dashboard` will load `/unity/index.html` automatically.

---

## Building via GameCI / GitHub Actions

A GameCI workflow is recommended. Example `.github/workflows/unity-build.yml`:

```yaml
name: Unity WebGL Build

on:
  push:
    branches: [main]
    paths:
      - 'unity-client/**'

jobs:
  build:
    name: Build WebGL
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true

      - uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          projectPath: unity-client
          targetPlatform: WebGL
          buildsPath: build
          buildName: EternalKingdoms

      - uses: actions/upload-artifact@v4
        with:
          name: unity-webgl
          path: build/WebGL/EternalKingdoms
```

Required GitHub secrets:
- `UNITY_LICENSE` — contents of your Unity license file (`.ulf`)
- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

To get your license file, run GameCI's activation workflow first:
https://game.ci/docs/github/activation

---

## Auth Bridge

The Unity client receives the player's JWT token via `postMessage` from the web launcher:

```csharp
// Already implemented in Assets/Scripts/Authentication/AuthManager.cs
// Listens for: { type: "UNITY_AUTH", token: "...", userId: 1 }
// Sends back:  { type: "UNITY_READY" }
```

---

## Scripting Define Symbols

The project includes two defines (set in `ProjectSettings.asset`):

| Symbol | Platform | Meaning |
|---|---|---|
| `EK_CLIENT` | All | Eternal Kingdoms client build |
| `EK_WEBGL` | WebGL | WebGL-specific code paths |

Use `#if EK_WEBGL` for browser-only code (postMessage, JS interop, etc.).

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `Library/` errors on open | Delete `Library/` folder and re-open |
| Package import fails | Check internet connection; retry via Package Manager |
| URP errors at startup | Create URP asset (see First-Time URP Setup above) |
| Script compile errors | Check Unity version is exactly `6000.0.23f1` |
| WebGL build fails OOM | Reduce `webGLMemorySize` in Player Settings or use 32-bit |
