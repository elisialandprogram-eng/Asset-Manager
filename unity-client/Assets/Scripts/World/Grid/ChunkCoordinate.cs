using UnityEngine;
using System;

namespace EternalKingdoms.World.Grid
{
    /// <summary>
    /// Immutable chunk coordinate (cx, cz) within the 32×32 chunk grid.
    ///
    /// Each chunk covers 64×64 tiles = 320×320 Unity units.
    /// Total chunks: 32×32 = 1,024.
    /// </summary>
    [Serializable]
    public readonly struct ChunkCoordinate : IEquatable<ChunkCoordinate>
    {
        public const int CHUNK_SIZE   = 64;     // tiles per chunk side
        public const int CHUNKS_TOTAL = 32;     // chunks per world side (2048/64)
        public const float CHUNK_WORLD_SIZE = CHUNK_SIZE * WorldCoordinate.TILE_SIZE; // 320 units

        public readonly int CX;  // chunk X [0, 31]
        public readonly int CZ;  // chunk Z [0, 31]

        public ChunkCoordinate(int cx, int cz)
        {
            CX = Mathf.Clamp(cx, 0, CHUNKS_TOTAL - 1);
            CZ = Mathf.Clamp(cz, 0, CHUNKS_TOTAL - 1);
        }

        // ── Factory ───────────────────────────────────────────────────────────

        public static ChunkCoordinate FromUnity(Vector3 worldPos)
        {
            var tile = WorldCoordinate.FromUnity(worldPos);
            return tile.ToChunk();
        }

        // ── Conversion ────────────────────────────────────────────────────────

        /// <summary>Returns the tile coordinate of this chunk's NW origin tile.</summary>
        public WorldCoordinate OriginTile => new(CX * CHUNK_SIZE, CZ * CHUNK_SIZE);

        /// <summary>Returns the Unity world position of this chunk's center.</summary>
        public Vector3 WorldCenter()
        {
            float x = (CX * CHUNK_SIZE + CHUNK_SIZE * 0.5f) * WorldCoordinate.TILE_SIZE - WorldCoordinate.WORLD_HALF;
            float z = (CZ * CHUNK_SIZE + CHUNK_SIZE * 0.5f) * WorldCoordinate.TILE_SIZE - WorldCoordinate.WORLD_HALF;
            return new Vector3(x, 0f, z);
        }

        /// <summary>Returns the Unity world position of this chunk's NW corner origin.</summary>
        public Vector3 WorldOrigin()
        {
            float x = CX * CHUNK_WORLD_SIZE - WorldCoordinate.WORLD_HALF;
            float z = CZ * CHUNK_WORLD_SIZE - WorldCoordinate.WORLD_HALF;
            return new Vector3(x, 0f, z);
        }

        // ── Validation ────────────────────────────────────────────────────────

        public bool IsValid => CX >= 0 && CX < CHUNKS_TOTAL && CZ >= 0 && CZ < CHUNKS_TOTAL;

        // ── Neighbourhood ─────────────────────────────────────────────────────

        public int ManhattanDistanceTo(ChunkCoordinate other) =>
            Mathf.Abs(other.CX - CX) + Mathf.Abs(other.CZ - CZ);

        public float EuclideanDistanceTo(ChunkCoordinate other)
        {
            float dx = other.CX - CX;
            float dz = other.CZ - CZ;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ── Operators ─────────────────────────────────────────────────────────

        public static bool operator ==(ChunkCoordinate a, ChunkCoordinate b) =>
            a.CX == b.CX && a.CZ == b.CZ;
        public static bool operator !=(ChunkCoordinate a, ChunkCoordinate b) => !(a == b);

        public bool Equals(ChunkCoordinate other) => CX == other.CX && CZ == other.CZ;
        public override bool Equals(object obj) => obj is ChunkCoordinate c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(CX, CZ);
        public override string ToString() => $"Chunk({CX}, {CZ})";

        public Vector2Int ToVector2Int() => new(CX, CZ);
    }
}
