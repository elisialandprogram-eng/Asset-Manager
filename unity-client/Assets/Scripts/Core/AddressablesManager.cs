using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EternalKingdoms.Core
{
    /// <summary>
    /// Manages Unity Addressables lifecycle.
    /// Provides typed async load/release API used throughout the game.
    /// All asset loads go through here to ensure ref-counting is correct.
    /// </summary>
    public class AddressablesManager : MonoBehaviour
    {
        public static AddressablesManager Instance { get; private set; }

        // Track handles so we can release on demand or scene unload
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator Initialize()
        {
            Debug.Log("[AddressablesManager] Initializing Addressables runtime...");
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            if (initHandle.Status == AsyncOperationStatus.Succeeded)
                Debug.Log("[AddressablesManager] Addressables ready.");
            else
                Debug.LogError("[AddressablesManager] Addressables initialization failed.");
        }

        // ── Load API ──────────────────────────────────────────────────────────

        /// <summary>Loads an asset by Addressables key. Returns null on failure.</summary>
        public IEnumerator LoadAsset<T>(string key, System.Action<T> onComplete) where T : Object
        {
            if (_handles.ContainsKey(key))
            {
                // Already loaded — return cached result
                onComplete?.Invoke((T)_handles[key].Result);
                yield break;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[key] = handle;
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"[AddressablesManager] Failed to load: {key}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>Instantiates a prefab from an Addressables key.</summary>
        public IEnumerator InstantiatePrefab(string key, Transform parent, System.Action<GameObject> onComplete)
        {
            var handle = Addressables.InstantiateAsync(key, parent);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                onComplete?.Invoke(handle.Result);
            else
            {
                Debug.LogError($"[AddressablesManager] Failed to instantiate: {key}");
                onComplete?.Invoke(null);
            }
        }

        // ── Release API ───────────────────────────────────────────────────────

        public void ReleaseAsset(string key)
        {
            if (_handles.TryGetValue(key, out var handle))
            {
                Addressables.Release(handle);
                _handles.Remove(key);
            }
        }

        public void ReleaseAll()
        {
            foreach (var handle in _handles.Values)
                Addressables.Release(handle);
            _handles.Clear();
            Debug.Log("[AddressablesManager] All assets released.");
        }
    }
}
