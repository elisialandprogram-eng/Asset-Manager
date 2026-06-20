/**
 * combatEngine.ts — Deterministic backend-only PvE combat resolver.
 *
 * Source: COMBAT_ENGINE_BIBLE.md + Phase 4 spec (U4.5)
 *
 * Rules:
 *   - Max 5 rounds (Phase 4 spec)
 *   - Damage formula: (Atk² / (Atk + Def)) × RPSModifier
 *   - Armor curve: effective_damage = raw × (1000 / (armor + 1000))
 *   - RPS: Infantry > Cavalry, Cavalry > Archer, Archer > Infantry, Siege neutral
 *   - Attacker = player troops (+ optional hero buffs)
 *   - Defender = monster (single entity with hp/attack/defense)
 *   - Combat resolves simultaneously each round
 *   - NO Unity combat calculations — backend only
 *
 * Hospital rules applied AFTER combat, not inside engine:
 *   - Field ops: 50% dead, 50% wounded
 *   - Overflow capacity → permanent death
 */

import {
  TROOP_DEFINITIONS,
  getDominantClass,
  parseTroopKey,
  type TroopClass,
  type TroopKey,
} from "./troopDefinitions";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface MonsterStats {
  name: string;
  tier: string;
  hp: number;
  attack: number;
  defense: number;
  /** Monster's dominant "class" for RPS calculation (null = neutral). */
  combatClass: TroopClass | null;
}

export interface HeroBuff {
  attackBonus: number;
  defenseBonus: number;
  speedBonus: number;
  /** Per-round skill triggers: round → bonus attack multiplier */
  roundSkills: Record<number, number>;
}

export interface CombatInput {
  attackerTroops: Record<string, number>;
  monster: MonsterStats;
  heroBuff?: HeroBuff;
  researchAttackBonus?: number;
  researchDefenseBonus?: number;
}

export interface RoundResult {
  round: number;
  attackerDamageDealt: number;
  defenderDamageDealt: number;
  attackerHpAfter: number;
  defenderHpAfter: number;
  attackerTroopsLostThisRound: Record<string, number>;
}

export interface CombatResult {
  attackerWon: boolean;
  roundsFought: number;
  rounds: RoundResult[];
  attackerTroopsSent: Record<string, number>;
  attackerTroopsSurvived: Record<string, number>;
  attackerTotalLosses: Record<string, number>;
  monsterSurvivingHp: number;
}

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

export const MAX_COMBAT_ROUNDS = 5;

// ---------------------------------------------------------------------------
// Core damage formula
// ---------------------------------------------------------------------------

/**
 * Damage formula from COMBAT_ENGINE_BIBLE.md §3:
 *   raw_damage = (Atk² / (Atk + Def))
 *   effective  = raw_damage × (1000 / (armor + 1000)) × RPSModifier
 *
 * Simplified for Phase 4: armor = def for monsters, no wall phase.
 */
function calcDamage(atk: number, def: number, rpsModifier: number): number {
  if (atk <= 0) return 0;
  const raw = (atk * atk) / (atk + Math.max(def, 1));
  const armorReduction = 1000 / (def + 1000);
  return raw * armorReduction * rpsModifier;
}

// ---------------------------------------------------------------------------
// Attacker aggregate stats
// ---------------------------------------------------------------------------

function getAttackerStats(
  troops: Record<string, number>,
  heroBuff: HeroBuff,
  researchAtk: number,
  researchDef: number,
  currentRound: number,
): { totalAtk: number; totalDef: number; totalHp: number; dominant: TroopClass } {
  let totalAtk = 0;
  let totalDef = 0;
  let totalHp  = 0;

  const roundSkillBonus = heroBuff.roundSkills[currentRound] ?? 0;
  const atkMultiplier =
    (1 + researchAtk) *
    (1 + heroBuff.attackBonus / 10_000) *
    (1 + roundSkillBonus);
  const defMultiplier =
    (1 + researchDef) *
    (1 + heroBuff.defenseBonus / 10_000);

  for (const [key, count] of Object.entries(troops)) {
    if (count <= 0) continue;
    const def = TROOP_DEFINITIONS[key as TroopKey];
    if (!def) continue;
    totalAtk += def.baseAttack * atkMultiplier * count;
    totalDef += def.baseDefense * defMultiplier * count;
    totalHp  += def.baseHp * count;
  }

  return {
    totalAtk,
    totalDef,
    totalHp,
    dominant: getDominantClass(troops),
  };
}

