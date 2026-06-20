using System;
using UnityEngine;
using EternalKingdoms.World.DTOs;

namespace EternalKingdoms.World.Entities
{
    /// <summary>
    /// ResourceNodeEntity — visual representation of a resource spawn node on
    /// the world map (food/wood/stone/iron/gold/crystal).
    ///
    /// Visual approach (Phase 3 — geometry only):
    ///   - Cube mesh scaled by level (0.6 base + 0.05 × level)
    ///   - Colour-coded by subtype
    ///   - Level number TMP label
    ///   - Hover ring (same pattern as other world entities)
    ///   - Pulsing emission when nearly expired
    ///   - Fully pooled — call Initialize() / Recycle()
    ///
    /// Clicking a ResourceNodeEntity raises OnNodeClicked, which
    /// WorldSelectionManager picks up to open ResourceGatherPanel.
    /// </summary>
    public class ResourceNodeEntity : MonoBehaviour
    {
        // Resource type colours
        private static readonly Color ColorFood    = new Color(0.6f, 0.9f, 0.3f);  // lime
        private static readonly Color ColorWood    = new Color(0.4f, 0.25f, 0.1f); // brown
        private static readonly Color ColorStone   = new Color(0.7f, 0.7f, 0.7f);  // grey
        private static readonly Color ColorIron    = new Color(0.5f, 0.5f, 0.7f);  // steel
        private static readonly Color ColorGold    = new Color(1.0f, 0.85f, 0.1f); // gold
        private static readonly Color ColorCrystal = new Color(0.5f, 0.2f, 1.0f);  // violet

        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [SerializeField] private MeshRenderer  _meshRenderer;
        [SerializeField] private GameObject    _hoverRing;
        [SerializeField] private GameObject    _selectionRing;
        [SerializeField] private TMPro.TextMeshPro _levelLabel;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private ResourceNodeDto     _data;
        private MaterialPropertyBlock _mpb;
        private bool _hovered;
        private bool _selected;

        public event Action<ResourceNodeEntity> OnNodeClicked;

        // -------------------------------------------------------------------------
        // Pool interface
        // -------------------------------------------------------------------------

        public void Initialize(ResourceNodeDto data)
        {
            _data = data;
            _mpb ??= new MaterialPropertyBlock();
            gameObject.SetActive(true);

            if (_hoverRing)     _hoverRing.SetActive(false);
            if (_selectionRing) _selectionRing.SetActive(false);

            ApplyVisuals();
        }

        public void Recycle()
        {
            _data     = null;
            _hovered  = false;
            _selected = false;
            gameObject.SetActive(false);
        }

        public int NodeId      => _data?.id ?? -1;
        public ResourceNodeDto Data => _data;

        // -------------------------------------------------------------------------
        // Interaction
        // -------------------------------------------------------------------------

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (_selectionRing) _selectionRing.SetActive(selected);
        }

        public void OnPointerEnter() { _hovered = true;  if (_hoverRing) _hoverRing.SetActive(true);  }
        public void OnPointerExit()  { _hovered = false; if (_hoverRing) _hoverRing.SetActive(false); }

        private void OnMouseDown() => OnNodeClicked?.Invoke(this);

        // -------------------------------------------------------------------------
        // Visuals
        // -------------------------------------------------------------------------

        private void ApplyVisuals()
        {
            if (_data == null) return;

            // Scale by level
            float scale = 0.6f + _data.level * 0.05f;
            transform.localScale = new Vector3(scale, scale * 0.6f, scale);

            // Colour
            Color c = SubtypeColor(_data.spawnSubtype);
            if (_meshRenderer)
            {
                _meshRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", c);
                _meshRenderer.SetPropertyBlock(_mpb);
            }

            // Label
            if (_levelLabel) _levelLabel.text = $"Lv{_data.level}";
        }

        private void Update()
        {
            if (_data == null) return;

            // Pulse emission when expiry is imminent (< 10 min)
            if (!string.IsNullOrEmpty(_data.expiresAt) &&
                DateTime.TryParse(_data.expiresAt, out var exp))
            {
                double remaining = (exp.ToUniversalTime() - DateTime.UtcNow).TotalMinutes;
                if (remaining < 10f)
                {
                    float pulse = (MathF.Sin(Time.time * 3f) + 1f) * 0.5f;
                    _meshRenderer?.GetPropertyBlock(_mpb);
                    _mpb?.SetFloat("_EmissionIntensity", pulse * 2f);
                    _meshRenderer?.SetPropertyBlock(_mpb);
                }
            }
        }

        private static Color SubtypeColor(string subtype) => subtype switch
        {
            "farm"    => ColorFood,
            "lumber"  => ColorWood,
            "stone"   => ColorStone,
            "iron"    => ColorIron,
            "gold"    => ColorGold,
            "crystal" => ColorCrystal,
            _         => Color.white,
        };
    }
}
