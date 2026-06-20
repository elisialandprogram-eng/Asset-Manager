using UnityEngine;

namespace EternalKingdoms.Interaction
{
    /// <summary>
    /// Scene-level singleton that owns the single-selection state.
    /// At most one entity can be selected at a time.
    ///
    /// Flow:
    ///   SelectableEntity.OnMouseDown → NotifyClick
    ///   → Deselect previous → Select new
    ///   → Broadcast OnEntitySelected event for UI panels
    ///
    /// Also handles Escape to deselect.
    /// </summary>
    public class EntitySelectionManager : MonoBehaviour
    {
        public static EntitySelectionManager Instance { get; private set; }

        private SelectableEntity _selected;
        private SelectableEntity _hovered;

        public SelectableEntity Selected => _selected;
        public SelectableEntity Hovered => _hovered;

        public event System.Action<SelectableEntity> OnEntitySelected;
        public event System.Action OnEntityDeselected;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                DeselectAll();
        }

        // ── Called by SelectableEntity ────────────────────────────────────────

        public void NotifyClick(SelectableEntity entity)
        {
            if (entity == _selected)
            {
                // Click on already-selected — deselect
                DeselectAll();
                return;
            }

            _selected?.Deselect();
            _selected = entity;
            _selected.Select();
            OnEntitySelected?.Invoke(_selected);
        }

        public void NotifyHoverEnter(SelectableEntity entity)
        {
            if (_hovered == entity) return;
            _hovered?.SetHovered(false);
            _hovered = entity;
            _hovered.SetHovered(true);
        }

        public void NotifyHoverExit(SelectableEntity entity)
        {
            if (_hovered == entity)
            {
                _hovered.SetHovered(false);
                _hovered = null;
            }
        }

        public void DeselectAll()
        {
            _selected?.Deselect();
            _selected = null;
            OnEntityDeselected?.Invoke();
        }
    }
}
