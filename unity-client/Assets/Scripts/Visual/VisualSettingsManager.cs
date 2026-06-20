using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections.Generic;

namespace EternalKingdoms.Visual
{
    /// <summary>
    /// VisualSettingsManager — Manages URP quality profiles and post-processing.
    ///
    /// Phase 5 (U5.1) responsibilities:
    ///   - Four quality tiers: Low / Medium / High / Ultra
    ///   - Auto-detects target tier from device GPU/CPU score
    ///   - Controls: Bloom, Ambient Occlusion, Color Grading, Tone Mapping,
    ///               Volumetric Fog, Soft Shadows, HDR, Anti-aliasing
    ///   - Persists player override in PlayerPrefs
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - URP Renderer Feature overrides applied at runtime
    ///   - VolumeProfile swapped per tier (set in Inspector)
    /// </summary>
    public class VisualSettingsManager : MonoBehaviour
    {
        public static VisualSettingsManager Instance { get; private set; }

        public enum QualityTier { Low = 0, Medium = 1, High = 2, Ultra = 3 }

        private const string PrefKey = "EK_QualityTier";

        [Header("Volume Profiles — assign per tier in Inspector")]
        [SerializeField] private VolumeProfile profileLow;
        [SerializeField] private VolumeProfile profileMedium;
        [SerializeField] private VolumeProfile profileHigh;
        [SerializeField] private VolumeProfile profileUltra;

        [Header("Global Volume")]
        [SerializeField] private Volume globalVolume;

        [Header("URP Asset Overrides")]
        [SerializeField] private UniversalRenderPipelineAsset urpAssetLow;
        [SerializeField] private UniversalRenderPipelineAsset urpAssetMedium;
        [SerializeField] private UniversalRenderPipelineAsset urpAssetHigh;
        [SerializeField] private UniversalRenderPipelineAsset urpAssetUltra;

        public QualityTier CurrentTier { get; private set; }
        public event Action<QualityTier> OnTierChanged;

        // Per-tier settings table
        private static readonly Dictionary<QualityTier, TierSettings> s_tiers = new()
        {
            [QualityTier.Low] = new TierSettings
            {
                shadowDistance      = 50f,
                shadowCascades      = 1,
                msaaSamples         = 1,
                hdr                 = false,
                bloomIntensity      = 0f,
                aoIntensity         = 0f,
                fogEnabled          = false,
                renderScale         = 0.75f,
            },
            [QualityTier.Medium] = new TierSettings
            {
                shadowDistance      = 100f,
                shadowCascades      = 2,
                msaaSamples         = 2,
                hdr                 = true,
                bloomIntensity      = 0.4f,
                aoIntensity         = 0.5f,
                fogEnabled          = false,
                renderScale         = 1.0f,
            },
            [QualityTier.High] = new TierSettings
            {
                shadowDistance      = 150f,
                shadowCascades      = 4,
                msaaSamples         = 4,
                hdr                 = true,
                bloomIntensity      = 0.7f,
                aoIntensity         = 0.8f,
                fogEnabled          = true,
                renderScale         = 1.0f,
            },
            [QualityTier.Ultra] = new TierSettings
            {
                shadowDistance      = 250f,
                shadowCascades      = 4,
                msaaSamples         = 8,
                hdr                 = true,
                bloomIntensity      = 1.0f,
                aoIntensity         = 1.0f,
                fogEnabled          = true,
                renderScale         = 1.0f,
            },
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var saved = PlayerPrefs.HasKey(PrefKey)
                ? (QualityTier)PlayerPrefs.GetInt(PrefKey)
                : AutoDetectTier();
            ApplyTier(saved);
        }

        /// <summary>Apply a quality tier immediately.</summary>
        public void ApplyTier(QualityTier tier)
        {
            CurrentTier = tier;
            PlayerPrefs.SetInt(PrefKey, (int)tier);

            var settings = s_tiers[tier];

            // Swap URP asset
            var urpAsset = tier switch
            {
                QualityTier.Low    => urpAssetLow,
                QualityTier.Medium => urpAssetMedium,
                QualityTier.High   => urpAssetHigh,
                QualityTier.Ultra  => urpAssetUltra,
                _                  => urpAssetMedium,
            };
            if (urpAsset != null)
                GraphicsSettings.defaultRenderPipeline = urpAsset;

            // Swap post-processing profile
            if (globalVolume != null)
            {
                globalVolume.profile = tier switch
                {
                    QualityTier.Low    => profileLow,
                    QualityTier.Medium => profileMedium,
                    QualityTier.High   => profileHigh,
                    QualityTier.Ultra  => profileUltra,
                    _                  => profileMedium,
                };

                // Drive Bloom intensity
                if (globalVolume.profile.TryGet<Bloom>(out var bloom))
                {
                    bloom.intensity.Override(settings.bloomIntensity);
                    bloom.active = settings.bloomIntensity > 0f;
                }

                // Drive Ambient Occlusion
                if (globalVolume.profile.TryGet<ScreenSpaceAmbientOcclusion>(out var ao))
                {
                    ao.intensity.Override(settings.aoIntensity);
                    ao.active = settings.aoIntensity > 0f;
                }
            }

            // Unity quality level (shadow cascade / distance driven by URP asset)
            QualitySettings.renderPipeline = urpAsset;
            QualitySettings.shadowDistance  = settings.shadowDistance;

            OnTierChanged?.Invoke(tier);
            Debug.Log($"[VisualSettings] Applied tier: {tier}");
        }

        public void CycleUp()
        {
            int next = Mathf.Min((int)CurrentTier + 1, (int)QualityTier.Ultra);
            ApplyTier((QualityTier)next);
        }

        public void CycleDown()
        {
            int prev = Mathf.Max((int)CurrentTier - 1, (int)QualityTier.Low);
            ApplyTier((QualityTier)prev);
        }

        // ── Auto-detection ────────────────────────────────────────────────────

        private static QualityTier AutoDetectTier()
        {
#if UNITY_WEBGL
            return QualityTier.Medium;
#elif UNITY_ANDROID || UNITY_IOS
            int gpuMem = SystemInfo.graphicsMemorySize;
            return gpuMem >= 4096 ? QualityTier.High
                 : gpuMem >= 2048 ? QualityTier.Medium
                 : QualityTier.Low;
#else
            int gpuMem = SystemInfo.graphicsMemorySize;
            return gpuMem >= 8192 ? QualityTier.Ultra
                 : gpuMem >= 4096 ? QualityTier.High
                 : gpuMem >= 2048 ? QualityTier.Medium
                 : QualityTier.Low;
#endif
        }

        private struct TierSettings
        {
            public float  shadowDistance;
            public int    shadowCascades;
            public int    msaaSamples;
            public bool   hdr;
            public float  bloomIntensity;
            public float  aoIntensity;
            public bool   fogEnabled;
            public float  renderScale;
        }
    }
}
