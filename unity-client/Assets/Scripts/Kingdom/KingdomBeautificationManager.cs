using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// U5.7.3 — Kingdom Beautification Pass
    /// Procedurally populates the kingdom scene with props that make it feel like
    /// a living medieval city.  Prop density scales with Palace level.
    /// Never feels empty — guaranteed minimum prop counts at every level.
    /// </summary>
    public class KingdomBeautificationManager : MonoBehaviour
    {
        public static KingdomBeautificationManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Kingdom Root")]
        public Transform kingdomCenter;
        [Range(10f, 120f)] public float kingdomRadius = 60f;

        [Header("Palace Level (drives density)")]
        [Range(1, 10)] public int palaceLevel = 1;

        [Header("Prop Addressable Keys")]
        public string[] roadDecalKeys;
        public string[] marketPropKeys;   // stalls, awnings
        public string[] cargoPropKeys;    // crates, barrels, sacks
        public string[] wagonKeys;
        public string[] fenceKeys;
        public string[] gardenKeys;
        public string[] treeKeys;
        public string[] statueKeys;
        public string[] fountainKeys;
        public string[] campfireKeys;
        public string[] trainingDummyKeys;

        [Header("Prop Counts — Base (Level 1) / Max (Level 10)")]
        public Vector2Int roadDecalRange     = new(6, 20);
        public Vector2Int marketPropRange    = new(2, 12);
        public Vector2Int cargoPropRange     = new(4, 18);
        public Vector2Int wagonRange         = new(1, 5);
        public Vector2Int fenceRange         = new(3, 12);
        public Vector2Int gardenRange        = new(1, 6);
        public Vector2Int treeRange          = new(4, 20);
        public Vector2Int statueRange        = new(0, 3);
        public Vector2Int fountainRange      = new(0, 2);
        public Vector2Int campfireRange      = new(1, 5);
        public Vector2Int trainingDummyRange = new(1, 4);

        [Header("Exclusion")]
        [Tooltip("Props do not spawn within this radius of the palace centre.")]
        public float palaceClearance = 12f;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawnedProps = new();
        private bool _populated;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => StartCoroutine(PopulateKingdom());

        // ─────────────────────────────────────────────────────────────────────
        //  Population
        // ─────────────────────────────────────────────────────────────────────
        public IEnumerator PopulateKingdom()
        {
            if (_populated) yield break;
            _populated = true;

            float t = Mathf.InverseLerp(1f, 10f, palaceLevel);

            yield return SpawnPropBatch(roadDecalKeys,      Lerp(roadDecalRange, t));
            yield return SpawnPropBatch(marketPropKeys,     Lerp(marketPropRange, t));
            yield return SpawnPropBatch(cargoPropKeys,      Lerp(cargoPropRange, t));
            yield return SpawnPropBatch(wagonKeys,          Lerp(wagonRange, t));
            yield return SpawnPropBatch(fenceKeys,          Lerp(fenceRange, t));
            yield return SpawnPropBatch(gardenKeys,         Lerp(gardenRange, t));
            yield return SpawnPropBatch(treeKeys,           Lerp(treeRange, t));
            yield return SpawnPropBatch(statueKeys,         Lerp(statueRange, t));
            yield return SpawnPropBatch(fountainKeys,       Lerp(fountainRange, t));
            yield return SpawnPropBatch(campfireKeys,       Lerp(campfireRange, t));
            yield return SpawnPropBatch(trainingDummyKeys,  Lerp(trainingDummyRange, t));

            Debug.Log($"[KingdomBeautificationManager] ✅ Kingdom populated with {_spawnedProps.Count} props (Palace L{palaceLevel}).");
        }

        /// <summary>Call when Palace upgrades to refresh prop density.</summary>
        public void OnPalaceLevelUp(int newLevel)
        {
            palaceLevel = newLevel;
            // Clear and repopulate
            foreach (var p in _spawnedProps) if (p != null) Addressables.ReleaseInstance(p);
            _spawnedProps.Clear();
            _populated = false;
            StartCoroutine(PopulateKingdom());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Spawning
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SpawnPropBatch(string[] keys, int count)
        {
            if (keys == null || keys.Length == 0 || count == 0) yield break;

            int spawned = 0;
            int maxAttempts = count * 5;

            for (int attempt = 0; attempt < maxAttempts && spawned < count; attempt++)
            {
                Vector3 pos = RandomKingdomPosition();
                if (!IsValidPropPosition(pos)) continue;

                string key = keys[Random.Range(0, keys.Length)];
                if (string.IsNullOrEmpty(key)) continue;

                var op = Addressables.InstantiateAsync(key, pos, RandomYRotation());
                yield return op;

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    _spawnedProps.Add(op.Result);
                    spawned++;
                }

                // Yield every 5 spawns to avoid frame spikes
                if (spawned % 5 == 0) yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private Vector3 RandomKingdomPosition()
        {
            Vector2 offset = Random.insideUnitCircle * kingdomRadius;
            Vector3 center = kingdomCenter != null ? kingdomCenter.position : Vector3.zero;
            return new Vector3(center.x + offset.x, center.y, center.z + offset.y);
        }

        private bool IsValidPropPosition(Vector3 pos)
        {
            Vector3 center = kingdomCenter != null ? kingdomCenter.position : Vector3.zero;
            float dist = Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(center.x, 0, center.z));
            return dist > palaceClearance && dist < kingdomRadius;
        }

        private static Quaternion RandomYRotation() =>
            Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        private static int Lerp(Vector2Int range, float t) =>
            Mathf.RoundToInt(Mathf.Lerp(range.x, range.y, t));
    }
}
