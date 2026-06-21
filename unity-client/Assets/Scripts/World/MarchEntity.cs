using System;
using UnityEngine;

namespace EternalKingdoms.World
{
    /// <summary>
    /// MarchEntity — visual representation of a single march on the world map.
    ///
    /// Lifecycle: spawned by MarchEntitySpawner when a march is created/loaded,
    /// destroyed when the march reaches COMPLETED or CANCELLED.
    ///
    /// Visual approach (Phase 3 — geometry only, no atlas):
    ///   - Base mesh: a procedural arrow/disc (switched per state)
    ///   - State colours: Outbound=gold, Gathering=green, Returning=blue
    ///   - Name label: TMP "Gathering" / "Returning" / troop count
    ///   - Hover → scale up slightly
    ///
    /// Position: updated every frame by interpolating from MarchModel.InterpolatedPosition().
    /// The march marker floats 2 Unity units above terrain (y=2).
    /// </summary>
    public class MarchEntity : MonoBehaviour
    {
        private const float HOVER_HEIGHT = 2.0f;

        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("State Colors")]
        [SerializeField] private Color _outboundColor  = new Color(1f, 0.85f, 0.1f);   // gold
        [SerializeField] private Color _gatheringColor = new Color(0.2f, 0.9f, 0.3f);  // green
        [SerializeField] private Color _returningColor = new Color(0.2f, 0.5f, 1.0f);  // blue

        [Header("Visual")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private GameObject   _selectionRing;
        [SerializeField] private TMPro.TextMeshPro _label;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private MarchModel   _model;
        private MaterialPropertyBlock _mpb;
#pragma warning disable CS0414
        private bool _hovered;
#pragma warning restore CS0414

        // -------------------------------------------------------------------------
        // Pool interface
        // -------------------------------------------------------------------------

        public void Initialize(MarchModel model)
        {
            _model = model;
            _mpb ??= new MaterialPropertyBlock();
            gameObject.SetActive(true);
            if (_selectionRing) _selectionRing.SetActive(false);
            Refresh();
        }

        public void Recycle()
        {
            _model = null;
            gameObject.SetActive(false);
        }

        public int MarchId => _model?.Id ?? -1;

        // -------------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------------

        private void Update()
        {
            if (_model == null) return;
            if (_model.IsTerminal) return;

            var now = DateTime.UtcNow;

            // Position
            var (wx, wz) = _model.InterpolatedPosition(now);
            transform.position = new Vector3(wx, HOVER_HEIGHT, wz);

            // Rotation — point arrow toward destination during outbound/returning
            if (_model.State == MarchState.Outbound)
                FaceTarget(_model.DestX, _model.DestY);
            else if (_model.State == MarchState.Returning)
                FaceTarget(_model.OriginX, _model.OriginY);

            // Refresh colour/label every 5 frames (cheap)
            if (Time.frameCount % 5 == 0)
                Refresh();
        }

        // -------------------------------------------------------------------------
        // Visuals
        // -------------------------------------------------------------------------

        private void Refresh()
        {
            if (_model == null) return;

            Color c = _model.State switch
            {
                MarchState.Outbound  => _outboundColor,
                MarchState.Gathering => _gatheringColor,
                MarchState.Returning => _returningColor,
                _                    => Color.white,
            };

            if (_meshRenderer)
            {
                _meshRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", c);
                _meshRenderer.SetPropertyBlock(_mpb);
            }

            if (_label)
            {
                var now = DateTime.UtcNow;
                double secs = Math.Max(0, _model.SecondsUntilNextTransition(now));
                string eta = FormatSeconds(secs);

                _label.text = _model.State switch
                {
                    MarchState.Outbound  => $"→ {eta}",
                    MarchState.Gathering => $"⛏ {eta}",
                    MarchState.Returning => $"← {eta}",
                    _                    => "",
                };
            }
        }

        public void OnHoverEnter()
        {
            _hovered = true;
            transform.localScale = Vector3.one * 1.3f;
        }

        public void OnHoverExit()
        {
            _hovered = false;
            transform.localScale = Vector3.one;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void FaceTarget(int tileX, int tileY)
        {
            const float TILE = 5f, OFFSET = 5120f;
            float tx = tileX * TILE - OFFSET;
            float tz = tileY * TILE - OFFSET;
            var dir = new Vector3(tx - transform.position.x, 0, tz - transform.position.z);
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        private static string FormatSeconds(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds:D2}s";
            return $"{(int)seconds}s";
        }
    }
}
