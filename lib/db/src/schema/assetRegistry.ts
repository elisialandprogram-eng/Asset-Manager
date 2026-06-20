import { pgTable, text, integer, timestamp, boolean, jsonb, pgEnum } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const assetCategoryEnum = pgEnum("asset_category", [
  "building",
  "troop",
  "monster",
  "dragon",
  "skin",
  "terrain",
  "ui",
]);

export const assetRegistryTable = pgTable("asset_registry", {
  assetId: text("asset_id").primaryKey(),
  category: assetCategoryEnum("category").notNull(),
  name: text("name").notNull(),
  variant: text("variant").notNull().default("default"),
  version: integer("version").notNull().default(1),
  placeholder: boolean("placeholder").notNull().default(true),
  imageUrl: text("image_url"),
  nftContractAddress: text("nft_contract_address"),
  nftTokenId: text("nft_token_id"),
  metadata: jsonb("metadata").default({}),
  createdAt: timestamp("created_at").notNull().defaultNow(),
  updatedAt: timestamp("updated_at").notNull().defaultNow(),
});

export const insertAssetRegistrySchema = createInsertSchema(assetRegistryTable).omit({ createdAt: true, updatedAt: true });
export type InsertAssetRegistry = z.infer<typeof insertAssetRegistrySchema>;
export type AssetRegistry = typeof assetRegistryTable.$inferSelect;
