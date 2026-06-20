# Eternal Kingdoms — World Architecture Bible

> **SPECIFICATION FREEZE DOCUMENT — IMMUTABLE REFERENCE ARCHITECTURE**
> Version 1.0 — June 2026

---

## 1. World Dimensions and Coordinate System

### World Grid

The Eternal Kingdoms world is a 2048 × 2048 tile grid.

```
World extent: 2048 tiles × 2048 tiles
Total tiles: 4,194,304 tiles
Coordinate origin: (0, 0) at northwest corner
Coordinate maximum: (2047, 2047) at southeast corner
```

### Backend Coordinate Space

The backend database stores all entity positions in a logical integer coordinate space spanning:

```
Backend space: 0–10,000 × 0–10,000
Mapping to tile space: tile_x = floor(backend_x / (10000 / 2048))
                        tile_z = floor(backend_z / (10000 / 2048))
Scale factor: 1 tile ≈ 4.88 backend units
```

This mapping is applied when converting stored kingdom positions (mapX, mapY) to tile coordinates for world rendering.

### Unity World Space

In Unity, tile coordinates are converted to world units:

```
TILE_SIZE = 5.0 Unity world units per tile
World extent = 2048 × 5.0 = 10,240 × 10,240 Unity world units
Unity origin: world center at (0, 0, 0)
Tile (tx, tz) → Unity (tx × 5.0 − 5120, 0, tz × 5.0 − 5120)
```

All game logic uses tile coordinates. Unity world units are a rendering concern only.

### Coordinate Ownership

Each tile in the 2048 × 2048 grid can be in one of the following ownership states:

| State | Description |
|-------|------------|
| `unoccupied` | No entity. Passable terrain. |
| `kingdom` | Owned by a kingdom. 2×2 tile footprint. Center tile stores kingdom ID. |
| `resource_node` | Spawned resource node. 1×1 tile. Node ID stored. |
| `monster_lair` | Permanent monster lair. 1×1 tile. Lair ID stored. |
| `shrine` | Fixed shrine location. 1×1 tile. Shrine ID stored. |
| `alliance_fortress` | Alliance-controlled fortress. 4×4 tile footprint. |
| `nft_land` | NFT Land plot boundary (32×32 tile block). |

---

## 2. Coordinate Mathematics

### Distance Formula

All game distance calculations use Euclidean distance in tile space:

```
distance(A, B) = sqrt((B.x − A.x)² + (B.z − A.z)²)
```

For march time calculations:
```
march_tiles_per_minute = BASE_MARCH_SPEED × speed_multipliers
travel_minutes = distance / march_tiles_per_minute
```

BASE_MARCH_SPEED = 2.0 tiles/minute (infantry, no buffs).

### Isometric Screen Projection (for 2D map fallback)

If a 2D isometric view is ever used:
```
HALF_W = TILE_SIZE_PX / 2   (half tile width in pixels)
HALF_H = TILE_SIZE_PX / 4   (half tile height, isometric)

screen_x = (tile_x − tile_z) × HALF_W × zoom + pan_x + canvas_cx
screen_y = (tile_x + tile_z) × HALF_H × zoom + pan_y + canvas_cy

Inverse (click to tile):
rel_x = (px − pan_x − canvas_cx) / (HALF_W × zoom)
rel_z = (py − pan_y − canvas_cy) / (HALF_H × zoom)
tile_x = (rel_x + rel_z) / 2
tile_z = (rel_z − rel_x) / 2
```

### Kingdom Placement Collision

Kingdom placement uses the following minimum separation rule:
```
separation(A, B) = sqrt((B.x − A.x)² + (B.z − A.z)²)
minimum_separation = 8 tiles (kingdoms may not be placed closer than 8 tiles)
```

The backend `generateKingdomPosition()` function enforces this at registration time using the stored `mapX`, `mapY` coordinates (in backend integer space).

---

## 3. Zone Architecture

The world is divided into 8 concentric zones based on distance from the world center (tile 1024, 1024).

### Zone Definitions

| Zone | Name | Tile Radius | Description |
|------|------|------------|-------------|
| 0 | Sanctum | 0–64 | Center of world. Central Shrine. Endgame territory. |
| 1 | Inner Reaches | 65–192 | High-tier resource nodes. Advanced monster lairs. |
| 2 | Mid Reaches | 193–384 | Mid-tier shrines. Contested resource zones. |
| 3 | Outer Mid | 385–576 | Standard combat zone. Mixed resource. |
| 4 | Frontier | 577–768 | Primary kingdom expansion zone. |
| 5 | Borderlands | 769–896 | Early-game zones. Protected newbie ring. |
| 6 | Edge | 897–1024 | Spawn zone for new kingdoms. Low PvP. |
| 7 | Periphery | 1025–1024 (corners) | Wild zone. No zone bonuses. Crystal deposits. |

