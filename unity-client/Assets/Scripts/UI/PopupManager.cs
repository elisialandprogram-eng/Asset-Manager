using UnityEngine;
using System.Collections.Generic;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// Manages instantiation and lifecycle of modal popups.
    /// All popup prefabs must be registered here before use.
    /// Popups are loaded from Resources/Prefabs/UI/Popups/.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        // Addressables key prefix for popup prefabs
        private const string POPUP_PREFAB_PATH = "Prefabs/UI/Popups/";

        private readonly Dictionary<string, GameObject> _prefabCache = new();
        private readonly List<GameObject> _active = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Show / Hide ───────────────────────────────────────────────────────

        /// <summary>Shows a popup by prefab key. Returns the instantiated instance.</summary>
        public GameObject ShowPopup(string key, Transform parent = null)
        {
            var prefab = LoadPrefab(key);
            if (prefab == null)
            {
                Debug.LogError($"[PopupManager] Prefab not found: {key}");
                return null;
            }

            var target = parent != null ? parent : UIManager.Instance?.PopupCanvas?.transform;
            var instance = Instantiate(prefab, target);
            _active.Add(instance);
            UIManager.Instance?.PushPopup(instance);
            return instance;
        }

        /// <summary>Closes the topmost popup.</summary>
        public void CloseTopPopup()
        {
            UIManager.Instance?.PopPopup();
            if (_active.Count > 0)
            {
                var top = _active[^1];
                _active.RemoveAt(_active.Count - 1);
                if (top != null) Destroy(top);
            }
        }

        /// <summary>Closes a specific popup instance.</summary>
        public void ClosePopup(GameObject instance)
        {
            if (_active.Contains(instance))
            {
                _active.Remove(instance);
                UIManager.Instance?.PopPopup();
                Destroy(instance);
            }
        }

        public void CloseAll()
        {
            foreach (var p in _active)
                if (p != null) Destroy(p);
            _active.Clear();
            UIManager.Instance?.CloseAllPopups();
        }

        // ── Prefab loading ────────────────────────────────────────────────────

        private GameObject LoadPrefab(string key)
        {
            if (_prefabCache.TryGetValue(key, out var cached)) return cached;
            var prefab = UnityEngine.Resources.Load<GameObject>(POPUP_PREFAB_PATH + key);
            if (prefab != null) _prefabCache[key] = prefab;
            return prefab;
        }

        public void RegisterPrefab(string key, GameObject prefab)
        {
            _prefabCache[key] = prefab;
        }
    }
}
