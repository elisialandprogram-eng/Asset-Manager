using UnityEngine;
using System.Collections;
using EternalKingdoms.Networking;
using EternalKingdoms.Authentication;

namespace EternalKingdoms.Core
{
    /// <summary>
    /// Entry point for the entire application.
    /// Initializes all managers in correct dependency order and routes to the
    /// appropriate scene based on stored auth state.
    ///
    /// Bootstrap order:
    ///   ConfigManager → SaveManager → AddressablesManager
    ///   → NetworkManager → AuthManager → SceneController
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        public static BootstrapManager Instance { get; private set; }

        [Header("Bootstrap Settings")]
        [SerializeField] private float initTimeoutSeconds = 30f;

        private bool _initialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(RunBootstrap());
        }

        private IEnumerator RunBootstrap()
        {
            Debug.Log("[Bootstrap] Starting initialization sequence...");
            float startTime = Time.time;

            // Step 1 — ConfigManager
            yield return StartCoroutine(WaitForManager("ConfigManager", ConfigManager.Instance.Initialize()));

            // Step 2 — SaveManager
            yield return StartCoroutine(WaitForManager("SaveManager", SaveManager.Instance.Initialize()));

            // Step 3 — AddressablesManager
            yield return StartCoroutine(WaitForManager("AddressablesManager", AddressablesManager.Instance.Initialize()));

            // Step 4 — NetworkManager
            yield return StartCoroutine(WaitForManager("NetworkManager", NetworkManager.Instance.Initialize()));

            // Step 5 — AuthManager
            yield return StartCoroutine(WaitForManager("AuthManager", AuthManager.Instance.Initialize()));

            // Step 6 — SceneController (final step — routes to correct scene)
            float elapsed = Time.time - startTime;
            Debug.Log($"[Bootstrap] All managers initialized in {elapsed:F2}s. Routing...");
            _initialized = true;

            SceneController.Instance.RouteFromBootstrap();
        }

        private IEnumerator WaitForManager(string managerName, IEnumerator initRoutine)
        {
            Debug.Log($"[Bootstrap] Initializing {managerName}...");
            float start = Time.time;
            yield return StartCoroutine(initRoutine);
            float elapsed = Time.time - start;
            Debug.Log($"[Bootstrap] {managerName} ready ({elapsed * 1000:F0}ms)");
        }

        public bool IsInitialized => _initialized;
    }
}
