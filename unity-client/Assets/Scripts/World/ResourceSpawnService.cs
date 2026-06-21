using System;
using System.Collections;
using UnityEngine;
using EternalKingdoms.Networking;
using EternalKingdoms.World.DTOs;

namespace EternalKingdoms.World
{
    /// <summary>
    /// ResourceSpawnService — polls backend for resource and monster node data.
    ///
    /// Endpoints:
    ///   GET /api/worlds/:id/resource-nodes  (30s poll → ResourceNodeManager)
    ///   GET /api/worlds/:id/monster-nodes   (30s poll → MonsterSpawnManager)
    ///
    /// Wired by WorldSceneController after bootstrap.
    /// Events fire on every successful poll, not just on change.
    /// </summary>
    public class ResourceSpawnService : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        public event Action<ResourceNodesResponse>  OnResourceNodesRefreshed;
        public event Action<MonsterNodesResponse>   OnMonsterNodesRefreshed;

        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [SerializeField] private float _pollIntervalSeconds = 30f;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private ApiClient _api;
        private int       _worldId;
        private bool      _running;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        public void Initialize(int worldId)
        {
            _worldId = worldId;
            _api = EternalKingdoms.Networking.NetworkManager.Instance?.Api;
            _running = true;
            StartCoroutine(ResourcePollLoop());
            StartCoroutine(MonsterPollLoop());
        }

        public void Shutdown()
        {
            _running = false;
            StopAllCoroutines();
        }

        // -------------------------------------------------------------------------
        // Poll loops
        // -------------------------------------------------------------------------

        private IEnumerator ResourcePollLoop()
        {
            while (_running)
            {
                yield return FetchResourceNodes();
                yield return new WaitForSeconds(_pollIntervalSeconds);
            }
        }

        private IEnumerator MonsterPollLoop()
        {
            // Offset monster poll by 15s so both don't fire simultaneously
            yield return new WaitForSeconds(15f);
            while (_running)
            {
                yield return FetchMonsterNodes();
                yield return new WaitForSeconds(_pollIntervalSeconds);
            }
        }

        // -------------------------------------------------------------------------
        // Fetches
        // -------------------------------------------------------------------------

        public IEnumerator FetchResourceNodes()
        {
            bool done = false;
            yield return _api.Get<ResourceNodesResponse>(
                $"/api/worlds/{_worldId}/resource-nodes",
                response =>
                {
                    OnResourceNodesRefreshed?.Invoke(response);
                    done = true;
                },
                err =>
                {
                    Debug.LogWarning($"[ResourceSpawnService] Resource nodes fetch failed: {err}");
                    done = true;
                });
            while (!done) yield return null;
        }

        public IEnumerator FetchMonsterNodes()
        {
            bool done = false;
            yield return _api.Get<MonsterNodesResponse>(
                $"/api/worlds/{_worldId}/monster-nodes",
                response =>
                {
                    OnMonsterNodesRefreshed?.Invoke(response);
                    done = true;
                },
                err =>
                {
                    Debug.LogWarning($"[ResourceSpawnService] Monster nodes fetch failed: {err}");
                    done = true;
                });
            while (!done) yield return null;
        }
    }

    // -------------------------------------------------------------------------
    // MonsterNodesResponse DTO (defined here to avoid a separate tiny file)
    // -------------------------------------------------------------------------

    [Serializable]
    public class MonsterNodeDto
    {
        public int    id;
        public int    worldId;
        public string spawnType;
        public string spawnSubtype;
        public int    level;
        public int    tileX;
        public int    tileY;
        public int    posX;
        public int    posY;
        public string biome;
        public string status;
        public string spawnedAt;
        public string expiresAt;
    }

    [Serializable]
    public class MonsterNodesResponse
    {
        public int              worldId;
        public MonsterNodeDto[] monsters;
    }
}
