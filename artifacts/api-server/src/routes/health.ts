import { Router, type IRouter } from "express";
import { HealthCheckResponse } from "@workspace/api-zod";
import { getDatabaseProviderInstance } from "@workspace/db";

const router: IRouter = Router();

router.get("/healthz", (_req, res) => {
  const data = HealthCheckResponse.parse({ status: "ok" });
  res.json(data);
});

router.get("/health/database", async (req, res) => {
  try {
    const provider = getDatabaseProviderInstance();
    const health = await provider.healthCheck();
    const statusCode = health.status === "ok" ? 200 : 503;
    res.status(statusCode).json(health);
  } catch (err) {
    req.log?.error({ err }, "Database health check error");
    res.status(503).json({
      provider: "unknown",
      status: "error",
      latencyMs: 0,
      environment: process.env.APP_ENV ?? "development",
      error: err instanceof Error ? err.message : "Unknown error",
    });
  }
});

export default router;
