#!/usr/bin/env bash
set -e

echo "==> Pushing database schema..."
pnpm --filter @workspace/db run push-force

echo "==> Building frontend..."
pnpm --filter @workspace/eternal-kingdoms exec vite build --config vite.config.ts

echo "==> Starting server on port 5000..."
PORT=5000 pnpm --filter @workspace/api-server run dev
