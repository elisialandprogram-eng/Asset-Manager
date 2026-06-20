using UnityEngine;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.World.Grid;
using EternalKingdoms.Utilities;

namespace EternalKingdoms.World.Entities
{
    /// <summary>
    /// World map entity representing a crystal resource node.
    ///
    /// Crystal types: fire | ice | earth | lightning | void | holy
    /// Each type has a distinct color and glow (MathExtensions.CrystalColor).
    ///
    /// Visual states:
    ///   Full         — crystal mesh at full scale, max glow
    ///   Harvested    — depleted visual (grey / small)
    ///   Owned        — tinted with the harvesting kingdom's color
    ///
    /// Click → WorldSelectionManager.SelectCrystal()
    ///       → WorldInfoPanel shows crystal type, yield, harvest status
    /// </summary>
    public class CrystalEntity : BaseWorldEntity
    {
        [Header("Crystal Mesh")]
        [SerializeField] private Renderer crystalRenderer;
        [SerializeField] private GameObject fullCrystal;
        [SerializeField] private GameObject depletedCrystal;

        [Header("Glow")]
        [SerializeField] private Light crystalLight;
        [SerializeField] private float maxLightIntensity = 2f;

        private CrystalNodeDto _data;
        public CrystalNodeDto Data => _data;

        // ── Initialize / Refresh ──────────────────────────────────────────────

        public void Initialize(CrystalNodeDto data)
        {
            _data     = data;
            _entityId = data.id;
            SetPosition(WorldCoordinate.FromBackend(data.x, data.y));
            ApplyVisuals();
            gameObject.SetActive(true);
        }

        public void Refresh(CrystalNodeDto data)
        {
            _data = data;
            ApplyVisuals();
        }

        // ── Visuals ───────────────────────────────────────────────────────────

        private void ApplyVisuals()
        {
            bool harvested = !string.IsNullOrEmpty(_data.harvestedByKingdomId);
            Color crystalColor = MathExtensions.CrystalColor(_data.crystalType);

            if (fullCrystal     != null) fullCrystal.SetActive(!harvested);
            if (depletedCrystal != null) depletedCrystal.SetActive(harvested);

            // Material color
            if (crystalRenderer != null)
            {
                var mat = crystalRenderer.material;
                if (mat != null)
                {
                    mat.color = harvested ? Color.grey : crystalColor;
                    // URP emissive (Baked or Realtime GI)
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", harvested ? Color.black : crystalColor * 2f);
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }

            // Point light glow
            if (crystalLight != null)
            {
                crystalLight.color     = crystalColor;
                crystalLight.intensity = harvested ? 0f : maxLightIntensity;
            }

            // Label: "Fire Crystal" / "Harvested"
            string label = CrystalTypeName(_data.crystalType) + " Crystal";
            if (harvested) label += " (Harvested)";
            SetNameLabel(label);
        }

        // ── Interaction ───────────────────────────────────────────────────────

        protected override void OnSelected()
        {
            WorldSelectionManager.Instance?.SelectCrystal(this);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string CrystalTypeName(string t) => t switch
        {
            "fire"      => "Fire",
            "ice"       => "Ice",
            "earth"     => "Earth",
            "lightning" => "Lightning",
            "void"      => "Void",
            "holy"      => "Holy",
            _           => "Crystal"
        };
    }
}
