using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EternalKingdoms.Environment
{
    /// <summary>
    /// U5.6.3 — Day/Night & Weather System
    /// Drives a 24-hour world cycle (Dawn/Day/Sunset/Night) and six weather types,
    /// synchronized to global server time.  Handles dynamic skybox, cloud layers,
    /// volumetric fog, directional light, ambient audio handoff, and particles.
    /// </summary>
    public class WorldEnvironmentManager : MonoBehaviour
    {
        public static WorldEnvironmentManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Time Settings")]
        [Tooltip("Length of one in-game day in real seconds (e.g. 1800 = 30 min).")]
        public float dayLengthSeconds = 1800f;
        [Range(0f, 24f)]
        public float startHour = 8f;

        [Header("Sun / Moon")]
        public Light sunLight;
        public Light moonLight;
        public AnimationCurve sunIntensityCurve;    // 0-24h → 0-1
        public AnimationCurve moonIntensityCurve;

        [Header("Skybox")]
        public Material daySkybox;
        public Material nightSkybox;
        [Range(0f, 1f)] public float skyboxBlendSpeed = 0.5f;

        [Header("Ambient Colors — Phases")]
        public Color dawnAmbient   = new(0.7f, 0.5f, 0.35f);
        public Color dayAmbient    = new(0.6f, 0.65f, 0.7f);
        public Color sunsetAmbient = new(0.8f, 0.45f, 0.2f);
        public Color nightAmbient  = new(0.08f, 0.09f, 0.18f);

        [Header("Post Processing")]
        public Volume globalVolume;
        public VolumeProfile profileDay;
        public VolumeProfile profileNight;
        public VolumeProfile profileStorm;

        [Header("Weather Particles")]
        public ParticleSystem rainParticles;
        public ParticleSystem snowParticles;
        public ParticleSystem ashfallParticles;
        public ParticleSystem fogParticles;

        [Header("Cloud Layers")]
        public MeshRenderer cloudLayerDay;
        public MeshRenderer cloudLayerStorm;

        [Header("Audio")]
        public AudioSource weatherAudioSource;
        public AudioClip clipRain;
        public AudioClip clipStorm;
        public AudioClip clipWind;

        [Header("Transition Speed")]
        [Range(0.01f, 2f)] public float weatherTransitionSpeed = 0.5f;

        // ── State ─────────────────────────────────────────────────────────────
        private float _currentHour;       // 0–24
        private TimeOfDay _timeOfDay;
        private WeatherType _weather      = WeatherType.Clear;
        private WeatherType _targetWeather= WeatherType.Clear;
        private bool _transitioning;
        private float _weatherBlend;      // 0 = old, 1 = new

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<TimeOfDay>  OnTimePhaseChanged;
        public event Action<WeatherType> OnWeatherChanged;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _currentHour = startHour;
        }

        private void Start()
        {
            SyncToServerTime();
            ApplyImmediatePhase();
        }

        private void Update()
        {
            AdvanceTime();
            UpdateLighting();

            if (_transitioning) TickWeatherTransition();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Time
        // ─────────────────────────────────────────────────────────────────────
        private void AdvanceTime()
        {
            _currentHour += (24f / dayLengthSeconds) * Time.deltaTime;
            if (_currentHour >= 24f) _currentHour -= 24f;

            var phase = HourToPhase(_currentHour);
            if (phase != _timeOfDay)
            {
                _timeOfDay = phase;
                OnTimePhaseChanged?.Invoke(_timeOfDay);
                ApplyPhaseAudio();
            }
        }

        private static TimeOfDay HourToPhase(float h) =>
            h < 6f  ? TimeOfDay.Night  :
            h < 8f  ? TimeOfDay.Dawn   :
            h < 18f ? TimeOfDay.Day    :
            h < 20f ? TimeOfDay.Sunset :
                      TimeOfDay.Night;

        /// <summary>Sync hour offset to server epoch so all players share the same time.</summary>
        private void SyncToServerTime()
        {
            double utcSeconds = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            double dayFraction = (utcSeconds % dayLengthSeconds) / dayLengthSeconds;
            _currentHour = (float)(dayFraction * 24.0);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Lighting
        // ─────────────────────────────────────────────────────────────────────
        private void UpdateLighting()
        {
            float t = _currentHour / 24f;

            // Sun arc: rises east (270°), sets west (90°)
            float sunAngle = Mathf.Lerp(-90f, 270f, t);
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
                sunLight.intensity = sunIntensityCurve != null
                    ? sunIntensityCurve.Evaluate(t)
                    : Mathf.Clamp01(Mathf.Sin(t * Mathf.PI));
                sunLight.color = Color.Lerp(
                    Color.white,
                    new Color(1f, 0.75f, 0.4f),
                    Mathf.Pow(Mathf.Abs(Mathf.Sin(t * Mathf.PI)), 4f));
            }

            // Moon opposite
            if (moonLight != null)
            {
                moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, -30f, 0f);
                moonLight.intensity = moonIntensityCurve != null
                    ? moonIntensityCurve.Evaluate(t)
                    : Mathf.Clamp01(1f - sunLight.intensity) * 0.15f;
            }

            // Ambient
            Color ambient = _timeOfDay switch
            {
                TimeOfDay.Dawn   => dawnAmbient,
                TimeOfDay.Day    => dayAmbient,
                TimeOfDay.Sunset => sunsetAmbient,
                _                => nightAmbient
            };
            RenderSettings.ambientLight = Color.Lerp(
                RenderSettings.ambientLight, ambient, Time.deltaTime * 2f);
        }

        private void ApplyImmediatePhase()
        {
            _timeOfDay = HourToPhase(_currentHour);
            if (globalVolume != null)
                globalVolume.profile = _timeOfDay == TimeOfDay.Night ? profileNight : profileDay;

            if (RenderSettings.skybox != null && daySkybox != null && nightSkybox != null)
                RenderSettings.skybox = _timeOfDay == TimeOfDay.Night ? nightSkybox : daySkybox;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Weather
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Request a weather transition.  Blends over weatherTransitionSpeed seconds.</summary>
        public void SetWeather(WeatherType weather)
        {
            if (weather == _targetWeather) return;
            _targetWeather = weather;
            _transitioning  = true;
            _weatherBlend   = 0f;
        }

        private void TickWeatherTransition()
        {
            _weatherBlend = Mathf.MoveTowards(_weatherBlend, 1f, Time.deltaTime * weatherTransitionSpeed);
            BlendWeatherParticles(_weather, _targetWeather, _weatherBlend);
            BlendWeatherAudio(_weather, _targetWeather, _weatherBlend);

            if (_weatherBlend >= 1f)
            {
                _weather      = _targetWeather;
                _transitioning = false;
                ApplyWeatherPostProcess(_weather);
                OnWeatherChanged?.Invoke(_weather);
            }
        }

        private void BlendWeatherParticles(WeatherType from, WeatherType to, float t)
        {
            SetParticleEmission(rainParticles,    IsRaining(to)   ? t : (IsRaining(from)   ? 1f - t : 0f));
            SetParticleEmission(snowParticles,    IsSnowing(to)   ? t : (IsSnowing(from)   ? 1f - t : 0f));
            SetParticleEmission(ashfallParticles, IsAshfall(to)   ? t : (IsAshfall(from)   ? 1f - t : 0f));
            SetParticleEmission(fogParticles,     IsFoggy(to)     ? t : (IsFoggy(from)     ? 1f - t : 0f));

            if (cloudLayerStorm != null)
            {
                float stormAlpha = (to == WeatherType.Storm) ? t : (from == WeatherType.Storm ? 1f - t : 0f);
                var c = cloudLayerStorm.material.color;
                c.a = stormAlpha;
                cloudLayerStorm.material.color = c;
            }
        }

        private void BlendWeatherAudio(WeatherType from, WeatherType to, float t)
        {
            if (weatherAudioSource == null) return;
            AudioClip targetClip = to switch
            {
                WeatherType.Rain  => clipRain,
                WeatherType.Storm => clipStorm,
                WeatherType.Fog   => clipWind,
                WeatherType.Snow  => clipWind,
                _                 => null
            };

            if (targetClip != null && weatherAudioSource.clip != targetClip)
            {
                weatherAudioSource.clip = targetClip;
                weatherAudioSource.Play();
            }
            weatherAudioSource.volume = Mathf.Lerp(
                (from != WeatherType.Clear) ? 0.6f : 0f,
                (to   != WeatherType.Clear) ? 0.6f : 0f,
                t);
        }

        private void ApplyWeatherPostProcess(WeatherType w)
        {
            if (globalVolume == null) return;
            globalVolume.profile = w == WeatherType.Storm ? profileStorm
                : (_timeOfDay == TimeOfDay.Night ? profileNight : profileDay);

            if (RenderSettings.fog != (w != WeatherType.Clear))
            {
                RenderSettings.fog = w != WeatherType.Clear;
                RenderSettings.fogDensity = w == WeatherType.Storm ? 0.04f
                    : w == WeatherType.Fog ? 0.06f : 0.015f;
            }
        }

        private void ApplyPhaseAudio()
        {
            // Notify AmbientAudioController of phase change for music crossfade
            var aac = FindAnyObjectByType<Audio.AmbientAudioController>();
            aac?.TransitionTo(_timeOfDay.ToString());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private static void SetParticleEmission(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.enabled = rate > 0.01f;
            var main = ps.main;
            main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(0, 2000, rate));
        }

        private static bool IsRaining(WeatherType w)  => w == WeatherType.Rain || w == WeatherType.Storm;
        private static bool IsSnowing(WeatherType w)  => w == WeatherType.Snow;
        private static bool IsAshfall(WeatherType w)  => w == WeatherType.Ashfall;
        private static bool IsFoggy(WeatherType w)    => w == WeatherType.Fog;

        // ── Public API ────────────────────────────────────────────────────────
        public float      CurrentHour => _currentHour;
        public TimeOfDay  CurrentPhase => _timeOfDay;
        public WeatherType CurrentWeather => _weather;

        /// <summary>Force-set the current in-game hour (for testing / server sync).</summary>
        public void SetHour(float hour) => _currentHour = Mathf.Repeat(hour, 24f);
    }

    // ── Enums ─────────────────────────────────────────────────────────────────
    public enum TimeOfDay  { Dawn, Day, Sunset, Night }
    public enum WeatherType { Clear, Rain, Storm, Snow, Fog, Ashfall }
}
