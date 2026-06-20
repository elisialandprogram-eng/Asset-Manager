using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using TMPro;

namespace EternalKingdoms.Demo
{
    /// <summary>
    /// DemoSceneController — Orchestrates the DemoScene.unity instant visual showcase.
    ///
    /// Phase 5.5 (U5.5.11) demo scene contains:
    ///   - One complete kingdom (Palace + all building types, citizens, flags, torches)
    ///   - One world region (Grasslands → Forest biome blend, river, roads)
    ///   - All 6 resource node types
    ///   - All 5 monster categories (T3 specimens for visual impact)
    ///   - Full VFX (march arrival, crystal pulse, selection rings)
    ///   - Full UI (HUD overlay with resource counts)
    ///   - Cinematic camera flythrough (optional, toggle with F1)
    ///
    /// Purpose: Instant visual showcase for stakeholders, screenshots, and videos.
    ///
    /// Architecture:
    ///   - Scene: Assets/Scenes/DemoScene.unity
    ///   - Loads and initializes all Phase 5 visual systems in correct order
    ///   - CinematicMode: pre-defined camera path flys through kingdom → world region
    ///   - Debug overlay (F2): shows performance stats + asset validation report
    ///   - Screenshot (F3): captures 4K screenshot to persistent data path
    ///   - No server connection required — all data is hardcoded for visual demo
    /// </summary>
    public class DemoSceneController : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool autoStartCinematic = true;
        [SerializeField] private float cinematicDelay    = 2f;

        [Header("Demo Kingdom Data")]
        [SerializeField] private int demoPalaceLevel     = 10;
        [SerializeField] private string demoBiome        = "grasslands";
        [SerializeField] private Color demoKingdomColor  = new Color(0.2f, 0.4f, 0.8f);

        [Header("Camera Path")]
        [SerializeField] private Transform[] cinematicWaypoints;
        [SerializeField] private float waypontSpeed = 5f;
        [SerializeField] private float waypointWait = 3f;

        [Header("UI References")]
        [SerializeField] private GameObject debugOverlayRoot;
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Demo Monsters")]
        [SerializeField] private Transform[] monsterSpawnPoints;
        [SerializeField] private GameObject[] monsterPrefabsSample;

        [Header("Demo Marches — auto-send for visual")]
        [SerializeField] private Transform marchOrigin;
        [SerializeField] private Transform[] marchDestinations;
        [SerializeField] private GameObject marchBannerPrefab;

        private bool _cinematicActive;
        private bool _debugVisible;
        private Camera _camera;
        private float _timer;

        private void Start()
        {
            _camera = Camera.main;
            StartCoroutine(InitializeDemo());
        }

        private void Update()
        {
            HandleInput();
            if (_debugVisible) UpdateDebugOverlay();
        }

        // ── Init ──────────────────────────────────────────────────────────────

        private IEnumerator InitializeDemo()
        {
            SetStatus("Initializing demo scene...");

            // 1. Trigger biome
            if (EternalKingdoms.Terrain.BiomeTerrainController.Instance != null)
                EternalKingdoms.Terrain.BiomeTerrainController.Instance.OnEnterBiome(demoBiome);

            yield return null;

            // 2. Ambient audio
            if (EternalKingdoms.Audio.AmbientAudioController.Instance != null)
                EternalKingdoms.Audio.AmbientAudioController.Instance.TransitionTo(demoBiome);

            yield return null;

            // 3. Ambient life
            if (EternalKingdoms.Environment.AmbientLifeManager.Instance != null)
                EternalKingdoms.Environment.AmbientLifeManager.Instance.SwitchBiomeLife(demoBiome);

            yield return null;

            // 4. Kingdom identity
            SetKingdomIdentity();

            yield return null;

            // 5. Spawn demo monsters
            SpawnDemoMonsters();

            yield return null;

            // 6. Auto-send demo marches
            StartCoroutine(AutoDemoMarches());

            yield return null;

            // 7. Validate assets and report
            StartCoroutine(RunAssetValidation());

            // 8. Cinematic
            if (autoStartCinematic)
            {
                SetStatus("Starting cinematic flythrough...");
                yield return new WaitForSeconds(cinematicDelay);
                StartCoroutine(CinematicFlythrough());
            }
            else
            {
                SetStatus("Demo ready. F1=Cinematic  F2=Debug  F3=Screenshot");
            }
        }

        private void SetKingdomIdentity()
        {
            var kvc = FindObjectOfType<EternalKingdoms.Kingdom.KingdomVisualController>();
            if (kvc == null) return;
            kvc.SetKingdomIdentity(demoKingdomColor, null);
            kvc.UpdateWalls(demoPalaceLevel);

            var cm = FindObjectOfType<EternalKingdoms.Population.CitizenManager>();
            if (cm != null) cm.SetPalaceLevel(demoPalaceLevel);
        }

