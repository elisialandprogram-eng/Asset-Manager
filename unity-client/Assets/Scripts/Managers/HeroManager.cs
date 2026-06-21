using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Networking;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// HeroManager — Owns the local hero inventory snapshot.
    ///
    /// Phase 4 responsibilities:
    ///   - Cache heroes fetched from GET /api/heroes
    ///   - Provide hero selection for march setup
    ///   - Track leading hero for display
    ///
    /// Architecture:
    ///   - Backend authoritative. Hero XP updates are server-side on march return.
    ///   - Each march supports 0 or 1 hero.
    ///   - Supports future ERC721 migration via nftTokenId field.
    /// </summary>
    public class HeroManager : MonoBehaviour
    {
        public static HeroManager Instance { get; private set; }

        [SerializeField] private float refreshIntervalSeconds = 60f;

        private CombatService _combatService;
        private int _kingdomId;

        public List<HeroDto> Heroes { get; private set; } = new();
        public bool IsLoaded { get; private set; }

        public event Action OnHeroesUpdated;

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
            yield return StartCoroutine(_combatService.GetHeroes(
                _kingdomId,
                heroes =>
                {
                    Heroes = heroes;
                    IsLoaded = true;
                    OnHeroesUpdated?.Invoke();
                },
                err => Debug.LogWarning($"[HeroManager] Refresh failed: {err.Message}")
            ));
        }

        public void RefreshNow() => StartCoroutine(Refresh());

        public HeroDto GetLeadingHero()
        {
            return Heroes.Find(h => h.isLeading);
        }

        public HeroDto GetById(int heroId)
        {
            return Heroes.Find(h => h.id == heroId);
        }
    }

    [Serializable]
    public class HeroDto
    {
        public int id;
        public int kingdomId;
        public string assetId;
        public string name;
        public string rarity;
        public int level;
        public int experience;
        public int experienceToNext;
        public int leadershipCapacity;
        public string troopAffinity;
        public HeroStatsDto stats;
        public List<HeroSkillDto> skills;
        public bool isLeading;
        public string nftTokenId;
        public string updatedAt;
    }

    [Serializable]
    public class HeroStatsDto
    {
        public int command;
        public int attack;
        public int defense;
        public int speed;
        public int gathering;
    }

    [Serializable]
    public class HeroSkillDto
    {
        public string skillId;
        public string name;
        public string description;
        public int triggerRound;
        public string effectType;
        public float effectValue;
    }
}