### Zone Resource Density

Resource node density scales with zone:
```
Zone 0: Crystal nodes only (6 permanent, rare)
Zone 1: Gold, Iron, Crystal (dense)
Zone 2: Gold, Iron, Stone (moderate-dense)
Zone 3: Iron, Stone, Wood (moderate)
Zone 4: Stone, Wood, Food (standard)
Zone 5: Wood, Food (low density)
Zone 6: Food, Wood (sparse — starter resources)
Zone 7: Crystal deposits (unique zone perk)
```

### Zone Bonuses (when controlled by alliance)

Controlling all zone tiles within a zone grants the controlling alliance:
- Zone 1: +15% resource production for all members in zone
- Zone 2: +12% production + +5% troop attack
- Zone 3: +10% production + +5% march speed
- Zone 4: +8% production
- Zone 5–7: No zone bonus (early-game zones)

---

## 4. Chunk System

The world is divided into 32 × 32 chunks for streaming, computation, and fog-of-war management.

### Chunk Specification

```
Chunk size: 64 × 64 tiles
Total chunks: (2048 / 64) × (2048 / 64) = 32 × 32 = 1,024 chunks
Chunk ID: chunk_id = chunk_z × 32 + chunk_x
Chunk coordinates: (0,0) to (31,31)

Tile → Chunk:
chunk_x = floor(tile_x / 64)
chunk_z = floor(tile_z / 64)
local_x = tile_x mod 64
local_z = tile_z mod 64
```

### Chunk Metadata

Each chunk stores:
- Biome type (primary)
- Elevation range
- Entity occupancy bitmask (64×64 = 4096 bits = 512 bytes)
- Entity list (kingdom IDs, node IDs, lair IDs in chunk)
- Ownership state (which alliance controls the chunk, if any)

### Chunk Loading Strategy (Unity)

```
Load radius: 5 chunks (320 tiles) from camera center — render immediately
Preload ring: 2 additional chunk ring — preload during idle
Unload threshold: 9 chunks from camera center — stream out and destroy
```

Chunk geometry (terrain mesh) is generated procedurally from the world seed and chunk coordinates — no terrain data stored server-side.

### Server-Side Chunk Awareness

The server maintains a chunk index for fast spatial queries:
- "Which kingdoms are in chunk X?" — O(1) lookup via chunk entity index
- "Which resource nodes are active near tile T?" — radius search via chunk index
- March path collision checking uses chunk-level bounding box tests before tile-level resolution

---

## 5. Tile Occupancy

### Tile Occupancy Model

Each tile tracks:
```
tile {
  x: int                    -- tile X coordinate
  z: int                    -- tile Z coordinate
  tile_type: enum           -- terrain biome (plains, forest, hills, mountains, peaks, snow, water, crystal_zone)
  occupant_type: enum       -- unoccupied | kingdom | resource | monster | shrine | fortress | nft_land
  occupant_id: uuid | null  -- ID of the occupying entity
  elevation: float          -- 0.0–1.0 noise elevation value
  is_passable: bool         -- false for mountains/water
}
```

### Non-Passable Tiles

Tiles with elevation > 0.85 (high peaks, snow caps) are non-passable — marches may not route through them. The march routing algorithm uses A* pathfinding on the tile grid, avoiding non-passable tiles.

### Tile Terrain Generation

Tile terrain is generated entirely procedurally using the world seed:

```
elevation(tile_x, tile_z) = fbm(tile_x / SCALE, tile_z / SCALE, seed, 6 octaves)
moisture(tile_x, tile_z) = fbm(tile_x / SCALE_M, tile_z / SCALE_M, seed + 1000, 4 octaves)
crystal_noise(tile_x, tile_z) = fbm(tile_x / 100, tile_z / 100, seed + 5000, 3 octaves)

biome = classify(elevation, moisture, crystal_noise)
```

Biome classification table:

| Elevation | Moisture | Crystal | Biome |
|-----------|---------|---------|-------|
| <0.18 | Any | No | Plains |
| 0.18–0.32 | <0.5 | No | Light Forest |
| 0.18–0.32 | ≥0.5 | No | Deep Forest |
| 0.32–0.50 | Any | No | Hills |
| 0.50–0.65 | Any | No | Highland |
| 0.65–0.80 | Any | No | Mountains |
| 0.80–0.91 | Any | No | High Peaks |
| >0.91 | Any | No | Snow Caps |
| Any mid-range | Any | >0.83 | Crystal Zone |

