using UnityEngine;
using System.Collections.Generic;

namespace EternalKingdoms.World.Grid
{
    /// <summary>
    /// Spatial index for fast "which entities are near position X?" queries.
    ///
    /// Uses a grid-cell bucketing approach: the world is divided into
    /// cells of size CELL_SIZE tiles. Each entity is registered in its cell.
    ///
    /// Supports:
    /// - Radius queries (get all entities within N tiles of a point)
    /// - Nearest-entity lookup
    /// - Type-filtered queries (kingdoms only, monsters only, etc.)
    /// </summary>
    public class SpatialIndex
    {
        private const int CELL_SIZE = 32; // tiles per spatial cell

        private readonly Dictionary<Vector2Int, List<SpatialEntry>> _cells = new();
        private readonly Dictionary<string, SpatialEntry> _byId = new();

        public int Count => _byId.Count;

        // ── Registration ──────────────────────────────────────────────────────

        public void Insert(string id, WorldCoordinate coord, TileOccupancy type, object userData = null)
        {
            Remove(id); // ensure no duplicates

            var entry = new SpatialEntry { Id = id, Coord = coord, Type = type, UserData = userData };
            _byId[id] = entry;

            var cell = GetCell(coord);
            if (!_cells.TryGetValue(cell, out var list))
                _cells[cell] = list = new List<SpatialEntry>();
            list.Add(entry);
        }

        public void Remove(string id)
        {
            if (!_byId.TryGetValue(id, out var entry)) return;
            _byId.Remove(id);

            var cell = GetCell(entry.Coord);
            if (_cells.TryGetValue(cell, out var list))
                list.Remove(entry);
        }

        public void Clear()
        {
            _cells.Clear();
            _byId.Clear();
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>Returns all entries within radius tiles of center.</summary>
        public List<SpatialEntry> QueryRadius(WorldCoordinate center, int radiusTiles)
        {
            var result = new List<SpatialEntry>();
            int cellRadius = Mathf.CeilToInt((float)radiusTiles / CELL_SIZE) + 1;
            var centerCell = GetCell(center);

            for (int dz = -cellRadius; dz <= cellRadius; dz++)
            {
                for (int dx = -cellRadius; dx <= cellRadius; dx++)
                {
                    var c = new Vector2Int(centerCell.x + dx, centerCell.y + dz);
                    if (!_cells.TryGetValue(c, out var list)) continue;
                    foreach (var e in list)
                        if (e.Coord.DistanceTo(center) <= radiusTiles)
                            result.Add(e);
                }
            }
            return result;
        }

        /// <summary>Returns the nearest entry of a given type to center.</summary>
        public bool TryGetNearest(WorldCoordinate center, TileOccupancy type, out SpatialEntry nearest, int searchRadiusTiles = 512)
        {
            nearest = default;
            float bestDist = float.MaxValue;
            bool found = false;

            foreach (var entry in QueryRadius(center, searchRadiusTiles))
            {
                if (entry.Type != type) continue;
                float d = entry.Coord.DistanceTo(center);
                if (d < bestDist) { bestDist = d; nearest = entry; found = true; }
            }
            return found;
        }

        public bool TryGet(string id, out SpatialEntry entry) => _byId.TryGetValue(id, out entry);

        // ── Internal ──────────────────────────────────────────────────────────

        private static Vector2Int GetCell(WorldCoordinate coord) =>
            new(coord.TX / CELL_SIZE, coord.TZ / CELL_SIZE);

        public struct SpatialEntry
        {
            public string Id;
            public WorldCoordinate Coord;
            public TileOccupancy Type;
            public object UserData;
        }
    }
}
