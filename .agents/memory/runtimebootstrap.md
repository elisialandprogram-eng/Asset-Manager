---
name: RuntimeBootstrap pattern
description: How the Unity WebGL self-boot system works and what to respect when modifying it
---

## The system

`RuntimeBootstrap.cs` (`unity-client/Assets/Scripts/Core/`) uses `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` to fire before any scene or MonoBehaviour. It creates all singleton manager GameObjects if they are absent from the active scene, then monitors bootstrap progress and spawns a procedural fallback world if no Camera.main is detected within the timeout.

## Key rules

- **EnsureManager<T> naming matters.** The AuthManager's host GameObject MUST be named `"AuthManager"` because Unity's `SendMessage("AuthManager", "ReceiveAuthToken", token)` resolves by GameObject name. `EnsureManager<AuthManager>("AuthManager")` enforces this.
- **BootstrapManager is created last.** Its `Start()` triggers `RunBootstrap()` which calls each manager's `Initialize()`. All `Initialize()` coroutines guard with `if (_initialized) yield break`, making double-calls safe.
- **Scene objects self-destruct.** Because RuntimeBootstrap fires before scene load, the managers it creates via `DontDestroyOnLoad` win the singleton race. Any identically-typed manager in Bootstrap.unity detects `Instance != null` in its `Awake()` and calls `Destroy(gameObject)` on itself.
- **Auth bridge flow.** React sends JWT via `postMessage({type:"UNITY_AUTH", token})` → `unity/index.html` forwards with `unityInstance.SendMessage("AuthManager","ReceiveAuthToken",token)` → `AuthManager.ReceiveAuthToken()` validates against `/api/auth/me` → fires `OnLoginSuccess` → `RuntimeBootstrap.HandleAuthSuccess()` calls `SceneController.GoToWorld()`.
- **Fallback world triggers on Camera.main == null.** The watcher gives up to 40 s for BootstrapManager to complete, then up to 20 s for `GameState.World/Kingdom`. If timeout or no camera → `SpawnFallbackWorld()` with primitives only.
- **JS plugin.** `unity-client/Assets/Plugins/WebGL/NativeBridge.jslib` exports `NotifyJsBridge(modePtr)` which fires `UNITY_GAME_READY` CustomEvent and posts to parent frame. C# calls it via `[DllImport("__Internal")]`.

**Why:** The existing WebGL build shows black because Bootstrap.unity's scene GameObjects may not be configured in the Editor. RuntimeBootstrap removes that dependency while preserving all existing Phase 0–5.8 systems intact.
