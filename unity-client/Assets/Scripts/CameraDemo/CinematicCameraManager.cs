using System;
using System.Collections;
using UnityEngine;

namespace EternalKingdoms.CameraDemo
{
    /// <summary>
    /// U5.6.6 — Cinematic Camera System
    /// Smooth flythroughs, entity focus, kingdom-entry cinematic, login cinematic,
    /// battle-victory camera, screenshot mode, and scene transition cinematics.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CinematicCameraManager : MonoBehaviour
    {
        public static CinematicCameraManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Flythrough Waypoints")]
        public Transform[] flythroughWaypoints;
        public float       flythroughSpeed     = 8f;
        public AnimationCurve flythroughCurve  = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Focus")]
        [Range(0.5f, 10f)] public float focusSpeed      = 4f;
        [Range(5f,  50f)]  public float focusDistance   = 12f;
        [Range(10f, 60f)]  public float focusFieldOfView = 35f;
        public float defaultFieldOfView = 60f;

        [Header("Kingdom Entry")]
        public float kingdomEntryDuration  = 5f;
        public AnimationCurve kingdomEntryAltitudeCurve;

        [Header("Screenshot Mode")]
        public int    screenshotSuperSize = 4;
        public string screenshotFolder    = "Screenshots";

        [Header("Smoothing")]
        [Range(0f, 1f)] public float positionSmoothing = 0.08f;
        [Range(0f, 1f)] public float rotationSmoothing = 0.06f;

        // ── State ─────────────────────────────────────────────────────────────
        private Camera _cam;
        private Vector3    _targetPos;
        private Quaternion _targetRot;
        private float      _targetFov;

        private bool _isScreenshotMode;
        private bool _isCinematicActive;
        private Coroutine _currentCinematic;

        private Vector3    _posVelocity;
        private float      _fovVelocity;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action OnCinematicStarted;
        public event Action OnCinematicEnded;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _cam = GetComponent<Camera>();
            _targetPos = transform.position;
            _targetRot = transform.rotation;
            _targetFov = defaultFieldOfView;
        }

        private void LateUpdate()
        {
            if (_isCinematicActive) return; // coroutine drives transform directly

            transform.position = Vector3.SmoothDamp(
                transform.position, _targetPos, ref _posVelocity, positionSmoothing);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, _targetRot, Time.deltaTime / rotationSmoothing);
            _cam.fieldOfView = Mathf.SmoothDamp(
                _cam.fieldOfView, _targetFov, ref _fovVelocity, 0.15f);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public — Focus
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Smoothly orbit-focus the camera on a world-space target.</summary>
        public void FocusOn(Transform target, Vector3 offset = default)
        {
            if (target == null) return;
            Vector3 dir = (transform.position - target.position).normalized;
            if (dir == Vector3.zero) dir = Vector3.back;
            _targetPos = target.position + dir * focusDistance + offset;
            _targetRot = Quaternion.LookRotation(target.position - _targetPos);
            _targetFov = focusFieldOfView;
        }

        /// <summary>Release focus and return to default FOV.</summary>
        public void ReleaseFocus() => _targetFov = defaultFieldOfView;

        // ─────────────────────────────────────────────────────────────────────
        //  Public — Cinematics
        // ─────────────────────────────────────────────────────────────────────
        public void PlayLoginCinematic()       => LaunchCinematic(LoginCinematic());
        public void PlayKingdomEntryCinematic(Transform kingdomRoot) =>
            LaunchCinematic(KingdomEntryCinematic(kingdomRoot));
        public void PlayFlythroughCinematic()  => LaunchCinematic(FlythroughCinematic());
        public void PlayBattleVictoryCamera(Transform battleCenter) =>
            LaunchCinematic(BattleVictoryCinematic(battleCenter));
        public void PlayWorldToKingdomTransition(Transform kingdomRoot) =>
            LaunchCinematic(WorldToKingdomTransition(kingdomRoot));

        public void StopCinematic()
        {
            if (_currentCinematic != null) StopCoroutine(_currentCinematic);
            _isCinematicActive = false;
            OnCinematicEnded?.Invoke();
        }

        private void LaunchCinematic(IEnumerator routine)
        {
            if (_currentCinematic != null) StopCoroutine(_currentCinematic);
            _currentCinematic = StartCoroutine(CinematicWrapper(routine));
        }

