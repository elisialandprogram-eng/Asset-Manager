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

// Correct MIME types for Unity WebGL assets.
// .unityweb files are gzip-compressed by Unity and the Unity JS loader
// decompresses them internally — do NOT set Content-Encoding here, or the
// browser will try to double-decompress them and corrupt the data.
app.use((req, res, next) => {
  const p = req.path;
  if (p.endsWith(".wasm.unityweb") || p.endsWith(".wasm")) {
    res.setHeader("Content-Type", "application/wasm");
  } else if (p.endsWith(".data.unityweb") || p.endsWith(".data")) {
    res.setHeader("Content-Type", "application/octet-stream");
  } else if (p.endsWith(".js.unityweb")) {
    res.setHeader("Content-Type", "application/javascript");
  }
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
