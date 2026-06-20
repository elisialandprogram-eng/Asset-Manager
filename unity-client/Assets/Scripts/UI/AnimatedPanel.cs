using UnityEngine;
using System;
using System.Collections;

namespace EternalKingdoms.UI
{
    /// <summary>
    /// AnimatedPanel — Base class for all UI panels providing entrance/exit animations.
    ///
    /// Phase 5 (U5.8) — all panels inherit this class.
    ///
    /// Animation modes:
    ///   SlideUp    — slides up from below screen with eased overshoot
    ///   SlideRight — slides in from right
    ///   ScaleIn    — scales from 0.85 to 1.0 with fade
    ///   Fade       — simple alpha fade
    ///
    /// Architecture:
    ///   - CanvasGroup for alpha control (also handles raycasting block during animation)
    ///   - RectTransform for position/scale animation
    ///   - Coroutine-based — no DOTween dependency (Phase 5 uses LeanTween/coroutines)
    ///   - OnShowComplete / OnHideComplete events for chaining
    ///   - All derived panels call Show() / Hide() — never SetActive directly
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class AnimatedPanel : MonoBehaviour
    {
        public enum AnimType { SlideUp, SlideRight, ScaleIn, Fade }

        [Header("Animation")]
        [SerializeField] private AnimType animType       = AnimType.ScaleIn;
        [SerializeField] private float    showDuration   = 0.22f;
        [SerializeField] private float    hideDuration   = 0.16f;
        [SerializeField] private float    slideDistance  = 60f;
        [SerializeField] private bool     blockRaycastDuringAnim = true;

        protected CanvasGroup   CG   { get; private set; }
        protected RectTransform RT   { get; private set; }

        public bool IsVisible { get; private set; }

        public event Action OnShowComplete;
        public event Action OnHideComplete;

        private Coroutine _animCoroutine;

        protected virtual void Awake()
        {
            CG = GetComponent<CanvasGroup>();
            RT = GetComponent<RectTransform>();
            CG.alpha          = 0f;
            CG.interactable   = false;
            CG.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            gameObject.SetActive(true);
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(ShowCoroutine());
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(HideCoroutine());
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            CG.alpha          = 1f;
            CG.interactable   = true;
            CG.blocksRaycasts = true;
            RT.localScale     = Vector3.one;
            RT.anchoredPosition = Vector2.zero;
            IsVisible = true;
        }

        public void HideImmediate()
        {
            CG.alpha          = 0f;
            CG.interactable   = false;
            CG.blocksRaycasts = false;
            IsVisible = false;
            gameObject.SetActive(false);
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator ShowCoroutine()
        {
            IsVisible = false;
            CG.interactable   = !blockRaycastDuringAnim;
            CG.blocksRaycasts = !blockRaycastDuringAnim;

            Vector2 startPos   = GetHiddenPosition();
            Vector3 startScale = GetHiddenScale();

            RT.anchoredPosition = startPos;
            RT.localScale       = startScale;
            CG.alpha            = 0f;

            float elapsed = 0f;
            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutBack(elapsed / showDuration);

                CG.alpha            = Mathf.Lerp(0f, 1f, elapsed / showDuration);
                RT.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, t);
                RT.localScale       = Vector3.Lerp(startScale, Vector3.one, t);
                yield return null;
            }

            CG.alpha            = 1f;
            RT.anchoredPosition = Vector2.zero;
            RT.localScale       = Vector3.one;
            CG.interactable     = true;
            CG.blocksRaycasts   = true;
            IsVisible           = true;
            OnShowComplete?.Invoke();

            UIThemeManager.Instance?.PlayUISfx("panel_open");
        }

        private IEnumerator HideCoroutine()
        {
            CG.interactable   = false;
            CG.blocksRaycasts = false;

            Vector2 endPos   = GetHiddenPosition() * 0.5f;
            Vector3 endScale = GetHiddenScale();

            float startAlpha = CG.alpha;
            float elapsed    = 0f;
            while (elapsed < hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseInQuad(elapsed / hideDuration);

                CG.alpha            = Mathf.Lerp(startAlpha, 0f, t);
                RT.anchoredPosition = Vector2.Lerp(Vector2.zero, endPos, t);
                RT.localScale       = Vector3.Lerp(Vector3.one, endScale, t);
                yield return null;
            }

            CG.alpha = 0f;
            IsVisible = false;
            gameObject.SetActive(false);
            OnHideComplete?.Invoke();
        }

        // ── Easing ────────────────────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInQuad(float t) => t * t;

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector2 GetHiddenPosition() => animType switch
        {
            AnimType.SlideUp    => new Vector2(0f, -slideDistance),
            AnimType.SlideRight => new Vector2(slideDistance, 0f),
            _                   => Vector2.zero,
        };

        private Vector3 GetHiddenScale() => animType switch
        {
            AnimType.ScaleIn => new Vector3(0.88f, 0.88f, 1f),
            _                => Vector3.one,
        };
    }
}
