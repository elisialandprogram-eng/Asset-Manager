using System;
using System.Collections.Generic;
using UnityEngine;

namespace EternalKingdoms.World
{
    /// <summary>
    /// MarchPathVisualizer — draws march path lines and ETA labels on the world map.
    ///
    /// One LineRenderer per active march (pooled). Lines lift 1 unit above terrain.
    ///
    /// Updated every second from MarchManager.OnMarchStateChanged.
    /// Visual spec:
    ///   - Outbound:  dashed gold line, origin→dest
    ///   - Gathering: pulsing green dot at destination
    ///   - Returning: dashed blue line, dest→origin
    ///   - Line width: 0.5 Unity units (visible at max zoom)
    ///   - TMP label above midpoint: "ETA 4m 32s"
    /// </summary>
    public class MarchPathVisualizer : MonoBehaviour
    {
        private const float PATH_HEIGHT = 1.2f;
        private const float TILE_SIZE   = 5.0f;
        private const float WORLD_OFFSET = 5120f;

        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [SerializeField] private Material _lineMaterialOutbound;
        [SerializeField] private Material _lineMaterialReturning;
        [SerializeField] private float    _lineWidth = 0.5f;
        [SerializeField] private GameObject _etaLabelPrefab;

        // -------------------------------------------------------------------------
        // Pool
        // -------------------------------------------------------------------------

        private class MarchVisual
        {
            public LineRenderer Line;
            public TMPro.TextMeshPro EtaLabel;
            public int MarchId;
        }

        private readonly Dictionary<int, MarchVisual> _visuals = new();

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        private void OnEnable()
        {
            if (MarchManager.Instance != null)
            {
                MarchManager.Instance.OnMarchCreated       += HandleMarchCreated;
                MarchManager.Instance.OnMarchStateChanged  += HandleMarchUpdated;
                MarchManager.Instance.OnMarchCompleted     += HandleMarchRemoved;
                MarchManager.Instance.OnMarchCancelled     += HandleMarchRemoved;
                MarchManager.Instance.OnMarchListRefreshed += HandleListRefresh;
            }
        }

        private void OnDisable()
        {
            if (MarchManager.Instance != null)
            {
                MarchManager.Instance.OnMarchCreated       -= HandleMarchCreated;
                MarchManager.Instance.OnMarchStateChanged  -= HandleMarchUpdated;
                MarchManager.Instance.OnMarchCompleted     -= HandleMarchRemoved;
                MarchManager.Instance.OnMarchCancelled     -= HandleMarchRemoved;
                MarchManager.Instance.OnMarchListRefreshed -= HandleListRefresh;
            }
        }

        // -------------------------------------------------------------------------
        // Event handlers
        // -------------------------------------------------------------------------

        private void HandleMarchCreated(MarchModel m) => EnsureVisual(m);
        private void HandleMarchUpdated(MarchModel m) => UpdateVisual(m);
        private void HandleMarchRemoved(int id)       => RemoveVisual(id);

        private void HandleListRefresh(List<MarchModel> all)
        {
            foreach (var m in all)
                EnsureVisual(m);
        }

        // -------------------------------------------------------------------------
        // Per-frame label update
        // -------------------------------------------------------------------------

        private void Update()
        {
            var now = DateTime.UtcNow;
            foreach (var (marchId, vis) in _visuals)
            {
                var model = MarchManager.Instance?.GetMarch(marchId);
                if (model == null) continue;

                double secs = Math.Max(0, model.SecondsUntilNextTransition(now));
                if (vis.EtaLabel)
                    vis.EtaLabel.text = $"ETA {FormatSeconds(secs)}";
            }
        }

        // -------------------------------------------------------------------------
        // Visual management
        // -------------------------------------------------------------------------

        private void EnsureVisual(MarchModel m)
        {
            if (_visuals.ContainsKey(m.Id)) return;
            var vis = CreateVisual(m.Id);
            _visuals[m.Id] = vis;
            UpdateVisual(m, vis);
        }

        private void UpdateVisual(MarchModel m)
        {
            if (!_visuals.TryGetValue(m.Id, out var vis)) return;
            UpdateVisual(m, vis);
        }

        private void UpdateVisual(MarchModel m, MarchVisual vis)
        {
            var origin = TileToWorld(m.OriginX, m.OriginY);
            var dest   = TileToWorld(m.DestX,   m.DestY);

            bool isReturn = m.State == MarchState.Returning;
            vis.Line.material = isReturn ? _lineMaterialReturning : _lineMaterialOutbound;
            vis.Line.SetPosition(0, isReturn ? dest   : origin);
            vis.Line.SetPosition(1, isReturn ? origin : dest);

            // Label position: midpoint
            var mid = Vector3.Lerp(origin, dest, 0.5f) + Vector3.up * 0.5f;
            if (vis.EtaLabel)
                vis.EtaLabel.transform.position = mid;

            // Hide line during gathering (stationary)
            vis.Line.enabled = m.State != MarchState.Gathering;
        }

        private void RemoveVisual(int marchId)
        {
            if (!_visuals.TryGetValue(marchId, out var vis)) return;
            if (vis.Line) Destroy(vis.Line.gameObject);
            if (vis.EtaLabel) Destroy(vis.EtaLabel.gameObject);
            _visuals.Remove(marchId);
        }

        private MarchVisual CreateVisual(int marchId)
        {
            var lineGo = new GameObject($"MarchLine_{marchId}");
            lineGo.transform.SetParent(transform);
            var lr = lineGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = _lineWidth;
            lr.endWidth   = _lineWidth;
            lr.useWorldSpace = true;
            if (_lineMaterialOutbound) lr.material = _lineMaterialOutbound;

            TMPro.TextMeshPro label = null;
            if (_etaLabelPrefab)
            {
                var labelGo = Instantiate(_etaLabelPrefab, transform);
                label = labelGo.GetComponent<TMPro.TextMeshPro>();
            }

            return new MarchVisual { Line = lr, EtaLabel = label, MarchId = marchId };
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static Vector3 TileToWorld(int tileX, int tileY) =>
            new(tileX * TILE_SIZE - WORLD_OFFSET, PATH_HEIGHT, tileY * TILE_SIZE - WORLD_OFFSET);

        private static string FormatSeconds(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)   return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds:D2}s";
            return $"{(int)seconds}s";
        }
    }
}
