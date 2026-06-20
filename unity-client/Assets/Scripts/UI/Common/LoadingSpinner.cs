using UnityEngine;
using UnityEngine.UI;

namespace EternalKingdoms.UI.Common
{
    /// <summary>
    /// Animated loading spinner using UI Image rotation.
    /// Attach to any Image GameObject; it will spin continuously while active.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LoadingSpinner : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = -200f; // deg/sec, negative = clockwise

        private RectTransform _rt;

        private void Awake() => _rt = GetComponent<RectTransform>();

        private void Update()
        {
            _rt.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
