using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalKingdoms.Core;
using EternalKingdoms.World.Grid;
using EternalKingdoms.World.Navigation;

namespace EternalKingdoms.World.UI
{
    /// <summary>
    /// World scene top navigation bar.
    ///
    /// Buttons:
    ///   My Kingdom    — navigate to GoToKingdom() → GoToKingdom scene
    ///   Search        — open CoordinateNavigator panel
    ///   Bookmarks     — placeholder (Phase 5+)
    ///   Center        — fly camera to own kingdom
    ///
    /// Kingdom name displayed in top-left.
    /// World name / season displayed in top-center.
    /// </summary>
    public class WorldTopBar : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI kingdomNameLabel;
        [SerializeField] private TextMeshProUGUI worldNameLabel;

        [Header("Buttons")]
        [SerializeField] private Button myKingdomButton;
        [SerializeField] private Button searchButton;
        [SerializeField] private Button bookmarksButton;
        [SerializeField] private Button centerButton;

        [Header("References")]
        [SerializeField] private CoordinateNavigator coordinateNavigator;
        [SerializeField] private WorldCameraController worldCamera;
        [SerializeField] private WorldSceneController sceneController;

        private string _myKingdomId;
        private WorldCoordinate _myKingdomCoord;

        private void Awake()
        {
            myKingdomButton?.onClick.AddListener(OnMyKingdomClicked);
            searchButton?.onClick.AddListener(OnSearchClicked);
            bookmarksButton?.onClick.AddListener(OnBookmarksClicked);
            centerButton?.onClick.AddListener(OnCenterClicked);

            // Bookmarks placeholder
            if (bookmarksButton != null)
                bookmarksButton.interactable = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetKingdomInfo(string kingdomName, string kingdomId, int mapX, int mapY)
        {
            _myKingdomId    = kingdomId;
            _myKingdomCoord = WorldCoordinate.FromBackend(mapX, mapY);

            if (kingdomNameLabel != null) kingdomNameLabel.text = kingdomName;
        }

        public void SetWorldInfo(string worldName, int season)
        {
            if (worldNameLabel != null) worldNameLabel.text = $"{worldName} — Season {season}";
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnMyKingdomClicked()
        {
            if (!string.IsNullOrEmpty(_myKingdomId))
                sceneController?.GoToKingdom(_myKingdomId);
        }

        private void OnSearchClicked()
        {
            coordinateNavigator?.Toggle();
        }

        private void OnCenterClicked()
        {
            if (_myKingdomCoord.IsValid)
                worldCamera?.FlyTo(_myKingdomCoord);
        }

        private void OnBookmarksClicked()
        {
            EternalKingdoms.UI.NotificationManager.Instance?.Show("Bookmarks coming in a future update.", EternalKingdoms.UI.NotificationType.Info);
        }
    }
}
