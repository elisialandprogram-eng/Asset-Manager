using UnityEngine;
using EternalKingdoms.Authentication;

namespace EternalKingdoms.Core
{
    /// <summary>
    /// Top-level application lifecycle manager.
    /// Persists across all scenes. Owns references to all subsystem managers
    /// and provides a central access point for game-wide state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        private GameState _currentState = GameState.Bootstrapping;

        public GameState CurrentState => _currentState;
        public bool IsLoggedIn => AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Debug.Log("[GameManager] Initialized.");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("[GameManager] App paused — saving state.");
                SaveManager.Instance?.SaveAll();
            }
        }

        private void OnApplicationQuit()
        {
            Debug.Log("[GameManager] App quitting — graceful shutdown.");
            Shutdown();
        }

        public void SetState(GameState newState)
        {
            Debug.Log($"[GameManager] State: {_currentState} → {newState}");
            _currentState = newState;
        }

        private void Shutdown()
        {
            SaveManager.Instance?.SaveAll();
            Networking.NetworkManager.Instance?.Shutdown();
        }
    }

    public enum GameState
    {
        Bootstrapping,
        Login,
        Loading,
        World,
        Kingdom,
        Error
    }
}
