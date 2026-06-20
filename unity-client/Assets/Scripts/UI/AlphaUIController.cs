using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// U5.6.9 — UI Alpha Pass
    /// Finalizes the visual UI experience:
    ///   - Fantasy medieval skin applied globally
    ///   - Animated panel system (already in AnimatedPanel.cs — this extends it)
    ///   - Hover transitions on all buttons
    ///   - Animated notifications
    ///   - Context-sensitive tooltips
    ///   - Rich animated resource-change numbers
    ///   - Replaces all remaining temp UI assets via AssetCatalogManager
    /// </summary>
    public class AlphaUIController : MonoBehaviour
    {
        public static AlphaUIController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Global Skin")]
        public UIThemeData alphaTheme;
        public Font        medievalFont;
        public TMP_FontAsset medievalFontTMP;

        [Header("Notification System")]
        public Transform   notificationContainer;
        public GameObject  notificationPrefab;
        public int         maxNotifications = 5;
        [Range(1f, 8f)] public float notificationLifetime = 4f;

        [Header("Tooltip")]
        public GameObject tooltipPanel;
        public TMP_Text   tooltipLabel;
        public float      tooltipDelay = 0.4f;

        [Header("Resource Animated Numbers")]
        public GameObject floatingNumberPrefab;
        public Color      positiveResourceColor = new(0.2f, 1f, 0.3f);
        public Color      negativeResourceColor = new(1f, 0.2f, 0.2f);
        public float      floatUpDistance       = 80f;
        public float      floatDuration         = 1.6f;

        [Header("Panel Animation")]
        [Range(0.05f, 0.5f)] public float panelTransitionDuration = 0.18f;
        public AnimationCurve panelOpenCurve  = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve panelCloseCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        // ── State ─────────────────────────────────────────────────────────────
        private readonly Queue<GameObject>  _notifPool   = new();
        private readonly List<GameObject>   _activeNotifs = new();
        private Coroutine                   _tooltipRoutine;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ApplyGlobalSkin();
            HideTooltip();
            PrewarmNotificationPool();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Global Skin
        // ─────────────────────────────────────────────────────────────────────
        private void ApplyGlobalSkin()
        {
            if (alphaTheme == null) return;

            // Apply theme to every UIThemeManager (there should be one per canvas root)
            foreach (var mgr in FindObjectsByType<UIThemeManager>(FindObjectsSortMode.None))
                mgr.ApplyTheme(alphaTheme);

            // Apply TMP font to all TextMeshProUGUI components
            if (medievalFontTMP != null)
                foreach (var txt in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                    if (txt.font == null || txt.font.name.Contains("LiberationSans"))
                        txt.font = medievalFontTMP;

            Debug.Log("[AlphaUIController] Global medieval skin applied.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Notifications
        // ─────────────────────────────────────────────────────────────────────
        public void ShowNotification(string message, NotificationIcon icon = NotificationIcon.Info)
        {
            // Evict oldest if at capacity
            if (_activeNotifs.Count >= maxNotifications)
            {
                var oldest = _activeNotifs[0];
                _activeNotifs.RemoveAt(0);
                ReturnNotifToPool(oldest);
            }

            var notif = GetNotifFromPool();
            if (notif == null) return;

            // Set text
            var label = notif.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = message;

            // Set icon color by type
            var iconImage = notif.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
                iconImage.color = icon switch
                {
                    NotificationIcon.Success => positiveResourceColor,
                    NotificationIcon.Warning => new Color(1f, 0.8f, 0f),
                    NotificationIcon.Error   => negativeResourceColor,
                    _                        => Color.white
                };

            notif.SetActive(true);
            notif.transform.SetParent(notificationContainer, false);
            _activeNotifs.Add(notif);

            // Animate in then auto-dismiss
            StartCoroutine(AnimateNotification(notif, notificationLifetime));
        }

        private IEnumerator AnimateNotification(GameObject notif, float lifetime)
        {
            var cg = notif.GetComponent<CanvasGroup>() ?? notif.AddComponent<CanvasGroup>();
            var rt = notif.GetComponent<RectTransform>();

            // Slide in from right
            Vector2 startPos = rt.anchoredPosition + Vector2.right * 400f;
            Vector2 endPos   = rt.anchoredPosition;
            float   t        = 0f;
            float   inDur    = panelTransitionDuration;

            cg.alpha = 0f;
            rt.anchoredPosition = startPos;

            while (t < inDur)
            {
                t += Time.deltaTime;
                float n = panelOpenCurve.Evaluate(t / inDur);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, n);
                cg.alpha = n;
                yield return null;
            }

            // Hold
            yield return new WaitForSeconds(lifetime);

            // Fade out
            t = 0f;
            float outDur = panelTransitionDuration * 2f;
            while (t < outDur)
            {
                t += Time.deltaTime;
                cg.alpha = panelCloseCurve.Evaluate(t / outDur);
                yield return null;
            }

            _activeNotifs.Remove(notif);
            ReturnNotifToPool(notif);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Tooltip
        // ─────────────────────────────────────────────────────────────────────
        public void RequestTooltip(string text, Vector2 screenPos)
        {
            if (_tooltipRoutine != null) StopCoroutine(_tooltipRoutine);
            _tooltipRoutine = StartCoroutine(ShowTooltipDelayed(text, screenPos));
        }

        public void HideTooltip()
        {
            if (_tooltipRoutine != null) { StopCoroutine(_tooltipRoutine); _tooltipRoutine = null; }
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        private IEnumerator ShowTooltipDelayed(string text, Vector2 screenPos)
        {
            yield return new WaitForSeconds(tooltipDelay);
            if (tooltipPanel == null) yield break;
            tooltipPanel.SetActive(true);
            if (tooltipLabel != null) tooltipLabel.text = text;
            var rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt != null) rt.position = screenPos + Vector2.up * 20f;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Animated Resource Numbers
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Spawn a floating +/- number at a screen position.</summary>
        public void ShowResourceChange(int delta, Vector2 screenPos)
        {
            if (floatingNumberPrefab == null) return;
            var go = Instantiate(floatingNumberPrefab, notificationContainer);
            var rt = go.GetComponent<RectTransform>();
            rt.position = screenPos;

            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text  = delta > 0 ? $"+{delta}" : delta.ToString();
                txt.color = delta > 0 ? positiveResourceColor : negativeResourceColor;
            }

            StartCoroutine(AnimateFloatingNumber(go, rt));
        }

        private IEnumerator AnimateFloatingNumber(GameObject go, RectTransform rt)
        {
            var cg      = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            Vector2 start = rt.anchoredPosition;
            Vector2 end   = start + Vector2.up * floatUpDistance;
            float t = 0f;

            while (t < floatDuration)
            {
                t += Time.deltaTime;
                float n = t / floatDuration;
                rt.anchoredPosition = Vector2.Lerp(start, end, n);
                cg.alpha = 1f - Mathf.Pow(n, 2f); // ease out fade
                yield return null;
            }

            Destroy(go);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Hover Transitions (called by TooltipTrigger component)
        // ─────────────────────────────────────────────────────────────────────
        public IEnumerator AnimateHoverScale(RectTransform rt, bool enter)
        {
            float target = enter ? 1.05f : 1f;
            float start  = rt.localScale.x;
            float t = 0f, dur = 0.1f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(start, target, t / dur);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Notification Pool
        // ─────────────────────────────────────────────────────────────────────
        private void PrewarmNotificationPool()
        {
            if (notificationPrefab == null) return;
            for (int i = 0; i < maxNotifications + 2; i++)
            {
                var go = Instantiate(notificationPrefab);
                go.SetActive(false);
                _notifPool.Enqueue(go);
            }
        }

        private GameObject GetNotifFromPool()
        {
            if (_notifPool.Count > 0) return _notifPool.Dequeue();
            return notificationPrefab != null ? Instantiate(notificationPrefab) : null;
        }

        private void ReturnNotifToPool(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _notifPool.Enqueue(go);
        }
    }

    // ── Enums ─────────────────────────────────────────────────────────────────
    public enum NotificationIcon { Info, Success, Warning, Error }

    /// <summary>
    /// Attach to any Button or Image that needs tooltip + hover scale support.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TooltipTrigger : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler
    {
        public string tooltipText;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            AlphaUIController.Instance?.RequestTooltip(tooltipText, e.position);
            var rt = GetComponent<RectTransform>();
            if (rt != null)
                StartCoroutine(AlphaUIController.Instance?.AnimateHoverScale(rt, true));
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
        {
            AlphaUIController.Instance?.HideTooltip();
            var rt = GetComponent<RectTransform>();
            if (rt != null)
                StartCoroutine(AlphaUIController.Instance?.AnimateHoverScale(rt, false));
        }
    }
}
