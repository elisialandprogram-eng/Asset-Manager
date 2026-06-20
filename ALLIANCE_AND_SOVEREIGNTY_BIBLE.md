# Eternal Kingdoms — Alliance and Sovereignty Bible

> **SPECIFICATION FREEZE DOCUMENT — IMMUTABLE REFERENCE ARCHITECTURE**
> Version 1.0 — June 2026

---

## 1. Alliance Overview

Alliances are the fundamental unit of political and military power in Eternal Kingdoms. No endgame mechanic — zone control, shrine capture, Congress, Ancient Dragon hunts — is achievable by a solo player. Alliance design is the social backbone of the game.

An Alliance is a persistent organization that:
- Holds territory on the world map
- Fields coordinated military forces (rally system)
- Conducts shared research
- Governs itself through a ranked permission system
- Participates in the Congress endgame

---

## 2. Alliance Formation

Any player at Palace Tier II (Palace level 3+) may found an alliance. Alliance creation costs:
- 50,000 Gold
- 10,000 Crystal
- A minimum power level of 5,000

At founding, the creator becomes the R5 (King/Leader) of the alliance.

### Alliance Identity

| Field | Constraint |
|-------|-----------|
| Name | 4–24 characters, unique per world |
| Tag | 3–5 uppercase characters, unique per world |
| Emblem | Selected from a fixed emblem library (future: custom NFT emblems) |
| Description | Up to 500 characters |
| Language | Declared primary language (used for matchmaking suggestions) |

---

## 3. Alliance Hierarchy and Ranks

### Rank Structure

| Rank | Title | Slots | Notes |
|------|-------|-------|-------|
| R5 | King / Leader | 1 | Unique. Cannot be demoted except by voluntary transfer. |
| R4 | Duke / Officer | Up to 4 | Senior leadership. Operational control. |
| R3 | Knight / Elite | Up to 10 | Combat leaders. Rally authority. |
| R2 | Soldier | Unlimited | Standard members. Full participation. |
| R1 | Recruit | Unlimited | New members. Limited permissions for 72 hours. |

### Rank Permissions

| Permission | R5 | R4 | R3 | R2 | R1 |
|------------|----|----|----|----|-----|
| Invite members | ✓ | ✓ | ✓ | — | — |
| Kick members | ✓ | ✓ | — | — | — |
| Promote/demote R1–R3 | ✓ | ✓ | — | — | — |
| Promote/demote R4 | ✓ | — | — | — | — |
| Transfer R5 | ✓ | — | — | — | — |
| Declare war | ✓ | ✓ | — | — | — |
| Declare NAP | ✓ | ✓ | — | — | — |
| Place fortress | ✓ | ✓ | — | — | — |
| Alliance bank withdrawal | ✓ | ✓ | — | — | — |
| Alliance bank deposit | ✓ | ✓ | ✓ | ✓ | — |
| Manage alliance research | ✓ | ✓ | — | — | — |
| Lead rally (standard) | ✓ | ✓ | ✓ | — | — |
| Lead rally (shrine/fortress) | ✓ | ✓ | — | — | — |
| Request alliance help | ✓ | ✓ | ✓ | ✓ | ✓ |
| Send alliance help | ✓ | ✓ | ✓ | ✓ | — |
| Reinforce ally | ✓ | ✓ | ✓ | ✓ | — |
| View alliance map | ✓ | ✓ | ✓ | ✓ | ✓ |

---

## 4. Alliance Treasury

### Treasury Types

| Treasury | Contents | Access |
|----------|----------|--------|
| Resource Bank | Food, Wood, Stone, Iron, Gold | R4/R5 withdrawal; R2+ deposit |
| Crystal Vault | Crystal | R5-only withdrawal; R3+ deposit |
| NFT Vault | Dragoon Eggs, seasonal items | R5-only |

### Resource Bank Mechanics

Members deposit resources voluntarily into the Resource Bank. Deposits earn Alliance Loyalty Points (ALP):
```
ALP_earned = resources_deposited × RESOURCE_VALUE_WEIGHT[resource_type] × loyalty_multiplier

RESOURCE_VALUE_WEIGHT:
  Food:   0.5
  Wood:   0.5
  Stone:  1.0
  Iron:   1.5
  Gold:   2.0
  Crystal: 5.0
```

ALP feeds the Alliance Research donation system and determines member seniority ranking.

### Alliance Help System

When a member requests "Alliance Help" on a building, research, or training queue:
1. Each help tick reduces the timer by 1% (minimum 1 minute).
2. Maximum 20 helps per queue item (20% time reduction).
3. Each helper earns +10 ALP.
4. Help is requested via a push notification to all online alliance members.

