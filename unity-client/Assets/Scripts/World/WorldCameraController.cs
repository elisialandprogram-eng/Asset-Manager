using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using EternalKingdoms.World.Grid;
using EternalKingdoms.World.UI;

namespace EternalKingdoms.World
{
    /// <summary>
    /// World scene camera controller.
    ///
    /// Fixed isometric angle: X=60°, Y=−45° (UNITY_WORLD_SYSTEM.md).
    ///
    /// Controls:
    ///   Drag pan (left mouse / touch)
    ///   Edge scroll (mouse near screen edge)
    ///   WASD keyboard pan
    ///   Scroll wheel zoom
    ///   Pinch zoom (two-finger touch)
    ///   Inertia + smooth damping
    ///   FlyTo(coord) — smooth animated travel to world coordinate
    ///
    /// Bounds: full world extent ±5120 units XZ.
    /// Zoom:   orthographic size 10 (close) – 120 (zoomed out).
    ///
    /// The WorldHUD subscribes to OnCoordChanged to update the
    /// coordinate display.
    /// </summary>
    public class WorldCameraController : MonoBehaviour
    {
        // ── Fixed isometric angle ─────────────────────────────────────────────
        private const float ISO_X = 60f;
        private const float ISO_Y = -45f;

        [Header("Pan Bounds (world units)")]
        [SerializeField] private float boundsXMin = -5120f;
        [SerializeField] private float boundsXMax =  5120f;
        [SerializeField] private float boundsZMin = -5120f;
        [SerializeField] private float boundsZMax =  5120f;

        [Header("Zoom")]
        [SerializeField] private float minOrthoSize =  10f;
        [SerializeField] private float maxOrthoSize = 120f;
        [SerializeField] private float initialOrthoSize = 50f;
        [SerializeField] private float scrollSensitivity = 6f;
        [SerializeField] private float pinchSensitivity  = 0.025f;
        [SerializeField] private float zoomSmoothTime    = 0.15f;

        [Header("Pan")]
        [SerializeField] private float dragSensitivity    = 1.0f;
        [SerializeField] private float keyboardPanSpeed   = 80f;  // units/sec
        [SerializeField] private float edgeScrollSpeed    = 60f;
        [SerializeField] private float edgeScrollThreshold = 0.02f; // fraction of screen
        [SerializeField] private float panSmoothTime      = 0.08f;
        [SerializeField] private float inertiaDecay       = 6f;

        [Header("FlyTo")]
        [SerializeField] private float flyToSpeed = 200f; // units/sec

        // ── Runtime ───────────────────────────────────────────────────────────
        private UnityEngine.Camera _cam;
        private Vector3 _pivot;
        private Vector3 _pivotVelocity;
        private float   _targetZoom;
        private float   _zoomVelocity;

        private bool    _isDragging;
        private Vector3 _dragStartWorld;
        private Vector3 _dragStartPivot;
        private Vector2 _inertia;

        private float _lastPinchDist;
        private bool  _isPinching;

        private Coroutine _flyCoroutine;

        // Event for WorldHUD coordinate display
        public event System.Action<WorldCoordinate> OnCoordChanged;

        private WorldCoordinate _lastEmittedCoord;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _cam = GetComponentInChildren<UnityEngine.Camera>() ?? UnityEngine.Camera.main;
        }

        private void Start()
        {
            transform.rotation      = Quaternion.Euler(ISO_X, ISO_Y, 0f);
            _targetZoom             = initialOrthoSize;
            _pivot                  = Vector3.zero;

            if (_cam != null)
            {
                _cam.orthographic     = true;
                _cam.orthographicSize = _targetZoom;
            }
        }

        private void Update()
        {
            HandleKeyboardPan();
            HandleEdgeScroll();
            HandleDragPan();
            HandleScrollZoom();
            HandlePinchZoom();
            ApplyInertia();
            ApplySmoothing();
            EmitCoordIfChanged();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public UnityEngine.Camera GetCamera() => _cam;

        /// <summary>Instantly or smoothly move camera to a Unity world position.</summary>
        public void FlyTo(Vector3 worldPos, bool immediate = false)
        {
            worldPos.y = 0f;
            worldPos   = ClampPivot(worldPos);

            if (_flyCoroutine != null) StopCoroutine(_flyCoroutine);

            if (immediate)
            {
                _pivot = worldPos;
                transform.position = _pivot + Vector3.up * 80f;
            }
            else
            {
                _flyCoroutine = StartCoroutine(FlyCoroutine(worldPos));
            }
        }

        public void FlyTo(WorldCoordinate coord, bool immediate = false) =>
            FlyTo(coord.ToUnityCenter(), immediate);

        public void CenterOnKingdom(WorldCoordinate coord) => FlyTo(coord);

        // ── Drag pan ──────────────────────────────────────────────────────────

        private void HandleDragPan()
        {
            if (Mouse.current == null) return;

            bool pressed = Mouse.current.leftButton.isPressed || Mouse.current.middleButton.isPressed;
            bool wasPressed = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame;

            if (wasPressed && !IsPointerOverUI())
            {
                _isDragging = true;
                _inertia = Vector2.zero;
                _dragStartWorld = ScreenToGround(Mouse.current.position.ReadValue());
                _dragStartPivot = _pivot;
            }

            if (_isDragging && pressed)
            {
                Vector3 cur = ScreenToGround(Mouse.current.position.ReadValue());
                Vector3 delta = (_dragStartWorld - cur) * dragSensitivity;
                _inertia = new Vector2(delta.x, delta.z) * Time.deltaTime * 12f;
                SetPivot(_dragStartPivot + delta);
            }

            if (_isDragging && !pressed) _isDragging = false;
        }

        // ── Keyboard pan ──────────────────────────────────────────────────────

        private void HandleKeyboardPan()
        {
            if (Keyboard.current == null) return;
            Vector3 dir = Vector3.zero;
            float speed = keyboardPanSpeed * Time.deltaTime;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)   dir.z += speed;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  dir.z -= speed;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) dir.x += speed;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  dir.x -= speed;

