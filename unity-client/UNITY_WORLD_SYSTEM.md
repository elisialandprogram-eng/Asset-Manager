# Unity Client — World System

## World Grid Specification

| Parameter | Value |
|-----------|-------|
| World size | 2048 × 2048 tiles |
| Tile size | ~5 world units |
| Total world extent | ~10,240 × 10,240 world units |
| Chunk size | 64 × 64 tiles |
| Total chunks | 32 × 32 = 1,024 chunks |
| Backend coordinate space | 0–10,000 × 0–10,000 (integer units) |
| Coordinate mapping | backendX → unityX = backendX / 10,000 × worldExtent |

## Chunk Streaming

Chunks load and unload dynamically around the camera position.

```
Load radius:   5 chunks (320 tiles) around camera
Unload radius: 7 chunks — chunks beyond this are destroyed
Preload:       1 chunk ahead in camera movement direction
```

`ChunkLoader.cs` monitors camera position every frame and triggers async load/unload:

```csharp
IEnumerator LoadChunk(int cx, int cz)
{
    var terrainData = await GenerateTerrainChunk(cx, cz, worldSeed);
    var chunk = Instantiate(chunkPrefab);
    chunk.Initialize(cx, cz, terrainData);
    activeChunks[new Vector2Int(cx, cz)] = chunk;
}
```

## Terrain Generation

Terrain uses the same fBm noise algorithm as the existing backend `terrainGenerator.ts` (ported to `NoiseUtils.cs`) for visual consistency with stored kingdom positions.

| Noise Layer | Purpose |
|-------------|---------|
| fBm elevation (6 octaves) | Primary biome height |
| fBm moisture | Forest vs plains blend |
| Crystal noise (separate seed) | Crystal zone overlay |

| Elevation Range | Biome |
|----------------|-------|
| 0.00–0.18 | Plains (light) |
| 0.18–0.32 | Plains / Forest |
| 0.32–0.50 | Deep Forest / Hills |
| 0.50–0.65 | Highland / Rock |
| 0.65–0.80 | Mountains |
| 0.80–0.91 | High Peaks |
| 0.91–1.00 | Snow Caps |
| crystalNoise > 0.83 | Crystal Zone (mid-elevation) |

## Camera System

### Camera Type: Fixed Isometric

```
Projection: Orthographic (or perspective with fixed angle — art style decision)
Pitch:      π/3.5 (~51°) — fixed, no tilt
Yaw:        45° — fixed, no rotation
```

### Controls

| Input | Action |
|-------|--------|
| Left drag / touch drag | Pan camera |
| Scroll wheel / pinch zoom | Zoom in/out |
| WASD | Pan camera (keyboard) |
| Click entity | Select + open info panel |
| Double-click / tap | Focus zoom to entity |

### Camera Bounds

Camera pans are clamped to world boundaries. Min/max zoom configured per platform (WebGL vs mobile).

## Entity Footprints

| Entity Type | Footprint | Notes |
|-------------|-----------|-------|
| Kingdom | 2 × 2 tiles | Center palace at tile origin |
| Resource node | 1 × 1 tile | Farm, lumber, quarry, iron, gold |
| Monster spawn | 1 × 1 tile | Lairs of any type |
| Alliance structure (future) | 4 × 4 tiles | Fortress / wonders |

## Entity Placement Rules (from existing backend seed)

| Separation Rule | Min Distance |
|----------------|-------------|
| Kingdom ↔ kingdom | ≥ 400 backend units |
| Resource ↔ resource | ≥ 4 tiles |
| Monster ↔ monster | ≥ 4 tiles |
| Crystal ↔ crystal | ≥ 6 tiles |

## World API Data Sources

| Data | API Endpoint | Refresh |
|------|-------------|---------|
| World info | `GET /api/worlds` | On scene load |
| Full world map | `GET /api/worlds/:id/map` | 60s poll |
| Active spawns | `GET /api/worlds/:id/spawns` | 30s poll |
| Kingdom list | `GET /api/worlds/:id/kingdoms` | 60s poll |

## Entity Visual Tiers (Kingdoms)

| Power Range | Tier Label | Visual |
|------------|-----------|--------|
| < 100 | Village | Small wooden hall |
| 100–499 | Town | Stone keep |
| 500–1,999 | Castle | Full castle with walls |
| ≥ 2,000 | Capital | Fortified city |
