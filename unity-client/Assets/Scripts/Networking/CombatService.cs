using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Managers;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.Networking
{
    /// <summary>
    /// CombatService — All Phase 4 API calls for the PvE combat system.
    ///
    /// Endpoints:
    ///   GET  /api/heroes?kingdomId=X
    ///   GET  /api/heroes/:id
    ///   GET  /api/troops?kingdomId=X
    ///   GET  /api/troops/definitions
    ///   GET  /api/hospital?kingdomId=X
    ///   POST /api/hospital/heal
    ///   GET  /api/reports?kingdomId=X
    ///   GET  /api/reports/:id
    ///   GET  /api/monsters/:spawnId
    ///   POST /api/monsters/:spawnId/attack
    ///   GET  /api/kingdoms/:id/ap  (action points)
    ///
    /// Architecture:
    ///   - Uses shared ApiClient for auth headers + JSON handling.
    ///   - All callbacks: onSuccess(TData) or onError(ApiError).
    ///   - No mock data. Backend authoritative.
    /// </summary>
    public class CombatService
    {
        private readonly ApiClient _api;
        public CombatService(ApiClient api) => _api = api;

        // ── Heroes ─────────────────────────────────────────────────────────────

        public IEnumerator GetHeroes(
            int kingdomId,
            Action<List<HeroDto>> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<HeroListResponseDto>(
                $"/api/heroes?kingdomId={kingdomId}",
                r => onSuccess(r.heroes),
                onError,
                requireAuth: true
            );
        }

        public IEnumerator GetHero(
            int heroId,
            Action<HeroDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<HeroResponseDto>(
                $"/api/heroes/{heroId}",
                r => onSuccess(r.hero),
                onError,
                requireAuth: true
            );
        }

        // ── Troops ─────────────────────────────────────────────────────────────

        public IEnumerator GetTroops(
            int kingdomId,
            Action<Dictionary<string, int>> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<TroopInventoryResponseDto>(
                $"/api/troops?kingdomId={kingdomId}",
                r => onSuccess(r.troops),
                onError,
                requireAuth: true
            );
        }

        public IEnumerator GetTroopDefinitions(
            Action<List<TroopDefinition>> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<TroopDefinitionsResponseDto>(
                "/api/troops/definitions",
                r => onSuccess(r.definitions),
                onError,
                requireAuth: true
            );
        }

        // ── Hospital ───────────────────────────────────────────────────────────

        public IEnumerator GetHospital(
            int kingdomId,
            Action<HospitalStateDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<HospitalResponseDto>(
                $"/api/hospital?kingdomId={kingdomId}",
                r => onSuccess(r.hospital),
                onError,
                requireAuth: true
            );
        }

        // ── Battle Reports ─────────────────────────────────────────────────────

        public IEnumerator GetReports(
            int kingdomId,
            int limit,
            int offset,
            Action<List<BattleReportDto>, int> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<BattleReportListResponseDto>(
                $"/api/reports?kingdomId={kingdomId}&limit={limit}&offset={offset}",
                r => onSuccess(r.reports, r.total),
                onError,
                requireAuth: true
            );
        }

        public IEnumerator GetReport(
            int reportId,
            Action<BattleReportDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<BattleReportResponseDto>(
                $"/api/reports/{reportId}",
                r => onSuccess(r.report),
                onError,
                requireAuth: true
            );
        }

        // ── Monsters ───────────────────────────────────────────────────────────

        public IEnumerator GetMonsterSpawn(
            int spawnId,
            Action<MonsterSpawnDetailDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<MonsterSpawnDetailDto>(
                $"/api/monsters/{spawnId}",
                onSuccess,
                onError,
                requireAuth: true
            );
        }

        public IEnumerator AttackMonster(
            int spawnId,
            AttackMarchRequestDto request,
            Action<AttackMarchResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Post<AttackMarchResponseDto>(
                $"/api/monsters/{spawnId}/attack",
                request,
                onSuccess,
                onError,
                requireAuth: true
            );
        }

        // ── Action Points ──────────────────────────────────────────────────────

        public IEnumerator GetActionPoints(
            int kingdomId,
            Action<ActionPointDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<ActionPointDto>(
                $"/api/kingdoms/{kingdomId}/ap",
                onSuccess,
                onError,
                requireAuth: true
            );
        }

        // ── Inventory ──────────────────────────────────────────────────────────

        public IEnumerator GetInventory(
            int kingdomId,
            Action<Dictionary<string, int>> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<InventoryResponseDto>(
                $"/api/inventory?kingdomId={kingdomId}",
                r => onSuccess(r.items),
                onError,
                requireAuth: true
            );
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    [Serializable]
    public class HeroListResponseDto  { public List<HeroDto> heroes; }

    [Serializable]
    public class HeroResponseDto      { public HeroDto hero; }

    [Serializable]
    public class TroopInventoryResponseDto
    {
        public int kingdomId;
        public Dictionary<string, int> troops;
    }

    [Serializable]
    public class TroopDefinitionsResponseDto { public List<TroopDefinition> definitions; }

    [Serializable]
    public class HospitalResponseDto  { public HospitalStateDto hospital; }

    [Serializable]
    public class BattleReportListResponseDto
    {
        public List<BattleReportDto> reports;
        public int total;
        public int limit;
        public int offset;
    }

    [Serializable]
    public class BattleReportResponseDto { public BattleReportDto report; }

    [Serializable]
    public class MonsterSpawnDetailDto
    {
        public MonsterSpawnInfoDto spawn;
        public MonsterInfoDto monster;
        public int apCost;
    }

    [Serializable]
    public class MonsterSpawnInfoDto
    {
        public int id;
        public int monsterId;
        public int worldId;
        public int x;
        public int y;
        public int currentHp;
        public string respawnAt;
        public int defeatedByKingdomId;
    }

    [Serializable]
    public class MonsterInfoDto
    {
        public int id;
        public string name;
        public string tier;
        public int power;
        public int hp;
        public int attack;
        public int defense;
        public string assetId;
    }

    [Serializable]
    public class AttackMarchRequestDto
    {
        public int kingdomId;
        public int heroId;
        public Dictionary<string, int> troops;
    }

    [Serializable]
    public class AttackMarchResponseDto
    {
        public AttackMarchDto march;
    }

    [Serializable]
    public class AttackMarchDto
    {
        public int id;
        public int worldId;
        public int kingdomId;
        public string marchType;
        public string status;
        public int spawnId;
        public Dictionary<string, int> troops;
        public int heroId;
        public float speedTpm;
        public float distanceTiles;
        public string startedAt;
        public string arrivesAt;
        public string returnsAt;
        public int apCost;
        public string monsterName;
        public string monsterTier;
    }

    [Serializable]
    public class BattleReportDto
    {
        public int id;
        public int attackerKingdomId;
        public int defenderMonsterSpawnId;
        public string monsterName;
        public string monsterTier;
        public bool attackerWon;
        public int roundsFought;
        public List<CombatRoundDto> rounds;
        public Dictionary<string, int> attackerTroopsSent;
        public Dictionary<string, int> attackerTroopsKilled;
        public Dictionary<string, int> attackerTroopsWounded;
        public Dictionary<string, int> attackerTroopsSurvived;
        public RewardsDto rewardsGranted;
        public int heroId;
        public int marchId;
        public string createdAt;
    }

    [Serializable]
    public class CombatRoundDto
    {
        public int round;
        public float attackerDamageDealt;
        public float defenderDamageDealt;
        public float attackerHpAfter;
        public float defenderHpAfter;
        public Dictionary<string, int> attackerTroopsLostThisRound;
    }

    [Serializable]
    public class RewardsDto
    {
        public int food;
        public int wood;
        public int stone;
        public int iron;
        public int gold;
        public int crystal;
        public int heroXp;
        public Dictionary<string, int> items;
    }

    [Serializable]
    public class ActionPointDto
    {
        public float currentAP;
        public float maxAP;
    }

    [Serializable]
    public class InventoryResponseDto
    {
        public int kingdomId;
        public Dictionary<string, int> items;
    }
}
