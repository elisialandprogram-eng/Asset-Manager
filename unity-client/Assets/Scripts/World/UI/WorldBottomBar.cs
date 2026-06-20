using UnityEngine;
using UnityEngine.UI;

namespace EternalKingdoms.World.UI
{
    /// <summary>
    /// World scene bottom navigation bar.
    ///
    /// Phase 2: all buttons shown as greyed-out placeholders.
    /// Phase 3+: buttons become active as features are implemented.
    ///
    /// Buttons (all disabled / placeholder in Phase 2):
    ///   Marches   — Phase 7 (march system)
    ///   Alliance  — Phase 6 (alliance features)
    ///   Events    — Phase 9 (seasonal events)
    ///   Rankings  — Phase 5+
    ///   Mail      — Phase 6+
    /// </summary>
    public class WorldBottomBar : MonoBehaviour
    {
        [Header("Placeholder Buttons (all disabled in Phase 2)")]
        [SerializeField] private Button marchesButton;
        [SerializeField] private Button allianceButton;
        [SerializeField] private Button eventsButton;
        [SerializeField] private Button rankingsButton;
        [SerializeField] private Button mailButton;

        private void Awake()
        {
            // All are placeholder — show coming-soon notification on click
            SetPlaceholder(marchesButton,  "March system available in a future update.");
            SetPlaceholder(allianceButton, "Alliance system available in a future update.");
            SetPlaceholder(eventsButton,   "Events available in a future update.");
            SetPlaceholder(rankingsButton, "Rankings available in a future update.");
            SetPlaceholder(mailButton,     "Mail available in a future update.");
        }

        private void SetPlaceholder(Button btn, string message)
        {
            if (btn == null) return;
            var colors = btn.colors;
            colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            btn.colors = colors;
            btn.onClick.AddListener(() =>
                EternalKingdoms.UI.NotificationManager.Instance?.Show(message, EternalKingdoms.UI.NotificationType.Info));
        }
    }
}
