using UnityEngine;
using System.Collections;
using EternalKingdoms.Data;

namespace EternalKingdoms.Core
{
    /// <summary>
    /// Loads and provides environment configuration.
    /// Configuration is driven by an EnvironmentConfig ScriptableObject
    /// stored in Resources/Environments/. The active environment is
    /// selected by the UNITY_ENV build define or PlayerPrefs (dev override).
    /// </summary>
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance { get; private set; }

        [Header("Config Assets")]
        [SerializeField] private EnvironmentConfig developmentConfig;
        [SerializeField] private EnvironmentConfig stagingConfig;
        [SerializeField] private EnvironmentConfig productionConfig;

        private EnvironmentConfig _activeConfig;
        private bool _initialized;

        public EnvironmentConfig ActiveConfig => _activeConfig;
        public string ApiBaseUrl => _activeConfig?.ApiBaseUrl ?? "http://localhost:3000";
        public string WebSocketUrl => _activeConfig?.WebSocketUrl ?? "ws://localhost:3000";
        public string EnvironmentName => _activeConfig?.EnvironmentName ?? "development";
        public float RequestTimeoutSeconds => _activeConfig?.RequestTimeoutSeconds ?? 15f;
        public int MaxRetries => _activeConfig?.MaxRetries ?? 3;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator Initialize()
        {
            if (_initialized) yield break;

            // Load configs from Resources if not assigned in Inspector
            if (developmentConfig == null)
                developmentConfig = UnityEngine.Resources.Load<EnvironmentConfig>("Environments/Development");
            if (stagingConfig == null)
                stagingConfig = UnityEngine.Resources.Load<EnvironmentConfig>("Environments/Staging");
            if (productionConfig == null)
                productionConfig = UnityEngine.Resources.Load<EnvironmentConfig>("Environments/Production");

            _activeConfig = SelectEnvironment();

            if (_activeConfig == null)
            {
                // Fallback: create a runtime default pointing to localhost
                _activeConfig = ScriptableObject.CreateInstance<EnvironmentConfig>();
                _activeConfig.ApiBaseUrl = "http://localhost:3000";
                _activeConfig.WebSocketUrl = "ws://localhost:3000";
                _activeConfig.EnvironmentName = "development";
                Debug.LogWarning("[ConfigManager] No EnvironmentConfig found — using localhost defaults.");
            }

            Debug.Log($"[ConfigManager] Environment: {_activeConfig.EnvironmentName} → {_activeConfig.ApiBaseUrl}");
            _initialized = true;
            yield return null;
        }

        private EnvironmentConfig SelectEnvironment()
        {
#if UNITY_ENV_PRODUCTION
            return productionConfig;
#elif UNITY_ENV_STAGING
            return stagingConfig;
#else
            // Allow runtime override via PlayerPrefs (dev QA convenience)
            string devOverride = PlayerPrefs.GetString("ek_env_override", "development");
            return devOverride switch
            {
                "production" => productionConfig,
                "staging" => stagingConfig,
                _ => developmentConfig,
            };
#endif
        }

        /// <summary>Dev-only: force a different environment at runtime without a rebuild.</summary>
        public void SetEnvironmentOverride(string envName)
        {
            PlayerPrefs.SetString("ek_env_override", envName);
            PlayerPrefs.Save();
            Debug.Log($"[ConfigManager] Environment override set to '{envName}'. Restart to apply.");
        }
    }
}