---

## 5. Alliance Research

Alliance research is funded collectively by member donations to the Resource Bank.

### Alliance Research Queue

Only one alliance research can be active at a time. R4/R5 select the active research. Research speed scales with donation volume in the current week.

### Alliance Research Tree

| Branch | Research | Max Level | Effect |
|--------|----------|-----------|--------|
| Military | Alliance March Capacity | 10 | +5% rally capacity per level |
| Military | Alliance Attack | 10 | +3% all troop attack per level |
| Military | Alliance Defense | 10 | +3% all troop defense per level |
| Economy | Alliance Production | 10 | +2% resource production per level |
| Economy | Alliance Hospital | 5 | +10% hospital capacity per level |
| Economy | Alliance Construction | 5 | −3% construction time per level |
| Territory | Fortress Defense | 10 | +5% fortress wall HP and defense per level |
| Territory | Territory Expansion | 5 | +2% zone tile control range per level |
| Intelligence | Shared Vision | 5 | +2 tiles alliance vision sharing range per level |

### Research Cost Formula

```
alliance_research_cost(level) = BASE_COST × 2.0^(level - 1)
alliance_research_speed_hours = BASE_DURATION × 1.5^(level - 1) / donation_speed_multiplier

donation_speed_multiplier = min(weekly_crystal_donated / WEEKLY_CRYSTAL_TARGET, 3.0)
```

Maximum 3× speed through heavy donations — research cannot be infinitely accelerated.

---

## 6. Territory Ownership

### Zone Control Mechanics

Alliance territory is tracked at the chunk level (64×64 tile chunks). An alliance "controls" a chunk by:
1. Having more ally kingdom power within the chunk than any enemy alliance.
2. Holding the control state for a minimum of 6 continuous hours.

```
chunk_power(alliance, chunk_id) = sum(kingdom_power for kingdom in chunk where kingdom.alliance = alliance)
controlling_alliance(chunk_id) = alliance with highest chunk_power, if held ≥ 6 hours
```

### Zone Ownership

An alliance controls a Zone when it controls ≥50% of the chunks within that zone's radius.

### Territory Bonus Application

Territory bonuses (see World Architecture Bible, Section 3) apply to all alliance member kingdoms within the controlled zone. Bonuses update every 60 minutes (server tick).

### Territory Contested State

If two alliances have competing power within a chunk (within 10% of each other), the chunk enters "Contested" state:
- Territory bonus suspended for both alliances in contested chunk
- All gather marches in the chunk become vulnerable to PvP attack
- Contested state resolved when one alliance exceeds the other by >10% power

---

## 7. Alliance Fortresses

### Fortress Purpose

Alliance Fortresses are permanent structures placed on the world map that:
- Serve as staging points for rally attacks
- Provide march speed bonuses to alliance members within range
- Extend the alliance's territory influence

### Fortress Mechanics

| Property | Value |
|----------|-------|
| Footprint | 4×4 tiles (see World Architecture Bible) |
| Max per alliance | 1 per zone (maximum 7 total) |
| Wall HP | 2,000,000 × fortress_level |
| Max garrison | 500,000 troops |
| March speed bonus radius | 20 tiles |
| March speed bonus | +15% for all alliance marches originating from within radius |

### Fortress Construction

Placing a fortress requires:
- R4 or R5 permission
- Alliance controls ≥30% of the target zone
- Resource cost: 10,000,000 Food + 5,000,000 Wood + 3,000,000 Stone + 1,000,000 Iron + 500,000 Gold
- Construction time: 48 hours (reducible by alliance help but not by speedups)

### Fortress Levels

Fortresses level up through resource investment by the alliance:

| Level | Wall HP | Garrison Cap | Rally Bonus |
|-------|---------|-------------|------------|
| 1 | 2M | 500K | +5% rally capacity |
| 2 | 5M | 1M | +10% rally capacity |
| 3 | 10M | 2M | +15% rally capacity |
| 4 | 20M | 3M | +20% rally capacity |
| 5 | 50M | 5M | +25% rally capacity, +10% all zone bonuses |

### Fortress Destruction

If a fortress is reduced to 0 HP by rally attack, it is destroyed — the structure is removed and the tiles become unoccupied. The alliance must rebuild from scratch. Fortress destruction is a major strategic setback.

---

## 8. Territory Taxation

### Taxation Mechanics

When a non-member player gathers resources from a node within an alliance-controlled zone:
- The controlling alliance taxes 5% of all resources gathered.
- Taxed resources are deposited directly into the Alliance Resource Bank.
- Non-alliance members are notified of the tax before sending a gather march.

