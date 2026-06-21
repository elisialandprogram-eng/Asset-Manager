using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EternalKingdoms.Content
{
    /// <summary>
    /// U5.6.1 — Alpha Art Import Pipeline
    /// Bulk registers, validates, and auto-assigns all production art assets.
    /// Generates a startup report covering missing assets, broken references,
    /// unassigned materials, and missing animations.
    /// </summary>
    public class ArtImportManager : MonoBehaviour
    {
        public static ArtImportManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Validation Config")]
        [Tooltip("Fail the build if any placeholder assets are detected.")]
        public bool failBuildOnPlaceholder = true;
        [Tooltip("LOD count required on every prefab (LOD0 + LOD1 + LOD2 + Culled).")]
        public int requiredLODCount = 4;
        [Tooltip("Every skinned mesh must have at least this many animation clips.")]
        public int minimumAnimationClips = 3;

        [Header("Fallback")]
        public GameObject fallbackPrimitivePrefab;

        // ── Internal ─────────────────────────────────────────────────────────
        private ImportReport _report;
        private bool _validationComplete;

        // Key → loaded prefab
        private readonly Dictionary<string, GameObject> _loadedAssets = new();
        // Category → list of address keys
        private static readonly Dictionary<string, string[]> AssetManifest = BuildManifest();

        // ── Events ───────────────────────────────────────────────────────────
        public event Action<ImportReport> OnValidationComplete;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartCoroutine(RunImportPipeline());

        // ─────────────────────────────────────────────────────────────────────
        //  Pipeline
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator RunImportPipeline()
        {
            _report = new ImportReport { startTime = DateTime.UtcNow };
            Debug.Log("[ArtImportManager] ▶ Alpha Import Pipeline starting…");

            // Phase 1 — bulk load
            yield return StartCoroutine(BulkLoadAssets());

            // Phase 2 — validate each loaded prefab
            ValidateAllPrefabs();

            // Phase 3 — auto-assign where supported
            AutoAssignAssets();

            // Phase 4 — generate report
            _report.endTime = DateTime.UtcNow;
            _report.duration = (float)(_report.endTime - _report.startTime).TotalSeconds;
            _validationComplete = true;

            PrintReport();
            OnValidationComplete?.Invoke(_report);

            if (failBuildOnPlaceholder && _report.placeholderCount > 0)
                Debug.LogError($"[ArtImportManager] ❌ BUILD BLOCKED — {_report.placeholderCount} placeholder asset(s) detected.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Phase 1 — Bulk Load
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator BulkLoadAssets()
        {
            int total = AssetManifest.Values.Sum(v => v.Length);
            int loaded = 0;

            foreach (var (category, keys) in AssetManifest)
            {
                foreach (var key in keys)
                {
                    var op = Addressables.LoadAssetAsync<GameObject>(key);
                    yield return op;

                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedAssets[key] = op.Result;
                        _report.loadedKeys.Add(key);
                    }
                    else
                    {
                        _report.missingKeys.Add(key);
                        _report.missingByCategory.TryAdd(category, new List<string>());
                        _report.missingByCategory[category].Add(key);

                        if (fallbackPrimitivePrefab != null)
                            _loadedAssets[key] = fallbackPrimitivePrefab;
                    }
                    loaded++;
                    _report.progressPercent = (float)loaded / total * 100f;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Phase 2 — Validate Prefabs
        // ─────────────────────────────────────────────────────────────────────
        private void ValidateAllPrefabs()
        {
            foreach (var (key, prefab) in _loadedAssets)
            {
                if (prefab == null || prefab == fallbackPrimitivePrefab)
                {
                    _report.placeholderCount++;
                    continue;
                }

                ValidateLODGroup(key, prefab);
                ValidateAnimator(key, prefab);
                ValidateMaterials(key, prefab);
            }
        }

        private void ValidateLODGroup(string key, GameObject prefab)
        {
            var lod = prefab.GetComponentInChildren<LODGroup>();
            if (lod == null)
            {
                _report.lodIssues.Add($"[MISSING LOD GROUP] {key}");
                return;
            }
            if (lod.GetLODs().Length < requiredLODCount)
                _report.lodIssues.Add($"[INSUFFICIENT LODs: {lod.GetLODs().Length}/{requiredLODCount}] {key}");
        }

        private void ValidateAnimator(string key, GameObject prefab)
        {
            var anim = prefab.GetComponentInChildren<Animator>();
            if (anim == null) return; // Static mesh — OK

            if (anim.runtimeAnimatorController == null)
            {
                _report.animatorIssues.Add($"[NULL ANIMATOR CONTROLLER] {key}");
                return;
            }

            int clipCount = anim.runtimeAnimatorController.animationClips?.Length ?? 0;
            if (clipCount < minimumAnimationClips)
                _report.animatorIssues.Add($"[FEW CLIPS: {clipCount}/{minimumAnimationClips}] {key}");
        }

        private void ValidateMaterials(string key, GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null)
                    {
                        _report.materialIssues.Add($"[NULL MATERIAL on {r.name}] {key}");
                        continue;
                    }
                    // Detect Unity default pink/missing material
                    if (mat.name.Contains("Default-Material") || mat.name.Contains("Missing"))
                        _report.materialIssues.Add($"[DEFAULT/MISSING MATERIAL: {mat.name} on {r.name}] {key}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Phase 3 — Auto-Assign
        // ─────────────────────────────────────────────────────────────────────
        private void AutoAssignAssets()
        {
            // Wire building prefabs into KingdomVisualController if present in scene
            var kvc = FindAnyObjectByType<Kingdom.KingdomVisualController>();
            if (kvc != null) AutoAssignBuildings(kvc);

            // Wire monster prefabs into MonsterSpawnManager if present
            var msm = FindAnyObjectByType<World.MonsterSpawnManager>();
            if (msm != null) AutoAssignMonsters(msm);

            _report.autoAssignedCount = _report.autoAssignedKeys.Count;
        }

        private void AutoAssignBuildings(Kingdom.KingdomVisualController kvc)
        {
            string[] buildingKeys = AssetManifest["Buildings"];
            foreach (var key in buildingKeys)
            {
                if (_loadedAssets.TryGetValue(key, out var prefab) && prefab != fallbackPrimitivePrefab)
                {
                    // KingdomVisualController exposes a method to receive hot-swapped prefabs
                    kvc.RegisterBuildingPrefab(key, prefab);
                    _report.autoAssignedKeys.Add(key);
                }
            }
        }

        private void AutoAssignMonsters(World.MonsterSpawnManager msm)
        {
            string[] monsterKeys = AssetManifest["Monsters"];
            foreach (var key in monsterKeys)
            {
                if (_loadedAssets.TryGetValue(key, out var prefab) && prefab != fallbackPrimitivePrefab)
                {
                    msm.RegisterMonsterPrefab(key, prefab);
                    _report.autoAssignedKeys.Add(key);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Report
        // ─────────────────────────────────────────────────────────────────────
        private void PrintReport()
        {
            int total = AssetManifest.Values.Sum(v => v.Length);
            float coverage = total > 0 ? (float)_report.loadedKeys.Count / total * 100f : 0f;

            Debug.Log(
                $"[ArtImportManager] ══════════════════════════════════════\n" +
                $"  Import Complete in {_report.duration:F2}s\n" +
                $"  Total required : {total}\n" +
                $"  Loaded OK      : {_report.loadedKeys.Count} ({coverage:F0}%)\n" +
                $"  Missing        : {_report.missingKeys.Count}\n" +
                $"  Placeholders   : {_report.placeholderCount}\n" +
                $"  LOD issues     : {_report.lodIssues.Count}\n" +
                $"  Animator issues: {_report.animatorIssues.Count}\n" +
                $"  Material issues: {_report.materialIssues.Count}\n" +
                $"  Auto-assigned  : {_report.autoAssignedCount}\n" +
                $"══════════════════════════════════════"
            );

            if (_report.missingByCategory.Count > 0)
            {
                foreach (var (cat, keys) in _report.missingByCategory)
                    Debug.LogWarning($"[ArtImportManager] ⚠️ [{cat}] {keys.Count} missing: {string.Join(", ", keys)}");
            }

            if (_report.lodIssues.Count > 0)
                Debug.LogWarning($"[ArtImportManager] ⚠️ LOD Issues:\n  " + string.Join("\n  ", _report.lodIssues));

            if (_report.animatorIssues.Count > 0)
                Debug.LogWarning($"[ArtImportManager] ⚠️ Animator Issues:\n  " + string.Join("\n  ", _report.animatorIssues));

            if (_report.materialIssues.Count > 0)
                Debug.LogWarning($"[ArtImportManager] ⚠️ Material Issues:\n  " + string.Join("\n  ", _report.materialIssues));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────
        public GameObject GetAsset(string key) =>
            _loadedAssets.TryGetValue(key, out var p) ? p : fallbackPrimitivePrefab;

        public ImportReport GetReport() => _report;
        public bool IsComplete => _validationComplete;

        // ─────────────────────────────────────────────────────────────────────
        //  Manifest
        // ─────────────────────────────────────────────────────────────────────
        private static Dictionary<string, string[]> BuildManifest() => new()
        {
            ["Buildings"] = new[]
            {
                "Buildings/building_palace_001",   "Buildings/building_farm_001",
                "Buildings/building_lumbermill_001","Buildings/building_quarry_001",
                "Buildings/building_ironmine_001",  "Buildings/building_goldmine_001",
                "Buildings/building_barracks_001",  "Buildings/building_academy_001",
                "Buildings/building_hospital_001",  "Buildings/building_market_001",
                "Buildings/building_watchtower_001","Buildings/building_walls_001",
                "Buildings/building_embassy_001",   "Buildings/building_forge_001"
            },
            ["Monsters"] = new[]
            {
                "Monsters/monster_bandit_t1",       "Monsters/monster_bandit_t2",
                "Monsters/monster_bandit_t3",       "Monsters/monster_direwolf_t1",
                "Monsters/monster_direwolf_t2",     "Monsters/monster_direwolf_t3",
                "Monsters/monster_ogre_t2",         "Monsters/monster_ogre_t3",
                "Monsters/monster_ogre_t4",         "Monsters/monster_guardian_t3",
                "Monsters/monster_guardian_t4",     "Monsters/monster_guardian_t5",
                "Monsters/monster_dragon_t4",       "Monsters/monster_dragon_t5"
            },
            ["Resources"] = new[]
            {
                "Resources/node_farm_basic",        "Resources/node_lumber_basic",
                "Resources/node_stone_basic",       "Resources/node_iron_basic",
                "Resources/node_gold_basic",        "Resources/node_crystal_common",
                "Resources/node_crystal_rare",      "Resources/node_crystal_epic"
            },
            ["Terrain"] = new[]
            {
                "Terrain/biome_grasslands_base",    "Terrain/biome_grasslands_detail",
                "Terrain/biome_forest_base",        "Terrain/biome_forest_detail",
                "Terrain/biome_snow_base",          "Terrain/biome_snow_detail",
                "Terrain/biome_desert_base",        "Terrain/biome_highland_base",
                "Terrain/biome_swamp_base",         "Terrain/biome_volcanic_base",
                "Terrain/cliff_universal"
            },
            ["Environment"] = new[]
            {
                "Environment/tree_oak_01",          "Environment/tree_pine_01",
                "Environment/tree_dead_01",         "Environment/shrub_bush_01",
                "Environment/grass_patch_01",       "Environment/rock_large_01",
                "Environment/rock_cluster_01",      "Environment/ruin_pillar_01",
                "Environment/ruin_arch_01",         "Environment/road_segment_01",
                "Environment/river_section_01",     "Environment/lake_water_plane",
                "Environment/mountain_ridge_01",    "Environment/fog_volume_01",
                "Environment/cloud_layer_01"
            },
            ["Characters"] = new[]
            {
                "Characters/npc_villager",          "Characters/npc_farmer",
                "Characters/npc_soldier",           "Characters/npc_merchant",
                "Characters/npc_guard"
            },
            ["VFX"] = new[]
            {
                "VFX/selection_ring",               "VFX/construction_dust",
                "VFX/building_complete_celebration","VFX/resource_gather_sparkle",
                "VFX/monster_death_dissolve",       "VFX/march_arrival_burst",
                "VFX/level_up_celebration",         "VFX/loot_explosion",
                "VFX/torch_flame",                  "VFX/chimney_smoke",
                "VFX/campfire_embers",              "VFX/rain_splatter",
                "VFX/snow_particles",               "VFX/ashfall_particles",
                "VFX/lightning_strike"
            },
            ["Audio"] = new[]
            {
                "Audio/music_login",                "Audio/music_kingdom",
                "Audio/music_world",                "Audio/music_combat",
                "Audio/music_victory",              "Audio/music_defeat",
                "Audio/ambient_birds",              "Audio/ambient_wind",
                "Audio/ambient_market",             "Audio/ambient_smithy",
                "Audio/ambient_monsters",           "Audio/ambient_rain",
                "Audio/ambient_storm",              "Audio/sfx_building_complete",
                "Audio/sfx_march_depart",           "Audio/sfx_combat_hit"
            }
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Data
    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    public class ImportReport
    {
        public DateTime startTime;
        public DateTime endTime;
        public float duration;
        public float progressPercent;

        public List<string> loadedKeys    = new();
        public List<string> missingKeys   = new();
        public Dictionary<string, List<string>> missingByCategory = new();

        public List<string> lodIssues      = new();
        public List<string> animatorIssues = new();
        public List<string> materialIssues = new();

        public int placeholderCount;
        public List<string> autoAssignedKeys = new();
        public int autoAssignedCount;
    }
}
