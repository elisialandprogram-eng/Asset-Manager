using UnityEngine;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.World.Grid;
using EternalKingdoms.World.UI;

namespace EternalKingdoms.World.Entities
{
    /// <summary>
    /// World map entity representing a player kingdom.
    ///
    /// Visual tiers (UNITY_WORLD_SYSTEM.md):
    ///   power < 100    → Village  (small wooden hall)
    ///   100–499        → Town     (stone keep)
    ///   500–1,999      → Castle   (full castle + walls)
    ///   ≥ 2,000        → Capital  (fortified city)
    ///
    /// Own kingdom gets:
    ///   - distinct color tint (gold/green glow)
    ///   - always-visible name label
    ///   - crown icon above name
    ///
    /// Click → WorldSelectionManager.SelectKingdom()
    ///       → WorldInfoPanel shows kingdom details
    /// </summary>
    public class KingdomEntity : BaseWorldEntity
    {
        [Header("Power Tier Meshes")]
        [SerializeField] private GameObject villageMesh;
        [SerializeField] private GameObject townMesh;
        [SerializeField] private GameObject castleMesh;
        [SerializeField] private GameObject capitalMesh;

        [Header("Own Kingdom")]
        [SerializeField] private GameObject ownKingdomIndicator;  // crown icon / gold ring
        [SerializeField] private Renderer   kingdomRenderer;

        [Header("Colors")]
        [SerializeField] private Color ownKingdomColor = new(1f, 0.92f, 0.3f);
        [SerializeField] private Color enemyColor      = new(0.6f, 0.75f, 1f);

        private WorldKingdomDto _data;
        private bool _isOwnKingdom;

        public WorldKingdomDto Data    => _data;
        public bool IsOwnKingdom       => _isOwnKingdom;

        // ── Initialize / Refresh ──────────────────────────────────────────────

        public void Initialize(WorldKingdomDto data, bool isOwn)
        {
            _data        = data;
            _isOwnKingdom = isOwn;
            _entityId    = data.id;

            SetPosition(WorldCoordinate.FromBackend(data.mapX, data.mapY));
            ApplyVisuals();
            gameObject.SetActive(true);
        }

        public void Refresh(WorldKingdomDto data, bool isOwn)
        {
            _data = data;
            _isOwnKingdom = isOwn;
            ApplyVisuals();
        }

        // ── Visuals ───────────────────────────────────────────────────────────

        private void ApplyVisuals()
        {
            // Tier meshes
            bool village = _data.power < 100;
            bool town    = _data.power >= 100  && _data.power < 500;
            bool castle  = _data.power >= 500  && _data.power < 2000;
            bool capital = _data.power >= 2000;

            if (villageMesh != null) villageMesh.SetActive(village);
            if (townMesh    != null) townMesh.SetActive(town);
            if (castleMesh  != null) castleMesh.SetActive(castle);
            if (capitalMesh != null) capitalMesh.SetActive(capital);

            // Name label
            SetNameLabel(_data.name);

            // Own kingdom indicator
            if (ownKingdomIndicator != null) ownKingdomIndicator.SetActive(_isOwnKingdom);

            // Color tint
            if (kingdomRenderer != null)
            {
                var mat = kingdomRenderer.material;
                if (mat != null) mat.color = _isOwnKingdom ? ownKingdomColor : enemyColor;
            }
        }

        // ── Interaction ───────────────────────────────────────────────────────

        protected override void OnSelected()
        {
            WorldSelectionManager.Instance?.SelectKingdom(this);
        }
    }
}
