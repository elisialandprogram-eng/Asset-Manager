# Eternal Kingdoms — Game Design Bible

> **SPECIFICATION FREEZE DOCUMENT — IMMUTABLE REFERENCE ARCHITECTURE**
> No implementation may deviate from this specification without a formal governance update.
> Version 1.0 — June 2026

---

## 1. Vision

Eternal Kingdoms is a massively multiplayer online kingdom strategy game (MMO-KSG) set in a persistent, semi-realistic medieval fantasy world. It targets the intersection of deep strategy gameplay, guild-scale social dynamics, blockchain-native ownership, and long-term seasonal competition.

**Core Pillars:**

1. **Depth over accessibility** — Eternal Kingdoms rewards planning, coordination, and mastery. Decisions have permanent consequences. Construction takes real time. Armies can be destroyed.
2. **Social gravity** — The most powerful gameplay states (rallies, shrines, congress) are impossible to achieve alone. The game is inherently cooperative and competitive at the macro scale.
3. **Ownership without barriers** — NFT-based land and asset ownership is opt-in, not mandatory. The game is fully playable without a wallet. Blockchain ownership adds value; it never gatekeeps.
4. **Seasonal renewal** — Worlds follow structured seasons that reset the competitive landscape, preventing permanent lock-in by early or dominant players. Progress carries forward in meaningful ways.
5. **Semi-realistic aesthetic** — Art direction targets League of Kingdoms and Rise of Kingdoms visual quality: detailed isometric medieval fantasy, no chibi, no cartoon.

**NOT Eternal Kingdoms:**
- A Clash of Clans clone (no village layout freedom, no cartoon art)
- An idle game (meaningful decisions required continuously)
- A card game (no card mechanics)
- Pay-to-win at a game-breaking level (whale controls enforced)

**Target Player Archetypes:**
- **The Builder** — Optimizes production chains, constructs efficiently, manages resources.
- **The Warlord** — Trains armies, scouts enemies, coordinates attacks, leads rallies.
- **The Diplomat** — Manages alliance politics, negotiates NAPs, brokers coalitions.
- **The Speculator** — Acquires NFT land, extracts resources, participates in the economy.
- **The Seasonal Champion** — Pursues congress and king-of-the-hill endgame mechanics.

---

## 2. Core Gameplay Loop

The core loop operates at three time scales: **micro (minutes)**, **macro (hours/days)**, and **seasonal (weeks/months)**.

### Micro Loop (Minutes)
```
Collect resources → Queue construction/upgrade/training → Scout and gather → Participate in events
```
1. Player logs in and collects accumulated resources (bounded by warehouse capacity).
2. Reviews production rates, queues building upgrades or troop training.
3. Sends march to resource nodes on the world map (gathering loop).
4. Participates in active PvE hunts (AP loop) or alliance activities.
5. Returns; collects march loot; re-queues marches.

### Macro Loop (Hours/Days)
```
Advance palace tier → Unlock building categories → Research tech tree nodes → Expand military capacity
```
1. Palace upgrades gate all progression. Every palace level unlocks new building types, troop tiers, and research branches.
2. Building upgrades take hours to days at high levels — the primary time gate.
3. Research runs on a separate queue alongside construction.
4. Military capacity scales with barracks, stable, archery range, and siege workshop levels.

### Seasonal Loop (Weeks/Months)
```
Compete for zones → Capture shrines → Win congress → Crown king → Season ends → Carry-forward rewards
```
1. Alliances contest zone control across the world map.
2. Zone capture grants resource production bonuses and march speed improvements.
3. Shrine battles determine which alliances hold permanent zone buffs.
4. Congress is a structured multi-day political and military event deciding the season's king.
5. Season ends with a global ranking; top players and alliances receive permanent NFT rewards and carry-forward bonuses.

---

## 3. Palace Progression

The Palace is the master gate for all kingdom development. No other building exceeds the Palace level. Every Palace upgrade is the most expensive and time-consuming action a player can take.

### Palace Tiers

