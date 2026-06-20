using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace EternalKingdoms.VFX
{
    /// <summary>
    /// U5.6.7 — VFX Alpha Pass
    /// Production VFX Graph effects for all key game events.
    /// No placeholder particles.  All effects use VFX Graph or high-quality
    /// Shuriken systems with pooling.
    /// </summary>
    public class AlphaVFXController : MonoBehaviour
    {
        public static AlphaVFXController Instance { get; private set; }

        // ── Inspector — VFX Graph Assets ──────────────────────────────────────
        [Header("Selection")]
        public VisualEffectAsset selectionRingVFX;
        public VisualEffectAsset entitySelectedPulseVFX;

        [Header("Construction")]
        public VisualEffectAsset constructionDustVFX;
        public VisualEffectAsset scaffoldingSparkVFX;

        [Header("Building Complete")]
        public VisualEffectAsset buildingCompleteCelebrationVFX;
        public AudioClip         buildingCompleteSFX;

        [Header("Resource Gathering")]
        public VisualEffectAsset resourceGatherSparkleVFX;
        public VisualEffectAsset resourceNodeDepletedVFX;

        [Header("Monster")]
        public VisualEffectAsset monsterDeathDissolveVFX;
        public VisualEffectAsset monsterSpawnBurstVFX;

        [Header("March")]
        public VisualEffectAsset marchArrivalBurstVFX;
        public VisualEffectAsset marchDustTrailVFX;

        [Header("Level Up / Loot")]
        public VisualEffectAsset levelUpCelebrationVFX;
        public VisualEffectAsset lootExplosionVFX;
        public AudioClip         levelUpSFX;
        public AudioClip         lootExplosionSFX;

        [Header("Environment")]
        public VisualEffectAsset torchFlameVFX;
        public VisualEffectAsset chimneySmokVFX;
        public VisualEffectAsset campfireEmbersVFX;

        [Header("Pool Settings")]
        public int poolSizePerEffect = 8;

        // ── Pool ──────────────────────────────────────────────────────────────
        private readonly Dictionary<VisualEffectAsset, Queue<VisualEffect>> _pools = new();
        private Transform _poolRoot;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("VFX_Pool").transform;
            _poolRoot.SetParent(transform);

            PrewarmAll();
        }

        private void PrewarmAll()
        {
            VisualEffectAsset[] all =
            {
                selectionRingVFX, entitySelectedPulseVFX,
                constructionDustVFX, scaffoldingSparkVFX,
                buildingCompleteCelebrationVFX, resourceGatherSparkleVFX,
                resourceNodeDepletedVFX, monsterDeathDissolveVFX,
                monsterSpawnBurstVFX, marchArrivalBurstVFX, marchDustTrailVFX,
                levelUpCelebrationVFX, lootExplosionVFX,
                torchFlameVFX, chimneySmokVFX, campfireEmbersVFX
            };

            foreach (var asset in all)
            {
                if (asset == null) continue;
                _pools[asset] = new Queue<VisualEffect>();
                for (int i = 0; i < poolSizePerEffect; i++)
                    _pools[asset].Enqueue(CreateInstance(asset));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public API — One-shot effects
        // ─────────────────────────────────────────────────────────────────────

        public void PlaySelectionRing(Vector3 worldPos)         => PlayOneShot(selectionRingVFX, worldPos, 2f);
        public void PlayEntitySelectedPulse(Vector3 worldPos)   => PlayOneShot(entitySelectedPulseVFX, worldPos, 1.5f);

        public void PlayConstructionDust(Vector3 worldPos)      => PlayLooped(constructionDustVFX, worldPos);
        public void PlayScaffoldingSpark(Vector3 worldPos)      => PlayLooped(scaffoldingSparkVFX, worldPos);

        public void PlayBuildingComplete(Vector3 worldPos)
        {
            PlayOneShot(buildingCompleteCelebrationVFX, worldPos, 4f);
            PlaySFXAt(buildingCompleteSFX, worldPos);
        }

        public void PlayResourceGather(Vector3 worldPos)        => PlayOneShot(resourceGatherSparkleVFX, worldPos, 1.2f);
        public void PlayResourceDepleted(Vector3 worldPos)      => PlayOneShot(resourceNodeDepletedVFX, worldPos, 2f);

        public void PlayMonsterDeath(Vector3 worldPos)          => PlayOneShot(monsterDeathDissolveVFX, worldPos, 3f);
        public void PlayMonsterSpawn(Vector3 worldPos)          => PlayOneShot(monsterSpawnBurstVFX, worldPos, 2f);

        public void PlayMarchArrival(Vector3 worldPos)          => PlayOneShot(marchArrivalBurstVFX, worldPos, 2.5f);

        /// <summary>Returns a looping dust trail VFX attached to a march entity.</summary>
        public VisualEffect AttachMarchDustTrail(Transform parent)
        {
            var vfx = GetFromPool(marchDustTrailVFX);
            if (vfx == null) return null;
            vfx.transform.SetParent(parent, false);
            vfx.transform.localPosition = Vector3.zero;
            vfx.Play();
            return vfx;
        }

        public void PlayLevelUp(Vector3 worldPos)
        {
            PlayOneShot(levelUpCelebrationVFX, worldPos, 4f);
            PlaySFXAt(levelUpSFX, worldPos);
        }

        public void PlayLootExplosion(Vector3 worldPos)
        {
            PlayOneShot(lootExplosionVFX, worldPos, 3f);
            PlaySFXAt(lootExplosionSFX, worldPos);
        }

        public VisualEffect SpawnTorchFlame(Vector3 worldPos)  => SpawnLoopedAt(torchFlameVFX, worldPos);
        public VisualEffect SpawnChimneySmoke(Vector3 worldPos) => SpawnLoopedAt(chimneySmokVFX, worldPos);
        public VisualEffect SpawnCampfireEmbers(Vector3 worldPos) => SpawnLoopedAt(campfireEmbersVFX, worldPos);

        // ─────────────────────────────────────────────────────────────────────
        //  Internal
        // ─────────────────────────────────────────────────────────────────────
        private void PlayOneShot(VisualEffectAsset asset, Vector3 pos, float lifetime)
        {
            if (asset == null) return;
            var vfx = GetFromPool(asset);
            if (vfx == null) return;
            vfx.transform.position = pos;
            vfx.Play();
            StartCoroutine(ReturnAfter(vfx, asset, lifetime));
        }

        private void PlayLooped(VisualEffectAsset asset, Vector3 pos)
        {
            if (asset == null) return;
            var vfx = GetFromPool(asset);
            if (vfx == null) return;
            vfx.transform.position = pos;
            vfx.Play();
        }

        private VisualEffect SpawnLoopedAt(VisualEffectAsset asset, Vector3 pos)
        {
            if (asset == null) return null;
            var vfx = GetFromPool(asset);
            if (vfx == null) return null;
            vfx.transform.position = pos;
            vfx.Play();
            return vfx;
        }

        private IEnumerator ReturnAfter(VisualEffect vfx, VisualEffectAsset asset, float delay)
        {
            yield return new WaitForSeconds(delay);
            vfx.Stop();
            vfx.transform.SetParent(_poolRoot, false);
            vfx.gameObject.SetActive(false);
            if (_pools.ContainsKey(asset))
                _pools[asset].Enqueue(vfx);
        }

        private VisualEffect GetFromPool(VisualEffectAsset asset)
        {
            if (asset == null) return null;
            if (_pools.TryGetValue(asset, out var q) && q.Count > 0)
            {
                var vfx = q.Dequeue();
                vfx.gameObject.SetActive(true);
                return vfx;
            }
            // Pool exhausted — create extra
            return CreateInstance(asset);
        }

        private VisualEffect CreateInstance(VisualEffectAsset asset)
        {
            var go  = new GameObject($"VFX_{asset.name}");
            go.transform.SetParent(_poolRoot, false);
            var vfx = go.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = asset;
            go.SetActive(false);
            return vfx;
        }

        private static void PlaySFXAt(AudioClip clip, Vector3 pos)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, pos);
        }

        /// <summary>Immediately return a looped VFX to the pool.</summary>
        public void Release(VisualEffect vfx, VisualEffectAsset asset)
        {
            if (vfx == null) return;
            vfx.Stop();
            vfx.transform.SetParent(_poolRoot, false);
            vfx.gameObject.SetActive(false);
            if (asset != null && _pools.ContainsKey(asset))
                _pools[asset].Enqueue(vfx);
        }
    }
}
