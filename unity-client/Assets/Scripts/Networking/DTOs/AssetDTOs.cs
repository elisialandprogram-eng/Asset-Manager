using System;

namespace EternalKingdoms.Networking.DTOs
{
    // ── GET /api/assets  &  GET /api/assets/:assetId ─────────────────────────

    [Serializable]
    public class AssetsResponseDto
    {
        public AssetDto[] assets;
    }

    [Serializable]
    public class AssetDto
    {
        public string assetId;
        public string category;
        public string name;
        public string nftContractAddress;
        public string nftTokenId;
        public string metadata;
        public string createdAt;
    }
}
