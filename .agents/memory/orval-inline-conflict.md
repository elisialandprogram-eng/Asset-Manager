---
name: Orval inline object TS2308 conflict
description: How inline objects in POST response schemas cause TS2308 name collisions in api-zod
---

**Problem:** When a POST endpoint response schema contains an inline nested object, Orval v8.9.1 generates:
1. A Zod const named `{OperationId}Response` in `./generated/api.ts` (a value export)
2. A TypeScript interface named `{SchemaName}` in `./generated/types/{schemaName}.ts` (a type export)

If the schema name matches the operationId-derived name (e.g., schema `PlaceKingdomResponse` + operationId `placeKingdom` → Orval generates `PlaceKingdomResponse` in both places), TypeScript raises TS2308 when `index.ts` does `export * from both`.

**Why:** Orval generates Zod response validators for POST routes when the response schema contains inline (non-ref) sub-objects. Schemas that only use `$ref` sub-properties don't trigger this (e.g., `ConstructionResponse` only uses `$ref` properties and does NOT appear in `api.ts`).

**Two fixes:**
1. **Preferred:** Change `operationId` so Orval's auto-generated name diverges from the schema name. Example: `operationId: devPlaceKingdom` → Orval generates `DevPlaceKingdomResponse` (Zod) while schema stays `PlaceKingdomResponse` (TS interface). No conflict.
2. **Alternative:** Extract all inline objects in the response schema to named `$ref` schemas. This removes the trigger for Orval to generate a Zod response const.

**How to apply:** Whenever adding a new POST endpoint to openapi.yaml — check if the response schema has inline sub-objects. If yes, either use a non-matching operationId or use $refs throughout.
