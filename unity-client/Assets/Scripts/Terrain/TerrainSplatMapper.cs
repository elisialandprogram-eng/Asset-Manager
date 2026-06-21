using UnityEngine;
using System.Collections.Generic;

namespace EternalKingdoms.Terrain
{
    /// <summary>
    /// TerrainSplatMapper — Applies biome-based splat weight maps to Unity Terrain objects.
    ///
    /// Phase 5 (U5.2) responsibilities:
    ///   - Builds alpha maps for Unity Terrain splat layers from BiomeData
    ///   - Applies procedural variation using Perlin noise to avoid flat tiling
    ///   - Blends cliff layers on steep slopes
    ///   - Maintains terrain layer references for runtime swaps
    ///
    /// Architecture:
    ///   - Component on BiomeTerrainController
    ///   - Operates per-chunk when chunk is streamed in
    ///   - No runtime update per frame — bakes once on chunk load
    /// </summary>
    [RequireComponent(typeof(BiomeTerrainController))]
    public class TerrainSplatMapper : MonoBehaviour
    {
        [Header("Slope Blending")]
        [Range(0f, 90f)] [SerializeField] private float cliffAngle = 45f;
#pragma warning disable CS0414
        [Range(0f, 1f)]  [SerializeField] private float cliffBlend = 0.3f;
#pragma warning restore CS0414

        [Header("Noise Variation")]
        [Range(0f, 1f)] [SerializeField] private float noiseStrength = 0.25f;
        [SerializeField] private float noiseScale = 0.05f;

        /// <summary>
        /// Applies splat map to a Unity Terrain object based on the given BiomeData.
        /// Call once when a chunk is streamed in.
        /// </summary>
        public void Apply(UnityEngine.Terrain terrain, BiomeData biome)
        {
            if (terrain == null || biome == null) return;

            var td = terrain.terrainData;

            // Build layer list from biome
            var layers = new List<TerrainLayer>();
            if (biome.groundLayer  != null) layers.Add(biome.groundLayer);
            if (biome.detailLayer1 != null) layers.Add(biome.detailLayer1);
            if (biome.detailLayer2 != null) layers.Add(biome.detailLayer2);
            if (biome.cliffLayer   != null) layers.Add(biome.cliffLayer);

            if (layers.Count == 0) return;

            td.terrainLayers = layers.ToArray();

            int aw   = td.alphamapWidth;
            int ah   = td.alphamapHeight;
            int lc   = layers.Count;
            var maps = new float[ah, aw, lc];

            int cliffLayerIdx = layers.Count - 1;
            bool hasCliff     = biome.cliffLayer != null;

            for (int y = 0; y < ah; y++)
            {
                for (int x = 0; x < aw; x++)
                {
                    float nx = (float)x / aw;
                    float ny = (float)y / ah;

                    // Sample terrain normal to detect slope
                    Vector3 normal  = td.GetInterpolatedNormal(nx, ny);
                    float   slope   = Vector3.Angle(normal, Vector3.up);
                    bool    isCliff = hasCliff && slope > cliffAngle;

                    // Noise for ground/detail blend variation
                    float noise = Mathf.PerlinNoise(
                        nx / noiseScale + 1000f,
                        ny / noiseScale + 1000f
                    );
                    float detailWeight = Mathf.Clamp01(noise * noiseStrength);

                    if (isCliff && hasCliff)
                    {
                        // Cliff dominant
                        float cliffW = Mathf.Lerp(0f, 1f, Mathf.InverseLerp(cliffAngle, 90f, slope));
                        float remain = 1f - cliffW;
                        if (lc >= 4)
                        {
                            maps[y, x, 0] = remain * (1f - detailWeight);
                            maps[y, x, 1] = remain * detailWeight * 0.6f;
                            maps[y, x, 2] = remain * detailWeight * 0.4f;
                            maps[y, x, 3] = cliffW;
                        }
                        else
                        {
                            maps[y, x, 0] = remain;
                            if (lc > 1) maps[y, x, lc - 1] = cliffW;
                        }
                    }
                    else
                    {
                        // Normal blend: ground + detail layers
                        float ground = 1f - detailWeight;
                        if (lc == 1)
                        {
                            maps[y, x, 0] = 1f;
                        }
                        else if (lc == 2)
                        {
                            maps[y, x, 0] = ground;
                            maps[y, x, 1] = detailWeight;
                        }
                        else
                        {
                            maps[y, x, 0] = ground;
                            maps[y, x, 1] = detailWeight * 0.6f;
                            maps[y, x, 2] = detailWeight * 0.4f;
                        }
                    }
                }
            }

            td.SetAlphamaps(0, 0, maps);
        }
    }
}
