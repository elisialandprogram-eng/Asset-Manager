using UnityEngine;
using System;
using System.Collections;

namespace EternalKingdoms.Population
{
    /// <summary>
    /// CitizenController — Individual NPC citizen behaviour state machine.
    ///
    /// Phase 5.5 (U5.5.6) states:
    ///   Patrolling — walking between road waypoints via NavMeshAgent or direct lerp
    ///   Idle       — standing still, playing idle animation, random look-arounds
    ///   Working    — near a building/workstation, playing work anim (chop, hammer, tend)
    ///   Talking    — facing a partner citizen, playing talk animation
    ///   Sitting    — at a bench/campfire, playing sit animation
    ///
    /// Architecture:
    ///   - Uses Animator with parameters: "Speed" (float), "IsWorking" (bool),
    ///     "IsTalking" (bool), "IsSitting" (bool)
    ///   - Movement: CharacterController or direct Transform.position lerp
    ///     (NavMesh bake required for NavMeshAgent; fallback uses waypoint lerp)
    ///   - State duration randomised within Inspector ranges
    ///   - LOD: CitizenController coroutines pause when > 80u from camera
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CitizenController : MonoBehaviour
    {
        private enum State { Patrolling, Idle, Working, Talking, Sitting }

        private Animator   _anim;
        private State      _state;
        private Transform[] _waypoints;
        private Transform[] _workStations;
        private Transform[] _sitSpots;
        private int         _waypointIdx;
        private float       _idleMin;
        private float       _idleMax;
        private float       _talkDuration;
        private float       _walkSpeed;
        private CitizenType _type;
        private Transform   _talkTarget;
        private Camera      _mainCamera;
        private Coroutine   _behaviourCoroutine;

        private static readonly int s_Speed     = Animator.StringToHash("Speed");
        private static readonly int s_Working   = Animator.StringToHash("IsWorking");
        private static readonly int s_Talking   = Animator.StringToHash("IsTalking");
        private static readonly int s_Sitting   = Animator.StringToHash("IsSitting");

        public void Initialize(CitizenType type, Transform[] waypoints, Transform[] workStations,
                                Transform[] sitSpots, float idleMin, float idleMax,
                                float talkDuration, float walkSpeed)
        {
            _type         = type;
            _waypoints    = waypoints;
            _workStations = workStations;
            _sitSpots     = sitSpots;
            _idleMin      = idleMin;
            _idleMax      = idleMax;
            _talkDuration = talkDuration;
            _walkSpeed    = walkSpeed;
            _anim         = GetComponent<Animator>();
            _mainCamera   = Camera.main;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active)
            {
                if (_behaviourCoroutine != null) StopCoroutine(_behaviourCoroutine);
                _behaviourCoroutine = StartCoroutine(BehaviourLoop());
            }
            else
            {
                if (_behaviourCoroutine != null) StopCoroutine(_behaviourCoroutine);
            }
        }

        public void StartTalking(Transform partner)
        {
            _talkTarget = partner;
            _state = State.Talking;
        }

        public void StopTalking()
        {
            _talkTarget = null;
        }

        // ── Behaviour Loop ────────────────────────────────────────────────────

