using UnityEngine;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.Interaction;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// Represents a single building slot in the Kingdom scene.
    ///
    /// Fixed-position nodes — every kingdom has identical slot positions.
    /// Slot renders whatever building the player has constructed via the API.
    ///
    /// Hierarchy per slot:
    ///   BuildingSlot_[TYPE]
    ///   ├── Placeholder (mesh — palace, farm, etc.)
    ///   ├── EmptyMarker (shown when slot is empty)
    ///   ├── SelectionRing
    ///   └── NameLabel (TextMeshPro)
    ///
    /// Building types map 1:1 with the backend buildingType string.
    /// </summary>
    [RequireComponent(typeof(SelectableEntity))]
    public class BuildingSlot : MonoBehaviour
    {
        [Header("Slot Identity")]
        [SerializeField] private string slotType;     // matches backend buildingType
        [SerializeField] private string displayName;

        [Header("Visual")]
        [SerializeField] private GameObject placeholderMesh;
        [SerializeField] private GameObject emptyMarker;
        [SerializeField] private GameObject selectionRing;
        [SerializeField] private TMPro.TextMeshProUGUI nameLabel;
        [SerializeField] private TMPro.TextMeshProUGUI statusLabel;

        // Live data from API
        private BuildingDto _buildingData;
        private bool _isEmpty = true;

        public string SlotType => slotType;
        public BuildingDto BuildingData => _buildingData;
        public bool IsEmpty => _isEmpty;

        private SelectableEntity _selectable;

        private void Awake()
        {
            _selectable = GetComponent<SelectableEntity>();
            _selectable.OnSelected += OnSelected;
            _selectable.OnDeselected += OnDeselected;
            _selectable.OnHovered += OnHovered;
            SetSelectionRing(false);
        }

        private void Start()
        {
            // Subscribe to state updates
            if (KingdomStateManager.Instance != null)
                KingdomStateManager.Instance.OnStateRefreshed += OnStateRefreshed;
        }

        private void OnDestroy()
        {
            if (_selectable != null)
            {
                _selectable.OnSelected -= OnSelected;
                _selectable.OnDeselected -= OnDeselected;
                _selectable.OnHovered -= OnHovered;
            }
            if (KingdomStateManager.Instance != null)
                KingdomStateManager.Instance.OnStateRefreshed -= OnStateRefreshed;
        }

        // ── State refresh ─────────────────────────────────────────────────────

        private void OnStateRefreshed(KingdomStateDto state)
        {
            if (state.buildings == null) return;
            BuildingDto found = null;
            foreach (var b in state.buildings)
            {
                if (b.buildingType == slotType && !b.isConstructing)
                {
                    found = b;
                    break;
                }
            }
            ApplyBuildingData(found);
        }

        public void ApplyBuildingData(BuildingDto data)
        {
            _buildingData = data;
            _isEmpty = data == null;

            if (emptyMarker != null) emptyMarker.SetActive(_isEmpty);
            if (placeholderMesh != null) placeholderMesh.SetActive(!_isEmpty);

            if (nameLabel != null)
                nameLabel.text = _isEmpty ? (displayName + " (Empty)") : $"{displayName} Lv.{data.level}";

            if (statusLabel != null)
            {
                if (_isEmpty) statusLabel.text = "";
                else if (data.isConstructing) statusLabel.text = "⚙ Under Construction";
                else statusLabel.text = $"Level {data.level}";
            }
        }

        // ── Interaction events ────────────────────────────────────────────────

        private void OnSelected()
        {
            SetSelectionRing(true);
            // Open upgrade/construct panel
            Debug.Log($"[BuildingSlot] Selected: {slotType}");
        }

        private void OnDeselected()
        {
            SetSelectionRing(false);
        }

        private void OnHovered(bool isHovered)
        {
            // Scale pulse handled by SelectableEntity
        }

        private void SetSelectionRing(bool visible)
        {
            if (selectionRing != null)
                selectionRing.SetActive(visible);
        }
    }
}
