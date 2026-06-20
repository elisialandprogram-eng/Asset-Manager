import app from "./app";
import { logger } from "./lib/logger";
import { startResourceTick } from "./engine/resourceTick";
import { processCompletedUpgrades } from "./engine/upgradeProcessor";
import { runWorldSeed } from "./engine/worldSeeder";
import { startSpawnScheduler } from "./modules/spawn-system";
import { TICK_INTERVAL_MS } from "@workspace/game-engine";

// Single-server: one port serves API + static frontend
// Supports NODE_ENV, PORT, DATABASE_URL, SESSION_SECRET
// SUPABASE_URL and SUPABASE_ANON_KEY are reserved for future Supabase migration
const rawPort = process.env["PORT"] ?? "3000";
const port = Number(rawPort);

if (Number.isNaN(port) || port <= 0) {
  throw new Error(`Invalid PORT value: "${rawPort}"`);
}

app.listen(port, "0.0.0.0", (err) => {
  if (err) {
    logger.error({ err }, "Error listening on port");
    process.exit(1);
  }

  logger.info({ port }, "Server listening");

  // Seed world, dev account, and static data once on startup
  void runWorldSeed();

  // Resource production + upgrade/construction completion (every 60s)
  startResourceTick();
  setInterval(() => void processCompletedUpgrades(), TICK_INTERVAL_MS);
  void processCompletedUpgrades();

  // Dynamic spawn lifecycle (every 5 min, configurable in config/world-spawns.json)
  // Runs an immediate first cycle so world_spawns is populated before frontend arrives
  startSpawnScheduler();
});
