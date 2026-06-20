import crypto from "crypto";
import {
  worldRepository,
  userRepository,
  kingdomRepository,
  resourceRepository,
  buildingRepository,
  monsterRepository,
  monsterSpawnRepository,
  crystalNodeRepository,
} from "@workspace/db";
import { seededRandom, generateKingdomPosition } from "@workspace/game-engine";
import { logger } from "../lib/logger";

const DEV_EMAIL = "dev@eternalkingdoms.com";
const DEV_USERNAME = "DevAdmin";

function hashPassword(password: string): string {
  return crypto
    .createHash("sha256")
    .update(password + "ek_salt_2026")
    .digest("hex");
}

const DEV_PASSWORD_HASH = hashPassword(process.env["DEV_SEED_PASSWORD"] ?? "Rcbk123@#");

const WORLD_SEED = 42937;

export async function runWorldSeed(): Promise<void> {
  try {
    logger.info("Running world seed...");

    let world = await worldRepository.findByName("Aethoria");

    if (!world) {
      world = await worldRepository.insert({
        name: "Aethoria",
        description: "The first world of Eternal Kingdoms — a vast realm of ancient magic and endless war.",
        status: "active",
        maxKingdoms: 1000,
        seed: WORLD_SEED,
      });
      logger.info({ worldId: world.id }, "Created Aethoria world");
    } else if (!world.seed) {
      world = await worldRepository.update(world.id, { seed: WORLD_SEED });
      logger.info({ worldId: world.id }, "Assigned seed to Aethoria world");
    }

    const existingDev = await userRepository.findByEmail(DEV_EMAIL);

    if (!existingDev) {
      const devUser = await userRepository.insert({
        username: DEV_USERNAME,
        email: DEV_EMAIL,
        passwordHash: DEV_PASSWORD_HASH,
        role: "admin",
        worldId: world.id,
      });

      const devKingdom = await kingdomRepository.insert({
        userId: devUser.id,
        worldId: world.id,
        name: "The Dev Citadel",
        mapX: 5000,
        mapY: 5000,
        power: 9999,
      });

      await resourceRepository.insert({
        kingdomId: devKingdom.id,
        food: 999999,
        wood: 999999,
        stone: 999999,
        iron: 999999,
        gold: 999999,
      });

      await buildingRepository.insert({
        kingdomId: devKingdom.id,
        buildingType: "palace",
        level: 10,
        assetId: "building_palace_001",
      });

      logger.info({ userId: devUser.id }, "Dev account created");
    } else {
      const correctHash = hashPassword(process.env["DEV_SEED_PASSWORD"] ?? "Rcbk123@#");
      if (existingDev.passwordHash !== correctHash) {
        await userRepository.updatePassword(existingDev.id, correctHash);
        logger.info({ userId: existingDev.id }, "Dev account password updated");
      }
    }

    const firstMonster = await monsterRepository.findFirstOne();
    if (!firstMonster) {
      await monsterRepository.insertMany([
        { assetId: "monster_bandit_001", name: "Bandit", tier: "common", power: 100, hp: 800, attack: 40, defense: 20 },
        { assetId: "monster_wolf_001", name: "Dire Wolf", tier: "common", power: 150, hp: 600, attack: 60, defense: 15 },
        { assetId: "monster_ogre_001", name: "Ogre", tier: "uncommon", power: 400, hp: 2500, attack: 120, defense: 80 },
        { assetId: "monster_guardian_001", name: "Ancient Guardian", tier: "elite", power: 1500, hp: 8000, attack: 350, defense: 280 },
        { assetId: "monster_dragon_001", name: "Dragon", tier: "boss", power: 5000, hp: 25000, attack: 800, defense: 500 },
      ]);
      logger.info("Seeded 5 monster types");
    }

    const firstSpawn = await monsterSpawnRepository.findFirstByWorldId(world.id);

    if (!firstSpawn) {
      const monsters = await monsterRepository.findAll();
      const spawns = [];
      for (let i = 0; i < 28; i++) {
        const monster = monsters[Math.floor(seededRandom(WORLD_SEED, i * 3) * monsters.length)];
        spawns.push({
          monsterId: monster!.id,
          worldId: world.id,
          x: Math.floor(seededRandom(WORLD_SEED, i * 3 + 1) * 9000 + 500),
          y: Math.floor(seededRandom(WORLD_SEED, i * 3 + 2) * 9000 + 500),
          currentHp: monster!.hp,
        });
      }
      await monsterSpawnRepository.insertMany(spawns);
      logger.info({ count: spawns.length }, "Seeded monster spawns");
    }

    const firstCrystal = await crystalNodeRepository.findFirstByWorldId(world.id);

    if (!firstCrystal) {
      const crystalTypes = ["fire", "ice", "earth", "lightning", "void", "holy"] as const;
      const crystalData = [];
      for (let i = 0; i < 15; i++) {
        crystalData.push({
          worldId: world.id,
          x: Math.floor(seededRandom(WORLD_SEED + 500, i * 2) * 9000 + 500),
          y: Math.floor(seededRandom(WORLD_SEED + 500, i * 2 + 1) * 9000 + 500),
          crystalType: crystalTypes[Math.floor(seededRandom(WORLD_SEED + 500, i * 2 + 2) * crystalTypes.length)],
          crystalYield: Math.floor(seededRandom(WORLD_SEED + 500, i * 2 + 3) * 900 + 100),
        });
      }
      await crystalNodeRepository.insertMany(crystalData);
      logger.info({ count: crystalData.length }, "Seeded crystal nodes");
    }

    const unplaced = await kingdomRepository.findUnplacedByWorldId(world.id);

    if (unplaced.length > 0) {
      const placed = await kingdomRepository.findPlacedByWorldId(world.id);
      const positions = placed
        .filter((k) => k.x !== null && k.y !== null)
        .map((k) => ({ x: k.x as number, y: k.y as number }));

      for (const kingdom of unplaced) {
        const pos = generateKingdomPosition(kingdom.id, WORLD_SEED, positions);
        await kingdomRepository.updatePosition(kingdom.id, pos.x, pos.y);
        positions.push(pos);
      }
      logger.info({ count: unplaced.length }, "Assigned positions to unplaced kingdoms");
    }

    logger.info("World seed complete");
  } catch (err) {
    logger.error({ err }, "World seed error — continuing without seed");
  }
}
