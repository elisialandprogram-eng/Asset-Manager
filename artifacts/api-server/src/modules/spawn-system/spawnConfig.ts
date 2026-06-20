/**
 * spawnConfig.ts — Loads and validates config/world-spawns.json.
 *
 * The config file lives at the workspace root (`config/world-spawns.json`).
 * We search a small set of candidate paths because `process.cwd()` differs
 * between dev (api-server dir) and production (workspace root or similar).
 *
 * Non-coders can edit world-spawns.json and restart the server to
 * change spawn rates, caps, and level ranges without touching code.
 */

import { readFileSync } from "fs";
import { join } from "path";
import type { SpawnConfig, SpawnTypeConfig } from "./spawnTypes";

// ---------------------------------------------------------------------------
// Defaults (used as fallback if a field is missing in the JSON)
// ---------------------------------------------------------------------------

const DEFAULT_TYPE_CONFIG: SpawnTypeConfig = {
  enabled: false,
  respawn_hours: 6,
  expiry_hours: 12,
  max_world_spawns: 50,
  min_level: 1,
  max_level: 5,
};

const DEFAULT_CONFIG: SpawnConfig = {
  scheduler_interval_minutes: 5,
  daily_reset_hour_utc: 0,
  resource_spawns: {},
  monster_spawns: {},
};

// ---------------------------------------------------------------------------
// Loader
// ---------------------------------------------------------------------------

/**
 * Tries several candidate paths for the config file.
 * Returns the first path that can be read, or null if none are found.
 *
 * Candidate order:
 *   1. {cwd}/config/world-spawns.json          → production / workspace root
 *   2. {cwd}/../../config/world-spawns.json    → dev server in artifacts/api-server
 *   3. {cwd}/../../../config/world-spawns.json → extra depth just in case
 */
function tryReadConfig(): string | null {
  const candidates = [
    join(process.cwd(), "config", "world-spawns.json"),
    join(process.cwd(), "..", "..", "config", "world-spawns.json"),
    join(process.cwd(), "..", "..", "..", "config", "world-spawns.json"),
  ];

  for (const p of candidates) {
    try {
      return readFileSync(p, "utf-8");
    } catch {
      // try next candidate
    }
  }
  return null;
}

function loadConfig(): SpawnConfig {
  const raw = tryReadConfig();

  if (!raw) {
    console.warn(
      "[SpawnConfig] Could not find config/world-spawns.json — using defaults.",
    );
    return DEFAULT_CONFIG;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<SpawnConfig>;

    const config: SpawnConfig = {
      scheduler_interval_minutes:
        parsed.scheduler_interval_minutes ?? DEFAULT_CONFIG.scheduler_interval_minutes,
      daily_reset_hour_utc:
        parsed.daily_reset_hour_utc ?? DEFAULT_CONFIG.daily_reset_hour_utc,
      resource_spawns: {},
      monster_spawns: {},
    };

    for (const [key, val] of Object.entries(parsed.resource_spawns ?? {})) {
      config.resource_spawns[key] = { ...DEFAULT_TYPE_CONFIG, ...val };
    }
    for (const [key, val] of Object.entries(parsed.monster_spawns ?? {})) {
      config.monster_spawns[key] = { ...DEFAULT_TYPE_CONFIG, ...val };
    }

    return config;
  } catch (err) {
    console.warn("[SpawnConfig] Failed to parse world-spawns.json — using defaults.", err);
    return DEFAULT_CONFIG;
  }
}

// Loaded once at startup — restart server to pick up config changes
export const spawnConfig: SpawnConfig = loadConfig();

/**
 * Returns the SpawnTypeConfig for a given subtype, regardless of whether
 * it is a resource or monster spawn.  Returns undefined if not found.
 */
export function getSpawnTypeConfig(subtype: string): SpawnTypeConfig | undefined {
  return (
    spawnConfig.resource_spawns[subtype] ??
    spawnConfig.monster_spawns[subtype]
  );
}

/**
 * Flattened list of all enabled spawn subtypes with their configs,
 * annotated with the top-level spawnType ("resource" | "monster").
 */
export function getAllEnabledSubtypes(): Array<{
  spawnType: "resource" | "monster";
  subtype: string;
  config: SpawnTypeConfig;
}> {
  const result: Array<{ spawnType: "resource" | "monster"; subtype: string; config: SpawnTypeConfig }> = [];

  for (const [subtype, cfg] of Object.entries(spawnConfig.resource_spawns)) {
    if (cfg.enabled) result.push({ spawnType: "resource", subtype, config: cfg });
  }
  for (const [subtype, cfg] of Object.entries(spawnConfig.monster_spawns)) {
    if (cfg.enabled) result.push({ spawnType: "monster", subtype, config: cfg });
  }

  return result;
}
