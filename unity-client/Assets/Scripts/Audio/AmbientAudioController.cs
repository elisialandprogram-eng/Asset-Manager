using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EternalKingdoms.Audio
{
    /// <summary>
    /// AmbientAudioController — Manages context-sensitive ambient audio.
    ///
    /// Phase 5 (U5.9) ambient layers:
    ///   Kingdom:  fireplace crackle, distant blacksmith, crowd murmur, flag snap
    ///   Grasslands: wind, birds, insects
    ///   Forest:   deep wind, owl, rustling leaves, water stream
    ///   Snow:     howling wind, silence, occasional crack
    ///   Desert:   hot wind, rare bird, sand scrape
    ///   Highlands: strong wind, eagle, rock tumble
    ///   Swamp:    bubbling mud, frogs, insects, drips
    ///   Volcanic: low rumble, fire crackle, hissing steam
    ///   Monster:  growl, breathing (proximity-based, within 50u)
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - Each ambient context has 2-3 AudioSource layers for cross-fade
    ///   - Cross-fade duration: 3 seconds between biome transitions
    ///   - Stingers: one-shot audio events for points of interest
    ///   - Monster proximity: AudioSource attached to monster with distance rolloff
    ///   - All audio loaded via AssetCatalogManager (Addressables)
    /// </summary>
    public class AmbientAudioController : MonoBehaviour
    {
        public static AmbientAudioController Instance { get; private set; }

        [Header("Ambient Sources")]
        [SerializeField] private AudioSource ambientLayerA;
        [SerializeField] private AudioSource ambientLayerB;
        [SerializeField] private AudioSource stingerSource;

        [Header("Cross-fade")]
        [SerializeField] private float crossfadeDuration = 3f;

        [Header("Ambient Contexts")]
        [SerializeField] private AmbientContext contextKingdom;
        [SerializeField] private AmbientContext contextGrasslands;
        [SerializeField] private AmbientContext contextForest;
        [SerializeField] private AmbientContext contextSnow;
        [SerializeField] private AmbientContext contextDesert;
        [SerializeField] private AmbientContext contextHighlands;
        [SerializeField] private AmbientContext contextSwamp;
        [SerializeField] private AmbientContext contextVolcanic;

        [Header("Monster Ambient")]
        [SerializeField] private GameObject monsterAmbientPrefab;
        [SerializeField] private float monsterAmbientRadius = 50f;

        private AudioSource _activeSource;
        private AudioSource _fadingSource;
        private AmbientContext _currentContext;
        private Coroutine _crossfadeCoroutine;

        private readonly Dictionary<string, AmbientContext> _registry = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _activeSource = ambientLayerA;
            _fadingSource = ambientLayerB;
            _fadingSource.volume = 0f;

            BuildRegistry();
        }

        private void BuildRegistry()
        {
            Register("kingdom",    contextKingdom);
            Register("grasslands", contextGrasslands);
            Register("forest",     contextForest);
            Register("snow",       contextSnow);
            Register("desert",     contextDesert);
            Register("highlands",  contextHighlands);
            Register("swamp",      contextSwamp);
            Register("volcanic",   contextVolcanic);
        }

        private void Register(string key, AmbientContext ctx)
        {
            if (ctx != null) _registry[key] = ctx;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void TransitionTo(string contextKey)
        {
            if (!_registry.TryGetValue(contextKey, out var ctx)) return;
            if (ctx == _currentContext) return;
            _currentContext = ctx;

            if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = StartCoroutine(Crossfade(ctx));
        }

        public void PlayStinger(string contextKey)
        {
            if (!_registry.TryGetValue(contextKey, out var ctx)) return;
            if (ctx.stinger == null || stingerSource == null) return;
            stingerSource.PlayOneShot(ctx.stinger);
        }

        /// <summary>Attach monster ambient to an entity. Auto-destructs when entity destroyed.</summary>
        public void AttachMonsterAmbient(Transform monsterTransform, AudioClip growl)
        {
            if (monsterAmbientPrefab == null || monsterTransform == null || growl == null) return;
            var go = Instantiate(monsterAmbientPrefab, monsterTransform);
            var src = go.GetComponent<AudioSource>();
            if (src != null)
            {
                src.clip         = growl;
                src.loop         = true;
                src.spatialBlend = 1f;
                src.minDistance  = 5f;
                src.maxDistance  = monsterAmbientRadius;
                src.Play();
            }
        }

        // ── Cross-fade ────────────────────────────────────────────────────────

        private IEnumerator Crossfade(AmbientContext ctx)
        {
            // Swap roles
            var temp = _fadingSource;
            _fadingSource = _activeSource;
            _activeSource = temp;

            // Set up new layer
            _activeSource.clip   = ctx.loop;
            _activeSource.loop   = true;
            _activeSource.volume = 0f;
            _activeSource.Play();

            float targetVolume = ctx.volume;
            float elapsed      = 0f;
            float startVolume  = _fadingSource.volume;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;
                _activeSource.volume = Mathf.Lerp(0f, targetVolume, t);
                _fadingSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            _activeSource.volume = targetVolume;
            _fadingSource.volume = 0f;
            _fadingSource.Stop();
        }
    }

    // ── Ambient Context ────────────────────────────────────────────────────────

    [Serializable]
    public class AmbientContext
    {
        public AudioClip loop;
        public AudioClip stinger;
        [Range(0f, 1f)] public float volume = 0.6f;
    }
}
