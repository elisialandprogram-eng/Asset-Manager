using UnityEngine;
using System.Collections.Generic;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// Top-level UI system manager.
    /// Owns the canvas layer stack and coordinates between PopupManager,
    /// NotificationManager, and scene-level HUDs.
    /// Persists across scenes.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Canvas Layers")]
        [SerializeField] private Canvas worldCanvas;        // World/HUD elements
        [SerializeField] private Canvas popupCanvas;        // Modal popups
        [SerializeField] private Canvas overlayCanvas;      // Loading / fullscreen overlays
        [SerializeField] private Canvas toastCanvas;        // Toast notifications (topmost)

        public Canvas WorldCanvas => worldCanvas;
        public Canvas PopupCanvas => popupCanvas;
        public Canvas OverlayCanvas => overlayCanvas;
        public Canvas ToastCanvas => toastCanvas;

        // Track which popups are open so we can block input correctly
        private readonly Stack<GameObject> _popupStack = new();

        public bool HasOpenPopup => _popupStack.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Popup stack ───────────────────────────────────────────────────────

        public void PushPopup(GameObject popup)
        {
            _popupStack.Push(popup);
            popup.SetActive(true);
        }

        public void PopPopup()
        {
            if (_popupStack.Count == 0) return;
            var top = _popupStack.Pop();
            if (top != null) top.SetActive(false);
        }

        public void CloseAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                var p = _popupStack.Pop();
                if (p != null) p.SetActive(false);
            }
        }

        // ── Scene transition hook ─────────────────────────────────────────────

        /// <summary>
        /// Called by scene controllers on scene load to register their
        /// canvases. Falls back to the persistent canvases if not provided.
        /// </summary>
        public void RegisterSceneCanvases(Canvas world = null, Canvas popup = null)
        {
            if (world != null) worldCanvas = world;
            if (popup != null) popupCanvas = popup;
        }
    }
}
