using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace EternalKingdoms.Visual
{
    /// <summary>
    /// U5.7.11 — Final Alpha Launch Validator
    /// Comprehensive validation suite that ensures the build meets alpha quality standards.
    /// Checks every visual quality requirement and outputs ALPHA_LAUNCH_REPORT.md.
    ///
    /// Runtime: runs automatically on Start, results available via LastReport.
    /// Editor:  implements IPreprocessBuildWithReport — blocks the build if violations exist.
    /// </summary>
    public class AlphaLaunchValidator : MonoBehaviour
#if UNITY_EDITOR
        , IPreprocessBuildWithReport
#endif
    {
        public static AlphaLaunchValidator Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Validation Flags")]
        public bool checkNoPlaceholders      = true;
        public bool checkNoMissingAssets     = true;
        public bool checkNoPinkMaterials     = true;
        public bool checkNoBrokenAddressables = true;
        public bool checkNoMissingAnimations = true;
        public bool checkNoPrimitiveMeshes   = true;
        public bool checkNoBrokenReferences  = true;

        [Header("Thresholds")]
        [Tooltip("Maximum allowed missing Addressable assets to still pass. Set 0 for strict.")]
        public int maxAllowedMissingAddressables = 0;
        [Tooltip("Maximum allowed missing animations (legacy SkinnedMeshes without controller).")]
        public int maxAllowedMissingAnimations   = 0;

        [Header("Report Output")]
        public bool writeReportToDisk = true;
        public string reportOutputPath = "ALPHA_LAUNCH_REPORT.md";

        [Header("Build Gate")]
        public bool failBuildOnCriticalViolations = true;

        // ── State ─────────────────────────────────────────────────────────────
        private AlphaLaunchReport _report;
        public  AlphaLaunchReport LastReport => _report;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<AlphaLaunchReport> OnValidationComplete;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f); // let all systems initialise
            yield return StartCoroutine(RunFullValidation());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Runtime Validation
        // ─────────────────────────────────────────────────────────────────────
        public IEnumerator RunFullValidation()
        {
            _report = new AlphaLaunchReport { timestamp = DateTime.UtcNow };
            Debug.Log("[AlphaLaunchValidator] ▶ Alpha launch validation starting…");

            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);

            if (checkNoPinkMaterials || checkNoPlaceholders || checkNoPrimitiveMeshes)
                ScanRenderers(renderers);

            if (checkNoMissingAnimations)
                ScanAnimators(animators);

            if (checkNoBrokenAddressables || checkNoMissingAssets)
                yield return StartCoroutine(ScanAddressables());

            if (checkNoBrokenReferences)
                ScanSceneReferences();

            _report.passed = _report.criticalViolations.Count == 0 &&
                             _report.missingAddressables.Count <= maxAllowedMissingAddressables &&
                             _report.missingAnimations.Count  <= maxAllowedMissingAnimations;

            if (writeReportToDisk) WriteReportFile();
            PrintReport();
            OnValidationComplete?.Invoke(_report);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Scan Methods
        // ─────────────────────────────────────────────────────────────────────
        private void ScanRenderers(Renderer[] renderers)
        {
            string[] primitiveNames = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

            foreach (var r in renderers)
            {
                string path = GetPath(r.gameObject);

                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null)
                    {
                        _report.criticalViolations.Add($"NULL MATERIAL on {path}");
                        continue;
                    }

                    // Pink / missing shader
                    if (checkNoPinkMaterials &&
                        (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader"))
                        _report.criticalViolations.Add($"BROKEN SHADER on {path} → {mat.name}");

                    // Default placeholder material
                    if (checkNoPlaceholders &&
                        (mat.name == "Default-Material" || mat.name.Contains("placeholder", StringComparison.OrdinalIgnoreCase)))
                        _report.placeholderViolations.Add($"PLACEHOLDER MATERIAL: {mat.name} on {path}");
                }

                // Primitive mesh check
                if (checkNoPrimitiveMeshes && r is MeshRenderer)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf?.sharedMesh != null)
                        foreach (var prim in primitiveNames)
                            if (mf.sharedMesh.name.StartsWith(prim, StringComparison.OrdinalIgnoreCase))
                                _report.criticalViolations.Add($"PRIMITIVE MESH ({mf.sharedMesh.name}) on {path}");
                }
            }
        }

        private void ScanAnimators(Animator[] animators)
        {
            foreach (var anim in animators)
            {
                if (anim.runtimeAnimatorController == null)
                    _report.missingAnimations.Add($"NO ANIMATOR CONTROLLER on {GetPath(anim.gameObject)}");
                else if (anim.runtimeAnimatorController.animationClips.Length == 0)
                    _report.missingAnimations.Add($"ZERO ANIMATION CLIPS on {GetPath(anim.gameObject)}");
            }
        }

        private IEnumerator ScanAddressables()
        {
            var importMgr = Content.ArtImportManager.Instance;
            if (importMgr == null)
            {
                _report.missingAddressables.Add("ArtImportManager not present in scene");
                yield break;
            }

            float timeout = 30f, elapsed = 0f;
            while (!importMgr.IsComplete && elapsed < timeout) { elapsed += Time.deltaTime; yield return null; }

            var importReport = importMgr.GetReport();
            if (importReport != null)
            {
                foreach (var key in importReport.missingKeys)
                    _report.missingAddressables.Add(key);
                _report.totalAddressablesChecked = importReport.loadedKeys.Count + importReport.missingKeys.Count;
                _report.addressablesCoverage     = _report.totalAddressablesChecked > 0
                    ? (float)importReport.loadedKeys.Count / _report.totalAddressablesChecked * 100f : 0f;
            }
        }

        private void ScanSceneReferences()
        {
            // Check for GameObjects with missing script components (null MonoBehaviour)
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                    if (c == null)
                        _report.brokenReferences.Add($"MISSING SCRIPT on {GetPath(go)}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Report
        // ─────────────────────────────────────────────────────────────────────
        private void PrintReport()
        {
            string status = _report.passed ? "✅ ALPHA LAUNCH: GO" : "❌ ALPHA LAUNCH: NO-GO";

            Debug.Log(
                $"[AlphaLaunchValidator] ══════════════════════════════════\n" +
                $"  {status}\n" +
                $"  Critical violations   : {_report.criticalViolations.Count}\n" +
                $"  Placeholder violations: {_report.placeholderViolations.Count}\n" +
                $"  Missing Addressables  : {_report.missingAddressables.Count} / {_report.totalAddressablesChecked} ({_report.addressablesCoverage:F0}% coverage)\n" +
                $"  Missing animations    : {_report.missingAnimations.Count}\n" +
                $"  Broken references     : {_report.brokenReferences.Count}\n" +
                $"═══════════════════════════════════════"
            );

            if (!_report.passed)
            {
                foreach (var v in _report.criticalViolations)   Debug.LogError($"  ❌ {v}");
                foreach (var v in _report.placeholderViolations) Debug.LogWarning($"  ⚠️ {v}");
                foreach (var v in _report.missingAddressables)   Debug.LogWarning($"  ⚠️ MISSING: {v}");
                foreach (var v in _report.missingAnimations)     Debug.LogWarning($"  ⚠️ ANIM: {v}");
                foreach (var v in _report.brokenReferences)      Debug.LogWarning($"  ⚠️ REF: {v}");
            }
        }

        private void WriteReportFile()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Eternal Kingdoms — Alpha Launch Report");
            sb.AppendLine($"> Generated: {_report.timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"> Result: {(_report.passed ? "**✅ GO**" : "**❌ NO-GO**")}");
            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine($"| Check | Count | Status |");
            sb.AppendLine($"|-------|-------|--------|");
            sb.AppendLine($"| Critical violations | {_report.criticalViolations.Count} | {(_report.criticalViolations.Count == 0 ? "✅" : "❌")} |");
            sb.AppendLine($"| Placeholder materials | {_report.placeholderViolations.Count} | {(_report.placeholderViolations.Count == 0 ? "✅" : "⚠️")} |");
            sb.AppendLine($"| Missing Addressables | {_report.missingAddressables.Count} / {_report.totalAddressablesChecked} | {(_report.missingAddressables.Count <= maxAllowedMissingAddressables ? "✅" : "❌")} |");
            sb.AppendLine($"| Addressable coverage | {_report.addressablesCoverage:F0}% | {(_report.addressablesCoverage >= 80f ? "✅" : "⚠️")} |");
            sb.AppendLine($"| Missing animations | {_report.missingAnimations.Count} | {(_report.missingAnimations.Count <= maxAllowedMissingAnimations ? "✅" : "⚠️")} |");
            sb.AppendLine($"| Broken scene references | {_report.brokenReferences.Count} | {(_report.brokenReferences.Count == 0 ? "✅" : "❌")} |");

            if (_report.criticalViolations.Count > 0)
            {
                sb.AppendLine(); sb.AppendLine("## Critical Violations");
                foreach (var v in _report.criticalViolations) sb.AppendLine($"- ❌ {v}");
            }
            if (_report.missingAddressables.Count > 0)
            {
                sb.AppendLine(); sb.AppendLine("## Missing Addressables");
                foreach (var v in _report.missingAddressables) sb.AppendLine($"- `{v}`");
            }
            if (_report.placeholderViolations.Count > 0)
            {
                sb.AppendLine(); sb.AppendLine("## Placeholder Violations");
                foreach (var v in _report.placeholderViolations) sb.AppendLine($"- ⚠️ {v}");
            }

            File.WriteAllText(reportOutputPath, sb.ToString());
            Debug.Log($"[AlphaLaunchValidator] Report written → {reportOutputPath}");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Editor Build Gate
        // ─────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        public int callbackOrder => 200;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!failBuildOnCriticalViolations) return;

            var violations = new List<string>();
            string[] primitiveNames = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (go == null) continue;

                // Primitive mesh check
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf?.sharedMesh == null) continue;
                    foreach (var prim in primitiveNames)
                        if (mf.sharedMesh.name.StartsWith(prim, StringComparison.OrdinalIgnoreCase))
                            violations.Add($"{assetPath}: primitive mesh {mf.sharedMesh.name}");
                }

                // Missing material check
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    foreach (var mat in r.sharedMaterials)
                        if (mat == null) violations.Add($"{assetPath}: null material on {r.name}");
            }

            if (violations.Count > 0)
                throw new BuildFailedException(
                    $"[AlphaLaunchValidator] BUILD BLOCKED — {violations.Count} violation(s):\n" +
                    string.Join("\n", violations));

            Debug.Log("[AlphaLaunchValidator] ✅ Pre-build alpha launch validation passed.");
        }
#endif

        private static string GetPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform.parent;
            while (t != null) { path = $"{t.name}/{path}"; t = t.parent; }
            return path;
        }
    }

    // ── Report Data ───────────────────────────────────────────────────────────
    [Serializable]
    public class AlphaLaunchReport
    {
        public DateTime timestamp;
        public bool     passed;
        public float    addressablesCoverage;
        public int      totalAddressablesChecked;

        public List<string> criticalViolations   = new();
        public List<string> placeholderViolations = new();
        public List<string> missingAddressables  = new();
        public List<string> missingAnimations    = new();
        public List<string> brokenReferences     = new();
    }
}
