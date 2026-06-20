using System;
using System.Collections.Generic;
using EternalKingdoms.World.DTOs;

namespace EternalKingdoms.World
{
    /// <summary>
    /// TravelCalculator — pure, no-MonoBehaviour travel math.
    ///
    /// Mirrors marchCalculator.ts exactly (COMBAT_ENGINE_BIBLE.md §2).
    /// Used by:
    ///   - ResourceGatherPanel: preview ETA before creating march
    ///   - MarchManager:        validate server ETA against local compute
    /// </summary>
    public static class TravelCalculator
    {
        // -------------------------------------------------------------------------
        // Constants — must match marchCalculator.ts
        // -------------------------------------------------------------------------

        public const float BASE_MARCH_SPEED_TPM = 2.0f;

        private static readonly Dictionary<string, float> TroopSpeedModifier = new()
        {
            { "militia",      1.0f },
            { "spearman",     1.0f },
            { "archer",       0.9f },
            { "scout",        1.6f },
            { "knight",       1.6f },
            { "catapult",     0.5f },
            { "dragon_rider", 1.4f },
        };

        private static readonly Dictionary<int, int> GatherDurationSeconds = new()
        {
            { 1,  300 },
            { 2,  480 },
            { 3,  720 },
            { 4,  960 },
            { 5,  1200 },
            { 6,  1500 },
            { 7,  1800 },
            { 8,  2400 },
            { 9,  3000 },
            { 10, 3600 },
        };

        private static readonly Dictionary<string, int> TroopCarryCapacity = new()
        {
            { "militia",      20 },
            { "spearman",     20 },
            { "archer",       25 },
            { "scout",        35 },
            { "knight",       40 },
            { "catapult",     60 },
            { "dragon_rider", 80 },
        };

        // -------------------------------------------------------------------------
        // Speed
        // -------------------------------------------------------------------------

        /// Weighted-average march speed in tiles/minute.
        public static float CalculateMarchSpeed(TroopLoad troops,
            float researchBonus   = 0f,
            float heroSpeedStat   = 0f,
            float allianceBonus   = 0f,
            float terrainModifier = 1.0f,
            float dragoonBonus    = 0f)
        {
            var counts = GetTroopCounts(troops);
            int total = 0;
            float weightedSum = 0f;
            foreach (var (type, count) in counts)
            {
                total += count;
                float mod = TroopSpeedModifier.GetValueOrDefault(type, 1.0f);
                weightedSum += count * mod;
            }
            if (total == 0) return BASE_MARCH_SPEED_TPM;

            float baseSpeed = BASE_MARCH_SPEED_TPM * (weightedSum / total);
            return baseSpeed
                * (1f + researchBonus)
                * (1f + heroSpeedStat / 10_000f)
                * (1f + allianceBonus)
                * (1f + dragoonBonus)
                * terrainModifier;
        }

        // -------------------------------------------------------------------------
        // Distance & travel time
        // -------------------------------------------------------------------------

        public static float CalculateDistance(int x1, int y1, int x2, int y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// One-way travel time in seconds.
        public static float CalculateTravelSeconds(float distanceTiles, float speedTpm)
        {
            if (speedTpm <= 0f) return 0f;
            return distanceTiles / speedTpm * 60f;
        }

        // -------------------------------------------------------------------------
        // Gather timing
        // -------------------------------------------------------------------------

        public static int GetGatherDurationSeconds(int nodeLevel)
        {
            if (GatherDurationSeconds.TryGetValue(nodeLevel, out var d)) return d;
            return GatherDurationSeconds[10];
        }

        // -------------------------------------------------------------------------
        // Preview — for UI display before creating march
        // -------------------------------------------------------------------------

        public class MarchPreview
        {
            public float  SpeedTpm;
            public float  DistanceTiles;
            public float  TravelSeconds;
            public int    GatherSeconds;
            public float  TotalSeconds;
            public DateTime ArrivesAt;
            public DateTime GatherEndsAt;
            public DateTime ReturnsAt;
            public int    CarryCapacity;
            public int    EstimatedYield;
        }

        public static MarchPreview Preview(
            int originX, int originY,
            int destX,   int destY,
            int nodeLevel,
            TroopLoad troops)
        {
            float speed   = CalculateMarchSpeed(troops);
            float dist    = CalculateDistance(originX, originY, destX, destY);
            float travel  = CalculateTravelSeconds(dist, speed);
            int   gather  = GetGatherDurationSeconds(nodeLevel);

            var now          = DateTime.UtcNow;
            var arrivesAt    = now.AddSeconds(travel);
            var gatherEndsAt = arrivesAt.AddSeconds(gather);
            var returnsAt    = gatherEndsAt.AddSeconds(travel);

            int carry = CalculateCarryCapacity(troops);
            int yield = CalculateGatherYield(nodeLevel, carry);

            return new MarchPreview
            {
                SpeedTpm        = speed,
                DistanceTiles   = dist,
                TravelSeconds   = travel,
                GatherSeconds   = gather,
                TotalSeconds    = travel * 2 + gather,
                ArrivesAt       = arrivesAt,
                GatherEndsAt    = gatherEndsAt,
                ReturnsAt       = returnsAt,
                CarryCapacity   = carry,
                EstimatedYield  = yield,
            };
        }

        // -------------------------------------------------------------------------
        // Carry capacity & yield
        // -------------------------------------------------------------------------

        public static int CalculateCarryCapacity(TroopLoad troops)
        {
            int total = 0;
            foreach (var (type, count) in GetTroopCounts(troops))
                total += count * TroopCarryCapacity.GetValueOrDefault(type, 20);
            return total;
        }

        public static int CalculateGatherYield(int nodeLevel, int carryCapacity,
            int nodeRemainingAmount = int.MaxValue)
        {
            const int YIELD_PER_LEVEL = 500;
            int raw = nodeLevel * YIELD_PER_LEVEL;
            return Math.Min(raw, Math.Min(carryCapacity, nodeRemainingAmount));
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static IEnumerable<(string type, int count)> GetTroopCounts(TroopLoad t)
        {
            if (t == null) yield break;
            if (t.militia      > 0) yield return ("militia",      t.militia);
            if (t.spearman     > 0) yield return ("spearman",     t.spearman);
            if (t.archer       > 0) yield return ("archer",       t.archer);
            if (t.scout        > 0) yield return ("scout",        t.scout);
            if (t.knight       > 0) yield return ("knight",       t.knight);
            if (t.catapult     > 0) yield return ("catapult",     t.catapult);
            if (t.dragon_rider > 0) yield return ("dragon_rider", t.dragon_rider);
        }
    }
}
