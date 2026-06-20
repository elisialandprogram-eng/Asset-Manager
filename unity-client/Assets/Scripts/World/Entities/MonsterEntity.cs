using UnityEngine;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.World.Grid;
using EternalKingdoms.World.UI;
using EternalKingdoms.Utilities;

namespace EternalKingdoms.World.Entities
{
    /// <summary>
    /// World map entity representing a monster spawn lair.
    ///
    /// Visual states:
    ///   Alive    — lair mesh visible, tier-colored glow, HP bar
    ///   Dead     — skull marker or invisible, respawn countdown shown
    ///
    /// Tier coloring:
    ///   T1 → grey,    T2 → green,  T3 → blue,
    ///   T4 → purple,  T5 → orange, T6+ → red (boss)
    ///
    /// Click → WorldSelectionManager.SelectMonster()
    ///       → WorldInfoPanel shows monster info + HP + rewards preview
    /// </summary>
    public class MonsterEntity : BaseWorldEntity
    {
        [Header("State Meshes")]
        [SerializeField] private GameObject aliveMesh;
        [SerializeField] private GameObject deadMarker;

        [Header("HP Bar")]
        [SerializeField] private UnityEngine.UI.Slider hpBar;

        [Header("Tier Colors (T1–T6)")]
        [SerializeField] private Color[] tierColors = new Color[]
        {
            new(0.75f, 0.75f, 0.75f),   // T1 grey
            new(0.20f, 0.80f, 0.20f),   // T2 green
            new(0.30f, 0.50f, 1.00f),   // T3 blue
            new(0.75f, 0.20f, 0.90f),   // T4 purple
            new(1.00f, 0.55f, 0.10f),   // T5 orange
            new(0.90f, 0.10f, 0.10f),   // T6 red (boss)
        };

        [SerializeField] private Renderer tierRenderer;

        private MonsterSpawnDto _data;
        public MonsterSpawnDto Data => _data;

        // ── Initialize / Refresh ──────────────────────────────────────────────

        public void Initialize(MonsterSpawnDto data)
        {
            _data     = data;
            _entityId = data.id;
            SetPosition(WorldCoordinate.FromBackend(data.x, data.y));
            ApplyVisuals();
            gameObject.SetActive(true);
        }

        public void Refresh(MonsterSpawnDto data)
        {
            _data = data;
            ApplyVisuals();
        }

        // ── Visuals ───────────────────────────────────────────────────────────

        private void ApplyVisuals()
        {
            bool alive = _data.currentHp > 0;

            if (aliveMesh   != null) aliveMesh.SetActive(alive);
            if (deadMarker  != null) deadMarker.SetActive(!alive);

            // Name label (monster name or "Lair")
            string label = _data.monster?.name ?? "Monster Lair";
            if (_data.monster != null) label += $" T{_data.monster.tier}";
            SetNameLabel(label);

            // HP bar
            if (hpBar != null && _data.monster != null && _data.monster.hp > 0)
            {
                hpBar.gameObject.SetActive(alive);
                hpBar.value = (float)_data.currentHp / _data.monster.hp;
            }

            // Tier color
            if (tierRenderer != null && _data.monster != null)
            {
                int tier  = Mathf.Clamp(_data.monster.tier - 1, 0, tierColors.Length - 1);
                var mat   = tierRenderer.material;
                if (mat != null) mat.color = tierColors[tier];
            }
        }

        // ── Interaction ───────────────────────────────────────────────────────

        protected override void OnSelected()
        {
            WorldSelectionManager.Instance?.SelectMonster(this);
        }
    }
}
