using System.Collections;
using UnityEngine;
using EternalKingdoms.Networking;
using EternalKingdoms.VFX;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// U5.7.9 — Closed Alpha Playtest Tools
    /// Development-only cheats for playtesting: instant upgrades, skip timers,
    /// resource spawning, monster spawning, teleport, god mode, infinite AP.
    ///
    /// DISABLED IN PRODUCTION — all methods are no-ops unless DEVELOPMENT_BUILD
    /// is set.  Runtime check via Application.isEditor || Debug.isDebugBuild.
    ///
    /// In-game: Press F12 to show/hide the playtest panel.
    /// </summary>
    public class PlaytestManager : MonoBehaviour
    {
        public static PlaytestManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("UI Panel")]
        public GameObject playtestPanel;

        [Header("Resource Spawn Amounts")]
        public int spawnGold    = 100000;
        public int spawnFood    = 100000;
        public int spawnWood    = 100000;
        public int spawnStone   = 100000;
        public int spawnIron    = 100000;
        public int spawnCrystal = 10000;

        [Header("Teleport Targets")]
        public Transform[] teleportPoints;

        [Header("God Mode")]
        public bool godModeActive;
        public float godModeTimeScale = 1f;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _panelVisible;
        private bool _isDevBuild;
        private ApiClient _api;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _isDevBuild = Application.isEditor || Debug.isDebugBuild;

            if (!_isDevBuild)
            {
                Debug.Log("[PlaytestManager] Production build — all playtest tools disabled.");
                if (playtestPanel != null) playtestPanel.SetActive(false);
                enabled = false;
                return;
            }

            Debug.Log("[PlaytestManager] ⚠️ DEVELOPMENT BUILD — Playtest tools active. F12 to toggle panel.");
        }

        private void Start()
        {
            _api = FindAnyObjectByType<ApiClient>();
            if (playtestPanel != null) playtestPanel.SetActive(false);
        }

        private void Update()
        {
            if (!_isDevBuild) return;
            if (Input.GetKeyDown(KeyCode.F12)) TogglePanel();

            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.F5)) SpawnAllResources();
            if (Input.GetKeyDown(KeyCode.F6)) SpawnMonster();
            if (Input.GetKeyDown(KeyCode.F7)) InstantUpgradeAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Panel
        // ─────────────────────────────────────────────────────────────────────
        public void TogglePanel()
        {
            if (!_isDevBuild) return;
            _panelVisible = !_panelVisible;
            if (playtestPanel != null) playtestPanel.SetActive(_panelVisible);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Cheat Commands
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Grant large amounts of all resources via API.</summary>
        public void SpawnAllResources()
        {
            if (!IsAllowed()) return;
            StartCoroutine(SpawnResourcesCoroutine());
            Debug.Log("[PlaytestManager] 💰 Resources spawned.");
        }

        private IEnumerator SpawnResourcesCoroutine()
        {
            // POST /api/playtest/resources with cheat grant (dev endpoint only)
            if (_api == null) yield break;
            var body = new ResourceGrantRequest
            {
                gold    = spawnGold,
                food    = spawnFood,
                wood    = spawnWood,
                stone   = spawnStone,
                iron    = spawnIron,
                crystal = spawnCrystal
            };
            yield return _api.PostAsync("/api/playtest/resources", body, onSuccess: (r) =>
            {
                AlphaVFXController.Instance?.PlayLootExplosion(Camera.main?.transform.position ?? Vector3.zero);
            });
        }

        /// <summary>Spawn a random monster at the camera's position for testing combat.</summary>
        public void SpawnMonster()
        {
            if (!IsAllowed()) return;
            var spawnMgr = FindAnyObjectByType<Monsters.MonsterSpawnManager>();
            if (spawnMgr == null) { Debug.LogWarning("[PlaytestManager] MonsterSpawnManager not found."); return; }

            Vector3 pos = (Camera.main?.transform.position ?? Vector3.zero) + Camera.main.transform.forward * 20f;
            spawnMgr.ForceSpawnAt(pos, monsterCategory: "bandit", tier: 2);
            Debug.Log($"[PlaytestManager] 👹 Monster spawned at {pos}.");
        }

        /// <summary>Instantly complete all building construction and upgrades.</summary>
        public void InstantUpgradeAll()
        {
            if (!IsAllowed()) return;
            StartCoroutine(InstantUpgradeCoroutine());
            Debug.Log("[PlaytestManager] ⚡ Instant upgrade triggered.");
        }

        private IEnumerator InstantUpgradeCoroutine()
        {
            if (_api == null) yield break;
            yield return _api.PostAsync("/api/playtest/instant-complete", null, onSuccess: (_) =>
            {
                var ksc = FindAnyObjectByType<Kingdom.KingdomSceneController>();
                ksc?.RefreshKingdomState();
            });
        }

        /// <summary>Skip all active timers (construction, upgrades, marches).</summary>
        public void SkipAllTimers()
        {
            if (!IsAllowed()) return;
            StartCoroutine(_api?.PostAsync("/api/playtest/skip-timers", null,
                onSuccess: (_) => Debug.Log("[PlaytestManager] ⏩ Timers skipped.")));
        }

        /// <summary>Teleport to a registered teleport point by index.</summary>
        public void TeleportTo(int index)
        {
            if (!IsAllowed()) return;
            if (teleportPoints == null || index < 0 || index >= teleportPoints.Length) return;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = teleportPoints[index].position;
                Debug.Log($"[PlaytestManager] 🌀 Teleported to {teleportPoints[index].name}.");
            }
        }

        /// <summary>Toggle god mode — invincibility and infinite action points.</summary>
        public void ToggleGodMode()
        {
            if (!IsAllowed()) return;
            godModeActive = !godModeActive;
            Debug.Log(godModeActive ? "[PlaytestManager] ⚡ GOD MODE ON" : "[PlaytestManager] God mode off.");
        }

        /// <summary>Grant maximum action points via API.</summary>
        public void GrantInfiniteAP()
        {
            if (!IsAllowed()) return;
            StartCoroutine(_api?.PostAsync("/api/playtest/grant-ap", new { amount = 9999 },
                onSuccess: (_) => Debug.Log("[PlaytestManager] ⚡ Infinite AP granted.")));
        }

        /// <summary>Set time scale for fast simulation during testing.</summary>
        public void SetTimeScale(float scale)
        {
            if (!IsAllowed()) return;
            Time.timeScale = scale;
            Debug.Log($"[PlaytestManager] ⏱ Time scale set to {scale}×.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Safety Gate
        // ─────────────────────────────────────────────────────────────────────
        private bool IsAllowed()
        {
            if (_isDevBuild) return true;
            Debug.LogError("[PlaytestManager] ❌ Cheat blocked — production build.");
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        [System.Serializable]
        private class ResourceGrantRequest
        {
            public int gold, food, wood, stone, iron, crystal;
        }
    }
}
