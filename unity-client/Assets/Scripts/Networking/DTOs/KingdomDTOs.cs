using System;

namespace EternalKingdoms.Networking.DTOs
{
    // ── GET /api/kingdoms/mine  &  GET /api/kingdoms/:id ─────────────────────

    [Serializable]
    public class KingdomDto
    {
        public string id;
        public string userId;
        public string worldId;
        public string name;
        public int mapX;
        public int mapY;
        public long power;
        public string createdAt;
    }

    // ── GET /api/kingdoms/:id/state ───────────────────────────────────────────

    [Serializable]
    public class KingdomStateDto
    {
        public KingdomDto kingdom;
        public ResourcesDto resources;
        public BuildingDto[] buildings;
        public ProductionRatesDto productionRates;
        public ResourceCapsDto resourceCaps;
        public PalaceTierDto palaceTier;
    }

    // ── GET /api/kingdoms/:id/buildings ───────────────────────────────────────

    [Serializable]
    public class BuildingsResponseDto
    {
        public BuildingDto[] buildings;
    }

    [Serializable]
    public class BuildingDto
    {
        public string id;
        public string kingdomId;
        public string buildingType;
        public int level;
        public string assetId;
        public bool isConstructing;
        public string constructionEndsAt;
        public string createdAt;
    }

    // ── GET /api/kingdoms/:id/resources ──────────────────────────────────────

    [Serializable]
    public class ResourcesResponseDto
    {
        public ResourcesDto resources;
    }

    [Serializable]
    public class ResourcesDto
    {
        public long food;
        public long wood;
        public long stone;
        public long iron;
        public long gold;
        public string updatedAt;
    }

    [Serializable]
    public class ProductionRatesDto
    {
        public float food;
        public float wood;
        public float stone;
        public float iron;
        public float gold;
    }

    [Serializable]
    public class ResourceCapsDto
    {
        public long food;
        public long wood;
        public long stone;
        public long iron;
        public long gold;
    }

    [Serializable]
    public class PalaceTierDto
    {
        public int tier;
        public int palaceLevel;
        public int constructionSlots;
        public int upgradeSlots;
        public string[] unlockedFeatures;
    }

    // ── GET /api/kingdoms/:id/queue ───────────────────────────────────────────

    [Serializable]
    public class UpgradeQueueResponseDto
    {
        public UpgradeQueueItemDto[] queue;
    }

    [Serializable]
    public class UpgradeQueueItemDto
    {
        public string id;
        public string kingdomId;
        public string buildingId;
        public string buildingType;
        public int fromLevel;
        public int toLevel;
        public string endsAt;
        public string status;
    }

    // ── GET /api/kingdoms/:id/construction-queue ──────────────────────────────

    [Serializable]
    public class ConstructionQueueResponseDto
    {
        public ConstructionQueueItemDto[] queue;
    }

    [Serializable]
    public class ConstructionQueueItemDto
    {
        public string id;
        public string kingdomId;
        public string buildingId;
        public string buildingType;
        public string startsAt;
        public string endsAt;
        public string status;
    }

    // ── GET /api/kingdoms/:id/construction-options ────────────────────────────

    [Serializable]
    public class ConstructionOptionsResponseDto
    {
        public ConstructionOptionDto[] options;
    }

    [Serializable]
    public class ConstructionOptionDto
    {
        public string buildingType;
        public string displayName;
        public ResourceCostDto cost;
        public int durationSeconds;
        public bool canAfford;
        public bool slotAvailable;
        public string blockedReason;
    }

    [Serializable]
    public class ResourceCostDto
    {
        public long food;
        public long wood;
        public long stone;
        public long iron;
        public long gold;
    }

    // ── POST /api/kingdoms/:id/construct ─────────────────────────────────────

    [Serializable]
    public class ConstructRequestDto
    {
        public string buildingType;
    }

    [Serializable]
    public class ConstructResponseDto
    {
        public string message;
        public BuildingDto building;
        public ConstructionQueueItemDto queueItem;
    }

    // ── POST /api/buildings/:id/upgrade ──────────────────────────────────────

    [Serializable]
    public class UpgradeResponseDto
    {
        public string message;
        public UpgradeQueueItemDto queueItem;
    }

    // ── GET /api/buildings/:id/upgrade-preview ────────────────────────────────

    [Serializable]
    public class UpgradePreviewDto
    {
        public int fromLevel;
        public int toLevel;
        public ResourceCostDto cost;
        public int durationSeconds;
        public bool canAfford;
        public bool isMaxLevel;
    }
}
