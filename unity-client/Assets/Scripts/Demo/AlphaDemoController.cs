using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EternalKingdoms.CameraDemo;
using EternalKingdoms.Environment;
using EternalKingdoms.VFX;
using EternalKingdoms.World;

namespace EternalKingdoms.Demo
{
    /// <summary>
    /// U5.6.10 — Alpha Demo Experience
    /// Orchestrates the complete automated demo player flow:
    ///   Login → Kingdom Cinematic → Kingdom Interaction → World Transition
    ///   → World Exploration → Monster Hunt → Resource Gather → Battle Report → Return Home
    /// Runs hands-free for trade shows / investor demos.  Press F1 to start, ESC to abort.
    /// </summary>
    public class AlphaDemoController : MonoBehaviour
    {
        public static AlphaDemoController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Scene Names")]
        public string loginSceneName   = "LoginScene";
        public string kingdomSceneName = "KingdomScene";
        public string worldSceneName   = "WorldScene";

        [Header("Demo Timing (seconds)")]
        public float loginDisplayTime       = 4f;
        public float kingdomBrowseTime      = 12f;
        public float buildingInteractTime   = 5f;
        public float worldExploreTime       = 15f;
        public float monsterHuntTime        = 10f;
        public float resourceGatherTime     = 8f;
        public float battleReportTime       = 8f;
        public float returnHomeTime         = 4f;

        [Header("Scene References (populated at runtime)")]
        public Transform kingdomRoot;
        public Transform nearestMonster;
        public Transform nearestResourceNode;

        [Header("UI")]
        public GameObject demoOverlayPanel;
        public TMPro.TextMeshProUGUI stepLabel;
        public TMPro.TextMeshProUGUI progressLabel;

        [Header("Auto-Loop")]
        public bool loopDemo = true;

        // ── State ─────────────────────────────────────────────────────────────
        private bool  _running;
        private int   _stepIndex;
        private const int TotalSteps = 9;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !_running) StartDemo();
            if (Input.GetKeyDown(KeyCode.Escape) && _running) AbortDemo();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────
        public void StartDemo()
        {
            if (_running) return;
            _running = true;
            StartCoroutine(RunFullDemo());
        }

