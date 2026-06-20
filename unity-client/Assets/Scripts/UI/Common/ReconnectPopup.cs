using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using EternalKingdoms.Networking;

namespace EternalKingdoms.UI.Common
{
    /// <summary>
    /// Shown when a previously-connected session drops.
    /// Polls the /healthz endpoint and auto-dismisses on reconnect.
    /// </summary>
    public class ReconnectPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Button manualRetryButton;

        private Coroutine _pollCoroutine;
        private float _pollInterval = 5f;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            manualRetryButton?.onClick.AddListener(OnManualRetry);
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            if (statusLabel != null) statusLabel.text = "Connection lost. Reconnecting...";
            _pollCoroutine = StartCoroutine(PollReconnect());
        }

        public void Hide()
        {
            if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
            if (panel != null) panel.SetActive(false);
        }

        private IEnumerator PollReconnect()
        {
            int attempts = 0;
            while (true)
            {
                yield return new WaitForSeconds(_pollInterval);
                attempts++;
                if (statusLabel != null)
                    statusLabel.text = $"Reconnecting... (attempt {attempts})";

                if (NetworkManager.Instance != null && NetworkManager.Instance.IsOnline)
                {
                    if (statusLabel != null) statusLabel.text = "Reconnected!";
                    yield return new WaitForSeconds(1f);
                    Hide();
                    yield break;
                }
            }
        }

        private void OnManualRetry()
        {
            if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
            _pollCoroutine = StartCoroutine(PollReconnect());
        }
    }
}
