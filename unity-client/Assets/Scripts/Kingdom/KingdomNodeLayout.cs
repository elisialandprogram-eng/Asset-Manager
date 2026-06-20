using UnityEngine;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// Defines the fixed building node positions for the Kingdom scene.
    ///
    /// Layout matches UNITY_KINGDOM_SYSTEM.md:
    ///   Center Ring  — Palace
    ///   Inner Ring   — Academy, Treasury, Warehouse, Alliance Center
    ///   Middle Ring  — Barracks, Stable, Archery Range, Siege Workshop
    ///   Outer Ring   — Farm ×5, Lumber Mill ×5, Quarry ×4, Iron Mine ×3, Gold Mine ×2
    ///
    /// All positions are in local space relative to the Kingdom root (0,0,0).
    /// Visual style: realistic medieval fantasy, isometric view.
    /// </summary>
    public static class KingdomNodeLayout
    {
        public const float INNER_RADIUS = 18f;
        public const float MIDDLE_RADIUS = 36f;
        public const float OUTER_RADIUS = 60f;

        /// <summary>
        /// Returns the local position for a node given its ring and slot index.
        /// </summary>
        public static Vector3 GetNodePosition(int ring, int slotIndex, int totalInRing)
        {
            if (ring == 0) return Vector3.zero; // Palace — always center

            float radius = ring switch
            {
                1 => INNER_RADIUS,
                2 => MIDDLE_RADIUS,
                _ => OUTER_RADIUS,
            };

            float angleStep = 360f / totalInRing;
            float angle = angleStep * slotIndex - 90f; // start from top
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
        }

        /// <summary>
        /// Returns all 12 predefined node descriptors for the Kingdom scene.
        /// These are fixed — every kingdom has the same slot layout.
        /// </summary>
        public static NodeDescriptor[] GetAllNodes()
        {
            return new NodeDescriptor[]
            {
                // Center (Ring 0)
                new("palace",           "Palace",           0, 0, 1),

                // Inner (Ring 1) — 4 nodes
                new("academy",          "Academy",          1, 0, 4),
                new("treasury",         "Treasury",         1, 1, 4),
                new("warehouse",        "Warehouse",        1, 2, 4),
                new("alliance_center",  "Alliance Center",  1, 3, 4),

                // Middle (Ring 2) — 4 nodes
                new("barracks",         "Barracks",         2, 0, 4),
                new("stable",           "Stable",           2, 1, 4),
                new("archery_range",    "Archery Range",    2, 2, 4),
                new("siege_workshop",   "Siege Workshop",   2, 3, 4),

                // Outer (Ring 3) — 4 nodes representing production building groups
                new("farm",             "Farm",             3, 0, 4),
                new("lumber_mill",      "Lumber Mill",      3, 1, 4),
                new("quarry",           "Quarry",           3, 2, 4),
                new("iron_mine",        "Iron Mine",        3, 3, 4),
            };
        }
    }

    public readonly struct NodeDescriptor
    {
        public readonly string BuildingType;
        public readonly string DisplayName;
        public readonly int Ring;
        public readonly int SlotIndex;
        public readonly int TotalInRing;

        public NodeDescriptor(string buildingType, string displayName, int ring, int slotIndex, int totalInRing)
        {
            BuildingType = buildingType;
            DisplayName = displayName;
            Ring = ring;
            SlotIndex = slotIndex;
            TotalInRing = totalInRing;
        }

        public Vector3 Position => KingdomNodeLayout.GetNodePosition(Ring, SlotIndex, TotalInRing);
    }
}
