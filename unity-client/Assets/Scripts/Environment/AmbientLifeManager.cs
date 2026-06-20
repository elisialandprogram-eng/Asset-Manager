using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Terrain;

namespace EternalKingdoms.Environment
{
    /// <summary>
    /// AmbientLifeManager — Spawns dynamic ambient life into the world scene.
    ///
    /// Phase 5.5 (U5.5.7) ambient life categories:
    ///   Birds       — flock VFX + 3D AudioSource, fly in arcs above trees
    ///   Butterflies — small particle swarms near flowers/bushes
    ///   Deer        — gentle idle near forest edge (spawned from pool)
    ///   Rabbits     — scatter-run behaviour on player proximity
    ///   Wind        — directional particle streams across open biomes
    ///   FallingLeaves — constant leaf fall in forest / autumn biomes
    ///   FirefliesGlow — evening-only particle bursts near swamp/forest
    ///
    /// Biome mapping:
    ///   Grasslands  — birds, butterflies, rabbits, wind
    ///   Forest      — birds, deer, rabbits, falling leaves, fireflies
    ///   Snow        — wind particles (snow drift), occasional wolf VFX
    ///   Desert      — wind (sand swirl), rare vulture arc
    ///   Highlands   — wind (strong gust), eagle arc
    ///   Swamp       — fireflies, butterflies, frog sounds (audio only)
    ///   Volcanic    — ash particles, steam VFX (from VFXLibrary)
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad (World scene)
    ///   - Each life type uses a separate pool
    ///   - Life is positioned relative to the camera frustum (always near-camera)
    ///   - Wind: DirectionalParticleSystem driven by wind speed float
    ///   - BiomeTerrainController.OnBiomeEntered → SwitchBiomeLife()
    ///   - Performance: max 10 bird flocks, 20 butterfly swarms, 5 deer active at once
    /// </summary>
    public class AmbientLifeManager : MonoBehaviour
    {
        public static AmbientLifeManager Instance { get; private set; }

        [Header("Life Prefabs")]
        [SerializeField] private GameObject birdFlockVFX;
        [SerializeField] private GameObject butterflySwarmVFX;
        [SerializeField] private GameObject fallingLeavesVFX;
        [SerializeField] private GameObject fireflyVFX;
        [SerializeField] private GameObject windParticlesVFX;
        [SerializeField] private GameObject ashParticlesVFX;
        [SerializeField] private GameObject deerPrefab;
        [SerializeField] private GameObject rabbitPrefab;

        [Header("Spawn Counts by Biome")]
        [SerializeField] private AmbientLifeProfile profileGrasslands;
        [SerializeField] private AmbientLifeProfile profileForest;
        [SerializeField] private AmbientLifeProfile profileSnow;
        [SerializeField] private AmbientLifeProfile profileDesert;
        [SerializeField] private AmbientLifeProfile profileHighlands;
        [SerializeField] private AmbientLifeProfile profileSwamp;
        [SerializeField] private AmbientLifeProfile profileVolcanic;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTarget;

        [Header("Spawn Radius")]
        [SerializeField] private float spawnRadiusMin = 20f;
        [SerializeField] private float spawnRadiusMax = 60f;

        private readonly Dictionary<string, AmbientLifeProfile> _profileRegistry = new();
        private readonly Dictionary<string, List<GameObject>> _activePools = new();
        private string _currentBiome = "grasslands";
        private AmbientLifeProfile _currentProfile;
        private Coroutine _spawnCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildRegistry();
        }

        private void Start()
        {
            // Subscribe to biome changes
            if (BiomeTerrainController.Instance != null)
                BiomeTerrainController.Instance.OnBiomeEntered += SwitchBiomeLife;

            SwitchBiomeLife(_currentBiome);
        }

