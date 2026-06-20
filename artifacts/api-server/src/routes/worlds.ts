import { Router } from "express";
import { worldRepository } from "@workspace/db";

const router = Router();

router.get("/", async (req, res) => {
  try {
    const worlds = await worldRepository.findAll();
    res.json(worlds);
  } catch (err) {
    req.log.error({ err }, "ListWorlds error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
