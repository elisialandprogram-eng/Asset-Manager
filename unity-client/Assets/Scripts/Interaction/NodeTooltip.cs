using UnityEngine;
using TMPro;

namespace EternalKingdoms.Interaction
{
    /// <summary>
    /// Floating tooltip that follows the cursor and displays
    /// node information on hover.
    ///
    /// Attach to a Canvas child. Set the tooltip singleton reference
    /// on scene Awake; SelectableEntity hover events drive Show/Hide.
    /// </summary>
    public class NodeTooltip : MonoBehaviour
    {
        public static NodeTooltip Instance { get; private set; }

        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI typeLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Vector2 offset = new(16f, -16f);

        private RectTransform _canvasRT;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvasRT = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            Hide();
        }

        private void Update()
        {
            if (tooltipPanel != null && tooltipPanel.gameObject.activeSelf)
                FollowCursor();
        }

        public void Show(string nodeName, string nodeType, string nodeStatus)
        {
            if (nameLabel != null) nameLabel.text = nodeName;
            if (typeLabel != null) typeLabel.text = nodeType;
            if (statusLabel != null) statusLabel.text = nodeStatus;
            if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
        }

        private void FollowCursor()
        {
            if (_canvasRT == null) return;
            var screenPos = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, screenPos, null, out Vector2 local);
            tooltipPanel.anchoredPosition = local + offset;
        }
    }
}