| Tier | Palace Level | Unlocks |
|------|-------------|---------|
| I | 1–2 | Farm, Lumber Mill, Quarry, Iron Mine, Gold Mine (basic production) |
| II | 3–4 | Barracks, Hospital, Academy (T1 troops, basic research) |
| III | 5–7 | Stable, Archery Range, Treasury, Warehouse expansion (T2 troops, expanded resources) |
| IV | 8–11 | Siege Workshop, Alliance Center, Shrine access (T3 troops, alliance features) |
| V | 12–15 | Dragon Roost, Research Lab (T4 troops, dragon binding, advanced research) |
| VI | 16–20 | Sovereign Gate, Congress participation (T5 troops, endgame access) |
| VII | 21–25 | Celestial Forge (Hero ascension, Dragoon breeding, NFT minting) |

### Palace Upgrade Rules
- Palace upgrades require all other buildings to be at or below Palace level − 1.
- Palace upgrades are blocked while any march is in flight from the kingdom.
- Palace upgrade materials (rare components) are obtained through research and events.
- Each Palace level multiplies construction queue capacity (+1 slot per 3 levels, max 5 slots at level 15).

---

## 4. Building Progression

### Building Categories

**Core (required):**
- Palace — master gate, kingdom power center
- Warehouse — resource protection cap (resources below this threshold cannot be plundered)
- Hospital — wounded troop recovery capacity

**Production:**
- Farm (max 5) — food production
- Lumber Mill (max 5) — wood production
- Quarry (max 4) — stone production
- Iron Mine (max 3) — iron production
- Gold Mine (max 2) — gold production

**Military:**
- Barracks (max 3) — infantry training (T1–T5)
- Stable (max 3) — cavalry training (T1–T5)
- Archery Range (max 3) — ranged training (T1–T5)
- Siege Workshop (max 2) — siege engines (T3–T5)

**Research & Development:**
- Academy — military and economic research tree
- Research Lab — advanced tech research (Palace Tier V+)

**Economy:**
- Treasury — crystal storage and exchange
- Market — inter-player trade (future phase)
- Alliance Center — alliance help, donations, and alliance technology

**Special:**
- Dragon Roost — dragon binding and leveling (Palace Tier V+)
- Sovereign Gate — season-end access (Palace Tier VI+)
- Celestial Forge — Hero and Dragoon operations (Palace Tier VII+)

### Building Upgrade Formula

```
Cost(level) = BASE_COST × 1.5^(level - 1)
Duration(level) = BASE_DURATION_SECONDS × 1.3^(level - 1)
```

Maximum level for all buildings is 25 (aligned to maximum Palace level).

### Construction vs Upgrade
- **New construction** uses the `construction_queue` table. A building starts at level 0 and completes to level 1.
- **Upgrade** uses the `upgrade_queue` table. A building goes from level N to N+1.
- Both queues run simultaneously (one construction + one upgrade per queue slot).
- Queue slots expand with Palace level: 2 at Tier I, 3 at Tier III, 4 at Tier V, 5 at Tier VII.

---

## 5. Resource Economy

### Resource Types

| Resource | Source | Primary Use |
|----------|--------|------------|
| Food | Farms, gathering | Troop upkeep, training |
| Wood | Lumber Mills, gathering | Construction, research |
| Stone | Quarries, gathering | Construction (high level) |
| Iron | Iron Mines, gathering | Troop training (high tier) |
| Gold | Gold Mines, quests | Premium building operations, treasury |
| Crystal | Crystal nodes, combat | Dragon binding, NFT operations, treasury |

### Production System

Resources are produced per tick (60-second server interval):
```
Production(resource, level) = BASE_RATE × level × BUILDING_COUNT
```

Production is bounded by the resource cap:
```
Cap(resource) = BASE_CAP + sum(LEVEL_CAP_BONUS × building_level) for all buildings of that type
```

Resources above the cap are wasted. Players must actively collect by sending marches, participating in events, or upgrading warehouse capacity.

### Warehouse Protection

