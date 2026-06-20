using UnityEngine;

namespace EternalKingdoms.Kingdom
{
    /// <summary>
    /// Procedurally builds the kingdom terrain plateau at scene start.
    ///
    /// Visual target: realistic medieval fantasy — stylized terrain plateau
    /// with a flat central court for the palace, gently raised edges,
    /// and supporting walls. Uses URP Lit materials.
    ///
    /// The plateau is generated in code so the scene ships without
    /// large mesh assets. Art team will replace with high-quality
    /// asset imports via Addressables in a future sprint.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class KingdomTerrainBuilder : MonoBehaviour
    {
        [Header("Plateau Dimensions")]
        [SerializeField] private float plateauRadius = 80f;
        [SerializeField] private float plateauHeight = 3f;
        [SerializeField] private int resolution = 32;

        [Header("Materials (URP Lit)")]
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material wallMaterial;

        private void Start()
        {
            BuildPlateau();
        }

        private void BuildPlateau()
        {
            var mesh = GeneratePlateauMesh();
            GetComponent<MeshFilter>().mesh = mesh;

            if (groundMaterial != null)
                GetComponent<MeshRenderer>().material = groundMaterial;

            // Plateau collider so raycasts land on it
            var col = gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private Mesh GeneratePlateauMesh()
        {
            var mesh = new Mesh { name = "KingdomPlateau" };

            int vCount = (resolution + 1) * (resolution + 1);
            var vertices = new Vector3[vCount];
            var uvs = new Vector2[vCount];
            var triangles = new int[resolution * resolution * 6];

            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float fx = (float)x / resolution * 2f - 1f;
                    float fz = (float)z / resolution * 2f - 1f;
                    float dist = Mathf.Sqrt(fx * fx + fz * fz);

                    // Flat center, gentle slope at edges
                    float y = dist < 0.7f ? 0f : Mathf.Pow((dist - 0.7f) / 0.3f, 2f) * -plateauHeight * 0.5f;

                    int idx = z * (resolution + 1) + x;
                    vertices[idx] = new Vector3(fx * plateauRadius, y, fz * plateauRadius);
                    uvs[idx] = new Vector2((float)x / resolution, (float)z / resolution);
                }
            }

            int t = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * (resolution + 1) + x;
                    triangles[t++] = i;
                    triangles[t++] = i + resolution + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + resolution + 1;
                    triangles[t++] = i + resolution + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
