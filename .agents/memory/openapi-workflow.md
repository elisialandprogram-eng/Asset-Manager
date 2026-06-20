---
name: OpenAPI-first workflow
description: Gotchas when editing openapi.yaml and working with Express 5 route params
---

Always run `pnpm --filter @workspace/api-spec run codegen` after any openapi.yaml change. Codegen cleans the output folder, causing transient Vite pre-transform errors — restart the frontend workflow after codegen completes.

Express 5 `req.params` is typed as `string | string[]` (not plain `string`). Always cast with `String(req.params["paramName"])` before passing to parseInt or other string-only APIs.

**Why:** TS2345 errors at typecheck otherwise. This pattern is required in every route that reads URL params.
