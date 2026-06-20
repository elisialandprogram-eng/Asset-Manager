using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using EternalKingdoms.Authentication;

namespace EternalKingdoms.Core
{
    /// <summary>
    /// Owns all scene transitions. No other system may call
    /// SceneManager.LoadScene directly — all loads go through here
    /// so loading screens, state tracking, and analytics hooks fire correctly.
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        public static SceneController Instance { get; private set; }

        // Scene name constants — must match entries in Build Settings
        public const string SCENE_BOOTSTRAP = "Bootstrap";
        public const string SCENE_LOGIN = "Login";
        public const string SCENE_LOADING = "Loading";
        public const string SCENE_WORLD = "World";
        public const string SCENE_KINGDOM = "Kingdom";

        private bool _isTransitioning;
        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Called by BootstrapManager after all managers are ready ──────────

        public void RouteFromBootstrap()
        {
            if (AuthManager.Instance.IsAuthenticated)
            {
                Debug.Log("[SceneController] Valid token found — routing to World.");
                GameManager.Instance.SetState(GameState.World);
                LoadScene(SCENE_WORLD);
            }
            else
            {
                Debug.Log("[SceneController] No valid token — routing to Login.");
                GameManager.Instance.SetState(GameState.Login);
                LoadScene(SCENE_LOGIN);
            }
        }

        // ── Public transition API ─────────────────────────────────────────────

        public void GoToLogin()
        {
            GameManager.Instance.SetState(GameState.Login);
            LoadScene(SCENE_LOGIN);
        }

        public void GoToWorld()
        {
            GameManager.Instance.SetState(GameState.World);
            LoadScene(SCENE_WORLD);
        }

        public void GoToKingdom(string kingdomId)
        {
            GameManager.Instance.SetState(GameState.Kingdom);
            PlayerPrefs.SetString("ek_target_kingdom", kingdomId);
            LoadScene(SCENE_KINGDOM);
        }

        /// <summary>Navigate to Kingdom scene using the kingdom ID already stored in SaveManager.</summary>
        public void GoToKingdom()
        {
            GameManager.Instance.SetState(GameState.Kingdom);
            LoadScene(SCENE_KINGDOM);
        }

        // ── Internal loader ───────────────────────────────────────────────────

        private void LoadScene(string sceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[SceneController] Already transitioning — ignoring request to load {sceneName}.");
                return;
            }
            StartCoroutine(LoadAsync(sceneName));
        }

        private IEnumerator LoadAsync(string sceneName)
        {
            _isTransitioning = true;
            Debug.Log($"[SceneController] Loading scene: {sceneName}");

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                yield return null;

            op.allowSceneActivation = true;

            while (!op.isDone)
                yield return null;

            _isTransitioning = false;
            Debug.Log($"[SceneController] Scene ready: {sceneName}");
        }
    }
}