The Warehouse determines how much of each resource is protected from plundering:
```
Protected(resource) = BASE_PROTECT + WAREHOUSE_LEVEL × PROTECT_PER_LEVEL
```

Resources above the protection cap are exposed to plunder when the city is attacked.

### Crystal Resource

Crystal is a special resource with different rules:
- Not produced by buildings — obtained only through gathering at crystal nodes, combat rewards, and event participation.
- Stored in the Treasury (separate capacity from standard resources).
- Spent on dragon binding, NFT minting operations, and high-tier research.
- A portion of every crystal transaction flows to the blockchain dividend engine (see Blockchain Bible).

### Resource Exchange

Players can exchange resources at a penalty rate:
```
Exchange rate: 10 of resource A → 7 of resource B (base rate, modified by research)
```

Alliance bank allows zero-penalty resource sharing between alliance members.

---

## 6. Research Trees

Research is conducted in the Academy (Tier I–IV) and Research Lab (Tier V+). Only one research can be active per queue (expandable to 2 with Research Lab).

### Economy Tree (Academy)
- **Production Efficiency I–X** — +2% per level to all resource production
- **Gathering Speed I–X** — +3% per level to march speed on gathering marches
- **Warehouse Expansion I–V** — +5% per level to protected resource cap
- **Construction Acceleration I–X** — −2% per level to all construction time

### Military Tree (Academy)
- **Infantry Training I–X** — +5% per level to infantry troop capacity
- **Cavalry Training I–X** — +5% per level to cavalry capacity
- **Archery Training I–X** — +5% per level to ranged capacity
- **March Speed I–X** — +3% per level to all march speeds
- **March Capacity I–X** — +5% per level to troop load in marches

### Defense Tree (Academy)
- **Wall Defense I–X** — +5% per level to wall defense rating
- **Trap Construction I–V** — +10% per level to trap effectiveness
- **Hospital Capacity I–X** — +5% per level to wounded recovery capacity
- **Morale Resilience I–V** — reduces morale loss in consecutive defenses

### Advanced Tree (Research Lab — Palace Tier V+)
- **T4 Troop Unlock** — prerequisite for T4 training
- **T5 Troop Unlock** — prerequisite for T5 training
- **Dragon Bond I–V** — increases dragon combat effectiveness
- **Crystal Efficiency I–V** — +5% per level to crystal gathering yield
- **Siege Mastery I–X** — +8% per level to siege engine attack

### Alliance Research (Alliance Center)
Each alliance has a shared research tree funded by member donations:
- **Alliance March Capacity I–X** — increases rally march capacity
- **Alliance Hospital I–V** — alliance-wide hospital bonus
- **Alliance Construction I–V** — construction speed bonus for all members
- **Fortress Defense I–X** — alliance fortress wall/trap buff

---

## 7. Time Gates

Time gates are the primary mechanism for progression pacing and anti-burnout design.

### Building Time Gates

| Palace Level | Example Building Duration |
|-------------|--------------------------|
| 1–5 | Minutes to hours |
| 6–10 | Hours to 1 day |
| 11–15 | 1–4 days |
| 16–20 | 4–10 days |
| 21–25 | 10–20 days |

### Speedup System

Speedups are consumable items that reduce construction, research, or training time:
- **1-minute speedup** — common, from daily login rewards
- **1-hour speedup** — uncommon, from events and quests
- **3-hour speedup** — rare, from PvE combat and alliance gifts
- **8-hour speedup** — very rare, from season rewards and special events
- **24-hour speedup** — epic, from season milestones and premium purchase
- **Universal speedup (8h)** — works for construction, research, or training
- **Training speedup (8h)** — training only

Speedups are the primary monetization lever. They are never available in quantities that would fully close the time gap between free and premium players at competitive tiers.

### Anti-Speedup-Spam Design

A single player spending unlimited speedups cannot exceed a maximum acceleration:
- Maximum speedup application: 72 hours per 7-day period per category (construction, research, training).
- This prevents single whales from building maxed kingdoms in days.

---

## 8. Speedups

