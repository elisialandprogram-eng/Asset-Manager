/**
 * spawnRepository.ts (spawn-system re-export)
 *
 * The canonical implementation lives in lib/db/src/repositories/spawnRepository.ts
 * and is exported from @workspace/db. This file re-exports it so other files
 * in the spawn-system module can use the clean local import pattern.
 */
export { spawnRepository } from "@workspace/db";
export type { WorldSpawn, InsertWorldSpawn } from "@workspace/db";
