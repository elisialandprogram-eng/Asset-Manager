using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using EternalKingdoms.Managers;
using EternalKingdoms.Networking;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// HospitalPanel — Displays wounded troops and heal queue.
    ///
    /// Phase 4 (U4.7):
    ///   - Shows total wounded count vs capacity
    ///   - Lists wounded troops by type (T5 → T1 priority)
    ///   - Shows estimated heal completion time
    ///   - Refreshes every 30 seconds (driven by HospitalManager)
    ///
    /// Architecture:
    ///   - Backend authoritative (lazy heal calculation on GET /api/hospital)
    ///   - HospitalPanel subscribes to HospitalManager.OnHospitalUpdated
    /// </summary>
    public class HospitalPanel : MonoBehaviour
    {
        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI capacityText;
        [SerializeField] private Slider capacityBar;
        [SerializeField] private TextMeshProUGUI totalWoundedText;
        [SerializeField] private TextMeshProUGUI healRateText;
        [SerializeField] private TextMeshProUGUI estimatedClearText;

        [Header("Troop List")]
        [SerializeField] private Transform woundedListParent;
        [SerializeField] private GameObject woundedRowPrefab;

        [Header("Actions")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button refreshButton;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            refreshButton.onClick.AddListener(() => HospitalManager.Instance?.RefreshNow());
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (HospitalManager.Instance != null)
                HospitalManager.Instance.OnHospitalUpdated += RefreshUI;

            RefreshUI();
        }

        private void OnDisable()
        {
            if (HospitalManager.Instance != null)
                HospitalManager.Instance.OnHospitalUpdated -= RefreshUI;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            HospitalManager.Instance?.RefreshNow();
        }

        private void RefreshUI()
        {
            if (!HospitalManager.Instance?.IsLoaded ?? true) return;

            var state = HospitalManager.Instance.HospitalState;
            if (state == null) return;

            int total    = state.totalWounded;
            int capacity = state.capacity;

            totalWoundedText.text  = $"Wounded: {total}";
            capacityText.text      = $"Capacity: {total} / {capacity}";
            healRateText.text      = $"Heal rate: {state.healRatePerMinute:F1}/min";
            capacityBar.value      = capacity > 0 ? (float)total / capacity : 0f;

            float minsToHeal = state.healRatePerMinute > 0 ? total / state.healRatePerMinute : 0f;
            estimatedClearText.text = total > 0
                ? $"Clear in ~{FormatTime(minsToHeal)}"
                : "Hospital empty";

            BuildWoundedList(state.woundedTroops);
        }

        private void BuildWoundedList(Dictionary<string, int> wounded)
        {
            foreach (Transform child in woundedListParent)
                Destroy(child.gameObject);

            if (wounded == null) return;

            var sorted = new List<(string key, int count)>();
            foreach (var kvp in wounded)
                sorted.Add((kvp.Key, kvp.Value));

            sorted.Sort((a, b) => GetTier(b.key) - GetTier(a.key));

            foreach (var (key, count) in sorted)
            {
                var row  = Instantiate(woundedRowPrefab, woundedListParent);
                var text = row.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = $"{key}: {count}";
            }
        }

        private static int GetTier(string key)
        {
            var match = System.Text.RegularExpressions.Regex.Match(key, @"_t(\d)$");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static string FormatTime(float minutes)
        {
            if (minutes < 60) return $"{Mathf.CeilToInt(minutes)}m";
            int hrs  = Mathf.FloorToInt(minutes / 60);
            int mins = Mathf.CeilToInt(minutes % 60);
            return mins > 0 ? $"{hrs}h {mins}m" : $"{hrs}h";
        }
    }
}
