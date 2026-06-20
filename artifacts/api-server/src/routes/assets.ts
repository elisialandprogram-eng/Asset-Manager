import { Router } from "express";
import { db } from "@workspace/db";
import { assetRegistryTable } from "@workspace/db";
import { eq } from "drizzle-orm";
import { ListAssetsQueryParams } from "@workspace/api-zod";

const router = Router();

router.get("/", async (req, res) => {
  const parsed = ListAssetsQueryParams.safeParse(req.query);

  try {
    let assets;
    if (parsed.success && parsed.data.category) {
      assets = await db
        .select()
        .from(assetRegistryTable)
        .where(eq(assetRegistryTable.category, parsed.data.category as any));
    } else {
      assets = await db.select().from(assetRegistryTable);
    }

    res.json(assets);
  } catch (err) {
    req.log.error({ err }, "ListAssets error");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/:assetId", async (req, res) => {
  const assetId = req.params["assetId"];
  if (!assetId) {
    res.status(400).json({ error: "Invalid asset ID" });
    return;
  }

  try {
    const [asset] = await db
      .select()
      .from(assetRegistryTable)
      .where(eq(assetRegistryTable.assetId, assetId))
      .limit(1);

    if (!asset) {
      res.status(404).json({ error: "Asset not found" });
      return;
    }

    res.json(asset);
  } catch (err) {
    req.log.error({ err }, "GetAsset error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
