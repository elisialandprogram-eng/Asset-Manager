using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Networking;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// TroopManager — Owns the local T1-T5 troop inventory snapshot.
    ///
    /// Phase 4 responsibilities:
    ///   - Cache troop inventory fetched from GET /api/troops
    ///   - Provide read access to other systems (MonsterAttackPanel, etc.)
    ///   - Validate troop counts before allowing a march to be sent
    ///   - Update local cache after march deduction / march return
    ///
    /// Architecture:
    ///   - Backend authoritative. Local cache is read-only display.
    ///   - All writes go through POST /api/monsters/:spawnId/attack.
    ///   - Refresh every 30 seconds or on explicit RefreshNow().
    /// </summary>
    public class TroopManager : MonoBehaviour
    {
        public static TroopManager Instance { get; private set; }

        [Header("Refresh")]
        [SerializeField] private float refreshIntervalSeconds = 30f;

        private CombatService _combatService;
        private int _kingdomId;

        public Dictionary<string, int> TroopCounts { get; private set; } = new();
        public List<TroopDefinition> TroopDefinitions { get; private set; } = new();
        public bool IsLoaded { get; private set; }

        public event Action OnTroopsUpdated;

        private Coroutine _refreshCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(CombatService combatService, int kingdomId)
        {
            _combatService = combatService;
            _kingdomId = kingdomId;

            if (_refreshCoroutine != null) StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = StartCoroutine(RefreshLoop());

            StartCoroutine(_combatService.GetTroopDefinitions(
                defs => { TroopDefinitions = defs; },
                err => Debug.LogWarning($"[TroopManager] Failed to load definitions: {err.Message}")
            ));
        }

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                yield return StartCoroutine(Refresh());
                yield return new WaitForSeconds(refreshIntervalSeconds);
            }
        }

        public IEnumerator Refresh()
        {
            yield return StartCoroutine(_combatService.GetTroops(
                _kingdomId,
                response =>
                {
                    TroopCounts = response;
                    IsLoaded = true;
                    OnTroopsUpdated?.Invoke();
                },
                err => Debug.LogWarning($"[TroopManager] Refresh failed: {err.Message}")
            ));
        }

        public void RefreshNow() => StartCoroutine(Refresh());

        /// <summary>
        /// Validate that the kingdom has enough of each requested troop type.
        /// </summary>
        public bool CanSendTroops(Dictionary<string, int> requested, out string failReason)
        {
            foreach (var kvp in requested)
            {
                int available = TroopCounts.TryGetValue(kvp.Key, out int v) ? v : 0;
                if (available < kvp.Value)
                {
                    failReason = $"Need {kvp.Value} {kvp.Key}, have {available}";
                    return false;
                }
            }
            failReason = string.Empty;
            return true;
        }

        /// <summary>Optimistically deduct troops from local cache after march is sent.</summary>
        public void LocalDeduct(Dictionary<string, int> deducted)
        {
            foreach (var kvp in deducted)
            {
                if (TroopCounts.ContainsKey(kvp.Key))
                    TroopCounts[kvp.Key] = Mathf.Max(0, TroopCounts[kvp.Key] - kvp.Value);
            }
            OnTroopsUpdated?.Invoke();
        }
    }

    [Serializable]
    public class TroopDefinition
    {
        public string key;
        public string @class;
        public int tier;
        public string name;
        public int baseAttack;
        public int baseDefense;
        public int baseHp;
        public float baseSpeed;
        public int loadCapacity;
        public int trainingTimeSec;
    }
}
