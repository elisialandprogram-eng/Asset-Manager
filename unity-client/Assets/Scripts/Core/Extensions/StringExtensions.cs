using System;

namespace EternalKingdoms.Core.Extensions
{
    /// <summary>General string utility extensions.</summary>
    public static class StringExtensions
    {
        /// <summary>Converts a backend buildingType snake_case to a display name (e.g. "lumber_mill" → "Lumber Mill").</summary>
        public static string ToDisplayName(this string buildingType)
        {
            if (string.IsNullOrEmpty(buildingType)) return "";
            var parts = buildingType.Split('_');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }

        /// <summary>Returns true if the ISO 8601 timestamp has already passed.</summary>
        public static bool IsPast(this string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return false;
            return DateTime.TryParse(isoTimestamp, out var dt) && dt < DateTime.UtcNow;
        }

        /// <summary>Returns seconds remaining until the ISO 8601 timestamp.</summary>
        public static float SecondsUntil(this string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return 0f;
            if (!DateTime.TryParse(isoTimestamp, out var dt)) return 0f;
            return (float)(dt - DateTime.UtcNow).TotalSeconds;
        }
    }
}