        private void OnDestroy()
        {
            if (BiomeTerrainController.Instance != null)
                BiomeTerrainController.Instance.OnBiomeEntered -= SwitchBiomeLife;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SwitchBiomeLife(string biomeKey)
        {
            _currentBiome = biomeKey;
            _profileRegistry.TryGetValue(biomeKey, out _currentProfile);

            // Stop existing
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            ReturnAllToPool();

            if (_currentProfile == null) return;
            _spawnCoroutine = StartCoroutine(LifeSpawnLoop());
        }

        // ── Spawn Loop ────────────────────────────────────────────────────────

        private IEnumerator LifeSpawnLoop()
        {
            // Initial spawn burst
            yield return SpawnCategory("birds",    birdFlockVFX,    _currentProfile.birdFlocks,    15f);
            yield return SpawnCategory("butterflies", butterflySwarmVFX, _currentProfile.butterflySwarms, 5f);
            yield return SpawnCategory("leaves",   fallingLeavesVFX, _currentProfile.fallingLeaves, 0f);
            yield return SpawnCategory("fireflies", fireflyVFX,     _currentProfile.fireflies,     1f);
            yield return SpawnCategory("wind",     _currentProfile.isVolcanic ? ashParticlesVFX : windParticlesVFX,
                                       _currentProfile.windEmitters, 0f);

            // Refresh loop: despawn expired, spawn new
            while (true)
            {
                yield return new WaitForSeconds(8f);

                // Remove inactive items
                foreach (var kvp in _activePools)
                {
                    kvp.Value.RemoveAll(go => go == null || !go.activeSelf);
                }

                // Spawn birds on interval
                if (birdFlockVFX != null && CountActive("birds") < _currentProfile.birdFlocks)
                    yield return SpawnSingle("birds", birdFlockVFX, BirdArcPosition(), 30f);
            }
        }

        private IEnumerator SpawnCategory(string key, GameObject prefab, int count, float height)
        {
            if (prefab == null || count <= 0) yield break;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = RandomCameraRelativePosition(height);
                SpawnSingleImmediate(key, prefab, pos);
                if (i % 3 == 0) yield return null;
            }
        }

        private IEnumerator SpawnSingle(string key, GameObject prefab, Vector3 pos, float lifetime)
        {
            var go = SpawnSingleImmediate(key, prefab, pos);
            if (lifetime > 0f)
            {
                yield return new WaitForSeconds(lifetime);
                ReturnToPool(key, go);
            }
        }

        private GameObject SpawnSingleImmediate(string key, GameObject prefab, Vector3 pos)
        {
            var go = Instantiate(prefab, pos, Quaternion.identity, transform);
            if (!_activePools.ContainsKey(key)) _activePools[key] = new List<GameObject>();
            _activePools[key].Add(go);

            // Auto-play particles
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
                ps.Play();

            return go;
        }

        // ── Position Helpers ──────────────────────────────────────────────────

        private Vector3 RandomCameraRelativePosition(float height)
        {
            Vector3 origin = cameraTarget != null ? cameraTarget.position : Vector3.zero;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist  = UnityEngine.Random.Range(spawnRadiusMin, spawnRadiusMax);
            return origin + new Vector3(
                Mathf.Cos(angle) * dist,
                height + UnityEngine.Random.Range(0f, 5f),
                Mathf.Sin(angle) * dist
            );
        }

        private Vector3 BirdArcPosition()
        {
            Vector3 origin = cameraTarget != null ? cameraTarget.position : Vector3.zero;
            return origin + new Vector3(
                UnityEngine.Random.Range(-80f, 80f),
                UnityEngine.Random.Range(20f, 50f),
                UnityEngine.Random.Range(-80f, 80f)
            );
        }

        // ── Pooling ───────────────────────────────────────────────────────────

        private int CountActive(string key)
        {
            return _activePools.TryGetValue(key, out var list) ? list.Count : 0;
        }

        private void ReturnToPool(string key, GameObject go)
        {
            if (go == null) return;
            if (_activePools.TryGetValue(key, out var list)) list.Remove(go);
            Destroy(go);
        }

        private void ReturnAllToPool()
        {
            foreach (var kvp in _activePools)
                foreach (var go in kvp.Value)
                    if (go != null) Destroy(go);
            _activePools.Clear();
        }

        // ── Registry ──────────────────────────────────────────────────────────

        private void BuildRegistry()
        {
            if (profileGrasslands != null) _profileRegistry["grasslands"] = profileGrasslands;
            if (profileForest     != null) _profileRegistry["forest"]     = profileForest;
            if (profileSnow       != null) _profileRegistry["snow"]       = profileSnow;
            if (profileDesert     != null) _profileRegistry["desert"]     = profileDesert;
            if (profileHighlands  != null) _profileRegistry["highlands"]  = profileHighlands;
            if (profileSwamp      != null) _profileRegistry["swamp"]      = profileSwamp;
            if (profileVolcanic   != null) _profileRegistry["volcanic"]   = profileVolcanic;
        }
    }

    // ── Life Profile ScriptableObject ─────────────────────────────────────────

    [CreateAssetMenu(fileName = "AmbientLifeProfile", menuName = "EK/Environment/Ambient Life Profile")]
    public class AmbientLifeProfile : ScriptableObject
    {
        [Header("Spawn Counts")]
        [Range(0, 10)] public int birdFlocks       = 3;
        [Range(0, 20)] public int butterflySwarms  = 5;
        [Range(0, 10)] public int fallingLeaves    = 0;
        [Range(0, 10)] public int fireflies        = 0;
        [Range(0, 5)]  public int windEmitters     = 1;

        [Header("Special")]
        public bool isVolcanic;  // uses ash particles instead of wind
    }
}
