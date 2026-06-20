using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using EternalKingdoms.Environment;

namespace EternalKingdoms.CameraDemo
{
    /// <summary>
    /// U5.7.7 — Photo Mode
    /// Free-camera photo mode for marketing screenshots.
    /// Hides all UI, unlocks camera movement, exposes time-of-day and
    /// weather controls, and captures 4K screenshots.
    /// Toggle: F8.  WASD/QE = move, Mouse = look, Scroll = FOV.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PhotoModeController : MonoBehaviour
    {
        public static PhotoModeController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Movement")]
        public float moveSpeed    = 20f;
        public float fastSpeed    = 60f;
        public float lookSensitivity = 2f;
        public float rollSpeed    = 30f;

        [Header("FOV")]
        [Range(10f, 120f)] public float minFOV = 10f;
        [Range(10f, 120f)] public float maxFOV = 120f;
        public float fovScrollSpeed = 4f;

        [Header("Depth of Field")]
        public bool enableDOF = true;
        [Range(0.1f, 100f)] public float focusDistance = 20f;
        [Range(0.5f, 32f)]  public float aperture      = 5.6f;

        [Header("Screenshot")]
        public int    superSize     = 4;
        public string outputFolder  = "PhotoMode";
        public string filenamePrefix = "EK";

        [Header("Photo Mode HUD")]
        public GameObject photoModeHUDRoot;
        public TMPro.TextMeshProUGUI fovLabel;
        public TMPro.TextMeshProUGUI timeLabel;
        public TMPro.TextMeshProUGUI weatherLabel;
        public TMPro.TextMeshProUGUI coordinatesLabel;

        // ── State ─────────────────────────────────────────────────────────────
        private Camera   _cam;
        private bool     _active;
        private float    _yaw;
        private float    _pitch;
        private float    _roll;
        private Vector3  _savedPosition;
        private Quaternion _savedRotation;
        private float    _savedFOV;
        private Canvas[] _hiddenCanvases;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<bool> OnPhotoModeToggled;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _cam = GetComponent<Camera>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) Toggle();
            if (!_active) return;

            HandleMovement();
            HandleLook();
            HandleFOV();
            HandleEnvironmentControls();
            UpdateHUD();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.P))
                StartCoroutine(CaptureScreenshot());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Toggle
        // ─────────────────────────────────────────────────────────────────────
        public void Toggle()
        {
            _active = !_active;

            if (_active)
            {
                // Save camera state
                _savedPosition = transform.position;
                _savedRotation = transform.rotation;
                _savedFOV      = _cam.fieldOfView;
                _yaw   = transform.eulerAngles.y;
                _pitch = transform.eulerAngles.x;
                _roll  = 0f;

                // Hide all game UI (keep photo HUD)
                _hiddenCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var c in _hiddenCanvases)
                    if (c.gameObject != photoModeHUDRoot && c.enabled)
                    {
                        c.enabled = false;
                    }

                if (photoModeHUDRoot != null) photoModeHUDRoot.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;

                Debug.Log("[PhotoMode] 📸 Entered. WASD=move, Mouse=look, Scroll=FOV, Enter=capture, Q/E=up/down, Z/C=roll.");
            }
            else
            {
                // Restore
                transform.position    = _savedPosition;
                transform.rotation    = _savedRotation;
                _cam.fieldOfView      = _savedFOV;

                if (_hiddenCanvases != null)
                    foreach (var c in _hiddenCanvases) if (c != null) c.enabled = true;

                if (photoModeHUDRoot != null) photoModeHUDRoot.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;

                Debug.Log("[PhotoMode] Exited photo mode.");
            }

            OnPhotoModeToggled?.Invoke(_active);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Controls
        // ─────────────────────────────────────────────────────────────────────
        private void HandleMovement()
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : moveSpeed;
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;

            transform.position += move * speed * Time.unscaledDeltaTime;
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            _yaw   += mouseX;
            _pitch -= mouseY;
            _pitch  = Mathf.Clamp(_pitch, -89f, 89f);

            if (Input.GetKey(KeyCode.Z)) _roll += rollSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.C)) _roll -= rollSpeed * Time.unscaledDeltaTime;

            transform.rotation = Quaternion.Euler(_pitch, _yaw, _roll);
        }

        private void HandleFOV()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _cam.fieldOfView = Mathf.Clamp(_cam.fieldOfView - scroll * fovScrollSpeed * 10f, minFOV, maxFOV);
        }

        private void HandleEnvironmentControls()
        {
            if (WorldEnvironmentManager.Instance == null) return;

            // Time of day: [ = earlier, ] = later
            if (Input.GetKey(KeyCode.LeftBracket))
                WorldEnvironmentManager.Instance.SetHour(WorldEnvironmentManager.Instance.CurrentHour - Time.unscaledDeltaTime * 2f);
            if (Input.GetKey(KeyCode.RightBracket))
                WorldEnvironmentManager.Instance.SetHour(WorldEnvironmentManager.Instance.CurrentHour + Time.unscaledDeltaTime * 2f);

            // Weather: number keys 1–6
            if (Input.GetKeyDown(KeyCode.Alpha1)) WorldEnvironmentManager.Instance.SetWeather(WeatherType.Clear);
            if (Input.GetKeyDown(KeyCode.Alpha2)) WorldEnvironmentManager.Instance.SetWeather(WeatherType.Rain);
            if (Input.GetKeyDown(KeyCode.Alpha3)) WorldEnvironmentManager.Instance.SetWeather(WeatherType.Storm);
            if (Input.GetKeyDown(KeyCode.Alpha4)) WorldEnvironmentManager.Instance.SetWeather(WeatherType.Snow);
            if (Input.GetKeyDown(KeyCode.Alpha5)) WorldEnvironmentManager.Instance.SetWeather(WeatherType.Fog);
            if (Input.GetKeyDown(KeyCode.Alpha6)) WorldEnvironmentManager.Instance.SetWeather(WeatherType.Ashfall);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HUD
        // ─────────────────────────────────────────────────────────────────────
        private void UpdateHUD()
        {
            if (fovLabel        != null) fovLabel.text         = $"FOV: {_cam.fieldOfView:F0}°";
            if (coordinatesLabel != null) coordinatesLabel.text = $"Pos: {transform.position:F1}";

            var env = WorldEnvironmentManager.Instance;
            if (env != null)
            {
                if (timeLabel    != null) timeLabel.text    = $"Time: {env.CurrentHour:F1}h ({env.CurrentPhase})";
                if (weatherLabel != null) weatherLabel.text = $"Weather: {env.CurrentWeather}";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Screenshot
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator CaptureScreenshot()
        {
            // Hide photo HUD for a clean shot
            if (photoModeHUDRoot != null) photoModeHUDRoot.SetActive(false);

            yield return new WaitForEndOfFrame();

            Directory.CreateDirectory(outputFolder);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine(outputFolder, $"{filenamePrefix}_{timestamp}.png");
            ScreenCapture.CaptureScreenshot(path, superSize);

            if (photoModeHUDRoot != null) photoModeHUDRoot.SetActive(true);

            Debug.Log($"[PhotoMode] 📸 Screenshot saved → {path} ({superSize * Screen.width}×{superSize * Screen.height})");
        }

        // ── Public API ────────────────────────────────────────────────────────
        public bool IsActive => _active;
    }
}
