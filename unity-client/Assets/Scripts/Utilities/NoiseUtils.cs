using UnityEngine;

namespace EternalKingdoms.Utilities
{
    /// <summary>
    /// Fractal Brownian Motion (fBm) terrain noise — mirrors lib/game-engine/terrainGenerator.ts.
    /// Uses the same algorithm so Unity-side terrain matches server-side biome classification.
    ///
    /// Input: tile coordinates (tx, tz) + world seed
    /// Output: normalized height value [0, 1]
    ///
    /// Biome thresholds (match WORLD_ARCHITECTURE_BIBLE.md):
    ///   Ocean   < 0.25
    ///   Plains  0.25 – 0.45
    ///   Forest  0.45 – 0.60
    ///   Hills   0.60 – 0.75
    ///   Mountains 0.75 – 0.88
    ///   Peaks   >= 0.88
    /// </summary>
    public static class NoiseUtils
    {
        // fBm parameters — must match terrainGenerator.ts
        private const int OCTAVES = 6;
        private const float LACUNARITY = 2.0f;
        private const float PERSISTENCE = 0.5f;
        private const float SCALE = 0.004f;

        /// <summary>
        /// Returns a normalized height [0,1] for tile (tx, tz) in a world with the given seed.
        /// </summary>
        public static float SampleHeight(int tx, int tz, int worldSeed)
        {
            float nx = tx * SCALE;
            float nz = tz * SCALE;
            return FBm(nx, nz, worldSeed);
        }

        /// <summary>
        /// Returns the biome type for a given height value.
        /// </summary>
        public static BiomeType HeightToBiome(float height)
        {
            if (height < 0.25f) return BiomeType.Ocean;
            if (height < 0.45f) return BiomeType.Plains;
            if (height < 0.60f) return BiomeType.Forest;
            if (height < 0.75f) return BiomeType.Hills;
            if (height < 0.88f) return BiomeType.Mountains;
            return BiomeType.Peaks;
        }

        /// <summary>
        /// Returns the march speed terrain modifier for a biome (see WORLD_ARCHITECTURE_BIBLE.md §2).
        /// </summary>
        public static float BiomeSpeedModifier(BiomeType biome) => biome switch
        {
            BiomeType.Plains => 1.0f,
            BiomeType.Forest => 0.85f,
            BiomeType.Hills => 0.80f,
            BiomeType.Mountains => 0.60f,
            BiomeType.Ocean => 0f,
            BiomeType.Peaks => 0.40f,
            _ => 1.0f
        };

        // ── fBm implementation ────────────────────────────────────────────────

        private static float FBm(float x, float z, int seed)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < OCTAVES; i++)
            {
                value += ValueNoise(x * frequency + seed * 0.1f, z * frequency + seed * 0.1f) * amplitude;
                maxValue += amplitude;
                amplitude *= PERSISTENCE;
                frequency *= LACUNARITY;
            }

            return value / maxValue;
        }

        /// <summary>
        /// Simple deterministic value noise using integer grid interpolation.
        /// Matches the TypeScript implementation in terrainGenerator.ts.
        /// </summary>
        private static float ValueNoise(float x, float z)
        {
            int xi = Mathf.FloorToInt(x);
            int zi = Mathf.FloorToInt(z);
            float tx = x - xi;
            float tz = z - zi;

            // Smoothstep
            float ux = tx * tx * (3f - 2f * tx);
            float uz = tz * tz * (3f - 2f * tz);

            float v00 = Hash(xi, zi);
            float v10 = Hash(xi + 1, zi);
            float v01 = Hash(xi, zi + 1);
            float v11 = Hash(xi + 1, zi + 1);

            return Mathf.Lerp(
                Mathf.Lerp(v00, v10, ux),
                Mathf.Lerp(v01, v11, ux),
                uz);
        }

        private static float Hash(int x, int z)
        {
            int n = x + z * 57;
            n = (n << 13) ^ n;
            return (1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f) * 0.5f + 0.5f;
        }
    }

    public enum BiomeType
    {
        Ocean,
        Plains,
        Forest,
        Hills,
        Mountains,
        Peaks
    }
}
