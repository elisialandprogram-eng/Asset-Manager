using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EternalKingdoms.World
{
    /// <summary>
    /// WorldSimulationManager — 1-second local simulation tick.
    ///
    /// Responsibilities (Phase 3):
    ///   1. Drive MarchStateMachine.Tick on all active marches.
    ///   2. Track expired resource nodes client-side (fire expiry events).
    ///   3. Trigger scheduled UI refreshes (ETA labels, progress bars).
    ///   4. Coordinate march, resource node, monster lifecycle callbacks.
    ///
    /// Architecture principles:
    ///   - SERVER IS AUTHORITATIVE. This manager only interpolates and predicts.
    ///   - All state corrections come from backend polls (MarchManager 15s,
    ///     ResourceSpawnService 30s).
    ///   - Designed for 100k+ server-side entities; client only simulates what's
    ///     currently loaded in view (streamed chunks).
    ///
    /// Future:
    ///   - Phase 5: march collision detection (UI warning only).
    ///   - Phase 6: alliance event countdown.
    ///   - Phase 7: seasonal event timers.
    ///
    /// DontDestroyOnLoad — survives scene transitions.
    /// </summary>
    public class WorldSimulationManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// Fires every simulation tick (1 second).
        public event Action<DateTime> OnSimulationTick;

        /// Fires when a resource node expires locally (before server confirms).
        public event Action<int> OnResourceNodeExpired;

        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------

        private static WorldSimulationManager _instance;
        public  static WorldSimulationManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [SerializeField] private float _tickIntervalSeconds = 1.0f;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private bool _running;
        private ResourceNodeManager  _nodeManager;
        private MonsterSpawnManager  _monsterManager;

        // Expiry tracking: nodeId → expiresAt
        private readonly Dictionary<int, DateTime> _nodeExpiryMap = new();

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        public void Initialize(
            ResourceNodeManager  nodeManager,
            MonsterSpawnManager  monsterManager)
        {
            _nodeManager    = nodeManager;
            _monsterManager = monsterManager;
            _running        = true;
            StartCoroutine(SimulationLoop());
        }

        public void Shutdown()
        {
            _running = false;
            StopAllCoroutines();
        }

        // -------------------------------------------------------------------------
        // Main loop
        // -------------------------------------------------------------------------

        private IEnumerator SimulationLoop()
        {
            while (_running)
            {
                yield return new WaitForSeconds(_tickIntervalSeconds);
                Tick();
            }
        }

        private void Tick()
        {
            var now = DateTime.UtcNow;

            // 1. Fire tick event (MarchPathVisualizer / HUD subscribe)
            OnSimulationTick?.Invoke(now);

            // 2. Check resource node expiry
            CheckNodeExpiry(now);
        }

        // -------------------------------------------------------------------------
        // Node expiry tracking
        // -------------------------------------------------------------------------

        /// Called by ResourceNodeManager when new nodes are loaded.
        public void RegisterNodeExpiry(int nodeId, DateTime expiresAt)
        {
            _nodeExpiryMap[nodeId] = expiresAt;
        }

        /// Called when a node is removed from the map.
        public void UnregisterNodeExpiry(int nodeId)
        {
            _nodeExpiryMap.Remove(nodeId);
        }

        private void CheckNodeExpiry(DateTime now)
        {
            var expired = new List<int>();
            foreach (var (id, expiresAt) in _nodeExpiryMap)
            {
                if (now >= expiresAt)
                    expired.Add(id);
            }
            foreach (var id in expired)
            {
                _nodeExpiryMap.Remove(id);
                OnResourceNodeExpired?.Invoke(id);
            }
        }

        // -------------------------------------------------------------------------
        // Future hooks (Phase 5+)
        // -------------------------------------------------------------------------

        // Phase 5: DetectMarchCollisions(marchA, marchB)
        // Phase 6: AllianceEventCountdown(eventId, endsAt)
        // Phase 7: SeasonEventTimer(seasonEnd)
    }
}
