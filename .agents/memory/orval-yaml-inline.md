---
name: Orval YAML inline schema fail
description: Orval 8.9.1 crashes on YAML flow-style inline schema objects — always use expanded block-style YAML.
---

## Rule
Never use YAML flow-style (inline) schema notation in `openapi.yaml`. Always use expanded block-style.

**Wrong (breaks Orval 8.9.1):**
```yaml
parameters:
  - name: kingdomId
    in: query
    schema: { type: integer }

properties:
  command:    { type: integer }
  rarity:     { type: string, enum: [common, uncommon, rare] }
  createdAt:  { type: string, format: date-time }
```

**Correct:**
```yaml
parameters:
  - name: kingdomId
    in: query
    schema:
      type: integer

properties:
  command:
    type: integer
  rarity:
    type: string
    enum: [common, uncommon, rare]
  createdAt:
    type: string
    format: date-time
```

**Why:** Orval 8.9.1 parses YAML using a custom walker that does `'propertyNames' in schema`. When schema properties are written as flow-style `{ type: integer }`, the walker receives a JavaScript object that resolves incorrectly, throwing `Cannot use 'in' operator to search for 'propertyNames' in integer }`. The existing (pre-Phase-4) paths in the spec always used expanded format and worked fine.

**How to apply:** Every time you add new paths or schemas to `openapi.yaml`, use expanded YAML throughout. If you ever add inline schemas accidentally, run the Python expansion script pattern from the Phase 4 session to batch-fix them, then re-run codegen.
