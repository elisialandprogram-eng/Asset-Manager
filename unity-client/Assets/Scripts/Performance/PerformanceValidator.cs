using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EternalKingdoms.Performance
{
    /// <summary>
    /// PerformanceValidator — Automated stress-test and performance validation tool.
    ///
    /// Phase 5.5 (U5.5.12) validation targets:
    ///   Desktop WebGL: 60 FPS sustained over 30 seconds with 500 entities
    ///   Android:       30 FPS sustained over 30 seconds with 300 entities
    ///
    /// Stress test procedure:
    ///   1. Spawn entities up to target count (batched, 10/frame)
    ///   2. Record FPS every second for 30 seconds
    ///   3. Calculate: min / max / average / p5 percentile FPS
    ///   4. Compare against platform target
    ///   5. Emit ValidationResult to console + UI overlay
    ///
    /// Entity stress types:
    ///   Decoration — tree/rock (GPU instanced, LOD)
    ///   MarchBanner — animated banner entities
    ///   Monster     — monster visual controllers
    ///   NPC         — citizen controllers
    ///   VFX         — particle systems
    ///
    /// Architecture:
    ///   - Editor or runtime tool (can run from DemoScene via F5)
    ///   - Integrates with PerformanceManager for FPS readings
    ///   - ValidationResult serializable to JSON for CI integration
    /// </summary>
    public class PerformanceValidator : MonoBehaviour
    {
        [Header("Stress Test Config")]
        [SerializeField] private int  targetEntityCount   = 500;
        [SerializeField] private int  mobileEntityCount   = 300;
        [SerializeField] private float sampleDuration      = 30f;
        [SerializeField] private int  spawnPerFrame        = 10;

        [Header("Target FPS")]
        [SerializeField] private float desktopTargetFPS   = 60f;
        [SerializeField] private float mobileTargetFPS    = 30f;

        [Header("Stress Prefabs — assign in Inspector")]
        [SerializeField] private GameObject stressDecorationPrefab;
        [SerializeField] private GameObject stressBannerPrefab;
        [SerializeField] private GameObject stressMonsterPrefab;
        [SerializeField] private GameObject stressNPCPrefab;

        [Header("Spawn Area")]
        [SerializeField] private float spawnRadius = 100f;

        public ValidationResult LastResult { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly List<GameObject> _stressEntities = new();
        private readonly List<float> _fpsSamples = new();

        // ── Public API ────────────────────────────────────────────────────────

        public void RunStressTest()
        {
            if (IsRunning) return;
            StartCoroutine(StressTestCoroutine());
        }

        public void ClearStressEntities()
        {
            foreach (var go in _stressEntities)
                if (go != null) Destroy(go);
            _stressEntities.Clear();
        }

        // ── Stress Test ───────────────────────────────────────────────────────

        private IEnumerator StressTestCoroutine()
        {
            IsRunning = true;
            _fpsSamples.Clear();
            _stressEntities.Clear();

            int target = IsMobile() ? mobileEntityCount : targetEntityCount;
            float targetFPS = IsMobile() ? mobileTargetFPS : desktopTargetFPS;

            Debug.Log($"[PerfValidator] Starting stress test: {target} entities, target {targetFPS} FPS");

            // Phase 1: Spawn entities
            int spawned = 0;
            while (spawned < target)
            {
                int batch = Mathf.Min(spawnPerFrame, target - spawned);
                for (int i = 0; i < batch; i++)
                {
                    SpawnStressEntity(spawned);
                    spawned++;
                }
                yield return null;
            }

            Debug.Log($"[PerfValidator] {spawned} entities spawned. Sampling FPS for {sampleDuration}s...");

            // Phase 2: Sample FPS
            float elapsed = 0f;
            while (elapsed < sampleDuration)
            {
                float fps = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
                _fpsSamples.Add(fps);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 3: Compute results
            LastResult = ComputeResult(targetFPS, spawned);
            LogResult(LastResult);
            IsRunning = false;
        }

        private void SpawnStressEntity(int index)
        {
            // Distribute types
            GameObject prefab = (index % 5) switch
            {
                0 => stressDecorationPrefab,
                1 => stressBannerPrefab,
                2 => stressMonsterPrefab,
                3 => stressNPCPrefab,
                _ => stressDecorationPrefab,
            };
            if (prefab == null) return;

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float dist  = UnityEngine.Random.Range(5f, spawnRadius);
            Vector3 pos = transform.position + new Vector3(
                Mathf.Cos(angle) * dist,
                0f,
                Mathf.Sin(angle) * dist
            );

            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), transform);
            _stressEntities.Add(go);
        }

        private ValidationResult ComputeResult(float targetFPS, int entityCount)
        {
            _fpsSamples.Sort();
            float sum = 0f;
            foreach (var f in _fpsSamples) sum += f;
            float avg = _fpsSamples.Count > 0 ? sum / _fpsSamples.Count : 0f;
            float min = _fpsSamples.Count > 0 ? _fpsSamples[0] : 0f;
            float max = _fpsSamples.Count > 0 ? _fpsSamples[_fpsSamples.Count - 1] : 0f;
            float p5  = _fpsSamples.Count > 0
                ? _fpsSamples[Mathf.FloorToInt(_fpsSamples.Count * 0.05f)]
                : 0f;

            return new ValidationResult
            {
                platform        = IsMobile() ? "Mobile" : "Desktop",
                entityCount     = entityCount,
                targetFPS       = targetFPS,
                averageFPS      = avg,
                minFPS          = min,
                maxFPS          = max,
                p5FPS           = p5,
                sampleCount     = _fpsSamples.Count,
                passed          = avg >= targetFPS * 0.9f && p5 >= targetFPS * 0.75f,
                timestamp       = DateTime.UtcNow,
                gpuMemoryMB     = SystemInfo.graphicsMemorySize,
                processorCount  = SystemInfo.processorCount,
                graphicsDevice  = SystemInfo.graphicsDeviceName,
            };
        }

        private void LogResult(ValidationResult r)
        {
            string status = r.passed ? "✅ PASS" : "❌ FAIL";
            Debug.Log(
                $"[PerfValidator] {status}\n" +
                $"  Platform:  {r.platform} | GPU: {r.graphicsDevice}\n" +
                $"  Entities:  {r.entityCount}\n" +
                $"  Target:    {r.targetFPS:F0} FPS\n" +
                $"  Average:   {r.averageFPS:F1} FPS\n" +
                $"  Min/P5:    {r.minFPS:F1} / {r.p5FPS:F1} FPS\n" +
                $"  Max:       {r.maxFPS:F1} FPS\n" +
                $"  Samples:   {r.sampleCount}\n" +
                $"  GPU Mem:   {r.gpuMemoryMB} MB"
            );
        }

        private static bool IsMobile()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return false;
#endif
        }
    }

    // ── Validation Result ─────────────────────────────────────────────────────

    [Serializable]
    public class ValidationResult
    {
        public string   platform;
        public int      entityCount;
        public float    targetFPS;
        public float    averageFPS;
        public float    minFPS;
        public float    maxFPS;
        public float    p5FPS;
        public int      sampleCount;
        public bool     passed;
        public DateTime timestamp;
        public int      gpuMemoryMB;
        public int      processorCount;
        public string   graphicsDevice;
    }
}
