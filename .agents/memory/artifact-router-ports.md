---
name: Artifact-router port configuration
description: How Replit's artifact-router proxy works and what ports the services must use
---

## Rule
The artifact-router process owns port 5000 (it IS the proxy on that port). Application services must use different ports. Current working config:

- `artifacts/eternal-kingdoms` (kind: web, previewPath: /) → `localPort = 8080`, `run = "bash start.sh"`
- `artifacts/api-server` (kind: api, previewPath: /api) → `localPort = 8080`
- `start.sh` starts the unified Express server with `PORT=8080`

**Why:** If any artifact service sets `localPort = 5000`, it conflicts with the artifact-router itself, causing a persistent 502 on the external Replit dev URL even though localhost works fine. The error message is: "router listen port 5000 conflicts with artifact service localPort 5000".

**How to apply:** Never set `PORT=5000` or `localPort=5000` in any artifact.toml or start.sh. The artifact-router auto-claims port 5000. `waitForPort = 5000` in the workflow is correct (waits for the router, not the app).

## Editing artifact.toml
Cannot use direct file writes. Must:
1. Write updated content to `artifacts/<name>/.replit-artifact/artifact.edit.toml`
2. Call `verifyAndReplaceArtifactToml({ tempFilePath, artifactTomlPath })` with absolute paths
- Cannot change `kind` field (ARTIFACT_EDITING_ERROR)
- Cannot set `previewPath = "/"` on a non-web artifact if another artifact already owns it (DUPLICATE_PREVIEW_PATH)
- "Project" is a prohibited workflow name for `configureWorkflow`
