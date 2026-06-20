using UnityEngine;
using System.Collections;

namespace EternalKingdoms.World
{
    /// <summary>
    /// ResourceNodeVisual — Drives visual state for all resource node types.
    ///
    /// Phase 5 (U5.5) node types:
    ///   Farm · Lumber Camp · Stone Quarry · Iron Mine · Gold Deposit · Crystal Cluster
    ///
    /// Visual states:
    ///   Idle       — full resource, looping idle animation + ambient particles
    ///   Harvesting — march present; intensified activity animation + harvest VFX
    ///   Depleted   — exhausted; desaturated, reduced particles, "recharging" shader pulse
    ///
    /// Crystal Cluster special:
    ///   - Emissive shader driven by sin-wave (animate between 0.5–2.0 emission)
    ///   - Particle system: glowing crystal shards floating upward
    ///   - Color shifts based on crystal tier (common=blue, rare=violet, epic=gold)
    ///
    /// Architecture:
    ///   - Attached to ResourceNodeEntity prefab root
    ///   - State driven by ResourceNodeManager when march attaches/detaches
    ///   - Animator parameters: "Idle", "Harvesting", "Depleted" triggers
    ///   - LODGroup configured: LOD0 full detail, LOD1 simplified, LOD2 impostor billboard
    /// </summary>
    public class ResourceNodeVisual : MonoBehaviour
    {
        public enum NodeState { Idle, Harvesting, Depleted }
        public enum NodeType  { Farm, LumberCamp, StoneQuarry, IronMine, GoldDeposit, CrystalCluster }

        [Header("Node Identity")]
        [SerializeField] private NodeType nodeType;

        [Header("Renderers")]
        [SerializeField] private Renderer[] mainRenderers;
        [SerializeField] private Renderer crystalRenderer;

        [Header("Animator")]
        [SerializeField] private Animator nodeAnimator;

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem idleParticles;
        [SerializeField] private ParticleSystem harvestParticles;
        [SerializeField] private ParticleSystem depletedParticles;
        [SerializeField] private ParticleSystem crystalFloatParticles;

        [Header("Crystal Settings")]
        [SerializeField] private float crystalEmissiveMin  = 0.5f;
        [SerializeField] private float crystalEmissiveMax  = 2.0f;
        [SerializeField] private float crystalPulseSpeed   = 1.2f;
        [SerializeField] private Color crystalColorCommon  = new Color(0.3f, 0.5f, 1.0f);
        [SerializeField] private Color crystalColorRare    = new Color(0.7f, 0.2f, 1.0f);
        [SerializeField] private Color crystalColorEpic    = new Color(1.0f, 0.8f, 0.1f);

        [Header("Depletion")]
        [SerializeField] private float depletedSaturation = 0.2f;

        private NodeState _currentState;
        private static readonly int s_EmissiveColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int s_Saturation    = Shader.PropertyToID("_Saturation");
        private Coroutine _crystalPulseCoroutine;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            SetState(NodeState.Idle);
        }

        private void OnDestroy()
        {
            if (_crystalPulseCoroutine != null) StopCoroutine(_crystalPulseCoroutine);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetState(NodeState newState)
        {
            if (newState == _currentState) return;
            _currentState = newState;
            ApplyState(newState);
        }

        public void SetCrystalTier(string tier)
        {
            if (nodeType != NodeType.CrystalCluster || crystalRenderer == null) return;
            var color = tier switch
            {
                "rare"  => crystalColorRare,
                "epic"  => crystalColorEpic,
                _       => crystalColorCommon,
            };
            crystalRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(s_EmissiveColor, color);
            crystalRenderer.SetPropertyBlock(_propBlock);

            if (crystalFloatParticles != null)
            {
                var main = crystalFloatParticles.main;
                main.startColor = color;
            }
        }

        // ── State Machine ─────────────────────────────────────────────────────

        private void ApplyState(NodeState state)
        {
            // Animator
            if (nodeAnimator != null)
            {
                nodeAnimator.SetTrigger("Idle");
                nodeAnimator.SetBool("Harvesting", state == NodeState.Harvesting);
                nodeAnimator.SetBool("Depleted",   state == NodeState.Depleted);
            }

            // Particles
            SetParticles(idleParticles,     state == NodeState.Idle);
            SetParticles(harvestParticles,  state == NodeState.Harvesting);
            SetParticles(depletedParticles, state == NodeState.Depleted);

            // Crystal pulse
            if (nodeType == NodeType.CrystalCluster)
            {
                if (_crystalPulseCoroutine != null) StopCoroutine(_crystalPulseCoroutine);
                if (state != NodeState.Depleted)
                    _crystalPulseCoroutine = StartCoroutine(PulseCrystalEmissive(state));
                else
                    SetCrystalEmissive(crystalEmissiveMin * 0.3f);
            }

            // Desaturation on depletion
            foreach (var rend in mainRenderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(s_Saturation, state == NodeState.Depleted ? depletedSaturation : 1f);
                rend.SetPropertyBlock(_propBlock);
            }
        }

        private IEnumerator PulseCrystalEmissive(NodeState state)
        {
            float baseMin = state == NodeState.Harvesting
                ? crystalEmissiveMin * 1.5f
                : crystalEmissiveMin;
            float baseMax = state == NodeState.Harvesting
                ? crystalEmissiveMax * 1.5f
                : crystalEmissiveMax;

            while (true)
            {
                float t = (Mathf.Sin(Time.time * crystalPulseSpeed) + 1f) * 0.5f;
                SetCrystalEmissive(Mathf.Lerp(baseMin, baseMax, t));
                yield return null;
            }
        }

        private void SetCrystalEmissive(float intensity)
        {
            if (crystalRenderer == null) return;
            crystalRenderer.GetPropertyBlock(_propBlock);
            var current = _propBlock.GetColor(s_EmissiveColor);
            _propBlock.SetColor(s_EmissiveColor, current.linear * intensity);
            crystalRenderer.SetPropertyBlock(_propBlock);
        }

        private static void SetParticles(ParticleSystem ps, bool active)
        {
            if (ps == null) return;
            if (active && !ps.isPlaying) ps.Play();
            else if (!active && ps.isPlaying) ps.Stop();
        }
    }
}
