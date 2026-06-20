using UnityEngine;
using System;
using System.Collections.Generic;

namespace EternalKingdoms.Terrain
{
    /// <summary>
    /// BiomeTerrainController — Orchestrates 7-biome terrain visuals.
    ///
    /// Phase 5 (U5.2) responsibilities:
    ///   - Defines per-biome material, decoration density, ambient VFX, and fog settings
    ///   - Drives TerrainSplatMapper to blend textures per-chunk
    ///   - Controls world-level environment transitions when camera crosses biome boundaries
    ///   - Manages road and river overlays
    ///
    /// Biomes: Grasslands, Forest, Snow, Desert, Highlands, Swamp, Volcanic
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - BiomeData ScriptableObjects assigned in Inspector
    ///   - TerrainSplatMapper reads BiomeData to build splat weight maps
    ///   - BiomeRegistry is indexed by the biome string returned from the API
    /// </summary>
    public class BiomeTerrainController : MonoBehaviour
    {
        public static BiomeTerrainController Instance { get; private set; }

        [Header("Biome Definitions — assign ScriptableObjects in Inspector")]
        [SerializeField] private BiomeData biomeGrasslands;
        [SerializeField] private BiomeData biomeForest;
        [SerializeField] private BiomeData biomeSnow;
        [SerializeField] private BiomeData biomeDesert;
        [SerializeField] private BiomeData biomeHighlands;
        [SerializeField] private BiomeData biomeSwamp;
        [SerializeField] private BiomeData biomeVolcanic;

        [Header("Road & River")]
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material riverMaterial;
        [SerializeField] private Material lakeMaterial;
        [SerializeField] private Material cliffMaterial;

        [Header("Terrain Decals")]
        [SerializeField] private GameObject[] decalPrefabs;

        private readonly Dictionary<string, BiomeData> _registry = new();

        public event Action<string> OnBiomeEntered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildRegistry();
        }

        private void BuildRegistry()
        {
            Register("grasslands", biomeGrasslands);
            Register("forest",     biomeForest);
            Register("snow",       biomeSnow);
            Register("desert",     biomeDesert);
            Register("highlands",  biomeHighlands);
            Register("swamp",      biomeSwamp);
            Register("volcanic",   biomeVolcanic);
        }

        private void Register(string key, BiomeData data)
        {
            if (data != null) _registry[key] = data;
        }

        /// <summary>Returns BiomeData for a biome string from the API.</summary>
        public BiomeData GetBiome(string biomeKey)
        {
            return _registry.TryGetValue(biomeKey.ToLower(), out var d) ? d : biomeGrasslands;
        }

        /// <summary>
        /// Called by WorldStreamingManager when the camera's current biome changes.
        /// Updates fog, ambient VFX, and global material overrides.
        /// </summary>
        public void OnEnterBiome(string biomeKey)
        {
            var biome = GetBiome(biomeKey);
            if (biome == null) return;

            // Fog
            RenderSettings.fog        = biome.fogEnabled;
            RenderSettings.fogColor   = biome.fogColor;
            RenderSettings.fogDensity = biome.fogDensity;

            // Ambient light
            RenderSettings.ambientLight = biome.ambientColor;
            RenderSettings.ambientIntensity = biome.ambientIntensity;

            OnBiomeEntered?.Invoke(biomeKey);
            Debug.Log($"[BiomeTerrain] Entered biome: {biomeKey}");
        }

        /// <summary>Applies biome splat to a terrain chunk.</summary>
        public void ApplyBiomeToChunk(UnityEngine.Terrain terrain, string biomeKey)
        {
            var biome = GetBiome(biomeKey);
            if (biome == null || terrain == null) return;

            var splatMapper = GetComponent<TerrainSplatMapper>() ?? gameObject.AddComponent<TerrainSplatMapper>();
            splatMapper.Apply(terrain, biome);
        }
    }

    // ── BiomeData ScriptableObject ─────────────────────────────────────────────

    [CreateAssetMenu(fileName = "BiomeData", menuName = "EK/Visual/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        [Header("Identity")]
        public string biomeKey;
        public string displayName;

        [Header("Terrain Textures — Terrain Layer assets")]
        public TerrainLayer groundLayer;
        public TerrainLayer detailLayer1;
        public TerrainLayer detailLayer2;
        public TerrainLayer cliffLayer;

        [Header("Foliage")]
        public GameObject[] treePrefabs;
        public GameObject[] shrubPrefabs;
        public GameObject[] grassPrefabs;
        public GameObject[] rockPrefabs;
        public GameObject[] flowerPrefabs;

        [Header("Ambient & Ruins")]
        public GameObject[] ruinPrefabs;
        public GameObject[] statuePrefabs;
        public GameObject campfirePrefab;

        [Header("Density (units per chunk tile)")]
        [Range(0, 50)] public int treeDensity    = 15;
        [Range(0, 50)] public int shrubDensity   = 20;
        [Range(0, 50)] public int grassDensity   = 30;
        [Range(0, 20)] public int rockDensity    = 8;
        [Range(0, 10)] public int ruinDensity    = 1;

        [Header("Environment")]
        public bool   fogEnabled      = false;
        public Color  fogColor        = Color.white;
        [Range(0, 0.1f)] public float fogDensity = 0.01f;
        public Color  ambientColor    = Color.white;
        [Range(0, 2f)] public float ambientIntensity = 1f;

        [Header("Ambient VFX")]
        public GameObject[] ambientVFXPrefabs;
        public float ambientVFXDensity = 0.5f;

        [Header("Audio")]
        public AudioClip ambientLoop;
        public AudioClip ambientStinger;
    }
}
