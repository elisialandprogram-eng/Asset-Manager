using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// UIThemeManager — Global AAA fantasy UI theme system.
    ///
    /// Phase 5 (U5.8) visual language:
    ///   Dark Medieval Fantasy — dark stone backgrounds, gold accent borders,
    ///   parchment panel interiors, animated transitions, glow highlights.
    ///
    /// Covers:
    ///   HUD | Panels | Buttons | Tooltips | Battle Reports | Hospital |
    ///   Inventory | Login screen
    ///
    /// Architecture:
    ///   - Singleton, DontDestroyOnLoad
    ///   - ThemeData ScriptableObject holds all colors/fonts/sprites
    ///   - ApplyTheme() called once at scene load; re-applied on scene change
    ///   - All panels extend AnimatedPanel for entrance/exit transitions
    ///   - Button states: Normal → Hover (gold glow) → Pressed → Disabled
    ///   - Font: TMP font asset with gold primary, cream secondary, red alert
    ///   - Sprite library: 9-slice panel backgrounds, border ornaments, icon frames
    /// </summary>
    public class UIThemeManager : MonoBehaviour
    {
        public static UIThemeManager Instance { get; private set; }

        [Header("Theme Data")]
        [SerializeField] private UIThemeData themeData;

        [Header("Root Canvases — register all top-level canvases")]
        [SerializeField] private Canvas[] rootCanvases;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ApplyThemeToAll();
        }

        public void ApplyThemeToAll()
        {
            foreach (var canvas in rootCanvases)
                if (canvas != null) ApplyThemeToCanvas(canvas.transform);
        }

        /// <summary>Apply a specific UIThemeData to all registered canvases.</summary>
        public void ApplyTheme(UIThemeData data)
        {
            themeData = data;
            ApplyThemeToAll();
        }

        public void ApplyThemeToCanvas(Transform root)
        {
            if (themeData == null) return;
            ApplyRecursive(root);
        }

        private void ApplyRecursive(Transform t)
        {
            // Themed button
            var btn = t.GetComponent<ThemedButton>();
            if (btn != null) btn.Apply(themeData);

            // Themed panel background
            var panel = t.GetComponent<ThemedPanel>();
            if (panel != null) panel.Apply(themeData);

            // Themed label
            var label = t.GetComponent<ThemedLabel>();
            if (label != null) label.Apply(themeData);

            for (int i = 0; i < t.childCount; i++)
                ApplyRecursive(t.GetChild(i));
        }

        /// <summary>Play a UI SFX by key through AudioManager.</summary>
        public void PlayUISfx(string sfxKey)
        {
            // Forwarded to AudioManager (Phase 5, U5.9)
            EternalKingdoms.Audio.AudioManager.Instance?.PlayUI(sfxKey);
        }
    }

    // ── Theme Data ScriptableObject ───────────────────────────────────────────

    [CreateAssetMenu(fileName = "UIThemeData", menuName = "EK/UI/Theme Data")]
    public class UIThemeData : ScriptableObject
    {
        [Header("Colors")]
        public Color backgroundDark     = new Color(0.08f, 0.06f, 0.04f, 0.97f);
        public Color backgroundMid      = new Color(0.14f, 0.10f, 0.07f, 0.95f);
        public Color backgroundLight    = new Color(0.22f, 0.17f, 0.11f, 0.92f);
        public Color goldPrimary        = new Color(0.93f, 0.78f, 0.35f);
        public Color goldSecondary      = new Color(0.70f, 0.56f, 0.20f);
        public Color textPrimary        = new Color(0.94f, 0.90f, 0.80f);
        public Color textSecondary      = new Color(0.70f, 0.65f, 0.55f);
        public Color textAlert          = new Color(0.98f, 0.30f, 0.20f);
        public Color buttonNormal       = new Color(0.25f, 0.18f, 0.10f);
        public Color buttonHover        = new Color(0.50f, 0.38f, 0.10f);
        public Color buttonPressed      = new Color(0.18f, 0.13f, 0.07f);
        public Color buttonDisabled     = new Color(0.20f, 0.18f, 0.16f, 0.5f);

        [Header("Fonts")]
        public TMP_FontAsset fontPrimary;
        public TMP_FontAsset fontSecondary;

        [Header("Sprites")]
        public Sprite panelBackground;         // 9-slice dark stone
        public Sprite panelBorderGold;         // 9-slice gold ornament border
        public Sprite buttonBackground;        // 9-slice button
        public Sprite buttonBorderGold;        // 9-slice gold border
        public Sprite iconFrameCommon;
        public Sprite iconFrameRare;
        public Sprite iconFrameEpic;
        public Sprite iconFrameLegendary;
        public Sprite scrollbarBackground;
        public Sprite scrollbarHandle;

        [Header("Glow Materials")]
        public Material goldGlowMaterial;
        public Material alertGlowMaterial;
    }

    // ── Themed Components ─────────────────────────────────────────────────────

    /// <summary>Mark a Button as themed — UIThemeManager applies gold hover/press states.</summary>
    public class ThemedButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image  background;
        [SerializeField] private Image  border;

        public void Apply(UIThemeData theme)
        {
            if (button == null) button = GetComponent<Button>();
            if (background == null) background = GetComponent<Image>();

            if (background != null && theme.panelBackground != null)
                background.sprite = theme.buttonBackground;

            var colors = button.colors;
            colors.normalColor      = theme.buttonNormal;
            colors.highlightedColor = theme.buttonHover;
            colors.pressedColor     = theme.buttonPressed;
            colors.disabledColor    = theme.buttonDisabled;
            button.colors           = colors;

            if (border != null)
            {
                border.sprite = theme.buttonBorderGold;
                border.color  = theme.goldPrimary;
            }
        }
    }

    /// <summary>Mark a panel Image as themed — UIThemeManager applies dark stone background + gold border.</summary>
    public class ThemedPanel : MonoBehaviour
    {
        public enum PanelVariant { Dark, Mid, Light }
        [SerializeField] private PanelVariant variant = PanelVariant.Mid;
        [SerializeField] private Image background;
        [SerializeField] private Image border;

        public void Apply(UIThemeData theme)
        {
            if (background == null) background = GetComponent<Image>();
            if (background != null)
            {
                background.sprite = theme.panelBackground;
                background.color  = variant switch
                {
                    PanelVariant.Dark  => theme.backgroundDark,
                    PanelVariant.Light => theme.backgroundLight,
                    _                  => theme.backgroundMid,
                };
            }
            if (border != null)
            {
                border.sprite = theme.panelBorderGold;
                border.color  = theme.goldPrimary;
            }
        }
    }

    /// <summary>Mark a TMP text as themed — applies font and color role.</summary>
    public class ThemedLabel : MonoBehaviour
    {
        public enum LabelRole { Primary, Secondary, Gold, Alert }
        [SerializeField] private LabelRole role = LabelRole.Primary;
        [SerializeField] private TextMeshProUGUI label;

        public void Apply(UIThemeData theme)
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            if (label == null) return;

            if (theme.fontPrimary != null) label.font = theme.fontPrimary;
            label.color = role switch
            {
                LabelRole.Secondary => theme.textSecondary,
                LabelRole.Gold      => theme.goldPrimary,
                LabelRole.Alert     => theme.textAlert,
                _                   => theme.textPrimary,
            };
        }
    }
}
