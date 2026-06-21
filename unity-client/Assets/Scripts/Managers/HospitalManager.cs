using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Networking;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// HospitalManager — Tracks wounded troop counts and healing progress.
    ///
    /// Phase 4 rules (GAME_DESIGN_BIBLE.md §10):
    ///   - Field operations: 50% dead, 50% wounded
    ///   - Home defense: 100% wounded (Phase 5)
    ///   - Hospital capacity enforced — overflow = permanent death
    ///   - Healing rate: 5 troops/min per Hospital level
    ///   - Priority: T5 > T4 > T3 > T2 > T1
    ///
    /// Architecture:
    ///   - Backend authoritative. Healing is calculated server-side (lazy regen).
    ///   - Client polls GET /api/hospital every 30 seconds.
    ///   - HospitalPanel subscribes to OnHospitalUpdated for UI refresh.
    /// </summary>
    public class HospitalManager : MonoBehaviour
    {
        public static HospitalManager Instance { get; private set; }

        [SerializeField] private float refreshIntervalSeconds = 30f;

        private CombatService _combatService;
        private int _kingdomId;

        public HospitalStateDto HospitalState { get; private set; }
        public bool IsLoaded { get; private set; }

        public event Action OnHospitalUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(CombatService combatService, int kingdomId)
        {
            _combatService = combatService;
            _kingdomId     = kingdomId;
            StartCoroutine(RefreshLoop());
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
            yield return StartCoroutine(_combatService.GetHospital(
                _kingdomId,
                state =>
                {
                    HospitalState = state;
                    IsLoaded = true;
                    OnHospitalUpdated?.Invoke();
                },
                err => Debug.LogWarning($"[HospitalManager] Refresh failed: {err.Message}")
            ));
        }

        public void RefreshNow() => StartCoroutine(Refresh());

        public int TotalWounded => HospitalState?.totalWounded ?? 0;
        public int Capacity    => HospitalState?.capacity     ?? 0;
    }

    [Serializable]
    public class HospitalStateDto
    {
        public int kingdomId;
        public Dictionary<string, int> woundedTroops;
        public int totalWounded;
        public int capacity;
        public float healRatePerMinute;
        public string lastHealAt;
    }
}
