#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace EternalKingdoms.Editor
{
    /// <summary>
    /// U5.8 — Addressables Populator
    /// Validates the Addressables catalog for every key defined in the
    /// FreeAssetDatabase, produces a gap report, and provides one-click
    /// actions to fill remaining gaps with procedural fallbacks.
    ///
    /// Run: Tools > Eternal Kingdoms > Phase 5.8 — Validate Addressables
    ///
    /// Also run after the main FreeAssetImporter completes to confirm
    /// 100% slot coverage with zero broken references.
    /// </summary>
    public class AddressablesPopulator : EditorWindow
    {
        // ── Window ────────────────────────────────────────────────────────────
        [MenuItem("Tools/Eternal Kingdoms/Phase 5.8 — Validate Addressables")]
        public static void ShowWindow()
        {
            GetWindow<AddressablesPopulator>("EK Addressables Populator");
        }

        // ── All expected Addressable keys (mirrors AssetManifest.json) ────────
        private static readonly string[] AllExpectedKeys =
        {
            // Buildings
            "Buildings/building_palace_001", "Buildings/building_farm_001",
            "Buildings/building_lumbermill_001", "Buildings/building_quarry_001",
            "Buildings/building_ironmine_001", "Buildings/building_goldmine_001",
            "Buildings/building_barracks_001", "Buildings/building_academy_001",
            "Buildings/building_hospital_001", "Buildings/building_market_001",
            "Buildings/building_watchtower_001", "Buildings/building_walls_001",
            "Buildings/building_embassy_001", "Buildings/building_forge_001",

            // Building Tiers
            "Buildings/building_palace_early", "Buildings/building_palace_developed",
            "Buildings/building_palace_advanced", "Buildings/building_barracks_early",
            "Buildings/building_barracks_developed", "Buildings/building_barracks_advanced",
            "Buildings/building_farm_early", "Buildings/building_farm_developed",
            "Buildings/building_farm_advanced",

            // Monsters
            "Monsters/monster_bandit_t1", "Monsters/monster_bandit_t2", "Monsters/monster_bandit_t3",
            "Monsters/monster_direwolf_t1", "Monsters/monster_direwolf_t2", "Monsters/monster_direwolf_t3",
            "Monsters/monster_ogre_t2", "Monsters/monster_ogre_t3", "Monsters/monster_ogre_t4",
            "Monsters/monster_guardian_t3", "Monsters/monster_guardian_t4", "Monsters/monster_guardian_t5",
            "Monsters/monster_dragon_t4", "Monsters/monster_dragon_t5",

            // Resources
            "Resources/node_farm_basic", "Resources/node_lumber_basic",
            "Resources/node_stone_basic", "Resources/node_iron_basic",
            "Resources/node_gold_basic", "Resources/node_crystal_common",
            "Resources/node_crystal_rare", "Resources/node_crystal_epic",

            // Terrain
            "Terrain/biome_grasslands_base", "Terrain/biome_grasslands_detail",
            "Terrain/biome_forest_base", "Terrain/biome_forest_detail",
            "Terrain/biome_snow_base", "Terrain/biome_snow_detail",
            "Terrain/biome_desert_base", "Terrain/biome_highland_base",
            "Terrain/biome_swamp_base", "Terrain/biome_volcanic_base",
            "Terrain/cliff_universal",

            // Environment
            "Environment/tree_oak_01", "Environment/tree_pine_01", "Environment/tree_dead_01",
            "Environment/shrub_bush_01", "Environment/grass_patch_01",
            "Environment/rock_large_01", "Environment/rock_cluster_01",
            "Environment/ruin_pillar_01", "Environment/ruin_arch_01",
            "Environment/road_segment_01", "Environment/river_section_01",
            "Environment/lake_water_plane", "Environment/mountain_ridge_01",
            "Environment/fog_volume_01", "Environment/cloud_layer_01",

            // Kingdom Props
            "Props/road_decal_cobble", "Props/market_stall_01", "Props/market_stall_02",
            "Props/crate_01", "Props/barrel_01", "Props/sack_01", "Props/wagon_01",
            "Props/fence_wood_01", "Props/fence_stone_01",
            "Props/garden_flower_01", "Props/garden_bed_01", "Props/tree_kingdom_oak",
            "Props/statue_knight", "Props/statue_king", "Props/fountain_stone",
            "Props/campfire_01", "Props/training_dummy_01",

            // World Landmarks
            "World/ancient_ruin_01", "World/ancient_ruin_02", "World/monument_obelisk",
            "World/watchtower_01", "World/shrine_01", "World/statue_ancient",
            "World/destroyed_camp_01", "World/road_decal_dirt", "World/biome_transition_blend",

            // Characters
            "Characters/npc_villager", "Characters/npc_farmer", "Characters/npc_soldier",
            "Characters/npc_merchant", "Characters/npc_guard",

            // VFX
            "VFX/selection_ring", "VFX/construction_dust", "VFX/building_complete_celebration",
            "VFX/resource_gather_sparkle", "VFX/monster_death_dissolve", "VFX/march_arrival_burst",
            "VFX/level_up_celebration", "VFX/loot_explosion", "VFX/torch_flame",
            "VFX/chimney_smoke", "VFX/campfire_embers", "VFX/rain_splatter",
            "VFX/snow_particles", "VFX/ashfall_particles", "VFX/lightning_strike",

            // Audio
            "Audio/music_login", "Audio/music_kingdom", "Audio/music_world",
            "Audio/music_combat", "Audio/music_victory", "Audio/music_defeat",
            "Audio/ambient_birds", "Audio/ambient_wind", "Audio/ambient_market",
            "Audio/ambient_smithy", "Audio/ambient_monsters", "Audio/ambient_rain",
            "Audio/ambient_storm", "Audio/sfx_building_complete",
            "Audio/sfx_march_depart", "Audio/sfx_combat_hit"
        };

        private static readonly HashSet<string> Priority1Keys = new(new[]
        {
            "Buildings/building_palace_001", "Buildings/building_barracks_001",
            "Monsters/monster_bandit_t1", "Monsters/monster_dragon_t5",
            "Resources/node_crystal_epic", "Characters/npc_soldier", "Characters/npc_villager",
            "VFX/building_complete_celebration", "VFX/monster_death_dissolve", "VFX/loot_explosion",
            "Audio/music_kingdom", "Audio/music_combat"
        });

        // ── Validation State ──────────────────────────────────────────────────
        private List<string> _registered   = new();
        private List<string> _missing      = new();
        private List<string> _priority1Missing = new();
        private Vector2      _scroll;
        private bool         _validated;

        // ── GUI ───────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUILayout.Label("Eternal Kingdoms — Addressables Populator (Phase 5.8)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("▶ Validate All Addressable Keys", GUILayout.Height(30)))
                ValidateAll();

            if (GUILayout.Button("⚡ Fill Gaps with Procedural Fallbacks", GUILayout.Height(25)))
                FillGapsWithFallbacks();

            if (GUILayout.Button("📝 Export Gap Report", GUILayout.Height(25)))
                ExportGapReport();

            if (!_validated) return;

            EditorGUILayout.Space();
            float coverage = (float)_registered.Count / AllExpectedKeys.Length * 100f;
            string status  = _priority1Missing.Count == 0 && coverage >= 80f ? "✅ ALPHA GO" : "⚠️ GAPS REMAIN";

            EditorGUILayout.HelpBox(
                $"{status}\n" +
                $"Registered:  {_registered.Count} / {AllExpectedKeys.Length}\n" +
                $"Coverage:    {coverage:F0}%\n" +
                $"Missing:     {_missing.Count}\n" +
                $"P1 Missing:  {_priority1Missing.Count}", MessageType.None);

            if (_missing.Count > 0)
            {
                EditorGUILayout.LabelField("Missing Keys:", EditorStyles.boldLabel);
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(300));
                foreach (var key in _missing)
                {
                    bool isP1 = Priority1Keys.Contains(key);
                    var  col  = isP1 ? Color.red : Color.yellow;
                    GUI.color = col;
                    EditorGUILayout.LabelField(isP1 ? $"❌ [P1] {key}" : $"⚠️ {key}");
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndScrollView();
            }
        }

        // ── Actions ───────────────────────────────────────────────────────────
        private void ValidateAll()
        {
            _registered.Clear();
            _missing.Clear();
            _priority1Missing.Clear();
            _validated = true;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[AddressablesPopulator] No Addressable settings found — run FreeAssetImporter first.");
                return;
            }

            // Build lookup of all registered addresses
            var registeredSet = new HashSet<string>();
            foreach (var group in settings.groups)
                foreach (var entry in group.entries)
                    registeredSet.Add(entry.address);

            foreach (var key in AllExpectedKeys)
            {
                if (registeredSet.Contains(key))
                    _registered.Add(key);
                else
                {
                    _missing.Add(key);
                    if (Priority1Keys.Contains(key)) _priority1Missing.Add(key);
                }
            }

            Debug.Log($"[AddressablesPopulator] Validation: {_registered.Count}/{AllExpectedKeys.Length} registered, " +
                      $"{_missing.Count} missing ({_priority1Missing.Count} Priority 1).");
        }

        private void FillGapsWithFallbacks()
        {
            ValidateAll();
            if (_missing.Count == 0) { Debug.Log("[AddressablesPopulator] ✅ No gaps to fill."); return; }

            var settings = GetOrCreateSettings();
            var group    = GetOrCreateGroup(settings, "EK_FreeAssets_Phase58_Fallbacks");

            int filled = 0;
            foreach (var key in _missing)
            {
                string category = key.Split('/')[0];
                var prefab = ProcGenFallbackFactory.CreateFallback(key, category);
                if (prefab == null) continue;

                string guid  = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
                var entry    = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                entry.address = key;
                entry.SetLabel("EK_Fallback",  true, true, false);
                entry.SetLabel("EK_Phase58",   true, true, false);
                filled++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AddressablesPopulator] ✅ Filled {filled} gaps with procedural fallbacks.");
            ValidateAll();
            Repaint();
        }

        private void ExportGapReport()
        {
            if (!_validated) ValidateAll();
            float coverage = (float)_registered.Count / AllExpectedKeys.Length * 100f;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Addressables Gap Report — Phase 5.8");
            sb.AppendLine($"> Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine($"> Coverage: {coverage:F0}% ({_registered.Count}/{AllExpectedKeys.Length})");
            sb.AppendLine($"> Priority 1 Missing: {_priority1Missing.Count}");
            sb.AppendLine();

            if (_priority1Missing.Count > 0)
            {
                sb.AppendLine("## Priority 1 Gaps (Blocking Demo)");
                foreach (var k in _priority1Missing) sb.AppendLine($"- ❌ {k}");
                sb.AppendLine();
            }

            if (_missing.Count > 0)
            {
                sb.AppendLine("## All Missing Keys");
                foreach (var k in _missing) sb.AppendLine($"- `{k}`");
            }

            string path = "Assets/Phase58_AddressablesGapReport.md";
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"[AddressablesPopulator] Gap report written → {path}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static AddressableAssetSettings GetOrCreateSettings()
        {
            var s = AddressableAssetSettingsDefaultObject.Settings;
            if (s != null) return s;
            s = AddressableAssetSettings.Create(
                AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
            AddressableAssetSettingsDefaultObject.Settings = s;
            return s;
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings s, string name)
        {
            var g = s.FindGroup(name);
            if (g != null) return g;
            return s.CreateGroup(name, false, false, false,
                null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
        }
    }
}
#endif