---

## 6. Kingdom Footprint

### Kingdom Tile Reservation

A kingdom occupies a 2×2 tile footprint:
```
kingdom_origin = (kx, kz)           -- northwest corner of footprint
reserved tiles: (kx, kz), (kx+1, kz), (kx, kz+1), (kx+1, kz+1)
palace visual center: (kx + 0.5, kz + 0.5) -- between the four tiles
```

All four tiles are marked as occupant_type = `kingdom` with the same kingdom ID.

### Kingdom Placement Rules

1. The 2×2 footprint must not overlap any existing occupant.
2. Nearest neighbor separation: ≥8 tiles from nearest other kingdom (measured corner to corner).
3. Kingdoms must be placed on passable tiles (elevation < 0.85).
4. Kingdoms spawned in Zone 6 or 7 at season start.
5. Kingdoms cannot be placed in Zone 0 (Sanctum) except via Congress King power.

### Kingdom Visual Boundary

In the Unity client, the kingdom interior scene (Kingdom.unity) represents a fixed visual area — the 2×2 tile footprint on the world map translates to a visual kingdom of approximately 100 × 100 Unity world units (independent of tile scale).

---

## 7. Alliance Fortress Footprint

### Fortress Tile Reservation

An Alliance Fortress occupies a 4×4 tile footprint:
```
fortress_origin = (fx, fz)
reserved tiles: all (fx+i, fz+j) for i ∈ [0,3], j ∈ [0,3]
fortress center: (fx + 2, fz + 2)
```

### Fortress Placement Rules

1. Fortress must be placed in a zone the alliance currently controls (≥50% of zone tiles held).
2. Minimum separation from other fortresses: 16 tiles.
3. Fortresses cannot be placed in Zone 0 or Zone 6/7.
4. Only R5 or R4 can place a fortress.
5. Maximum one fortress per zone per alliance.

### Fortress Combat

Fortresses are defended by alliance garrison troops (each member can station up to march_capacity troops). Fortress walls have HP, defense rating, and trap capacity. Fortresses can only be captured via rally attack.

---

## 8. Shrine Footprint

Shrines are 1×1 tile fixed points. Shrine positions are seeded at world creation and never change.

### Shrine Distribution (8 per world)

| ID | Zone | Approximate Tile Position | Power |
|----|------|--------------------------|-------|
| Central Shrine | 0 | (1024, 1024) | Tier 3 (strongest) |
| Mid Shrine N | 1 | (1024, 700) | Tier 2 |
| Mid Shrine E | 1 | (1348, 1024) | Tier 2 |
| Mid Shrine S | 1 | (1024, 1348) | Tier 2 |
| Outer Shrine NW | 2 | (700, 700) | Tier 1 |
| Outer Shrine NE | 2 | (1348, 700) | Tier 1 |
| Outer Shrine SE | 2 | (1348, 1348) | Tier 1 |
| Outer Shrine SW | 2 | (700, 1348) | Tier 1 |

Exact positions use the world seed for minor offset within their zone — they are seeded once and stored in the `shrines` table.

---

## 9. NFT Land Boundaries

### Land Plot Grid

The world is divided into 4096 NFT Land plots arranged in a 64 × 64 plot grid.

```
Land plot size: 32 × 32 tiles
Total plots: 64 × 64 = 4,096
Plot coordinate: (plot_x, plot_z) ∈ [0, 63] × [0, 63]
Tile coverage: plot (px, pz) covers tiles (px×32, pz×32) to (px×32+31, pz×32+31)

Token ID formula:
token_id = plot_z × 64 + plot_x + 1  (1-indexed, range 1–4096)
```

### Land Ownership Mechanics

- NFT Land plots are ERC-721 tokens on Polygon.
- Owning a land plot grants the owner resource siphoning rights over all resource nodes spawned within the plot boundaries.
- Siphoning rate: 5% of all resources gathered from nodes within the plot are directed to the plot owner's treasury.
- Land owners receive crystal dividends from the blockchain dividend engine.
- Land ownership is visible on the world map as a plot boundary overlay.

### Land Plot Zones

```
Zone 0 (center, 2×2 plots = 4 plots): Sanctum — highest value, highest competition
Zone 1 (next ring, ~12 plots): Inner Reaches — premium plots
Zone 2–4: Standard competitive plots
Zone 5–7 (outer edge): Low-value plots — starter-area resource siphoning
```

---

## 10. Streaming Architecture

