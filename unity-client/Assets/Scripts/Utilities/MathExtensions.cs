using UnityEngine;

namespace EternalKingdoms.Utilities
{
    /// <summary>
    /// General math helpers used throughout the game client.
    /// </summary>
    public static class MathExtensions
    {
        // ── World coordinate mapping ──────────────────────────────────────────

        private const float BACKEND_SPACE_MAX = 10000f;
        private const float WORLD_EXTENT_UNITY = 10240f;  // 2048 tiles × 5 units/tile
        private const float TILE_SIZE = 5f;
        private const int WORLD_TILES = 2048;

        /// <summary>Converts backend integer coordinate (0–10000) to Unity world units.</summary>
        public static float BackendToUnity(float backendCoord)
        {
            return (backendCoord / BACKEND_SPACE_MAX) * WORLD_EXTENT_UNITY - WORLD_EXTENT_UNITY * 0.5f;
        }

        /// <summary>Converts a backend (x, y) position to a Unity XZ world position (y=0).</summary>
        public static Vector3 BackendToUnityXZ(int backendX, int backendY)
        {
            return new Vector3(BackendToUnity(backendX), 0f, BackendToUnity(backendY));
        }

        /// <summary>Converts tile coordinates to Unity world position.</summary>
        public static Vector3 TileToUnity(int tx, int tz)
        {
            return new Vector3(
                tx * TILE_SIZE - WORLD_EXTENT_UNITY * 0.5f,
                0f,
                tz * TILE_SIZE - WORLD_EXTENT_UNITY * 0.5f);
        }

        /// <summary>Converts Unity world position to tile coordinates.</summary>
        public static Vector2Int UnityToTile(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt((worldPos.x + WORLD_EXTENT_UNITY * 0.5f) / TILE_SIZE),
                Mathf.FloorToInt((worldPos.z + WORLD_EXTENT_UNITY * 0.5f) / TILE_SIZE));
        }

        // ── Chunk math ────────────────────────────────────────────────────────

        private const int CHUNK_SIZE = 64;

        public static Vector2Int TileToChunk(int tx, int tz)
        {
            return new Vector2Int(tx / CHUNK_SIZE, tz / CHUNK_SIZE);
        }

        public static Vector2Int ChunkOriginTile(int cx, int cz)
        {
            return new Vector2Int(cx * CHUNK_SIZE, cz * CHUNK_SIZE);
        }

        // ── General ───────────────────────────────────────────────────────────

        /// <summary>Tile-space Euclidean distance (matches WORLD_ARCHITECTURE_BIBLE.md).</summary>
        public static float TileDistance(Vector2Int a, Vector2Int b)
        {
            float dx = b.x - a.x;
            float dz = b.y - a.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Eased lerp using smoothstep.</summary>
        public static float SmoothStep(float a, float b, float t)
        {
            t = Mathf.Clamp01(t);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(a, b, t);
        }

        /// <summary>Returns a color for a crystal type string.</summary>
        public static Color CrystalColor(string crystalType) => crystalType switch
        {
            "fire" => new Color(1f, 0.3f, 0.1f),
            "ice" => new Color(0.4f, 0.8f, 1f),
            "earth" => new Color(0.5f, 0.8f, 0.3f),
            "lightning" => new Color(1f, 0.9f, 0.1f),
            "void" => new Color(0.5f, 0.1f, 0.9f),
            "holy" => new Color(1f, 0.95f, 0.6f),
            _ => Color.white
        };

        /// <summary>Formats seconds as "Xh Ym Zs" remaining-time string.</summary>
        public static string FormatDuration(float totalSeconds)
        {
            if (totalSeconds <= 0f) return "Done";
            int h = (int)(totalSeconds / 3600f);
            int m = (int)((totalSeconds % 3600f) / 60f);
            int s = (int)(totalSeconds % 60f);
            if (h > 0) return $"{h}h {m}m";
            if (m > 0) return $"{m}m {s}s";
            return $"{s}s";
        }
    }
}
