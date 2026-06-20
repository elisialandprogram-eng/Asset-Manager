/**
 * marchProcessor.ts — Tick-driven march lifecycle engine.
 *
 * Called once per resource tick (every 60 seconds).
 *
 * State transitions:
 *   Gather marches:
 *     OUTBOUND   → (arrivesAt ≤ now)    → GATHERING   (start gather timer)
 *     GATHERING  → (gatherEndsAt ≤ now) → RETURNING   (compute resources, start return)
 *     RETURNING  → (returnsAt ≤ now)    → COMPLETED   (deposit resources into kingdom)
 *
 *   Attack Monster marches (Phase 4):
 *     OUTBOUND   → (arrivesAt ≤ now)    → [combat resolves instantly] → RETURNING
 *     RETURNING  → (returnsAt ≤ now)    → COMPLETED   (return troops, admit wounded, grant rewards)
 *
 * Architecture:
 *   - No raw DB access — uses repositories only.
 *   - No in-process state — all state lives in DB.
 *   - Idempotent: safe to call multiple times for the same march.
 */

import {
  marchRepository,
  resourceRepository,
  spawnRepository,
  monsterRepository,
  monsterSpawnRepository,
  heroRepository,
  battleReportRepository,
  hospitalRepository,
  inventoryRepository,
  troopInventoryRepository,
} from "@workspace/db";
import {
  calculateGatherYield,
  resolve as resolveCombat,
  splitCasualties,
  applyHospitalCapacity,
  generateLoot,
  type MonsterStats,
  type HeroBuff,
} from "@workspace/game-engine";
import type { March } from "@workspace/db";
import { logger } from "../lib/logger";

// ---------------------------------------------------------------------------
// Gather: Outbound → Gathering
// ---------------------------------------------------------------------------

async function processGatherArrivals(now: Date): Promise<number> {
  const arriving = await marchRepository.findPendingArrivals(now);
  const gatherArrivals = arriving.filter((m) => m.marchType === "gather");
  let processed = 0;

  for (const march of gatherArrivals) {
    try {
      if (!march.gatherEndsAt) {
        logger.warn({ marchId: march.id }, "march-processor: gather march has no gatherEndsAt, skipping");
        continue;
      }
      await marchRepository.markArrived(march.id, march.gatherEndsAt);
      logger.debug({ marchId: march.id }, "march-processor: gather march arrived → gathering");
      processed++;
    } catch (err) {
      logger.error({ err, marchId: march.id }, "march-processor: error processing gather arrival");
    }
  }

  return processed;
}

// ---------------------------------------------------------------------------
// Attack Monster: Outbound → Combat → Returning (instant)
// ---------------------------------------------------------------------------

async function processMonsterAttackArrivals(now: Date): Promise<number> {
  const arriving = await marchRepository.findPendingArrivals(now);
  const attackArrivals = arriving.filter((m) => m.marchType === "attack_monster");
  let processed = 0;

  for (const march of attackArrivals) {
    try {
      await resolveMonsterCombat(march, now);
      processed++;
    } catch (err) {
      logger.error({ err, marchId: march.id }, "march-processor: error resolving monster combat");
    }
  }

  return processed;
}

