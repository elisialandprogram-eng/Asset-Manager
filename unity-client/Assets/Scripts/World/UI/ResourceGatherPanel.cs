using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalKingdoms.World.DTOs;
using EternalKingdoms.World.Entities;

namespace EternalKingdoms.World.UI
{
    /// <summary>
    /// ResourceGatherPanel — UI panel shown when the player clicks a resource node.
    ///
    /// Player flow:
    ///   Click resource node → Panel opens → Choose troop amount → "Send March" →
    ///   CreateMarch API call → Panel closes → MarchEntity spawns on world map.
    ///
    /// Phase 3 scope — single troop preset (militia only):
    ///   - Shows node info (type, level, estimated yield).
    ///   - Shows ETA preview (travel time + gather time + return time).
    ///   - Troop count slider: 1–100 militia troops.
    ///   - "Send March" button: calls MarchManager.CreateMarch().
    ///   - "Close" button: closes panel without action.
    ///
    /// Phase 4: full troop composition picker (multi-type).
    /// Phase 5: march capacity limit enforcement.
    ///
    /// Inspector wiring:
    ///   Assign all TMP fields, slider, and buttons in the Unity Inspector.
    ///   Panel root: a Canvas group (alpha fade on open/close).
    /// </summary>
    public class ResourceGatherPanel : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector bindings
        // -------------------------------------------------------------------------

        [Header("Node Info")]
        [SerializeField] private TextMeshProUGUI _nodeTitleLabel;     // "Iron Deposit — Lv3"
        [SerializeField] private TextMeshProUGUI _nodeSubtypeLabel;   // "iron"
        [SerializeField] private TextMeshProUGUI _nodeExpiryLabel;    // "Expires in 43m"

        [Header("Troop Picker (Phase 3: militia only)")]
        [SerializeField] private Slider          _troopSlider;        // 1–100
        [SerializeField] private TextMeshProUGUI _troopCountLabel;    // "50 Militia"

        [Header("March Preview")]
        [SerializeField] private TextMeshProUGUI _travelTimeLabel;    // "Travel: 4m 32s"
        [SerializeField] private TextMeshProUGUI _gatherTimeLabel;    // "Gather: 5m 00s"
        [SerializeField] private TextMeshProUGUI _returnTimeLabel;    // "Return: 4m 32s"
        [SerializeField] private TextMeshProUGUI _totalTimeLabel;     // "Total: 14m 04s"
        [SerializeField] private TextMeshProUGUI _estimatedYieldLabel;// "~500 Iron"
        [SerializeField] private TextMeshProUGUI _carryCapLabel;      // "Carry: 2000"

        [Header("Buttons")]
        [SerializeField] private Button          _sendMarchButton;
        [SerializeField] private Button          _closeButton;

        [Header("State")]
        [SerializeField] private TextMeshProUGUI _errorLabel;
        [SerializeField] private GameObject      _loadingOverlay;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private ResourceNodeEntity _targetNode;
        private int _ownKingdomTileX;
        private int _ownKingdomTileY;
        private int _worldId;

        private bool _sending;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (_sendMarchButton) _sendMarchButton.onClick.AddListener(OnSendMarchClicked);
            if (_closeButton)     _closeButton.onClick.AddListener(Close);
            if (_troopSlider)     _troopSlider.onValueChanged.AddListener(OnTroopSliderChanged);

            gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Open / Close
        // -------------------------------------------------------------------------

        public void Open(
            ResourceNodeEntity node,
            int kingdomTileX,
            int kingdomTileY,
            int worldId)
        {
            _targetNode       = node;
            _ownKingdomTileX  = kingdomTileX;
            _ownKingdomTileY  = kingdomTileY;
            _worldId          = worldId;
            _sending          = false;

            if (_errorLabel)    _errorLabel.text = "";
            if (_loadingOverlay) _loadingOverlay.SetActive(false);
            if (_troopSlider)
            {
                _troopSlider.minValue = 1;
                _troopSlider.maxValue = 100;
                _troopSlider.value    = 10;
            }

            gameObject.SetActive(true);
            PopulateNodeInfo();
            UpdatePreview((int)(_troopSlider ? _troopSlider.value : 10));
        }

