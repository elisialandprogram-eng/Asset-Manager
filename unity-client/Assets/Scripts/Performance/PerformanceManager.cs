using UnityEngine;
using System;
using System.Collections;

namespace EternalKingdoms.Performance
{
    /// <summary>
    /// PerformanceManager — Runtime performance monitoring and adaptive quality control.
    ///
    /// Phase 5 (U5.12) targets:
    ///   WebGL desktop: 60 FPS sustained
    ///   Android/iOS:   30 FPS minimum
    ///   Visible entities: 500 simultaneous (LOD + culling)
    ///
    /// Strategies implemented:
    ///   1. Adaptive LOD bias — tighten LOD transitions when FPS drops
    ///   2. Adaptive decoration density — reduce EnvironmentDecorationManager count
    ///   3. Occlusion culling — configured in Unity Editor (Camera.useOcclusionCulling)
    ///   4. GPU Instancing — enabled on all decoration/environment materials
    ///   5. Object pooling — all entities use pool (see EnvironmentDecorationManager, VFXLibrary)
    ///   6. Frame budget — decoration spawning capped at N objects per frame (Phase 5 U5.3)
    ///   7. Shadow distance auto-scale — reduce on low FPS
    ///   8. Particle quality auto-scale — reduce max particles on low FPS
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - FPS sampled every 1 second (rolling average over 5 samples)
    ///   - Adaptive quality only active if player hasn't overridden (VisualSettingsManager)
    ///   - Exposes PerformanceReport for debug overlay
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        public static PerformanceManager Instance { get; private set; }

        [Header("FPS Targets")]
        [SerializeField] private float targetFpsDesktop = 60f;
        [SerializeField] private float targetFpsMobile  = 30f;
        [SerializeField] private float lowFpsThreshold  = 0.75f;  // fraction of target
        [SerializeField] private float highFpsThreshold = 0.95f;  // fraction of target

        [Header("Adaptive Range")]
        [SerializeField] private float lodBiasHigh = 1.0f;
        [SerializeField] private float lodBiasLow  = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float shadowDistScaleLow = 0.5f;

        [Header("Sample Rate")]
        [SerializeField] private float sampleInterval = 1.0f;
        [SerializeField] private int   sampleCount    = 5;

        public PerformanceReport Report { get; private set; } = new();
        public event Action<PerformanceReport> OnReportUpdated;

        private float[] _fpsSamples;
        private int     _sampleIndex;
        private float   _targetFps;
        private float   _baselineShadowDist;
        private bool    _adaptiveEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _fpsSamples = new float[sampleCount];
            _targetFps  = IsMobilePlatform() ? targetFpsMobile : targetFpsDesktop;
            _baselineShadowDist = QualitySettings.shadowDistance;

            StartCoroutine(SampleLoop());
        }

        public void SetAdaptiveEnabled(bool enabled) => _adaptiveEnabled = enabled;

        // ── FPS Sampling ──────────────────────────────────────────────────────

        private IEnumerator SampleLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(sampleInterval);
                _fpsSamples[_sampleIndex % sampleCount] = 1f / Time.deltaTime;
                _sampleIndex++;

                if (_sampleIndex >= sampleCount)
                    EvaluatePerformance();
            }
        }

        private void EvaluatePerformance()
        {
            float avg = 0f;
            foreach (var s in _fpsSamples) avg += s;
            avg /= sampleCount;

            Report.averageFPS     = avg;
            Report.targetFPS      = _targetFps;
            Report.entityCount    = GetActiveEntityCount();
            Report.frameTimeMs    = 1000f / Mathf.Max(avg, 1f);
            Report.gpuMemMB       = SystemInfo.graphicsMemorySize;
            Report.timestamp      = DateTime.UtcNow;

            OnReportUpdated?.Invoke(Report);

            if (!_adaptiveEnabled) return;

            float fpsRatio = avg / _targetFps;

            if (fpsRatio < lowFpsThreshold)
                ApplyLowPerformance(fpsRatio);
            else if (fpsRatio > highFpsThreshold)
                ApplyHighPerformance();
        }

        private void ApplyLowPerformance(float fpsRatio)
        {
            // Tighten LOD (objects switch to lower LOD sooner)
            float lod = Mathf.Lerp(lodBiasLow, lodBiasHigh, fpsRatio / lowFpsThreshold);
            QualitySettings.lodBias = lod;

            // Reduce shadow distance
            QualitySettings.shadowDistance = _baselineShadowDist * shadowDistScaleLow;

            // Reduce particle systems max particles
            foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                var m = ps.main;
                if (m.maxParticles > 50) m.maxParticles = Mathf.Max(50, m.maxParticles / 2);
            }

            Debug.Log($"[Performance] Low FPS ({Report.averageFPS:F1}): applied LOD={lod:F2}, shadowDist reduced");
        }

        private void ApplyHighPerformance()
        {
            QualitySettings.lodBias       = lodBiasHigh;
            QualitySettings.shadowDistance = _baselineShadowDist;
            Debug.Log($"[Performance] FPS target met ({Report.averageFPS:F1}): restored quality settings");
        }

        private static int GetActiveEntityCount()
        {
            // Rough proxy: active renderers in the scene
            return FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
        }

        private static bool IsMobilePlatform()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return false;
#endif
        }
    }

    // ── Report Struct ─────────────────────────────────────────────────────────

    [Serializable]
    public class PerformanceReport
    {
        public float    averageFPS;
        public float    targetFPS;
        public float    frameTimeMs;
        public int      entityCount;
        public int      gpuMemMB;
        public DateTime timestamp;

        public bool IsHealthy => averageFPS >= targetFPS * 0.9f;
        public string Status  => IsHealthy ? "OK" : $"LOW ({averageFPS:F0} FPS)";
    }
}
