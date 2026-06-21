using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using EternalKingdoms.Authentication;
using EternalKingdoms.Networking;

namespace EternalKingdoms.Core
{
    /// <summary>
    /// RuntimeBootstrap — removes Unity Editor scene dependency.
    ///
    /// Uses [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] to fire before ANY
    /// scene or MonoBehaviour runs. Creates all singleton manager GameObjects that
    /// are missing from the active scene, then monitors the bootstrap sequence and
    /// spawns a procedural fallback world if no camera-rendered output is detected
    /// within the timeout window.
    ///
    /// Safe integration guarantees:
    ///   - EnsureManager<T> checks FindAnyObjectByType before creating anything.
    ///     If the scene already has the manager, it is used as-is.
    ///   - All Initialize() coroutines in existing managers guard with
    ///     "if (_initialized) yield break" — double-calls are harmless.
    ///   - BootstrapManager.Start() owns the official bootstrap sequence.
    ///     RuntimeBootstrap only monitors and intervenes on failure.
    ///   - SpawnFallbackWorld() is only called when no camera is rendering after
    ///     the timeout — it never fires when the real world scene is working.
    ///
    /// Auth bridge:
    ///   The React host sends the JWT via:
    ///     unityInstance.SendMessage("AuthManager", "ReceiveAuthToken", token)
    ///   AuthManager.ReceiveAuthToken() is added by this system and handles
    ///   the JS→Unity token handshake.
    /// </summary>
    public class RuntimeBootstrap : MonoBehaviour
    {
        // ── Static entry point ────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            Debug.Log("[RuntimeBootstrap] Initializing runtime bootstrap system...");
            var host = new GameObject("[EK-RuntimeBootstrap]");
            DontDestroyOnLoad(host);
            host.AddComponent<RuntimeBootstrap>();
        }

        // ── Configuration ─────────────────────────────────────────────────────

        private const float BootstrapTimeoutSeconds = 40f;
        private const float WorldReadyTimeoutSeconds = 20f;
        private const float FallbackWarmupSeconds   = 3f;

        // Isometric camera angles matching WorldCameraController convention
        private const float ISO_X = 55f;
        private const float ISO_Y = -45f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool _waitingForAuth;
        private bool _fallbackSpawned;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Create every singleton manager if it is not already present.
            // Managers created here use DontDestroyOnLoad inside their own Awake,
            // so if the loaded scene has the same manager type, its Awake will
            // detect the existing Instance and self-destroy.
            EnsureManager<GameManager>("GameManager");
            EnsureManager<ConfigManager>("ConfigManager");
            EnsureManager<SaveManager>("SaveManager");
            EnsureManager<AddressablesManager>("AddressablesManager");
            EnsureManager<NetworkManager>("NetworkManager");

            // "AuthManager" name is required — Unity's SendMessage("AuthManager", ...) resolves by GO name.
            EnsureManager<AuthManager>("AuthManager");

            EnsureManager<SceneController>("SceneController");
            EnsureManager<UI.UIManager>("UIManager");

            // BootstrapManager must be last — its Start() triggers RunBootstrap().
            EnsureManager<BootstrapManager>("BootstrapManager");