Speedups are obtained through:
- **Daily Login** — 1-minute to 1-hour speedups
- **Quest Rewards** — event and story quests
- **PvE Monster Kills** — drop rate based on monster tier
- **Alliance Help** — each member help reduces timer by 1% (up to 20 helps per building)
- **Season Milestones** — large speedup packages at season event tiers
- **Premium Purchase** — direct purchase, subject to weekly cap enforcement

Alliance Help is a critical social mechanic: requesting help from alliance members grants both parties loyalty points, which feed the Alliance Research donation system.

---

## 9. March System

### March Definition

A march is a military order dispatching troops from a kingdom to a target location on the world map.

### March Types

| Type | Target | Purpose |
|------|--------|---------|
| Gather | Resource node | Collect resources |
| Attack | Enemy kingdom | Plunder and destroy |
| Scout | Any location | Intelligence gathering |
| Reinforce | Ally kingdom | Support defense |
| Rally | Rally point | Coordinated alliance attack |
| Return | Home kingdom | Abort march, return troops |

### March Speed Formula

```
Speed(march) = BASE_SPEED × (1 + march_speed_research%) × (1 + hero_speed_bonus%) × terrain_modifier
Travel_time = Distance / Speed
Distance = sqrt((x2-x1)² + (z2-z1)²) in world units
```

Terrain modifiers:
- Plains: 1.0×
- Forest: 0.85×
- Hills: 0.80×
- Mountains: 0.60×
- Road (future): 1.20×

### March Capacity

```
March_capacity = BASE_CAPACITY × (1 + march_capacity_research%) × (1 + hero_capacity_bonus%)
```

Only troops up to march capacity may be sent per march. Multiple marches can be dispatched simultaneously (max marches = palace level / 4, minimum 1).

### Gather March Rules

- A gather march carries empty load; troop count determines gather rate.
- Resources collected per hour = GATHER_RATE × march_troop_count × (1 + gather_speed_research%)
- Gather march auto-returns when loaded to march capacity.
- Gather marches are safe from attack while at an uncontested node.
- Contested nodes (in war zones) expose gather marches to attack.

### Scout March Rules

- Scouts return with intelligence: troop counts, building levels (approximate), resource quantities, defensive traps.
- Scout success depends on relative scouting power vs. defensive detection rating.
- Failed scouts return no data; the enemy is notified.

---

## 10. AP System (Action Points)

Action Points (AP) gate PvE combat frequency to prevent round-the-clock farming abuse.

### AP Mechanics
- Maximum AP pool: 200 (base), expandable to 300 with research.
- AP regenerates at 1 per 6 minutes (10 per hour).
- AP is consumed by attacking monsters on the world map:
  - Tier 1 monster: 6 AP
  - Tier 2 monster: 8 AP
  - Tier 3 monster: 12 AP
  - Tier 4 monster: 20 AP
  - Tier 5 monster: 40 AP

### AP Rewards from PvE
AP spending yields:
- Speedup items (construction, research, training)
- Crystal fragments (combine 10 to form 1 crystal)
- Equipment material fragments
- Hero experience shards
- Troop training resources

### AP Replenishment Items
- **AP Potion (Small)** — +20 AP
- **AP Potion (Medium)** — +50 AP
- **AP Potion (Full)** — +200 AP (max pool refill)

AP potions are earned through events, season milestones, and premium purchase.

---

## 11. Gathering Loop

### Node Types

| Node | Resource | Spawn Rate | Capacity |
|------|----------|-----------|---------|
| Grain Field | Food | Common | Low |
| Lumber Stand | Wood | Common | Low |
| Stone Deposit | Stone | Common | Medium |
| Iron Vein | Iron | Uncommon | Medium |
| Gold Deposit | Gold | Rare | High |
| Crystal Node | Crystal | Very Rare | Very High |

### Node Lifecycle
1. Node spawns at a tile on the world map (determined by world seed + daily RNG).
2. Node becomes visible to all players within fog-of-war range.
3. Players send gather marches to nodes.
4. Node depletes as marches collect. Capacity shared across all concurrent marchers.
5. Depleted node despawns after a cooldown (1–6 hours based on type).
6. Node respawns at same or adjacent tile at the next daily regeneration cycle.

