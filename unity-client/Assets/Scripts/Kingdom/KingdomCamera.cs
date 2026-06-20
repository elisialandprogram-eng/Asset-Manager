using UnityEngine;
using EternalKingdoms.Camera;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// Kingdom scene camera controller.
    /// Inherits the shared isometric camera base and applies
    /// kingdom-specific bounds (kingdom footprint is ~300×300 units).
    ///
    /// Fixed isometric angle: X=60°, Y=-45°
    /// Camera follows a pivot point that pans within bounds.
    /// </summary>
    public class KingdomCamera : IsometricCameraController
    {
        [Header("Kingdom Bounds")]
        [SerializeField] private Vector2 boundsMin = new(-150f, -150f);
        [SerializeField] private Vector2 boundsMax = new(150f, 150f);
        [SerializeField] private float minZoom = 8f;
        [SerializeField] private float maxZoom = 60f;
        [SerializeField] private float initialZoom = 25f;

        protected override void Awake()
        {
            base.Awake();
            PanBoundsMin = boundsMin;
            PanBoundsMax = boundsMax;
            MinOrthographicSize = minZoom;
            MaxOrthographicSize = maxZoom;
            CurrentOrthographicSize = initialZoom;
        }

        protected override void Start()
        {
            base.Start();
            // Kingdom origin is (0,0,0); camera starts looking at Palace
            SetPivot(Vector3.zero);
        }
    }
}
