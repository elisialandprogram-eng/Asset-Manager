using System;
using System.Collections.Generic;
using UnityEngine;

namespace EternalKingdoms.World
{
    /// <summary>
    /// MonsterSpawnManager — manages the lifecycle of world_spawns with
    /// spawnType="monster" on the world map.
    ///
    /// Phase 3 scope: infrastructure only.
    ///   - Receives monster node data from ResourceSpawnService polls.
    ///   - Tracks which monsters are active, expired, or depleted.
    ///   - Fires OnMonsterListChanged for MonsterEntity (Phase 2) to reconcile.
    ///   - Respawn / despawn logic is server-authoritative; Unity observes only.
    ///
    /// Phase 4: will drive combat UI trigger (attack march selection).
    /// Phase 5: will integrate with combat processor and casualty reports.
    ///
    /// Spawn density is zone-driven on the server (WORLD_ARCHITECTURE_BIBLE §3):
    ///   Zone 0: none | Zone 1–2: T4–T6 | Zone 3–4: T2–T4 | Zone 5–7: T1–T2
    /// Land NFT integration: landDevelopmentLevel will scale spawn level (Phase 10).
    /// </summary>
    public class MonsterSpawnManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        public event Action<List<MonsterNodeDto>> OnMonsterListChanged;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private readonly Dictionary<int, MonsterNodeDto> _activeMonsters = new();
        private ResourceSpawnService _spawnService;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        public void Initialize(ResourceSpawnService spawnService)
        {
            _spawnService = spawnService;
            _spawnService.OnMonsterNodesRefreshed += HandleMonsterPoll;
        }

        public void Shutdown()
        {
            if (_spawnService != null)
                _spawnService.OnMonsterNodesRefreshed -= HandleMonsterPoll;
            _activeMonsters.Clear();
        }

        // -------------------------------------------------------------------------
        // Poll handler
        // -------------------------------------------------------------------------

        private void HandleMonsterPoll(MonsterNodesResponse response)
        {
            if (response?.monsters == null) return;

            var freshIds = new HashSet<int>();
            bool changed = false;

            foreach (var dto in response.monsters)
            {
                freshIds.Add(dto.id);
                if (!_activeMonsters.ContainsKey(dto.id))
                {
                    _activeMonsters[dto.id] = dto;
                    changed = true;
                }
            }

            // Remove despawned monsters
            var toRemove = new List<int>();
            foreach (var id in _activeMonsters.Keys)
                if (!freshIds.Contains(id))
                    toRemove.Add(id);

            foreach (var id in toRemove)
            {
                _activeMonsters.Remove(id);
                changed = true;
            }

            if (changed)
                OnMonsterListChanged?.Invoke(new List<MonsterNodeDto>(_activeMonsters.Values));
        }

        // -------------------------------------------------------------------------
        // Queries
        // -------------------------------------------------------------------------

        public IReadOnlyCollection<MonsterNodeDto> GetActiveMonsters() =>
            _activeMonsters.Values;

        public MonsterNodeDto GetMonster(int id) =>
            _activeMonsters.TryGetValue(id, out var m) ? m : null;

        /// Returns tier of the spawn subtype (used for colouring MonsterEntity).
        /// Monster tiers map: bandit=1, dire_wolf=2, ogre=3, guardian=4, dragon=5+
        public static int GetTierForSubtype(string subtype) => subtype switch
        {
            "bandit"    => 1,
            "dire_wolf" => 2,
            "ogre"      => 3,
            "guardian"  => 4,
            "dragon"    => 5,
            _           => 1,
        };

        /// Zone 0–7 default spawn subtypes (mirrors WORLD_ARCHITECTURE_BIBLE §3).
        public static string[] DefaultSubtypesForZone(int zone) => zone switch
        {
            0 => Array.Empty<string>(),
            1 => new[] { "dragon",    "guardian" },
            2 => new[] { "guardian",  "ogre"     },
            3 => new[] { "ogre",      "dire_wolf" },
            4 => new[] { "dire_wolf", "ogre"     },
            5 => new[] { "dire_wolf", "bandit"   },
            6 => new[] { "bandit"                 },
            7 => new[] { "bandit",    "dire_wolf" },
            _ => new[] { "bandit"                 },
        };
    }
}