### Gold and Crystal Nodes — Contested Gathering
Gold and crystal nodes are treated as contested zones. Any player may attack gather marches at these nodes. This creates high-risk, high-reward gameplay requiring alliance escort or rapid extraction.

---

## 12. PvE Loop

### Monster System

Monsters spawn at fixed lairs on the world map (persistent through the season). Lair locations are seeded at world creation.

### Monster Tiers

| Tier | Power | AP Cost | Drop Quality |
|------|-------|---------|-------------|
| 1 | 500–1,000 | 6 AP | Common |
| 2 | 2,000–5,000 | 8 AP | Uncommon |
| 3 | 10,000–25,000 | 12 AP | Rare |
| 4 | 60,000–100,000 | 20 AP | Epic |
| 5 | 250,000+ | 40 AP | Legendary |

### Monster Respawn

Monsters respawn at their lair after a cooldown:
- Tier 1: 30 minutes
- Tier 2: 1 hour
- Tier 3: 4 hours
- Tier 4: 12 hours
- Tier 5: 24 hours (requires full alliance rally to kill)

### Monster Drops

All drops are rolled on the server using the combat outcome seed. Drops are distributed to the attacking march owner. For rally attacks on T4/T5 monsters, drops are distributed proportionally to damage contribution.

---

## 13. Rally System

Rallies allow multiple alliance members to combine troops into a single overwhelming attack.

### Rally Creation
1. Any alliance member above Palace Tier II can open a rally.
2. Rally leader selects a target (enemy kingdom, monster, shrine, fortress).
3. Rally timer set (5 minutes to 1 hour; longer = more potential troops).
4. Alliance members send troops to the rally point.

### Rally Capacity
```
Rally_capacity = RALLY_LEADER_BASE × (1 + alliance_research_march_capacity%) × hero_bonus
```
Rally capacity scales with the rally leader's research and the Alliance March Capacity research level.

### Rally Combat
Rally troops fight as a unified force. The combat simulation resolves all participants' buffs, research, hero bonuses, and troop compositions into a single round-by-round battle.

Casualties are returned to each member's hospital proportionally to their contribution.

### Rally Against Shrines and Fortresses

Shrines and alliance fortresses require rally attacks — individual attacks deal reduced damage (10% of normal) and cannot capture them.

---

## 14. Hospital System

Hospitals prevent permanent troop loss during battle.

### Hospital Rules
- When a kingdom is attacked, a percentage of defeated troops go to the hospital (wounded) rather than dying.
- Hospital capacity = BASE_CAPACITY + HOSPITAL_LEVEL × CAPACITY_PER_LEVEL
- Troops above hospital capacity die permanently.
- Wounded troops heal over time (base: 10 troops per minute per Hospital level).
- Healing can be accelerated with speedups.

### Hospital Priority

Healing priority: Higher troop tiers heal first (T5 > T4 > T3 > T2 > T1). Players may override priority manually.

### Medical Speedups

- **Medical Speedup (1h)** — reduces healing time by 1 hour
- **Instant Heal** — immediately heals all wounded troops (rare premium item)

### Death vs. Wound Rate

```
Wound_rate = BASE_WOUND_RATE × (1 + hospital_research%) ← more wounds to hospital, fewer deaths
Death_rate = 1 - Wound_rate
```

At maximum hospital research, up to 95% of defeated troops become wounded rather than dying.

---

## 15. Alliance System

### Alliance Formation

Alliances are created by any player at Palace Tier II+. Alliances have:
- Name (unique per world)
- Tag (3–5 characters, unique)
- Emblem
- Description
- Maximum members (30 base, expandable to 50 with Alliance Center level)

### Alliance Hierarchy

