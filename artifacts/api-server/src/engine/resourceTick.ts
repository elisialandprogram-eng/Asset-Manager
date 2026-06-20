import { kingdomRepository, buildingRepository, resourceRepository } from "@workspace/db";
import { applyTick, type BuildingSnapshot, TICK_INTERVAL_MS } from "@workspace/game-engine";
import { logger } from "../lib/logger";
import { processCompletedConstructions } from "./constructionProcessor";
import { processMarchs } from "./marchProcessor";

async function runTick(): Promise<void> {
  const tickStart = Date.now();

  try {
    const kingdoms = await kingdomRepository.findAllActive();

    if (kingdoms.length === 0) return;

    let updated = 0;

    for (const kingdom of kingdoms) {
      try {
        const [buildings, resources] = await Promise.all([
          buildingRepository.findSnapshotsByKingdomId(kingdom.id),
          resourceRepository.findByKingdomId(kingdom.id),
        ]);

        if (buildings.length === 0 || !resources) continue;

        const snapshots: BuildingSnapshot[] = buildings.map((b) => ({
          buildingType: b.buildingType,
          level: b.level,
          isConstructing: b.isConstructing,
        }));

        const current = {
          food: resources.food,
          wood: resources.wood,
          stone: resources.stone,
          iron: resources.iron,
          gold: resources.gold,
        };

        const next = applyTick(current, snapshots);

        const changed =
          next.food !== current.food ||
          next.wood !== current.wood ||
          next.stone !== current.stone ||
          next.iron !== current.iron ||
          next.gold !== current.gold;

        if (changed) {
          await resourceRepository.applyTick(kingdom.id, next);
          updated++;
        }
      } catch (err) {
        logger.error({ err, kingdomId: kingdom.id }, "resource-tick: error updating kingdom");
      }
    }

    await processCompletedConstructions();
    await processMarchs();

    const elapsed = Date.now() - tickStart;
    logger.info(
      { kingdoms: kingdoms.length, updated, elapsedMs: elapsed },
      "resource-tick: complete",
    );
  } catch (err) {
    logger.error({ err }, "resource-tick: fatal error");
  }
}

let tickInterval: NodeJS.Timeout | null = null;

export function startResourceTick(): void {
  if (tickInterval) return;
  logger.info({ intervalMs: TICK_INTERVAL_MS }, "resource-tick: starting scheduler");
  void runTick();
  tickInterval = setInterval(() => void runTick(), TICK_INTERVAL_MS);
}

export function stopResourceTick(): void {
  if (tickInterval) {
    clearInterval(tickInterval);
    tickInterval = null;
    logger.info("resource-tick: stopped");
  }
}
