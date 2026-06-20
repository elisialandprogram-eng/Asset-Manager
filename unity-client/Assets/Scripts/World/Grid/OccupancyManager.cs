using UnityEngine;
using System.Collections.Generic;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.World.Grid
{
    public enum TileOccupancy
    {
        Unoccupied = 0,
        Kingdom    = 1,
        Monster    = 2,
        Crystal    = 3,
        Shrine     = 4,
        Alliance   = 5,
    }

    /// <summary>
    /// Tracks which entities occupy which tiles in the world grid.
    ///
    /// Used by:
    /// - WorldEntitySpawner — register/deregister entities
    /// - SpatialIndex — fast "what's at tile X,Z?" lookups
    /// - Fog of War — determine discovered vs undiscovered tiles
    ///
    /// Kingdom footprint: 2×2 tiles (center tile + 3 surrounding).
    /// Monster / Crystal footprint: 1×1 tile.
    /// Alliance Fortress: 4×4 tiles.
    /// </summary>
    public class OccupancyManager
    {
        // Maps each occupied tile → (entityId, occupancy type)
        private readonly Dictionary<Vector2Int, (string entityId, TileOccupancy type)> _tiles = new();

        // Maps entityId → list of tiles it occupies (for fast removal)
        private readonly Dictionary<string, List<Vector2Int>> _entityTiles = new();

        // ── Registration ──────────────────────────────────────────────────────

        public void Register(string entityId, WorldCoordinate center, TileOccupancy type, int footprint = 1)
        {
            var tiles = GetFootprintTiles(center, footprint);
            _entityTiles[entityId] = tiles;
            foreach (var t in tiles)
                _tiles[t] = (entityId, type);
        }

        public void Unregister(string entityId)
        {
            if (!_entityTiles.TryGetValue(entityId, out var tiles)) return;
            foreach (var t in tiles)
                _tiles.Remove(t);
            _entityTiles.Remove(entityId);
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public bool IsOccupied(WorldCoordinate coord) =>
            _tiles.ContainsKey(new Vector2Int(coord.TX, coord.TZ));

        public bool TryGetOccupant(WorldCoordinate coord, out string entityId, out TileOccupancy type)
        {
            if (_tiles.TryGetValue(new Vector2Int(coord.TX, coord.TZ), out var entry))
            {
                entityId = entry.entityId;
                type = entry.type;
                return true;
            }
            entityId = null;
            type = TileOccupancy.Unoccupied;
            return false;
        }

        public TileOccupancy GetOccupancy(WorldCoordinate coord) =>
            _tiles.TryGetValue(new Vector2Int(coord.TX, coord.TZ), out var e)
                ? e.type : TileOccupancy.Unoccupied;

        // ── Bulk population from API ──────────────────────────────────────────

        public void PopulateFromWorldMap(WorldMapResponseDto map)
        {
            _tiles.Clear();
            _entityTiles.Clear();

            if (map.kingdoms != null)
                foreach (var k in map.kingdoms)
                    Register(k.id, WorldCoordinate.FromBackend(k.mapX, k.mapY), TileOccupancy.Kingdom, footprint: 2);

            if (map.spawns != null)
                foreach (var s in map.spawns)
                    Register(s.id, WorldCoordinate.FromBackend(s.x, s.y), TileOccupancy.Monster, footprint: 1);

            if (map.crystalNodes != null)
                foreach (var c in map.crystalNodes)
                    Register(c.id, WorldCoordinate.FromBackend(c.x, c.y), TileOccupancy.Crystal, footprint: 1);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private static List<Vector2Int> GetFootprintTiles(WorldCoordinate center, int footprint)
        {
            var result = new List<Vector2Int>(footprint * footprint);
            for (int dz = 0; dz < footprint; dz++)
                for (int dx = 0; dx < footprint; dx++)
                    result.Add(new Vector2Int(center.TX + dx, center.TZ + dz));
            return result;
        }
    }
}
