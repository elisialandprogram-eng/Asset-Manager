using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EternalKingdoms.Environment;
using EternalKingdoms.VFX;

namespace EternalKingdoms.CameraDemo
{
    /// <summary>
    /// U5.7.8 — Trailer Mode
    /// Orchestrates predefined cinematic sequences for trailer capture.
    ///
    /// Sequences (press F9 to start, F10 to skip shot):
    ///   1. Kingdom flythrough
    ///   2. World exploration pan
    ///   3. March movement
    ///   4. Monster combat
    ///   5. Sunrise time-lapse
    ///   6. Night kingdom
    ///   7. Storm weather
    ///
    /// Each shot is tagged, timed, and export-ready.
    /// </summary>
    public class TrailerCaptureController : MonoBehaviour
    {
        public static TrailerCaptureController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Camera")]
        public CinematicCameraManager cinematicCamera;

        [Header("Scene References")]
        public Transform kingdomRoot;
        public Transform[] marchWaypoints;
        public Transform   monsterTarget;

        [Header("Shot Durations (seconds)")]
        public float shotKingdomFlythrough = 18f;
        public float shotWorldExploration  = 20f;
        public float shotMarchMovement     = 15f;
        public float shotMonsterCombat     = 12f;
        public float shotSunrise           = 20f;
        public float shotNightKingdom      = 15f;
        public float shotStormWeather      = 18f;

        [Header("Inter-Shot Pause")]
        [Range(0f, 5f)] public float pauseBetweenShots = 2f;

        [Header("Output")]
        public string outputFolder         = "TrailerCapture";
        public bool   captureScreenshotsPerShot = true;
        public int    screenshotSuperSize  = 4;

        [Header("HUD")]
        public GameObject trailerHUDRoot;
        public TMPro.TextMeshProUGUI shotNameLabel;
        public TMPro.TextMeshProUGUI shotTimerLabel;
        public UnityEngine.UI.Image  shotProgressBar;

        // ── State ─────────────────────────────────────────────────────────────
        private bool     _running;
        private int      _currentShot;
        private bool     _skipRequested;

