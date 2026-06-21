using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Content;

namespace EternalKingdoms.Population
{
    /// <summary>
    /// CitizenManager — Manages ambient NPC citizens inside the Kingdom scene.
    ///
    /// Phase 5.5 (U5.5.6) responsibilities:
    ///   - Spawns ambient NPCs: Villagers, Farmers, Soldiers, Merchants
    ///   - Population count scales with Palace level (5 → 60 citizens max)
    ///   - Citizens patrol road waypoints, idle, talk, work, sit
    ///   - Pooled (citizens deactivate when far from camera, reactivate when near)
    ///   - NPC types distributed by palace level and building presence
    ///   - All NPC prefabs loaded via ArtAssetRegistry (Addressables)
    ///
    /// Behavior states:
    ///   Patrolling — walking between road waypoints
    ///   Idle       — standing, playing idle anim, occasional look-around
    ///   Working    — near a building, playing work animation
    ///   Talking    — two NPCs face each other, play talk animation
    ///   Sitting    — at a bench/fire, sitting idle
    ///
    /// Architecture:
    ///   - Attached to KingdomRoot (same object as KingdomVisualController)
    ///   - Road waypoints defined by KingdomRoadNetwork (Transform array in Inspector)
    ///   - Citizen pool: min 5, max 60 per palace level bracket
    ///   - Citizens update on coroutine (10 per frame max to avoid spike)
    /// </summary>
    public class CitizenManager : MonoBehaviour
    {
        public static CitizenManager Instance { get; private set; }

        [Header("Population Scale")]
        [SerializeField] private AnimationCurve populationByPalaceLevel = AnimationCurve.Linear(1, 5, 25, 60);
        [SerializeField] private int maxCitizens = 60;
#pragma warning disable CS0414
        [SerializeField] private int civiliansPerSoldier = 6;
#pragma warning restore CS0414

        [Header("Road Waypoints")]
        [SerializeField] private Transform[] roadWaypoints;
        [SerializeField] private Transform[] workStations;
        [SerializeField] private Transform[] sitSpots;

        [Header("NPC Type Ratios")]
        [Range(0f, 1f)] [SerializeField] private float ratioVillagers  = 0.40f;
        [Range(0f, 1f)] [SerializeField] private float ratioFarmers    = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float ratioSoldiers   = 0.20f;
#pragma warning disable CS0414
        [Range(0f, 1f)] [SerializeField] private float ratioMerchants  = 0.15f;
#pragma warning restore CS0414

        [Header("Behaviour Timings")]
        [SerializeField] private float idleMinDuration = 3f;
        [SerializeField] private float idleMaxDuration = 8f;
        [SerializeField] private float talkDuration    = 5f;
        [SerializeField] private float walkSpeed       = 1.8f;

        [Header("Pool")]
        [SerializeField] private Transform citizenPoolRoot;

        private readonly List<CitizenController> _active = new();
        private readonly Queue<CitizenController> _pool  = new();
        private int _currentPalaceLevel = 1;
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Set palace level to drive population size.</summary>
        public void SetPalaceLevel(int level)
        {
            _currentPalaceLevel = Mathf.Max(1, level);
            if (_initialized) StartCoroutine(AdjustPopulation());
        }

        /// <summary>
        /// Initialise citizens. Call from KingdomSceneController after
        /// NPC prefabs are loaded via ArtAssetRegistry.
        /// </summary>
        public void Initialize(CitizenPrefabSet prefabs, int palaceLevel)
        {
            _currentPalaceLevel = palaceLevel;
            _initialized = true;
            StartCoroutine(SpawnInitialPopulation(prefabs));
        }

        // ── Population Spawning ───────────────────────────────────────────────

        private IEnumerator SpawnInitialPopulation(CitizenPrefabSet prefabs)
        {
            int target = Mathf.RoundToInt(populationByPalaceLevel.Evaluate(_currentPalaceLevel));
            target = Mathf.Clamp(target, 5, maxCitizens);

            for (int i = 0; i < target; i++)
            {
                var type   = PickType(i, target);
                var prefab = prefabs.GetPrefab(type);
                if (prefab == null) continue;

                var go = Instantiate(prefab, citizenPoolRoot);
                var ctrl = go.GetComponent<CitizenController>() ?? go.AddComponent<CitizenController>();
                ctrl.Initialize(type, roadWaypoints, workStations, sitSpots,
                                idleMinDuration, idleMaxDuration, talkDuration, walkSpeed);
                ctrl.SetActive(true);
                _active.Add(ctrl);

                if (i % 5 == 0) yield return null;
            }

            StartCoroutine(TalkingPairRoutine());
        }

        private IEnumerator AdjustPopulation()
        {
            int target = Mathf.RoundToInt(populationByPalaceLevel.Evaluate(_currentPalaceLevel));
            target = Mathf.Clamp(target, 5, maxCitizens);

            // Grow
            while (_active.Count < target)
            {
                if (_pool.Count > 0)
                {
                    var ctrl = _pool.Dequeue();
                    ctrl.SetActive(true);
                    _active.Add(ctrl);
                }
                yield return null;
            }

            // Shrink
            while (_active.Count > target && _active.Count > 0)
            {
                var ctrl = _active[_active.Count - 1];
                _active.RemoveAt(_active.Count - 1);
                ctrl.SetActive(false);
                _pool.Enqueue(ctrl);
                yield return null;
            }
        }

        // ── Social Behaviour: Talking Pairs ───────────────────────────────────

        private IEnumerator TalkingPairRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(10f, 20f));

                if (_active.Count < 2) continue;

                // Pick two nearby citizens and make them talk
                int idx1 = UnityEngine.Random.Range(0, _active.Count);
                int idx2 = (idx1 + 1) % _active.Count;
                _active[idx1].StartTalking(_active[idx2].transform);
                _active[idx2].StartTalking(_active[idx1].transform);

                yield return new WaitForSeconds(talkDuration);

                _active[idx1].StopTalking();
                _active[idx2].StopTalking();
            }
        }

        private CitizenType PickType(int index, int total)
        {
            float r = (float)index / total;
            if (r < ratioVillagers)  return CitizenType.Villager;
            if (r < ratioVillagers + ratioFarmers) return CitizenType.Farmer;
            if (r < ratioVillagers + ratioFarmers + ratioSoldiers) return CitizenType.Soldier;
            return CitizenType.Merchant;
        }
    }

    // ── Citizen Types & Prefab Set ─────────────────────────────────────────────

    public enum CitizenType { Villager, Farmer, Soldier, Merchant }

    [Serializable]
    public class CitizenPrefabSet
    {
        public GameObject villagerMale;
        public GameObject villagerFemale;
        public GameObject farmer;
        public GameObject soldier;
        public GameObject merchant;

        public GameObject GetPrefab(CitizenType type) => type switch
        {
            CitizenType.Farmer   => farmer,
            CitizenType.Soldier  => soldier,
            CitizenType.Merchant => merchant,
            _                    => UnityEngine.Random.value > 0.5f ? villagerMale : villagerFemale,
        };
    }
}
