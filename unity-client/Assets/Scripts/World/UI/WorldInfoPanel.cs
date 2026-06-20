using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.World.Grid;
using EternalKingdoms.Utilities;

namespace EternalKingdoms.World.UI
{
    /// <summary>
    /// Side-panel shown when an entity is selected on the world map.
    ///
    /// Supports three entity types via ShowKingdom/ShowMonster/ShowCrystal.
    /// Each type activates its own sub-panel and hides the others.
    ///
    /// Kingdom info:
    ///   - Name, owner, power, palace level, coordinates
    ///   - "Enter Kingdom" button (own kingdom only)
    ///   - (Future) Attack, Scout, Send Resources buttons
    ///
    /// Monster info:
    ///   - Monster name, tier, HP bar, AP reward preview
    ///   - (Future) Hunt button
    ///
    /// Crystal info:
    ///   - Crystal type, yield per hour, harvest status
    ///   - (Future) Harvest button
    /// </summary>
    public class WorldInfoPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("Kingdom sub-panel")]
        [SerializeField] private GameObject kingdomPanel;
        [SerializeField] private TextMeshProUGUI kingdomNameLabel;
        [SerializeField] private TextMeshProUGUI kingdomPowerLabel;
        [SerializeField] private TextMeshProUGUI kingdomCoordsLabel;
        [SerializeField] private Button enterKingdomButton;

        [Header("Monster sub-panel")]
        [SerializeField] private GameObject monsterPanel;
        [SerializeField] private TextMeshProUGUI monsterNameLabel;
        [SerializeField] private TextMeshProUGUI monsterTierLabel;
        [SerializeField] private TextMeshProUGUI monsterHpLabel;
        [SerializeField] private Slider monsterHpBar;

        [Header("Crystal sub-panel")]
        [SerializeField] private GameObject crystalPanel;
        [SerializeField] private TextMeshProUGUI crystalTypeLabel;
        [SerializeField] private TextMeshProUGUI crystalYieldLabel;
        [SerializeField] private TextMeshProUGUI crystalStatusLabel;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            closeButton?.onClick.AddListener(Hide);
            enterKingdomButton?.onClick.AddListener(OnEnterKingdomClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void ShowKingdom(WorldKingdomDto data)
        {
            Show();
            SetActivePanel(PanelType.Kingdom);

            if (kingdomNameLabel  != null) kingdomNameLabel.text  = data.name;
            if (kingdomPowerLabel != null) kingdomPowerLabel.text = $"Power: {data.power:N0}";

            var coord = WorldCoordinate.FromBackend(data.mapX, data.mapY);
            if (kingdomCoordsLabel != null)
                kingdomCoordsLabel.text = $"Coords: ({data.mapX}, {data.mapY}) | Tile {coord}";

            // "Enter Kingdom" only for own kingdom — managed by WorldSelectionManager
            bool isOwn = EternalKingdoms.Core.SaveManager.Instance.GetString(
                            EternalKingdoms.Core.SaveManager.KEY_KINGDOM_ID) == data.id;
            if (enterKingdomButton != null) enterKingdomButton.gameObject.SetActive(isOwn);
        }

        public void ShowMonster(MonsterSpawnDto data)
        {
            Show();
            SetActivePanel(PanelType.Monster);

            var m = data.monster;
            if (monsterNameLabel != null) monsterNameLabel.text = m?.name ?? "Monster";
            if (monsterTierLabel != null) monsterTierLabel.text = m != null ? $"Tier {m.tier}" : "";
            if (monsterHpLabel   != null)
                monsterHpLabel.text = m != null ? $"HP: {data.currentHp:N0} / {m.hp:N0}" : "";
            if (monsterHpBar != null && m != null && m.hp > 0)
                monsterHpBar.value = (float)data.currentHp / m.hp;
        }

        public void ShowCrystal(CrystalNodeDto data)
        {
            Show();
            SetActivePanel(PanelType.Crystal);

            if (crystalTypeLabel  != null) crystalTypeLabel.text  = CapType(data.crystalType) + " Crystal";
            if (crystalYieldLabel != null) crystalYieldLabel.text  = $"Yield: {data.crystalYield}/hr";
            if (crystalStatusLabel!= null)
                crystalStatusLabel.text = string.IsNullOrEmpty(data.harvestedByKingdomId)
                    ? "Available" : "Harvested";
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        private void SetActivePanel(PanelType type)
        {
            if (kingdomPanel != null) kingdomPanel.SetActive(type == PanelType.Kingdom);
            if (monsterPanel != null) monsterPanel.SetActive(type == PanelType.Monster);
            if (crystalPanel != null) crystalPanel.SetActive(type == PanelType.Crystal);
        }

        private void OnEnterKingdomClicked()
        {
            WorldSelectionManager.Instance?.EnterSelectedKingdom();
        }

        private static string CapType(string s) =>
            string.IsNullOrEmpty(s) ? "" : char.ToUpper(s[0]) + s.Substring(1);

        private enum PanelType { Kingdom, Monster, Crystal }
    }
}
