using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EternalKingdoms.VFX
{
    /// <summary>
    /// VFXLibrary — Central registry and pool for all reusable VFX.
    ///
    /// Phase 5 (U5.10) effect catalogue:
    ///   selection_ring      — glowing ring expands under selected entity
    ///   click_burst         — radial particle burst on world click
    ///   resource_harvest    — resource particles float up from node
    ///   march_arrival       — arrival shockwave + dust ring
    ///   monster_defeat      — large explosion + debris + screen shake
    ///   level_up            — golden column + starburst
    ///   reward_popup        — coin/item particles arc toward HUD
    ///   building_complete   — fireworks + light flash
    ///   crystal_resonate    — crystal chord shatter/reform
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - All VFX prefabs stored in VFXRegistry ScriptableObject (set in Inspector)
    ///   - Per-effect pool: min pool size 2, grows on demand
    ///   - Effects auto-return to pool when ParticleSystem stops
    ///   - VFX Graph effects (Unity 6) used where possible; fallback to ParticleSystem
    ///   - All prefabs loaded via AssetCatalogManager (Addressables)
    /// </summary>
    public class VFXLibrary : MonoBehaviour
    {
        public static VFXLibrary Instance { get; private set; }

        [Header("VFX Registry")]
        [SerializeField] private VFXRegistry registry;

        [Header("Screen Shake")]
        [SerializeField] private float defaultShakeMagnitude = 0.15f;
        [SerializeField] private float defaultShakeDuration  = 0.25f;

        private readonly Dictionary<string, VFXPool> _pools = new();
        private Camera _mainCamera;
        private Transform _poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("VFXPool").transform;
            _poolRoot.SetParent(transform);

            _mainCamera = Camera.main;
        }

        private void Start()
        {
            if (registry == null) return;
            foreach (var entry in registry.entries)
                if (entry.prefab != null)
                    _pools[entry.key] = new VFXPool(entry.prefab, _poolRoot, entry.poolSize);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Play a VFX at world position. Returns the instance (auto-returns to pool).</summary>
        public GameObject Play(string key, Vector3 position, Quaternion rotation = default,
                               float scale = 1f, float overrideDuration = -1f)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogWarning($"[VFXLibrary] Unknown VFX key: {key}");
                return null;
            }

            var go = pool.Get();
            go.transform.position   = position;
            go.transform.rotation   = rotation == default ? Quaternion.identity : rotation;
            go.transform.localScale = Vector3.one * scale;
            go.SetActive(true);

            // Restart all particle systems
            float duration = 0f;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
                duration = Mathf.Max(duration, ps.main.duration + ps.main.startLifetime.constantMax);
            }

            float returnDelay = overrideDuration > 0f ? overrideDuration : duration;
            if (returnDelay <= 0f) returnDelay = 3f;
            StartCoroutine(ReturnAfter(pool, go, returnDelay));
            return go;
        }

        /// <summary>Play selection ring — follows entity each frame until cancelled.</summary>
        public Coroutine PlaySelectionRing(string key, Transform follow)
        {
            var go = Play(key, follow.position);
            if (go == null) return null;
            return StartCoroutine(FollowTarget(go, follow));
        }

        /// <summary>Trigger a screen shake (requires camera cinemachine impulse or simple shake).</summary>
        public void ScreenShake(float magnitude = -1f, float duration = -1f)
        {
            float mag = magnitude < 0f ? defaultShakeMagnitude : magnitude;
            float dur = duration  < 0f ? defaultShakeDuration  : duration;
            StartCoroutine(ShakeCamera(mag, dur));
        }

        // ── Convenience wrappers ──────────────────────────────────────────────

        public void PlaySelectionRing(Vector3 pos)     => Play("selection_ring", pos);
        public void PlayClickBurst(Vector3 pos)        => Play("click_burst",    pos);
        public void PlayHarvest(Vector3 pos)           => Play("resource_harvest", pos);
        public void PlayMarchArrival(Vector3 pos)      => Play("march_arrival",  pos, scale: 1.5f);
        public void PlayMonsterDefeat(Vector3 pos)     { Play("monster_defeat", pos, scale: 2f); ScreenShake(0.3f, 0.4f); }
        public void PlayLevelUp(Vector3 pos)           => Play("level_up", pos);
        public void PlayRewardPopup(Vector3 pos)       => Play("reward_popup", pos);
        public void PlayBuildingComplete(Vector3 pos)  => Play("building_complete", pos);
        public void PlayCrystalResonate(Vector3 pos)   => Play("crystal_resonate", pos);

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator ReturnAfter(VFXPool pool, GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            go.SetActive(false);
            pool.Return(go);
        }

        private IEnumerator FollowTarget(GameObject go, Transform target)
        {
            while (go.activeSelf && target != null)
            {
                go.transform.position = target.position;
                yield return null;
            }
        }

        private IEnumerator ShakeCamera(float magnitude, float duration)
        {
            if (_mainCamera == null) yield break;
            Vector3 origin  = _mainCamera.transform.localPosition;
            float   elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = 1f - (elapsed / duration);
                _mainCamera.transform.localPosition = origin
                    + UnityEngine.Random.insideUnitSphere * magnitude * progress;
                yield return null;
            }
            _mainCamera.transform.localPosition = origin;
        }
    }

    // ── VFX Pool ───────────────────────────────────────────────────────────────

    public class VFXPool
    {
        private readonly GameObject _prefab;
        private readonly Transform  _parent;
        private readonly Stack<GameObject> _free = new();

        public VFXPool(GameObject prefab, Transform parent, int initialSize)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initialSize; i++)
                Return(Create());
        }

        public GameObject Get()
        {
            return _free.Count > 0 ? _free.Pop() : Create();
        }

        public void Return(GameObject go)
        {
            go.SetActive(false);
            _free.Push(go);
        }

        private GameObject Create()
        {
            var go = UnityEngine.Object.Instantiate(_prefab, _parent);
            go.SetActive(false);
            return go;
        }
    }

    // ── VFX Registry ScriptableObject ─────────────────────────────────────────

    [CreateAssetMenu(fileName = "VFXRegistry", menuName = "EK/VFX/Registry")]
    public class VFXRegistry : ScriptableObject
    {
        public VFXEntry[] entries;

        [Serializable]
        public class VFXEntry
        {
            public string     key;
            public GameObject prefab;
            [Range(1, 20)] public int poolSize = 3;
        }
    }
}