        private void SpawnDemoMonsters()
        {
            if (monsterPrefabsSample == null || monsterSpawnPoints == null) return;
            for (int i = 0; i < monsterSpawnPoints.Length && i < monsterPrefabsSample.Length; i++)
            {
                if (monsterPrefabsSample[i] == null || monsterSpawnPoints[i] == null) continue;
                var go = Instantiate(monsterPrefabsSample[i],
                                     monsterSpawnPoints[i].position,
                                     monsterSpawnPoints[i].rotation);
                var mvc = go.GetComponent<EternalKingdoms.Monsters.MonsterVisualController>();
                mvc?.SetTier(3);
                mvc?.PlaySpawn();
            }
        }

        private IEnumerator AutoDemoMarches()
        {
            if (marchBannerPrefab == null || marchOrigin == null
                || marchDestinations == null || marchDestinations.Length == 0) yield break;

            foreach (var dest in marchDestinations)
            {
                if (dest == null) continue;
                var banner = Instantiate(marchBannerPrefab, marchOrigin.position, Quaternion.identity);
                var entity = banner.GetComponent<EternalKingdoms.World.MarchBannerEntity>();
                entity?.PlaceDestinationBeacon(dest.position);
                entity?.SetupGatherMarch("outbound", DateTime.UtcNow.AddMinutes(5),
                                          demoKingdomColor, 1500);
                yield return new WaitForSeconds(1.5f);
            }
        }

        private IEnumerator RunAssetValidation()
        {
            var registry = EternalKingdoms.Content.ArtAssetRegistry.Instance;
            if (registry == null) yield break;
            yield return StartCoroutine(registry.ValidateAll());
            SetStatus($"Asset validation: {registry.ValidationReport.CoveragePercent:F0}% coverage" +
                       (registry.ValidationReport.MissingCount > 0
                           ? $" | {registry.ValidationReport.MissingCount} missing"
                           : " | All assets present ✅"));
        }

        // ── Cinematic Flythrough ──────────────────────────────────────────────

        private IEnumerator CinematicFlythrough()
        {
            if (cinematicWaypoints == null || cinematicWaypoints.Length < 2) yield break;
            _cinematicActive = true;

            for (int i = 0; i < cinematicWaypoints.Length; i++)
            {
                var target = cinematicWaypoints[i];
                if (target == null) continue;

                // Smooth move to waypoint
                Vector3 startPos = _camera.transform.position;
                Quaternion startRot = _camera.transform.rotation;
                float elapsed = 0f;
                float duration = Vector3.Distance(startPos, target.position) / waypontSpeed;
                duration = Mathf.Max(duration, 1f);

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    _camera.transform.position = Vector3.Lerp(startPos, target.position, t);
                    _camera.transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
                    yield return null;
                }

                yield return new WaitForSeconds(waypointWait);
            }

            _cinematicActive = false;
            SetStatus("Cinematic complete. F1=Replay  F2=Debug  F3=Screenshot");
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (!_cinematicActive) StartCoroutine(CinematicFlythrough());
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                _debugVisible = !_debugVisible;
                if (debugOverlayRoot != null) debugOverlayRoot.SetActive(_debugVisible);
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                StartCoroutine(TakeScreenshot());
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                StopAllCoroutines();
                _cinematicActive = false;
            }
        }

        // ── Debug Overlay ─────────────────────────────────────────────────────

        private void UpdateDebugOverlay()
        {
            if (debugText == null) return;
            var pm = EternalKingdoms.Performance.PerformanceManager.Instance;
            var ar = EternalKingdoms.Content.ArtAssetRegistry.Instance;

            debugText.text =
                $"FPS: {(pm != null ? pm.Report.averageFPS.ToString("F1") : "—")}\n" +
                $"Entities: {(pm != null ? pm.Report.entityCount.ToString() : "—")}\n" +
                $"Assets: {(ar != null ? $"{ar.ValidationReport.found.Count}/{ar.ValidationReport.TotalCount}" : "—")}\n" +
                $"Missing: {(ar != null ? ar.ValidationReport.MissingCount.ToString() : "—")}\n" +
                $"Quality: {EternalKingdoms.Visual.VisualSettingsManager.Instance?.CurrentTier}\n" +
                $"Biome: {demoBiome}";
        }

        private IEnumerator TakeScreenshot()
        {
            yield return new WaitForEndOfFrame();
            string path = System.IO.Path.Combine(
                Application.persistentDataPath,
                $"EK_Demo_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            );
            ScreenCapture.CaptureScreenshot(path, 4);
            SetStatus($"Screenshot saved: {path}");
            Debug.Log($"[Demo] Screenshot: {path}");
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
            Debug.Log($"[DemoScene] {msg}");
        }
    }
}
