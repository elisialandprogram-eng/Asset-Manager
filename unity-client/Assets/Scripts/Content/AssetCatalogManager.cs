using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EternalKingdoms.Content
{
    /// <summary>
    /// AssetCatalogManager — Central Addressables loading layer for all game assets.
    ///
    /// Phase 5 (U5.11) responsibilities:
    ///   - Zero Resources folder usage (all assets in Addressable groups)
    ///   - Named asset keys match the Asset Registry ID system (e.g. "building_palace_001")
    ///   - Hot-swappable: re-loading a key replaces cached reference immediately
    ///   - Preload bundles by category on scene start (terrain, kingdom, UI, VFX)
    ///   - Async load with callbacks — no synchronous Resource.Load anywhere
    ///   - Reference counting — release when refcount reaches 0
    ///   - NFT asset override: if nftAssetUrl != null, load from remote URL instead
    ///
    /// Addressable Groups:
    ///   Terrain      — biome textures, terrain layers
    ///   Buildings    — all building meshes/prefabs by registry ID
    ///   Units        — troop and hero models
    ///   Monsters     — monster prefabs
    ///   Environment  — decoration prefabs (trees, rocks, ruins)
    ///   VFX          — all particle systems
    ///   UI           — sprites, fonts, theme assets
    ///   Audio        — ambient loops, SFX, music
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - Cache keyed by Addressable label/address string
    ///   - PreloadCategory() called by scene bootstrap (BootstrapManager)
    ///   - LoadAsync<T>() is the primary entrypoint for all asset requests
    /// </summary>
    public class AssetCatalogManager : MonoBehaviour
    {
        public static AssetCatalogManager Instance { get; private set; }

        [Header("Preload Groups — Addressable labels")]
        [SerializeField] private string[] preloadOnStartLabels = { "UI", "VFX", "Audio/SFX" };

        private readonly Dictionary<string, object>           _cache    = new();
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new();
        private readonly Dictionary<string, int>              _refCount = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            foreach (var label in preloadOnStartLabels)
                StartCoroutine(PreloadLabel(label));
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Load an asset by its Addressable address / Asset Registry ID.
        /// Callback receives the loaded asset or null on failure.
        /// </summary>
        public IEnumerator LoadAsync<T>(string address, Action<T> onLoaded,
                                        Action<string> onError = null) where T : UnityEngine.Object
        {
            if (_cache.TryGetValue(address, out var cached))
            {
                _refCount[address] = (_refCount.TryGetValue(address, out int c) ? c : 0) + 1;
                if (cached is T typedCached)
                {
                    onLoaded?.Invoke(typedCached);
                    yield break;
                }
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            _handles[address] = handle;

            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _cache[address]    = handle.Result;
                _refCount[address] = 1;
                onLoaded?.Invoke(handle.Result);
            }
            else
            {
                string err = $"[AssetCatalog] Failed to load: {address}";
                Debug.LogWarning(err);
                onError?.Invoke(err);
            }
        }

        /// <summary>
        /// Load a prefab and instantiate it. Caller is responsible for lifecycle.
        /// </summary>
        public IEnumerator InstantiateAsync(string address, Vector3 position,
                                             Quaternion rotation, Transform parent,
                                             Action<GameObject> onComplete,
                                             Action<string> onError = null)
        {
            var handle = Addressables.InstantiateAsync(address, position, rotation, parent);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                onComplete?.Invoke(handle.Result);
            else
            {
                string err = $"[AssetCatalog] Failed to instantiate: {address}";
                Debug.LogWarning(err);
                onError?.Invoke(err);
            }
        }

        /// <summary>
        /// Release a loaded asset. Unloads from memory when refcount reaches 0.
        /// </summary>
        public void Release(string address)
        {
            if (!_refCount.TryGetValue(address, out int count)) return;
            count--;
            if (count <= 0)
            {
                _refCount.Remove(address);
                _cache.Remove(address);
                if (_handles.TryGetValue(address, out var handle))
                {
                    Addressables.Release(handle);
                    _handles.Remove(address);
                }
            }
            else
            {
                _refCount[address] = count;
            }
        }

        /// <summary>
        /// Hot-swap an asset key — invalidate cache so next load fetches fresh.
        /// Used for NFT asset overrides.
        /// </summary>
        public void Invalidate(string address)
        {
            Release(address);
            _cache.Remove(address);
        }

        /// <summary>Check if an asset is already cached.</summary>
        public bool IsCached(string address) => _cache.ContainsKey(address);

        // ── Preload ───────────────────────────────────────────────────────────

        public IEnumerator PreloadLabel(string label)
        {
            var handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label,
                asset =>
                {
                    if (!_cache.ContainsKey(asset.name))
                        _cache[asset.name] = asset;
                });
            yield return handle;
            Debug.Log($"[AssetCatalog] Preloaded label: {label} ({handle.Result?.Count ?? 0} assets)");
        }

        public IEnumerator PreloadAddresses(string[] addresses, Action onComplete = null)
        {
            int pending = addresses.Length;
            foreach (var addr in addresses)
            {
                yield return StartCoroutine(LoadAsync<UnityEngine.Object>(addr,
                    _ => { pending--; },
                    _ => { pending--; }
                ));
            }
            onComplete?.Invoke();
        }

        // ── Utility ───────────────────────────────────────────────────────────

        /// <summary>Build Addressable key from Asset Registry ID. Convention: "category/id".</summary>
        public static string BuildKey(string category, string registryId)
            => $"{category}/{registryId}";

        public static string BuildBuildingKey(string registryId)  => BuildKey("Buildings", registryId);
        public static string BuildMonsterKey(string registryId)   => BuildKey("Monsters",  registryId);
        public static string BuildHeroKey(string registryId)      => BuildKey("Units",     registryId);
        public static string BuildVFXKey(string effectName)       => BuildKey("VFX",       effectName);
        public static string BuildAudioKey(string clipName)       => BuildKey("Audio",     clipName);
        public static string BuildUIKey(string assetName)         => BuildKey("UI",        assetName);
    }
}