        private IEnumerator CinematicWrapper(IEnumerator inner)
        {
            _isCinematicActive = true;
            OnCinematicStarted?.Invoke();
            yield return StartCoroutine(inner);
            _isCinematicActive = false;
            OnCinematicEnded?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Cinematics — Implementations
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator LoginCinematic()
        {
            // Slow aerial pan across the world before login screen appears
            Vector3 start = transform.position + Vector3.up * 60f + Vector3.forward * -80f;
            Vector3 end   = start + Vector3.right * 120f;
            float   t     = 0f;
            float   dur   = 8f;

            transform.position = start;
            transform.rotation = Quaternion.Euler(25f, -10f, 0f);
            _cam.fieldOfView   = 50f;

            while (t < dur)
            {
                t += Time.deltaTime;
                float n = flythroughCurve.Evaluate(t / dur);
                transform.position = Vector3.Lerp(start, end, n);
                // Gentle banking
                transform.rotation = Quaternion.Euler(25f + Mathf.Sin(n * Mathf.PI) * 4f, -10f + n * 30f, 0f);
                yield return null;
            }
        }

        private IEnumerator KingdomEntryCinematic(Transform root)
        {
            if (root == null) yield break;
            Vector3 center = root.position;

            // Start high, arc down to front-gate level
            float   altStart = 80f;
            float   altEnd   = 18f;
            float   radius   = 40f;
            float   t        = 0f;

            while (t < kingdomEntryDuration)
            {
                t += Time.deltaTime;
                float n = flythroughCurve.Evaluate(t / kingdomEntryDuration);

                float angle = Mathf.Lerp(200f, 340f, n) * Mathf.Deg2Rad;
                float alt   = kingdomEntryAltitudeCurve != null
                    ? Mathf.Lerp(altStart, altEnd, kingdomEntryAltitudeCurve.Evaluate(n))
                    : Mathf.Lerp(altStart, altEnd, n);

                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, alt, Mathf.Sin(angle) * radius);
                transform.position = pos;
                transform.LookAt(center + Vector3.up * 5f);
                _cam.fieldOfView = Mathf.Lerp(50f, defaultFieldOfView, n);
                yield return null;
            }
        }

        private IEnumerator FlythroughCinematic()
        {
            if (flythroughWaypoints == null || flythroughWaypoints.Length < 2) yield break;

            for (int i = 0; i < flythroughWaypoints.Length - 1; i++)
            {
                Vector3 from  = flythroughWaypoints[i].position;
                Vector3 to    = flythroughWaypoints[i + 1].position;
                float   dist  = Vector3.Distance(from, to);
                float   dur   = dist / flythroughSpeed;
                float   t     = 0f;

                Quaternion fromRot = flythroughWaypoints[i].rotation;
                Quaternion toRot   = flythroughWaypoints[i + 1].rotation;

                while (t < dur)
                {
                    t += Time.deltaTime;
                    float n = flythroughCurve.Evaluate(t / dur);
                    transform.position = Vector3.Lerp(from, to, n);
                    transform.rotation = Quaternion.Slerp(fromRot, toRot, n);
                    yield return null;
                }
            }
        }

        private IEnumerator BattleVictoryCinematic(Transform center)
        {
            if (center == null) yield break;
            // Dramatic zoom-in then pull back orbit
            float t = 0f;
            float dur = 6f;
            Vector3 startPos = center.position + new Vector3(0, 25f, -30f);
            Vector3 endPos   = center.position + new Vector3(0, 40f, -55f);

            while (t < dur)
            {
                t += Time.deltaTime;
                float n = flythroughCurve.Evaluate(t / dur);
                // Phase 1 (0-0.4): zoom in fast
                // Phase 2 (0.4-1): pull back orbiting
                if (n < 0.4f)
                {
                    float nn = n / 0.4f;
                    transform.position = Vector3.Lerp(startPos, center.position + Vector3.up * 8f, nn);
                    _cam.fieldOfView   = Mathf.Lerp(defaultFieldOfView, 20f, nn);
                }
                else
                {
                    float nn = (n - 0.4f) / 0.6f;
                    float angle = Mathf.Lerp(0f, 360f, nn) * Mathf.Deg2Rad;
                    transform.position = Vector3.Lerp(
                        center.position + Vector3.up * 8f,
                        endPos + new Vector3(Mathf.Cos(angle) * 20f, 0, Mathf.Sin(angle) * 20f),
                        nn);
                    _cam.fieldOfView = Mathf.Lerp(20f, defaultFieldOfView, nn);
                }
                transform.LookAt(center.position + Vector3.up * 3f);
                yield return null;
            }
        }

        private IEnumerator WorldToKingdomTransition(Transform kingdomRoot)
        {
            // Fade-style: zoom into kingdom center rapidly then hand off to scene load
            float t = 0f, dur = 2.5f;
            Vector3 startPos = transform.position;
            Vector3 target   = kingdomRoot != null ? kingdomRoot.position + Vector3.up * 5f : Vector3.zero;

            while (t < dur)
            {
                t += Time.deltaTime;
                float n = flythroughCurve.Evaluate(t / dur);
                transform.position = Vector3.Lerp(startPos, target, n);
                _cam.fieldOfView   = Mathf.Lerp(defaultFieldOfView, 10f, n);
                transform.LookAt(target);
                yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Screenshot Mode
        // ─────────────────────────────────────────────────────────────────────
        public void ToggleScreenshotMode()
        {
            _isScreenshotMode = !_isScreenshotMode;
            // Hide HUD canvas
            var uiCanvas = FindAnyObjectByType<Canvas>();
            if (uiCanvas != null) uiCanvas.enabled = !_isScreenshotMode;
            Debug.Log(_isScreenshotMode ? "[Camera] 📸 Screenshot mode ON" : "[Camera] Screenshot mode OFF");
        }

        public void CaptureScreenshot()
        {
            System.IO.Directory.CreateDirectory(screenshotFolder);
            string path = $"{screenshotFolder}/EK_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            ScreenCapture.CaptureScreenshot(path, screenshotSuperSize);
            Debug.Log($"[Camera] Screenshot saved → {path}");
        }

        // ── Public State ──────────────────────────────────────────────────────
        public bool IsCinematicActive  => _isCinematicActive;
        public bool IsScreenshotMode   => _isScreenshotMode;
    }
}
