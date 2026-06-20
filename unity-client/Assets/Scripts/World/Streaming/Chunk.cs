using UnityEngine;
using EternalKingdoms.World.Grid;
using EternalKingdoms.World.Terrain;

namespace EternalKingdoms.World.Streaming
{
    public enum ChunkState
    {
        Unloaded,
        Loading,
        Active,
        Unloading
    }

    /// <summary>
    /// Runtime representation of a single 64×64-tile world chunk.
    ///
    /// Each chunk owns:
    ///   - TerrainChunk component (mesh + material)
    ///   - Entity references that fall within its tile bounds
    ///   - Fog-of-war visibility state per tile (Phase U2.10)
    ///
    /// Chunks are managed by ChunkManager and pooled to avoid
    /// Instantiate/Destroy spam (requirement: U2.3).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────────
        public ChunkCoordinate Coord { get; private set; }
        public ChunkState State { get; private set; } = ChunkState.Unloaded;

        private TerrainChunk _terrain;

        // ── Initialization ────────────────────────────────────────────────────

        /// <summary>Called by ChunkManager when this chunk is pulled from the pool.</summary>
        public void Initialize(ChunkCoordinate coord, int worldSeed)
        {
            Coord = coord;
            State = ChunkState.Loading;

            name = $"Chunk_{coord.CX}_{coord.CZ}";

            // Position this GameObject at the chunk's NW world-space corner
            transform.position = coord.WorldOrigin();

            // Build terrain mesh
            if (_terrain == null) _terrain = GetComponent<TerrainChunk>() ?? gameObject.AddComponent<TerrainChunk>();
            _terrain.Generate(coord, worldSeed);

            State = ChunkState.Active;
            gameObject.SetActive(true);
        }

        /// <summary>Called by ChunkManager when returning this chunk to the pool.</summary>
        public void Recycle()
        {
            State = ChunkState.Unloading;
            gameObject.SetActive(false);
            Coord = default;
            State = ChunkState.Unloaded;
        }

        public bool ContainsTile(WorldCoordinate tile)
        {
            return tile.TX >= Coord.CX * ChunkCoordinate.CHUNK_SIZE &&
                   tile.TX < (Coord.CX + 1) * ChunkCoordinate.CHUNK_SIZE &&
                   tile.TZ >= Coord.CZ * ChunkCoordinate.CHUNK_SIZE &&
                   tile.TZ < (Coord.CZ + 1) * ChunkCoordinate.CHUNK_SIZE;
        }
    }
}
