using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Networking;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// InventoryManager — Owns the local item bag snapshot.
    ///
    /// Phase 4 items:
    ///   - Speedups: speedup_universal_1m, _1h, _3h, _8h, _24h
    ///   - AP Potions: ap_potion_small, ap_potion_medium, ap_potion_full
    ///   - Hero XP: hero_xp_small, hero_xp_medium, hero_xp_large
    ///   - Resources: resource_food_10k, resource_iron_5k, etc.
    ///
    /// Architecture:
    ///   - Backend authoritative. Items are granted server-side on march completion.
    ///   - Supports future ERC1155 migration (itemKey → NFT token).
    ///   - Refreshes every 60 seconds or on explicit RefreshNow().
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private float refreshIntervalSeconds = 60f;

        private CombatService _combatService;
        private int _kingdomId;

        public Dictionary<string, int> Items { get; private set; } = new Dictionary<string, int>();
        public bool IsLoaded { get; private set; }

        public event Action OnInventoryUpdated;

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
            yield return StartCoroutine(_combatService.GetInventory(
                _kingdomId,
                items =>
                {
                    Items    = items;
                    IsLoaded = true;
                    OnInventoryUpdated?.Invoke();
                },
                err => Debug.LogWarning($"[InventoryManager] Refresh failed: {err.message}")
            ));
        }

        public void RefreshNow() => StartCoroutine(Refresh());

        public int GetItemCount(string itemKey)
        {
            return Items.TryGetValue(itemKey, out int v) ? v : 0;
        }

        /// <summary>Friendly display name for an item key.</summary>
        public static string GetDisplayName(string itemKey)
        {
            return itemKey switch
            {
                "speedup_universal_1m"  => "1m Speedup",
                "speedup_universal_1h"  => "1h Speedup",
                "speedup_universal_3h"  => "3h Speedup",
                "speedup_universal_8h"  => "8h Speedup",
                "speedup_universal_24h" => "24h Speedup",
                "ap_potion_small"       => "AP Potion (Small)",
                "ap_potion_medium"      => "AP Potion (Medium)",
                "ap_potion_full"        => "AP Potion (Full)",
                "hero_xp_small"         => "Hero XP (Small)",
                "hero_xp_medium"        => "Hero XP (Medium)",
                "hero_xp_large"         => "Hero XP (Large)",
                _                       => itemKey,
            };
        }
    }
}
