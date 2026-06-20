using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EternalKingdoms.UI.Common
{
    /// <summary>
    /// Fullscreen popup displayed when the backend is unreachable.
    /// Provides a Retry button that re-triggers the failed operation.
    /// Attach to a Canvas GameObject in every scene.
    /// </summary>
    public class NetworkErrorPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button dismissButton;

        private System.Action _onRetry;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            retryButton?.onClick.AddListener(OnRetryClicked);
            dismissButton?.onClick.AddListener(Hide);
        }

        public void Show(string message = null, System.Action onRetry = null)
        {
            _onRetry = onRetry;
            if (messageLabel != null)
                messageLabel.text = message ?? "Unable to connect to the server.\nPlease check your internet connection.";
            if (panel != null) panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnRetryClicked()
        {
            Hide();
            _onRetry?.Invoke();
        }
    }
}
