using UnityEngine;
using System;
using System.Collections;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.Networking
{
    /// <summary>
    /// Handles all world map API calls.
    ///
    /// Endpoints:
    ///   GET  /api/worlds
    ///   GET  /api/worlds/:id/map
    ///   GET  /api/worlds/:id/kingdoms
    ///   GET  /api/worlds/:id/spawns
    /// </summary>
    public class WorldService
    {
        private readonly ApiClient _api;
        public WorldService(ApiClient api) => _api = api;

        public IEnumerator GetWorlds(
            Action<WorldsResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<WorldsResponseDto>("/api/worlds", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetWorldMap(
            string worldId,
            Action<WorldMapResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<WorldMapResponseDto>($"/api/worlds/{worldId}/map", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetWorldKingdoms(
            string worldId,
            Action<WorldKingdomsResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<WorldKingdomsResponseDto>($"/api/worlds/{worldId}/kingdoms", onSuccess, onError, requireAuth: true);
        }

        public IEnumerator GetWorldSpawns(
            string worldId,
            Action<WorldSpawnsResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<WorldSpawnsResponseDto>($"/api/worlds/{worldId}/spawns", onSuccess, onError, requireAuth: true);
        }
    }
}
