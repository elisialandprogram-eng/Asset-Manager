using UnityEngine;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.NFT
{
    /// <summary>
    /// Asset Registry ID ↔ NFT contract bridge.
    ///
    /// Phase 10 will wire this to the Polygon wallet adapter.
    /// For now, it provides lookup helpers for NFT-ready assets
    /// identified in GET /api/assets responses.
    ///
    /// Asset registry IDs are permanent — never renamed once assigned.
    /// NFT contract address and token ID fields are reserved (null until Phase 10).
    /// </summary>
    public static class NFTBridge
    {
        /// <summary>
        /// Returns true if the given asset has an NFT contract address assigned.
        /// Will always be false until Polygon integration ships in Phase 10.
        /// </summary>
        public static bool HasNFT(AssetDto asset)
        {
            return asset != null &&
                   !string.IsNullOrEmpty(asset.nftContractAddress) &&
                   !string.IsNullOrEmpty(asset.nftTokenId);
        }

        /// <summary>
        /// Returns a display label for the NFT ownership status of an asset.
        /// </summary>
        public static string GetOwnershipLabel(AssetDto asset)
        {
            if (!HasNFT(asset)) return "Game Asset";
            return $"NFT #{asset.nftTokenId}";
        }

        /// <summary>
        /// Constructs an Addressables key from an asset registry ID.
        /// e.g. "building_palace_001" → "Buildings/building_palace_001"
        /// </summary>
        public static string AssetIdToAddressableKey(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return null;
            var parts = assetId.Split('_');
            if (parts.Length < 2) return assetId;
            string category = char.ToUpper(parts[0][0]) + parts[0].Substring(1) + "s"; // "building" → "Buildings"
            return $"{category}/{assetId}";
        }
    }
}
