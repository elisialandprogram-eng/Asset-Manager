# Eternal Kingdoms — Combat Engine Bible

> **SPECIFICATION FREEZE DOCUMENT — IMMUTABLE REFERENCE ARCHITECTURE**
> Version 1.0 — June 2026

---

## 1. Overview

Combat in Eternal Kingdoms is a **deterministic server-side simulation**. All battle outcomes are computed authoritatively on the server using sealed input parameters (troop counts, buffs, research, hero stats, dragoon bonuses). No client can influence a battle outcome. Results are signed and published to clients as battle reports.

The combat engine is a pure computation module (no DB calls, no side effects) — structurally identical to the existing `lib/game-engine` pattern used for resource production.

---

## 2. March Equations

### March Speed

```
march_speed_tiles_per_min = BASE_MARCH_SPEED
  × (1 + march_speed_research_bonus)
  × (1 + hero_speed_stat / 10000)
  × (1 + dragoon_agility_bonus)
  × terrain_modifier(source_tile)

BASE_MARCH_SPEED = 2.0 tiles/minute (infantry reference)
```

Troop type speed modifiers:
- Infantry: 1.0×
- Cavalry: 1.6×
- Ranged: 0.9×
- Siege: 0.5×

Mixed march speed = weighted average by troop count:
```
effective_speed = sum(troop_type_count × type_speed_modifier) / total_troops
```

### Distance Formula

```
distance_tiles = sqrt((target_x − source_x)² + (target_z − source_z)²)
```

### Travel Time

```
travel_time_minutes = distance_tiles / march_speed_tiles_per_min
travel_time_seconds = travel_time_minutes × 60
```

Travel time is computed at march creation. The march `arrives_at` timestamp is:
```
arrives_at = created_at + travel_time_seconds
```

Server processes combat resolution when `now >= arrives_at`.

### March Return Time

Return travel time uses the same formula. Surviving troops march back at the same speed as the outgoing march (no penalty for casualties).

---

## 3. Speed Formulas by Troop Tier

| Tier | Infantry Speed | Cavalry Speed | Ranged Speed | Siege Speed |
|------|---------------|--------------|-------------|------------|
| T1 | 2.0 | 3.2 | 1.8 | 1.0 |
| T2 | 2.2 | 3.5 | 2.0 | 1.1 |
| T3 | 2.4 | 3.8 | 2.2 | 1.2 |
| T4 | 2.6 | 4.2 | 2.4 | 1.4 |
| T5 | 2.8 | 4.6 | 2.6 | 1.6 |

All values are in tiles/minute before research and buff multipliers.

---

## 4. Combat Simulation

### Combat Model

Combat is resolved in **round-by-round simulation**. Each round, both sides deal simultaneous damage. Rounds continue until one side is eliminated or the battle timer expires (maximum 100 rounds per engagement).

### Sides

- **Attacker**: The marching force (player troops + hero + dragoon)
- **Defender**: The defending force (player troops + wall phase troops + traps + hero + dragoon + garrison reinforcements)

### Unit Categories

Each side's force is composed of three unit types participating in combat in parallel:

| Category | Role | Countered By |
|----------|------|-------------|
| Infantry | Front line, high HP | Cavalry |
| Cavalry | High attack, flanking | Ranged |
| Ranged | Sustained DPS | Infantry |

This is the **RPS (Rock-Paper-Scissors) triangle**:
```
Infantry > Ranged (close the gap, overwhelm archers)
Cavalry > Infantry (charge, break formation)
Ranged > Cavalry (devastate unshielded mounts)
```

Siege engines are special — they deal bonus damage to walls and fortresses but are ineffective against units (0.5× combat multiplier vs. troops).

---

## 5. RPS Modifiers

The RPS system applies a multiplier to damage dealt based on the relationship between attacker unit type and defender unit type:

```
RPS matrix (attacker unit → defender unit: damage multiplier)
                    DEF: Infantry  DEF: Cavalry  DEF: Ranged  DEF: Siege
ATK: Infantry         1.0×          0.6×           1.4×         0.8×
ATK: Cavalry          1.4×          1.0×           0.6×         0.8×
ATK: Ranged           0.6×          1.4×           1.0×         0.8×
ATK: Siege            0.8×          0.8×           0.8×         2.0×
```

The RPS modifier is applied **per unit type pair** within each round.

---

## 6. Armor Curves

Armor reduces incoming damage. The damage reduction formula uses a diminishing returns curve to prevent armor from becoming infinite:

```
damage_reduction = armor / (armor + ARMOR_CONSTANT)
ARMOR_CONSTANT = 1000  (tunable parameter)

effective_damage = raw_damage × (1 - damage_reduction)
               = raw_damage × (ARMOR_CONSTANT / (armor + ARMOR_CONSTANT))
```

At armor = 0: damage_reduction = 0% (no protection)
At armor = 1000: damage_reduction = 50%
At armor = 4000: damage_reduction = 80%
At armor = 9000: damage_reduction = 90%

Armor is a property of each troop type, scaled by tier:
```
armor(troop_type, tier) = BASE_ARMOR[troop_type] × (1 + 0.15 × (tier - 1))
```

Base armor values:
- Infantry: 800 (heavy armor)
- Cavalry: 600 (medium armor)
- Ranged: 300 (light armor)
- Siege: 200 (very light)

---

## 7. Attack Formulas

### Per-Round Attack Power

```
unit_attack = BASE_ATTACK[type][tier]
            × (1 + attack_research_bonus)
            × (1 + hero_attack_stat / 10000)
            × (1 + dragoon_strength_bonus)
            × buff_multiplier_total
            × rps_modifier(attacker_type, defender_type)

round_damage_dealt = unit_attack × unit_count
```

Base attack values by type and tier:

| Tier | Infantry Attack | Cavalry Attack | Ranged Attack | Siege Attack |
|------|----------------|---------------|--------------|-------------|
| T1 | 150 | 200 | 180 | 500 |
| T2 | 230 | 310 | 270 | 800 |
| T3 | 360 | 480 | 420 | 1,300 |
| T4 | 560 | 740 | 650 | 2,100 |
| T5 | 880 | 1,160 | 1,020 | 3,400 |

### HP Values by Type and Tier

| Tier | Infantry HP | Cavalry HP | Ranged HP | Siege HP |
|------|------------|-----------|----------|---------|
| T1 | 600 | 500 | 400 | 1,200 |
| T2 | 950 | 790 | 620 | 1,900 |
| T3 | 1,500 | 1,250 | 980 | 3,000 |
| T4 | 2,400 | 2,000 | 1,560 | 4,800 |
| T5 | 3,800 | 3,200 | 2,500 | 7,800 |

---

## 8. Defense Formulas

### Defender Base Defense

The defender's combat stats follow the same formula as attacker, but the defender side also receives additional bonuses from:
- Wall defense (if attacking a kingdom with a constructed wall)
- Garrison troops (reinforcements from alliance members)
- Traps
- Morale (consecutive defense bonus/penalty)

### Wall Defense Bonus

```
wall_defense_bonus = WALL_TIER × WALL_DEFENSE_PER_TIER × (1 + wall_research_bonus)
defender_effective_attack × (1 + wall_defense_bonus)
```

Wall tiers are constructed separately from buildings (constructed with stone at a dedicated Wall structure).

### Trap Damage

Traps activate in the first 2 rounds of any kingdom attack, dealing flat damage to the attacker's front line:
```
trap_damage_round1 = TRAP_COUNT × TRAP_DAMAGE_PER_UNIT × (1 + trap_research_bonus)
trap_damage_round2 = trap_damage_round1 × 0.5  (traps are partially destroyed in round 1)
```

Traps are consumed in combat — half are destroyed per engagement. Players must rebuild traps between defenses.

---

## 9. Buff Stacking Rules

### Buff Categories

Buffs are divided into categories. Within each category, only the highest value applies (no additive stacking of same-category buffs). Across categories, all buffs are multiplicative.

| Category | Source | Example |
|----------|--------|---------|
| Research | Academy / Research Lab | Infantry Attack Research +30% |
| Hero | Hero attached to march | Hero Attack Stat +15% |
| Dragoon | Dragoon bond | Fire Drake Siege +25% |
| Alliance | Alliance territory / research | Alliance Attack +10% |
| Buff Item | Consumable (event rewards) | Attack Buff +20% (4h) |
| Shrine | Shrine control | Shrine buff +8% all combat |
| Morale | Consecutive victories | +5% per consecutive win, max +25% |

### Stacking Formula

```
total_attack_multiplier = (1 + best_research_bonus)
                        × (1 + hero_attack_bonus)
                        × (1 + dragoon_attack_bonus)
                        × (1 + alliance_attack_bonus)
                        × (1 + buff_item_attack_bonus)
                        × (1 + shrine_attack_bonus)
                        × (1 + morale_bonus)
```

Maximum theoretical attack multiplier (all buffs maxed): ~5.0× base attack.

### Anti-Monopoly Cap

No single buff category may provide more than 50% bonus. This prevents research-only or hero-only strategies from being overwhelmingly dominant:
```
max_single_category_bonus = 0.50 (50%)
```

