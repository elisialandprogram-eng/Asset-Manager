using UnityEngine;
using System;
using System.Collections;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// BuildingVisualState — Per-slot state machine for building visual presentation.
    ///
    /// Phase 5 (U5.4) states:
    ///   EmptyLot      — bare ground, foundation placeholder
    ///   Foundation    — foundation mesh visible, no building
    ///   Constructing  — scaffolding + animated construction progress
    ///   Complete(N)   — level N building mesh, idle animations active
    ///
    /// Architecture:
    ///   - Added as component to each building slot Transform
    ///   - Prefab instances managed per state (pooled from KingdomVisualController)
    ///   - Completion VFX fires once on transition to Complete
    ///   - Idle animation: building flag sway, torch flicker, smoke — driven by Animators
    ///   - All prefab loading via AssetCatalogManager (Addressables)
    /// </summary>
    public class BuildingVisualState : MonoBehaviour
    {
        public enum State { EmptyLot, Foundation, Constructing, Complete }

        private BuildingVisualData _data;
        private State _currentState = State.EmptyLot;
        private int _currentLevel;
        private GameObject _activeInstance;
        private ParticleSystem _activeVFX;

        public State CurrentState => _currentState;
        public int CurrentLevel => _currentLevel;

        public event Action<State, int> OnStateChanged;

        public void Initialize(BuildingVisualData data)
        {
            _data = data;
            ShowEmptyLot();
        }

        public void TransitionTo(State newState, int level = 1)
        {
            if (newState == _currentState && level == _currentLevel) return;

            _currentState = newState;
            _currentLevel = level;

            DestroyActive();

            switch (newState)
            {
                case State.EmptyLot:    ShowEmptyLot();            break;
                case State.Foundation:  ShowFoundation();          break;
                case State.Constructing:ShowConstructing(level);   break;
                case State.Complete:    ShowComplete(level);        break;
            }

            OnStateChanged?.Invoke(newState, level);
        }

        // ── State Visuals ──────────────────────────────────────────────────────

        private void ShowEmptyLot()
        {
            if (_data?.emptyLotPrefab != null)
                _activeInstance = Spawn(_data.emptyLotPrefab);
        }

        private void ShowFoundation()
        {
            if (_data?.foundationPrefab != null)
                _activeInstance = Spawn(_data.foundationPrefab);
        }

        private void ShowConstructing(int level)
        {
            if (_data == null) return;

            var stages = _data.constructionStagePrefabs;
            if (stages != null && stages.Length > 0)
            {
                int idx = Mathf.Clamp(level - 1, 0, stages.Length - 1);
                if (stages[idx] != null)
                    _activeInstance = Spawn(stages[idx]);
            }

            // Dust VFX
            if (_data.constructionDustVFX != null)
            {
                _activeVFX = Instantiate(_data.constructionDustVFX, transform.position, Quaternion.identity, transform);
                _activeVFX.Play();
            }
        }

        private void ShowComplete(int level)
        {
            if (_data == null) return;

            var levels = _data.completedLevelPrefabs;
            if (levels != null && levels.Length > 0)
            {
                int idx = Mathf.Clamp(level - 1, 0, levels.Length - 1);
                if (levels[idx] != null)
                    _activeInstance = Spawn(levels[idx]);
            }

            // One-shot completion VFX
            if (_data.completionVFX != null)
                StartCoroutine(PlayCompletionVFX());

            // Enable idle animations
            var animator = _activeInstance?.GetComponentInChildren<Animator>();
            if (animator != null)
                animator.SetTrigger("Idle");
        }

        private IEnumerator PlayCompletionVFX()
        {
            yield return new WaitForSeconds(0.1f);
            var vfx = Instantiate(_data.completionVFX, transform.position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + 0.5f);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private GameObject Spawn(GameObject prefab)
        {
            return Instantiate(prefab, transform.position, transform.rotation, transform);
        }

        private void DestroyActive()
        {
            if (_activeInstance != null) { Destroy(_activeInstance); _activeInstance = null; }
            if (_activeVFX != null)      { Destroy(_activeVFX.gameObject); _activeVFX = null; }
        }
    }
}
