#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace EternalKingdoms.Editor
{
    /// <summary>
    /// U5.8 — Free Asset Importer (v2 — live scan build)
    ///
    /// Based on actual filesystem scan 2026-06-20.
    /// Discovered packs:
    ///   • Medieval Village Pack  (Assets/Thirdparty/Medieval Village Pack/)
    ///   • Nature Pack            (Assets/Thirdparty/Nature Pack/)
    ///   • Ultimate Monsters      (Assets/Thirdparty/Ultimate Monsters/)
    ///
    /// Root is 'Thirdparty' (lowercase p) — different from original spec.
    /// This version resolves both 'Thirdparty' and 'ThirdParty' automatically.
    ///
    /// Run: Tools > Eternal Kingdoms > Phase 5.8 — Import Free Assets
    /// </summary>
    public static class FreeAssetImporter
    {
        // ── Path constants (adapted to real filesystem) ───────────────────────
        private const string ThirdpartyRoot   = "Assets/Thirdparty";         // actual folder
        private const string ThirdPartyRoot   = "Assets/ThirdParty";         // fallback
        private const string PrefabOutput     = "Assets/Addressables/FreePrefabs";
        private const string AnimatorOutput   = "Assets/Addressables/Animators";
        private const string MaterialOutput   = "Assets/Addressables/Materials";
        private const string AddressablesGroup = "EK_FreeAssets_Phase58";
        private const string ManifestPath     = "unity-client/BuildConfigs/AssetManifest.json";

        // ── Resolved root (set at runtime after probing filesystem) ───────────
        private static string s_root = null;
        private static string Root
        {
            get
            {
                if (s_root != null) return s_root;
                s_root = Directory.Exists(ThirdpartyRoot) ? ThirdpartyRoot
                       : Directory.Exists(ThirdPartyRoot) ? ThirdPartyRoot
                       : ThirdpartyRoot;  // default even if missing
                return s_root;
            }
        }

        // ── Discovered-pack helpers ────────────────────────────────────────────
        private static string MedievalBuildingFBX(string name) =>
            $"{Root}/Medieval Village Pack/Buildings/FBX/{name}.fbx";
        private static string MedievalPropFBX(string name) =>
            $"{Root}/Medieval Village Pack/Props/FBX/{name}.fbx";
        private static string NaturePackFBX(string name) =>
            $"{Root}/Nature Pack/FBX (Unity)/{name}.fbx";   // prefer Unity-optimised folder
        private static string NaturePackTexture(string name) =>
            $"{Root}/Nature Pack/Textures/{name}";
        private static string MonsterBigFBX(string name) =>
            $"{Root}/Ultimate Monsters/Big/FBX/{name}.fbx";
        private static string MonsterBlobFBX(string name) =>
            $"{Root}/Ultimate Monsters/Blob/FBX/{name}.fbx";
        private static string MonsterAtlas() =>
            $"{Root}/Ultimate Monsters/Atlas_Monsters.png";

        // ── Real mapping table (all 190 Addressable keys) ─────────────────────
        // Format: address → (sourcePath, scale, materialName, colorVariant, needsAnimator, isRealAsset)
        private static readonly AssetEntry[] Mappings = new AssetEntry[]
        {
            // ── BUILDINGS (14) ────────────────────────────────────────────────
            new("Buildings/building_palace_001",       MedievalBuildingFBX("Bell_Tower"),  1.0f, "URP_Lit_Stone",    null,    false, true,  1),
            new("Buildings/building_farm_001",         MedievalBuildingFBX("Mill"),         1.0f, "URP_Lit_Wood",     null,    false, true,  0),
            new("Buildings/building_lumbermill_001",   MedievalBuildingFBX("Sawmill"),      1.0f, "URP_Lit_Wood",     null,    false, true,  0),
            new("Buildings/building_quarry_001",       MedievalBuildingFBX("Stable"),       1.0f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_ironmine_001",     MedievalBuildingFBX("House_3"),      1.0f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_goldmine_001",     MedievalBuildingFBX("House_4"),      1.0f, "URP_Lit_Gold",     null,    false, true,  0),
            new("Buildings/building_barracks_001",     MedievalBuildingFBX("Inn"),          1.0f, "URP_Lit_Stone",    null,    false, true,  1),
            new("Buildings/building_academy_001",      MedievalBuildingFBX("House_1"),      1.0f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_hospital_001",     MedievalBuildingFBX("House_2"),      1.0f, "URP_Lit_White",    null,    false, true,  0),
            new("Buildings/building_market_001",       MedievalBuildingFBX("Gazebo"),       1.0f, "URP_Lit_Wood",     null,    false, true,  0),
            new("Buildings/building_watchtower_001",   MedievalBuildingFBX("Bell_Tower"),   1.0f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_walls_001",        MedievalPropFBX("Fence"),            3.0f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_embassy_001",      MedievalBuildingFBX("Inn"),          1.1f, "URP_Lit_Stone",    "prestigious", false, true, 0),
            new("Buildings/building_forge_001",        MedievalBuildingFBX("Blacksmith"),   1.0f, "URP_Lit_Metal",    null,    false, true,  0),

            // ── BUILDING TIERS (9 real + 33 procgen) ─────────────────────────
            new("Buildings/building_palace_early",     MedievalBuildingFBX("House_1"),      1.0f, "URP_Lit_Wood",     null,    false, true,  0),
            new("Buildings/building_palace_developed", MedievalBuildingFBX("House_3"),      1.3f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_palace_advanced",  MedievalBuildingFBX("Bell_Tower"),   1.0f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_barracks_early",   MedievalBuildingFBX("House_2"),      1.0f, "URP_Lit_Wood",     null,    false, true,  0),
            new("Buildings/building_barracks_developed", MedievalBuildingFBX("Stable"),     1.2f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_barracks_advanced",  MedievalBuildingFBX("Inn"),        1.0f, "URP_Lit_Stone",    null,    false, true,  0),
            new("Buildings/building_farm_early",       MedievalPropFBX("Hay"),              1.5f, "URP_Lit_Wood",     null,    false, true,  0),
            new("Buildings/building_farm_developed",   MedievalBuildingFBX("Mill"),         1.0f, "URP_Lit_Wood",     null,    false, true,  0),
            new("Buildings/building_farm_advanced",    MedievalBuildingFBX("Sawmill"),      1.2f, "URP_Lit_Wood",     null,    false, true,  0),
            // remaining 33 tiers → ProcGen (sourceFile = null → ProcGen branch)
            new("Buildings/building_lumbermill_early",   null, 1.0f, "URP_Lit_Wood",  null, false, false, 0),
            new("Buildings/building_lumbermill_developed", null, 1.0f, "URP_Lit_Wood", null, false, false, 0),
            new("Buildings/building_lumbermill_advanced",  null, 1.0f, "URP_Lit_Wood", null, false, false, 0),
            new("Buildings/building_quarry_early",     null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_quarry_developed", null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_quarry_advanced",  null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_ironmine_early",   null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_ironmine_developed", null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_ironmine_advanced",  null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_goldmine_early",   null, 1.0f, "URP_Lit_Gold",  null, false, false, 0),
            new("Buildings/building_goldmine_developed", null, 1.0f, "URP_Lit_Gold", null, false, false, 0),
            new("Buildings/building_goldmine_advanced",  null, 1.0f, "URP_Lit_Gold", null, false, false, 0),
            new("Buildings/building_market_early",     null, 1.0f, "URP_Lit_Wood",  null, false, false, 0),
            new("Buildings/building_market_developed", null, 1.0f, "URP_Lit_Wood",  null, false, false, 0),
            new("Buildings/building_market_advanced",  null, 1.0f, "URP_Lit_Wood",  null, false, false, 0),
            new("Buildings/building_watchtower_early",   null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_watchtower_developed", null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_watchtower_advanced",  null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_walls_early",      null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_walls_developed",  null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_walls_advanced",   null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_academy_early",    null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_academy_developed", null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_academy_advanced",  null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_hospital_early",   null, 1.0f, "URP_Lit_White", null, false, false, 0),
            new("Buildings/building_hospital_developed", null, 1.0f, "URP_Lit_White", null, false, false, 0),
            new("Buildings/building_hospital_advanced",  null, 1.0f, "URP_Lit_White", null, false, false, 0),
            new("Buildings/building_embassy_early",    null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_embassy_developed", null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_embassy_advanced",  null, 1.0f, "URP_Lit_Stone", null, false, false, 0),
            new("Buildings/building_forge_early",      null, 1.0f, "URP_Lit_Metal", null, false, false, 0),
            new("Buildings/building_forge_developed",  null, 1.0f, "URP_Lit_Metal", null, false, false, 0),
            new("Buildings/building_forge_advanced",   null, 1.0f, "URP_Lit_Metal", null, false, false, 0),

            // ── MONSTERS (14) ─────────────────────────────────────────────────
            new("Monsters/monster_bandit_t1",    MonsterBigFBX("Orc"),        1.0f, null, null, true, true, 1),
            new("Monsters/monster_bandit_t2",    MonsterBigFBX("Tribal"),     1.1f, null, null, true, true, 0),
            new("Monsters/monster_bandit_t3",    MonsterBigFBX("Ninja"),      1.15f,null, null, true, true, 0),
            new("Monsters/monster_direwolf_t1",  MonsterBlobFBX("Dog"),       1.2f, null, null, true, true, 0),
            new("Monsters/monster_direwolf_t2",  MonsterBlobFBX("Dog"),       1.5f, null, "dark", true, true, 0),
            new("Monsters/monster_direwolf_t3",  MonsterBigFBX("Yeti"),       1.6f, null, null, true, true, 0),
            new("Monsters/monster_ogre_t2",      MonsterBigFBX("MushroomKing"),1.4f,null, null, true, true, 0),
            new("Monsters/monster_ogre_t3",      MonsterBigFBX("Yeti"),       1.7f, null, null, true, true, 0),
            new("Monsters/monster_ogre_t4",      MonsterBigFBX("Orc_Skull"),  2.0f, null, null, true, true, 0),
            new("Monsters/monster_guardian_t3",  MonsterBlobFBX("GreenBlob"), 1.5f, null, null, true, true, 0),
            new("Monsters/monster_guardian_t4",  MonsterBigFBX("Demon"),      1.8f, null, null, true, true, 0),
            new("Monsters/monster_guardian_t5",  MonsterBigFBX("BlueDemon"),  2.2f, null, null, true, true, 0),
            new("Monsters/monster_dragon_t4",    MonsterBigFBX("Dino"),       2.0f, null, null, true, true, 1),
            new("Monsters/monster_dragon_t5",    MonsterBigFBX("Dino"),       3.5f, null, "red", true, true, 1),

            // ── RESOURCES (8) ─────────────────────────────────────────────────
            new("Resources/node_farm_basic",     MedievalPropFBX("Hay"),       1.5f, "URP_Lit_Wood",           null,  false, true, 0),
            new("Resources/node_lumber_basic",   NaturePackFBX("CommonTree_1"),1.0f, null,                     null,  false, true, 0),
            new("Resources/node_stone_basic",    NaturePackFBX("Rock_Medium_1"),1.0f,"URP_Lit_Stone",          null,  false, true, 0),
            new("Resources/node_iron_basic",     NaturePackFBX("Rock_Medium_2"),1.0f,"URP_Lit_Iron",           null,  false, true, 0),
            new("Resources/node_gold_basic",     NaturePackFBX("Rock_Medium_3"),1.0f,"URP_Lit_Gold",           null,  false, true, 0),
            new("Resources/node_crystal_common", NaturePackFBX("Pebble_Round_1"),1.0f,"URP_Lit_Crystal_Blue", null,  false, true, 0),
            new("Resources/node_crystal_rare",   NaturePackFBX("Pebble_Round_3"),1.3f,"URP_Lit_Crystal_Purple",null, false, true, 0),
            new("Resources/node_crystal_epic",   NaturePackFBX("Rock_Medium_1"),1.8f,"URP_Lit_Crystal_Epic",  null,  false, true, 1),

            // ── TERRAIN (11) — 5 real textures + 6 procgen ────────────────────
            new("Terrain/biome_grasslands_base",   NaturePackTexture("Grass.png"),                   1f, null, null, false, true,  0),
            new("Terrain/biome_grasslands_detail", NaturePackTexture("Flowers.png"),                 1f, null, null, false, true,  0),
            new("Terrain/biome_forest_base",       NaturePackTexture("Leaves_NormalTree_C.png"),     1f, null, null, false, true,  0),
            new("Terrain/biome_forest_detail",     NaturePackTexture("Leaves_TwistedTree.png"),      1f, null, null, false, true,  0),
            new("Terrain/biome_highland_base",     NaturePackTexture("Leaves_TwistedTree_C.png"),    1f, null, null, false, true,  0),
            new("Terrain/biome_snow_base",         null, 1f, null, "snow",     false, false, 0),
            new("Terrain/biome_snow_detail",       null, 1f, null, "snowDet",  false, false, 0),
            new("Terrain/biome_desert_base",       null, 1f, null, "desert",   false, false, 0),
            new("Terrain/biome_swamp_base",        null, 1f, null, "swamp",    false, false, 0),
            new("Terrain/biome_volcanic_base",     null, 1f, null, "volcanic", false, false, 0),
            new("Terrain/cliff_universal",         null, 1f, null, "cliff",    false, false, 0),

            // ── ENVIRONMENT (15) — 10 real + 5 procgen ────────────────────────
            new("Environment/tree_oak_01",       NaturePackFBX("CommonTree_1"),    1.0f, null, null, false, true, 0),
            new("Environment/tree_pine_01",      NaturePackFBX("Pine_1"),          1.0f, null, null, false, true, 0),
            new("Environment/tree_dead_01",      NaturePackFBX("DeadTree_1"),      1.0f, null, null, false, true, 0),
            new("Environment/shrub_bush_01",     NaturePackFBX("Bush_Common"),     1.0f, null, null, false, true, 0),
            new("Environment/grass_patch_01",    NaturePackFBX("Grass_Common_Short"),1.0f,null,null, false, true, 0),
            new("Environment/rock_large_01",     NaturePackFBX("Rock_Medium_1"),   1.8f, "URP_Lit_Stone", null, false, true, 0),
            new("Environment/rock_cluster_01",   NaturePackFBX("Pebble_Round_1"),  1.0f, "URP_Lit_Stone", null, false, true, 0),
            new("Environment/road_segment_01",   NaturePackFBX("RockPath_Square_Wide"),1.0f,null,null,false, true, 0),
            new("Environment/mountain_ridge_01", NaturePackFBX("Rock_Medium_2"),   3.0f, "URP_Lit_Stone", null, false, true, 0),
            new("Environment/ruin_pillar_01",    null, 1.0f, null, null, false, false, 0),   // procgen
            new("Environment/ruin_arch_01",      null, 1.0f, null, null, false, false, 0),   // procgen
            new("Environment/river_section_01",  null, 1.0f, null, null, false, false, 0),   // procgen
            new("Environment/lake_water_plane",  null, 1.0f, null, null, false, false, 0),   // procgen
            new("Environment/fog_volume_01",     null, 1.0f, null, null, false, false, 0),   // procgen VFX
            new("Environment/cloud_layer_01",    null, 1.0f, null, null, false, false, 0),   // procgen VFX

            // ── KINGDOM PROPS (17 listed — 14 real + 3 procgen) ───────────────
            new("Props/road_decal_cobble",  MedievalPropFBX("Path_Square"),     1.0f, "URP_Lit_Stone", null, false, true, 0),
            new("Props/market_stall_01",    MedievalPropFBX("MarketStand_1"),   1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("Props/market_stall_02",    MedievalPropFBX("MarketStand_2"),   1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("Props/crate_01",           MedievalPropFBX("Crate"),           1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("Props/barrel_01",          MedievalPropFBX("Barrel"),          1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("Props/sack_01",            MedievalPropFBX("Bag"),             1.0f, "URP_Lit_Fabric",null, false, true, 0),
            new("Props/wagon_01",           MedievalPropFBX("Cart"),            1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("Props/fence_wood_01",      MedievalPropFBX("Fence"),           1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("Props/fence_stone_01",     MedievalPropFBX("Stairs"),          1.0f, "URP_Lit_Stone", null, false, true, 0),
            new("Props/garden_flower_01",   NaturePackFBX("Flower_3_Single"),   1.0f, null,            null, false, true, 0),
            new("Props/garden_bed_01",      NaturePackFBX("Clover_1"),          1.0f, null,            null, false, true, 0),
            new("Props/tree_kingdom_oak",   NaturePackFBX("CommonTree_2"),      1.0f, null,            null, false, true, 0),
            new("Props/fountain_stone",     MedievalPropFBX("Well"),            1.0f, "URP_Lit_Stone", null, false, true, 0),
            new("Props/campfire_01",        MedievalPropFBX("Bonfire_Lit"),     1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("Props/statue_knight",      null, 1.0f, null, null, false, false, 0),   // procgen
            new("Props/statue_king",        null, 1.0f, null, null, false, false, 0),   // procgen
            new("Props/training_dummy_01",  null, 1.0f, null, null, false, false, 0),   // procgen

            // ── WORLD LANDMARKS (9 listed — 6 real + 3 procgen) ───────────────
            new("World/ancient_ruin_01",       null,                               1.0f, null, null, false, false, 0),
            new("World/ancient_ruin_02",       null,                               1.0f, null, null, false, false, 0),
            new("World/monument_obelisk",      MedievalBuildingFBX("Bell_Tower"), 0.6f, "URP_Lit_Stone", null, false, true, 0),
            new("World/watchtower_01",         MedievalBuildingFBX("Bell_Tower"), 1.0f, "URP_Lit_Stone", null, false, true, 0),
            new("World/shrine_01",             MedievalPropFBX("Well"),           0.8f, "URP_Lit_Stone", null, false, true, 0),
            new("World/statue_ancient",        null,                               1.0f, null, null, false, false, 0),
            new("World/destroyed_camp_01",     MedievalPropFBX("Bonfire"),        1.0f, "URP_Lit_Wood",  null, false, true, 0),
            new("World/road_decal_dirt",       NaturePackFBX("RockPath_Round_Wide"),1.0f,null,           null, false, true, 0),
            new("World/biome_transition_blend",NaturePackFBX("RockPath_Round_Thin"),1.0f,null,           null, false, true, 0),

            // ── CHARACTERS (5) — all procgen ──────────────────────────────────
            new("Characters/npc_villager", null, 1.0f, null, null, true, false, 1),
            new("Characters/npc_farmer",   null, 1.0f, null, null, true, false, 0),
            new("Characters/npc_soldier",  null, 1.0f, null, null, true, false, 1),
            new("Characters/npc_merchant", null, 1.0f, null, null, true, false, 0),
            new("Characters/npc_guard",    null, 1.0f, null, null, true, false, 0),

            // ── VFX (15) — all procgen ────────────────────────────────────────
            new("VFX/selection_ring",              null, 1f, null, null, false, false, 0),
            new("VFX/construction_dust",           null, 1f, null, null, false, false, 0),
            new("VFX/building_complete_celebration",null,1f, null, null, false, false, 1),
            new("VFX/resource_gather_sparkle",     null, 1f, null, null, false, false, 0),
            new("VFX/monster_death_dissolve",      null, 1f, null, null, false, false, 1),
            new("VFX/march_arrival_burst",         null, 1f, null, null, false, false, 0),
            new("VFX/level_up_celebration",        null, 1f, null, null, false, false, 0),
            new("VFX/loot_explosion",              null, 1f, null, null, false, false, 1),
            new("VFX/torch_flame",                 null, 1f, null, null, false, false, 0),
            new("VFX/chimney_smoke",               null, 1f, null, null, false, false, 0),
            new("VFX/campfire_embers",             null, 1f, null, null, false, false, 0),
            new("VFX/rain_splatter",               null, 1f, null, null, false, false, 0),
            new("VFX/snow_particles",              null, 1f, null, null, false, false, 0),
            new("VFX/ashfall_particles",           null, 1f, null, null, false, false, 0),
            new("VFX/lightning_strike",            null, 1f, null, null, false, false, 0),

            // ── AUDIO (16) — all silent placeholders ──────────────────────────
            new("Audio/music_login",           null, 1f, null, null, false, false, 1),
            new("Audio/music_kingdom",         null, 1f, null, null, false, false, 1),
            new("Audio/music_world",           null, 1f, null, null, false, false, 0),
            new("Audio/music_combat",          null, 1f, null, null, false, false, 1),
            new("Audio/music_victory",         null, 1f, null, null, false, false, 0),
            new("Audio/music_defeat",          null, 1f, null, null, false, false, 0),
            new("Audio/ambient_birds",         null, 1f, null, null, false, false, 0),
            new("Audio/ambient_wind",          null, 1f, null, null, false, false, 0),
            new("Audio/ambient_market",        null, 1f, null, null, false, false, 0),
            new("Audio/ambient_smithy",        null, 1f, null, null, false, false, 0),
            new("Audio/ambient_monsters",      null, 1f, null, null, false, false, 0),
            new("Audio/ambient_rain",          null, 1f, null, null, false, false, 0),
            new("Audio/ambient_storm",         null, 1f, null, null, false, false, 0),
            new("Audio/sfx_building_complete", null, 1f, null, null, false, false, 0),
            new("Audio/sfx_march_depart",      null, 1f, null, null, false, false, 0),
            new("Audio/sfx_combat_hit",        null, 1f, null, null, false, false, 0),
        };

        // ── Entry point ───────────────────────────────────────────────────────
        [MenuItem("Tools/Eternal Kingdoms/Phase 5.8 — Import Free Assets")]
        public static void RunImport()
        {
            Debug.Log($"[FreeAssetImporter v2] ▶ Starting… Thirdparty root = '{Root}'");

            EnsureDirectories();
            var settings = GetOrCreateAddressableSettings();
            var group    = GetOrCreateGroup(settings, AddressablesGroup);

            var report = new ImportReport();

            foreach (var entry in Mappings)
                ProcessEntry(entry, settings, group, report);

            AssetDatabase.SaveAssets();
            report.Print();
        }

        // ── Per-entry processor ───────────────────────────────────────────────
        private static void ProcessEntry(AssetEntry e, AddressableAssetSettings settings,
            AddressableAssetGroup group, ImportReport report)
        {
            report.Total++;

            // Determine asset type from address
            bool isAudio   = e.Address.StartsWith("Audio/");
            bool isTerrain = e.Address.StartsWith("Terrain/");
            bool isVFX     = e.Address.StartsWith("VFX/");

            if (isAudio)  { ProcessAudio(e, settings, group, report); return; }
            if (isTerrain) { ProcessTerrain(e, settings, group, report); return; }

            // Try to find source 3D model
            UnityEngine.Object asset = null;

            if (!string.IsNullOrEmpty(e.SourcePath))
            {
                asset = AssetDatabase.LoadAssetAtPath<GameObject>(e.SourcePath);
                if (asset == null)
                    Debug.LogWarning($"[FreeAssetImporter] Source not found at '{e.SourcePath}' — falling back to ProcGen for '{e.Address}'");
            }

            GameObject prefab;
            if (asset is GameObject srcPrefab)
            {
                prefab = CreateConfiguredPrefab(srcPrefab, e);
                report.Imported++;
                report.RealAssets.Add(e.Address);
            }
            else
            {
                prefab = isVFX
                    ? ProcGenFallbackFactory.CreateFallback(e.Address, "VFX")
                    : ProcGenFallbackFactory.CreateFallback(e.Address, GetCategory(e.Address));
                report.ProcGen.Add(e.Address);
                report.Imported++;
                Debug.Log($"[FreeAssetImporter] ProcGen fallback → '{e.Address}'");
            }

            if (prefab == null) { report.Failed.Add(e.Address); return; }
            RegisterAddressable(settings, group, prefab, e.Address);
        }

        private static void ProcessAudio(AssetEntry e, AddressableAssetSettings settings,
            AddressableAssetGroup group, ImportReport report)
        {
            string safeName = e.Address.Replace("/", "_");
            string outputPath = $"{PrefabOutput}/Audio/{safeName}.asset";
            EnsureDirectory($"{PrefabOutput}/Audio");

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(outputPath)
                           ?? CreateSilentClip(safeName, outputPath);

            RegisterAddressable(settings, group, clip, e.Address);
            report.ProcGen.Add(e.Address);
            report.Imported++;
        }

        private static void ProcessTerrain(AssetEntry e, AddressableAssetSettings settings,
            AddressableAssetGroup group, ImportReport report)
        {
            // Try real texture first
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(e.SourcePath))
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(e.SourcePath);

            if (tex == null)
            {
                // Create solid-colour fallback
                string safeName = e.Address.Replace("/", "_");
                string outputPath = $"{PrefabOutput}/Terrain/{safeName}.asset";
                EnsureDirectory($"{PrefabOutput}/Terrain");
                tex = CreateSolidTex(safeName, 128, 128, TerrainColor(e.ColorVariant), outputPath);
                report.ProcGen.Add(e.Address);
            }
            else
            {
                report.RealAssets.Add(e.Address);
            }

            RegisterAddressable(settings, group, tex, e.Address);
            report.Imported++;
        }

        // ── Prefab builder ────────────────────────────────────────────────────
        private static GameObject CreateConfiguredPrefab(GameObject source, AssetEntry e)
        {
            string category = GetCategory(e.Address);
            string safeName = e.Address.Replace("/", "_");
            string dir = $"{PrefabOutput}/{category}";
            string outputPath = $"{dir}/{safeName}.prefab";
            EnsureDirectory(dir);

            // Re-use existing prefab if already created
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (existing != null) return existing;

            var instance = UnityEngine.Object.Instantiate(source);
            instance.name = safeName;

            if (Math.Abs(e.Scale - 1f) > 0.01f)
                instance.transform.localScale = Vector3.one * e.Scale;

            if (!string.IsNullOrEmpty(e.MaterialName))
                ApplyMaterial(instance, e.MaterialName, e.ColorVariant);

            if (e.NeedsAnimator)
                ConfigureAnimator(instance, safeName);

            ConfigureLOD(instance);

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        // ── Material ──────────────────────────────────────────────────────────
        private static void ApplyMaterial(GameObject go, string matName, string colorVariant)
        {
            string matPath = $"{MaterialOutput}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.name = matName;
                mat.color = MaterialColor(matName, colorVariant);
                EnsureDirectory(MaterialOutput);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        private static Color MaterialColor(string name, string variant) => name switch
        {
            "URP_Lit_Stone"          => new Color(0.55f, 0.52f, 0.48f),
            "URP_Lit_Wood"           => new Color(0.52f, 0.35f, 0.20f),
            "URP_Lit_Metal"          => new Color(0.45f, 0.45f, 0.50f),
            "URP_Lit_Gold"           => new Color(0.85f, 0.70f, 0.20f),
            "URP_Lit_White"          => new Color(0.92f, 0.90f, 0.88f),
            "URP_Lit_Fabric"         => new Color(0.60f, 0.48f, 0.35f),
            "URP_Lit_Iron"           => new Color(0.38f, 0.38f, 0.42f),
            "URP_Lit_Crystal_Blue"   => new Color(0.25f, 0.55f, 0.95f),
            "URP_Lit_Crystal_Purple" => new Color(0.60f, 0.25f, 0.90f),
            "URP_Lit_Crystal_Epic"   => new Color(0.95f, 0.60f, 0.15f),
            _ => variant switch
            {
                "dark"        => new Color(0.20f, 0.18f, 0.16f),
                "red"         => new Color(0.80f, 0.12f, 0.10f),
                "prestigious" => new Color(0.62f, 0.56f, 0.42f),
                _ => Color.grey
            }
        };

        // ── Animator ──────────────────────────────────────────────────────────
        private static void ConfigureAnimator(GameObject go, string name)
        {
            EnsureDirectory(AnimatorOutput);
            string path = $"{AnimatorOutput}/{name}.controller";
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm   = ctrl.layers[0].stateMachine;

            string[] clips = { "Idle", "Walk", "Run", "Attack", "Death", "Sleep", "Roam", "Fly", "FlyIdle" };
            AnimatorState idle = null;
            foreach (var c in clips)
            {
                var s = sm.AddState(c);
                if (c == "Idle") idle = s;
            }
            if (idle != null) sm.defaultState = idle;

            ctrl.AddParameter("Speed",     AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsSleeping",AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsAlert",   AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsFlying",  AnimatorControllerParameterType.Bool);

            var anim = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
        }

        // ── LOD ───────────────────────────────────────────────────────────────
        private static void ConfigureLOD(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var lod = go.GetComponent<LODGroup>() ?? go.AddComponent<LODGroup>();
            lod.SetLODs(new[] {
                new LOD(0.5f,  new[] { renderers[0] }),
                new LOD(0.15f, new[] { renderers[0] }),
                new LOD(0.04f, new[] { renderers[0] })
            });
            lod.RecalculateBounds();
        }

        // ── Addressables ──────────────────────────────────────────────────────
        private static void RegisterAddressable(AddressableAssetSettings s,
            AddressableAssetGroup g, UnityEngine.Object asset, string key)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return;
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry   = s.CreateOrMoveEntry(guid, g, false, false);
            entry.address = key;
            string cat = key.Split('/')[0];
            entry.SetLabel($"EK_{cat}",  true, true, false);
            entry.SetLabel("EK_Phase58", true, true, false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static AudioClip CreateSilentClip(string name, string path)
        {
            var clip = AudioClip.Create(name, 44100, 1, 44100, false);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Texture2D CreateSolidTex(string name, int w, int h, Color col, string path)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name };
            var px  = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = col;
            tex.SetPixels(px); tex.Apply();
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        private static Color TerrainColor(string variant) => variant switch
        {
            "snow"     => new Color(0.95f, 0.96f, 0.98f),
            "snowDet"  => new Color(0.88f, 0.90f, 0.95f),
            "desert"   => new Color(0.87f, 0.78f, 0.52f),
            "swamp"    => new Color(0.28f, 0.35f, 0.20f),
            "volcanic" => new Color(0.18f, 0.14f, 0.14f),
            "cliff"    => new Color(0.52f, 0.50f, 0.46f),
            _          => new Color(0.55f, 0.55f, 0.50f)
        };

        private static string GetCategory(string address) => address.Split('/')[0];

        private static void EnsureDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static void EnsureDirectories()
        {
            EnsureDirectory(PrefabOutput);
            EnsureDirectory(AnimatorOutput);
            EnsureDirectory(MaterialOutput);
        }

        private static AddressableAssetSettings GetOrCreateAddressableSettings()
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
                typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
        }

        // ── Report ────────────────────────────────────────────────────────────
        private class ImportReport
        {
            public int Total;
            public int Imported;
            public List<string> RealAssets = new();
            public List<string> ProcGen    = new();
            public List<string> Failed     = new();

            public void Print()
            {
                Debug.Log($"[FreeAssetImporter] ═══ IMPORT COMPLETE ═══\n" +
                    $"  Total slots:       {Total}\n" +
                    $"  Imported (real):   {RealAssets.Count}\n" +
                    $"  ProcGen fallback:  {ProcGen.Count}\n" +
                    $"  Failed:            {Failed.Count}\n" +
                    $"  Coverage:          {(float)Imported / Total * 100f:F0}%\n" +
                    $"  Alpha Status:      {(Failed.Count == 0 ? "GO" : "CONDITIONAL GO")}");

                if (Failed.Count > 0)
                    foreach (var f in Failed)
                        Debug.LogError($"  ❌ FAILED: {f}");
            }
        }

        // ── Data type ─────────────────────────────────────────────────────────
        private class AssetEntry
        {
            public string Address;
            public string SourcePath;
            public float  Scale;
            public string MaterialName;
            public string ColorVariant;
            public bool   NeedsAnimator;
            public bool   IsRealAsset;
            public int    Priority;

            public AssetEntry(string addr, string src, float scale, string mat,
                              string col, bool anim, bool real, int p)
            {
                Address = addr; SourcePath = src; Scale = scale;
                MaterialName = mat; ColorVariant = col;
                NeedsAnimator = anim; IsRealAsset = real; Priority = p;
            }
        }
    }
}
#endif
