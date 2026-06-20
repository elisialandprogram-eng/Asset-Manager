import { pgTable, serial, integer, timestamp, text, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const tileTypeEnum = pgEnum("tile_type", [
  "plains",
  "forest",
  "mountain",
  "water",
  "desert",
  "swamp",
  "crystal_field",
]);

export const mapTilesTable = pgTable("map_tiles", {
  id: serial("id").primaryKey(),
  worldId: integer("world_id").notNull(),
  x: integer("x").notNull(),
  y: integer("y").notNull(),
  tileType: tileTypeEnum("tile_type").notNull().default("plains"),
  occupiedByKingdomId: integer("occupied_by_kingdom_id"),
  terrainAssetId: text("terrain_asset_id"),
  createdAt: timestamp("created_at").notNull().defaultNow(),
});

export const insertMapTileSchema = createInsertSchema(mapTilesTable).omit({ id: true, createdAt: true });
export type InsertMapTile = z.infer<typeof insertMapTileSchema>;
export type MapTile = typeof mapTilesTable.$inferSelect;
