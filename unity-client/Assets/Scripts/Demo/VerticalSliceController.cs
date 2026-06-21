using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using TMPro;

namespace EternalKingdoms.Demo
{
    /// <summary>
    /// VerticalSliceController — Orchestrates the complete player-facing vertical slice.
    ///
    /// Phase 5.5 (U5.5.10) target flow:
    ///   Login → Enter Kingdom → See rendered kingdom → Citizens walking →
    ///   Buildings animated → Open World → Forests/rivers/mountains visible →
    ///   Monsters roaming → Resource nodes active → Send march →
    ///   Watch animated banner move → Receive battle report
    ///
    /// Architecture:
    ///   - Attached to a GameManager-layer object (persists across scenes)
    ///   - Subscribes to existing manager events (KingdomManager, MarchManager,
    ///     WorldManager, BattleReportPanel) to drive UI narrative
    ///   - SliceStep enum tracks current position in the flow
    ///   - Each step has a visual prompt/overlay ("Welcome to your Kingdom!")
    ///   - Completable: after all steps trigger, marks slice as complete
    ///   - Can be disabled (player has completed tutorial) via PlayerPrefs
    ///
    /// Steps:
    ///   Step0_Login         — Auth success → transition to Kingdom
    ///   Step1_KingdomEnter  — Kingdom scene loaded, visual settled (2s)
    ///   Step2_CitizensAlive — First citizen animation detected
    ///   Step3_BuildingsAnim — First building idle animation plays
    ///   Step4_WorldOpen     — World scene entered
    ///   Step5_WorldVisuals  — Terrain + decorations streaming visible
    ///   Step6_MonstersSpawn — First monster entity visible
    ///   Step7_ResourceNodes — First resource node visible
    ///   Step8_MarchSent     — Player sends first march
    ///   Step9_BannerMoving  — MarchBannerEntity first frame update
    ///   Step10_BattleReport — BattleReportPanel shown
    ///   Complete            — All steps done; PlayerPrefs flag set
    /// </summary>
    public class VerticalSliceController : MonoBehaviour
    {
        public static VerticalSliceController Instance { get; private set; }

        private const string PrefKey = "EK_VerticalSliceComplete";

        public enum SliceStep
        {
            Step0_Login,
            Step1_KingdomEnter,
            Step2_CitizensAlive,
            Step3_BuildingsAnim,
            Step4_WorldOpen,
            Step5_WorldVisuals,
            Step6_MonstersSpawn,
            Step7_ResourceNodes,
            Step8_MarchSent,
            Step9_BannerMoving,
            Step10_BattleReport,
            Complete
        }

        [Header("Overlay UI")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private TextMeshProUGUI stepLabel;
        [SerializeField] private TextMeshProUGUI stepHint;
        [SerializeField] private float displayDuration = 3.5f;

        [Header("Skip")]
        [SerializeField] private bool skipIfComplete = true;

        public SliceStep CurrentStep { get; private set; } = SliceStep.Step0_Login;
        public bool IsComplete => CurrentStep == SliceStep.Complete;

        public event Action<SliceStep> OnStepReached;
        public event Action           OnSliceComplete;

#pragma warning disable CS0414
        private bool _overlayShowing;
#pragma warning restore CS0414

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (skipIfComplete && PlayerPrefs.GetInt(PrefKey, 0) == 1)
            {
                CurrentStep = SliceStep.Complete;
                if (overlayRoot != null) overlayRoot.SetActive(false);
                return;
            }

            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        // ── Public Step Triggers ──────────────────────────────────────────────
        // Call these from the relevant managers when the event occurs.

        public void TriggerLogin()           => AdvanceTo(SliceStep.Step1_KingdomEnter,
                                                   "Welcome to your Kingdom!",
                                                   "Your realm awaits…");

        public void TriggerKingdomLoaded()   => AdvanceTo(SliceStep.Step2_CitizensAlive,
                                                   "Your Kingdom is alive.",
                                                   "Watch your citizens go about their day.");

        public void TriggerCitizensAlive()   => AdvanceTo(SliceStep.Step3_BuildingsAnim,
                                                   "Your people thrive.",
                                                   "Every building tells a story.");

        public void TriggerBuildingsAnimated() => AdvanceTo(SliceStep.Step4_WorldOpen,
                                                   "A kingdom built on dreams.",
                                                   "Open the world map to expand your power.");

        public void TriggerWorldOpened()     => AdvanceTo(SliceStep.Step5_WorldVisuals,
                                                   "The world stretches before you.",
                                                   "Forests, rivers, mountains — and danger.");

        public void TriggerWorldVisuals()    => AdvanceTo(SliceStep.Step6_MonstersSpawn,
                                                   "Ancient threats roam the land.",
                                                   "Seek them out — great rewards await.");

        public void TriggerMonstersVisible() => AdvanceTo(SliceStep.Step7_ResourceNodes,
                                                   "Resources for the taking.",
                                                   "Gather them to fuel your war machine.");

        public void TriggerNodesVisible()    => AdvanceTo(SliceStep.Step8_MarchSent,
                                                   "Ready your armies.",
                                                   "Send a march to gather or attack.");

        public void TriggerMarchSent()       => AdvanceTo(SliceStep.Step9_BannerMoving,
                                                   "Your banner flies!",
                                                   "Watch your army march across the map.");

        public void TriggerBannerMoving()    => AdvanceTo(SliceStep.Step10_BattleReport,
                                                   "The battle has begun.",
                                                   "Await the outcome — victory or defeat?");

        public void TriggerBattleReport()    => AdvanceTo(SliceStep.Complete,
                                                   "The vertical slice is complete.",
                                                   "Eternal Kingdoms — Phase 5.5 ✅");

        // ── Internal ──────────────────────────────────────────────────────────

        private void AdvanceTo(SliceStep step, string title, string hint)
        {
            if ((int)step <= (int)CurrentStep) return;
            CurrentStep = step;
            OnStepReached?.Invoke(step);

            if (step == SliceStep.Complete)
            {
                PlayerPrefs.SetInt(PrefKey, 1);
                OnSliceComplete?.Invoke();
                Debug.Log("[VerticalSlice] ✅ Complete!");
            }

            StartCoroutine(ShowOverlay(title, hint));
        }

        private IEnumerator ShowOverlay(string title, string hint)
        {
            if (overlayRoot == null) yield break;

            overlayRoot.SetActive(true);
            if (stepLabel != null) stepLabel.text = title;
            if (stepHint  != null) stepHint.text  = hint;
            _overlayShowing = true;

            // Fade in
            var cg = overlayRoot.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                float t = 0f;
                while (t < 1f) { t += Time.deltaTime * 3f; cg.alpha = t; yield return null; }
            }

            yield return new WaitForSeconds(displayDuration);

            // Fade out
            if (cg != null)
            {
                float t = 1f;
                while (t > 0f) { t -= Time.deltaTime * 2f; cg.alpha = t; yield return null; }
            }

            overlayRoot.SetActive(false);
            _overlayShowing = false;
        }

        /// <summary>Reset vertical slice progress (for testing).</summary>
        [ContextMenu("Reset Vertical Slice")]
        public void ResetSlice()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            CurrentStep = SliceStep.Step0_Login;
            Debug.Log("[VerticalSlice] Reset.");
        }
    }
}