            // Subscribe to auth success so we can route after a JS token arrives.
            if (AuthManager.Instance != null)
                AuthManager.Instance.OnLoginSuccess += HandleAuthSuccess;
        }

        private void Start()
        {
            StartCoroutine(WatchBootstrap());
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
                AuthManager.Instance.OnLoginSuccess -= HandleAuthSuccess;
        }

        // ── Bootstrap watcher ─────────────────────────────────────────────────

        private IEnumerator WatchBootstrap()
        {
            // Phase 1: wait for BootstrapManager to complete its sequence.
            float elapsed = 0f;
            while ((BootstrapManager.Instance == null || !BootstrapManager.Instance.IsInitialized)
                   && elapsed < BootstrapTimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (BootstrapManager.Instance == null || !BootstrapManager.Instance.IsInitialized)
            {
                Debug.LogWarning($"[RuntimeBootstrap] Bootstrap timed out after {BootstrapTimeoutSeconds}s — spawning fallback world.");
                SpawnFallbackWorld();
                yield break;
            }

            Debug.Log("[RuntimeBootstrap] Bootstrap complete. Monitoring world state...");

            // Phase 2: wait for game to reach a rendered state.
            elapsed = 0f;
            while (elapsed < WorldReadyTimeoutSeconds)
            {
                var state = GameManager.Instance?.CurrentState ?? GameState.Bootstrapping;

                if (state == GameState.World || state == GameState.Kingdom)
                {
                    // Routing succeeded. Give scene controllers time to set up cameras.
                    yield return new WaitForSecondsRealtime(FallbackWarmupSeconds);

                    if (Camera.main == null)
                    {
                        Debug.LogWarning("[RuntimeBootstrap] World/Kingdom state active but no Camera.main — spawning fallback world.");
                        SpawnFallbackWorld();
                    }
                    else
                    {
                        Debug.Log("[RuntimeBootstrap] Camera detected — runtime bootstrap complete. Game is live.");
                        NotifyJsReady("world_active");
                    }
                    yield break;
                }

                if (state == GameState.Login)
                {
                    // Auth failed. Wait for the React host to send a token.
                    Debug.Log("[RuntimeBootstrap] Waiting for auth token from React bridge...");
                    _waitingForAuth = true;

                    // Spawn a visible waiting screen so the canvas isn't black.
                    SpawnAuthWaitScreen();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Timeout — fallback.
            Debug.LogWarning("[RuntimeBootstrap] World-ready timeout — spawning fallback world.");
            SpawnFallbackWorld();
        }

        // ── Auth bridge ───────────────────────────────────────────────────────

        private void HandleAuthSuccess()
        {
            if (!_waitingForAuth) return;
            _waitingForAuth = false;

            Debug.Log("[RuntimeBootstrap] Auth success received — routing to World.");
            DestroyAuthWaitScreen();

            if (SceneExists(SceneController.SCENE_WORLD))
                SceneController.Instance?.GoToWorld();
            else
                SpawnFallbackWorld();
        }

        // ── Scene validation ──────────────────────────────────────────────────

        private static bool SceneExists(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByIndex(i);
                if (path.EndsWith($"/{sceneName}.unity") ||
                    path.EndsWith($"\\{sceneName}.unity") ||
                    path == sceneName)
                    return true;
            }
            return false;
        }

        // ── Manager factory ───────────────────────────────────────────────────

        private static T EnsureManager<T>(string goName) where T : MonoBehaviour
        {
#if UNITY_6000_0_OR_NEWER
            var existing = FindAnyObjectByType<T>(FindObjectsInactive.Include);
#else
            var existing = FindObjectOfType<T>(true);
#endif
            if (existing != null)
            {
                Debug.Log($"[RuntimeBootstrap] {typeof(T).Name} already present — skipping creation.");
                return existing;
            }

            var go = new GameObject(goName);
            DontDestroyOnLoad(go);
            var component = go.AddComponent<T>();
            Debug.Log($"[RuntimeBootstrap] Created missing manager: {goName}");
            return component;
        }

        // ── Auth wait screen ──────────────────────────────────────────────────

        private GameObject _authWaitRoot;

        private void SpawnAuthWaitScreen()
        {
            if (_authWaitRoot != null) return;

            _authWaitRoot = new GameObject("[EK-AuthWait]");
            DontDestroyOnLoad(_authWaitRoot);

            // Minimal camera so the canvas renders.
            if (Camera.main == null)
            {
                var camGO = new GameObject("WaitCamera");
                camGO.tag = "MainCamera";
                camGO.transform.SetParent(_authWaitRoot.transform);
                var cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
            }

            BuildOverlayCanvas(_authWaitRoot.transform,
                title: "ETERNAL KINGDOMS",
                subtitle: "Authenticating…",
                footer: "Connecting to your kingdom");
        }

        private void DestroyAuthWaitScreen()
        {
            if (_authWaitRoot != null)
                Destroy(_authWaitRoot);
            _authWaitRoot = null;
        }

        // ── Fallback world ────────────────────────────────────────────────────

        /// <summary>
        /// Spawns a minimal but visible procedural world using only built-in
        /// Unity primitives. Called only when no camera-rendered content is
        /// detected within the timeout window.
        ///
        /// Layout:
        ///   - Isometric camera (ISO_X/ISO_Y matching WorldCameraController)
        ///   - Directional light (warm medieval sun angle)
        ///   - Terrain plane (200×200 units, forest green)
        ///   - 1 Kingdom entity at world centre (gold tower)
        ///   - 3 Resource nodes (jade green cubes)
        ///   - 3 Monster entities (crimson cubes, patrol positions)
        ///   - Screen-space HUD canvas (title, status, legend)
        /// </summary>
        private void SpawnFallbackWorld()
        {
            if (_fallbackSpawned) return;
            _fallbackSpawned = true;

            DestroyAuthWaitScreen();

            Debug.Log("[RuntimeBootstrap] Spawning procedural fallback world.");
            var worldRoot = new GameObject("[EK-FallbackWorld]");
            DontDestroyOnLoad(worldRoot);

            // ── Camera ────────────────────────────────────────────────────────
            Camera cam;
            if (Camera.main != null)
            {
                cam = Camera.main;
            }
            else
            {
                var camGO = new GameObject("MainCamera");
                camGO.tag = "MainCamera";
                camGO.transform.SetParent(worldRoot.transform);
                cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.28f, 0.41f, 0.58f);
            }

            // Isometric position — matches WorldCameraController angles.
            cam.gameObject.transform.position = new Vector3(0f, 35f, -25f);
            cam.gameObject.transform.rotation = Quaternion.Euler(ISO_X, 0f, 0f);
            cam.orthographic = false;
            cam.fieldOfView = 50f;

            // ── Lighting ──────────────────────────────────────────────────────
            var lightGO = new GameObject("DirectionalLight");
            lightGO.transform.SetParent(worldRoot.transform);
            lightGO.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var sun = lightGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.93f, 0.78f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(0.3f, 0.35f, 0.45f);
            RenderSettings.fog = false;

            // ── Terrain ───────────────────────────────────────────────────────
            var terrain = CreatePrimitive("WorldTerrain", PrimitiveType.Plane,
                new Vector3(0f, 0f, 0f),
                new Vector3(20f, 1f, 20f),
                new Color(0.22f, 0.38f, 0.18f));
            terrain.transform.SetParent(worldRoot.transform);

            // ── Kingdom ───────────────────────────────────────────────────────
            var kingdomRoot = new GameObject("KingdomEntity");
            kingdomRoot.transform.SetParent(worldRoot.transform);
            kingdomRoot.transform.position = Vector3.zero;

            CreatePrimitive("Keep_Base", PrimitiveType.Cube,
                new Vector3(0f, 1.5f, 0f),
                new Vector3(3f, 3f, 3f),
                new Color(0.72f, 0.61f, 0.38f),
                kingdomRoot.transform);

            CreatePrimitive("Keep_Tower", PrimitiveType.Cube,
                new Vector3(0f, 4.5f, 0f),
                new Vector3(1.8f, 3f, 1.8f),
                new Color(0.62f, 0.52f, 0.28f),
                kingdomRoot.transform);

            CreatePrimitive("Keep_Battlements", PrimitiveType.Cube,
                new Vector3(0f, 6.5f, 0f),
                new Vector3(2.0f, 0.6f, 2.0f),
                new Color(0.75f, 0.64f, 0.42f),
                kingdomRoot.transform);

            // ── Resource nodes ────────────────────────────────────────────────
            SpawnResourceNode("ResourceNode_Wood",    new Vector3( 18f, 0f,  12f), new Color(0.25f, 0.62f, 0.28f), worldRoot.transform);
            SpawnResourceNode("ResourceNode_Stone",   new Vector3(-14f, 0f,  16f), new Color(0.55f, 0.55f, 0.60f), worldRoot.transform);
            SpawnResourceNode("ResourceNode_Crystal", new Vector3(  8f, 0f, -20f), new Color(0.30f, 0.72f, 0.82f), worldRoot.transform);

            // ── Monsters ──────────────────────────────────────────────────────
            SpawnMonster("Monster_Orc",   new Vector3( 24f, 0f,  -8f), new Color(0.68f, 0.22f, 0.18f), worldRoot.transform);
            SpawnMonster("Monster_Troll", new Vector3(-20f, 0f, -10f), new Color(0.55f, 0.18f, 0.15f), worldRoot.transform);
            SpawnMonster("Monster_Drake", new Vector3(  4f, 0f,  28f), new Color(0.72f, 0.25f, 0.20f), worldRoot.transform);

            // ── HUD ───────────────────────────────────────────────────────────
            BuildOverlayCanvas(worldRoot.transform,
                title: "ETERNAL KINGDOMS",
                subtitle: null,
                footer: "Fallback world — game scene initializing");

            Debug.Log("[RuntimeBootstrap] Fallback world ready.");
            NotifyJsReady("fallback_world");
        }

        // ── Spawn helpers ─────────────────────────────────────────────────────

        private static void SpawnResourceNode(string name, Vector3 pos, Color color, Transform parent)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent);
            node.transform.position = pos;

            CreatePrimitive("Body", PrimitiveType.Cube,
                pos + new Vector3(0f, 0.6f, 0f),
                new Vector3(1.2f, 1.2f, 1.2f),
                color,
                parent);

            CreatePrimitive("Crystal", PrimitiveType.Cube,
                pos + new Vector3(0f, 1.6f, 0f),
                new Vector3(0.5f, 0.8f, 0.5f),
                color * 1.3f,
                parent);
        }

        private static void SpawnMonster(string name, Vector3 pos, Color color, Transform parent)
        {
            var mob = new GameObject(name);
            mob.transform.SetParent(parent);
            mob.transform.position = pos;

            // Body
            CreatePrimitive("Body", PrimitiveType.Cube,
                pos + new Vector3(0f, 1.0f, 0f),
                new Vector3(1.4f, 2.0f, 1.0f),
                color,
                parent);

            // Head
            CreatePrimitive("Head", PrimitiveType.Sphere,
                pos + new Vector3(0f, 2.6f, 0f),
                new Vector3(0.9f, 0.9f, 0.9f),
                color * 1.1f,
                parent);
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Color color,
            Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;

            if (parent != null)
                go.transform.SetParent(parent, worldPositionStays: true);

            // URP / Built-in RP material.
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard")
                      ?? Shader.Find("Sprites/Default");

            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = color;

                // URP: enable emission so colours read well in ambient light.
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 0.08f);
                }

                go.GetComponent<Renderer>().material = mat;
            }

            // Remove colliders to avoid physics overhead.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return go;
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private static void BuildOverlayCanvas(Transform parent, string title, string subtitle, string footer)
        {
            var canvasGO = new GameObject("[EK-OverlayCanvas]");
            canvasGO.transform.SetParent(parent, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Title bar.
            AddLabel(canvasGO.transform, "TitleLabel",
                title,
                font, 32,
                FontStyle.Bold,
                new Color(0.98f, 0.82f, 0.22f),
                TextAnchor.UpperCenter,
                new Vector2(0f, 0.88f),
                new Vector2(1f, 1.00f));

            // Subtitle (optional).
            if (!string.IsNullOrEmpty(subtitle))
            {
                AddLabel(canvasGO.transform, "SubtitleLabel",
                    subtitle,
                    font, 18,
                    FontStyle.Normal,
                    new Color(0.85f, 0.85f, 0.85f),
                    TextAnchor.UpperCenter,
                    new Vector2(0f, 0.80f),
                    new Vector2(1f, 0.88f));
            }

            // Legend — bottom-left.
            AddLabel(canvasGO.transform, "LegendLabel",
                "<color=#e0c84f>■</color> Kingdom   " +
                "<color=#45b03a>■</color> Resources   " +
                "<color=#c03a3a>■</color> Monsters",
                font, 13,
                FontStyle.Normal,
                new Color(0.78f, 0.78f, 0.78f, 0.85f),
                TextAnchor.LowerLeft,
                new Vector2(0.01f, 0.00f),
                new Vector2(0.60f, 0.06f));

            // Footer status — bottom-right.
            AddLabel(canvasGO.transform, "FooterLabel",
                footer,
                font, 12,
                FontStyle.Italic,
                new Color(0.60f, 0.60f, 0.60f, 0.70f),
                TextAnchor.LowerRight,
                new Vector2(0.40f, 0.00f),
                new Vector2(0.99f, 0.06f));
        }

        private static void AddLabel(
            Transform parent,
            string name,
            string text,
            Font font,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor anchor,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<UnityEngine.UI.Text>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.supportRichText = true;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(8f, 4f);
            rt.offsetMax = new Vector2(-8f, -4f);
        }

        // ── JS bridge ─────────────────────────────────────────────────────────

        /// <summary>
        /// Notifies the React host that Unity is rendering.
        /// Calls window.dispatchEvent(new CustomEvent("UNITY_GAME_READY", {...})).
        /// </summary>
        private static void NotifyJsReady(string mode)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyJsBridge(mode);
#endif
            Debug.Log($"[RuntimeBootstrap] Notified JS — mode: {mode}");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void NotifyJsBridge(string mode);
#endif
    }
}
