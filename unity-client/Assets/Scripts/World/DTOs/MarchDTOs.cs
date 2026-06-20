using System;
using System.Collections.Generic;

namespace EternalKingdoms.World.DTOs
{
    // -------------------------------------------------------------------------
    // Troop composition (matches API and DB schema)
    // -------------------------------------------------------------------------

    [Serializable]
    public class TroopLoad
    {
        public int militia;
        public int spearman;
        public int archer;
        public int scout;
        public int knight;
        public int catapult;
        public int dragon_rider;

        public int Total()
        {
            return militia + spearman + archer + scout + knight + catapult + dragon_rider;
        }
    }

    // -------------------------------------------------------------------------
    // Resource gathered on return
    // -------------------------------------------------------------------------

    [Serializable]
    public class ResourceGathered
    {
        public float food;
        public float wood;
        public float stone;
        public float iron;
        public float gold;
        public float crystal;
    }

    // -------------------------------------------------------------------------
    // March DTO (matches API response shape)
    // -------------------------------------------------------------------------

    [Serializable]
    public class MarchDto
    {
        public int    id;
        public int    worldId;
        public int    kingdomId;
        public string marchType;        // gather | attack | reinforce | scout | rally
        public string status;           // outbound | gathering | returning | completed | cancelled
        public int    originX;
        public int    originY;
        public int    destX;
        public int    destY;
        public int    spawnId;          // 0 if null
        public int    targetKingdomId;  // 0 if null
        public TroopLoad troops;
        public float  speedTpm;
        public float  distanceTiles;
        public string startedAt;        // ISO 8601
        public string arrivesAt;        // ISO 8601
        public string gatherEndsAt;     // ISO 8601, null if not gather
        public string returnsAt;        // ISO 8601
        public string completedAt;      // ISO 8601, null if active
        public ResourceGathered resourcesGathered;
        public int    carryCapacity;
        public int    estimatedYield;
    }

    // -------------------------------------------------------------------------
    // Resource node DTO (world_spawns filtered to type=resource)
    // -------------------------------------------------------------------------

    [Serializable]
    public class ResourceNodeDto
    {
        public int    id;
        public int    worldId;
        public string spawnType;        // always "resource" here
        public string spawnSubtype;     // farm | lumber | iron | gold | crystal | stone
        public int    level;
        public int    tileX;
        public int    tileY;
        public int    posX;
        public int    posY;
        public string biome;
        public string status;           // active | expired | depleted
        public string spawnedAt;
        public string expiresAt;
    }

    // -------------------------------------------------------------------------
    // Request / Response wrappers
    // -------------------------------------------------------------------------

    [Serializable]
    public class CreateMarchRequest
    {
        public int      kingdomId;
        public int      worldId;
        public int      spawnId;
        public TroopLoad troops;
    }

    [Serializable]
    public class CreateMarchResponse
    {
        public MarchDto march;
    }

    [Serializable]
    public class ListMarchesResponse
    {
        public MarchDto[] marches;
    }

    [Serializable]
    public class ResourceNodesResponse
    {
        public int               worldId;
        public ResourceNodeDto[] nodes;
    }
}
