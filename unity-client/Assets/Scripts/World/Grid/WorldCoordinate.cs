using UnityEngine;
using System;

namespace EternalKingdoms.World.Grid
{
    /// <summary>
    /// Immutable tile-space coordinate (tx, tz) within the 2048×2048 world grid.
    ///
    /// Coordinate origin:  (0,0) = NW corner
    /// Coordinate maximum: (2047, 2047) = SE corner
    ///
    /// Conversion constants (WORLD_ARCHITECTURE_BIBLE.md §1):
    ///   TILE_SIZE = 5.0 Unity units per tile
    ///   World extent = 10,240 × 10,240 Unity units, centered at (0,0,0)
    ///   Tile (tx, tz) → Unity (tx×5 − 5120, 0, tz×5 − 5120)
    /// </summary>
    [Serializable]
    public readonly struct WorldCoordinate : IEquatable<WorldCoordinate>
    {
        // ── Constants ─────────────────────────────────────────────────────────
        public const int WORLD_SIZE_TILES = 2048;
        public const float TILE_SIZE = 5f;
        public const float WORLD_EXTENT = WORLD_SIZE_TILES * TILE_SIZE;       // 10240
        public const float WORLD_HALF   = WORLD_EXTENT * 0.5f;                // 5120
        public const float BACKEND_MAX  = 10000f;
        public const float BACKEND_TO_TILE_SCALE = WORLD_SIZE_TILES / BACKEND_MAX; // ≈ 0.2048

        // ── Fields ────────────────────────────────────────────────────────────
        public readonly int TX;  // tile X [0, 2047]
        public readonly int TZ;  // tile Z [0, 2047]

        public WorldCoordinate(int tx, int tz)
        {
            TX = Mathf.Clamp(tx, 0, WORLD_SIZE_TILES - 1);
            TZ = Mathf.Clamp(tz, 0, WORLD_SIZE_TILES - 1);
        }

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>Backend integer coords (0–10000) → tile coord.</summary>
        public static WorldCoordinate FromBackend(int bx, int bz)
        {
            int tx = Mathf.FloorToInt(bx * BACKEND_TO_TILE_SCALE);
            int tz = Mathf.FloorToInt(bz * BACKEND_TO_TILE_SCALE);
            return new WorldCoordinate(tx, tz);
        }

        /// <summary>Unity world position → nearest tile coord.</summary>
        public static WorldCoordinate FromUnity(Vector3 worldPos)
        {
            int tx = Mathf.FloorToInt((worldPos.x + WORLD_HALF) / TILE_SIZE);
            int tz = Mathf.FloorToInt((worldPos.z + WORLD_HALF) / TILE_SIZE);
            return new WorldCoordinate(tx, tz);
        }

        // ── Conversion ────────────────────────────────────────────────────────

        /// <summary>Tile center in Unity world space (Y=0).</summary>
        public Vector3 ToUnityCenter() =>
            new(TX * TILE_SIZE - WORLD_HALF + TILE_SIZE * 0.5f, 0f,
                TZ * TILE_SIZE - WORLD_HALF + TILE_SIZE * 0.5f);

        /// <summary>Tile origin (NW corner) in Unity world space.</summary>
        public Vector3 ToUnityOrigin() =>
            new(TX * TILE_SIZE - WORLD_HALF, 0f, TZ * TILE_SIZE - WORLD_HALF);

        public ChunkCoordinate ToChunk() =>
            new(TX / ChunkCoordinate.CHUNK_SIZE, TZ / ChunkCoordinate.CHUNK_SIZE);

        // ── Validation ────────────────────────────────────────────────────────

        public bool IsValid => TX >= 0 && TX < WORLD_SIZE_TILES && TZ >= 0 && TZ < WORLD_SIZE_TILES;

        public static bool IsValidBackend(int bx, int bz) =>
            bx >= 0 && bx <= 10000 && bz >= 0 && bz <= 10000;

        // ── Maths ─────────────────────────────────────────────────────────────

        public float DistanceTo(WorldCoordinate other)
        {
            float dx = other.TX - TX;
            float dz = other.TZ - TZ;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public int ManhattanDistanceTo(WorldCoordinate other) =>
            Mathf.Abs(other.TX - TX) + Mathf.Abs(other.TZ - TZ);

        public static WorldCoordinate WorldCenter => new(1024, 1024);

        // ── Operators ─────────────────────────────────────────────────────────

        public static WorldCoordinate operator +(WorldCoordinate a, WorldCoordinate b) =>
            new(a.TX + b.TX, a.TZ + b.TZ);
        public static WorldCoordinate operator -(WorldCoordinate a, WorldCoordinate b) =>
            new(a.TX - b.TX, a.TZ - b.TZ);
        public static bool operator ==(WorldCoordinate a, WorldCoordinate b) =>
            a.TX == b.TX && a.TZ == b.TZ;
        public static bool operator !=(WorldCoordinate a, WorldCoordinate b) => !(a == b);

        public bool Equals(WorldCoordinate other) => TX == other.TX && TZ == other.TZ;
        public override bool Equals(object obj) => obj is WorldCoordinate c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(TX, TZ);
        public override string ToString() => $"({TX}, {TZ})";
    }
}
