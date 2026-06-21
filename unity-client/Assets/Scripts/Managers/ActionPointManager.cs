using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Networking;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// ActionPointManager — Tracks and displays the AP pool for the kingdom.
    ///
    /// Phase 4 rules (GAME_DESIGN_BIBLE.md §14):
    ///   - Max 200 AP base (300 with research)
    ///   - Regen: 1 AP per 6 minutes (10/hr)
    ///   - AP costs: T1 monster = 6, T2 = 12, T3 = 20, T4 = 30, T5 = 40, Ancient = 60
    ///
    /// Architecture:
    ///   - Backend authoritative. AP is deducted server-side on POST /api/monsters/:id/attack.
    ///   - Local display uses client-side regen extrapolation between refreshes.
    ///   - Refreshes from server every 60 seconds.
    /// </summary>
    public class ActionPointManager : MonoBehaviour
    {
        public static ActionPointManager Instance { get; private set; }

        [SerializeField] private float serverRefreshIntervalSeconds = 60f;

        private CombatService _combatService;
        private int _kingdomId;

        public float CurrentAP { get; private set; }
        public float MaxAP { get; private set; } = 200f;
        public float RegenPerMinute { get; private set; } = 1f / 6f;
        public bool IsLoaded { get; private set; }

        private DateTime _lastServerSync = DateTime.UtcNow;
        private float _apAtSync;

        public event Action OnAPUpdated;

        private static readonly Dictionary<string, int> s_apCostByTier = new Dictionary<string, int>
        {
            { "common",   6  },
            { "uncommon", 12 },
            { "rare",     20 },
            { "elite",    30 },
            { "boss",     40 },
            { "ancient",  60 },
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!IsLoaded) return;

            double elapsed = (DateTime.UtcNow - _lastServerSync).TotalMinutes;
            float extrapolated = _apAtSync + (float)(elapsed * RegenPerMinute);
            CurrentAP = Mathf.Min(extrapolated, MaxAP);
        }

        public void Initialize(CombatService combatService, int kingdomId)
        {
            _combatService = combatService;
            _kingdomId = kingdomId;
            StartCoroutine(RefreshLoop());
        }

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                yield return StartCoroutine(RefreshFromServer());
                yield return new WaitForSeconds(serverRefreshIntervalSeconds);
            }
        }

        public IEnumerator RefreshFromServer()
        {
            yield return StartCoroutine(_combatService.GetActionPoints(
                _kingdomId,
                ap =>
                {
                    CurrentAP   = ap.currentAP;
                    MaxAP       = ap.maxAP;
                    _apAtSync   = ap.currentAP;
                    _lastServerSync = DateTime.UtcNow;
                    IsLoaded    = true;
                    OnAPUpdated?.Invoke();
                },
                err => Debug.LogWarning($"[ActionPointManager] Refresh failed: {err.Message}")
            ));
        }

        public static int GetApCost(string monsterTier)
        {
            return s_apCostByTier.TryGetValue(monsterTier, out int cost) ? cost : 6;
        }

        public bool CanAfford(string monsterTier) => CurrentAP >= GetApCost(monsterTier);

        public void LocalDeduct(int amount)
        {
            _apAtSync       = Mathf.Max(0f, CurrentAP - amount);
            _lastServerSync = DateTime.UtcNow;
            CurrentAP       = _apAtSync;
            OnAPUpdated?.Invoke();
        }
    }
}
