using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.World.Grid;

namespace EternalKingdoms.World.Streaming
{
    /// <summary>
    /// Dynamic chunk streaming system.
    ///
    /// Streaming parameters (UNITY_WORLD_SYSTEM.md):
    ///   Visible radius:  5 chunks around camera
    ///   Preload buffer:  1 chunk ahead in movement direction
    ///   Unload radius:   7 chunks — beyond this is unloaded
    ///
    /// Algorithm:
    ///   1. Every SCAN_INTERVAL seconds, determine the camera's current chunk.
    ///   2. Compute desired set = all chunks within LOAD_RADIUS.
    ///   3. Load any desired chunks not already active.
    ///   4. Unload any active chunks beyond UNLOAD_RADIUS.
    ///
    /// Performance: loads happen in coroutines, at most MAX_LOADS_PER_FRAME
    /// per frame to avoid frame spikes (target < 20ms per frame, U2.11).
    /// </summary>
    public class WorldStreamingManager : MonoBehaviour
    {
        public static WorldStreamingManager Instance { get; private set; }

        [Header("Streaming Radii (chunks)")]
        [SerializeField] private int loadRadius   = 5;
        [SerializeField] private int unloadRadius = 7;

        [Header("Performance")]
        [SerializeField] private float scanInterval       = 0.5f;  // seconds between scans
        [SerializeField] private int   maxLoadsPerFrame   = 2;     // chunk loads per frame
        [SerializeField] private int   maxUnloadsPerFrame = 4;

        [Header("References")]
        [SerializeField] private UnityEngine.Camera worldCamera;

        private WorldGrid _grid;
        private ChunkCoordinate _lastCameraChunk;
        private int _worldSeed;
        private bool _initialized;

        private Coroutine _streamCoroutine;

        // Queues for spreading load over multiple frames
        private readonly Queue<ChunkCoordinate> _loadQueue   = new();
        private readonly Queue<ChunkCoordinate> _unloadQueue = new();

        // Stats
        public int LoadQueueDepth   => _loadQueue.Count;
        public int UnloadQueueDepth => _unloadQueue.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Initialize(WorldGrid grid, int worldSeed, UnityEngine.Camera cam = null)
        {
            _grid = grid;
            _worldSeed = worldSeed;
            if (cam != null) worldCamera = cam;
            _initialized = true;

            ChunkManager.Instance?.PrewarmPool();
            _streamCoroutine = StartCoroutine(StreamLoop());

            Debug.Log($"[WorldStreamingManager] Initialized — seed={worldSeed}, load={loadRadius}ch, unload={unloadRadius}ch");
        }

        public void Shutdown()
        {
            if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
            _loadQueue.Clear();
            _unloadQueue.Clear();
            _initialized = false;
        }

        // ── Core loop ─────────────────────────────────────────────────────────

        private IEnumerator StreamLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(scanInterval);
                if (!_initialized || worldCamera == null) continue;

                var camChunk = ChunkCoordinate.FromUnity(worldCamera.transform.position);
                if (camChunk != _lastCameraChunk)
                {
                    _lastCameraChunk = camChunk;
                    ScheduleLoadUnload(camChunk);
                }

                // Process load queue
                int loads = 0;
                while (_loadQueue.Count > 0 && loads < maxLoadsPerFrame)
                {
                    var coord = _loadQueue.Dequeue();
                    if (!ChunkManager.Instance.IsLoaded(coord))
                    {
                        ChunkManager.Instance.LoadChunk(coord, _worldSeed);
                        _grid?.RegisterChunkLoaded(coord);
                        loads++;
                    }
                }

                // Process unload queue
                int unloads = 0;
                while (_unloadQueue.Count > 0 && unloads < maxUnloadsPerFrame)
                {
                    var coord = _unloadQueue.Dequeue();
                    if (ChunkManager.Instance.IsLoaded(coord))
                    {
                        ChunkManager.Instance.UnloadChunk(coord);
                        _grid?.RegisterChunkUnloaded(coord);
                        unloads++;
                    }
                }

                if (loads > 0 || unloads > 0)
                    yield return null; // give Unity a frame to breathe after loads
            }
        }

        private void ScheduleLoadUnload(ChunkCoordinate center)
        {
            var desired = new HashSet<Vector2Int>();
            foreach (var c in WorldGrid.GetChunksInRadius(center, loadRadius))
                desired.Add(c.ToVector2Int());

            // Queue loads for desired chunks not yet active
            foreach (var v in desired)
            {
                var c = new ChunkCoordinate(v.x, v.y);
                if (!ChunkManager.Instance.IsLoaded(c))
                    _loadQueue.Enqueue(c);
            }

            // Queue unloads for active chunks outside unload radius
            foreach (var activeCoord in ChunkManager.Instance.GetActiveCoords())
            {
                if (activeCoord.EuclideanDistanceTo(center) > unloadRadius)
                    _unloadQueue.Enqueue(activeCoord);
            }
        }

        // ── Preload at coordinate (GoTo navigation) ───────────────────────────

        public void PreloadAroundCoord(WorldCoordinate coord)
        {
            var chunk = coord.ToChunk();
            foreach (var c in WorldGrid.GetChunksInRadius(chunk, loadRadius))
                if (!ChunkManager.Instance.IsLoaded(c))
                    _loadQueue.Enqueue(c);
        }
    }
}
