using UnityEngine;
using System;
using System.Collections;

namespace EternalKingdoms.Monsters
{
    /// <summary>
    /// MonsterVisualController — Drives visual state for all monster types.
    ///
    /// Phase 5 (U5.6) monster categories:
    ///   Bandits · Dire Wolves · Ogres · Ancient Guardians · Dragons
    ///
    /// Each category has:
    ///   - Idle animation (looping, subtle movement)
    ///   - Spawn effect (emerge from ground / portal open)
    ///   - Selection effect (selection ring + name plate glow)
    ///   - Death effect (dissolve shader + debris particles)
    ///
    /// LOD tiers:
    ///   LOD0 (< 50u)  — full rig + all particles
    ///   LOD1 (< 150u) — simplified mesh, billboard particles
    ///   LOD2 (< 300u) — impostor sprite
    ///   Culled         — > 300u
    ///
    /// Architecture:
    ///   - Attached to MonsterEntity prefab root
    ///   - State driven by MonsterSpawnManager / EntitySelectionManager
    ///   - Tier drives scale and VFX color (T1=grey, T2=green, T3=blue, T4=purple, T5=red/gold)
    ///   - Death triggers dissolve shader then deactivates entity
    /// </summary>
    public class MonsterVisualController : MonoBehaviour
    {
        public enum MonsterState { Idle, Spawning, Selected, Dying, Dead }

        [Header("Animation")]
        [SerializeField] private Animator monsterAnimator;
#pragma warning disable CS0414
        [SerializeField] private string idleTrigger    = "Idle";
#pragma warning restore CS0414
        [SerializeField] private string spawnTrigger   = "Spawn";
        [SerializeField] private string dieTrigger     = "Die";

        [Header("Renderers")]
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private LODGroup lodGroup;

        [Header("VFX")]
        [SerializeField] private ParticleSystem spawnVFX;
        [SerializeField] private ParticleSystem deathDebrisVFX;
        [SerializeField] private ParticleSystem selectionRingVFX;
        [SerializeField] private ParticleSystem ambientAuraVFX;
        [SerializeField] private Light          ambientLight;

        [Header("Selection")]
        [SerializeField] private GameObject selectionRingObject;
        [SerializeField] private float      selectionPulseSpeed = 2f;

        [Header("Dissolve Shader")]
        [SerializeField] private float dissolveDuration = 1.8f;

        [Header("Tier Color Tints")]
        [SerializeField] private Color tierColorT1 = new Color(0.7f, 0.7f, 0.7f);  // grey
        [SerializeField] private Color tierColorT2 = new Color(0.4f, 0.9f, 0.4f);  // green
        [SerializeField] private Color tierColorT3 = new Color(0.3f, 0.6f, 1.0f);  // blue
        [SerializeField] private Color tierColorT4 = new Color(0.7f, 0.3f, 1.0f);  // purple
        [SerializeField] private Color tierColorT5 = new Color(1.0f, 0.4f, 0.1f);  // red/gold

        private MonsterState _state;
        private static readonly int s_DissolveAmount = Shader.PropertyToID("_DissolveAmount");
        private static readonly int s_TierColor      = Shader.PropertyToID("_TierColor");
        private MaterialPropertyBlock _propBlock;
        private Coroutine _selectionPulseCoroutine;
        private Coroutine _dissolveCoroutine;

        public event Action OnDeathComplete;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            SetDissolve(0f);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetTier(int tier)
        {
            var color = tier switch
            {
                1 => tierColorT1,
                2 => tierColorT2,
                3 => tierColorT3,
                4 => tierColorT4,
                _ => tierColorT5,
            };

            float scale = 0.8f + (tier - 1) * 0.15f;
            transform.localScale = Vector3.one * scale;

            foreach (var rend in bodyRenderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(s_TierColor, color);
                rend.SetPropertyBlock(_propBlock);
            }

            if (ambientLight != null)
            {
                ambientLight.color     = color;
                ambientLight.intensity = 0.3f + (tier - 1) * 0.1f;
            }
        }

        public void PlaySpawn()
        {
            _state = MonsterState.Spawning;
            monsterAnimator?.SetTrigger(spawnTrigger);
            if (spawnVFX != null) { spawnVFX.Stop(); spawnVFX.Play(); }
            if (ambientAuraVFX != null) ambientAuraVFX.Play();
        }

        public void SetSelected(bool selected)
        {
            if (selectionRingObject != null) selectionRingObject.SetActive(selected);

            if (selected)
            {
                if (selectionRingVFX != null) selectionRingVFX.Play();
                if (_selectionPulseCoroutine != null) StopCoroutine(_selectionPulseCoroutine);
                _selectionPulseCoroutine = StartCoroutine(PulseSelection());
                _state = MonsterState.Selected;
            }
            else
            {
                if (selectionRingVFX != null) selectionRingVFX.Stop();
                if (_selectionPulseCoroutine != null) StopCoroutine(_selectionPulseCoroutine);
                _state = MonsterState.Idle;
            }
        }

        public void PlayDeath()
        {
            if (_state == MonsterState.Dying || _state == MonsterState.Dead) return;
            _state = MonsterState.Dying;

            monsterAnimator?.SetTrigger(dieTrigger);
            if (deathDebrisVFX != null) deathDebrisVFX.Play();

            if (_dissolveCoroutine != null) StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = StartCoroutine(DissolveOut());
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator PulseSelection()
        {
            while (true)
            {
                float s = (Mathf.Sin(Time.time * selectionPulseSpeed) + 1f) * 0.5f;
                if (selectionRingObject != null)
                {
                    var rend = selectionRingObject.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.GetPropertyBlock(_propBlock);
                        _propBlock.SetFloat("_Intensity", Mathf.Lerp(0.5f, 1.5f, s));
                        rend.SetPropertyBlock(_propBlock);
                    }
                }
                yield return null;
            }
        }

        private IEnumerator DissolveOut()
        {
            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                SetDissolve(elapsed / dissolveDuration);
                yield return null;
            }
            SetDissolve(1f);
            _state = MonsterState.Dead;
            OnDeathComplete?.Invoke();
            gameObject.SetActive(false);
        }

        private void SetDissolve(float amount)
        {
            foreach (var rend in bodyRenderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(s_DissolveAmount, amount);
                rend.SetPropertyBlock(_propBlock);
            }
        }
    }
}
