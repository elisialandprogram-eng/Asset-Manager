using UnityEngine;
using System.Collections.Generic;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.World.Grid;

namespace EternalKingdoms.World.Entities
{
    /// <summary>
    /// Fetches world entity data from the API and places entity GameObjects
    /// on the world map.
    ///
    /// Entity types:
    ///   KingdomEntity  — player kingdoms (2×2 tile footprint)
    ///   MonsterEntity  — monster spawn lairs (1×1 tile)
    ///   CrystalEntity  — crystal resource nodes (1×1 tile)
    ///
    /// All entity prefabs are pooled. SpawnAll() is called on world load;
    /// RefreshAll() is called on poll cycle to update existing entities.
    ///
    /// Own kingdom is tagged with a distinct visual so the player can
    /// always identify their location.
    /// </summary>
    public class WorldEntitySpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject kingdomEntityPrefab;
        [SerializeField] private GameObject monsterEntityPrefab;
        [SerializeField] private GameObject crystalEntityPrefab;

        [Header("Pool Sizes")]
        [SerializeField] private int kingdomPoolSize = 100;
        [SerializeField] private int monsterPoolSize = 300;
        [SerializeField] private int crystalPoolSize = 200;

        [Header("Parent Transforms")]
        [SerializeField] private Transform kingdomParent;
        [SerializeField] private Transform monsterParent;
        [SerializeField] private Transform crystalParent;

        // ── Active instances by entity ID ─────────────────────────────────────
        private readonly Dictionary<string, KingdomEntity>  _kingdoms  = new();
        private readonly Dictionary<string, MonsterEntity>  _monsters  = new();
        private readonly Dictionary<string, CrystalEntity>  _crystals  = new();

        // ── Object pools ──────────────────────────────────────────────────────
        private readonly Queue<KingdomEntity>  _kingdomPool  = new();
        private readonly Queue<MonsterEntity>  _monsterPool  = new();
        private readonly Queue<CrystalEntity>  _crystalPool  = new();

        private string _myKingdomId;
        private SpatialIndex _spatial;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            PrewarmPools();
        }

        private void PrewarmPools()
        {
            for (int i = 0; i < kingdomPoolSize; i++)
                _kingdomPool.Enqueue(CreateEntity<KingdomEntity>(kingdomEntityPrefab, kingdomParent));
            for (int i = 0; i < monsterPoolSize; i++)
                _monsterPool.Enqueue(CreateEntity<MonsterEntity>(monsterEntityPrefab, monsterParent));
            for (int i = 0; i < crystalPoolSize; i++)
                _crystalPool.Enqueue(CreateEntity<CrystalEntity>(crystalEntityPrefab, crystalParent));
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SpawnAll(WorldMapResponseDto map, SpatialIndex spatial, string myKingdomId)
        {
            _myKingdomId = myKingdomId;
            _spatial     = spatial;
            ClearAll();

            if (map.kingdoms != null)
                foreach (var k in map.kingdoms)
                    SpawnKingdom(k);

            if (map.spawns != null)
                foreach (var s in map.spawns)
                    SpawnMonster(s);

            if (map.crystalNodes != null)
                foreach (var c in map.crystalNodes)
                    SpawnCrystal(c);

            Debug.Log($"[WorldEntitySpawner] Spawned {_kingdoms.Count} kingdoms, {_monsters.Count} monsters, {_crystals.Count} crystals.");
        }

        public void RefreshAll(WorldMapResponseDto map, SpatialIndex spatial, string myKingdomId)
        {
            _spatial = spatial;
            _myKingdomId = myKingdomId;

            // Kingdoms
            var seenKingdoms = new HashSet<string>();
            if (map.kingdoms != null)
                foreach (var k in map.kingdoms)
                {
                    seenKingdoms.Add(k.id);
                    if (_kingdoms.TryGetValue(k.id, out var entity))
                        entity.Refresh(k, k.id == myKingdomId);
                    else
                        SpawnKingdom(k);
                }
            DespawnMissing(_kingdoms, _kingdomPool, seenKingdoms);

            // Crystals
            var seenCrystals = new HashSet<string>();
            if (map.crystalNodes != null)
                foreach (var c in map.crystalNodes)
                {
                    seenCrystals.Add(c.id);
                    if (_crystals.TryGetValue(c.id, out var entity))
                        entity.Refresh(c);
                    else
                        SpawnCrystal(c);
                }
            DespawnMissing(_crystals, _crystalPool, seenCrystals);
        }

        public void RefreshMonsters(MonsterSpawnDto[] spawns)
        {
            var seen = new HashSet<string>();
            if (spawns != null)
                foreach (var s in spawns)
                {
                    seen.Add(s.id);
                    if (_monsters.TryGetValue(s.id, out var entity))
                        entity.Refresh(s);
                    else
                        SpawnMonster(s);
                }
            DespawnMissing(_monsters, _monsterPool, seen);
        }

        // ── Spawn helpers ─────────────────────────────────────────────────────

        private void SpawnKingdom(WorldKingdomDto data)
        {
            var entity = _kingdomPool.Count > 0
                ? _kingdomPool.Dequeue()
                : CreateEntity<KingdomEntity>(kingdomEntityPrefab, kingdomParent);
            entity.Initialize(data, data.id == _myKingdomId);
            _kingdoms[data.id] = entity;
        }

        private void SpawnMonster(MonsterSpawnDto data)
        {
            var entity = _monsterPool.Count > 0
                ? _monsterPool.Dequeue()
                : CreateEntity<MonsterEntity>(monsterEntityPrefab, monsterParent);
            entity.Initialize(data);
            _monsters[data.id] = entity;
        }

        private void SpawnCrystal(CrystalNodeDto data)
        {
            var entity = _crystalPool.Count > 0
                ? _crystalPool.Dequeue()
                : CreateEntity<CrystalEntity>(crystalEntityPrefab, crystalParent);
            entity.Initialize(data);
            _crystals[data.id] = entity;
        }

        private void DespawnMissing<T>(
            Dictionary<string, T> active,
            Queue<T> pool,
            HashSet<string> seen) where T : BaseWorldEntity
        {
            var toRemove = new List<string>();
            foreach (var kv in active)
                if (!seen.Contains(kv.Key)) { toRemove.Add(kv.Key); kv.Value.Recycle(); pool.Enqueue(kv.Value); }
            foreach (var id in toRemove) active.Remove(id);
        }

        private void ClearAll()
        {
            foreach (var e in _kingdoms.Values) { e.Recycle(); _kingdomPool.Enqueue(e); }
            foreach (var e in _monsters.Values) { e.Recycle(); _monsterPool.Enqueue(e); }
            foreach (var e in _crystals.Values) { e.Recycle(); _crystalPool.Enqueue(e); }
            _kingdoms.Clear();
            _monsters.Clear();
            _crystals.Clear();
        }

        private T CreateEntity<T>(GameObject prefab, Transform parent) where T : MonoBehaviour
        {
            var go = prefab != null
                ? Instantiate(prefab, parent != null ? parent : transform)
                : new GameObject(typeof(T).Name);
            if (parent != null && prefab == null) go.transform.SetParent(parent);
            go.SetActive(false);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
    }
}
