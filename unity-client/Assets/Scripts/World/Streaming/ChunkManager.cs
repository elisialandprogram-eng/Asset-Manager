using UnityEngine;
using System.Collections.Generic;
using EternalKingdoms.World.Grid;

namespace EternalKingdoms.World.Streaming
{
    /// <summary>
    /// Manages the pool of Chunk GameObjects.
    ///
    /// Pattern: object pool — never Instantiate/Destroy chunks at runtime.
    /// The pool pre-allocates MAX_POOL_SIZE chunks at scene start.
    /// WorldStreamingManager requests chunks from here; ChunkManager
    /// handles pool growth when needed.
    ///
    /// Active chunks are indexed by ChunkCoordinate for fast lookup.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        public static ChunkManager Instance { get; private set; }

        [Header("Pool")]
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private int initialPoolSize = 60;   // (2×radius+1)² at radius=3
        [SerializeField] private int maxPoolSize = 121;      // (2×5+1)²

        [Header("References")]
        [SerializeField] private Transform chunkParent;

        private readonly Queue<Chunk> _pool = new();
        private readonly Dictionary<Vector2Int, Chunk> _active = new();

        public int ActiveCount => _active.Count;
        public int PoolCount => _pool.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Pool initialization ───────────────────────────────────────────────

        public void PrewarmPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
                _pool.Enqueue(CreateChunk());
            Debug.Log($"[ChunkManager] Preallocated {initialPoolSize} chunks.");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Load a chunk at the given coordinate, using a pooled instance.</summary>
        public Chunk LoadChunk(ChunkCoordinate coord, int worldSeed)
        {
            var key = coord.ToVector2Int();
            if (_active.ContainsKey(key)) return _active[key]; // already loaded

            var chunk = _pool.Count > 0 ? _pool.Dequeue() : CreateChunk();
            chunk.Initialize(coord, worldSeed);
            _active[key] = chunk;
            return chunk;
        }

        /// <summary>Unload and return a chunk to the pool.</summary>
        public void UnloadChunk(ChunkCoordinate coord)
        {
            var key = coord.ToVector2Int();
            if (!_active.TryGetValue(key, out var chunk)) return;
            _active.Remove(key);
            chunk.Recycle();
            if (_pool.Count < maxPoolSize)
                _pool.Enqueue(chunk);
            else
                Destroy(chunk.gameObject);
        }

        public bool IsLoaded(ChunkCoordinate coord) =>
            _active.ContainsKey(coord.ToVector2Int());

        public Chunk GetActive(ChunkCoordinate coord) =>
            _active.TryGetValue(coord.ToVector2Int(), out var c) ? c : null;

        /// <summary>Returns all active chunk coordinates (for visibility culling).</summary>
        public IEnumerable<ChunkCoordinate> GetActiveCoords()
        {
            foreach (var kv in _active)
                yield return kv.Value.Coord;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private Chunk CreateChunk()
        {
            var go = chunkPrefab != null
                ? Instantiate(chunkPrefab, chunkParent != null ? chunkParent : transform)
                : new GameObject("Chunk_Pool", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));

            if (chunkParent != null && chunkPrefab == null)
                go.transform.SetParent(chunkParent);

            go.SetActive(false);
            return go.GetComponent<Chunk>() ?? go.AddComponent<Chunk>();
        }
    }
}