        private readonly List<(string name, Func<IEnumerator> shot)> _shots = new();

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RegisterShots();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9) && !_running)  StartTrailer();
            if (Input.GetKeyDown(KeyCode.F10) && _running)  _skipRequested = true;
            if (Input.GetKeyDown(KeyCode.Escape) && _running) StopTrailer();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Shot Registry
        // ─────────────────────────────────────────────────────────────────────
        private void RegisterShots()
        {
            _shots.Add(("Kingdom Flythrough",  ShotKingdomFlythrough));
            _shots.Add(("World Exploration",   ShotWorldExploration));
            _shots.Add(("March Movement",      ShotMarchMovement));
            _shots.Add(("Monster Combat",      ShotMonsterCombat));
            _shots.Add(("Sunrise",             ShotSunrise));
            _shots.Add(("Night Kingdom",       ShotNightKingdom));
            _shots.Add(("Storm Weather",       ShotStormWeather));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Playback
        // ─────────────────────────────────────────────────────────────────────
        public void StartTrailer()
        {
            if (_running) return;
            _running = true;
            StartCoroutine(RunTrailer());
        }

        public void StopTrailer()
        {
            StopAllCoroutines();
            _running = false;
            HideHUD();
            cinematicCamera?.StopCinematic();
            Debug.Log("[TrailerCapture] Trailer stopped.");
        }

        private IEnumerator RunTrailer()
        {
            Debug.Log("[TrailerCapture] ▶ Trailer capture sequence starting…");
            System.IO.Directory.CreateDirectory(outputFolder);
            if (trailerHUDRoot != null) trailerHUDRoot.SetActive(true);

            for (int i = 0; i < _shots.Count; i++)
            {
                _currentShot   = i;
                _skipRequested = false;
                var (name, shotFunc) = _shots[i];

                Debug.Log($"[TrailerCapture] 🎬 Shot {i + 1}/{_shots.Count}: {name}");
                UpdateHUD(name, i);

                yield return StartCoroutine(shotFunc());

                if (captureScreenshotsPerShot)
                    yield return StartCoroutine(CaptureShot(name));

                yield return new WaitForSeconds(pauseBetweenShots);
            }

            Debug.Log("[TrailerCapture] ✅ All shots complete.");
            _running = false;
            HideHUD();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Individual Shots
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator ShotKingdomFlythrough()
        {
            cinematicCamera?.PlayKingdomEntryCinematic(kingdomRoot);
            yield return TimedShot(shotKingdomFlythrough);
        }

        private IEnumerator ShotWorldExploration()
        {
            cinematicCamera?.PlayFlythroughCinematic();
            yield return TimedShot(shotWorldExploration);
        }

        private IEnumerator ShotMarchMovement()
        {
            // Follow a march banner entity if any are in scene
            var march = FindAnyObjectByType<World.MarchBannerEntity>();
            if (march != null)
                cinematicCamera?.FocusOn(march.transform, Vector3.up * 5f);
            yield return TimedShot(shotMarchMovement);
            cinematicCamera?.ReleaseFocus();
        }

        private IEnumerator ShotMonsterCombat()
        {
            if (monsterTarget != null)
            {
                cinematicCamera?.FocusOn(monsterTarget);
                yield return new WaitForSeconds(2f);
                // Trigger monster spawn VFX for drama
                AlphaVFXController.Instance?.PlayMonsterSpawn(monsterTarget.position);
            }
            yield return TimedShot(shotMonsterCombat);
            cinematicCamera?.ReleaseFocus();
        }

        private IEnumerator ShotSunrise()
        {
            // Force dawn, then fast-forward to day
            WorldEnvironmentManager.Instance?.SetHour(5.5f);
            if (kingdomRoot != null) cinematicCamera?.PlayKingdomEntryCinematic(kingdomRoot);

            float elapsed = 0f;
            while (elapsed < shotSunrise && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                // Accelerate time toward 9h
                WorldEnvironmentManager.Instance?.SetHour(Mathf.Lerp(5.5f, 9f, elapsed / shotSunrise));
                UpdateShotTimer(shotSunrise - elapsed);
                yield return null;
            }
        }

        private IEnumerator ShotNightKingdom()
        {
            WorldEnvironmentManager.Instance?.SetHour(22f);
            WorldEnvironmentManager.Instance?.SetWeather(WeatherType.Clear);
            if (kingdomRoot != null) cinematicCamera?.PlayKingdomEntryCinematic(kingdomRoot);
            yield return TimedShot(shotNightKingdom);
        }

        private IEnumerator ShotStormWeather()
        {
            WorldEnvironmentManager.Instance?.SetWeather(WeatherType.Storm);
            yield return new WaitForSeconds(2f); // let storm build
            cinematicCamera?.PlayFlythroughCinematic();
            yield return TimedShot(shotStormWeather);
            WorldEnvironmentManager.Instance?.SetWeather(WeatherType.Clear);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator TimedShot(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                UpdateShotTimer(duration - elapsed);
                if (shotProgressBar != null) shotProgressBar.fillAmount = elapsed / duration;
                yield return null;
            }
        }

        private IEnumerator CaptureShot(string name)
        {
            yield return new WaitForEndOfFrame();
            string safe = name.Replace(" ", "_");
            string path = System.IO.Path.Combine(outputFolder, $"EK_Trailer_{safe}_{DateTime.Now:HHmmss}.png");
            ScreenCapture.CaptureScreenshot(path, screenshotSuperSize);
            Debug.Log($"[TrailerCapture] 📸 {path}");
        }

        private void UpdateHUD(string name, int index)
        {
            if (shotNameLabel != null) shotNameLabel.text = $"Shot {index + 1}/{_shots.Count}: {name}";
        }

        private void UpdateShotTimer(float remaining)
        {
            if (shotTimerLabel != null) shotTimerLabel.text = $"{remaining:F1}s";
        }

        private void HideHUD()
        {
            if (trailerHUDRoot != null) trailerHUDRoot.SetActive(false);
        }

        public bool IsRunning => _running;
    }
}
