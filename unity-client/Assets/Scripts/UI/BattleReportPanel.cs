using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Networking;
using EternalKingdoms.Managers;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// BattleReportPanel — Displays a completed PvE battle report.
    ///
    /// Phase 4 (U4.6):
    ///   - Shows attacker won/lost result
    ///   - Round-by-round breakdown (up to 5 rounds)
    ///   - Casualties: killed vs wounded
    ///   - Rewards granted (resources, items, hero XP)
    ///
    /// Architecture:
    ///   - Reports fetched from GET /api/reports/:id (backend authoritative)
    ///   - Reports survive logout (permanent storage)
    ///   - Panel opened by MarchEntity when march status → completed
    ///   - No local combat visualization — battle resolved before this panel shows
    ///
    /// Unity visualization (U4.11):
    ///   - Victory: green glow + fanfare
    ///   - Defeat: red tint + sad sound
    ///   - Round rows animate in sequentially
    /// </summary>
    public class BattleReportPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI monsterNameText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Image resultBackground;
        [SerializeField] private Color victoryColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color defeatColor  = new Color(0.8f, 0.2f, 0.2f);

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI roundsFoughtText;
        [SerializeField] private TextMeshProUGUI troopsSentText;
        [SerializeField] private TextMeshProUGUI troopsKilledText;
        [SerializeField] private TextMeshProUGUI troopsWoundedText;
        [SerializeField] private TextMeshProUGUI troopsSurvivedText;

        [Header("Rewards")]
        [SerializeField] private TextMeshProUGUI rewardsText;

        [Header("Rounds")]
        [SerializeField] private Transform roundsParent;
        [SerializeField] private GameObject roundRowPrefab;

        [Header("Actions")]
        [SerializeField] private Button closeButton;

        [Header("Effects")]
        [SerializeField] private ParticleSystem victoryEffect;
        [SerializeField] private ParticleSystem defeatEffect;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            gameObject.SetActive(false);
        }

        public void ShowReport(BattleReportDto report)
        {
            gameObject.SetActive(true);
            StartCoroutine(PopulateReport(report));
        }

        private IEnumerator PopulateReport(BattleReportDto report)
        {
            titleText.text       = "Battle Report";
            monsterNameText.text = report.monsterName;

            if (report.attackerWon)
            {
                resultText.text              = "VICTORY";
                resultBackground.color       = victoryColor;
                if (victoryEffect != null) victoryEffect.Play();
            }
            else
            {
                resultText.text              = "DEFEAT";
                resultBackground.color       = defeatColor;
                if (defeatEffect != null)  defeatEffect.Play();
            }

            roundsFoughtText.text  = $"Rounds: {report.roundsFought}";
            troopsSentText.text    = FormatTroops("Sent",     report.attackerTroopsSent);
            troopsKilledText.text  = FormatTroops("Killed",   report.attackerTroopsKilled);
            troopsWoundedText.text = FormatTroops("Wounded",  report.attackerTroopsWounded);
            troopsSurvivedText.text= FormatTroops("Survived", report.attackerTroopsSurvived);
            rewardsText.text       = FormatRewards(report.rewardsGranted);

            // Clear existing round rows
            foreach (Transform child in roundsParent)
                Destroy(child.gameObject);

            // Animate round rows in sequence
            foreach (var round in report.rounds)
            {
                var row     = Instantiate(roundRowPrefab, roundsParent);
                var rowText = row.GetComponentInChildren<TextMeshProUGUI>();
                if (rowText != null)
                {
                    rowText.text =
                        $"R{round.round}: You dealt {round.attackerDamageDealt:F0} dmg | " +
                        $"Monster dealt {round.defenderDamageDealt:F0} dmg";
                }
                yield return new WaitForSeconds(0.15f);
            }
        }

        private static string FormatTroops(string label, Dictionary<string, int> troops)
        {
            if (troops == null || troops.Count == 0) return $"{label}: none";
            var parts = new System.Text.StringBuilder($"{label}: ");
            foreach (var kvp in troops)
                parts.Append($"{kvp.Key} ×{kvp.Value}  ");
            return parts.ToString().TrimEnd();
        }

        private static string FormatRewards(RewardsDto r)
        {
            if (r == null) return "No rewards";
            var parts = new System.Text.StringBuilder("Rewards: ");
            if (r.food    > 0) parts.Append($"Food {r.food:N0}  ");
            if (r.wood    > 0) parts.Append($"Wood {r.wood:N0}  ");
            if (r.stone   > 0) parts.Append($"Stone {r.stone:N0}  ");
            if (r.iron    > 0) parts.Append($"Iron {r.iron:N0}  ");
            if (r.gold    > 0) parts.Append($"Gold {r.gold:N0}  ");
            if (r.crystal > 0) parts.Append($"Crystal {r.crystal:N0}  ");
            if (r.heroXp  > 0) parts.Append($"Hero XP +{r.heroXp}  ");
            if (r.items   != null)
                foreach (var kvp in r.items)
                    parts.Append($"{InventoryManager.GetDisplayName(kvp.Key)} ×{kvp.Value}  ");
            return parts.Length > "Rewards: ".Length ? parts.ToString().TrimEnd() : "No rewards";
        }
    }
}