---

## 10. Research Modifiers

Combat research is applied at simulation time. The server queries the attacking and defending kingdoms' research states before resolving combat.

### Applied Research Bonuses

**Attacker research modifiers:**
- Infantry Attack I–X: +3% per level (max +30%)
- Cavalry Attack I–X: +3% per level (max +30%)
- Ranged Attack I–X: +3% per level (max +30%)
- Siege Mastery I–X: +5% per level (max +50%)
- March Speed I–X: applied to travel time, not combat damage
- March Capacity I–X: applied to troop count cap, not combat damage

**Defender research modifiers:**
- Wall Defense I–X: +5% wall bonus per level
- Hospital Capacity I–X: affects wound conversion rate, not combat
- Morale Resilience I–V: +3% morale floor per level (min morale never below floor)

---

## 11. Hero Modifiers

### Hero Stat Contributions to Combat

| Hero Stat | Combat Effect | Formula |
|-----------|--------------|---------|
| Attack | All troop attack | +hero_attack / 10000 fractional bonus |
| Defense | All troop defense rating | +hero_defense / 10000 fractional bonus |
| Speed | March travel speed | +hero_speed / 10000 fractional bonus |
| Command | March troop capacity | +hero_command / 10000 fractional bonus |
| Gathering | Gather rate (non-combat) | +hero_gathering / 10000 fractional bonus |

### Hero Active Skills (in combat)

Hero active skills trigger once per battle in the declared phase:

| Skill | Phase | Effect |
|-------|-------|--------|
| Iron Will | Round 1 | All friendly troops immune to trap damage |
| Cavalry Charge | Round 1 | Cavalry attack ×2.0 for rounds 1–2 |
| Volley | Round 2 | Ranged attack ×1.5 for rounds 2–3 |
| Berserk | Round 5+ | All friendly attack +30% when HP below 50% |
| Shield Wall | Round 1 | Infantry armor +50% for rounds 1–3 |