| Rank | Title | Permissions |
|------|-------|------------|
| R5 | King/Leader | All permissions |
| R4 | Duke/Officer | Kick members, manage research, declare war |
| R3 | Knight/Elite | Rally lead, reinforce, access alliance bank |
| R2 | Soldier | Send alliance help, participate in rallies |
| R1 | Recruit | Basic participation, no permissions |

### Alliance Territory

Alliances capture territory by holding zone tiles for a sustained period. Territory ownership grants:
- Resource production bonus to all member kingdoms in the zone (+5–15% based on zone tier)
- March speed bonus across owned territory
- Reduced plunder loss for members attacked in owned territory

### Alliance Research

Alliance members donate resources to fund shared research (see Section 6 Alliance Research). Research speed scales with member donations.

### Alliance Bank

Members may deposit resources into the Alliance Bank. R4/R5 can distribute bank resources to members in need (emergency resupply for active war zones).

---

## 16. Shrine System

Shrines are fixed points of power on the world map that provide zone-wide bonuses to the controlling alliance.

### Shrine Locations

8 Shrines exist per world (4 outer, 3 mid, 1 central). Shrine power scales with proximity to the world center.

### Shrine Capture

1. Shrines are initially neutral. First capture requires a rally attack.
2. Once held, a Shrine generates a buff for the controlling alliance.
3. Shrines can be contested — enemy alliances rally-attack to capture.
4. Shrine defense requires active player presence or garrison troops.

### Shrine Buffs (per tier)

| Tier | Buff |
|------|------|
| Outer (4) | +5% resource production in zone, +5% march speed |
| Mid (3) | +10% rally capacity, +8% troop attack |
| Central (1) | +15% all combat stats, +10% server-wide rally capacity |

---

## 17. Congress System

Congress is the endgame political and military event that determines the season's King.

### Congress Triggers

Congress opens when an alliance controls 3+ Shrines simultaneously and has held them for 72 continuous hours.

### Congress Phases

**Phase 1 — Declaration (72 hours)**
Eligible alliances (any holding ≥1 Shrine) may declare candidacy for the Congress seat. Declarations require a resource investment (treasury burn).

**Phase 2 — Campaigns (5 days)**
Candidate alliances compete for total VP (victory points) earned through:
- Zone control (VP per hour per zone tile held)
- Monster kills across all member kingdoms
- PvP attack victories against opposing alliances

**Phase 3 — Battle for the Seat (24 hours)**
Top 2 VP leaders face off in a coordinated server-wide battle event. The winning alliance's leader is crowned King for the season.

### King Powers

The King's account receives:
- Server-wide 20% resource production bonus broadcast to all alliance members
- The ability to declare "Server War" — enabling inter-world skirmish events
- A permanent seasonal NFT crown token

---

## 18. Hero System

Heroes are named characters that provide leadership bonuses to marching troops.

### Hero Slots
Players unlock hero slots based on Palace level:
- Palace 5: 1 hero slot
- Palace 10: 2 hero slots
- Palace 15: 3 hero slots
- Palace 20: 4 hero slots
- Palace 25: 5 hero slots

### Hero Stats

| Stat | Effect |
|------|--------|
| Command | March capacity bonus |
| Attack | Troop attack multiplier |
| Defense | Troop defense multiplier |
| Speed | March speed bonus |
| Gathering | Gather rate bonus |

### Hero Leveling

Heroes gain experience from:
- Leading combat marches (PvE and PvP)
- Hero training sessions (resource cost)
- Alliance help events

Heroes level 1–60. Each 10 levels unlock a hero skill (passive or active). Active skills trigger once per battle phase.

### Hero Acquisition

- **Basic heroes** — from quests and early events (uncommon tier)
- **Rare heroes** — from season milestone rewards
- **Legendary heroes** — from season ranking rewards and NFT marketplace

### Hero NFTs

Tier 4 and 5 heroes can be minted as ERC-721 NFTs. NFT heroes retain their stats and skills when sold on the marketplace. See Blockchain Bible.

---

## 19. Dragoon System

Dragoons are creature companions bound to a kingdom — distinct from Heroes (which are humanoid leaders).

### Dragoon Binding

