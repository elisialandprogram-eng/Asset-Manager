# Unity Client — Art Pipeline

## Visual Style

**Semi-realistic medieval fantasy.** Reference: League of Kingdoms, Rise of Kingdoms.

**NOT:**
- Cartoon or chibi art
- Clash of Clans style
- Card-based or tile-based art direction
- Stylised flat icons

## Asset Naming Convention

All assets must follow the existing Asset Registry ID pattern:

```
{category}_{name}_{version_padded}
```

Examples:
- `building_palace_001`
- `building_farm_001`
- `troop_militia_001`
- `monster_wolf_001`
- `dragon_fire_001`

These IDs are **permanent**. They bridge to future NFT contracts via `nft_token_id` in `asset_registry`. Never rename a registered ID.

## Asset Registry Integration

The backend `asset_registry` table (`GET /api/assets`) is the source of truth for all visual asset IDs. The Unity `AssetManager.cs` fetches this registry on boot and maps IDs to Addressable bundle keys.

## URP Materials

All materials use URP shaders:
- Terrain: `Universal Render Pipeline/Terrain/Lit`
- Buildings: `Universal Render Pipeline/Lit` (metallic workflow)
- Units/characters: `Universal Render Pipeline/Lit` with subsurface scattering
- UI: `Universal Render Pipeline/Unlit` sprites
- Effects: `Universal Render Pipeline/Particles/Unlit`

No built-in pipeline materials. No custom shader graph nodes unless documented here.

## Addressable Asset Groups

| Group | Contents | Load Strategy |
|-------|----------|--------------|
| `core` | Shared fonts, common UI atlas | Preload on startup |
| `world-terrain` | Biome tiles per terrain type | Load on world scene entry |
| `buildings` | Per-building-type model + texture bundles | Load on kingdom scene entry |
| `units` | Troop model bundles | Load on barracks open |
| `monsters` | Monster model bundles | Load on world spawn |
| `effects` | Particle system prefabs | Load on world scene entry |

## Art Handoff Requirements

For each building asset, deliver:
- `.fbx` model file (Y-up, Z-forward, 1 unit = 1 Unity unit)
- Albedo, Normal, Metallic-Smoothness texture maps (2048×2048 minimum)
- LOD levels: LOD0 (full), LOD1 (50% tri), LOD2 (25% tri)
- Isometric facing: building front faces south-east (camera default view)

## Existing SVG Placeholder Assets

The `assets/` directory in the repo root contains 33 placeholder SVGs organised by category. These are the canonical visual references for Unity art replacement:

```
assets/
├── buildings/   — 9 building types
├── troops/      — 4 troop types
├── monsters/    — 4 monster types
├── dragons/     — 1 dragon type
├── terrain/     — terrain tiles
└── ui/          — UI elements
```

Each placeholder SVG maps to an `asset_registry` row. Replace SVGs with Unity Addressable bundles without changing the registry IDs.
