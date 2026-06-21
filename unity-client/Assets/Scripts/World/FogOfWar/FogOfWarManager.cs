using UnityEngine;
using System.Collections.Generic;
using EternalKingdoms.World.Grid;

namespace EternalKingdoms.World.FogOfWar
{
    public enum TileVisibility
    {
        Undiscovered = 0,   // never seen — fully hidden
        Discovered   = 1,   // seen before — dimmed / greyed
        Visible      = 2,   // currently visible — full render
    }

    /// <summary>
    /// Fog of War infrastructure — Phase U2.10 (foundation only).
    ///
    /// Phase 2: infrastructure exists, no gameplay restrictions.
    ///          All tiles default to Visible. No fog applied.
    ///
    /// Phase 5+ (Vision System):
    ///   - Kingdom vision range radiates from mapX/Y
    ///   - Troop marches reveal tiles along their path
    ///   - Crystal nodes reveal a small radius
    ///   - Alliance structures share vision with members
    ///   - Tiles outside vision range become Discovered (grey)
    ///   - Tiles beyond discovery range become Undiscovered (black)
    ///
    /// Storage: uses a compressed bitfield (2 bits per tile) backed by
    /// a flat int[] array. The full 2048×2048 grid requires:
    ///   2048 × 2048 × 2 bits = ~1 MB — acceptable for WebGL/mobile.
    ///
    /// The discovery state is persisted across sessions (future: server-side).
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        // ── Phase 2: all tiles visible — no fog ───────────────────────────────
        private const TileVisibility DEFAULT_VISIBILITY = TileVisibility.Visible;

        private WorldGrid _grid;
#pragma warning disable CS0414
        private bool _initialized;
#pragma warning restore CS0414

        // Vision data — 2 bits per tile, packed into int[]
        // Full world: 2048×2048 = 4,194,304 tiles × 2 bits ÷ 32 bits/int = 262,144 ints ≈ 1 MB
        private int[] _visionBits;

        private const int WORLD_SIZE = WorldCoordinate.WORLD_SIZE_TILES;

        // ── Vision source registry ────────────────────────────────────────────
        private readonly List<VisionSource> _sources = new();

        public void Initialize(WorldGrid grid)
        {
            _grid = grid;
            // Phase 2: allocate array but leave everything at "Visible" (all zeros = Undiscovered by default,
            // but we return Visible from GetVisibility until gameplay restrictions activate).
            // Pre-allocate for future phases.
            int arraySize = WORLD_SIZE * WORLD_SIZE * 2 / 32 + 1;
            _visionBits = new int[arraySize];
            _initialized = true;
            Debug.Log("[FogOfWarManager] Initialized (Phase 2: all tiles visible — no restrictions).");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Phase 2: always returns Visible.
        /// Phase 5+: returns actual tile visibility from bitfield.
        /// </summary>
        public TileVisibility GetVisibility(WorldCoordinate coord)
        {
            return DEFAULT_VISIBILITY; // Phase 2 — no fog
        }

        /// <summary>Register a vision source (kingdom, march, scout).</summary>
        public void AddVisionSource(string id, WorldCoordinate center, int radiusTiles)
        {
            _sources.Add(new VisionSource { Id = id, Center = center, Radius = radiusTiles });
            // Phase 5+: mark tiles in radius as Visible in _visionBits
        }

        public void RemoveVisionSource(string id)
        {
            _sources.RemoveAll(s => s.Id == id);
        }

        /// <summary>Returns true if chunk has any visible tiles (Phase 5+ chunk culling).</summary>
        public bool IsChunkVisible(ChunkCoordinate chunk)
        {
            return true; // Phase 2 — no culling
        }

        // ── Bitfield helpers (used in Phase 5+) ──────────────────────────────

        private void SetTileVisibility(WorldCoordinate coord, TileVisibility vis)
        {
            if (!coord.IsValid || _visionBits == null) return;
            int tileIndex = coord.TZ * WORLD_SIZE + coord.TX;
            int bitIndex  = tileIndex * 2;
            int arrIndex  = bitIndex / 32;
            int bitOffset = bitIndex % 32;
            _visionBits[arrIndex] = (_visionBits[arrIndex] & ~(3 << bitOffset)) | ((int)vis << bitOffset);
        }

        private TileVisibility ReadTileVisibility(WorldCoordinate coord)
        {
            if (!coord.IsValid || _visionBits == null) return TileVisibility.Undiscovered;
            int tileIndex = coord.TZ * WORLD_SIZE + coord.TX;
            int bitIndex  = tileIndex * 2;
            int arrIndex  = bitIndex / 32;
            int bitOffset = bitIndex % 32;
            return (TileVisibility)((_visionBits[arrIndex] >> bitOffset) & 3);
        }

        private struct VisionSource
        {
            public string Id;
            public WorldCoordinate Center;
            public int Radius;
        }
    }
}
