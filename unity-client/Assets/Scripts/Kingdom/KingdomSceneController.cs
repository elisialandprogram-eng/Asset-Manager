using UnityEngine;
using EternalKingdoms.Core;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// Scene controller for Kingdom.unity.
    ///
    /// Responsibilities:
    /// - Manages the loading overlay
    /// - Wires scene UI canvas into UIManager
    /// - Exposes the back-to-World button
    ///
    /// Kingdom scene hierarchy:
    ///
    ///   KingdomSceneRoot
    ///   ├── KingdomStateManager       (manages data)
    ///   ├── KingdomTerrain            (stylized plateau mesh)
    ///   ├── BuildingNodes             (12 building slot GameObjects)
    ///   ├── Palace                    (central palace placeholder)
    ///   ├── IsometricCamera           (Cinemachine virtual cam)
    ///   └── Canvas_HUD                (resource HUD, queue timers)
    /// </summary>
    public class KingdomSceneController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private UnityEngine.Canvas hudCanvas;
        [SerializeField] private UnityEngine.Canvas popupCanvas;

        private void Awake()
        {
            GameManager.Instance?.SetState(GameState.Kingdom);
            UI.UIManager.Instance?.RegisterSceneCanvases(hudCanvas, popupCanvas);
        }

        private void Start()
        {
            ShowLoading(true);
            // KingdomStateManager.Start() handles the initial data fetch
            // and calls ShowLoading(false) when ready.
        }

        public void ShowLoading(bool visible)
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(visible);
        }

        public void OnBackToWorldClicked()
        {
            SceneController.Instance.GoToWorld();
        }
    }
}
