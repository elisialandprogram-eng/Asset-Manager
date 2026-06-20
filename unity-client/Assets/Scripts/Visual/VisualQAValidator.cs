using System.Collections;
using System.Collections.Generic;
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
    /// U5.6.11 — Visual QA Validator
    /// Automated runtime validator that checks for missing materials, textures,
    /// Addressables, animations, placeholder assets, and primitive meshes.
    /// In Editor builds, blocks the build pipeline if any violations are found.
    /// </summary>
    public class VisualQAValidator : MonoBehaviour
#if UNITY_EDITOR
        , IPreprocessBuildWithReport
#endif
    {
        public static VisualQAValidator Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Validation Rules")]
        public bool checkMissingMaterials  = true;
        public bool checkMissingTextures   = true;
        public bool checkMissingAddressables = true;
        public bool checkMissingAnimations = true;
        public bool checkPlaceholderAssets = true;
        public bool checkPrimitiveMeshes   = true;

        [Header("Primitive Mesh Names (blocked)")]
        public string[] blockedMeshNames = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

        [Header("Build Gate")]
        [Tooltip("If true, build will fail when violations are found (Editor only).")]
        public bool failBuildOnViolations = true;

        // ── Results ───────────────────────────────────────────────────────────
        private QAReport _lastReport;
        public  QAReport LastReport => _lastReport;

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
            yield return null; // let scene finish loading
            yield return StartCoroutine(RunFullValidation());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Runtime Validation
        // ─────────────────────────────────────────────────────────────────────
        public IEnumerator RunFullValidation()
        {
            _lastReport = new QAReport();
            Debug.Log("[VisualQAValidator] ▶ Running visual QA checks…");

            // Gather all scene renderers
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);

            if (checkMissingMaterials  || checkMissingTextures || checkPrimitiveMeshes || checkPlaceholderAssets)
                CheckRenderers(renderers);

            if (checkMissingAnimations)
                CheckAnimators(animators);

            if (checkMissingAddressables)
                yield return StartCoroutine(CheckAddressableCatalog());

            PrintReport();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Checks
        // ─────────────────────────────────────────────────────────────────────
        private void CheckRenderers(Renderer[] renderers)
        {
            foreach (var r in renderers)
            {
                string path = GetPath(r.gameObject);

                foreach (var mat in r.sharedMaterials)
                {
                    // Missing material
                    if (checkMissingMaterials && mat == null)
                    {
                        _lastReport.missingMaterials.Add(path);
                        continue;
                    }

                    // Missing / pink shader
                    if (mat != null && mat.shader != null && mat.shader.name == "Hidden/InternalErrorShader")
                        _lastReport.missingMaterials.Add($"{path} [broken shader]");

                    // Missing textures on material
                    if (checkMissingTextures && mat != null)
                    {
                        foreach (var propName in new[] { "_MainTex", "_BaseMap", "_BumpMap" })
                        {
                            if (mat.HasProperty(propName) && mat.GetTexture(propName) == null)
                                _lastReport.missingTextures.Add($"{path} → {mat.name}.{propName}");
                        }
                    }
                }

                // Primitive mesh check
                if (checkPrimitiveMeshes && r is MeshRenderer mr)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        foreach (var blocked in blockedMeshNames)
                        {
                            if (mf.sharedMesh.name.StartsWith(blocked))
                            {
                                _lastReport.primitiveMeshes.Add($"{path} [{mf.sharedMesh.name}]");
                                break;
                            }
                        }
                    }
                }

                // Placeholder detection (grey default material)
                if (checkPlaceholderAssets && r.sharedMaterial != null &&
                    (r.sharedMaterial.name == "Default-Material" ||
                     r.sharedMaterial.name.Contains("placeholder") ||
                     r.sharedMaterial.name.Contains("Placeholder")))
                {
                    _lastReport.placeholderAssets.Add(path);
                }
            }
        }

        private void CheckAnimators(Animator[] animators)
        {
            foreach (var anim in animators)
            {
                if (anim.runtimeAnimatorController == null)
                    _lastReport.missingAnimations.Add($"{GetPath(anim.gameObject)} [no controller]");
                else if (anim.runtimeAnimatorController.animationClips.Length == 0)
                    _lastReport.missingAnimations.Add($"{GetPath(anim.gameObject)} [0 clips]");
            }
        }

        private IEnumerator CheckAddressableCatalog()
        {
            var registry = Content.ArtImportManager.Instance;
            if (registry == null)
            {
                _lastReport.addressableIssues.Add("ArtImportManager not found in scene.");
                yield break;
            }

            // Wait for import pipeline to finish
            float timeout = 30f;
            float elapsed = 0f;
            while (!registry.IsComplete && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            var importReport = registry.GetReport();
            if (importReport != null)
                foreach (var key in importReport.missingKeys)
                    _lastReport.addressableIssues.Add($"MISSING: {key}");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Report
        // ─────────────────────────────────────────────────────────────────────
        private void PrintReport()
        {
            bool clean = _lastReport.IsClean;
            string header = clean
                ? "[VisualQAValidator] ✅ ALL VISUAL QA CHECKS PASSED"
                : "[VisualQAValidator] ❌ VISUAL QA VIOLATIONS FOUND";

            string body =
                $"  Missing materials   : {_lastReport.missingMaterials.Count}\n" +
                $"  Missing textures    : {_lastReport.missingTextures.Count}\n" +
                $"  Addressable issues  : {_lastReport.addressableIssues.Count}\n" +
                $"  Missing animations  : {_lastReport.missingAnimations.Count}\n" +
                $"  Placeholder assets  : {_lastReport.placeholderAssets.Count}\n" +
                $"  Primitive meshes    : {_lastReport.primitiveMeshes.Count}";

            if (clean) Debug.Log($"{header}\n{body}");
            else       Debug.LogError($"{header}\n{body}");

            if (!clean)
            {
                LogList("Missing Materials",   _lastReport.missingMaterials);
                LogList("Missing Textures",    _lastReport.missingTextures);
                LogList("Addressable Issues",  _lastReport.addressableIssues);
                LogList("Missing Animations",  _lastReport.missingAnimations);
                LogList("Placeholder Assets",  _lastReport.placeholderAssets);
                LogList("Primitive Meshes",    _lastReport.primitiveMeshes);
            }
        }

        private static void LogList(string label, List<string> items)
        {
            if (items.Count == 0) return;
            Debug.LogWarning($"[VisualQAValidator] ── {label} ({items.Count}):\n  " +
                             string.Join("\n  ", items));
        }

        private static string GetPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform.parent;
            while (t != null) { path = $"{t.name}/{path}"; t = t.parent; }
            return path;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Editor Build Gate
        // ─────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!failBuildOnViolations) return;

            // Check for known placeholder patterns in project assets
            var allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            var violations = new List<string>();

            foreach (var guid in allPrefabs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (go == null) continue;

                var renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf?.sharedMesh == null) continue;

                    foreach (var blocked in new[] { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" })
                    {
                        if (mf.sharedMesh.name.StartsWith(blocked))
                        {
                            violations.Add($"{assetPath} uses primitive: {mf.sharedMesh.name}");
                            break;
                        }
                    }
                }
            }

            if (violations.Count > 0)
            {
                string msg = $"[VisualQAValidator] BUILD FAILED — {violations.Count} placeholder primitive(s) detected:\n" +
                             string.Join("\n", violations);
                throw new BuildFailedException(msg);
            }

            Debug.Log("[VisualQAValidator] ✅ Pre-build visual QA passed.");
        }
#endif
    }

    // ── Data ──────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class QAReport
    {
        public List<string> missingMaterials   = new();
        public List<string> missingTextures    = new();
        public List<string> addressableIssues  = new();
        public List<string> missingAnimations  = new();
        public List<string> placeholderAssets  = new();
        public List<string> primitiveMeshes    = new();

        public bool IsClean =>
            missingMaterials.Count   == 0 &&
            missingTextures.Count    == 0 &&
            addressableIssues.Count  == 0 &&
            missingAnimations.Count  == 0 &&
            placeholderAssets.Count  == 0 &&
            primitiveMeshes.Count    == 0;

        public int TotalViolations =>
            missingMaterials.Count + missingTextures.Count +
            addressableIssues.Count + missingAnimations.Count +
            placeholderAssets.Count + primitiveMeshes.Count;
    }
}
