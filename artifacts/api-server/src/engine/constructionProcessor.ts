import { constructionRepository, buildingRepository, kingdomRepository } from "@workspace/db";
import { computeKingdomPower } from "@workspace/game-engine";
import { logger } from "../lib/logger";
import type { BuildingType } from "@workspace/game-engine";

export async function processCompletedConstructions(): Promise<void> {
  const now = new Date();

  try {
    const completed = await constructionRepository.findCompletedNow(now);

    if (completed.length === 0) return;

    for (const item of completed) {
      try {
        await buildingRepository.completeConstruction(item.buildingId);
        await constructionRepository.markCompleted(item.id);

        const buildings = await buildingRepository.findActiveLevelSnapshotsByKingdomId(item.kingdomId);
        const power = computeKingdomPower(
          buildings.map((b) => ({
            buildingType: b.buildingType as BuildingType,
            level: b.level,
          })),
        );

        await kingdomRepository.updatePower(item.kingdomId, power);

        logger.info(
          { buildingId: item.buildingId, buildingType: item.buildingType, kingdomId: item.kingdomId },
          "construction-processor: construction completed",
        );
      } catch (err) {
        logger.error({ err, constructionId: item.id }, "construction-processor: error completing construction");
      }
    }
  } catch (err) {
    logger.error({ err }, "construction-processor: fatal error");
  }
}