### Tax-Free Access

Non-allied players may pay a "tribute" to the controlling alliance to gather tax-free:
- Tribute cost: 10% of expected gather value, paid upfront in gold.
- Tribute duration: 24 hours.
- R4/R5 must accept the tribute request.

### War Declaration and Taxation

During a declared war between alliances:
- The attacking alliance may zero-tax all resource nodes in contested chunks.
- The defending alliance loses tax income from contested chunks.
- This is an economic warfare dimension: attacking territory has immediate revenue impact.

---

## 9. Shrines

See World Architecture Bible for shrine positions and footprints. This section covers shrine governance.

### Shrine Governance

Shrines are governed independently from chunk territory control. Holding a shrine requires active military presence (garrison) and rally defense.

### Shrine Capture Flow

1. Attacking alliance forms a rally targeted at the shrine.
2. Shrine combat resolution follows standard rally battle rules (see Combat Engine Bible, Section 19).
3. If the shrine's HP reaches 0 and no garrison remains, the shrine changes ownership.
4. New ownership immediately applies the shrine buff to the capturing alliance.
5. 4-hour immunity window begins — no attacks accepted during this period.

### Shrine Upkeep

Controlling a shrine requires a weekly crystal payment:
- Outer shrine (Tier 1): 500 Crystal/week
- Mid shrine (Tier 2): 1,500 Crystal/week
- Central shrine (Tier 3): 5,000 Crystal/week

Upkeep is automatically deducted from the Crystal Vault. If the vault runs dry, the shrine becomes unguarded (any alliance may attack with no garrison impediment).

---

## 10. Congress System

### Congress Eligibility

Congress requires:
1. Holding ≥3 shrines simultaneously for 72 continuous hours.
2. Alliance treasury contains minimum: 10,000 Crystal for the declaration fee.
3. Alliance has ≥20 active members (logged in within the past 48 hours).

### Congress Phases (Detail)

