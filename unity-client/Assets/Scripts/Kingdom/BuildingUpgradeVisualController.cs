using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using EternalKingdoms.VFX;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// U5.7.2 — Building Visual Evolution
    /// Manages level-gated model swapping, upgrade transition animations,
    /// construction scaffolding, and completion celebrations.
    ///
    /// Visual tiers:
    ///   L1–L3  → Early settlement (weathered wood/stone)
    ///   L4–L6  → Developed structure (reinforced stone, banners)
    ///   L7–L10 → Advanced structure (grand architecture, gilded details)
    /// </summary>
    public class BuildingUpgradeVisualController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Building Identity")]
        public string buildingType;   // e.g. "palace", "barracks", "farm"
        public int    currentLevel    = 1;

        [Header("Addressable Keys — 3 Tiers (Early/Developed/Advanced)")]
        public string tierEarlyKey;       // L1–L3
        public string tierDevelopedKey;   // L4–L6
        public string tierAdvancedKey;    // L7–L10

        [Header("Construction")]
        public string scaffoldingKey;     // address of scaffolding prefab
        public string foundationKey;      // address of foundation prefab
        [Range(0.5f, 5f)] public float scaffoldingDuration = 3f;

        [Header("Transition")]
        [Range(0.05f, 1f)] public float swapFadeDuration = 0.35f;
        public AnimationCurve swapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Celebration VFX")]
        public bool playCelebrationOnComplete = true;

        // ── State ─────────────────────────────────────────────────────────────
        private GameObject _currentModel;
        private GameObject _scaffolding;
        private int        _displayedTier = -1;   // 0=Early, 1=Developed, 2=Advanced

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<int> OnLevelDisplayChanged;
        public event Action      OnUpgradeAnimationComplete;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Start() => ApplyLevelVisual(currentLevel, instant: true);

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Call when a building upgrade completes to play the full transition.</summary>
        public void OnBuildingUpgraded(int newLevel)
        {
            currentLevel = newLevel;
            StartCoroutine(UpgradeSequence(newLevel));
        }

        /// <summary>Snap immediately to the correct visual for the given level (no animation).</summary>
        public void ApplyLevelVisual(int level, bool instant = false)
        {
            int tier = LevelToTier(level);
            if (tier == _displayedTier && !instant) return;
            StartCoroutine(SwapModel(TierKey(tier), instant));
            _displayedTier = tier;
            OnLevelDisplayChanged?.Invoke(level);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Sequences
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator UpgradeSequence(int newLevel)
        {
            // Phase 1 — spawn scaffolding
            yield return StartCoroutine(ShowScaffolding());

            // Phase 2 — wait construction period
            yield return new WaitForSeconds(scaffoldingDuration);

            // Phase 3 — swap model
            int tier = LevelToTier(newLevel);
            _displayedTier = tier;
            yield return StartCoroutine(SwapModel(TierKey(tier), instant: false));

            // Phase 4 — remove scaffolding
            yield return StartCoroutine(HideScaffolding());

            // Phase 5 — celebration
            if (playCelebrationOnComplete)
                AlphaVFXController.Instance?.PlayBuildingComplete(transform.position + Vector3.up * 2f);

            OnUpgradeAnimationComplete?.Invoke();
        }

        private IEnumerator ShowScaffolding()
        {
            if (string.IsNullOrEmpty(scaffoldingKey)) yield break;
            var op = Addressables.InstantiateAsync(scaffoldingKey, transform.position, Quaternion.identity, transform);
            yield return op;
            if (op.Status == AsyncOperationStatus.Succeeded)
                _scaffolding = op.Result;
        }

        private IEnumerator HideScaffolding()
        {
            if (_scaffolding == null) yield break;
            // Fade out scaffolding over 0.5s
            var renderers = _scaffolding.GetComponentsInChildren<Renderer>();
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float alpha = 1f - t / 0.5f;
                foreach (var r in renderers)
                    foreach (var m in r.materials)
                        if (m.HasProperty("_Alpha")) m.SetFloat("_Alpha", alpha);
                yield return null;
            }
            Addressables.ReleaseInstance(_scaffolding);
            _scaffolding = null;
        }

        private IEnumerator SwapModel(string addressKey, bool instant)
        {
            // Fade out current
            if (_currentModel != null && !instant)
                yield return StartCoroutine(FadeRenderers(_currentModel, 1f, 0f, swapFadeDuration));

            // Destroy old
            if (_currentModel != null)
            {
                Addressables.ReleaseInstance(_currentModel);
                _currentModel = null;
            }

            if (string.IsNullOrEmpty(addressKey)) yield break;

            // Load new
            var op = Addressables.InstantiateAsync(addressKey, transform.position, transform.rotation, transform);
            yield return op;

            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[BuildingUpgradeVisualController] Could not load {addressKey}");
                yield break;
            }

            _currentModel = op.Result;

            // Fade in
            if (!instant)
                yield return StartCoroutine(FadeRenderers(_currentModel, 0f, 1f, swapFadeDuration));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator FadeRenderers(GameObject target, float from, float to, float dur)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, swapCurve.Evaluate(t / dur));
                foreach (var r in renderers)
                    foreach (var m in r.materials)
                        if (m.HasProperty("_Alpha")) m.SetFloat("_Alpha", alpha);
                yield return null;
            }
        }

        private static int LevelToTier(int level)
        {
            if (level <= 3) return 0;   // Early
            if (level <= 6) return 1;   // Developed
            return 2;                    // Advanced
        }

        private string TierKey(int tier) => tier switch
        {
            0 => tierEarlyKey,
            1 => tierDevelopedKey,
            _ => tierAdvancedKey
        };

        public static string TierLabel(int level) => LevelToTier(level) switch
        {
            0 => "Early Settlement",
            1 => "Developed Structure",
            _ => "Advanced Structure"
        };
    }
}
