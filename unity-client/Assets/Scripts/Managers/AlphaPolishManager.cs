using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace EternalKingdoms.Managers
{
    /// <summary>
    /// U5.7.6 — Alpha Polish Pass
    /// Provides camera shake, hover/button/selection sounds, smooth scene transitions
    /// with loading screens, animated context-sensitive hints, and ensures
    /// no abrupt cuts occur anywhere in the player experience.
    /// </summary>
    public class AlphaPolishManager : MonoBehaviour
    {
        public static AlphaPolishManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Camera Shake")]
        public Camera           mainCamera;
        [Range(0.01f, 0.5f)] public float shakeDecay    = 0.1f;
        public AnimationCurve   shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Audio — UI")]
        public AudioSource uiAudioSource;
        public AudioClip   sfxButtonHover;
        public AudioClip   sfxButtonClick;
        public AudioClip   sfxPanelOpen;
        public AudioClip   sfxPanelClose;
        public AudioClip   sfxSelection;
        public AudioClip   sfxError;
        public AudioClip   sfxSuccess;

        [Header("Loading Screen")]
        public GameObject     loadingScreenRoot;
        public UnityEngine.UI.Image loadingBar;
        public TMP_Text       loadingLabel;
        public float          minimumLoadTime = 1.2f;

        [Header("Scene Transition")]
        public CanvasGroup    fadeCanvas;
        [Range(0.1f, 1.5f)] public float fadeDuration = 0.4f;

        [Header("Context Hints")]
        public GameObject   hintPanel;
        public TMP_Text     hintLabel;
        public float        hintDisplayTime = 5f;

        [Header("Hints Content")]
        [TextArea(1, 2)]
        public string[] hintMessages;

        // ── State ─────────────────────────────────────────────────────────────
        private Vector3  _cameraOrigin;
        private float    _shakeIntensity;
        private Coroutine _hintRoutine;
        private Coroutine _loadRoutine;
        private int       _hintIndex;

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
            if (mainCamera != null) _cameraOrigin = mainCamera.transform.localPosition;
            SetFadeAlpha(0f);
            HideLoadingScreen();

            // Cycle hints automatically
            if (hintMessages != null && hintMessages.Length > 0)
                _hintRoutine = StartCoroutine(CycleHints());
        }

        private void Update()
        {
            if (_shakeIntensity > 0.001f) TickCameraShake();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Camera Shake
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Trigger a camera shake.  intensity 0–1, duration in seconds.</summary>
        public void ShakeCamera(float intensity = 0.25f, float duration = 0.3f)
        {
            _shakeIntensity = intensity;
            StartCoroutine(DecayShake(duration));
        }

        private void TickCameraShake()
        {
            if (mainCamera == null) return;
            float s = _shakeIntensity;
            mainCamera.transform.localPosition = _cameraOrigin +
                new Vector3(Random.Range(-s, s), Random.Range(-s, s), 0);
        }

        private IEnumerator DecayShake(float duration)
        {
            float elapsed = 0f;
            float start   = _shakeIntensity;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _shakeIntensity = Mathf.Lerp(start, 0f, shakeCurve.Evaluate(elapsed / duration));
                yield return null;
            }
            _shakeIntensity = 0f;
            if (mainCamera != null) mainCamera.transform.localPosition = _cameraOrigin;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Audio
        // ─────────────────────────────────────────────────────────────────────
        public void PlayHoverSound()   => PlayUI(sfxButtonHover, 0.4f);
        public void PlayClickSound()   => PlayUI(sfxButtonClick);
        public void PlayPanelOpen()    => PlayUI(sfxPanelOpen);
        public void PlayPanelClose()   => PlayUI(sfxPanelClose);
        public void PlaySelectionSound() => PlayUI(sfxSelection);
        public void PlayErrorSound()   => PlayUI(sfxError);
        public void PlaySuccessSound() => PlayUI(sfxSuccess);

        private void PlayUI(AudioClip clip, float volume = 1f)
        {
            if (clip != null && uiAudioSource != null)
                uiAudioSource.PlayOneShot(clip, volume);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Scene Transitions
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Smooth fade-out → load scene → fade-in, with loading screen.</summary>
        public void LoadSceneSmooth(string sceneName, string loadingText = "Loading…")
        {
            if (_loadRoutine != null) return;
            _loadRoutine = StartCoroutine(SmoothSceneLoad(sceneName, loadingText));
        }

        private IEnumerator SmoothSceneLoad(string sceneName, string text)
        {
            // Fade out
            PlayPanelClose();
            yield return StartCoroutine(FadeTo(1f));

            // Show loading screen
            ShowLoadingScreen(text);

            float loadStart = Time.realtimeSinceStartup;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                if (loadingBar != null) loadingBar.fillAmount = op.progress / 0.9f;
                yield return null;
            }

            // Enforce minimum load time so screen doesn't flash
            float elapsed = Time.realtimeSinceStartup - loadStart;
            if (elapsed < minimumLoadTime)
                yield return new WaitForSecondsRealtime(minimumLoadTime - elapsed);

            if (loadingBar != null) loadingBar.fillAmount = 1f;
            op.allowSceneActivation = true;
            yield return new WaitUntil(() => op.isDone);

            HideLoadingScreen();

            // Fade in
            yield return StartCoroutine(FadeTo(0f));
            PlayPanelOpen();

            _loadRoutine = null;
        }

        private IEnumerator FadeTo(float target)
        {
            float start = fadeCanvas != null ? fadeCanvas.alpha : 0f;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(start, target, t / fadeDuration));
                yield return null;
            }
            SetFadeAlpha(target);
        }

        private void SetFadeAlpha(float a)
        {
            if (fadeCanvas == null) return;
            fadeCanvas.alpha          = a;
            fadeCanvas.interactable   = a > 0.5f;
            fadeCanvas.blocksRaycasts = a > 0.5f;
        }

        private void ShowLoadingScreen(string text)
        {
            if (loadingScreenRoot != null) loadingScreenRoot.SetActive(true);
            if (loadingLabel      != null) loadingLabel.text = text;
            if (loadingBar        != null) loadingBar.fillAmount = 0f;
        }

        private void HideLoadingScreen()
        {
            if (loadingScreenRoot != null) loadingScreenRoot.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Context Hints
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Show a specific hint message immediately.</summary>
        public void ShowHint(string message)
        {
            if (_hintRoutine != null) StopCoroutine(_hintRoutine);
            _hintRoutine = StartCoroutine(DisplayHint(message));
        }

        private IEnumerator CycleHints()
        {
            while (true)
            {
                yield return new WaitForSeconds(20f);
                if (hintMessages.Length == 0) continue;
                _hintIndex = (_hintIndex + 1) % hintMessages.Length;
                yield return StartCoroutine(DisplayHint(hintMessages[_hintIndex]));
            }
        }

        private IEnumerator DisplayHint(string message)
        {
            if (hintPanel == null) yield break;
            if (hintLabel != null) hintLabel.text = message;

            var cg = hintPanel.GetComponent<CanvasGroup>() ?? hintPanel.AddComponent<CanvasGroup>();
            hintPanel.SetActive(true);

            // Fade in
            float t = 0f;
            while (t < 0.3f) { t += Time.deltaTime; cg.alpha = t / 0.3f; yield return null; }

            yield return new WaitForSeconds(hintDisplayTime);

            // Fade out
            t = 0f;
            while (t < 0.3f) { t += Time.deltaTime; cg.alpha = 1f - t / 0.3f; yield return null; }
            hintPanel.SetActive(false);
        }
    }
}
