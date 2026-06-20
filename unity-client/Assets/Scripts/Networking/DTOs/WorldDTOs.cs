using System;

namespace EternalKingdoms.Networking.DTOs
{
    // ── GET /api/worlds ───────────────────────────────────────────────────────

    [Serializable]
    public class WorldsResponseDto
    {
        public WorldDto[] worlds;
    }

    [Serializable]
    public class WorldDto
    {
        public string id;
        public string name;
        public string status;       // "active" | "ended" | "pending"
        public int maxKingdoms;
        public int season;
        public int seed;
        public string createdAt;
    }

    // ── GET /api/worlds/:id/map ───────────────────────────────────────────────

    [Serializable]
    public class WorldMapResponseDto
    {
        public WorldDto world;
        public WorldKingdomDto[] kingdoms;
        public MonsterSpawnDto[] spawns;
        public CrystalNodeDto[] crystalNodes;
    }

    // ── GET /api/worlds/:id/kingdoms ─────────────────────────────────────────

    [Serializable]
    public class WorldKingdomsResponseDto
    {
        public WorldKingdomDto[] kingdoms;
    }

    [Serializable]
    public class WorldKingdomDto
    {
        public string id;
        public string name;
        public int mapX;
        public int mapY;
        public long power;
        public string userId;
    }

    // ── GET /api/worlds/:id/spawns ────────────────────────────────────────────

    [Serializable]
    public class WorldSpawnsResponseDto
    {
        public MonsterSpawnDto[] spawns;
    }

    [Serializable]
    public class MonsterSpawnDto
    {
        public string id;
        public string monsterId;
        public string worldId;
        public int x;
        public int y;
        public int currentHp;
        public string respawnAt;
        public MonsterDto monster;
    }

    [Serializable]
    public class MonsterDto
    {
        public string id;
        public string assetId;
        public string name;
        public int tier;
        public long power;
        public int hp;
        public int attack;
        public int defense;
    }

    [Serializable]
    public class CrystalNodeDto
    {
        public string id;
        public string worldId;
        public int x;
        public int y;
        public string crystalType;  // fire|ice|earth|lightning|void|holy
        public int crystalYield;
        public string harvestedByKingdomId;
    }
}