### Server-Side Streaming Contract

The server does not send the entire world state to the client. It streams entities within the client's visibility range.

**API streaming endpoints:**
- `GET /worlds/:id/map` — full world snapshot (used on initial load; heavy; 60s cache)
- `GET /worlds/:id/chunks?cx=&cz=&radius=` — chunk-level entity list (fast; per-frame or 5s cache)
- Socket.IO channel `world:chunk_update` — real-time entity changes within subscribed chunks

**Client visibility management:**
1. Client subscribes to chunks within its camera view radius.
2. Server pushes delta updates (entity spawn, despawn, state change) to subscribed chunks.
3. Client renders only entities within loaded chunks.
4. When camera moves, client unsubscribes from out-of-range chunks and subscribes to new chunks.

### Client-Side Terrain Streaming

Terrain geometry is never sent from the server. It is generated client-side from the deterministic noise function using the world seed:

```csharp
// Unity pseudo-code
void GenerateChunk(int cx, int cz, int worldSeed) {
    for (int lx = 0; lx < 64; lx++) {
        for (int lz = 0; lz < 64; lz++) {
            int tile_x = cx * 64 + lx;
            int tile_z = cz * 64 + lz;
            float elevation = FBM(tile_x / NOISE_SCALE, tile_z / NOISE_SCALE, worldSeed, 6);
            float moisture = FBM(tile_x / NOISE_SCALE_M, tile_z / NOISE_SCALE_M, worldSeed + 1000, 4);
            BiomeType biome = ClassifyBiome(elevation, moisture);
            mesh.SetTileAt(lx, lz, biome, elevation);
        }
    }
}
```

This ensures terrain is pixel-perfect consistent between all clients and the server spatial index.

---

## 11. Fog of War

### Fog of War Model

Fog of war limits the information a player sees on the world map to areas they have actively explored.

### Vision Radius

| Source | Vision Radius (tiles) |
|--------|----------------------|
| Kingdom (base) | 12 tiles |
| Kingdom + research (max) | 24 tiles |
| Scout march (during travel) | 8 tiles around march |
| Storm Eagle Dragoon | +8 tiles to all sources |
| Alliance territory tile | +6 tiles (shared vision among members) |

### Fog States

Each tile is in one of three states per player:

| State | Display |
|-------|---------|
| Unexplored | Dark overlay, no terrain visible |
| Explored (not in vision) | Dimmed terrain, no real-time entity updates |
| In Vision | Full brightness, real-time entity positions |

### Fog Persistence

Explored-but-not-in-vision tiles retain their last-known state (entities are shown at their last-seen position, with a timestamp indicator). This creates strategic information asymmetry.

### Alliance Vision Sharing

Alliance members share their vision ranges. Combined alliance vision coverage is the union of all member vision circles. This is computed per-player on the server from all ally kingdoms' positions.

---

## 12. Spawn Architecture

### Monster Lair Spawns

Monster lairs are permanent, fixed-coordinate entities seeded at world creation. Monsters respawn at their lair after a cooldown period (see Game Design Bible, Section 12). Lair positions are stored in `monster_spawns` table.

### Resource Node Spawns

Resource nodes spawn dynamically according to the Daily Regeneration cycle.

**Spawn rules:**
1. Each biome tile has a spawn probability based on its zone and biome type.
2. Nodes spawn randomly each day during the 04:00–05:00 UTC maintenance window.
3. Maximum concurrent nodes per chunk: 4 nodes per 64×64 tile area.
4. Node spawn position within chunk is seeded by (world_seed + day_number + chunk_id).

**Node spawn table by biome:**

| Biome | Possible Nodes |
|-------|---------------|
| Plains | Grain Field (70%), Lumber Stand (30%) |
| Light Forest | Lumber Stand (60%), Grain Field (40%) |
| Deep Forest | Lumber Stand (80%), Stone Deposit (20%) |
| Hills | Stone Deposit (50%), Iron Vein (30%), Lumber (20%) |
| Highland | Iron Vein (60%), Stone Deposit (40%) |
| Mountains | Gold Deposit (40%), Iron Vein (60%) |
| Crystal Zone | Crystal Node (100%) |

---

## 13. Node Lifecycle

### Full Node Lifecycle State Machine

```
[Dormant] → [Spawning] → [Active] → [Depleting] → [Depleted] → [Despawning] → [Dormant]
```

**State transitions:**

