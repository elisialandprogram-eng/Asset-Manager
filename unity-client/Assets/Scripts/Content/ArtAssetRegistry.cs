using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EternalKingdoms.Content
{
    /// <summary>
    /// ArtAssetRegistry — Central registry for all production art assets.
    ///
    /// Phase 5.5 (U5.5.1) responsibilities:
    ///   - Defines every required asset address across all categories
    ///   - Validates assets exist in Addressables catalog at startup
    ///   - Reports missing/broken/unassigned prefabs to console + debug overlay
    ///   - Provides typed lookup methods for each asset category
    ///   - Fallback asset support: if primary fails, serves placeholder and logs warning
    ///
    /// Asset categories:
    ///   Environment, Terrain, Buildings, Monsters, Resources,
    ///   Characters (NPCs), VFX, UI, Audio, Materials, Animations
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - ValidateAll() runs on Start; results exposed via ValidationReport
    ///   - All addresses match AssetCatalogManager key convention: "{Group}/{id}"
    ///   - Missing assets listed in ValidationReport.missing for debug overlay
    ///
    /// Usage:
    ///   var prefab = await ArtAssetRegistry.Instance.LoadBuilding("building_palace_001");
    ///   var missing = ArtAssetRegistry.Instance.ValidationReport.missing;
    /// </summary>
    public class ArtAssetRegistry : MonoBehaviour
    {
        public static ArtAssetRegistry Instance { get; private set; }

        [Header("Fallback Assets — assign grey-box standbys in Inspector")]
        [SerializeField] private GameObject fallbackBuildingPrefab;
        [SerializeField] private GameObject fallbackMonsterPrefab;
        [SerializeField] private GameObject fallbackResourceNodePrefab;
        [SerializeField] private GameObject fallbackNPCPrefab;
        [SerializeField] private Material   fallbackMaterial;

        public AssetValidationReport ValidationReport { get; private set; } = new();

        // ── Manifest: every required Addressable address ──────────────────────

        private static readonly string[] s_requiredBuildings = {
            "Buildings/building_palace_001",
            "Buildings/building_farm_001",
            "Buildings/building_lumbermill_001",
            "Buildings/building_quarry_001",
            "Buildings/building_ironmine_001",
            "Buildings/building_goldmine_001",
            "Buildings/building_barracks_001",
            "Buildings/building_academy_001",
            "Buildings/building_hospital_001",
            "Buildings/building_wall_001",
            "Buildings/building_storage_001",
            "Buildings/building_alliancehall_001",
            // Construction stages
            "Buildings/construction_foundation",
            "Buildings/construction_scaffolding",
        };

        private static readonly string[] s_requiredMonsters = {
            "Monsters/monster_bandit_t1",
            "Monsters/monster_bandit_t2",
            "Monsters/monster_bandit_t3",
            "Monsters/monster_bandit_t4",
            "Monsters/monster_bandit_t5",
            "Monsters/monster_direwolf_t1",
            "Monsters/monster_direwolf_t3",
            "Monsters/monster_direwolf_t5",
            "Monsters/monster_ogre_t1",
            "Monsters/monster_ogre_t3",
            "Monsters/monster_ogre_t5",
            "Monsters/monster_guardian_t4",
            "Monsters/monster_guardian_t5",
            "Monsters/monster_dragon_t5",
        };

        private static readonly string[] s_requiredResources = {
            "Resources/node_farm_001",
            "Resources/node_lumbercamp_001",
            "Resources/node_stonequarry_001",
            "Resources/node_ironmine_001",
            "Resources/node_golddeposit_001",
            "Resources/node_crystalcluster_common",
            "Resources/node_crystalcluster_rare",
            "Resources/node_crystalcluster_epic",
        };

        private static readonly string[] s_requiredTerrain = {
            "Terrain/biome_grasslands_ground",
            "Terrain/biome_grasslands_detail1",
            "Terrain/biome_forest_ground",
            "Terrain/biome_forest_detail1",
            "Terrain/biome_snow_ground",
            "Terrain/biome_desert_ground",
            "Terrain/biome_highlands_ground",
            "Terrain/biome_swamp_ground",
            "Terrain/biome_volcanic_ground",
            "Terrain/road_material",
            "Terrain/river_material",
        };

        private static readonly string[] s_requiredEnvironment = {
            "Environment/tree_oak_001",
            "Environment/tree_pine_001",
            "Environment/tree_birch_001",
            "Environment/tree_dead_001",
            "Environment/shrub_001",
            "Environment/grass_001",
            "Environment/rock_large_001",
            "Environment/rock_medium_001",
            "Environment/flower_001",
            "Environment/ruins_001",
            "Environment/campfire_001",
            "Environment/statue_ancient_001",
            "Environment/tree_snow_001",
            "Environment/cactus_001",
            "Environment/cypress_001",
        };

        private static readonly string[] s_requiredCharacters = {
            "Characters/npc_villager_male",
            "Characters/npc_villager_female",
            "Characters/npc_farmer",
            "Characters/npc_soldier",
            "Characters/npc_merchant",
        };

        private static readonly string[] s_requiredVFX = {
            "VFX/selection_ring",
            "VFX/click_burst",
            "VFX/resource_harvest",
            "VFX/march_arrival",
            "VFX/monster_defeat",
            "VFX/level_up",
            "VFX/reward_popup",
            "VFX/building_complete",
            "VFX/crystal_resonate",
            "VFX/torch_flame",
            "VFX/smoke_building",
            "VFX/bird_flock",
            "VFX/butterfly",
            "VFX/wind_leaves",
            "VFX/falling_leaves",
        };

        private static readonly string[] s_requiredAudio = {
            "Audio/music_kingdom_peaceful",
            "Audio/music_world_explore",
            "Audio/ambient_grasslands_loop",
            "Audio/ambient_forest_loop",
            "Audio/ambient_snow_loop",
            "Audio/ambient_desert_loop",
            "Audio/ambient_swamp_loop",
            "Audio/ambient_volcanic_loop",
            "Audio/ambient_kingdom_loop",
            "Audio/sfx_panel_open",
            "Audio/sfx_panel_close",
            "Audio/sfx_button_click",
            "Audio/sfx_march_sent",
            "Audio/sfx_building_complete",
            "Audio/sfx_monster_defeat",
            "Audio/sfx_level_up",
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(ValidateAll());
        }

        // ── Validation ────────────────────────────────────────────────────────

        public IEnumerator ValidateAll()
        {
            ValidationReport = new AssetValidationReport();
            ValidationReport.startTime = DateTime.UtcNow;

            yield return ValidateCategory("Buildings",   s_requiredBuildings);
            yield return ValidateCategory("Monsters",    s_requiredMonsters);
            yield return ValidateCategory("Resources",   s_requiredResources);
            yield return ValidateCategory("Terrain",     s_requiredTerrain);
            yield return ValidateCategory("Environment", s_requiredEnvironment);
            yield return ValidateCategory("Characters",  s_requiredCharacters);
            yield return ValidateCategory("VFX",         s_requiredVFX);
            yield return ValidateCategory("Audio",       s_requiredAudio);

            ValidationReport.endTime    = DateTime.UtcNow;
            ValidationReport.validated  = true;

            LogReport();
        }

        private IEnumerator ValidateCategory(string category, string[] addresses)
        {
            foreach (var addr in addresses)
            {
                var handle = Addressables.LoadResourceLocationsAsync(addr);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result.Count > 0)
                {
                    ValidationReport.found.Add(addr);
                }
                else
                {
                    ValidationReport.missing.Add(addr);
                    ValidationReport.missingByCategory.TryGetValue(category, out var list);
                    if (list == null) { list = new List<string>(); ValidationReport.missingByCategory[category] = list; }
                    list.Add(addr);
                }

                Addressables.Release(handle);
            }
        }

        private void LogReport()
        {
            int total   = ValidationReport.found.Count + ValidationReport.missing.Count;
            int missing = ValidationReport.missing.Count;
            int pct     = total > 0 ? (ValidationReport.found.Count * 100 / total) : 0;

            if (missing == 0)
            {
                Debug.Log($"[ArtAssetRegistry] ✅ All {total} assets validated.");
            }
            else
            {
                Debug.LogWarning($"[ArtAssetRegistry] ⚠️ {missing}/{total} assets MISSING ({pct}% coverage).");
                foreach (var kvp in ValidationReport.missingByCategory)
                {
                    Debug.LogWarning($"  [{kvp.Key}] {kvp.Value.Count} missing:");
                    foreach (var addr in kvp.Value)
                        Debug.LogWarning($"    ✗ {addr}");
                }
            }
        }

        // ── Typed Loaders ─────────────────────────────────────────────────────

        public IEnumerator LoadBuilding(string registryId, Action<GameObject> onLoaded)
        {
            string addr = $"Buildings/{registryId}";
            yield return LoadWithFallback<GameObject>(addr, fallbackBuildingPrefab, onLoaded);
        }

        public IEnumerator LoadMonster(string registryId, Action<GameObject> onLoaded)
        {
            string addr = $"Monsters/{registryId}";
            yield return LoadWithFallback<GameObject>(addr, fallbackMonsterPrefab, onLoaded);
        }

        public IEnumerator LoadResourceNode(string registryId, Action<GameObject> onLoaded)
        {
            string addr = $"Resources/{registryId}";
            yield return LoadWithFallback<GameObject>(addr, fallbackResourceNodePrefab, onLoaded);
        }

        public IEnumerator LoadNPC(string registryId, Action<GameObject> onLoaded)
        {
            string addr = $"Characters/{registryId}";
            yield return LoadWithFallback<GameObject>(addr, fallbackNPCPrefab, onLoaded);
        }

        // ── Fallback Load ─────────────────────────────────────────────────────

        private IEnumerator LoadWithFallback<T>(string address, T fallback,
                                                  Action<T> onLoaded) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onLoaded?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogWarning($"[ArtAssetRegistry] Missing asset '{address}' — using fallback.");
                onLoaded?.Invoke(fallback);
            }
        }
    }

    // ── Validation Report ──────────────────────────────────────────────────────

    public class AssetValidationReport
    {
        public bool validated;
        public DateTime startTime;
        public DateTime endTime;
        public List<string> found   = new();
        public List<string> missing = new();
        public Dictionary<string, List<string>> missingByCategory = new();

        public int TotalCount    => found.Count + missing.Count;
        public int MissingCount  => missing.Count;
        public float CoveragePercent => TotalCount > 0 ? (float)found.Count / TotalCount * 100f : 0f;
        public bool IsComplete   => validated && missing.Count == 0;
        public TimeSpan Duration => endTime - startTime;
    }
}
