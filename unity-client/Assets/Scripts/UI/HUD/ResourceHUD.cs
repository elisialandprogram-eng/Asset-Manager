using UnityEngine;
using TMPro;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.UI.HUD
{
    /// <summary>
    /// Displays current resources and production rates in the Kingdom HUD.
    /// Refreshed by KingdomStateManager every poll cycle.
    /// </summary>
    public class ResourceHUD : MonoBehaviour
    {
        [Header("Resource Labels")]
        [SerializeField] private TextMeshProUGUI foodLabel;
        [SerializeField] private TextMeshProUGUI woodLabel;
        [SerializeField] private TextMeshProUGUI stoneLabel;
        [SerializeField] private TextMeshProUGUI ironLabel;
        [SerializeField] private TextMeshProUGUI goldLabel;

        [Header("Production Rate Labels (optional)")]
        [SerializeField] private TextMeshProUGUI foodRateLabel;
        [SerializeField] private TextMeshProUGUI woodRateLabel;
        [SerializeField] private TextMeshProUGUI stoneRateLabel;
        [SerializeField] private TextMeshProUGUI ironRateLabel;
        [SerializeField] private TextMeshProUGUI goldRateLabel;

        public void Refresh(ResourcesDto resources, ProductionRatesDto rates, ResourceCapsDto caps)
        {
            SetResource(foodLabel, foodRateLabel, resources.food, rates?.food ?? 0, caps?.food ?? 0);
            SetResource(woodLabel, woodRateLabel, resources.wood, rates?.wood ?? 0, caps?.wood ?? 0);
            SetResource(stoneLabel, stoneRateLabel, resources.stone, rates?.stone ?? 0, caps?.stone ?? 0);
            SetResource(ironLabel, ironRateLabel, resources.iron, rates?.iron ?? 0, caps?.iron ?? 0);
            SetResource(goldLabel, goldRateLabel, resources.gold, rates?.gold ?? 0, caps?.gold ?? 0);
        }

        private void SetResource(TextMeshProUGUI valueLabel, TextMeshProUGUI rateLabel, long value, float rate, long cap)
        {
            if (valueLabel != null)
                valueLabel.text = cap > 0
                    ? $"{value:N0} / {cap:N0}"
                    : value.ToString("N0");

            if (rateLabel != null)
                rateLabel.text = rate >= 0 ? $"+{rate:F1}/min" : $"{rate:F1}/min";
        }
    }
}