| From | To | Trigger |
|------|----|---------|
| Dormant | Spawning | Daily regen cycle selects this tile |
| Spawning | Active | Server inserts node record, notifies chunk subscribers |
| Active | Depleting | First gather march arrives at node |
| Depleting | Active | All gather marches depart (node has remaining capacity) |
| Depleting | Depleted | Node capacity reaches 0 |
| Depleted | Despawning | Timer expires (15 minutes after depletion) |
| Despawning | Dormant | Server deletes node record, notifies chunk subscribers |

### Node Capacity by Type

| Node Type | Base Capacity (resources) | Regen Rate |
|-----------|--------------------------|-----------|
| Grain Field | 50,000 food | N/A (static capacity) |
| Lumber Stand | 50,000 wood | N/A |
| Stone Deposit | 80,000 stone | N/A |
| Iron Vein | 40,000 iron | N/A |
| Gold Deposit | 20,000 gold | N/A |
| Crystal Node | 5,000 crystal | N/A |

Capacity scales with zone:
```
zone_capacity_multiplier = 1.0 + (4 - zone_index) × 0.15  (for zones 0–4)
```

### Daily Regeneration

At 04:00 UTC daily, the server's node regeneration job:
1. Despawns all depleted nodes older than 15 minutes (cleanup pass).
2. Calculates available spawn slots per chunk (max 4 minus active nodes).
3. For each available slot, rolls node type based on biome probability.
4. Inserts node records into `crystal_nodes` or creates spawn records.
5. Broadcasts spawn events to all subscribed clients in affected chunks.

---

## 14. Biome Architecture

Biomes determine visual appearance, terrain passability, resource node types, and monster lair tier ranges.

### Biome Definitions

| Biome | Elevation | Visual Theme | March Modifier | Monster Tiers | Node Types |
|-------|-----------|-------------|---------------|--------------|-----------|
| Plains | 0.00–0.18 | Open grassland | 1.00× | 1–2 | Food, Wood |
| Light Forest | 0.18–0.25 | Scattered trees | 0.90× | 1–2 | Wood, Food |
| Deep Forest | 0.25–0.32 | Dense canopy | 0.85× | 1–3 | Wood |
| Hills | 0.32–0.50 | Rolling hills | 0.80× | 2–3 | Stone, Wood |
| Highland | 0.50–0.65 | Rocky plateau | 0.75× | 3–4 | Stone, Iron |
| Mountains | 0.65–0.80 | Steep mountains | 0.60× | 4–5 | Iron, Gold |
| High Peaks | 0.80–0.91 | Bare rock | 0.50× (slow) | 5 only | Gold |
| Snow Caps | >0.91 | Snow-covered | 0.45× | 5 only | None |
| Crystal Zone | Any (mid) | Purple shimmer | Special | Special | Crystal |

### Crystal Zone Rules

Crystal zones ignore the elevation marching modifier (crystal hunters can reach them at full speed). This is intentional — crystal nodes are the economy's most valuable resource, and access should be competitive, not terrain-gated.

### Terrain Passability

Non-passable tiles (Snow Caps with elevation > 0.91 and water tiles in future water biomes) block march routing. A\* pathfinding routes around them. Players cannot build kingdoms or place fortresses on non-passable tiles.

---

## 15. Future Multi-World Architecture

### Multi-World Design Principles

The backend schema is world-scoped from Day 1. Every kingdom, resource node, monster spawn, and crystal node belongs to a `world_id`. This allows:
- 20+ concurrent parallel worlds per server region
- Independent seasonal resets per world
- Cross-world skirmish events (declared by the season King)

### World Types

| Type | Description |
|------|------------|
| `main` | Standard seasonal world. Primary play environment. |
| `battle` | Temporary cross-world battle arena. 1 week duration. No buildings. |
| `practice` | Non-competitive sandbox. No season rewards. |

### Cross-World Architecture

Cross-world events require a dedicated `battle` world:
1. Season King declares a Cross-World War.
2. A `battle` world is instantiated with a condensed 512×512 grid.
3. Top alliances from each participating world are invited to field armies.
4. Troops deployed to the battle world are cloned (originals remain at home).
5. Battle world runs for 7 days, then closes with rankings published.
6. No resources cross world boundaries — only glory and NFT rewards.

### World Seed Determinism

All worlds use a deterministic seed system. Given a `world_id` and `seed`, the entire terrain, shrine positions, and initial monster lair positions are deterministically reproducible. This allows:
- Offline validation of terrain data
- Instant world regeneration for testing
- Client-side terrain generation without server dependency

*Current world: "Aethoria" — id=1, seed=42937*

---

*This World Architecture Bible defines the authoritative spatial and structural design for Eternal Kingdoms. All world generation, streaming, and occupancy logic must conform to this specification.*
