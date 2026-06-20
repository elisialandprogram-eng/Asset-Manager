/**
 * reports.ts — Battle report endpoints.
 *
 * GET /api/reports?kingdomId=X          — List reports (paginated)
 * GET /api/reports/:id                  — Get a specific report
 */

import { Router } from "express";
import { requireAuth, type AuthRequest } from "../middlewares/auth";
import { battleReportRepository, kingdomRepository } from "@workspace/db";

const router = Router();

// ---------------------------------------------------------------------------
// GET /api/reports?kingdomId=X&limit=20&offset=0
// ---------------------------------------------------------------------------

router.get("/", requireAuth, async (req: AuthRequest, res) => {
  const kingdomId = Number(String(req.query["kingdomId"] ?? ""));
  const limit     = Math.min(Number(String(req.query["limit"]  ?? "20")), 100);
  const offset    = Number(String(req.query["offset"] ?? "0"));

  if (isNaN(kingdomId) || kingdomId <= 0) {
    res.status(400).json({ error: "kingdomId query parameter is required" });
    return;
  }

  const kingdom = await kingdomRepository.findById(kingdomId);
  if (!kingdom) {
    res.status(404).json({ error: "Kingdom not found" });
    return;
  }
  if (kingdom.userId !== req.user!.userId) {
    res.status(403).json({ error: "You do not own this kingdom" });
    return;
  }

  try {
    const reports = await battleReportRepository.findByKingdomId(
      kingdomId,
      limit,
      offset,
    );
    const total = await battleReportRepository.countByKingdomId(kingdomId);
    res.json({ reports, total, limit, offset });
  } catch (err) {
    req.log.error({ err }, "ListReports error");
    res.status(500).json({ error: "Internal server error" });
  }
});

// ---------------------------------------------------------------------------
// GET /api/reports/:id
// ---------------------------------------------------------------------------

router.get("/:id", requireAuth, async (req: AuthRequest, res) => {
  const reportId = Number(String(req.params["id"]));
  if (isNaN(reportId)) {
    res.status(400).json({ error: "Invalid report ID" });
    return;
  }

  try {
    const report = await battleReportRepository.findById(reportId);
    if (!report) {
      res.status(404).json({ error: "Battle report not found" });
      return;
    }

    const kingdom = await kingdomRepository.findById(report.attackerKingdomId);
    if (!kingdom || kingdom.userId !== req.user!.userId) {
      res.status(403).json({ error: "Forbidden" });
      return;
    }

    res.json({ report });
  } catch (err) {
    req.log.error({ err }, "GetReport error");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;
