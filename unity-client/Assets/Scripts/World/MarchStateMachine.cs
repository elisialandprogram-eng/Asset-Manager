using System;
using EternalKingdoms.World.DTOs;

namespace EternalKingdoms.World
{
    // -------------------------------------------------------------------------
    // March state enum — mirrors DB enum exactly
    // -------------------------------------------------------------------------

    public enum MarchState
    {
        Outbound,
        Gathering,
        Returning,
        Completed,
        Cancelled,
    }

    // -------------------------------------------------------------------------
    // MarchModel — full in-memory representation of one march
    // -------------------------------------------------------------------------

    public class MarchModel
    {
        public int      Id;
        public int      WorldId;
        public int      KingdomId;
        public string   MarchType;
        public MarchState State;

        // Tile-space coordinates
        public int      OriginX;
        public int      OriginY;
        public int      DestX;
        public int      DestY;

        public int      SpawnId;
        public int      TargetKingdomId;

        public TroopLoad Troops;
        public float    SpeedTpm;
        public float    DistanceTiles;

        public DateTime StartedAt;
        public DateTime ArrivesAt;
        public DateTime GatherEndsAt;
        public DateTime ReturnsAt;
        public DateTime? CompletedAt;

        public ResourceGathered ResourcesGathered;

        // -------------------------------------------------------------------------
        // Factory — parse from API DTO
        // -------------------------------------------------------------------------

        public static MarchModel FromDto(MarchDto dto)
        {
            return new MarchModel
            {
                Id                = dto.id,
                WorldId           = dto.worldId,
                KingdomId         = dto.kingdomId,
                MarchType         = dto.marchType,
                State             = ParseState(dto.status),
                OriginX           = dto.originX,
                OriginY           = dto.originY,
                DestX             = dto.destX,
                DestY             = dto.destY,
                SpawnId           = dto.spawnId,
                TargetKingdomId   = dto.targetKingdomId,
                Troops            = dto.troops ?? new TroopLoad(),
                SpeedTpm          = dto.speedTpm,
                DistanceTiles     = dto.distanceTiles,
                StartedAt         = ParseUtc(dto.startedAt),
                ArrivesAt         = ParseUtc(dto.arrivesAt),
                GatherEndsAt      = ParseUtc(dto.gatherEndsAt),
                ReturnsAt         = ParseUtc(dto.returnsAt),
                CompletedAt       = string.IsNullOrEmpty(dto.completedAt) ? null : (DateTime?)ParseUtc(dto.completedAt),
                ResourcesGathered = dto.resourcesGathered,
            };
        }

        // -------------------------------------------------------------------------
        // Derived properties
        // -------------------------------------------------------------------------

        public bool IsActive =>
            State == MarchState.Outbound ||
            State == MarchState.Gathering ||
            State == MarchState.Returning;

        public bool IsTerminal =>
            State == MarchState.Completed ||
            State == MarchState.Cancelled;

        /// Returns 0.0–1.0 progress for the current phase.
        public float PhaseProgress(DateTime now)
        {
            switch (State)
            {
                case MarchState.Outbound:
                {
                    double total = (ArrivesAt - StartedAt).TotalSeconds;
                    double elapsed = (now - StartedAt).TotalSeconds;
                    return (float)Math.Clamp(elapsed / total, 0.0, 1.0);
                }
                case MarchState.Gathering:
                {
                    double total = (GatherEndsAt - ArrivesAt).TotalSeconds;
                    double elapsed = (now - ArrivesAt).TotalSeconds;
                    return (float)Math.Clamp(elapsed / total, 0.0, 1.0);
                }
                case MarchState.Returning:
                {
                    double total = (ReturnsAt - (GatherEndsAt != DateTime.MinValue ? GatherEndsAt : ArrivesAt)).TotalSeconds;
                    double elapsed = (now - (GatherEndsAt != DateTime.MinValue ? GatherEndsAt : ArrivesAt)).TotalSeconds;
                    return (float)Math.Clamp(elapsed / total, 0.0, 1.0);
                }
                default:
                    return 1.0f;
            }
        }

        /// World-space XZ position of the march icon at the given time.
        public (float worldX, float worldZ) InterpolatedPosition(DateTime now)
        {
            const float TILE = 5.0f;
            const float OFFSET = 5120f;

            float ox = OriginX * TILE - OFFSET;
            float oz = OriginY * TILE - OFFSET;
            float dx = DestX * TILE - OFFSET;
            float dz = DestY * TILE - OFFSET;

            float t = PhaseProgress(now);

            switch (State)
            {
                case MarchState.Outbound:
                    return (Lerp(ox, dx, t), Lerp(oz, dz, t));
                case MarchState.Gathering:
                    return (dx, dz);
                case MarchState.Returning:
                    return (Lerp(dx, ox, t), Lerp(dz, oz, t));
                default:
                    return (ox, oz);
            }
        }

        /// Seconds until the current phase transition.
        public double SecondsUntilNextTransition(DateTime now)
        {
            switch (State)
            {
                case MarchState.Outbound:  return (ArrivesAt   - now).TotalSeconds;
                case MarchState.Gathering: return (GatherEndsAt - now).TotalSeconds;
                case MarchState.Returning: return (ReturnsAt    - now).TotalSeconds;
                default: return 0;
            }
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static MarchState ParseState(string s) => s switch
        {
            "outbound"  => MarchState.Outbound,
            "gathering" => MarchState.Gathering,
            "returning" => MarchState.Returning,
            "completed" => MarchState.Completed,
            "cancelled" => MarchState.Cancelled,
            _           => MarchState.Outbound,
        };

        private static DateTime ParseUtc(string s)
        {
            if (string.IsNullOrEmpty(s)) return DateTime.UtcNow;
            return DateTime.TryParse(s, out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    // -------------------------------------------------------------------------
    // MarchStateMachine — pure state transition logic (no MonoBehaviour)
    // -------------------------------------------------------------------------

    public static class MarchStateMachine
    {
        /// Evaluate which state a march should be in given server timestamps.
        /// Called both when loading from backend and on every simulation tick.
        public static MarchState Evaluate(MarchModel march, DateTime now)
        {
            if (march.IsTerminal) return march.State;

            if (now < march.ArrivesAt)
                return MarchState.Outbound;

            if (march.GatherEndsAt != DateTime.MinValue && now < march.GatherEndsAt)
                return MarchState.Gathering;

            if (now < march.ReturnsAt)
                return MarchState.Returning;

            // returnsAt has passed — completed (server will write DB; client infers)
            return MarchState.Completed;
        }

        /// Apply state transition and return true if state changed.
        public static bool Tick(MarchModel march, DateTime now)
        {
            var next = Evaluate(march, now);
            if (next == march.State) return false;
            march.State = next;
            return true;
        }
    }
}
