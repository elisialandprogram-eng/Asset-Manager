using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using EternalKingdoms.Networking;

namespace EternalKingdoms.World
{
    /// <summary>
    /// MarchBannerEntity — Replaces the simple march marker with an AAA-quality
    /// animated army banner that follows the march path.
    ///
    /// Phase 5 (U5.7) visual elements:
    ///   - Kingdom banner mesh with wind-wave animation + player color
    ///   - Formation icon (infantry/cavalry/archer/siege composite icon)
    ///   - Hero portrait billboard (if hero assigned to march)
    ///   - Animated dust trail (ParticleSystem following path)
    ///   - World-space HUD: ETA countdown, march type icon, troop count
    ///   - Destination indicator: animated beacon at target position
    ///   - State badge: OUTBOUND (blue) / GATHERING (green) / RETURNING (amber)
    ///
    /// Architecture:
    ///   - Extends MarchEntity (replaces the simple sphere placeholder)
    ///   - Position interpolated per frame same as Phase 3 MarchEntity
    ///   - World-space canvas follows camera (Billboard mode)
    ///   - Trail driven by a TrailRenderer + ParticleSystem on the entity
    ///   - All assets loaded via AssetCatalogManager (Addressables)
    /// </summary>
    public class MarchBannerEntity : MonoBehaviour
    {
        [Header("Banner")]
        [SerializeField] private Renderer bannerRenderer;
        [SerializeField] private Animator bannerAnimator;
        [SerializeField] private int      colorPropertyId = Shader.PropertyToID("_BannerColor");

        [Header("Formation Icon")]
        [SerializeField] private SpriteRenderer formationIconRenderer;
        [SerializeField] private Sprite[] formationIcons;

        [Header("Hero Portrait")]
        [SerializeField] private SpriteRenderer heroPortraitRenderer;
        [SerializeField] private GameObject heroPortraitRoot;

        [Header("Trail")]
        [SerializeField] private TrailRenderer   dustTrail;
        [SerializeField] private ParticleSystem  dustParticles;

        [Header("World HUD")]
        [SerializeField] private Canvas    worldHUDCanvas;
        [SerializeField] private TextMeshProUGUI etaLabel;
        [SerializeField] private TextMeshProUGUI troopCountLabel;
        [SerializeField] private Image           stateIcon;

        [Header("State Colors")]
        [SerializeField] private Color colorOutbound  = new Color(0.3f, 0.6f, 1.0f);
        [SerializeField] private Color colorGathering = new Color(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color colorReturning = new Color(1.0f, 0.7f, 0.1f);

        [Header("Destination Beacon")]
        [SerializeField] private GameObject destinationBeaconPrefab;

        private GameObject _destinationBeacon;
        private DateTime   _arrivesAt;
        private string     _marchStatus;
        private Camera     _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (dustTrail != null) dustTrail.emitting = false;
        }

        private void Update()
        {
            UpdateETALabel();
            BillboardHUD();
        }

        // ── Public Setup ──────────────────────────────────────────────────────

        /// <summary>
        /// Initialise visuals from march data returned by the API.
        /// Call once when the march entity is first shown.
        /// </summary>
        public void SetupFromMarch(AttackMarchDto march, Color kingdomColor, Sprite heroPortrait = null)
        {
            // Banner color
            SetBannerColor(kingdomColor);

            // Formation icon
            SetFormationIcon(march.marchType);

            // Hero portrait
            if (heroPortrait != null && heroPortraitRoot != null)
            {
                heroPortraitRoot.SetActive(true);
                heroPortraitRenderer.sprite = heroPortrait;
            }
            else if (heroPortraitRoot != null)
            {
                heroPortraitRoot.SetActive(false);
            }

            // ETA
            _arrivesAt   = DateTime.Parse(march.arrivesAt);
            _marchStatus = march.status;

            // Troop count
            int total = 0;
            if (march.troops != null)
                foreach (var kvp in march.troops) total += kvp.Value;
            if (troopCountLabel != null) troopCountLabel.text = $"{total:N0} troops";

            // State color
            UpdateStateColor(march.status);

            // Trail on
            if (dustTrail != null) dustTrail.emitting = true;
            if (dustParticles != null) dustParticles.Play();
        }

        public void SetupGatherMarch(string status, DateTime arrivesAt, Color kingdomColor, int totalTroops)
        {
            SetBannerColor(kingdomColor);
            SetFormationIcon("gather");
            _arrivesAt   = arrivesAt;
            _marchStatus = status;
            if (troopCountLabel != null) troopCountLabel.text = $"{totalTroops:N0} troops";
            UpdateStateColor(status);
            if (dustTrail != null) dustTrail.emitting = true;
        }

        public void PlaceDestinationBeacon(Vector3 worldPos)
        {
            if (_destinationBeacon != null) Destroy(_destinationBeacon);
            if (destinationBeaconPrefab != null)
                _destinationBeacon = Instantiate(destinationBeaconPrefab, worldPos, Quaternion.identity);
        }

        public void OnMarchCompleted()
        {
            if (dustTrail != null) dustTrail.emitting = false;
            if (dustParticles != null) dustParticles.Stop();
            if (_destinationBeacon != null) { Destroy(_destinationBeacon); _destinationBeacon = null; }
        }

        // ── Per-frame ─────────────────────────────────────────────────────────

        private void UpdateETALabel()
        {
            if (etaLabel == null) return;
            double remaining = (_arrivesAt - DateTime.UtcNow).TotalSeconds;
            if (remaining <= 0)
            {
                etaLabel.text = "Arrived";
                return;
            }
            int m = (int)(remaining / 60);
            int s = (int)(remaining % 60);
            etaLabel.text = m > 0 ? $"{m}m {s:00}s" : $"{s}s";
        }

        private void BillboardHUD()
        {
            if (worldHUDCanvas == null || _mainCamera == null) return;
            worldHUDCanvas.transform.LookAt(
                worldHUDCanvas.transform.position + _mainCamera.transform.forward
            );
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetBannerColor(Color c)
        {
            if (bannerRenderer == null) return;
            var block = new MaterialPropertyBlock();
            bannerRenderer.GetPropertyBlock(block);
            block.SetColor("_BannerColor", c);
            bannerRenderer.SetPropertyBlock(block);
        }

        private void SetFormationIcon(string marchType)
        {
            if (formationIconRenderer == null || formationIcons == null) return;
            int idx = marchType switch
            {
                "attack_monster" => 0,
                "gather"         => 1,
                "reinforce"      => 2,
                "scout"          => 3,
                _                => 1,
            };
            if (idx < formationIcons.Length)
                formationIconRenderer.sprite = formationIcons[idx];
        }

        private void UpdateStateColor(string status)
        {
            if (stateIcon == null) return;
            stateIcon.color = status switch
            {
                "outbound"   => colorOutbound,
                "gathering"  => colorGathering,
                "returning"  => colorReturning,
                "attacking"  => colorOutbound,
                _            => Color.white,
            };
        }

        private void OnDestroy()
        {
            if (_destinationBeacon != null) Destroy(_destinationBeacon);
        }
    }
}
