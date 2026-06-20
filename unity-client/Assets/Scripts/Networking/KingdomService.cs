using UnityEngine;
using System;
using System.Collections;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.Networking
{
    /// <summary>
    /// Handles all kingdom-related API calls.
    ///
    /// Endpoints:
    ///   GET  /api/kingdoms/mine
    ///   GET  /api/kingdoms/:id/state
    ///   GET  /api/kingdoms/:id/buildings
    ///   GET  /api/kingdoms/:id/resources
    ///   GET  /api/kingdoms/:id/queue
    ///   GET  /api/kingdoms/:id/construction-queue
    ///   GET  /api/kingdoms/:id/construction-options
    ///   POST /api/kingdoms/:id/construct
    ///   POST /api/buildings/:id/upgrade
    ///   GET  /api/buildings/:id/upgrade-preview
    /// </summary>
    public class KingdomService
    {
        private readonly ApiClient _api;
        public KingdomService(ApiClient api) => _api = api;

        public IEnumerator GetMyKingdom(
            Action<KingdomDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<KingdomDto>("/api/kingdoms/mine", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetKingdomState(
            string kingdomId,
            Action<KingdomStateDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<KingdomStateDto>($"/api/kingdoms/{kingdomId}/state", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetBuildings(
            string kingdomId,
            Action<BuildingsResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<BuildingsResponseDto>($"/api/kingdoms/{kingdomId}/buildings", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetResources(
            string kingdomId,
            Action<ResourcesResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<ResourcesResponseDto>($"/api/kingdoms/{kingdomId}/resources", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetUpgradeQueue(
            string kingdomId,
            Action<UpgradeQueueResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<UpgradeQueueResponseDto>($"/api/kingdoms/{kingdomId}/queue", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetConstructionQueue(
            string kingdomId,
            Action<ConstructionQueueResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<ConstructionQueueResponseDto>($"/api/kingdoms/{kingdomId}/construction-queue", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetConstructionOptions(
            string kingdomId,
            Action<ConstructionOptionsResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<ConstructionOptionsResponseDto>($"/api/kingdoms/{kingdomId}/construction-options", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator Construct(
            string kingdomId,
            string buildingType,
            Action<ConstructResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            var body = new ConstructRequestDto { buildingType = buildingType };
            yield return _api.Post<ConstructResponseDto>($"/api/kingdoms/{kingdomId}/construct", body, onSuccess, onError, requireAuth: true);
        }

        public IEnumerator UpgradeBuilding(
            string buildingId,
            Action<UpgradeResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Post<UpgradeResponseDto>($"/api/buildings/{buildingId}/upgrade", null, onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetUpgradePreview(
            string buildingId,
            Action<UpgradePreviewDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<UpgradePreviewDto>($"/api/buildings/{buildingId}/upgrade-preview", onSuccess, onError, requireAuth: true);
        }
    }
}
