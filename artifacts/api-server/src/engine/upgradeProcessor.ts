import { upgradeQueueRepository, buildingRepository, kingdomRepository } from "@workspace/db";
import { computeKingdomPower } from "@workspace/game-engine";
import { logger } from "../lib/logger";
import type { BuildingType } from "@workspace/game-engine";

export async function processCompletedUpgrades(): Promise<void> {
  const now = new Date();

  try {
    const completed = await upgradeQueueRepository.findCompletedNow(now);

    if (completed.length === 0) return;

    for (const item of completed) {
      try {
        await buildingRepository.completeUpgrade(item.buildingId, item.toLevel);
        await upgradeQueueRepository.markCompleted(item.id);

        const buildings = await buildingRepository.findLevelSnapshotsByKingdomId(item.kingdomId);
        const power = computeKingdomPower(
          buildings.map((b) => ({
            buildingType: b.buildingType as BuildingType,
            level: b.level,
          })),
        );

        await kingdomRepository.updatePower(item.kingdomId, power);

        logger.info(
          { buildingId: item.buildingId, toLevel: item.toLevel, kingdomId: item.kingdomId },
          "upgrade-processor: upgrade completed",
        );
      } catch (err) {
        logger.error({ err, upgradeId: item.id }, "upgrade-processor: error applying upgrade");
      }
    }
  } catch (err) {
    logger.error({ err }, "upgrade-processor: fatal error");
  }
}