Only one active skill fires per battle (the assigned skill on the hero's active skill slot).

---

## 12. Dragoon Modifiers

Dragoons accompany the march and provide passive bonuses throughout combat.

### Dragoon Type Combat Bonuses

| Dragoon | Bonus |
|---------|-------|
| Fire Drake | Siege attack +25%, +10% fortress wall damage |
| Frost Wyvern | Cavalry attack +20%, enemy cavalry speed −15% (applied to counterattack) |
| Storm Eagle | Not a direct combat bonus — improves scout intelligence quality |
| Earth Titan | All friendly defense +20%, trap regeneration +50% after battle |
| Void Serpent | +20% plunder rate, hospital bypass: 10% of wounds become deaths instead of recoveries |

### Dragoon Strength Scaling

```
dragoon_combat_bonus = BASE_BONUS[dragoon_type]
                     × (1 + (dragoon_level - 1) × 0.04)  -- +4% per level
                     × (1 + dragoon_strength / 5000)     -- stat scaling
```

At level 30 with max strength: up to 2.5× the base bonus.

---

## 13. Hospital Routing

### Post-Battle Casualty Assignment

After battle resolution, casualties on the defender's side are divided:
```
total_casualties = initial_troops - surviving_troops

wound_capacity_remaining = hospital_capacity - currently_wounded
wounds_to_hospital = min(total_casualties × wound_rate, wound_capacity_remaining)
permanent_deaths = total_casualties - wounds_to_hospital

wound_rate = BASE_WOUND_RATE × (1 + hospital_research_bonus)
```

Casualties on the attacker's side follow the same formula, applied to the attacker's home kingdom hospital.

### Wound Priority

Highest-tier troops are hospitalized first:
```
Priority: T5 > T4 > T3 > T2 > T1
Within same tier: Infantry > Cavalry > Ranged > Siege
```

### Healing Rate

```
heal_per_minute = HOSPITAL_LEVEL × HEAL_RATE_PER_LEVEL × (1 + medical_research_bonus)
HEAL_RATE_PER_LEVEL = 5 troops/minute per hospital level
```

At Hospital level 20 with max research: ~200 troops healed per minute.

---

## 14. Casualty Rules

### Death vs. Wound Determination

```
wound_rate_base = 0.80  (80% of casualties become wounded by default)
wound_rate_modified = wound_rate_base × (1 + hospital_capacity_research)
                    - void_serpent_penalty                          (if attacker has Void Serpent)
                    + earth_titan_bonus                             (if defender has Earth Titan)

wound_rate_final = clamp(wound_rate_modified, 0.50, 0.95)
```

Maximum wound rate: 95% (with max hospital research + Earth Titan). Minimum wound rate: 50% (absolute floor — some deaths always occur in combat).

### Attacker Casualties

Attacker casualties follow the same wound/death split, applied to the attacker's home kingdom hospital. If the attacker is away on a long march, wounds queue in the hospital and heal passively.

---

## 15. Plunder Rules

### Plunder Eligibility

Only resources above the defender's Warehouse protection cap are plunderable:
```
plunderable(resource) = max(0, defender_resource[resource] - warehouse_protection[resource])
```

### Plunder Capacity

```
plunder_capacity = march_troop_count × LOAD_PER_TROOP[troop_type] × (1 + plunder_research_bonus)

LOAD_PER_TROOP:
  Infantry: 4
  Cavalry: 10 (fast loaders)
  Ranged: 6
  Siege: 2
```

### Plunder Distribution

When the attacker wins:
```
total_loot = min(total_plunderable, plunder_capacity)

food_loot = min(total_loot × 0.35, plunderable_food)
wood_loot = min(total_loot × 0.30, plunderable_wood)
stone_loot = min(total_loot × 0.20, plunderable_stone)
iron_loot = min(total_loot × 0.10, plunderable_iron)
gold_loot = min(total_loot × 0.05, plunderable_gold)
```

The attacker carries loot home. The march return is loaded and slower if loot is heavy:
```
loaded_speed_modifier = 0.85  (15% march speed reduction when carrying full loot)
```

---

## 16. Wall Phase

### Wall Phase Description

When attacking a kingdom with a constructed wall, combat proceeds in two phases:

**Phase 1 — Wall Phase (rounds 1–N_wall_rounds):**
- Defender fights from behind wall: +wall_defense_bonus
- Attacker's siege engines deal full damage to the wall (reducing its HP)
- Wall HP = WALL_TIER × HP_PER_TIER
- Phase 1 ends when wall HP reaches 0 OR after N_wall_rounds (where N = wall_hp / siege_damage_per_round)

**Phase 2 — Open Combat (after wall falls):**
- Wall defense bonus removed
- Combat continues with remaining troops
- Traps are activated at the start of Phase 2 (if wall fell in Phase 1)

**No Siege Case:**
If the attacker brings no siege engines, the wall never falls. All combat occurs against the wall bonus. This strongly favors the defender — attackers facing walls should bring siege engines.

---

## 17. Battle Reports

### Battle Report Structure

```json
{
  "battle_id": "uuid",
  "type": "kingdom_attack",
  "occurred_at": "2026-06-20T14:30:00Z",
  "attacker": {
    "kingdom_id": "...",
    "kingdom_name": "The Iron Citadel",
    "hero": { "name": "Aethelred", "level": 45 },
    "dragoon": { "type": "fire_drake", "level": 12 },
    "troops_sent": { "infantry_t3": 5000, "cavalry_t2": 2000, "siege_t3": 500 },
    "troops_survived": { "infantry_t3": 3200, "cavalry_t2": 1800, "siege_t3": 500 },
    "troops_wounded": { "infantry_t3": 1400, "cavalry_t2": 150 },
    "troops_killed": { "infantry_t3": 400, "cavalry_t2": 50 },
    "loot": { "food": 150000, "wood": 80000, "stone": 30000, "iron": 10000, "gold": 5000 }
  },
  "defender": {
    "kingdom_id": "...",
    "kingdom_name": "Mossbrook Keep",
    "hero": null,
    "dragoon": { "type": "earth_titan", "level": 8 },
    "troops_defending": { "infantry_t2": 10000, "ranged_t2": 5000 },
    "troops_survived": { "infantry_t2": 2000 },
    "troops_wounded": { "infantry_t2": 5500, "ranged_t2": 4500 },
    "troops_killed": { "infantry_t2": 2500, "ranged_t2": 500 },
    "wall_destroyed": false,
    "traps_triggered": 200
  },
  "outcome": "attacker_victory",
  "rounds_simulated": 18,
  "simulation_seed": "0xabc123..."
}
```

### Battle Report Storage

Battle reports are stored in the `battle_reports` table. Players can view their last 30 reports. Reports older than 30 days are archived (accessible via account page, not in-game UI).

### Battle Report Sharing

Players can share a battle report link. The link resolves to a static battle report page (no auth required) for a limited time (7 days).

---

## 18. Rally Battles

### Rally Combat Resolution

Rally battles aggregate all participating troops into a single attacking force. Combat simulation treats the rally as one attacker.

### Hero Selection for Rallies

The rally leader's hero applies to the entire rally. Individual member heroes do not apply (only the rally leader's hero bonus is used). This incentivizes high-level heroes leading important rallies.