async function resolveMonsterCombat(march: March, now: Date): Promise<void> {
  if (!march.spawnId) {
    logger.warn({ marchId: march.id }, "march-processor: attack_monster march has no spawnId");
    return;
  }

  const spawn = await monsterSpawnRepository.findById(march.spawnId);
  if (!spawn) {
    logger.warn({ marchId: march.id, spawnId: march.spawnId }, "march-processor: monster spawn not found, skipping");
    return;
  }

  const monster = await monsterRepository.findById(spawn.monsterId);
  if (!monster) {
    logger.warn({ marchId: march.id }, "march-processor: monster definition not found, skipping");
    return;
  }

  // Build hero buff if hero assigned
  let heroBuff: HeroBuff | undefined;
  if (march.heroId) {
    const hero = await heroRepository.findById(march.heroId);
    if (hero && hero.stats) {
      const stats = hero.stats as { command: number; attack: number; defense: number; speed: number; gathering: number };
      heroBuff = {
        attackBonus:  stats.attack,
        defenseBonus: stats.defense,
        speedBonus:   stats.speed,
        roundSkills:  {},
      };
    }
  }

  const monsterStats: MonsterStats = {
    name:        monster.name,
    tier:        monster.tier,
    hp:          spawn.currentHp > 0 ? spawn.currentHp : monster.hp,
    attack:      monster.attack,
    defense:     monster.defense,
    combatClass: null,
  };

  const attackerTroops = march.troops as Record<string, number>;

  // Run deterministic combat engine
  const result = resolveCombat({
    attackerTroops,
    monster: monsterStats,
    heroBuff,
    researchAttackBonus:  0,
    researchDefenseBonus: 0,
  });

  // Determine casualties split (field op: 50% dead, 50% wounded)
  const casualtySplit = splitCasualties(result.attackerTotalLosses, true);

  // Apply hospital capacity
  const hospital = await hospitalRepository.findByKingdomId(march.kingdomId);
  const hospitalCapacity = hospital?.capacity ?? 500;
  const currentWounded  = (hospital?.woundedTroops ?? {}) as Record<string, number>;
  const { admitted: woundedAdmitted, overflow: permanentDead } = applyHospitalCapacity(
    casualtySplit.wounded,
    currentWounded,
    hospitalCapacity,
  );

  // Generate loot (only if attacker won)
  const loot = result.attackerWon ? generateLoot(monster.tier) : {
    food: 0, wood: 0, stone: 0, iron: 0, gold: 0, crystal: 0, heroXp: 0, items: {},
  };

  // Persist battle report
  const report = await battleReportRepository.insert({
    attackerKingdomId:    march.kingdomId,
    defenderMonsterSpawnId: march.spawnId,
    monsterName:          monster.name,
    monsterTier:          monster.tier,
    attackerWon:          result.attackerWon,
    roundsFought:         result.roundsFought,
    rounds:               result.rounds,
    attackerTroopsSent:   result.attackerTroopsSent,
    attackerTroopsKilled: {
      ...casualtySplit.killed,
      ...permanentDead,
    },
    attackerTroopsWounded: woundedAdmitted,
    attackerTroopsSurvived: result.attackerTroopsSurvived,
    rewardsGranted:       {
      food:    loot.food,
      wood:    loot.wood,
      stone:   loot.stone,
      iron:    loot.iron,
      gold:    loot.gold,
      crystal: loot.crystal,
      heroXp:  loot.heroXp,
      items:   loot.items,
    },
    heroId:  march.heroId ?? null,
    marchId: march.id,
  });

  // Admit wounded to hospital
  if (Object.keys(woundedAdmitted).length > 0) {
    await hospitalRepository.admitWounded(march.kingdomId, woundedAdmitted);
  }

  // Grant hero XP
  if (march.heroId && loot.heroXp > 0) {
    await heroRepository.addExperience(march.heroId, loot.heroXp);
  }

  // Store loot items (deposited on march return)
  // Resources deposited in processMonsterAttackReturns

  // Update monster spawn HP / mark defeated
  if (result.attackerWon) {
    const respawnSeconds = getRespawnSeconds(monster.tier);
    const respawnAt = new Date(now.getTime() + respawnSeconds * 1000);
    await spawnRepository.markDepleted(march.spawnId);
    logger.info({ marchId: march.id, monsterName: monster.name }, "march-processor: monster defeated");
  } else {
    // Monster survives with reduced HP — update it
    // (spawnRepository doesn't have updateHp in Phase 3; skip for now — Phase 5 adds partial damage)
    logger.info({ marchId: march.id, monsterName: monster.name }, "march-processor: monster survived combat");
  }

  // Transition march: OUTBOUND → RETURNING (skip gathering phase for combat)
  const travelMs = march.arrivesAt.getTime() - march.startedAt.getTime();
  const returnsAt = new Date(now.getTime() + travelMs);

  // Pack loot resources into resourcesGathered for deposit on return
  const resourcesGathered: Record<string, number> = result.attackerWon ? {
    food:    loot.food,
    wood:    loot.wood,
    stone:   loot.stone,
    iron:    loot.iron,
    gold:    loot.gold,
    crystal: loot.crystal,
  } : {};

  // Store battle report ID + survivors on march, transition to returning
  await marchRepository.markReturning(
    march.id,
    now,
    returnsAt,
    resourcesGathered,
  );

  // Update marchRepository battleReportId (patch via markReturning extension not available;
  // best-effort: log the report ID for reconciliation)
  logger.info(
    { marchId: march.id, reportId: report.id, attackerWon: result.attackerWon },
    "march-processor: combat resolved, march returning",
  );
}

function getRespawnSeconds(tier: string): number {
  const times: Record<string, number> = {
    common:   1800,   // 30 min
    uncommon: 3600,   // 1 hr
    rare:     14400,  // 4 hr
    elite:    28800,  // 8 hr
    boss:     86400,  // 24 hr
    ancient:  172800, // 48 hr
  };
  return times[tier] ?? 1800;
}

// ---------------------------------------------------------------------------
// Gather: Gathering → Returning
// ---------------------------------------------------------------------------