        public void Close()
        {
            _targetNode?.SetSelected(false);
            _targetNode = null;
            gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // UI population
        // -------------------------------------------------------------------------

        private void PopulateNodeInfo()
        {
            if (_targetNode?.Data == null) return;
            var d = _targetNode.Data;

            string subtypeLabel = SubtypeDisplayName(d.spawnSubtype);
            if (_nodeTitleLabel)   _nodeTitleLabel.text   = $"{subtypeLabel} — Level {d.level}";
            if (_nodeSubtypeLabel) _nodeSubtypeLabel.text = subtypeLabel;

            if (_nodeExpiryLabel && !string.IsNullOrEmpty(d.expiresAt) &&
                DateTime.TryParse(d.expiresAt, out var exp))
            {
                double mins = (exp.ToUniversalTime() - DateTime.UtcNow).TotalMinutes;
                _nodeExpiryLabel.text = mins > 0
                    ? $"Expires in {FormatSeconds(mins * 60)}"
                    : "Expiring soon";
            }
        }

        private void OnTroopSliderChanged(float value)
        {
            int troops = Mathf.RoundToInt(value);
            if (_troopCountLabel) _troopCountLabel.text = $"{troops} Militia";
            UpdatePreview(troops);
        }

        private void UpdatePreview(int troopCount)
        {
            if (_targetNode?.Data == null) return;
            var d = _targetNode.Data;

            var load = new TroopLoad { militia = troopCount };
            var preview = TravelCalculator.Preview(
                _ownKingdomTileX, _ownKingdomTileY,
                d.tileX, d.tileY,
                d.level,
                load);

            if (_travelTimeLabel)     _travelTimeLabel.text     = $"Travel: {FormatSeconds(preview.TravelSeconds)}";
            if (_gatherTimeLabel)     _gatherTimeLabel.text     = $"Gather: {FormatSeconds(preview.GatherSeconds)}";
            if (_returnTimeLabel)     _returnTimeLabel.text     = $"Return: {FormatSeconds(preview.TravelSeconds)}";
            if (_totalTimeLabel)      _totalTimeLabel.text      = $"Total: {FormatSeconds(preview.TotalSeconds)}";
            if (_estimatedYieldLabel) _estimatedYieldLabel.text = $"~{preview.EstimatedYield} {SubtypeDisplayName(d.spawnSubtype)}";
            if (_carryCapLabel)       _carryCapLabel.text       = $"Carry: {preview.CarryCapacity}";
        }

        // -------------------------------------------------------------------------
        // Send march
        // -------------------------------------------------------------------------

        private void OnSendMarchClicked()
        {
            if (_sending || _targetNode == null) return;
            _sending = true;

            if (_errorLabel) _errorLabel.text = "";
            if (_loadingOverlay) _loadingOverlay.SetActive(true);
            if (_sendMarchButton) _sendMarchButton.interactable = false;

            int troops = _troopSlider ? Mathf.RoundToInt(_troopSlider.value) : 10;
            var troopLoad = new TroopLoad { militia = troops };

            StartCoroutine(MarchManager.Instance.CreateMarch(
                _targetNode.Data.id,
                _worldId,
                troopLoad,
                onSuccess: _ =>
                {
                    if (_loadingOverlay) _loadingOverlay.SetActive(false);
                    Close();
                },
                onError: err =>
                {
                    _sending = false;
                    if (_loadingOverlay) _loadingOverlay.SetActive(false);
                    if (_sendMarchButton) _sendMarchButton.interactable = true;
                    if (_errorLabel) _errorLabel.text = $"Error: {err}";
                }));
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static string SubtypeDisplayName(string subtype) => subtype switch
        {
            "farm"    => "Food",
            "lumber"  => "Wood",
            "stone"   => "Stone",
            "iron"    => "Iron",
            "gold"    => "Gold",
            "crystal" => "Crystal",
            _         => subtype,
        };

        private static string FormatSeconds(double seconds)
        {
            if (seconds <= 0) return "0s";
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)   return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds:D2}s";
            return $"{(int)seconds}s";
        }
    }
}
