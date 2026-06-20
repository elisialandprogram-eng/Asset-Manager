using UnityEngine;
using EternalKingdoms.World.Grid;
using EternalKingdoms.Utilities;

namespace EternalKingdoms.World.Terrain
{
    /// <summary>
    /// Generates terrain mesh data for a single chunk.
    ///
    /// Algorithm:
    ///   1. For each vertex in the chunk's (resolution+1)×(resolution+1) grid,
    ///      sample fBm elevation via NoiseUtils.SampleHeight().
    ///   2. Apply biome height offset from BiomeGenerator.GetHeightOffset().
    ///   3. Apply vertex color from BiomeGenerator.GetBiomeColor() for fast
    ///      vertex-colored rendering without separate texture atlases.
    ///
    /// Deterministic: same seed + same chunk coord → identical mesh every time.
    ///
    /// Resolution: 16 quads per chunk side (16×16 quads, 17×17 vertices).
    /// Higher resolution increases visual fidelity at a memory + compute cost.
    /// For Phase 2 this stays at 16 for WebGL performance targets (U2.11).
    ///
    /// Y scale: 1 Unity unit height per 1.0 elevation unit (artistic value).
    /// </summary>
    public static class TerrainGenerator
    {
        public const int CHUNK_RESOLUTION = 16; // quads per chunk side
        public const float HEIGHT_SCALE   = 6f; // Unity units per full elevation unit

        public static TerrainMeshData Generate(ChunkCoordinate coord, int worldSeed)
        {
            int vertsPerSide = CHUNK_RESOLUTION + 1;
            int vertCount    = vertsPerSide * vertsPerSide;
            int quadCount    = CHUNK_RESOLUTION * CHUNK_RESOLUTION;

            var vertices  = new Vector3[vertCount];
            var uvs       = new Vector2[vertCount];
            var colors    = new Color[vertCount];
            var triangles = new int[quadCount * 6];

            float tileStep = (float)ChunkCoordinate.CHUNK_SIZE / CHUNK_RESOLUTION;

            for (int vz = 0; vz < vertsPerSide; vz++)
            {
                for (int vx = 0; vx < vertsPerSide; vx++)
                {
                    int idx = vz * vertsPerSide + vx;

                    // Tile coordinates for this vertex
                    int tx = coord.CX * ChunkCoordinate.CHUNK_SIZE + Mathf.FloorToInt(vx * tileStep);
                    int tz = coord.CZ * ChunkCoordinate.CHUNK_SIZE + Mathf.FloorToInt(vz * tileStep);

                    tx = Mathf.Clamp(tx, 0, WorldCoordinate.WORLD_SIZE_TILES - 1);
                    tz = Mathf.Clamp(tz, 0, WorldCoordinate.WORLD_SIZE_TILES - 1);

                    float elev  = NoiseUtils.SampleHeight(tx, tz, worldSeed);
                    var   biome = BiomeGenerator.GetBiome(tx, tz, worldSeed);
                    float y     = (elev + BiomeGenerator.GetHeightOffset(biome)) * HEIGHT_SCALE;

                    // Local position within chunk (chunk positioned at WorldOrigin)
                    float localX = vx * (ChunkCoordinate.CHUNK_WORLD_SIZE / CHUNK_RESOLUTION);
                    float localZ = vz * (ChunkCoordinate.CHUNK_WORLD_SIZE / CHUNK_RESOLUTION);

                    vertices[idx] = new Vector3(localX, y, localZ);
                    uvs[idx]      = new Vector2((float)vx / CHUNK_RESOLUTION, (float)vz / CHUNK_RESOLUTION);
                    colors[idx]   = BiomeGenerator.GetBiomeColor(biome);

                    // Blend with crystal tint
                    if (BiomeGenerator.IsCrystalZone(tx, tz, worldSeed))
                        colors[idx] = Color.Lerp(colors[idx], new Color(0.7f, 0.6f, 1f), 0.25f);
                }
            }

            // Build triangles
            int t = 0;
            for (int qz = 0; qz < CHUNK_RESOLUTION; qz++)
            {
                for (int qx = 0; qx < CHUNK_RESOLUTION; qx++)
                {
                    int i = qz * vertsPerSide + qx;
                    triangles[t++] = i;
                    triangles[t++] = i + vertsPerSide;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + vertsPerSide;
                    triangles[t++] = i + vertsPerSide + 1;
                }
            }

            return new TerrainMeshData
            {
                Vertices  = vertices,
                UVs       = uvs,
                Colors    = colors,
                Triangles = triangles,
            };
        }
    }

    public class TerrainMeshData
    {
        public Vector3[] Vertices;
        public Vector2[] UVs;
        public Color[]   Colors;
        public int[]     Triangles;
    }
}
