# Unity Client — Kingdom System

## Overview

The kingdom view is a separate Unity scene (`Kingdom.unity`) loaded when a player enters their own kingdom. It renders a fixed-layout isometric 3D kingdom interior.

## Fixed Building Node Layout

Kingdoms use **fixed building slots** — no procedural placement, no random layouts. Every kingdom has identical slot positions. Buildings are rendered based on what the player has constructed via the API.

### Layout Rings

```
Kingdom Layout (top-down schematic):

                        [Palace]
                       /   |   \
              [Academy] [Treasury] [Warehouse] [Alliance Center]
             /        \          /             \
   [Barracks] [Stable] [Archery Range] [Siege Workshop]
  /      |      |      |       |       |       |      \
[Farm][Farm][Lumber][Lumber][Quarry][Quarry][Iron][Gold]
```

### Center Ring

| Slot | Building |
|------|----------|
| CENTER_0 | Palace (always present) |

### Inner Ring (4 slots)

| Slot | Building Type |
|------|--------------|
| INNER_0 | Academy |
| INNER_1 | Treasury |
| INNER_2 | Warehouse |
| INNER_3 | Alliance Center |

### Military Ring (4 slots)

| Slot | Building Type |
|------|--------------|
| MILITARY_0 | Barracks |
| MILITARY_1 | Stable |
| MILITARY_2 | Archery Range |
| MILITARY_3 | Siege Workshop |

### Resource/Outer Ring (up to 8 slots)

| Slot | Building Types Allowed |
|------|----------------------|
| OUTER_0–7 | Farm, Lumber Mill, Quarry, Iron Mine, Gold Mine |

Outer ring slots are unlocked by Palace level (controlled by `palaceRules.ts` / `PALACE_TIERS` on the backend). The Unity client reads slot availability from `GET /kingdoms/:id/state → palaceTier.maxResourceBuildings`.

## Building State Model

Each building slot renders one of:

| State | Visual |
|-------|--------|
| Empty (not built) | Empty ground plot with crosshair marker |
| Under construction | Building at ~30% completion + scaffolding overlay + timer |
| Level 1–20 | Full building model with level-appropriate upgrades |
| Upgrading | Building with animated scaffold overlay + timer |

## Click Interactions

| Click Target | Action |
|-------------|--------|
| Built building (level > 0) | Open Upgrade dialog |
| Empty plot (slot unlocked) | Open Construct dialog |
| Empty plot (slot locked) | Show "Palace upgrade required" tooltip |
| Under-construction building | Show construction timer info |

## Data Flow

```
KingdomManager.LoadKingdom(kingdomId)
  └── GET /api/kingdoms/:id/state
       ├── state.buildings     → BuildingSlot[].Initialize()
       ├── state.resources     → ResourceHUD.Update()
       ├── state.palaceTier    → SlotController.SetUnlockedSlots()
       └── state.upgradeQueue  → UpgradeTimerOverlay.Set()

Player clicks "Upgrade":
  └── POST /api/buildings/:id/upgrade
       └── On success → GET /api/kingdoms/:id/state (refresh)

Player clicks "Construct":
  └── POST /api/kingdoms/:id/construct { buildingType }
       └── On success → GET /api/kingdoms/:id/state (refresh)
```

## Palace Tier Rules

| Tier | Palace Level | Max Resource Buildings | Unlocked Features |
|------|-------------|----------------------|-------------------|
| I | 1 | 2 | Basic resources |
| II | 3 | 4 | Military buildings |
| III | 5 | 6 | Academy, Treasury |
| IV | 8 | 8 | Alliance Center |
| V | 12 | 10 | Siege Workshop, advanced combat |

Source: `lib/game-engine/src/gameBalance.ts → PALACE_TIERS`

## Kingdom Scene Camera

```
Type:    Fixed isometric (same angle as world map)
Pitch:   ~51° fixed
Yaw:     45° fixed
Pan:     Drag to pan within kingdom bounds
Zoom:    Scroll/pinch, clamped to kingdom extents
Rotate:  Disabled (fixed axis)
```
