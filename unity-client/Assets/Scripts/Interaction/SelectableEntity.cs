using UnityEngine;
using UnityEngine.EventSystems;

namespace EternalKingdoms.Interaction
{
    /// <summary>
    /// Base component for any entity that can be selected or hovered
    /// in the Kingdom or World scenes.
    ///
    /// Requires a Collider on the same or child GameObject for raycasting.
    ///
    /// Events:
    ///   OnSelected   — first click (or tap) when not already selected
    ///   OnDeselected — another entity selected, or Escape pressed
    ///   OnHovered    — mouse enter / exit
    ///
    /// EntitySelectionManager calls Select() / Deselect() centrally.
    /// </summary>
    public class SelectableEntity : MonoBehaviour
    {
        [Header("Hover Scale")]
        [SerializeField] private bool enableHoverScale = true;
        [SerializeField] private float hoverScaleFactor = 1.05f;
        [SerializeField] private float scaleSpeed = 8f;

        public event System.Action OnSelected;
        public event System.Action OnDeselected;
        public event System.Action<bool> OnHovered;

        public bool IsSelected { get; private set; }
        public bool IsHovered { get; private set; }

        private Vector3 _baseScale;
        private Vector3 _targetScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
        }

        private void Update()
        {
            if (enableHoverScale)
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleSpeed);
        }

        // ── Called by EntitySelectionManager ─────────────────────────────────

        public void Select()
        {
            if (IsSelected) return;
            IsSelected = true;
            OnSelected?.Invoke();
        }

        public void Deselect()
        {
            if (!IsSelected) return;
            IsSelected = false;
            OnDeselected?.Invoke();
        }

        public void SetHovered(bool hovered)
        {
            if (IsHovered == hovered) return;
            IsHovered = hovered;
            _targetScale = hovered ? _baseScale * hoverScaleFactor : _baseScale;
            OnHovered?.Invoke(hovered);
        }

        // ── Mouse events (fallback when EntitySelectionManager uses physics) ──

        private void OnMouseEnter() => EntitySelectionManager.Instance?.NotifyHoverEnter(this);
        private void OnMouseExit() => EntitySelectionManager.Instance?.NotifyHoverExit(this);
        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            EntitySelectionManager.Instance?.NotifyClick(this);
        }
    }
}
