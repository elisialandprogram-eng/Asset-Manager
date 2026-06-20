using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace EternalKingdoms.Monsters
{
    /// <summary>
    /// U5.7.5 — Advanced Monster Behaviour
    /// Full AI state machine: Idle / Patrol / Sleep / Roam / Investigate /
    /// CombatAlert / ReturnHome.
    /// Rare monsters (tier ≥ 4) patrol larger territories.
    /// Dragons (tier 5) perform periodic flight animations.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Monster Identity")]
        public MonsterCategory category = MonsterCategory.Bandit;
        [Range(1, 5)] public int tier   = 1;

        [Header("Territory")]
        [Tooltip("Home position — monster returns here after investigation.")]
        public Vector3 homePosition;
        [Tooltip("Base patrol radius (multiplied by tier for rare monsters).")]
        public float basePatrolRadius = 15f;
        [Tooltip("Rare tier threshold — monsters at or above this tier get 3× territory.")]
        public int rareTierThreshold  = 4;

        [Header("Behaviour Timings")]
        [Range(2f, 15f)] public float idleDuration     = 5f;
        [Range(5f, 30f)] public float sleepDuration    = 12f;
        [Range(10f, 60f)] public float roamInterval    = 20f;
        [Range(1f, 5f)] public float patrolWaitTime    = 2f;
        public float dragonFlightInterval              = 40f;

        [Header("Detection")]
        public float alertRadius = 20f;
        public LayerMask playerLayer;

        [Header("Animator")]
        public Animator animator;

        // ── State ─────────────────────────────────────────────────────────────
        public MonsterAIState CurrentState { get; private set; } = MonsterAIState.Idle;
        private NavMeshAgent   _agent;
        private float          _stateTimer;
        private Vector3        _investigateTarget;
        private bool           _playerDetected;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<MonsterAIState> OnStateChanged;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (homePosition == Vector3.zero) homePosition = transform.position;
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            _agent.speed        = BaseSpeed();
            _agent.stoppingDistance = 1.5f;
            EnterState(MonsterAIState.Idle);

            if (category == MonsterCategory.Dragon)
                StartCoroutine(DragonFlightRoutine());
        }

        private void Update()
        {
            _stateTimer += Time.deltaTime;
            TickState();
            ScanForPlayer();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  State Machine
        // ─────────────────────────────────────────────────────────────────────
        private void EnterState(MonsterAIState state)
        {
            CurrentState = state;
            _stateTimer  = 0f;
            OnStateChanged?.Invoke(state);
            SetAnimatorState(state);

            switch (state)
            {
                case MonsterAIState.Idle:
                    _agent.isStopped = true;
                    break;

                case MonsterAIState.Patrol:
                    _agent.isStopped = false;
                    MoveToPatrolPoint();
                    break;

                case MonsterAIState.Sleep:
                    _agent.isStopped = true;
                    break;

                case MonsterAIState.Roam:
                    _agent.isStopped = false;
                    MoveToRoamPoint();
                    break;

                case MonsterAIState.Investigate:
                    _agent.isStopped = false;
                    _agent.SetDestination(_investigateTarget);
                    break;

                case MonsterAIState.CombatAlert:
                    _agent.isStopped = true;
                    StartCoroutine(CombatAlertPulse());
                    break;

                case MonsterAIState.ReturnHome:
                    _agent.isStopped = false;
                    _agent.SetDestination(homePosition);
                    break;
            }
        }

        private void TickState()
        {
            switch (CurrentState)
            {
                case MonsterAIState.Idle:
                    if (_stateTimer > idleDuration)
                        EnterState(Random.value < 0.4f ? MonsterAIState.Sleep : MonsterAIState.Patrol);
                    break;

                case MonsterAIState.Sleep:
                    if (_stateTimer > sleepDuration)
                        EnterState(MonsterAIState.Roam);
                    break;

                case MonsterAIState.Patrol:
                    if (_agent.remainingDistance < 1f && !_agent.pathPending)
                    {
                        if (_stateTimer > patrolWaitTime)
                        {
                            // Loop patrol or return to idle
                            EnterState(Random.value < 0.5f ? MonsterAIState.Idle : MonsterAIState.Patrol);
                        }
                    }
                    break;

                case MonsterAIState.Roam:
                    if (_stateTimer > roamInterval)
                        EnterState(MonsterAIState.ReturnHome);
                    if (_agent.remainingDistance < 1f && !_agent.pathPending)
                        MoveToRoamPoint();
                    break;

                case MonsterAIState.Investigate:
                    if (_agent.remainingDistance < 2f && !_agent.pathPending)
                        EnterState(MonsterAIState.CombatAlert);
                    if (_stateTimer > 15f) // Give up
                        EnterState(MonsterAIState.ReturnHome);
                    break;

                case MonsterAIState.CombatAlert:
                    // Remain alert briefly then return
                    if (_stateTimer > 8f && !_playerDetected)
                        EnterState(MonsterAIState.ReturnHome);
                    break;

                case MonsterAIState.ReturnHome:
                    if (Vector3.Distance(transform.position, homePosition) < 2f)
                        EnterState(MonsterAIState.Idle);
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Detection
        // ─────────────────────────────────────────────────────────────────────
        private void ScanForPlayer()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius, playerLayer);
            bool wasDetected = _playerDetected;
            _playerDetected = hits.Length > 0;

            if (_playerDetected && !wasDetected)
            {
                // Player entered alert radius
                _investigateTarget = hits[0].transform.position;
                if (CurrentState != MonsterAIState.CombatAlert && CurrentState != MonsterAIState.Investigate)
                    EnterState(MonsterAIState.Investigate);
            }
            else if (!_playerDetected && wasDetected)
            {
                // Player left radius
                if (CurrentState == MonsterAIState.CombatAlert)
                    EnterState(MonsterAIState.ReturnHome);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Movement
        // ─────────────────────────────────────────────────────────────────────
        private void MoveToPatrolPoint()
        {
            Vector3 point = RandomPointInTerritory(basePatrolRadius);
            _agent.SetDestination(point);
        }

        private void MoveToRoamPoint()
        {
            float roamRadius = IsRare() ? basePatrolRadius * 3f : basePatrolRadius * 1.5f;
            Vector3 point = RandomPointInTerritory(roamRadius);
            _agent.SetDestination(point);
        }

        private Vector3 RandomPointInTerritory(float radius)
        {
            Vector2 circle = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = homePosition + new Vector3(circle.x, 0, circle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, radius, NavMesh.AllAreas))
                return hit.position;
            return homePosition;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Dragon Flight
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator DragonFlightRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(dragonFlightInterval);
                StartCoroutine(PerformFlightAnimation());
            }
        }

        private IEnumerator PerformFlightAnimation()
        {
            animator?.SetBool("IsFlying", true);
            _agent.isStopped = true;

            // Ascend
            float elapsed = 0f, ascendDur = 3f;
            Vector3 groundPos = transform.position;
            while (elapsed < ascendDur)
            {
                elapsed += Time.deltaTime;
                transform.position = groundPos + Vector3.up * Mathf.Lerp(0, 40f, elapsed / ascendDur);
                yield return null;
            }

            // Fly a circle
            float flyDur = 15f;
            elapsed = 0f;
            float radius = IsRare() ? 60f : 30f;
            Vector3 center = transform.position;
            while (elapsed < flyDur)
            {
                elapsed += Time.deltaTime;
                float angle = elapsed / flyDur * 360f * Mathf.Deg2Rad;
                transform.position = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(elapsed * 0.5f) * 8f, Mathf.Sin(angle) * radius);
                transform.LookAt(center);
                yield return null;
            }

            // Descend
            elapsed = 0f;
            Vector3 airPos = transform.position;
            while (elapsed < ascendDur)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(airPos, groundPos, elapsed / ascendDur);
                yield return null;
            }

            transform.position = groundPos;
            animator?.SetBool("IsFlying", false);
            _agent.isStopped = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Combat Alert VFX
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator CombatAlertPulse()
        {
            // Flash selection ring + alert icon
            VFX.AlphaVFXController.Instance?.PlayEntitySelectedPulse(transform.position + Vector3.up);
            yield return new WaitForSeconds(1f);
            VFX.AlphaVFXController.Instance?.PlayEntitySelectedPulse(transform.position + Vector3.up);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Animator
        // ─────────────────────────────────────────────────────────────────────
        private void SetAnimatorState(MonsterAIState state)
        {
            if (animator == null) return;
            animator.SetFloat("Speed", (state == MonsterAIState.Patrol || state == MonsterAIState.Roam
                || state == MonsterAIState.Investigate || state == MonsterAIState.ReturnHome)
                ? _agent.speed : 0f);
            animator.SetBool("IsSleeping", state == MonsterAIState.Sleep);
            animator.SetBool("IsAlert",    state == MonsterAIState.CombatAlert || state == MonsterAIState.Investigate);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────
        private bool IsRare()    => tier >= rareTierThreshold;
        private bool IsDragon()  => category == MonsterCategory.Dragon;

        private float BaseSpeed() => category switch
        {
            MonsterCategory.DireWolf => 4.5f,
            MonsterCategory.Dragon   => 3.0f,
            _                        => 2.5f + tier * 0.3f
        };

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, alertRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(homePosition, basePatrolRadius);
        }
    }

    // ── Enums ─────────────────────────────────────────────────────────────────
    public enum MonsterAIState
    {
        Idle, Patrol, Sleep, Roam, Investigate, CombatAlert, ReturnHome
    }
}