            if (dir.sqrMagnitude > 0.001f) SetPivot(_pivot + dir);
        }

        // ── Edge scroll ───────────────────────────────────────────────────────

        private void HandleEdgeScroll()
        {
            if (Mouse.current == null || _isDragging) return;
            var mp = Mouse.current.position.ReadValue();
            float sw = Screen.width;
            float sh = Screen.height;
            float t  = edgeScrollThreshold;
            Vector3 dir = Vector3.zero;
            float speed = edgeScrollSpeed * Time.deltaTime;

            if (mp.x < sw * t)       dir.x -= speed;
            if (mp.x > sw * (1 - t)) dir.x += speed;
            if (mp.y < sh * t)       dir.z -= speed;
            if (mp.y > sh * (1 - t)) dir.z += speed;

            if (dir.sqrMagnitude > 0.001f) SetPivot(_pivot + dir);
        }

        // ── Scroll zoom ───────────────────────────────────────────────────────

        private void HandleScrollZoom()
        {
            if (Mouse.current == null) return;
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            _targetZoom -= scroll * scrollSensitivity;
            _targetZoom = Mathf.Clamp(_targetZoom, minOrthoSize, maxOrthoSize);
        }

        // ── Pinch zoom ────────────────────────────────────────────────────────

        private void HandlePinchZoom()
        {
            if (Touchscreen.current == null || Touchscreen.current.touches.Count < 2)
            { _isPinching = false; return; }

            var t0 = Touchscreen.current.touches[0].position.ReadValue();
            var t1 = Touchscreen.current.touches[1].position.ReadValue();
            float dist = Vector2.Distance(t0, t1);

            if (!_isPinching) { _lastPinchDist = dist; _isPinching = true; return; }
            float delta = (_lastPinchDist - dist) * pinchSensitivity;
            _targetZoom = Mathf.Clamp(_targetZoom + delta, minOrthoSize, maxOrthoSize);
            _lastPinchDist = dist;
        }

        // ── Inertia ───────────────────────────────────────────────────────────

        private void ApplyInertia()
        {
            if (_isDragging || _inertia.sqrMagnitude < 0.001f) return;
            _inertia = Vector2.Lerp(_inertia, Vector2.zero, inertiaDecay * Time.deltaTime);
            SetPivot(_pivot + new Vector3(_inertia.x, 0f, _inertia.y));
        }

        // ── Smoothing ─────────────────────────────────────────────────────────

        private void ApplySmoothing()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, _pivot + Vector3.up * 80f, ref _pivotVelocity, panSmoothTime);

            if (_cam != null)
                _cam.orthographicSize = Mathf.SmoothDamp(
                    _cam.orthographicSize, _targetZoom, ref _zoomVelocity, zoomSmoothTime);
        }

        // ── FlyTo coroutine ───────────────────────────────────────────────────

        private IEnumerator FlyCoroutine(Vector3 target)
        {
            while (Vector3.Distance(_pivot, target) > 1f)
            {
                _pivot = Vector3.MoveTowards(_pivot, target, flyToSpeed * Time.deltaTime);
                yield return null;
            }
            _pivot = target;
            _flyCoroutine = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetPivot(Vector3 pos) => _pivot = ClampPivot(pos);

        private Vector3 ClampPivot(Vector3 p) => new(
            Mathf.Clamp(p.x, boundsXMin, boundsXMax),
            0f,
            Mathf.Clamp(p.z, boundsZMin, boundsZMax));

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (_cam == null) return Vector3.zero;
            var ray = _cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0));
            var plane = new Plane(Vector3.up, Vector3.zero);
            return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : Vector3.zero;
        }

        private void EmitCoordIfChanged()
        {
            var cur = WorldCoordinate.FromUnity(transform.position);
            if (cur != _lastEmittedCoord)
            {
                _lastEmittedCoord = cur;
                OnCoordChanged?.Invoke(cur);
            }
        }

        private bool IsPointerOverUI() =>
            UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}
