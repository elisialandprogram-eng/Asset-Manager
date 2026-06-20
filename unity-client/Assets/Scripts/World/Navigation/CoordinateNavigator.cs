using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EternalKingdoms.World.Grid;

namespace EternalKingdoms.World.Navigation
{
    /// <summary>
    /// "Go To Coordinate" feature.
    ///
    /// The player enters X and Y (backend integer space 0–10,000).
    /// CoordinateNavigator validates the input, converts to Unity world space,
    /// and triggers WorldCameraController.FlyTo() for smooth animated travel.
    ///
    /// Future: bookmarks. The data model is ready — each validated jump
    /// can be saved to SaveManager as a named bookmark entry.
    ///
    /// UI wiring (via Inspector):
    ///   xField / yField  → TMP_InputField (integer, 0–10000)
    ///   goButton         → calls Navigate()
    ///   errorLabel       → shows validation errors
    /// </summary>
    public class CoordinateNavigator : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_InputField xField;
        [SerializeField] private TMP_InputField yField;
        [SerializeField] private Button goButton;
        [SerializeField] private TextMeshProUGUI errorLabel;
        [SerializeField] private TextMeshProUGUI previewLabel;

        [Header("References")]
        [SerializeField] private WorldCameraController worldCamera;

        private void Awake()
        {
            goButton?.onClick.AddListener(Navigate);
            xField?.onValueChanged.AddListener(_ => UpdatePreview());
            yField?.onValueChanged.AddListener(_ => UpdatePreview());

            // Integer-only input
            if (xField != null) xField.contentType = TMP_InputField.ContentType.IntegerNumber;
            if (yField != null) yField.contentType = TMP_InputField.ContentType.IntegerNumber;

            ClearError();
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open()  { if (panelRoot != null) panelRoot.SetActive(true); }
        public void Close() { if (panelRoot != null) panelRoot.SetActive(false); }
        public void Toggle() { if (panelRoot != null) panelRoot.SetActive(!panelRoot.activeSelf); }

        public void Navigate()
        {
            ClearError();

            if (!TryParseInput(xField, "X", out int bx)) return;
            if (!TryParseInput(yField, "Y", out int by)) return;

            if (!WorldCoordinate.IsValidBackend(bx, by))
            {
                ShowError($"Coordinates must be 0–10,000. Got ({bx}, {by}).");
                return;
            }

            var coord    = WorldCoordinate.FromBackend(bx, by);
            var unityPos = coord.ToUnityCenter();

            worldCamera?.FlyTo(unityPos);
            Close();

            Debug.Log($"[CoordinateNavigator] Navigating to backend ({bx},{by}) → tile {coord} → Unity {unityPos}");
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void UpdatePreview()
        {
            if (previewLabel == null) return;
            bool xOk = int.TryParse(xField?.text, out int bx);
            bool yOk = int.TryParse(yField?.text, out int by);

            if (xOk && yOk && WorldCoordinate.IsValidBackend(bx, by))
            {
                var coord = WorldCoordinate.FromBackend(bx, by);
                previewLabel.text = $"Tile: {coord} | Zone: {WorldGrid.GetZoneName(coord)}";
            }
            else
            {
                previewLabel.text = "";
            }
        }

        private bool TryParseInput(TMP_InputField field, string label, out int value)
        {
            value = 0;
            if (field == null || !int.TryParse(field.text, out value))
            {
                ShowError($"Enter a valid {label} coordinate (0–10,000).");
                return false;
            }
            return true;
        }

        private void ShowError(string msg)
        {
            if (errorLabel != null) { errorLabel.text = msg; errorLabel.gameObject.SetActive(true); }
        }

        private void ClearError()
        {
            if (errorLabel != null) { errorLabel.text = ""; errorLabel.gameObject.SetActive(false); }
        }
    }
}
