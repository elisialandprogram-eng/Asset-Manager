---
name: Construction vs Upgrade pattern
description: How new-building construction differs from upgrade_queue in Eternal Kingdoms
---

**New building construction** (Phase 1.3–1.5) uses a separate `construction_queue` table, not `upgrade_queue`.

**Flow:**
1. POST /kingdoms/:id/construct → INSERT building (level=0, isConstructing=true) + INSERT construction_queue (in_progress)
2. processCompletedConstructions() runs on each 60s tick → sets building.level=1, isConstructing=false, queue.status=completed
3. Frontend filters level=0 buildings out of BuildingCard grid — they show only in ConstructionQueue component

**Why separate table:** upgrade_queue tracks level N→N+1 on existing buildings; construction_queue tracks the initial construction (level 0→1 of brand-new buildings). Keeps the two concerns cleanly separate and avoids ambiguity in the processor.

**Cost formula:** construction cost = computeUpgradeCost(type, 0) = base upgrade cost (scale^0 = 1). Duration = computeUpgradeDuration(type, 0) = base upgrade seconds. No separate balance constants needed.

**How to apply:** Any new building type added to BuildingType enum must also be added to CONSTRUCTABLE_BUILDING_TYPES in constructionCalculator.ts (palace is excluded — it's created on registration).
