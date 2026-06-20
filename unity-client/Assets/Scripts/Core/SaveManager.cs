using UnityEngine;
using System.Collections;

namespace EternalKingdoms.Core
{
    /// <summary>
    /// Manages persistent client-side data using PlayerPrefs.
    /// All keys are namespaced under "ek_" to avoid collisions.
    /// Sensitive data (JWT) is stored here but should be encrypted
    /// in a production release via platform-native secure storage.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        // Key constants
        public const string KEY_JWT = "ek_token";
        public const string KEY_USER_ID = "ek_user_id";
        public const string KEY_USER_EMAIL = "ek_user_email";
        public const string KEY_KINGDOM_ID = "ek_kingdom_id";
        public const string KEY_REMEMBER_ME = "ek_remember_me";
        public const string KEY_LAST_WORLD_X = "ek_last_world_x";
        public const string KEY_LAST_WORLD_Z = "ek_last_world_z";
        public const string KEY_GRAPHICS_QUALITY = "ek_gfx_quality";
        public const string KEY_AUDIO_VOLUME = "ek_audio_volume";
        public const string KEY_ENV_OVERRIDE = "ek_env_override";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator Initialize()
        {
            Debug.Log("[SaveManager] Initialized.");
            yield return null;
        }

        // ── Generic helpers ───────────────────────────────────────────────────

        public void SetString(string key, string value) { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }
        public string GetString(string key, string fallback = "") => PlayerPrefs.GetString(key, fallback);

        public void SetInt(string key, int value) { PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); }
        public int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(key, fallback);

        public void SetFloat(string key, float value) { PlayerPrefs.SetFloat(key, value); PlayerPrefs.Save(); }
        public float GetFloat(string key, float fallback = 0f) => PlayerPrefs.GetFloat(key, fallback);

        public void SetBool(string key, bool value) => SetInt(key, value ? 1 : 0);
        public bool GetBool(string key, bool fallback = false) => GetInt(key, fallback ? 1 : 0) == 1;

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }

        // ── Session helpers ───────────────────────────────────────────────────

        public void SaveSession(string jwt, string userId, string email, string kingdomId)
        {
            SetString(KEY_JWT, jwt);
            SetString(KEY_USER_ID, userId);
            SetString(KEY_USER_EMAIL, email);
            SetString(KEY_KINGDOM_ID, kingdomId);
        }

        public void ClearSession()
        {
            DeleteKey(KEY_JWT);
            DeleteKey(KEY_USER_ID);
            DeleteKey(KEY_USER_EMAIL);
            DeleteKey(KEY_KINGDOM_ID);
            Debug.Log("[SaveManager] Session cleared.");
        }

        public void SaveAll()
        {
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] Flushed PlayerPrefs.");
        }
    }
}
