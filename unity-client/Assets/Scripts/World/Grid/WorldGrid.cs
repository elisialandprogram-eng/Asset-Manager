using UnityEngine;
using System.Collections.Generic;

namespace EternalKingdoms.World.Grid
{
    /// <summary>
    /// Logical world grid — the authoritative in-memory representation
    /// of the 2048×2048 tile world state.
    ///
    /// Responsibilities:
    /// - Chunk registration / lookup
    /// - Tile occupancy tracking (via OccupancyManager)
    /// - World ↔ Unity ↔ backend coordinate conversions
    /// - Zone classification (§3 WORLD_ARCHITECTURE_BIBLE.md)
    ///
    /// Does NOT own MonoBehaviours — purely data. WorldSceneController
    /// and WorldStreamingManager use this grid as their shared truth.
    /// </summary>
    public class WorldGrid
    {
        // ── Zone radii from world center (tile 1024, 1024) ────────────────────
        //   Source: WORLD_ARCHITECTURE_BIBLE.md §3
        private static readonly int[] ZONE_RADII = { 64, 192, 384, 576, 768, 960, 1152, 2048 };
        private static readonly string[] ZONE_NAMES =
        {
            "Sanctum", "Inner Reaches", "Mid Reaches", "Border Reaches",
            "Outlands", "Far Outlands", "Frontier", "Wilderness"
        };

        private static readonly WorldCoordinate _worldCenter = new(1024, 1024);

        // ── Chunk registry ────────────────────────────────────────────────────
        private readonly Dictionary<Vector2Int, bool> _loadedChunks = new();

        public void RegisterChunkLoaded(ChunkCoordinate coord) =>
            _loadedChunks[coord.ToVector2Int()] = true;

        public void RegisterChunkUnloaded(ChunkCoordinate coord) =>
            _loadedChunks.Remove(coord.ToVector2Int());

        public bool IsChunkLoaded(ChunkCoordinate coord) =>
            _loadedChunks.ContainsKey(coord.ToVector2Int());

        public int LoadedChunkCount => _loadedChunks.Count;

        // ── Zone queries ──────────────────────────────────────────────────────

        /// <summary>Returns the zone index (0–7) for a tile coordinate.</summary>
        public static int GetZone(WorldCoordinate coord)
        {
            float dist = coord.DistanceTo(_worldCenter);
            for (int i = 0; i < ZONE_RADII.Length; i++)
                if (dist <= ZONE_RADII[i]) return i;
            return ZONE_RADII.Length - 1;
        }

        public static string GetZoneName(WorldCoordinate coord) =>
            ZONE_NAMES[GetZone(coord)];

        // ── Chunk range query ─────────────────────────────────────────────────

        /// <summary>
        /// Returns all ChunkCoordinates within Chebyshev distance `radius` of `center`.
        /// Chebyshev distance = max(|dx|, |dz|) — efficient square neighbourhood.
        /// </summary>
        public static List<ChunkCoordinate> GetChunksInRadius(ChunkCoordinate center, int radius)
        {
            var result = new List<ChunkCoordinate>((radius * 2 + 1) * (radius * 2 + 1));
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var c = new ChunkCoordinate(center.CX + dx, center.CZ + dz);
                    if (c.IsValid) result.Add(c);
                }
            }
            return result;
        }

        /// <summary>Returns tiles in a square radius around a center tile.</summary>
        public static List<WorldCoordinate> GetTilesInRadius(WorldCoordinate center, int radiusTiles)
        {
            var result = new List<WorldCoordinate>();
            for (int dz = -radiusTiles; dz <= radiusTiles; dz++)
                for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
                {
                    var c = new WorldCoordinate(center.TX + dx, center.TZ + dz);
                    if (c.IsValid) result.Add(c);
                }
            return result;
        }
    }
}
