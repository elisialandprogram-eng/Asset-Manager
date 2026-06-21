using UnityEngine;
using UnityEngine.InputSystem;

namespace EternalKingdoms.Cameras
{
    /// <summary>
    /// Shared isometric camera controller for World and Kingdom scenes.
    ///
    /// Features:
    /// - Fixed isometric angle (X=60°, Y=-45°)
    /// - Drag pan (mouse / touch)
    /// - Scroll / pinch zoom
    /// - Inertia (pan momentum after drag release)
    /// - Bounds clamping — camera pivot cannot leave set boundaries
    /// - Smooth damping on all movements
    ///
    /// Subclasses set PanBoundsMin/Max and zoom limits in Awake().
    /// Uses the new Unity Input System.
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        // ── Settings (overridable by subclasses) ──────────────────────────────

        protected Vector2 PanBoundsMin = new(-500f, -500f);
        protected Vector2 PanBoundsMax = new(500f, 500f);
        protected float MinOrthographicSize = 5f;
        protected float MaxOrthographicSize = 100f;
        protected float CurrentOrthographicSize = 30f;

        [Header("Pan Settings")]
        [SerializeField] protected float panSensitivity = 0.5f;
        [SerializeField] protected float inertiaDecay = 8f;
        [SerializeField] protected float panSmoothTime = 0.1f;

        [Header("Zoom Settings")]
        [SerializeField] protected float scrollZoomSensitivity = 3f;
        [SerializeField] protected float pinchZoomSensitivity = 0.02f;
        [SerializeField] protected float zoomSmoothTime = 0.15f;

        // ── Runtime state ─────────────────────────────────────────────────────

        private Vector3 _pivotPosition;           // target pivot (XZ plane)
        private Vector3 _pivotVelocity;           // for SmoothDamp
        private float _targetZoom;
        private float _zoomVelocity;

        private bool _isDragging;
        private Vector3 _dragStartWorldPos;
        private Vector3 _dragStartPivot;
        private Vector2 _inertiaVelocity;

        private float _lastPinchDistance;
        private bool _isPinching;

        private UnityEngine.Camera _cam;

        // ── Fixed isometric angle ─────────────────────────────────────────────
        private const float ISO_ANGLE_X = 60f;
        private const float ISO_ANGLE_Y = -45f;

        protected virtual void Awake()
        {
            _cam = GetComponentInChildren<UnityEngine.Camera>() ?? UnityEngine.Camera.main;
            _targetZoom = CurrentOrthographicSize;
        }

        protected virtual void Start()
        {
            // Set fixed isometric rotation
            transform.rotation = Quaternion.Euler(ISO_ANGLE_X, ISO_ANGLE_Y, 0f);

            _pivotPosition = transform.position;
            _pivotPosition.y = 0f;
            _targetZoom = CurrentOrthographicSize;

            if (_cam != null)
            {
                _cam.orthographic = true;
                _cam.orthographicSize = _targetZoom;
            }
        }

        protected virtual void Update()
        {
            HandleScrollZoom();
            HandleDragPan();
            HandlePinchZoom();
            ApplyInertia();
            ApplySmoothing();
        }

        // ── Drag pan ──────────────────────────────────────────────────────────

        private void HandleDragPan()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.middleButton.wasPressedThisFrame ||
                (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI()))
            {
                _isDragging = true;
                _inertiaVelocity = Vector2.zero;
                _dragStartWorldPos = ScreenToGroundPlane(Mouse.current.position.ReadValue());
                _dragStartPivot = _pivotPosition;
            }

            if (_isDragging && (Mouse.current.middleButton.isPressed || Mouse.current.leftButton.isPressed))
            {
                Vector3 currentWorld = ScreenToGroundPlane(Mouse.current.position.ReadValue());
                Vector3 delta = _dragStartWorldPos - currentWorld;

                Vector3 newPivot = _dragStartPivot + delta;
                _inertiaVelocity = new Vector2(delta.x, delta.z) * Time.deltaTime * 10f;
                SetPivotRaw(newPivot);
            }

            if (_isDragging && (Mouse.current.middleButton.wasReleasedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame))
            {
                _isDragging = false;
            }
        }

        // ── Scroll zoom ───────────────────────────────────────────────────────

        private void HandleScrollZoom()
        {
            if (Mouse.current == null) return;
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            _targetZoom -= scroll * scrollZoomSensitivity;
            _targetZoom = Mathf.Clamp(_targetZoom, MinOrthographicSize, MaxOrthographicSize);
        }

        // ── Pinch zoom (touch) ────────────────────────────────────────────────

        private void HandlePinchZoom()
        {
            if (Touchscreen.current == null || Touchscreen.current.touches.Count < 2) { _isPinching = false; return; }

            var t0 = Touchscreen.current.touches[0].position.ReadValue();
            var t1 = Touchscreen.current.touches[1].position.ReadValue();
            float dist = Vector2.Distance(t0, t1);

            if (!_isPinching)
            {
                _lastPinchDistance = dist;
                _isPinching = true;
                return;
            }

            float delta = _lastPinchDistance - dist;
            _targetZoom += delta * pinchZoomSensitivity;
            _targetZoom = Mathf.Clamp(_targetZoom, MinOrthographicSize, MaxOrthographicSize);
            _lastPinchDistance = dist;
        }

        // ── Inertia ───────────────────────────────────────────────────────────

        private void ApplyInertia()
        {
            if (_isDragging) return;
            if (_inertiaVelocity.sqrMagnitude < 0.001f) return;
            _inertiaVelocity = Vector2.Lerp(_inertiaVelocity, Vector2.zero, inertiaDecay * Time.deltaTime);
            Vector3 offset = new(_inertiaVelocity.x, 0, _inertiaVelocity.y);
            SetPivotRaw(_pivotPosition + offset);
        }

        // ── Smoothing ─────────────────────────────────────────────────────────

        private void ApplySmoothing()
        {
            // Smooth pan
            transform.position = Vector3.SmoothDamp(
                transform.position,
                _pivotPosition + Vector3.up * 50f,  // offset camera above pivot
                ref _pivotVelocity,
                panSmoothTime);

            // Smooth zoom
            if (_cam != null)
            {
                _cam.orthographicSize = Mathf.SmoothDamp(
                    _cam.orthographicSize,
                    _targetZoom,
                    ref _zoomVelocity,
                    zoomSmoothTime);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetPivot(Vector3 worldPos)
        {
            worldPos.y = 0f;
            _pivotPosition = ClampPivot(worldPos);
        }

        private void SetPivotRaw(Vector3 pos)
        {
            pos.y = 0f;
            _pivotPosition = ClampPivot(pos);
        }

        private Vector3 ClampPivot(Vector3 pos) => new(
            Mathf.Clamp(pos.x, PanBoundsMin.x, PanBoundsMax.x),
            0f,
            Mathf.Clamp(pos.z, PanBoundsMin.y, PanBoundsMax.y));

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector3 ScreenToGroundPlane(Vector2 screenPos)
        {
            if (_cam == null) return Vector3.zero;
            Ray ray = _cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        private bool IsPointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}
