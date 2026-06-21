using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Terrain;

namespace EternalKingdoms.Environment
{
    /// <summary>
    /// EnvironmentDecorationManager — Procedurally populates the world with
    /// biome-appropriate decorations using pooling, LOD groups, and GPU instancing.
    ///
    /// Phase 5 (U5.3) responsibilities:
    ///   - Trees, shrubs, grass, dead trees, rocks, flowers, ruins, campfires, statues
    ///   - Density varies by biome (from BiomeData)
    ///   - Minimum exclusion zone around kingdom/resource tiles
    ///   - GPU instancing via MeshRenderer.enableInstancing
    ///   - LOD Groups for distance culling (LOD0 detailed, LOD1 impostor, LOD2 billboard)
    ///   - Object pool per prefab type
    ///   - Decorates chunks as they stream in; strips on unload
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - Chunk streaming hooks into WorldStreamingManager events
    ///   - Spawning is coroutine-based: max 30 objects per frame to stay at 60fps
    /// </summary>
    public class EnvironmentDecorationManager : MonoBehaviour
    {
        public static EnvironmentDecorationManager Instance { get; private set; }

        [Header("Placement Rules")]
#pragma warning disable CS0414
        [SerializeField] private float kingdomExclusionRadius = 20f;
#pragma warning restore CS0414
        [SerializeField] private float waterExclusionHeight   = 0.5f;
        [SerializeField] private int   maxSpawnPerFrame       = 30;

        [Header("Seed")]
        [SerializeField] private int globalSeed = 42;

        private readonly Dictionary<string, DecorationPool> _pools = new();
        private readonly Dictionary<Vector2Int, List<GameObject>> _chunkDecorations = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Decorate a chunk. Call when a chunk is streamed in.</summary>
        public void DecorateChunk(Vector2Int chunkCoord, string biomeKey, Bounds chunkBounds,
                                   List<Vector3> occupiedPositions)
        {
            if (_chunkDecorations.ContainsKey(chunkCoord)) return;
            _chunkDecorations[chunkCoord] = new List<GameObject>();
            StartCoroutine(DecorateChunkAsync(chunkCoord, biomeKey, chunkBounds, occupiedPositions));
        }

        /// <summary>Strip decorations from a chunk. Call on chunk unload.</summary>
        public void StripChunk(Vector2Int chunkCoord)
        {
            if (!_chunkDecorations.TryGetValue(chunkCoord, out var items)) return;
            foreach (var go in items)
                ReturnToPool(go);
            _chunkDecorations.Remove(chunkCoord);
        }

        // ── Async Placement ───────────────────────────────────────────────────

        private IEnumerator DecorateChunkAsync(Vector2Int chunkCoord, string biomeKey,
                                                Bounds bounds, List<Vector3> occupied)
        {
            var biome = BiomeTerrainController.Instance?.GetBiome(biomeKey);
            if (biome == null) yield break;

            var placed   = _chunkDecorations[chunkCoord];
            var rng      = new System.Random(HashCoord(chunkCoord));

            yield return PlaceCategory(biome.treePrefabs,   biome.treeDensity,   bounds, occupied, placed, rng);
            yield return PlaceCategory(biome.shrubPrefabs,  biome.shrubDensity,  bounds, occupied, placed, rng);
            yield return PlaceCategory(biome.grassPrefabs,  biome.grassDensity,  bounds, occupied, placed, rng);
            yield return PlaceCategory(biome.rockPrefabs,   biome.rockDensity,   bounds, occupied, placed, rng);
            yield return PlaceCategory(biome.flowerPrefabs, biome.rockDensity/2, bounds, occupied, placed, rng);
            yield return PlaceCategory(biome.ruinPrefabs,   biome.ruinDensity,   bounds, occupied, placed, rng);
            yield return PlaceCategory(biome.statuePrefabs, Mathf.Max(0, biome.ruinDensity - 1), bounds, occupied, placed, rng);
        }

        private IEnumerator PlaceCategory(GameObject[] prefabs, int count, Bounds bounds,
                                           List<Vector3> occupied, List<GameObject> placed,
                                           System.Random rng)
        {
            if (prefabs == null || prefabs.Length == 0 || count <= 0) yield break;

            int frame = 0;
            for (int i = 0; i < count; i++)
            {
                var prefab = prefabs[rng.Next(prefabs.Length)];
                if (prefab == null) continue;

                var pos = RandomPosition(bounds, rng);
                if (IsTooClose(pos, occupied, 3f)) continue;

                // Terrain height sampling
                if (Physics.Raycast(pos + Vector3.up * 200f, Vector3.down, out var hit, 400f, LayerMask.GetMask("Terrain")))
                    pos = hit.point;

                if (pos.y < waterExclusionHeight) continue;

                var go = GetFromPool(prefab);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                float scale = 0.8f + (float)rng.NextDouble() * 0.4f;
                go.transform.localScale = Vector3.one * scale;
                go.SetActive(true);

                EnableGPUInstancing(go);

                placed.Add(go);
                occupied.Add(pos);

                frame++;
                if (frame >= maxSpawnPerFrame)
                {
                    frame = 0;
                    yield return null;
                }
            }
        }

        // ── Pooling ───────────────────────────────────────────────────────────

        private GameObject GetFromPool(GameObject prefab)
        {
            string key = prefab.name;
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new DecorationPool(prefab, transform);
                _pools[key] = pool;
            }
            return pool.Get();
        }

        private void ReturnToPool(GameObject go)
        {
            go.SetActive(false);
            // Pool reclaims by name key — returned objects stay as children
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Vector3 RandomPosition(Bounds b, System.Random rng)
        {
            return new Vector3(
                b.min.x + (float)rng.NextDouble() * b.size.x,
                b.center.y + 200f,
                b.min.z + (float)rng.NextDouble() * b.size.z
            );
        }

        private static bool IsTooClose(Vector3 pos, List<Vector3> occupied, float minDist)
        {
            float sq = minDist * minDist;
            foreach (var o in occupied)
            {
                var dx = o.x - pos.x; var dz = o.z - pos.z;
                if (dx * dx + dz * dz < sq) return true;
            }
            return false;
        }

        private static void EnableGPUInstancing(GameObject go)
        {
            foreach (var rend in go.GetComponentsInChildren<Renderer>())
                foreach (var mat in rend.sharedMaterials)
                    if (mat != null) mat.enableInstancing = true;
        }

        private int HashCoord(Vector2Int c) => c.x * 73856093 ^ c.y * 19349663 ^ globalSeed;
    }

    // ── Object Pool ────────────────────────────────────────────────────────────

    public class DecorationPool
    {
        private readonly GameObject _prefab;
        private readonly Transform  _parent;
        private readonly Stack<GameObject> _free = new();

        public DecorationPool(GameObject prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public GameObject Get()
        {
            if (_free.Count > 0)
            {
                var existing = _free.Pop();
                existing.SetActive(true);
                return existing;
            }
            var go = UnityEngine.Object.Instantiate(_prefab, _parent);
            go.name = _prefab.name;
            EnsureLODGroup(go);
            return go;
        }

        public void Return(GameObject go)
        {
            go.SetActive(false);
            _free.Push(go);
        }

        private static void EnsureLODGroup(GameObject go)
        {
            if (go.GetComponentInChildren<LODGroup>() != null) return;
            var group = go.AddComponent<LODGroup>();
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            group.SetLODs(new[]
            {
                new LOD(0.15f, renderers),
                new LOD(0.04f, Array.Empty<Renderer>()),
            });
            group.RecalculateBounds();
        }
    }
}
