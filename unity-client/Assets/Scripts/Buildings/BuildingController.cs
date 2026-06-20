using UnityEngine;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.Data;

namespace EternalKingdoms.Buildings
{
    /// <summary>
    /// Per-building behaviour component.
    /// Attached to instantiated building prefabs inside the Kingdom scene.
    ///
    /// Drives:
    /// - Addressable mesh swap based on level (future — swap to level-specific LOD)
    /// - Under-construction visual state (scaffolding overlay)
    /// - Click → detail panel routing
    ///
    /// Data flow:
    ///   KingdomStateManager.OnStateRefreshed → BuildingSlot.ApplyBuildingData
    ///   → BuildingController.Refresh(dto)
    /// </summary>
    public class BuildingController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BuildingData buildingData;

        [Header("Visual States")]
        [SerializeField] private GameObject constructionScaffolding;
        [SerializeField] private Renderer[] buildingRenderers;

        private BuildingDto _dto;

        public BuildingDto Data => _dto;
        public BuildingData Config => buildingData;

        public void Refresh(BuildingDto dto)
        {
            _dto = dto;

            bool underConstruction = dto?.isConstructing ?? false;
            if (constructionScaffolding != null)
                constructionScaffolding.SetActive(underConstruction);

            // Renderers visible only when not under initial construction
            foreach (var r in buildingRenderers)
                if (r != null) r.enabled = !underConstruction || (dto?.level ?? 0) > 0;
        }
    }
}
