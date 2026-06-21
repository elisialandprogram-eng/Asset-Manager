using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EternalKingdoms.Core;
using EternalKingdoms.World.DTOs;

namespace EternalKingdoms.World
{
    /// <summary>
    /// MarchManager — singleton that owns all march state for the local player.
    ///
    /// Responsibilities:
    ///   - Maintain the authoritative in-memory march list (refreshed from backend).
    ///   - Drive MarchStateMachine.Tick every second to advance local state.
    ///   - Spawn/destroy MarchEntity visuals via MarchEntityPool.
    ///   - Expose CreateMarch() for ResourceGatherPanel.
    ///   - Expose CancelMarch() for world UI.
    ///   - Fire events so MarchPathVisualizer and UI update reactively.
    ///
    /// DontDestroyOnLoad — survives scene transitions so marches persist across
    /// Kingdom ↔ World scene changes.
    ///
    /// Poll interval: 15 seconds (marches are time-stamped so local simulation
    /// fills the gaps between polls).
    /// </summary>
    public class MarchManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        public event Action<MarchModel> OnMarchCreated;
        public event Action<MarchModel> OnMarchStateChanged;
        public event Action<int>        OnMarchCompleted;    // marchId
        public event Action<int>        OnMarchCancelled;    // marchId
        public event Action<List<MarchModel>> OnMarchListRefreshed;

        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Poll Settings")]
        [Tooltip("How often to re-fetch march list from backend (seconds).")]
        [SerializeField] private float _pollInterval = 15f;

        [Header("Simulation")]
        [Tooltip("How often to tick the local march state machine (seconds).")]
        [SerializeField] private float _tickInterval = 1f;

        // -------------------------------------------------------------------------
        // Internal state
        // -------------------------------------------------------------------------

        private MarchService _marchService;
        private readonly Dictionary<int, MarchModel> _marches = new();
        private int  _currentKingdomId;
        private bool _initialized;

        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------

        private static MarchManager _instance;
        public  static MarchManager Instance => _instance;

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
        // Lifecycle
        // -------------------------------------------------------------------------

        /// Call this from WorldSceneController after kingdom data is loaded.
        public void Initialize(int kingdomId)
        {
            _currentKingdomId = kingdomId;
            _marchService = FindFirstObjectByType<MarchService>();
            if (_marchService == null)
            {
                var go = new GameObject("MarchService");
                DontDestroyOnLoad(go);
                _marchService = go.AddComponent<MarchService>();
            }

            _initialized = true;
            StartCoroutine(PollLoop());
            StartCoroutine(TickLoop());
        }

        private IEnumerator PollLoop()
        {
            while (_initialized)
            {
                yield return StartCoroutine(RefreshFromBackend());
                yield return new WaitForSeconds(_pollInterval);
            }
        }

        private IEnumerator TickLoop()
        {
            while (_initialized)
            {
                yield return new WaitForSeconds(_tickInterval);
                TickAllMarches();
            }
        }

        // -------------------------------------------------------------------------
        // Backend refresh
        // -------------------------------------------------------------------------

        private IEnumerator RefreshFromBackend()
        {
            bool done = false;
            yield return _marchService.ListMarches(
                _currentKingdomId,
                response =>
                {
                    MergeFromDtos(response?.marches ?? Array.Empty<MarchDto>());
                    done = true;
                },
                err =>
                {
                    Debug.LogWarning($"[MarchManager] Poll failed: {err}");
                    done = true;
                });
            while (!done) yield return null;
        }

        private void MergeFromDtos(MarchDto[] dtos)
        {
            var serverIds = new HashSet<int>();
            foreach (var dto in dtos)
            {
                serverIds.Add(dto.id);
                if (_marches.TryGetValue(dto.id, out var existing))
                {
                    // Update mutable fields from server (canonical state)
                    var updated = MarchModel.FromDto(dto);
                    _marches[dto.id] = updated;
                }
                else
                {
                    var model = MarchModel.FromDto(dto);
                    _marches[dto.id] = model;
                    OnMarchCreated?.Invoke(model);
                }
            }

            // Remove marches that are no longer on server (completed/cancelled)
            var toRemove = new List<int>();
            foreach (var id in _marches.Keys)
                if (!serverIds.Contains(id))
                    toRemove.Add(id);

            foreach (var id in toRemove)
            {
                if (_marches.TryGetValue(id, out var m) && m.State == MarchState.Completed)
                    OnMarchCompleted?.Invoke(id);
                _marches.Remove(id);
            }

            OnMarchListRefreshed?.Invoke(new List<MarchModel>(_marches.Values));
        }

        // -------------------------------------------------------------------------
        // Local simulation tick
        // -------------------------------------------------------------------------

        private void TickAllMarches()
        {
            var now = DateTime.UtcNow;
            foreach (var march in _marches.Values)
            {
                bool changed = MarchStateMachine.Tick(march, now);
                if (changed)
                    OnMarchStateChanged?.Invoke(march);
            }
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        public IEnumerator CreateMarch(
            int spawnId, int worldId,
            TroopLoad troops,
            Action<MarchModel> onSuccess,
            Action<string> onError)
        {
            var request = new CreateMarchRequest
            {
                kingdomId = _currentKingdomId,
                worldId   = worldId,
                spawnId   = spawnId,
                troops    = troops,
            };

            bool done = false;
            yield return _marchService.CreateMarch(
                request,
                response =>
                {
                    var model = MarchModel.FromDto(response.march);
                    _marches[model.Id] = model;
                    OnMarchCreated?.Invoke(model);
                    onSuccess?.Invoke(model);
                    done = true;
                },
                err =>
                {
                    onError?.Invoke(err);
                    done = true;
                });
            while (!done) yield return null;
        }

        public IEnumerator CancelMarch(
            int marchId,
            Action onSuccess,
            Action<string> onError)
        {
            bool done = false;
            yield return _marchService.CancelMarch(
                marchId,
                () =>
                {
                    _marches.Remove(marchId);
                    OnMarchCancelled?.Invoke(marchId);
                    onSuccess?.Invoke();
                    done = true;
                },
                err =>
                {
                    onError?.Invoke(err);
                    done = true;
                });
            while (!done) yield return null;
        }

        public IReadOnlyCollection<MarchModel> GetActiveMarches() =>
            _marches.Values;

        public MarchModel GetMarch(int id) =>
            _marches.TryGetValue(id, out var m) ? m : null;

        // -------------------------------------------------------------------------
        // Cleanup
        // -------------------------------------------------------------------------

        public void Shutdown()
        {
            _initialized = false;
            StopAllCoroutines();
            _marches.Clear();
        }
    }
}
