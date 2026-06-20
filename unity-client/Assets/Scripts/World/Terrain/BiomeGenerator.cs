using UnityEngine;
using EternalKingdoms.World.Grid;
using EternalKingdoms.Utilities;

namespace EternalKingdoms.World.Terrain
{
    public enum BiomeType
    {
        Grasslands = 0,
        Forest     = 1,
        Highlands  = 2,
        Snow       = 3,
        Desert     = 4,
        Swamp      = 5,
        Volcanic   = 6,
    }

    /// <summary>
    /// Determines the biome for any tile based on:
    ///   1. fBm elevation noise (primary — from NoiseUtils.cs / terrainGenerator.ts)
    ///   2. Zone distance from world center (higher zones have harsher biomes)
    ///   3. fBm moisture noise (secondary — forest vs. grasslands vs. desert blend)
    ///
    /// Elevation thresholds from UNITY_WORLD_SYSTEM.md:
    ///   0.00–0.18 Plains (light)
    ///   0.18–0.32 Plains/Forest
    ///   0.32–0.50 Deep Forest/Hills
    ///   0.50–0.65 Highland/Rock
    ///   0.65–0.80 Mountains
    ///   0.80–0.91 High Peaks
    ///   0.91–1.00 Snow Caps
    ///   crystalNoise > 0.83 → Crystal Zone (mid-elevation only)
    ///
    /// Biome visual mapping:
    ///   Plains (light)      → Grasslands
    ///   Plains/Forest       → Forest or Swamp (moisture)
    ///   Deep Forest/Hills   → Forest or Highlands
    ///   Highland/Rock       → Highlands
    ///   Mountains           → Highlands / Snow (zone-dependent)
    ///   High Peaks/Snow     → Snow
    ///   Volcanic zone       → overrides near zone 0 Sanctum
    ///   Desert              → low elevation + low moisture + outer zones
    /// </summary>
    public static class BiomeGenerator
    {
        // Second noise layer (moisture) — offset by large prime to decorrelate
        private const float MOISTURE_SEED_OFFSET = 7919f;

        // Crystal noise — separate layer
        private const float CRYSTAL_THRESHOLD = 0.83f;
        private const float CRYSTAL_MIN_ELEV  = 0.20f;
        private const float CRYSTAL_MAX_ELEV  = 0.65f;

        public static BiomeType GetBiome(int tx, int tz, int worldSeed)
        {
            float elev     = NoiseUtils.SampleHeight(tx, tz, worldSeed);
            float moisture = NoiseUtils.SampleHeight(tx, tz, worldSeed + (int)MOISTURE_SEED_OFFSET);
            float crystal  = NoiseUtils.SampleHeight(tx + 1000, tz + 1000, worldSeed + 31337);

            int zone = WorldGrid.GetZone(new WorldCoordinate(tx, tz));

            // Crystal zone check (overrides terrain visual but not biome)
            // Zone 0–2 volcanic override
            if (zone == 0 && elev > 0.40f)
                return BiomeType.Volcanic;

            // Snow caps
            if (elev >= 0.80f)
                return BiomeType.Snow;

            // High peaks / mountains
            if (elev >= 0.65f)
                return zone >= 4 ? BiomeType.Snow : BiomeType.Highlands;

            // Highlands
            if (elev >= 0.50f)
                return BiomeType.Highlands;

            // Forest / Swamp zone
            if (elev >= 0.32f)
            {
                if (moisture > 0.65f && zone >= 3) return BiomeType.Swamp;
                return BiomeType.Forest;
            }

            // Low-elevation — plains / desert / forest
            if (elev >= 0.18f)
            {
                if (moisture < 0.25f && zone >= 4) return BiomeType.Desert;
                if (moisture > 0.60f)              return BiomeType.Forest;
                return BiomeType.Grasslands;
            }

            // Lowest elevation
            if (moisture < 0.30f && zone >= 3) return BiomeType.Desert;
            return BiomeType.Grasslands;
        }

        public static bool IsCrystalZone(int tx, int tz, int worldSeed)
        {
            float elev    = NoiseUtils.SampleHeight(tx, tz, worldSeed);
            float crystal = NoiseUtils.SampleHeight(tx + 1000, tz + 1000, worldSeed + 31337);
            return crystal > CRYSTAL_THRESHOLD && elev >= CRYSTAL_MIN_ELEV && elev <= CRYSTAL_MAX_ELEV;
        }

        // ── Biome → visual properties ─────────────────────────────────────────

        public static Color GetBiomeColor(BiomeType biome) => biome switch
        {
            BiomeType.Grasslands => new Color(0.44f, 0.65f, 0.28f),   // medium green
            BiomeType.Forest     => new Color(0.18f, 0.42f, 0.18f),   // dark green
            BiomeType.Highlands  => new Color(0.58f, 0.52f, 0.40f),   // dusty khaki
            BiomeType.Snow       => new Color(0.90f, 0.93f, 0.97f),   // near-white
            BiomeType.Desert     => new Color(0.82f, 0.72f, 0.42f),   // warm sand
            BiomeType.Swamp      => new Color(0.28f, 0.38f, 0.22f),   // murky green
            BiomeType.Volcanic   => new Color(0.25f, 0.10f, 0.08f),   // dark red-black
            _                    => Color.grey,
        };

        public static string GetBiomeName(BiomeType biome) => biome switch
        {
            BiomeType.Grasslands => "Grasslands",
            BiomeType.Forest     => "Ancient Forest",
            BiomeType.Highlands  => "Highland Reaches",
            BiomeType.Snow       => "Frozen Peaks",
            BiomeType.Desert     => "Scorched Wastes",
            BiomeType.Swamp      => "Murk Swamp",
            BiomeType.Volcanic   => "Volcanic Sanctum",
            _                    => "Unknown",
        };

        public static float GetHeightOffset(BiomeType biome) => biome switch
        {
            BiomeType.Snow      => 0.8f,
            BiomeType.Highlands => 0.5f,
            BiomeType.Volcanic  => 0.3f,
            BiomeType.Swamp     => -0.1f,
            _                   => 0f,
        };
    }
}