        private IEnumerator BehaviourLoop()
        {
            // Random entry point
            int roll = UnityEngine.Random.Range(0, 3);
            if      (roll == 0) _state = State.Patrolling;
            else if (roll == 1) _state = State.Idle;
            else                _state = State.Working;

            while (true)
            {
                // LOD: pause updates when far from camera
                if (_mainCamera != null &&
                    Vector3.Distance(transform.position, _mainCamera.transform.position) > 80f)
                {
                    SetAnimSpeed(0f);
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                switch (_state)
                {
                    case State.Patrolling: yield return DoPatrol();   break;
                    case State.Idle:       yield return DoIdle();     break;
                    case State.Working:    yield return DoWork();     break;
                    case State.Talking:    yield return DoTalk();     break;
                    case State.Sitting:    yield return DoSit();      break;
                }

                // State transition
                _state = PickNextState();
            }
        }

        private IEnumerator DoPatrol()
        {
            if (_waypoints == null || _waypoints.Length == 0) { yield return DoIdle(); yield break; }

            _waypointIdx = (_waypointIdx + 1) % _waypoints.Length;
            var target = _waypoints[_waypointIdx];
            SetAnimSpeed(_walkSpeed);

            while (Vector3.Distance(transform.position, target.position) > 0.3f)
            {
                if (_state == State.Talking) yield break;

                Vector3 dir = (target.position - transform.position).normalized;
                transform.position += dir * _walkSpeed * Time.deltaTime;
                transform.rotation  = Quaternion.Slerp(transform.rotation,
                                        Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                yield return null;
            }

            SetAnimSpeed(0f);
        }

        private IEnumerator DoIdle()
        {
            SetAnimSpeed(0f);
            _anim?.SetBool(s_Working, false);
            yield return new WaitForSeconds(UnityEngine.Random.Range(_idleMin, _idleMax));

            // Occasional look-around
            float lookAngle = UnityEngine.Random.Range(-45f, 45f);
            var startRot = transform.rotation;
            var endRot   = startRot * Quaternion.Euler(0f, lookAngle, 0f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
        }

        private IEnumerator DoWork()
        {
            if (_workStations == null || _workStations.Length == 0) { yield return DoIdle(); yield break; }

            var station = _workStations[UnityEngine.Random.Range(0, _workStations.Length)];
            SetAnimSpeed(_walkSpeed);

            // Walk to station
            float elapsed = 0f;
            while (Vector3.Distance(transform.position, station.position) > 0.5f && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                Vector3 dir = (station.position - transform.position).normalized;
                transform.position += dir * _walkSpeed * Time.deltaTime;
                transform.rotation  = Quaternion.Slerp(transform.rotation,
                                        Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                yield return null;
            }

            SetAnimSpeed(0f);
            _anim?.SetBool(s_Working, true);
            yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 12f));
            _anim?.SetBool(s_Working, false);
        }

        private IEnumerator DoTalk()
        {
            SetAnimSpeed(0f);
            _anim?.SetBool(s_Talking, true);

            float elapsed = 0f;
            while (_state == State.Talking && elapsed < _talkDuration + 2f)
            {
                elapsed += Time.deltaTime;
                if (_talkTarget != null)
                {
                    var dir = (_talkTarget.position - transform.position).normalized;
                    dir.y = 0f;
                    if (dir != Vector3.zero)
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(dir), 5f * Time.deltaTime);
                }
                yield return null;
            }

            _anim?.SetBool(s_Talking, false);
            _state = State.Idle;
        }

        private IEnumerator DoSit()
        {
            if (_sitSpots == null || _sitSpots.Length == 0) { yield return DoIdle(); yield break; }

            var spot = _sitSpots[UnityEngine.Random.Range(0, _sitSpots.Length)];
            transform.position = spot.position;
            transform.rotation = spot.rotation;

            SetAnimSpeed(0f);
            _anim?.SetBool(s_Sitting, true);
            yield return new WaitForSeconds(UnityEngine.Random.Range(8f, 15f));
            _anim?.SetBool(s_Sitting, false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetAnimSpeed(float speed)
        {
            _anim?.SetFloat(s_Speed, speed);
        }

        private State PickNextState()
        {
            if (_type == CitizenType.Soldier)
                return UnityEngine.Random.value < 0.7f ? State.Patrolling : State.Idle;

            float r = UnityEngine.Random.value;
            if (r < 0.40f) return State.Patrolling;
            if (r < 0.65f) return State.Working;
            if (r < 0.80f) return State.Idle;
            if (r < 0.92f) return State.Sitting;
            return State.Idle;
        }
    }
}
