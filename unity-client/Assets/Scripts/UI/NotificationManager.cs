using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace EternalKingdoms.UI
{
    public enum NotificationType { Info, Success, Warning, Error }

    /// <summary>
    /// Spawns and manages toast notification banners.
    /// Notifications auto-dismiss after a configurable duration.
    /// Stacks up to maxVisible toasts vertically.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float defaultDuration = 3.5f;
        [SerializeField] private int maxVisible = 4;
        [SerializeField] private float stackSpacing = 70f;

        [Header("Prefab")]
        [SerializeField] private GameObject toastPrefab;

        private readonly Queue<ToastRequest> _queue = new();
        private readonly List<GameObject> _visible = new();
        private bool _processing;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(string message, NotificationType type = NotificationType.Info, float duration = -1f)
        {
            _queue.Enqueue(new ToastRequest
            {
                message = message,
                type = type,
                duration = duration < 0 ? defaultDuration : duration
            });
            if (!_processing) StartCoroutine(ProcessQueue());
        }

        public void ShowError(string message) => Show(message, NotificationType.Error, 5f);
        public void ShowSuccess(string message) => Show(message, NotificationType.Success);
        public void ShowWarning(string message) => Show(message, NotificationType.Warning);

        // ── Internal ──────────────────────────────────────────────────────────

        private IEnumerator ProcessQueue()
        {
            _processing = true;
            while (_queue.Count > 0)
            {
                if (_visible.Count < maxVisible)
                {
                    var req = _queue.Dequeue();
                    SpawnToast(req);
                }
                yield return new WaitForSeconds(0.3f);
            }
            _processing = false;
        }

        private void SpawnToast(ToastRequest req)
        {
            var parent = UIManager.Instance?.ToastCanvas?.transform ?? transform;
            GameObject toast;

            if (toastPrefab != null)
                toast = Instantiate(toastPrefab, parent);
            else
            {
                // Fallback: create a minimal toast at runtime
                toast = CreateFallbackToast(req.message, req.type, parent);
            }

            _visible.Add(toast);
            RepositionToasts();
            StartCoroutine(DismissAfter(toast, req.duration));
        }

        private IEnumerator DismissAfter(GameObject toast, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_visible.Contains(toast)) _visible.Remove(toast);
            if (toast != null) Destroy(toast);
            RepositionToasts();
        }

        private void RepositionToasts()
        {
            for (int i = 0; i < _visible.Count; i++)
            {
                if (_visible[i] == null) continue;
                var rt = _visible[i].GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(0, i * stackSpacing);
            }
        }

        private GameObject CreateFallbackToast(string message, NotificationType type, Transform parent)
        {
            var go = new GameObject("Toast");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 56);
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = type switch
            {
                NotificationType.Success => new Color(0.1f, 0.6f, 0.2f, 0.9f),
                NotificationType.Warning => new Color(0.8f, 0.5f, 0.0f, 0.9f),
                NotificationType.Error => new Color(0.8f, 0.1f, 0.1f, 0.9f),
                _ => new Color(0.1f, 0.2f, 0.5f, 0.9f),
            };
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 14f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8, 4);
            trt.offsetMax = new Vector2(-8, -4);
            return go;
        }

        private struct ToastRequest
        {
            public string message;
            public NotificationType type;
            public float duration;
        }
    }
}
