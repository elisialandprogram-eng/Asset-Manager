using UnityEngine;

namespace EternalKingdoms.Data
{
    /// <summary>
    /// ScriptableObject defining static properties for one building type.
    /// One asset per building type stored in:
    ///   Assets/ScriptableObjects/Buildings/
    ///
    /// The buildingType field must match the backend string exactly
    /// (e.g. "farm", "palace", "barracks"). Asset registry IDs are permanent.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "EternalKingdoms/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Must match backend buildingType string exactly.")]
        public string buildingType;

        [Tooltip("Display name shown in UI.")]
        public string displayName;

        [Tooltip("Asset registry ID (e.g. building_palace_001). Permanent — never rename.")]
        public string assetRegistryId;

        [Header("Visuals")]
        [Tooltip("Addressables key for the 3D prefab (URP).")]
        public string prefabAddressableKey;

        [Tooltip("Addressables key for the UI icon sprite.")]
        public string iconAddressableKey;

        [Header("Slot Layout")]
        [Tooltip("Which ring this building occupies in the fixed kingdom layout.")]
        public KingdomRing ring;

        [Tooltip("Slot index within the ring (0-based).")]
        public int ringSlotIndex;

        [Tooltip("Maximum instances of this building type per kingdom.")]
        public int maxCount;

        [Header("Description")]
        [TextArea(2, 4)]
        public string description;

        [Header("Unlock")]
        [Tooltip("Minimum palace tier required to construct this building.")]
        public int requiredPalaceTier;
    }

    public enum KingdomRing
    {
        Center = 0,     // Palace only
        Inner = 1,      // Academy, Treasury, Warehouse, Alliance Center
        Middle = 2,     // Barracks, Stable, Archery Range, Siege Workshop
        Outer = 3       // Farms, Lumber Mills, Quarries, Mines
    }
}
