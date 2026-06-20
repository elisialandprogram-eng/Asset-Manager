using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Core;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// Manages all game audio: background music and SFX.
    /// Persists across scenes. Volume settings sync with SaveManager.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.6f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1.0f;

        // Clip registry (populated via Addressables or Inspector assignment)
        private readonly Dictionary<string, AudioClip> _clips = new();

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (musicSource) musicSource.volume = musicVolume;
                SaveManager.Instance?.SetFloat(SaveManager.KEY_AUDIO_VOLUME, musicVolume);
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                if (sfxSource) sfxSource.volume = sfxVolume;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
        }

        private void Start()
        {
            if (SaveManager.Instance != null)
                MusicVolume = SaveManager.Instance.GetFloat(SaveManager.KEY_AUDIO_VOLUME, 0.6f);
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.volume = sfxVolume;
                sfxSource.playOnAwake = false;
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic() => musicSource.Stop();

        public void PlaySfx(string clipKey)
        {
            if (_clips.TryGetValue(clipKey, out var clip))
                sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
                sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void RegisterClip(string key, AudioClip clip)
        {
            if (clip != null)
                _clips[key] = clip;
        }

        public IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            musicSource.Stop();
            musicSource.volume = startVolume;
        }

        // ── Phase 5: Categorized audio channels (U5.9) ────────────────────────

        [Header("Phase 5 — Categorized Channels")]
        [SerializeField] [Range(0f, 1f)] private float uiVolume      = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float combatVolume  = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float worldVolume   = 0.7f;

        private AudioSource _uiSource;
        private AudioSource _combatSource;
        private AudioSource _worldSource;

        private void EnsurePhase5Sources()
        {
            if (_uiSource == null)
            {
                _uiSource = gameObject.AddComponent<AudioSource>();
                _uiSource.loop = false; _uiSource.playOnAwake = false;
                _uiSource.volume = uiVolume;
            }
            if (_combatSource == null)
            {
                _combatSource = gameObject.AddComponent<AudioSource>();
                _combatSource.loop = false; _combatSource.playOnAwake = false;
                _combatSource.volume = combatVolume;
            }
            if (_worldSource == null)
            {
                _worldSource = gameObject.AddComponent<AudioSource>();
                _worldSource.loop = false; _worldSource.playOnAwake = false;
                _worldSource.volume = worldVolume;
            }
        }

        /// <summary>Play a categorized UI sound (button click, panel open, etc.)</summary>
        public void PlayUI(string clipKey)
        {
            EnsurePhase5Sources();
            if (_clips.TryGetValue(clipKey, out var clip))
                _uiSource.PlayOneShot(clip, uiVolume);
        }

        /// <summary>Play a combat sound effect (sword clash, spell, roar).</summary>
        public void PlayCombat(string clipKey)
        {
            EnsurePhase5Sources();
            if (_clips.TryGetValue(clipKey, out var clip))
                _combatSource.PlayOneShot(clip, combatVolume);
        }

        /// <summary>Play a world event sound (march arrival, building complete).</summary>
        public void PlayWorld(string clipKey)
        {
            EnsurePhase5Sources();
            if (_clips.TryGetValue(clipKey, out var clip))
                _worldSource.PlayOneShot(clip, worldVolume);
        }

        public float UIVolume
        {
            get => uiVolume;
            set { uiVolume = Mathf.Clamp01(value); if (_uiSource) _uiSource.volume = uiVolume; }
        }
        public float CombatVolume
        {
            get => combatVolume;
            set { combatVolume = Mathf.Clamp01(value); if (_combatSource) _combatSource.volume = combatVolume; }
        }
        public float WorldVolume
        {
            get => worldVolume;
            set { worldVolume = Mathf.Clamp01(value); if (_worldSource) _worldSource.volume = worldVolume; }
        }
    }
}

// ── Namespace alias so AmbientAudioController (EternalKingdoms.Audio) can find it ──
namespace EternalKingdoms.Audio
{
    /// <summary>Proxy re-export so EternalKingdoms.Audio.AudioManager resolves to the Managers singleton.</summary>
    public static class AudioManager
    {
        public static EternalKingdoms.Managers.AudioManager Instance
            => EternalKingdoms.Managers.AudioManager.Instance;
    }
}
