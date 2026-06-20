using UnityEngine;
using EternalKingdoms.World.Grid;

namespace EternalKingdoms.World.Terrain
{
    /// <summary>
    /// MonoBehaviour component that lives on a Chunk GameObject.
    /// Owns the Mesh, MeshFilter, MeshRenderer, and MeshCollider for this chunk.
    ///
    /// Material assignment:
    ///   Uses a vertex-color URP material (no separate texture atlas needed).
    ///   A single 'TerrainVertexColor' URP Lit material is assigned at scene setup.
    ///   Each vertex carries biome color data — BiomeGenerator.GetBiomeColor().
    ///
    ///   Art team will replace vertex colors with splat-map textures in a
    ///   future sprint by swapping out the material without changing this component.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TerrainChunk : MonoBehaviour
    {
        [Header("Material (Vertex Color URP Lit)")]
        [SerializeField] private Material terrainMaterial;

        private MeshFilter   _mf;
        private MeshRenderer _mr;
        private MeshCollider _mc;
        private Mesh         _mesh;

        private void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            _mr = GetComponent<MeshRenderer>();
            _mc = GetComponent<MeshCollider>();
        }

        // ── Called by Chunk.Initialize() ─────────────────────────────────────

        public void Generate(ChunkCoordinate coord, int worldSeed)
        {
            var data = TerrainGenerator.Generate(coord, worldSeed);
            ApplyMesh(data);
        }

        private void ApplyMesh(TerrainMeshData data)
        {
            if (_mesh == null)
                _mesh = new Mesh { name = "TerrainChunkMesh" };
            else
                _mesh.Clear();

            // Use 32-bit index buffer for large meshes (Unity 2020+)
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            _mesh.vertices  = data.Vertices;
            _mesh.uv        = data.UVs;
            _mesh.colors    = data.Colors;
            _mesh.triangles = data.Triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _mesh.RecalculateTangents();

            _mf.sharedMesh = _mesh;

            // Collider — updated lazily (async in a future sprint)
            _mc.sharedMesh = _mesh;

            // Material
            if (terrainMaterial != null)
                _mr.sharedMaterial = terrainMaterial;
            else
            {
                // Fallback: standard URP Lit shader at runtime
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "TerrainVertexColor_Fallback"
                };
                _mr.sharedMaterial = mat;
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
