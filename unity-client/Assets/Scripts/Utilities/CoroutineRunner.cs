using UnityEngine;
using System.Collections;

namespace EternalKingdoms.Utilities
{
    /// <summary>
    /// Persistent MonoBehaviour that allows non-MonoBehaviour classes
    /// (services, plain C# objects) to start coroutines.
    ///
    /// Usage:
    ///   CoroutineRunner.Instance.Run(MyCoroutine());
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        public static CoroutineRunner Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);
        public void Stop(Coroutine coroutine) { if (coroutine != null) StopCoroutine(coroutine); }
    }
}
