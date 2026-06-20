using UnityEngine;
using TMPro;
using EternalKingdoms.World.Grid;

namespace EternalKingdoms.World.UI
{
    /// <summary>
    /// World scene heads-up display — bottom overlay showing real-time
    /// camera position information.
    ///
    /// Updates every frame from WorldCameraController.OnCoordChanged event.
    ///
    /// Displayed info:
    ///   Current tile coordinates (X, Z)
    ///   Current chunk (CX, CZ)
    ///   Biome name at current tile
    ///   Current orthographic zoom level
    ///   Zone name (from WorldGrid.GetZoneName)
    /// </summary>
    public class WorldHUD : MonoBehaviour
    {
        [Header("Coordinate Display")]
        [SerializeField] private TextMeshProUGUI coordLabel;
        [SerializeField] private TextMeshProUGUI chunkLabel;
        [SerializeField] private TextMeshProUGUI biomeLabel;
        [SerializeField] private TextMeshProUGUI zoneLabel;
        [SerializeField] private TextMeshProUGUI zoomLabel;

        [Header("Camera reference")]
        [SerializeField] private WorldCameraController worldCamera;

        private int _worldSeed;

        private void Start()
        {
            if (worldCamera != null)
                worldCamera.OnCoordChanged += OnCoordChanged;
        }

        private void OnDestroy()
        {
            if (worldCamera != null)
                worldCamera.OnCoordChanged -= OnCoordChanged;
        }

        public void SetWorldSeed(int seed) => _worldSeed = seed;

        public void SetCoords(WorldCoordinate coord) => OnCoordChanged(coord);

        private void OnCoordChanged(WorldCoordinate coord)
        {
            if (coordLabel != null)
                coordLabel.text = $"({coord.TX}, {coord.TZ})";

            var chunk = coord.ToChunk();
            if (chunkLabel != null)
                chunkLabel.text = $"Chunk ({chunk.CX}, {chunk.CZ})";

            if (biomeLabel != null && _worldSeed != 0)
            {
                var biome = Terrain.BiomeGenerator.GetBiome(coord.TX, coord.TZ, _worldSeed);
                biomeLabel.text = Terrain.BiomeGenerator.GetBiomeName(biome);
            }

            if (zoneLabel != null)
                zoneLabel.text = WorldGrid.GetZoneName(coord);

            // Zoom — read from camera orthographic size
            if (zoomLabel != null && worldCamera != null)
            {
                var cam = worldCamera.GetCamera();
                if (cam != null) zoomLabel.text = $"Zoom: {cam.orthographicSize:F0}";
            }
        }
    }
}
