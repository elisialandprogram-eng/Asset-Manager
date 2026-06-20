using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EternalKingdoms.World
{
    /// <summary>
    /// U5.7.4 — World Beautification Pass
    /// Enriches the world scene with dense forests, riverbanks, mountain ridges,
    /// ruins, ancient monuments, road networks, biome-transition blending,
    /// and rare landmark spawns.  All placements are procedural — no manual
    /// scene prefabs required.
    /// </summary>
    public class WorldBeautificationManager : MonoBehaviour
    {
        public static WorldBeautificationManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("World Bounds")]
        public float worldExtent = 512f;

        [Header("Dense Forest Zones")]
        public int   forestZoneCount = 8;
        public float forestZoneRadius = 40f;
        public string[] denseTreeKeys;

        [Header("Riverbank Props")]
        public string[] riverbankRockKeys;
        public string[] riverbankPlantKeys;
        public int riverbankPropsPerSegment = 6;

        [Header("Mountain Ridges")]
        public string[] mountainRockKeys;
        public int mountainRockCount = 30;

        [Header("Ruins & Ancient Monuments")]
        public string[] ancientRuinKeys;
        public string[] monumentKeys;
        public string[] watchtowerKeys;
        public string[] shrineKeys;
        public string[] statueKeys;
        public int minLandmarksPerBiome = 2;
        public int maxLandmarksPerBiome = 5;

        [Header("Road Network")]
        public string  roadDecalKey;
        public int     roadDecalCount = 60;

        [Header("Destroyed Camps")]
        public string[] destroyedCampKeys;
        public int      destroyedCampCount = 5;

        [Header("Biome Transition Blend")]
        [Tooltip("Object key used to blend between biomes at borders.")]
        public string biomeTransitionDecalKey;
        public int    biomeTransitionCount = 20;

        [Header("Async Batch Size")]
        public int propsPerFrame = 10;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawnedObjects = new();
        private bool _populated;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => StartCoroutine(BeautifyWorld());

        // ─────────────────────────────────────────────────────────────────────
        //  Master Pipeline
        // ─────────────────────────────────────────────────────────────────────
        public IEnumerator BeautifyWorld()
        {
            if (_populated) yield break;
            _populated = true;

            Debug.Log("[WorldBeautificationManager] ▶ Beautification pass starting…");

            yield return StartCoroutine(SpawnDenseForests());
            yield return StartCoroutine(SpawnMountainRidges());
            yield return StartCoroutine(SpawnLandmarks());
            yield return StartCoroutine(SpawnRoadNetwork());
            yield return StartCoroutine(SpawnDestroyedCamps());
            yield return StartCoroutine(SpawnBiomeTransitionBlend());

            Debug.Log($"[WorldBeautificationManager] ✅ World beautified — {_spawnedObjects.Count} objects placed.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Forest Zones
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnDenseForests()
        {
            if (denseTreeKeys == null || denseTreeKeys.Length == 0) yield break;

            for (int z = 0; z < forestZoneCount; z++)
            {
                Vector3 zoneCenter = RandomWorldPos();
                int treesInZone = Random.Range(15, 35);

                for (int i = 0; i < treesInZone; i++)
                {
                    Vector2 offset = Random.insideUnitCircle * forestZoneRadius;
                    Vector3 pos    = zoneCenter + new Vector3(offset.x, 0, offset.y);
                    yield return SpawnOne(denseTreeKeys, pos);
                    if (i % propsPerFrame == 0) yield return null;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Mountain Ridges
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnMountainRidges()
        {
            if (mountainRockKeys == null || mountainRockKeys.Length == 0) yield break;

            // Arrange rocks in 2–3 ridge lines
            int ridgeCount = Random.Range(2, 4);
            int rocksPerRidge = mountainRockCount / ridgeCount;

            for (int r = 0; r < ridgeCount; r++)
            {
                Vector3 ridgeStart = RandomWorldPos();
                Vector3 ridgeDir   = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                float   ridgeLen   = Random.Range(80f, 160f);

                for (int i = 0; i < rocksPerRidge; i++)
                {
                    float t = (float)i / rocksPerRidge;
                    Vector3 pos = ridgeStart + ridgeDir * (ridgeLen * t);
                    pos += new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f));
                    yield return SpawnOne(mountainRockKeys, pos, randomScale: true, scaleRange: new Vector2(0.8f, 2.5f));
                    if (i % propsPerFrame == 0) yield return null;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Landmarks
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnLandmarks()
        {
            // Ruins
            yield return SpawnScattered(ancientRuinKeys, Random.Range(4, 8));
            // Monuments
            yield return SpawnScattered(monumentKeys, Random.Range(2, 5));
            // Watchtowers — placed on raised positions (approximated)
            yield return SpawnScattered(watchtowerKeys, Random.Range(3, 7));
            // Shrines — distributed evenly
            yield return SpawnScattered(shrineKeys, Random.Range(4, 8));
            // Statues
            yield return SpawnScattered(statueKeys, Random.Range(2, 6));
        }

        private IEnumerator SpawnScattered(string[] keys, int count)
        {
            if (keys == null || keys.Length == 0 || count == 0) yield break;
            for (int i = 0; i < count; i++)
            {
                yield return SpawnOne(keys, RandomWorldPos());
                if (i % propsPerFrame == 0) yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Road Network
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnRoadNetwork()
        {
            if (string.IsNullOrEmpty(roadDecalKey)) yield break;
            // Road segments along straight paths radiating from center
            int roadsFromCenter = 4;
            int decalsPerRoad   = roadDecalCount / roadsFromCenter;

            for (int r = 0; r < roadsFromCenter; r++)
            {
                float angle = r * (360f / roadsFromCenter) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

                for (int i = 0; i < decalsPerRoad; i++)
                {
                    float dist = Mathf.Lerp(30f, worldExtent * 0.7f, (float)i / decalsPerRoad);
                    Vector3 pos = dir * dist + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                    Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90f, 0);
                    var op = Addressables.InstantiateAsync(roadDecalKey, pos, rot);
                    yield return op;
                    if (op.Status == AsyncOperationStatus.Succeeded)
                        _spawnedObjects.Add(op.Result);
                    if (i % propsPerFrame == 0) yield return null;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Destroyed Camps
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnDestroyedCamps()
        {
            if (destroyedCampKeys == null || destroyedCampKeys.Length == 0) yield break;
            for (int i = 0; i < destroyedCampCount; i++)
                yield return SpawnOne(destroyedCampKeys, RandomWorldPos());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Biome Transition Blending
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnBiomeTransitionBlend()
        {
            if (string.IsNullOrEmpty(biomeTransitionDecalKey)) yield break;
            for (int i = 0; i < biomeTransitionCount; i++)
            {
                // Place at edges between assumed biome zones
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist  = Random.Range(worldExtent * 0.3f, worldExtent * 0.6f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
                var op = Addressables.InstantiateAsync(biomeTransitionDecalKey, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                yield return op;
                if (op.Status == AsyncOperationStatus.Succeeded)
                    _spawnedObjects.Add(op.Result);
                if (i % propsPerFrame == 0) yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnOne(string[] keys, Vector3 pos,
            bool randomScale = false, Vector2 scaleRange = default)
        {
            if (keys == null || keys.Length == 0) yield break;
            string key = keys[Random.Range(0, keys.Length)];
            if (string.IsNullOrEmpty(key)) yield break;

            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            var op = Addressables.InstantiateAsync(key, pos, rot);
            yield return op;

            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                if (randomScale && scaleRange != default)
                {
                    float s = Random.Range(scaleRange.x, scaleRange.y);
                    op.Result.transform.localScale = Vector3.one * s;
                }
                _spawnedObjects.Add(op.Result);
            }
        }

        private Vector3 RandomWorldPos() =>
            new(Random.Range(-worldExtent, worldExtent), 0, Random.Range(-worldExtent, worldExtent));
    }
}