// ---------------------------------------------------------------------------
// Main combat resolver
// ---------------------------------------------------------------------------

/**
 * resolve() — Runs the full deterministic PvE combat simulation.
 *
 * Returns complete round-by-round breakdown and final casualties.
 * Pure function — no DB calls, no side effects.
 */
export function resolve(input: CombatInput): CombatResult {
  const {
    attackerTroops,
    monster,
    heroBuff = { attackBonus: 0, defenseBonus: 0, speedBonus: 0, roundSkills: {} },
    researchAttackBonus = 0,
    researchDefenseBonus = 0,
  } = input;

  const troopsSent = { ...attackerTroops };
  const currentTroops = { ...attackerTroops };

  let monsterHp = monster.hp;
  let attackerTotalHp = 0;
  for (const [key, count] of Object.entries(currentTroops)) {
    const def = TROOP_DEFINITIONS[key as TroopKey];
    if (def) attackerTotalHp += def.baseHp * count;
  }

  const rounds: RoundResult[] = [];
  let totalLosses: Record<string, number> = {};

  for (let round = 1; round <= MAX_COMBAT_ROUNDS; round++) {
    if (monsterHp <= 0) break;
    if (attackerTotalHp <= 0) break;

    const attackerStats = getAttackerStats(
      currentTroops,
      heroBuff,
      researchAttackBonus,
      researchDefenseBonus,
      round,
    );

    // RPS: attacker dominant class vs monster (null = neutral)
    const monsterClass = monster.combatClass ?? "infantry";
    const rpsAtk = getRpsForAttack(attackerStats.dominant, monsterClass);
    const rpsMonster = getRpsForAttack(monsterClass, attackerStats.dominant);

    const attackerDmg = calcDamage(
      attackerStats.totalAtk,
      monster.defense,
      rpsAtk,
    );
    const monsterDmg = calcDamage(
      monster.attack,
      attackerStats.totalDef,
      rpsMonster,
    );

    // Apply simultaneously
    monsterHp = Math.max(0, monsterHp - attackerDmg);
    attackerTotalHp = Math.max(0, attackerTotalHp - monsterDmg);

    // Distribute attacker losses proportionally by HP pool
    const lossesThisRound = distributeAttackerLosses(
      currentTroops,
      monsterDmg,
    );

    for (const [key, lost] of Object.entries(lossesThisRound)) {
      totalLosses[key] = (totalLosses[key] ?? 0) + lost;
      currentTroops[key] = Math.max(0, (currentTroops[key] ?? 0) - lost);
    }

    rounds.push({
      round,
      attackerDamageDealt: Math.floor(attackerDmg),
      defenderDamageDealt: Math.floor(monsterDmg),
      attackerHpAfter: Math.floor(attackerTotalHp),
      defenderHpAfter: Math.floor(monsterHp),
      attackerTroopsLostThisRound: lossesThisRound,
    });
  }

  const attackerWon = monsterHp <= 0;

  // Survivors
  const survived: Record<string, number> = {};
  for (const [key, count] of Object.entries(currentTroops)) {
    if (count > 0) survived[key] = count;
  }

  return {
    attackerWon,
    roundsFought: rounds.length,
    rounds,
    attackerTroopsSent: troopsSent,
    attackerTroopsSurvived: survived,
    attackerTotalLosses: totalLosses,
    monsterSurvivingHp: Math.floor(monsterHp),
  };
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Get RPS modifier for attacker class vs defender class.
 * Infantry > Cavalry (1.4x), Cavalry > Archer (1.4x), Archer > Infantry (1.4x).
 * Siege neutral (1.0x always).
 */
function getRpsForAttack(atk: TroopClass, def: TroopClass): number {
  if (atk === "siege") return 1.0;
  if (atk === "infantry" && def === "cavalry") return 1.4;
  if (atk === "cavalry"  && def === "archer")  return 1.4;
  if (atk === "archer"   && def === "infantry") return 1.4;
  if (atk === "infantry" && def === "archer")  return 0.7;
  if (atk === "cavalry"  && def === "infantry") return 0.7;
  if (atk === "archer"   && def === "cavalry") return 0.7;
  return 1.0;
}

/**
 * Distribute attacker losses proportionally across troop types.
 * Troops with higher HP pool absorb more damage.
 */
function distributeAttackerLosses(
  troops: Record<string, number>,
  totalDamage: number,
): Record<string, number> {
  if (totalDamage <= 0) return {};

  const troopHps: Record<string, number> = {};
  let totalHp = 0;
  for (const [key, count] of Object.entries(troops)) {
    if (count <= 0) continue;
    const def = TROOP_DEFINITIONS[key as TroopKey];
    if (!def) continue;
    const hp = def.baseHp * count;
    troopHps[key] = hp;
    totalHp += hp;
  }

  if (totalHp <= 0) return {};

  const losses: Record<string, number> = {};
  for (const [key, hp] of Object.entries(troopHps)) {
    const share = hp / totalHp;
    const dmgShare = totalDamage * share;
    const def = TROOP_DEFINITIONS[key as TroopKey];
    if (!def) continue;
    const currentCount = troops[key] ?? 0;
    const unitsLost = Math.min(
      Math.ceil(dmgShare / def.baseHp),
      currentCount,
    );
    if (unitsLost > 0) losses[key] = unitsLost;
  }

  return losses;
}

// ---------------------------------------------------------------------------
// Hospital split — applied after combat, not inside engine
// ---------------------------------------------------------------------------

export interface CasualtySplit {
  killed: Record<string, number>;
  wounded: Record<string, number>;
}

/**
 * Split total losses into killed vs wounded (field operation: 50/50).
 * Hospital overflow check is done by the caller.
 */
export function splitCasualties(
  losses: Record<string, number>,
  fieldOperation = true,
): CasualtySplit {
  const killed: Record<string, number> = {};
  const wounded: Record<string, number> = {};

  const deathRate = fieldOperation ? 0.5 : 0.0;

  for (const [key, count] of Object.entries(losses)) {
    if (count <= 0) continue;
    const dead = Math.floor(count * deathRate);
    const wound = count - dead;
    if (dead > 0) killed[key] = dead;
    if (wound > 0) wounded[key] = wound;
  }

  return { killed, wounded };
}

/**
 * Apply hospital capacity. Troops that exceed capacity die permanently.
 * Priority: T5 > T4 > T3 > T2 > T1 (higher tier saved first).
 */
export function applyHospitalCapacity(
  wounded: Record<string, number>,
  currentWounded: Record<string, number>,
  capacity: number,
): { admitted: Record<string, number>; overflow: Record<string, number> } {
  const totalCurrentWounded = Object.values(currentWounded).reduce(
    (s, v) => s + v, 0,
  );
  let remaining = Math.max(0, capacity - totalCurrentWounded);

  const tierOrder = [5, 4, 3, 2, 1];
  const admitted: Record<string, number> = {};
  const overflow: Record<string, number> = {};

  for (const tier of tierOrder) {
    for (const [key, count] of Object.entries(wounded)) {
      const parsed = parseTroopKey(key);
      if (!parsed || parsed.tier !== tier) continue;

      const admit = Math.min(count, remaining);
      if (admit > 0) admitted[key] = admit;
      const dead = count - admit;
      if (dead > 0) overflow[key] = dead;
      remaining -= admit;
    }
  }

  return { admitted, overflow };
}
