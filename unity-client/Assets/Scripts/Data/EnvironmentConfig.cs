using UnityEngine;

namespace EternalKingdoms.Data
{
    /// <summary>
    /// ScriptableObject that holds environment-specific configuration.
    ///
    /// Create three instances:
    ///   Resources/Environments/Development.asset
    ///   Resources/Environments/Staging.asset
    ///   Resources/Environments/Production.asset
    ///
    /// ConfigManager selects the active instance based on the build define
    /// UNITY_ENV_PRODUCTION / UNITY_ENV_STAGING, or a PlayerPrefs override.
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentConfig", menuName = "EternalKingdoms/Environment Config")]
    public class EnvironmentConfig : ScriptableObject
    {
        [Header("Identity")]
        public string EnvironmentName = "development";

        [Header("API")]
        [Tooltip("Base URL for the Express backend. No trailing slash.")]
        public string ApiBaseUrl = "http://localhost:3000";

        [Tooltip("WebSocket URL for Socket.IO (Phase 8).")]
        public string WebSocketUrl = "ws://localhost:3000";

        [Header("HTTP Settings")]
        [Tooltip("Request timeout in seconds.")]
        public float RequestTimeoutSeconds = 15f;

        [Tooltip("Maximum retry attempts on network error.")]
        public int MaxRetries = 3;

        [Header("Polling")]
        [Tooltip("Kingdom state poll interval in seconds.")]
        public float KingdomPollIntervalSeconds = 15f;

        [Tooltip("World map poll interval in seconds.")]
        public float WorldPollIntervalSeconds = 30f;

        [Header("Debug")]
        [Tooltip("Enable verbose API request logging.")]
        public bool VerboseLogging = false;
    }
}
