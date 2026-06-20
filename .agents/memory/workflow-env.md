---
name: Workflow env vars
description: Required environment variables for the two Eternal Kingdoms workflows
---

Both workflows need env vars injected directly in the command string (not set externally).

- **API Server**: `PORT=8080 pnpm --filter @workspace/api-server run dev` — waitForPort: 8080, outputType: console
- **Frontend**: `PORT=8081 BASE_PATH=/ pnpm --filter @workspace/eternal-kingdoms run dev` — waitForPort: 8081, outputType: webview

**Why:** The vite.config.ts and api server index.ts both throw hard errors if PORT or BASE_PATH are absent. The .replit port mapping is 8080→8080 (API) and 8081→80 (frontend root). BASE_PATH=/ because externalPort 80 serves at root.