**Phase 1 — Declaration (72 hours):**
- Any eligible alliance may declare candidacy by spending 10,000 Crystal.
- Multiple alliances may declare; there is no exclusivity.
- Declarations are visible to all players on the world map (a Congress banner appears over the declaring alliance's Central Fortress or nearest holding).

**Phase 2 — Campaigns (5 days):**
VP scoring:

| Activity | VP Awarded |
|----------|-----------|
| Holding a zone tile for 1 hour | +1 VP |
| Shrine controlled (per shrine, per hour) | +50 VP |
| Killing a monster (scaled by tier) | +T VP where T = tier |
| Winning a PvP battle against Congress candidate alliance | +10 VP per 1,000 casualties inflicted |
| Alliance member palace upgrade | +5 VP |

**Phase 3 — Battle for the Seat (24 hours):**
- Top 2 VP-scoring alliances advance to the final battle.
- The Central Shrine (Zone 0) becomes the battle arena.
- Standard zone bonuses are suspended — only shrine buffs apply.
- The alliance holding the Central Shrine at the 24-hour mark wins Congress.

---

## 11. King Powers

The King (Congress winner, R5 of the winning alliance) gains server-wide powers for the duration of their reign:

### Active King Powers

| Power | Effect | Cooldown |
|-------|--------|---------|
| Royal Decree | Broadcast a server-wide message | 24 hours |
| Tax Exemption | Declare one zone tax-free for alliance members for 24h | 48 hours |
| Rally Mandate | Next 3 rallies have +50% troop capacity | 72 hours |
| War Declaration | Initiate cross-world skirmish event | Once per reign |
| Pardon | Remove a player's "Wanted" marker (future PvP feature) | 72 hours |

### King Reputation

The King's actions are visible to all players. Over-aggressive use of king powers may trigger a "Rebellion" mechanic — if a majority of R5 leaders (from alliances with ≥50 members) vote for Rebellion, the King's powers are suspended for 24 hours. This is a governance safety valve.

### Dethroning

A King can be dethroned if:
- The Central Shrine is captured by another alliance (shrines remain active during a King's reign).
- The King's alliance falls below 10 members.
- A server-level vote (Rebellion) strips powers.

If dethroned, the server enters an Interregnum (no king, no king powers) until the next Congress cycle.

---

## 12. Server Governance

### Player Governance

Server governance is player-driven at the alliance level:
- King has nominal server governance authority (decrees, tax adjustments).
- Rebellion mechanic provides a check on King power.
- Future: DAO-style on-chain voting for major server parameter changes (season length, resource rates).

### Server Moderation

Game moderators (admin role) retain separate override authority from the King system:
- Moderators can ban, mute, and remove players for rule violations.
- Moderator actions are logged and auditable.
- Players can appeal moderator decisions through the support system.

### World Events

The game operations team may trigger special server events:
- **Ancient Dragon Spawn** — T6 monster requiring cross-alliance cooperation
- **Meteor Strike** — A rich crystal zone spawns at a random location for 24 hours
- **Ceasefire Treaty** — A 12-hour server-wide combat pause (used sparingly)

---

## 13. Diplomacy Systems

### Diplomatic States

Any two alliances can be in one of four diplomatic states:

| State | Description | Effects |
|-------|------------|---------|
| Neutral | Default state | Standard interaction; gathering may be taxed |
| NAP | Non-Aggression Pact | Cannot rally-attack each other's kingdoms or fortresses |
| Allied | Military alliance | Shared vision, can reinforce each other |
| War | Active conflict | All interaction is hostile; extra VP from kills |

### NAP System

Non-Aggression Pacts (NAPs) are formal diplomatic agreements:
1. R5 of Alliance A proposes NAP to Alliance B.
2. R5 of Alliance B accepts (or declines).
3. NAP is logged on-chain as a signed treaty (future blockchain phase).
4. NAP duration: Minimum 48 hours. Maximum 30 days. Renewable.
5. Breaking a NAP: Either alliance can break with 12-hour notice. NAP breaking is visible to all players (diplomacy log).

**NAP violation penalties (planned):**
- Breaking NAP without notice: −1,000 ALP for all members of the breaking alliance.
- Server-wide notification of NAP breaking (reputation mechanic).

### Alliance Merger

Two alliances may merge:
1. Both R5 leaders must agree.
2. Combined member count must not exceed the cap.
3. One alliance absorbs the other (R5 of the absorbing alliance retains rank).
4. Absorbed R5 is offered R4.
5. All ALP from both alliances transfers to the merged entity.
6. All territory and fortress ownership transfers.

### Alliance Split

Alliances cannot formally "split" — members must leave individually and re-form. This design is intentional: splitting creates transition chaos that mirrors real medieval politics.

---

## 14. Alliance Migration

### Kingdom Migration

Players may migrate their kingdom to a different zone on the same world map:
- Migration requires a "Migration Scroll" (rare event item or premium purchase).
- Migration cooldown: 30 days per kingdom.
- Migration to Zone 0 or Zone 1 is not possible without King permission.
- Migration cannot place the kingdom within 8 tiles of an existing kingdom.

### Alliance-Wide Migration

An R5 may coordinate a mass migration event for the alliance. This requires:
- Individual Migration Scrolls for each migrating member.
- All members must migrate within a 24-hour window.
- A designated "arrival zone" is declared; the server attempts to place all migrating kingdoms as a cluster.

### Inter-World Migration (Cross-World)

Cross-world migration is a paid premium feature:
- Requires "World Portal Token" (NFT, obtained through season rewards or marketplace).
- Only available during designated cross-world windows (announced 7 days in advance).
- All progress carries — palace level, buildings, troops, resources.
- Alliance membership must be manually reformed on the new world.
- NFT land plots do NOT migrate — they remain world-specific.

---

## 15. Cross-World Warfare

### Mechanic

Cross-world warfare is triggered by the reigning King (once per reign) or by server operations for special events.

### Cross-World War Rules

1. A temporary "Battle World" (condensed 512×512 grid) is instantiated.
2. The King selects participating worlds (minimum 2, maximum 4).
3. Each world selects its top 3 alliances by power to participate.
4. Participating alliances' top players are cloned into the Battle World (original kingdoms untouched).
5. Battle World runs for 7 days.
6. Victory determined by final zone tile control at day 7.

### Cross-World Rewards

| Rank | Reward |
|------|--------|
| 1st (winning alliance) | Legendary seasonal NFT badge + 5,000 Crystal per member |
| 2nd | Epic seasonal NFT badge + 2,000 Crystal per member |
| 3rd | Rare seasonal NFT badge + 500 Crystal per member |
| Participation | Common badge + 100 Crystal per member |

### Isolation Guarantee

Cross-world warfare is economically isolated:
- Resources, troops, and buildings in the Battle World are clones — nothing deducted from home world.
- Battle World troops cannot return to the home world.
- Crystal earned in the Battle World is deposited directly to the home world treasury.

---

*This Alliance and Sovereignty Bible defines the authoritative design for all social organization, governance, and diplomatic mechanics in Eternal Kingdoms. All alliance system implementation must conform to this specification.*
