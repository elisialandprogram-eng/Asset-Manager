---
name: Phase 5.8 free asset population
description: Actual packs downloaded, real file paths, import validation results, CONDITIONAL GO status
---

## Discovered Packs (real filesystem scan 2026-06-20)

Root: `unity-client/Assets/Thirdparty/` — lowercase 'p'. FreeAssetImporter v2 auto-resolves both spellings.

| Pack | Path | Files |
|------|------|-------|
| Medieval Village Pack | `Assets/Thirdparty/Medieval Village Pack/` | Buildings/FBX/ (10) + Props/FBX/ (31) — no textures |
| Nature Pack | `Assets/Thirdparty/Nature Pack/` | `FBX (Unity)/` (62) + `Textures/` (7 PNG) |
| Ultimate Monsters | `Assets/Thirdparty/Ultimate Monsters/` | `Big/FBX/` (16) + `Blob/FBX/` (17) + Atlas_Monsters.png |

## Missing Packs (still ProcGen)
- Quaternius Animated Characters → 5 character slots = blue biped stubs
- Unity Particle Pack → 15 VFX = magenta billboard stubs
- OpenGameArt audio → 16 audio = silent AudioClip
- Mixamo animations → AnimatorController stubs wired, no clips
- Quaternius Dungeon Kit, Farm Kit, Crystals → ruins/farm/crystal slots substituted

## Coverage Result
- Real-asset Addressable keys: **79 / 190**
- ProcGen fallback keys: **95 / 190**
- Silent audio keys: **16 / 190**
- Total: **190 / 190 (100%)**
- Alpha Status: **⚡ CONDITIONAL GO**

## Key Mapping Decisions

**Why:**
- Bell_Tower.fbx → palace (tallest available) AND watchtower AND monument_obelisk (scaled 0.6)
- Blacksmith.fbx → forge (direct match)
- Sawmill.fbx → lumbermill (direct match)
- Inn.fbx → barracks AND embassy (largest communal building)
- Gazebo.fbx → market (open pavilion analogue)
- Dino.fbx → dragon_t4 AND dragon_t5 (scale 3.5 + red material)
- Dog.fbx → direwolf_t1/t2 (canine shape — closest wolf available)
- Rock_Medium_1-3.fbx → stone/iron/gold nodes (material-differentiated)
- Pebble_Round FBX → crystal nodes (emissive material; no crystal pack)
- Well.fbx → fountain_stone AND shrine_01
- RockPath_Square_Wide.fbx → road_segment_01
- Nature Pack `FBX (Unity)/` preferred over `FBX/` (Unity-optimised rigs)

## How to Apply
If packs are re-downloaded or new packs added: re-run `Tools > EK > Phase 5.8 — Import Free Assets`.
FreeAssetDatabase.json v2 has all real paths. FreeAssetImporter.cs v2 has complete 190-key mapping table.
