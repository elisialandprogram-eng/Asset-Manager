using UnityEngine;
using System.Collections;
using EternalKingdoms.Core;
using EternalKingdoms.Networking;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.UI.HUD;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// Manages live kingdom state for the Kingdom scene.
    ///
    /// On scene entry:
    ///   1. Fetches GET /api/kingdoms/mine to resolve current kingdom ID.
    ///   2. Fetches GET /api/kingdoms/:id/state for full game state.
    ///   3. Broadcasts state to BuildingSlot renderers and ResourceHUD.
    ///
    /// Polling: every 15 seconds. Architecture is ready for Socket.IO
    /// real-time updates — replace the poll coroutine with event handlers.
    /// </summary>
    public class KingdomStateManager : MonoBehaviour
    {
        public static KingdomStateManager Instance { get; private set; }

        [Header("Poll Settings")]
        [SerializeField] private float pollIntervalSeconds = 15f;

        [Header("Scene References")]
        [SerializeField] private ResourceHUD resourceHud;
        [SerializeField] private KingdomSceneController sceneController;

        // Cached state
        private KingdomDto _kingdom;
        private KingdomStateDto _state;
        private string _kingdomId;
        private Coroutine _pollCoroutine;

        public KingdomDto Kingdom => _kingdom;
        public KingdomStateDto State => _state;
        public bool IsLoaded => _state != null;

        // Events — subscribe in BuildingSlot, ResourceHUD, etc.
        public event System.Action<KingdomStateDto> OnStateRefreshed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Resolve kingdom ID — prefer stored value, fall back to API call
            _kingdomId = SaveManager.Instance.GetString(SaveManager.KEY_KINGDOM_ID);
            if (string.IsNullOrEmpty(_kingdomId))
                StartCoroutine(FetchMyKingdomThenState());
            else
                StartCoroutine(FetchState(_kingdomId));
        }

        private void OnDestroy()
        {
            if (_pollCoroutine != null)
                StopCoroutine(_pollCoroutine);
            Instance = null;
        }

        // ── Initial load ──────────────────────────────────────────────────────

        private IEnumerator FetchMyKingdomThenState()
        {
            var service = new KingdomService(NetworkManager.Instance.Api);
            bool done = false;

            yield return service.GetMyKingdom(
                onSuccess: (k) =>
                {
                    _kingdom = k;
                    _kingdomId = k.id;
                    SaveManager.Instance.SetString(SaveManager.KEY_KINGDOM_ID, k.id);
                    done = true;
                },
                onError: (err) =>
                {
                    Debug.LogError($"[KingdomStateManager] Failed to get kingdom: {err}");
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);

            if (!string.IsNullOrEmpty(_kingdomId))
                yield return FetchState(_kingdomId);
        }

        private IEnumerator FetchState(string kingdomId)
        {
            sceneController?.ShowLoading(true);

            bool done = false;
            var service = new KingdomService(NetworkManager.Instance.Api);

            yield return service.GetKingdomState(
                kingdomId,
                onSuccess: (state) =>
                {
                    _state = state;
                    _kingdom = state.kingdom;
                    done = true;
                },
                onError: (err) =>
                {
                    Debug.LogError($"[KingdomStateManager] State fetch failed: {err}");
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);
            sceneController?.ShowLoading(false);

            if (_state != null)
            {
                BroadcastState();
                if (_pollCoroutine == null)
                    _pollCoroutine = StartCoroutine(PollLoop());
            }
        }

        // ── Poll loop ─────────────────────────────────────────────────────────

        private IEnumerator PollLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(pollIntervalSeconds);
                if (string.IsNullOrEmpty(_kingdomId)) continue;

                var service = new KingdomService(NetworkManager.Instance.Api);
                yield return service.GetKingdomState(
                    _kingdomId,
                    onSuccess: (state) =>
                    {
                        _state = state;
                        BroadcastState();
                    },
                    onError: (err) => Debug.LogWarning($"[KingdomStateManager] Poll error: {err}")
                );
            }
        }

        // ── Broadcast ─────────────────────────────────────────────────────────

        private void BroadcastState()
        {
            resourceHud?.Refresh(_state.resources, _state.productionRates, _state.resourceCaps);
            OnStateRefreshed?.Invoke(_state);
            Debug.Log($"[KingdomStateManager] State refreshed — {_state.buildings?.Length ?? 0} buildings, power {_state.kingdom?.power}");
        }

        // ── Public trigger (force-refresh from UI) ────────────────────────────

        public void ForceRefresh()
        {
            if (!string.IsNullOrEmpty(_kingdomId))
                StartCoroutine(FetchState(_kingdomId));
        }
    }
}
