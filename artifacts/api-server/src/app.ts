import express, { type Express } from "express";
import pinoHttp from "pino-http";
import path from "path";
import { fileURLToPath } from "url";
import router from "./routes";
import { logger } from "./lib/logger";

const app: Express = express();

app.use(
  pinoHttp({
    logger,
    serializers: {
      req(req) {
        return {
          id: req.id,
          method: req.method,
          url: req.url?.split("?")[0],
        };
      },
      res(res) {
        return {
          statusCode: res.statusCode,
        };
      },
    },
  }),
);

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// API routes
app.use("/api", router);

// Serve built frontend static files
const distDir = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  "eternal-kingdoms",
  "dist",
  "public",
);

// Correct MIME types for Unity WebGL assets
app.use((req, res, next) => {
  if (req.path.endsWith(".wasm")) res.type("application/wasm");
  else if (req.path.endsWith(".data")) res.type("application/octet-stream");
  next();
});

app.use(express.static(distDir));

// Serve game assets from /assets path
const assetsDir = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  "..",
  "..",
  "assets",
);
app.use("/assets", express.static(assetsDir));

// SPA fallback — return index.html for any non-API route
app.use((_req, res) => {
  res.sendFile(path.join(distDir, "index.html"));
});

export default app;