async function processGatherEnd(now: Date): Promise<number> {
  const done = await marchRepository.findPendingGatherEnd(now);
  let processed = 0;

  for (const march of done) {
    try {
      let resourcesGathered: Record<string, number> = {};

      if (march.spawnId && march.marchType === "gather") {
        const subtypeToResource: Record<string, string> = {
          farm:    "food",
          lumber:  "wood",
          iron:    "iron",
          gold:    "gold",
          crystal: "crystal",
          stone:   "stone",
        };

        const PHASE3_DEFAULT_LEVEL = 1;
        const yieldAmount = calculateGatherYield(PHASE3_DEFAULT_LEVEL, march.troops as Record<string, number>);

        try {
          await spawnRepository.markDepleted(march.spawnId);
        } catch {
          // Non-fatal
        }

        resourcesGathered = { food: yieldAmount };
      }

      const returnStartedAt = now;
      const travelMs = march.arrivesAt.getTime() - march.startedAt.getTime();
      const returnsAt = new Date(returnStartedAt.getTime() + travelMs);

      await marchRepository.markReturning(march.id, returnStartedAt, returnsAt, resourcesGathered);
      logger.debug({ marchId: march.id, resourcesGathered }, "march-processor: gather complete → returning");
      processed++;
    } catch (err) {
      logger.error({ err, marchId: march.id }, "march-processor: error processing gather end");
    }
  }

  return processed;
}

// ---------------------------------------------------------------------------
// Both march types: Returning → Completed
// ---------------------------------------------------------------------------

async function processReturns(now: Date): Promise<number> {
  const returning = await marchRepository.findPendingReturns(now);
  let processed = 0;

  for (const march of returning) {
    try {
      const gathered = (march.resourcesGathered ?? {}) as Record<string, number>;

      if (Object.keys(gathered).length > 0) {
        await depositResources(march.kingdomId, gathered);
      }

      // For attack_monster returns: restore surviving troops to inventory
      if (march.marchType === "attack_monster") {
        await processMonsterAttackReturn(march);
      }

      await marchRepository.markCompleted(march.id);
      logger.debug(
        { marchId: march.id, marchType: march.marchType, gathered },
        "march-processor: march completed",
      );
      processed++;
    } catch (err) {
      logger.error({ err, marchId: march.id }, "march-processor: error processing return");
    }
  }

  return processed;
}

async function processMonsterAttackReturn(march: March): Promise<void> {
  // We need to find the battle report to get survivors
  // Look up latest report for this march via marchId
  const reports = await battleReportRepository.findByKingdomId(march.kingdomId, 10, 0);
  const report = reports.find((r) => r.marchId === march.id);

  if (!report) {
    logger.warn({ marchId: march.id }, "march-processor: no battle report found for returning attack march");
    return;
  }

  // Return surviving troops to inventory
  const survived = report.attackerTroopsSurvived as Record<string, number>;
  if (Object.keys(survived).length > 0) {
    await troopInventoryRepository.returnTroops(march.kingdomId, survived);
  }

  // Grant loot items (resources already deposited via depositResources above)
  const rewards = report.rewardsGranted as { items?: Record<string, number> };
  if (rewards?.items && Object.keys(rewards.items).length > 0) {
    await inventoryRepository.addItems(march.kingdomId, rewards.items);
  }
}

async function depositResources(
  kingdomId: number,
  gathered: Record<string, number>,
): Promise<void> {
  const resources = await resourceRepository.findByKingdomId(kingdomId);
  if (!resources) return;

  const updated = {
    food:  resources.food  + (gathered["food"]    ?? 0),
    wood:  resources.wood  + (gathered["wood"]    ?? 0),
    stone: resources.stone + (gathered["stone"]   ?? 0),
    iron:  resources.iron  + (gathered["iron"]    ?? 0),
    gold:  resources.gold  + (gathered["gold"]    ?? 0),
  };

  await resourceRepository.applyTick(kingdomId, updated);
}

// ---------------------------------------------------------------------------
// Main entry point
// ---------------------------------------------------------------------------

export async function processMarchs(): Promise<void> {
  const now = new Date();

  const [gatherArrivals, monsterArrivals, gathers, returns_] = await Promise.all([
    processGatherArrivals(now),
    processMonsterAttackArrivals(now),
    processGatherEnd(now),
    processReturns(now),
  ]);

  const total = gatherArrivals + monsterArrivals + gathers + returns_;
  if (total > 0) {
    logger.info(
      { gatherArrivals, monsterArrivals, gathers, returns: returns_ },
      "march-processor: tick complete",
    );
  }
}