Dragoons are bound at the Dragon Roost (Palace Tier V+). Binding requires:
- Crystal investment (varies by dragoon tier)
- Dragon Roost level matching dragoon tier requirement
- A "Dragoon Egg" NFT or in-game event item

### Dragoon Types

| Type | Specialty | Bonus |
|------|-----------|-------|
| Fire Drake | Siege | +Siege attack, +Fortress destruction |
| Frost Wyvern | Cavalry | +Cavalry speed, +Freeze debuff |
| Storm Eagle | Scouting | +Scout range, +Fog reduction |
| Earth Titan | Defense | +Wall defense, +Trap power |
| Void Serpent | Plunder | +Resource plunder, +Hospital bypass |

### Dragoon Leveling

Dragoons level 1–30. Feed resources and crystal to level. At levels 10, 20, and 30, a dragoon evolves its art and gains an additional passive ability.

### Dragoon Combat

A dragoon bound to a kingdom accompanies all marches from that kingdom. The dragoon's bonus applies to the march's troop stats. Only one dragoon can be active per march.

### Dragoon Breeding

At Dragon Roost level 15+, two dragoons can be bred to produce a new egg. Offspring inherit traits from both parents (trait inheritance rules documented in Blockchain Bible — mutation system).

### Dragoon NFTs

All dragoons are ERC-721 NFTs. Stats and lineage are stored on-chain. Breeding events emit on-chain transactions.

---

## 20. Endgame Loop

The endgame loop sustains player engagement beyond maximum Palace level.

### Endgame Activities

1. **Sovereign Gate** — Daily dungeon-style event accessible at Palace 16+. Teams of 5 alliance members battle through wave encounters for loot and crystal.
2. **Ancient Dragon Hunts** — Server-wide alliance rallies against Ancient Dragons (Tier 6 PvE). These spawn once per week and require coordination across multiple alliances.
3. **Territory Wars** — Continuous zone combat; endgame kingdoms operate as defensive anchors for alliance territory.
4. **NFT Crafting** — Celestial Forge allows combining materials into NFT equipment for heroes and dragoons.
5. **Seasonal Ranking** — Leaderboard competition across power, kills, crystal gathered, and monster kills.

### Power Ceiling Prevention

Once all buildings hit max level, the progression loop continues through:
- Hero experience and skill advancement
- Dragoon leveling and evolution
- NFT equipment crafting and upgrading
- Alliance territory competition
- Crystal economy participation

---

## 21. Seasonal Cycles

### Season Structure

A Season lasts approximately 90 days.

**Days 1–30 (Expansion Phase)**
- New world initializes with fresh spawn
- Kingdoms develop palace, buildings, and troops
- Early aggression is moderated by early-game protections (shield: 72 hours)
- Alliances form and settle territory

**Days 31–60 (War Phase)**
- Shields expire on lower-level kingdoms
- Zone warfare begins in earnest
- Shrine contests begin
- Monster hunting and crystal economy active

**Days 61–85 (Endgame Phase)**
- Congress events run
- Ancient Dragon spawns
- Sovereign Gate fully active
- NFT crafting reaches peak viability

**Days 86–90 (Closing Ceremony)**
- All combat suspended in final 24 hours
- Final rankings locked
- Season NFT rewards distributed
- Carry-forward items awarded (speedups, hero fragments, crystal)

### Season Carry-Forward

At season end, players receive:
- A permanent seasonal badge NFT
- Speedup package scaled to season ranking
- Hero fragments (do not reset)
- Dragoon lineage (NFTs persist across seasons on-chain)
- Crystal balance (up to 50% carries forward; remainder burns)

### Season Reset

At season start:
- Building levels reset to 0 (new world)
- Resources reset to 0
- Troops reset to 0
- Palace tier resets to 0
- Land NFTs persist — NFT land owners receive their plots pre-assigned in the new season

---

*This Game Design Bible defines the authoritative gameplay vision for Eternal Kingdoms. All feature implementation must align with this specification. Deviations require formal governance document updates before implementation begins.*
