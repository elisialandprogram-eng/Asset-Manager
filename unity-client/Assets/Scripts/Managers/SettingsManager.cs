using UnityEngine;
using EternalKingdoms.Core;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// Manages graphics quality and audio preferences.
    /// Settings are persisted in PlayerPrefs via SaveManager.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        private const string KEY_GFX_QUALITY = "ek_gfx_quality";
        private const string KEY_MUSIC_VOL = "ek_music_vol";
        private const string KEY_SFX_VOL = "ek_sfx_vol";

        private int _graphicsQuality;
        private float _musicVolume;
        private float _sfxVolume;

        public int GraphicsQuality => _graphicsQuality;
        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void Load()
        {
            _graphicsQuality = PlayerPrefs.GetInt(KEY_GFX_QUALITY, QualitySettings.GetQualityLevel());
            _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.6f);
            _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1.0f);
            Apply();
        }

        private void Apply()
        {
            QualitySettings.SetQualityLevel(_graphicsQuality, applyExpensiveChanges: true);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.MusicVolume = _musicVolume;
                AudioManager.Instance.SfxVolume = _sfxVolume;
            }
        }

        public void SetGraphicsQuality(int level)
        {
            _graphicsQuality = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(KEY_GFX_QUALITY, _graphicsQuality);
            QualitySettings.SetQualityLevel(_graphicsQuality, applyExpensiveChanges: true);
        }

        public void SetMusicVolume(float v)
        {
            _musicVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, _musicVolume);
            if (AudioManager.Instance != null) AudioManager.Instance.MusicVolume = _musicVolume;
        }

        public void SetSfxVolume(float v)
        {
            _sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KEY_SFX_VOL, _sfxVolume);
            if (AudioManager.Instance != null) AudioManager.Instance.SfxVolume = _sfxVolume;
        }

        public void SaveAll() => PlayerPrefs.Save();
    }
}
