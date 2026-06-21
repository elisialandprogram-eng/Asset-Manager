using System;
using System.Collections;
using UnityEngine;
using EternalKingdoms.Networking;
using EternalKingdoms.World.DTOs;

namespace EternalKingdoms.World
{
    /// <summary>
    /// MarchService — HTTP wrapper for all march-related API endpoints.
    ///
    /// POST  /api/marches              → CreateMarch
    /// GET   /api/marches?kingdomId=X  → ListMarches
    /// DELETE /api/marches/:id         → CancelMarch
    /// GET   /api/worlds/:id/resource-nodes → GetResourceNodes
    ///
    /// All calls use ApiClient.Request (JWT injected automatically).
    /// </summary>
    public class MarchService : MonoBehaviour
    {
        private ApiClient _api;

        private void Awake()
        {
            _api = NetworkManager.Instance?.Api;
        }

        // -------------------------------------------------------------------------
        // POST /api/marches
        // -------------------------------------------------------------------------

        public IEnumerator CreateMarch(
            CreateMarchRequest request,
            Action<CreateMarchResponse> onSuccess,
            Action<string> onError)
        {
            string body = JsonUtility.ToJson(request);
            yield return _api.Post<CreateMarchResponse>(
                "/api/marches",
                body,
                onSuccess,
                err => onError?.Invoke(err.Message));
        }

        // -------------------------------------------------------------------------
        // GET /api/marches?kingdomId=X
        // -------------------------------------------------------------------------

        public IEnumerator ListMarches(
            int kingdomId,
            Action<ListMarchesResponse> onSuccess,
            Action<string> onError)
        {
            yield return _api.Get<ListMarchesResponse>(
                $"/api/marches?kingdomId={kingdomId}",
                onSuccess,
                err => onError?.Invoke(err.Message));
        }

        // -------------------------------------------------------------------------
        // DELETE /api/marches/:id
        // -------------------------------------------------------------------------

        public IEnumerator CancelMarch(
            int marchId,
            Action onSuccess,
            Action<string> onError)
        {
            yield return _api.Delete(
                $"/api/marches/{marchId}",
                _ => onSuccess?.Invoke(),
                err => onError?.Invoke(err.Message));
        }

        // -------------------------------------------------------------------------
        // GET /api/worlds/:id/resource-nodes
        // -------------------------------------------------------------------------

        public IEnumerator GetResourceNodes(
            int worldId,
            Action<ResourceNodesResponse> onSuccess,
            Action<string> onError)
        {
            yield return _api.Get<ResourceNodesResponse>(
                $"/api/worlds/{worldId}/resource-nodes",
                onSuccess,
                err => onError?.Invoke(err.Message));
        }
    }
}
