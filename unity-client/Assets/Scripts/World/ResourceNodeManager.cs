using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EternalKingdoms.World.DTOs;
using EternalKingdoms.World.Entities;

namespace EternalKingdoms.World
{
    /// <summary>
    /// ResourceNodeManager — owns the pool of ResourceNodeEntity objects and
    /// reconciles them with the backend data polled by ResourceSpawnService.
    ///
    /// Architecture:
    ///   - Pre-allocates 400 node slots (matches worldSpawns table capacity).
    ///   - SpawnAll() on initial load. RefreshNodes() on every poll.
    ///   - Handles node expiry: removes entities for expired/depleted spawns.
    ///   - Fires OnNodeSelected for WorldSelectionManager.
    ///
    /// Scene placement:
    ///   GameObject "ResourceNodeManager" in World Scene.
    ///   Drag ResourceNodeEntity prefab into the inspector slot.
    /// </summary>
    public class ResourceNodeManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [SerializeField] private GameObject _resourceNodePrefab;
        [SerializeField] private int        _poolSize = 400;
        [SerializeField] private Transform  _poolParent;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        public event Action<ResourceNodeEntity> OnNodeSelected;

        // -------------------------------------------------------------------------
        // Pool + index
        // -------------------------------------------------------------------------

        private readonly Queue<ResourceNodeEntity>      _pool    = new();
        private readonly Dictionary<int, ResourceNodeEntity> _active = new();

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (_poolParent == null) _poolParent = transform;
            PreAllocate();
        }

        private void PreAllocate()
        {
            if (_resourceNodePrefab == null) return;
            for (int i = 0; i < _poolSize; i++)
            {
                var go = Instantiate(_resourceNodePrefab, _poolParent);
                var entity = go.GetComponent<ResourceNodeEntity>();
                entity.Recycle();
                _pool.Enqueue(entity);
            }
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// Replace the entire node set with fresh backend data.
        public void SpawnAll(ResourceNodeDto[] nodes)
        {
            // Recycle all active
            foreach (var entity in _active.Values)
            {
                entity.OnNodeClicked -= HandleNodeClicked;
                entity.Recycle();
                _pool.Enqueue(entity);
            }
            _active.Clear();

            foreach (var dto in nodes)
                SpawnNode(dto);
        }

        /// Reconcile live data without replacing everything.
        public void RefreshNodes(ResourceNodeDto[] freshNodes)
        {
            var freshIds = new HashSet<int>();
            foreach (var dto in freshNodes)
            {
                freshIds.Add(dto.id);
                if (!_active.ContainsKey(dto.id))
                    SpawnNode(dto);
            }

            // Remove nodes that are no longer active on server
            var toRemove = new List<int>();
            foreach (var id in _active.Keys)
                if (!freshIds.Contains(id))
                    toRemove.Add(id);

            foreach (var id in toRemove)
                RecycleNode(id);
        }

        public ResourceNodeEntity GetNode(int id) =>
            _active.TryGetValue(id, out var e) ? e : null;

        public IReadOnlyCollection<ResourceNodeEntity> GetAllNodes() =>
            _active.Values;

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------

        private void SpawnNode(ResourceNodeDto dto)
        {
            if (dto.status != "active") return;

            // Convert backend coords (0–10000) to Unity tile then world space
            const float TILE_SIZE = 5.0f;
            const float OFFSET    = 5120f;
            const float NODE_HEIGHT = 0.5f;

            float toTile(int v) => v * 0.2048f;  // backend 0–10000 → tile 0–2048
            float tileToWorld(float t) => t * TILE_SIZE - OFFSET;

            float wx = tileToWorld(toTile(dto.posX));
            float wz = tileToWorld(toTile(dto.posY));

            if (_pool.Count == 0)
            {
                Debug.LogWarning("[ResourceNodeManager] Pool exhausted, growing");
                if (_resourceNodePrefab)
                {
                    var go = Instantiate(_resourceNodePrefab, _poolParent);
                    _pool.Enqueue(go.GetComponent<ResourceNodeEntity>());
                }
                else return;
            }

            var entity = _pool.Dequeue();
            entity.transform.position = new Vector3(wx, NODE_HEIGHT, wz);
            entity.Initialize(dto);
            entity.OnNodeClicked += HandleNodeClicked;
            _active[dto.id] = entity;
        }

        private void RecycleNode(int id)
        {
            if (!_active.TryGetValue(id, out var entity)) return;
            entity.OnNodeClicked -= HandleNodeClicked;
            entity.Recycle();
            _pool.Enqueue(entity);
            _active.Remove(id);
        }

        private void HandleNodeClicked(ResourceNodeEntity entity) =>
            OnNodeSelected?.Invoke(entity);
    }
}
