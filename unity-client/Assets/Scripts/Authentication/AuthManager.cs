using UnityEngine;
using System;
using System.Collections;
using EternalKingdoms.Core;
using EternalKingdoms.Networking;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.Authentication
{
    /// <summary>
    /// Manages JWT authentication lifecycle.
    ///
    /// Responsibilities:
    /// - Login() / Logout()
    /// - ValidateToken() — checks stored token on bootstrap
    /// - GetCurrentUser() — cached user profile
    /// - HandleUnauthorized() — called by ApiClient on 401
    ///
    /// JWT storage: PlayerPrefs key "ek_token" (via SaveManager)
    /// Token is injected into ApiClient on every authenticate call.
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        private UserDto _currentUser;
        private string _token;
        private bool _initialized;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_token) && _currentUser != null;
        public UserDto CurrentUser => _currentUser;
        public string Token => _token;

        // Events
        public event Action OnLoginSuccess;
        public event Action OnLogout;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Bootstrap ─────────────────────────────────────────────────────────

        public IEnumerator Initialize()
        {
            if (_initialized) yield break;

            string storedToken = SaveManager.Instance.GetString(SaveManager.KEY_JWT);
            if (!string.IsNullOrEmpty(storedToken))
            {
                Debug.Log("[AuthManager] Found stored token — validating...");
                yield return ValidateToken(storedToken);
            }
            else
            {
                Debug.Log("[AuthManager] No stored token.");
            }
            _initialized = true;
        }

        // ── Login ─────────────────────────────────────────────────────────────

        public IEnumerator Login(
            string email,
            string password,
            Action<UserDto> onSuccess,
            Action<string> onError)
        {
            var authService = new AuthService(NetworkManager.Instance.Api);
            bool done = false;

            yield return authService.Login(
                email, password,
                onSuccess: (resp) =>
                {
                    ApplySession(resp.token, resp.user);
                    Debug.Log($"[AuthManager] Login OK — {resp.user.email}");
                    onSuccess?.Invoke(resp.user);
                    OnLoginSuccess?.Invoke();
                    done = true;
                },
                onError: (err) =>
                {
                    Debug.LogWarning($"[AuthManager] Login failed: {err.Message}");
                    onError?.Invoke(err.Message);
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);
        }

        // ── Logout ────────────────────────────────────────────────────────────

        public void Logout()
        {
            Debug.Log("[AuthManager] Logging out.");
            ClearSession();
            OnLogout?.Invoke();
            SceneController.Instance.GoToLogin();
        }

        // ── Token Validation ──────────────────────────────────────────────────

        private IEnumerator ValidateToken(string token)
        {
            // Inject token speculatively so the /me call works
            NetworkManager.Instance.Api.SetBearerToken(token);

            var authService = new AuthService(NetworkManager.Instance.Api);
            bool done = false;

            yield return authService.GetMe(
                onSuccess: (resp) =>
                {
                    _token = token;
                    _currentUser = resp.user;
                    Debug.Log($"[AuthManager] Token valid — {resp.user.email}");
                    done = true;
                },
                onError: (err) =>
                {
                    Debug.LogWarning($"[AuthManager] Token invalid ({err.StatusCode}) — clearing.");
                    ClearSession();
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);
        }

        // ── GetCurrentUser ────────────────────────────────────────────────────

        public IEnumerator RefreshCurrentUser(Action<UserDto> onComplete = null)
        {
            var authService = new AuthService(NetworkManager.Instance.Api);
            yield return authService.GetMe(
                onSuccess: (resp) =>
                {
                    _currentUser = resp.user;
                    onComplete?.Invoke(resp.user);
                },
                onError: (_) => onComplete?.Invoke(null)
            );
        }

        // ── 401 Handler (called by ApiClient) ────────────────────────────────

        public void HandleUnauthorized()
        {
            Debug.LogWarning("[AuthManager] 401 received — forcing logout.");
            ClearSession();
            OnLogout?.Invoke();
            if (SceneController.Instance != null)
                SceneController.Instance.GoToLogin();
        }

        // ── JS Bridge — called via SendMessage("AuthManager", "ReceiveAuthToken", token) ──

        /// <summary>
        /// Entry point for the React-to-Unity auth handshake.
        /// The host page calls:
        ///   unityInstance.SendMessage("AuthManager", "ReceiveAuthToken", jwtString)
        /// This method validates the token against /api/auth/me and fires
        /// OnLoginSuccess if valid, so RuntimeBootstrap can route to World.
        /// The GameObject hosting this component MUST be named "AuthManager"
        /// (RuntimeBootstrap.EnsureManager enforces this).
        /// </summary>
        public void ReceiveAuthToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[AuthManager] ReceiveAuthToken called with empty token — ignored.");
                return;
            }

            Debug.Log("[AuthManager] Received auth token from JS bridge — validating...");
            SaveManager.Instance?.SetString(SaveManager.KEY_JWT, token);
            StartCoroutine(ValidateReceivedToken(token));
        }

        private IEnumerator ValidateReceivedToken(string token)
        {
            yield return ValidateToken(token);

            if (IsAuthenticated)
            {
                Debug.Log("[AuthManager] JS-bridged token validated successfully.");
                _initialized = true;
                OnLoginSuccess?.Invoke();
            }
            else
            {
                Debug.LogWarning("[AuthManager] JS-bridged token failed validation.");
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void ApplySession(string token, UserDto user)
        {
            _token = token;
            _currentUser = user;
            NetworkManager.Instance.Api.SetBearerToken(token);
            SaveManager.Instance.SaveSession(token, user.id, user.email, "");
        }

        private void ClearSession()
        {
            _token = null;
            _currentUser = null;
            NetworkManager.Instance?.Api?.ClearBearerToken();
            SaveManager.Instance?.ClearSession();
        }
    }
}
