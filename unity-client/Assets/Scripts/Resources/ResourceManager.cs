using UnityEngine;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.Resources
{
    /// <summary>
    /// Holds the current local resource snapshot for the authenticated player's kingdom.
    /// Updated by KingdomStateManager on every poll.
    /// UI components read from this manager for display purposes only.
    /// Authoritative values always come from the backend.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        private ResourcesDto _resources;
        private ProductionRatesDto _rates;
        private ResourceCapsDto _caps;

        public ResourcesDto Resources => _resources;
        public ProductionRatesDto Rates => _rates;
        public ResourceCapsDto Caps => _caps;

        public event System.Action OnResourcesUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Apply(ResourcesDto resources, ProductionRatesDto rates, ResourceCapsDto caps)
        {
            _resources = resources;
            _rates = rates;
            _caps = caps;
            OnResourcesUpdated?.Invoke();
        }

        // ── Convenience accessors ─────────────────────────────────────────────

        public long Food => _resources?.food ?? 0;
        public long Wood => _resources?.wood ?? 0;
        public long Stone => _resources?.stone ?? 0;
        public long Iron => _resources?.iron ?? 0;
        public long Gold => _resources?.gold ?? 0;

        public bool CanAfford(NetworkingDTOs.ResourceCostDto cost)
        {
            if (cost == null || _resources == null) return false;
            return _resources.food >= cost.food &&
                   _resources.wood >= cost.wood &&
                   _resources.stone >= cost.stone &&
                   _resources.iron >= cost.iron &&
                   _resources.gold >= cost.gold;
        }
    }
}

// Alias to avoid namespace conflict with Unity's built-in Resources class
namespace EternalKingdoms.Resources.NetworkingDTOs
{
    using EternalKingdoms.Networking.DTOs;
}
