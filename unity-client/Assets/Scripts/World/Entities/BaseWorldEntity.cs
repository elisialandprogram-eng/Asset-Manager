using UnityEngine;
using EternalKingdoms.World.Grid;
using EternalKingdoms.Interaction;

namespace EternalKingdoms.World.Entities
{
    /// <summary>
    /// Base class for all world map entities (kingdoms, monsters, crystals).
    ///
    /// Provides:
    ///   - World-space positioning via WorldCoordinate
    ///   - SelectableEntity integration (hover + click)
    ///   - LOD visibility toggle based on camera distance
    ///   - Selection ring visual
    ///   - Common recycle / pool lifecycle hooks
    ///
    /// Subclasses override Initialize() and Refresh() to apply
    /// type-specific data and visuals.
    /// </summary>
    [RequireComponent(typeof(SelectableEntity))]
    public abstract class BaseWorldEntity : MonoBehaviour
    {
        [Header("Selection")]
        [SerializeField] protected GameObject selectionRing;
        [SerializeField] protected GameObject hoverRing;

        [Header("LOD")]
        [SerializeField] protected float lodFarDistance = 400f;  // units — hide detail mesh
        [SerializeField] protected GameObject detailMesh;
        [SerializeField] protected GameObject farMesh;

        [Header("Label")]
        [SerializeField] protected TMPro.TextMeshProUGUI nameLabel;

        protected SelectableEntity _selectable;
        protected WorldCoordinate _coord;
        protected string _entityId;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            _selectable = GetComponent<SelectableEntity>();
            _selectable.OnSelected   += OnEntitySelected;
            _selectable.OnDeselected += OnEntityDeselected;
            _selectable.OnHovered    += OnEntityHovered;
            SetSelectionRing(false);
            SetHoverRing(false);
        }

        protected virtual void OnDestroy()
        {
            if (_selectable != null)
            {
                _selectable.OnSelected   -= OnEntitySelected;
                _selectable.OnDeselected -= OnEntityDeselected;
                _selectable.OnHovered    -= OnEntityHovered;
            }
        }

        // ── Pool lifecycle ────────────────────────────────────────────────────

        public virtual void Recycle()
        {
            _selectable?.Deselect();
            SetSelectionRing(false);
            SetHoverRing(false);
            gameObject.SetActive(false);
        }

        // ── Positioning ───────────────────────────────────────────────────────

        protected void SetPosition(WorldCoordinate coord)
        {
            _coord = coord;
            transform.position = coord.ToUnityCenter();
        }

        // ── Interaction callbacks ─────────────────────────────────────────────

        private void OnEntitySelected()
        {
            SetSelectionRing(true);
            OnSelected();
        }

        private void OnEntityDeselected()
        {
            SetSelectionRing(false);
            OnDeselected();
        }

        private void OnEntityHovered(bool hovered)
        {
            SetHoverRing(hovered);
            OnHovered(hovered);
        }

        protected virtual void OnSelected()   { }
        protected virtual void OnDeselected() { }
        protected virtual void OnHovered(bool hovered) { }

        // ── Visual helpers ────────────────────────────────────────────────────

        protected void SetSelectionRing(bool v) { if (selectionRing != null) selectionRing.SetActive(v); }
        protected void SetHoverRing(bool v)     { if (hoverRing     != null) hoverRing.SetActive(v); }

        protected void UpdateLOD(float cameraDistance)
        {
            bool near = cameraDistance < lodFarDistance;
            if (detailMesh != null) detailMesh.SetActive(near);
            if (farMesh    != null) farMesh.SetActive(!near);
        }

        protected void SetNameLabel(string text)
        {
            if (nameLabel != null) nameLabel.text = text;
        }
    }
}
