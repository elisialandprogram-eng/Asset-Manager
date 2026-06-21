using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Managers;
using EternalKingdoms.Networking;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// MonsterAttackPanel — The player-facing UI for initiating a PvE march.
    ///
    /// Phase 4 flow (U4.4):
    ///   1. Player selects monster on world map → EntitySelectionManager opens this panel
    ///   2. Panel fetches monster info (GET /api/monsters/:spawnId)
    ///   3. Player selects hero (optional)
    ///   4. Player selects troops (via TroopSlider components)
    ///   5. Panel shows AP cost + validates AP/troops in real time
    ///   6. Player clicks Attack → POST /api/monsters/:spawnId/attack
    ///   7. Panel closes; BattleReportPanel opens when report arrives
    ///
    /// Architecture:
    ///   - No local combat calculation. Backend authoritative.
    ///   - Reads from TroopManager, HeroManager, ActionPointManager.
    ///   - Shows real-time AP cost and troop validation.
    /// </summary>
    public class MonsterAttackPanel : MonoBehaviour
    {
        [Header("Monster Info")]
        [SerializeField] private TextMeshProUGUI monsterNameText;
        [SerializeField] private TextMeshProUGUI monsterTierText;
        [SerializeField] private TextMeshProUGUI monsterPowerText;
        [SerializeField] private Image monsterIcon;

        [Header("AP Display")]
        [SerializeField] private TextMeshProUGUI apCostText;
        [SerializeField] private TextMeshProUGUI apAvailableText;

        [Header("Hero Selection")]
        [SerializeField] private TextMeshProUGUI heroNameText;
        [SerializeField] private Button heroSelectButton;

        [Header("Troop Sliders")]
        [SerializeField] private Transform troopSlidersParent;
        [SerializeField] private GameObject troopSliderPrefab;

        [Header("Action")]
        [SerializeField] private Button attackButton;
        [SerializeField] private TextMeshProUGUI attackButtonText;
        [SerializeField] private TextMeshProUGUI errorText;

        private CombatService _combatService;
        private int _kingdomId;
        private int _spawnId;
        private MonsterSpawnDetailDto _monsterDetail;
        private int _selectedHeroId = -1;

        private readonly Dictionary<string, Slider> _troopSliders
            = new Dictionary<string, Slider>();

        public event Action<AttackMarchDto> OnMarchSent;

        private void Awake()
        {
            attackButton.onClick.AddListener(OnAttackClicked);
            heroSelectButton.onClick.AddListener(OnHeroSelectClicked);
            gameObject.SetActive(false);
        }

        public void Initialize(CombatService combatService, int kingdomId)
        {
            _combatService = combatService;
            _kingdomId     = kingdomId;
        }

        public void OpenForMonster(int spawnId)
        {
            _spawnId        = spawnId;
            _selectedHeroId = -1;
            errorText.text  = string.Empty;
            gameObject.SetActive(true);
            StartCoroutine(LoadMonsterInfo());
        }

        private IEnumerator LoadMonsterInfo()
        {
            attackButton.interactable = false;
            attackButtonText.text     = "Loading...";

            yield return StartCoroutine(_combatService.GetMonsterSpawn(
                _spawnId,
                detail =>
                {
                    _monsterDetail = detail;
                    PopulateMonsterInfo(detail);
                    BuildTroopSliders();
                    RefreshButtonState();
                },
                err =>
                {
                    errorText.text = $"Failed to load monster: {err.Message}";
                    attackButton.interactable = false;
                }
            ));
        }

        private void PopulateMonsterInfo(MonsterSpawnDetailDto detail)
        {
            monsterNameText.text  = detail.monster.name;
            monsterTierText.text  = $"Tier: {CapFirst(detail.monster.tier)}";
            monsterPowerText.text = $"Power: {detail.monster.power:N0}";
            apCostText.text       = $"AP Cost: {detail.apCost}";
            UpdateApDisplay();
        }

        private void UpdateApDisplay()
        {
            if (ActionPointManager.Instance == null) return;
            float current = ActionPointManager.Instance.CurrentAP;
            float max     = ActionPointManager.Instance.MaxAP;
            apAvailableText.text = $"AP: {Mathf.Floor(current)} / {max}";
        }

        private void BuildTroopSliders()
        {
            foreach (Transform child in troopSlidersParent)
                Destroy(child.gameObject);
            _troopSliders.Clear();

            if (TroopManager.Instance == null) return;

            foreach (var kvp in TroopManager.Instance.TroopCounts)
            {
                if (kvp.Value <= 0) continue;

                var sliderGo = Instantiate(troopSliderPrefab, troopSlidersParent);
                var slider   = sliderGo.GetComponentInChildren<Slider>();
                var label    = sliderGo.GetComponentInChildren<TextMeshProUGUI>();

                if (label  != null) label.text = kvp.Key;
                if (slider != null)
                {
                    slider.minValue = 0;
                    slider.maxValue = kvp.Value;
                    slider.wholeNumbers = true;
                    slider.onValueChanged.AddListener(_ => RefreshButtonState());
                    _troopSliders[kvp.Key] = slider;
                }
            }
        }

        private Dictionary<string, int> GetSelectedTroops()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _troopSliders)
            {
                int count = Mathf.RoundToInt(kvp.Value.value);
                if (count > 0) result[kvp.Key] = count;
            }
            return result;
        }

        private void RefreshButtonState()
        {
            if (_monsterDetail == null) return;

            UpdateApDisplay();

            bool hasAp     = ActionPointManager.Instance?.CanAfford(_monsterDetail.monster.tier) ?? false;
            var troops     = GetSelectedTroops();
            bool hasTroops = troops.Count > 0;
            string troopError;
            bool hasTroopCount = TroopManager.Instance?.CanSendTroops(troops, out troopError) ?? false;

            bool canAttack = hasAp && hasTroops && hasTroopCount;
            attackButton.interactable = canAttack;

            if (!hasAp)
                attackButtonText.text = "Not Enough AP";
            else if (!hasTroops)
                attackButtonText.text = "Select Troops";
            else
                attackButtonText.text = "Attack!";
        }

        private void OnHeroSelectClicked()
        {
            if (HeroManager.Instance == null || HeroManager.Instance.Heroes.Count == 0)
            {
                heroNameText.text = "No heroes available";
                return;
            }

            var leadHero = HeroManager.Instance.GetLeadingHero();
            if (leadHero != null)
            {
                _selectedHeroId   = leadHero.id;
                heroNameText.text = $"{leadHero.name} (Lv.{leadHero.level})";
            }
        }

        private void OnAttackClicked()
        {
            if (_monsterDetail == null) return;

            var troops = GetSelectedTroops();
            if (troops.Count == 0)
            {
                errorText.text = "Please select at least one troop type.";
                return;
            }

            attackButton.interactable = false;
            attackButtonText.text     = "Sending...";
            errorText.text            = string.Empty;

            var request = new AttackMarchRequestDto
            {
                kingdomId = _kingdomId,
                heroId    = _selectedHeroId > 0 ? _selectedHeroId : 0,
                troops    = troops,
            };

            StartCoroutine(_combatService.AttackMonster(
                _spawnId,
                request,
                response =>
                {
                    ActionPointManager.Instance?.LocalDeduct(response.march.apCost);
                    TroopManager.Instance?.LocalDeduct(troops);
                    OnMarchSent?.Invoke(response.march);
                    gameObject.SetActive(false);
                },
                err =>
                {
                    errorText.text            = $"Error: {err.Message}";
                    attackButton.interactable = true;
                    attackButtonText.text     = "Attack!";
                }
            ));
        }

        private static string CapFirst(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