        public void AbortDemo()
        {
            StopAllCoroutines();
            _running = false;
            HideOverlay();
            Debug.Log("[AlphaDemoController] Demo aborted by user.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Flow
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator RunFullDemo()
        {
            Debug.Log("[AlphaDemoController] ▶ Alpha Demo Experience starting…");

            // ── Step 1: Login ──────────────────────────────────────────────────
            yield return Step(1, "Login", RunLoginStep());

            // ── Step 2: Kingdom Entry Cinematic ───────────────────────────────
            yield return Step(2, "Kingdom Entry Cinematic", RunKingdomEntryStep());

            // ── Step 3: Kingdom Interaction ───────────────────────────────────
            yield return Step(3, "Kingdom Interaction", RunKingdomInteractionStep());

            // ── Step 4: World Transition Cinematic ────────────────────────────
            yield return Step(4, "World Transition", RunWorldTransitionStep());

            // ── Step 5: World Exploration ─────────────────────────────────────
            yield return Step(5, "World Exploration", RunWorldExploreStep());

            // ── Step 6: Monster Hunt ──────────────────────────────────────────
            yield return Step(6, "Monster Hunt", RunMonsterHuntStep());

            // ── Step 7: Resource Gathering ────────────────────────────────────
            yield return Step(7, "Resource Gathering", RunResourceGatherStep());

            // ── Step 8: Battle Report ─────────────────────────────────────────
            yield return Step(8, "Battle Report", RunBattleReportStep());

            // ── Step 9: Return Home ───────────────────────────────────────────
            yield return Step(9, "Return Home", RunReturnHomeStep());

            Debug.Log("[AlphaDemoController] ✅ Alpha Demo complete.");
            _running = false;
            HideOverlay();

            if (loopDemo)
            {
                yield return new WaitForSeconds(3f);
                StartDemo();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Step Implementations
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator RunLoginStep()
        {
            // Play login cinematic (aerial world pan)
            CinematicCameraManager.Instance?.PlayLoginCinematic();
            // Simulate login delay
            yield return new WaitForSeconds(loginDisplayTime);
            // In real build: trigger AuthManager auto-login with demo credentials
        }

        private IEnumerator RunKingdomEntryStep()
        {
            // Load kingdom scene if needed
            if (SceneManager.GetActiveScene().name != kingdomSceneName)
            {
                yield return SceneManager.LoadSceneAsync(kingdomSceneName, LoadSceneMode.Single);
                yield return new WaitForSeconds(1f); // scene settle
                FindKingdomRoot();
            }

            // Play entry cinematic
            CinematicCameraManager.Instance?.PlayKingdomEntryCinematic(kingdomRoot);
            yield return new WaitForSeconds(kingdomBrowseTime);
        }

        private IEnumerator RunKingdomInteractionStep()
        {
            // Focus on Palace
            if (kingdomRoot != null)
                CinematicCameraManager.Instance?.FocusOn(kingdomRoot);

            yield return new WaitForSeconds(buildingInteractTime);

            // Simulate building upgrade VFX
            Vector3 palacePos = kingdomRoot != null ? kingdomRoot.position : Vector3.zero;
            AlphaVFXController.Instance?.PlayBuildingComplete(palacePos + Vector3.up * 3f);

            yield return new WaitForSeconds(2f);
            CinematicCameraManager.Instance?.ReleaseFocus();
        }

        private IEnumerator RunWorldTransitionStep()
        {
            // Cinematic zoom-out to world scene
            CinematicCameraManager.Instance?.PlayWorldToKingdomTransition(kingdomRoot);
            yield return new WaitForSeconds(2.5f);

            // Load world scene
            yield return SceneManager.LoadSceneAsync(worldSceneName, LoadSceneMode.Single);
            yield return new WaitForSeconds(1f);

            FindWorldEntities();
        }

        private IEnumerator RunWorldExploreStep()
        {
            // Flythrough world waypoints
            CinematicCameraManager.Instance?.PlayFlythroughCinematic();
            yield return new WaitForSeconds(worldExploreTime);
        }

        private IEnumerator RunMonsterHuntStep()
        {
            if (nearestMonster != null)
                CinematicCameraManager.Instance?.FocusOn(nearestMonster);

            yield return new WaitForSeconds(monsterHuntTime * 0.5f);

            // Simulate monster death
            Vector3 monsterPos = nearestMonster != null ? nearestMonster.position : Vector3.forward * 10f;
            AlphaVFXController.Instance?.PlayMonsterDeath(monsterPos);
            yield return new WaitForSeconds(monsterHuntTime * 0.5f);

            CinematicCameraManager.Instance?.ReleaseFocus();
        }

        private IEnumerator RunResourceGatherStep()
        {
            if (nearestResourceNode != null)
                CinematicCameraManager.Instance?.FocusOn(nearestResourceNode);

            yield return new WaitForSeconds(resourceGatherTime * 0.5f);

            Vector3 nodePos = nearestResourceNode != null ? nearestResourceNode.position : Vector3.right * 10f;
            AlphaVFXController.Instance?.PlayResourceGather(nodePos + Vector3.up);
            yield return new WaitForSeconds(resourceGatherTime * 0.5f);

            AlphaVFXController.Instance?.PlayResourceDepleted(nodePos + Vector3.up);
            CinematicCameraManager.Instance?.ReleaseFocus();
        }

        private IEnumerator RunBattleReportStep()
        {
            // Battle victory camera
            Vector3 center = nearestMonster != null ? nearestMonster.position : Vector3.zero;
            CinematicCameraManager.Instance?.PlayBattleVictoryCamera(
                nearestMonster ?? new GameObject("tmp").transform);
            yield return new WaitForSeconds(3f);

            AlphaVFXController.Instance?.PlayLootExplosion(center + Vector3.up * 2f);
            AlphaVFXController.Instance?.PlayLevelUp(center + Vector3.up * 4f);
            yield return new WaitForSeconds(battleReportTime);
        }

        private IEnumerator RunReturnHomeStep()
        {
            // Fly back toward kingdom
            if (kingdomRoot != null)
                CinematicCameraManager.Instance?.PlayKingdomEntryCinematic(kingdomRoot);
            yield return new WaitForSeconds(returnHomeTime);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator Step(int index, string name, IEnumerator routine)
        {
            _stepIndex = index;
            ShowOverlay(name, index);
            Debug.Log($"[AlphaDemoController] ── Step {index}/{TotalSteps}: {name}");
            yield return StartCoroutine(routine);
        }

        private void FindKingdomRoot()
        {
            var ksc = FindAnyObjectByType<Kingdom.KingdomSceneController>();
            if (ksc != null) kingdomRoot = ksc.transform;
        }

        private void FindWorldEntities()
        {
            var monster = FindAnyObjectByType<Monsters.MonsterSpawnController>();
            if (monster != null) nearestMonster = monster.transform;

            var node = FindAnyObjectByType<ResourceNodeVisual>();
            if (node != null) nearestResourceNode = node.transform;
        }

        private void ShowOverlay(string stepName, int idx)
        {
            if (demoOverlayPanel != null) demoOverlayPanel.SetActive(true);
            if (stepLabel   != null) stepLabel.text   = stepName;
            if (progressLabel != null) progressLabel.text = $"{idx} / {TotalSteps}";
        }

        private void HideOverlay()
        {
            if (demoOverlayPanel != null) demoOverlayPanel.SetActive(false);
        }
    }
}
