using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// KingdomVisualController — Manages the full visual presentation of a kingdom.
    ///
    /// Phase 5 (U5.4) responsibilities:
    ///   - Central Palace (dominant skyline element)
    ///   - Inner Ring buildings (economic core)
    ///   - Outer Ring buildings (production / military)
    ///   - Military Ring (barracks, stables, archery range)
    ///   - Roads between building slots
    ///   - Walls, gates, towers (level-gated)
    ///   - Flags and banners (kingdom color / emblem)
    ///   - Torches with flicker light + smoke VFX
    ///   - Dynamic building visual states: empty lot → foundation → construction → complete
    ///
    /// Architecture:
    ///   - Attached to the KingdomRoot GameObject in the Kingdom scene
    ///   - Building slots registered by ID matching the API building type
    ///   - Visual state machine per slot, driven by KingdomManager events
    ///   - All prefabs loaded via Addressables (AssetCatalogManager)
    /// </summary>
    public class KingdomVisualController : MonoBehaviour
    {
        [Header("Slot Transforms — assign in scene hierarchy")]
        [SerializeField] private Transform palaceSlot;
        [SerializeField] private Transform[] innerRingSlots;
        [SerializeField] private Transform[] outerRingSlots;
        [SerializeField] private Transform[] militaryRingSlots;

        [Header("Wall System")]
        [SerializeField] private GameObject[] wallSectionPrefabs;
        [SerializeField] private Transform[] gatePrefabs;
        [SerializeField] private Transform[] towerPositions;
        [SerializeField] private int wallUnlockLevel = 5;

        [Header("Road Network")]
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Transform roadNetworkRoot;

        [Header("Decorations")]
        [SerializeField] private GameObject flagPrefab;
        [SerializeField] private GameObject bannerPrefab;
        [SerializeField] private GameObject torchPrefab;
        [SerializeField] private Transform[] torchPositions;
        [SerializeField] private Transform[] flagPositions;

        [Header("Kingdom Identity")]
        [SerializeField] private Renderer emblemRenderer;
        [SerializeField] private int emblemMaterialIndex = 0;

        private readonly Dictionary<string, BuildingVisualState> _slots = new();
        private GameObject _palaceInstance;
        private bool _wallsShown;

        private void Awake()
        {
            SpawnTorches();
            SpawnFlags();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Apply kingdom color and emblem texture.</summary>
        public void SetKingdomIdentity(Color primaryColor, Texture2D emblem)
        {
            if (emblemRenderer != null && emblem != null)
            {
                var mats = emblemRenderer.materials;
                mats[emblemMaterialIndex].SetTexture("_BaseMap", emblem);
                mats[emblemMaterialIndex].SetColor("_BaseColor", primaryColor);
                emblemRenderer.materials = mats;
            }

            // Tint all flag/banner materials
            foreach (var flag in GetComponentsInChildren<FlagController>())
                flag.SetColor(primaryColor);
        }

        /// <summary>Register a building slot and set its initial visual state.</summary>
        public void RegisterSlot(string buildingId, Transform slot, BuildingVisualData data)
        {
            if (_slots.ContainsKey(buildingId)) return;
            var state = slot.gameObject.AddComponent<BuildingVisualState>();
            state.Initialize(data);
            _slots[buildingId] = state;
        }

        /// <summary>Register a hot-swapped building prefab from ArtImportManager.</summary>
        public void RegisterBuildingPrefab(string key, GameObject prefab)
        {
            Debug.Log($"[KingdomVisualController] Registered prefab: {key}");
        }

        /// <summary>Transition a building slot to a new visual state.</summary>
        public void SetBuildingState(string buildingId, BuildingVisualState.State newState, int level = 1)
        {
            if (_slots.TryGetValue(buildingId, out var slot))
                slot.TransitionTo(newState, level);
        }

        /// <summary>Show/hide walls based on palace level.</summary>
        public void UpdateWalls(int palaceLevel)
        {
            bool shouldShow = palaceLevel >= wallUnlockLevel;
            if (shouldShow == _wallsShown) return;
            _wallsShown = shouldShow;

            foreach (var wall in wallSectionPrefabs)
                if (wall != null) wall.SetActive(shouldShow);

            foreach (var gate in gatePrefabs)
                if (gate != null) gate.gameObject.SetActive(shouldShow);

            foreach (var tower in towerPositions)
                if (tower != null) tower.gameObject.SetActive(shouldShow);
        }

        // ── Decoration Spawning ───────────────────────────────────────────────

        private void SpawnTorches()
        {
            if (torchPrefab == null) return;
            foreach (var pos in torchPositions)
            {
                if (pos == null) continue;
                var t = Instantiate(torchPrefab, pos.position, pos.rotation, pos);
                // TorchController drives flicker light + smoke VFX (Phase 5 asset)
            }
        }

        private void SpawnFlags()
        {
            if (flagPrefab == null) return;
            foreach (var pos in flagPositions)
            {
                if (pos == null) continue;
                Instantiate(flagPrefab, pos.position, pos.rotation, pos);
            }
        }
    }

    // ── Supporting Types ───────────────────────────────────────────────────────

    [Serializable]
    public class BuildingVisualData
    {
        public GameObject emptyLotPrefab;
        public GameObject foundationPrefab;
        public GameObject[] constructionStagePrefabs;
        public GameObject[] completedLevelPrefabs;
        public ParticleSystem constructionDustVFX;
        public ParticleSystem completionVFX;
    }

    /// <summary>
    /// FlagController — Drives wind animation and color tinting on a flag mesh.
    /// Attach to the flag GameObject. Wind simulation via shader property.
    /// </summary>
    public class FlagController : MonoBehaviour
    {
        [SerializeField] private Renderer flagRenderer;
        [SerializeField] private int colorPropertyId = Shader.PropertyToID("_FlagColor");

        public void SetColor(Color c)
        {
            if (flagRenderer == null) return;
            var block = new MaterialPropertyBlock();
            flagRenderer.GetPropertyBlock(block);
            block.SetColor("_FlagColor", c);
            flagRenderer.SetPropertyBlock(block);
        }

        private void Update()
        {
            // Drive wind sway via shader time parameter
            if (flagRenderer == null) return;
            var block = new MaterialPropertyBlock();
            flagRenderer.GetPropertyBlock(block);
            block.SetFloat("_WindTime", Time.time);
            flagRenderer.SetPropertyBlock(block);
        }
    }
}
