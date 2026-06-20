using UnityEngine;
using System.Collections;
using EternalKingdoms.Core;

namespace EternalKingdoms.Networking
{
    /// <summary>
    /// Top-level networking subsystem manager.
    /// Owns the ApiClient instance and monitors connectivity state.
    /// All services (AuthService, KingdomService, WorldService) use
    /// ApiClient through this manager.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public ApiClient Api { get; private set; }
        public bool IsOnline { get; private set; } = true;

        private float _connectivityCheckInterval = 30f;
        private float _nextCheckAt;
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator Initialize()
        {
            if (_initialized) yield break;

            string baseUrl = ConfigManager.Instance.ApiBaseUrl;
            float timeout = ConfigManager.Instance.RequestTimeoutSeconds;
            int maxRetries = ConfigManager.Instance.MaxRetries;

            Api = new ApiClient(baseUrl, timeout, maxRetries);
            _initialized = true;
            _nextCheckAt = Time.time + _connectivityCheckInterval;
            Debug.Log($"[NetworkManager] ApiClient ready → {baseUrl}");
        }

        private void Update()
        {
            if (!_initialized) return;
            if (Time.time >= _nextCheckAt)
            {
                _nextCheckAt = Time.time + _connectivityCheckInterval;
                StartCoroutine(PingHealthz());
            }
        }

        private IEnumerator PingHealthz()
        {
            yield return Api.Get<HealthzResponse>(
                "/api/healthz",
                onSuccess: (r) => { IsOnline = true; },
                onError: (_) => { IsOnline = false; Debug.LogWarning("[NetworkManager] Backend unreachable."); }
            );
        }

        public void Shutdown()
        {
            Api = null;
            _initialized = false;
            Debug.Log("[NetworkManager] Shutdown.");
        }

        [System.Serializable]
        private class HealthzResponse
        {
            public string status;
        }
    }
}