### Rally Casualty Distribution

After combat, casualties are assigned to rally participants proportionally:
```
member_casualty_share = member_troops_contributed / total_rally_troops
member_casualties = total_rally_casualties × member_casualty_share
```

Casualties are distributed to each member's home hospital independently.

### Rally vs. Monster (T4/T5)

For Tier 4 and 5 monsters, the combat simulation is inverted — the monster is the "defender" with its own attack, defense, and HP values. T5 monsters require 50,000+ total rally troops to have a reasonable kill probability.

---

## 19. Shrine Battles

### Shrine Combat Rules

Shrines have their own HP pool and defense:
```
shrine_hp = 500,000 × shrine_tier
shrine_defense_rating = 2000 × shrine_tier
shrine_attack_rating = 1500 × shrine_tier (damages attacker each round)
```

Shrines are captured by reducing HP to 0 via rally attack. Individual attacks deal 10% of normal damage to shrines.

### Shrine Defense

The alliance controlling a shrine may garrison troops (up to 100,000 total). Garrisoned troops are treated as the shrine's defending force in the combat simulation. All garrisoned troop casualties follow standard hospital/death rules.

### Shrine Recapture

After capture, a newly captured shrine has a 4-hour immunity window — it cannot be attacked during this period. This prevents immediate recapture ping-pong.

---

## 20. Congress Wars

### Congress War Phase

During the 24-hour Congress Battle Phase, combat rules are modified:

- **Enhanced rally capacity:** All rally capacity limits are removed during Congress War.
- **No shields:** No protection shields (beginner or purchased) apply during Congress War.
- **Hospital override:** Hospital capacity effectively doubles (emergency wartime field hospitals).
- **Sanctuary zones:** Zone 6 and 7 are sanctuary — no PvP allowed in those zones during Congress War.
- **Zone bonus stacking:** All held zone bonuses stack additively (not multiplicatively) during Congress War.

### King Coronation Combat

The Congress final battle is a structured server event — all armies from the top 2 alliances converge on the Central Shrine (Zone 0). The alliance that holds the Central Shrine at the 24-hour mark wins and their leader is crowned King.

---

## 21. Deterministic Server Simulation

### Simulation Guarantees

All combat simulations produce identical results given identical inputs. Inputs are:
1. Attacker force composition (troop counts by type and tier)
2. Defender force composition (troop counts + wall HP + trap count)
3. Research levels (attacker and defender)
4. Hero stats (attacker and defender, if any)
5. Dragoon stats (attacker and defender, if any)
6. Active buff states (alliance territory, shrine buffs, buff items)
7. Simulation seed (derived from `keccak256(battle_id, server_timestamp)`)

The simulation seed is used only for tie-breaking in edge cases (e.g., exact HP ties). All other outcomes are deterministic from the inputs.

### Replay Validation

Any battle can be replayed by re-running the simulation with the stored inputs. The replay output must match the original output exactly. Discrepancies indicate a bug or input tampering.

---

## 22. Anti-Cheat Principles

### Threat Model

- **Client-side manipulation:** The Unity client is untrusted. Clients report intended actions (send march, confirm attack). The server validates all inputs before accepting.
- **March manipulation:** Clients cannot modify march arrival times. All march timestamps are computed server-side at march creation.
- **Stat injection:** Combat resolution uses only server-stored stats (from DB). Clients never contribute stat values to combat resolution.
- **Replay attack:** March orders include a server-issued nonce. Re-submitting an old march order is rejected (nonce already consumed).

### Validation Rules

Before processing any combat march:
1. Kingdom ownership verified (player JWT must match march source kingdom).
2. Troop count ≤ available troops in kingdom (not already marching).
3. Target coordinates exist and are within world bounds.
4. March capacity not exceeded.
5. No active shield on the target kingdom (unless specific shield-bypass conditions met).

### Rate Limiting

- Maximum 10 marches submitted per second per kingdom (burst limit).
- Maximum 100 march submissions per minute per account.
- Exceeding rate limits triggers a temporary lockout (60 seconds) and logs a suspicious activity record.

### Simulation Auditing

All combat simulations are logged server-side with full input parameters and output hash. A weekly audit job replays a random 5% of all battles from logs and verifies outputs match. Discrepancies trigger an alert and engineering review.

---

*This Combat Engine Bible defines the authoritative mathematical foundation for all combat resolution in Eternal Kingdoms. The Unity client displays battle animations and results; it never computes outcomes. All deviations from this specification require a formal governance document update.*
