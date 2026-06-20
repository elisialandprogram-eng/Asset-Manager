using UnityEngine;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.Utilities;

namespace EternalKingdoms.Monsters
{
    /// <summary>
    /// Represents a monster spawn entity on the World map.
    /// Placed by EntitySpawner from GET /api/worlds/:id/spawns data.
    ///
    /// Visual state:
    ///   - Alive: monster mesh visible, HP bar shown
    ///   - Dead (respawning): skull marker or invisible, respawn timer shown
    /// </summary>
    public class MonsterSpawnController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private GameObject aliveMesh;
        [SerializeField] private GameObject deadMarker;
        [SerializeField] private TMPro.TextMeshProUGUI nameLabel;
        [SerializeField] private TMPro.TextMeshProUGUI tierLabel;

        private MonsterSpawnDto _data;

        public MonsterSpawnDto Data => _data;

        public void Initialize(MonsterSpawnDto data)
        {
            _data = data;
            Refresh(data);

            // Position in Unity world space using backend coordinates
            transform.position = MathExtensions.BackendToUnityXZ(data.x, data.y);
        }

        public void Refresh(MonsterSpawnDto data)
        {
            _data = data;
            bool alive = data.currentHp > 0;

            if (aliveMesh != null) aliveMesh.SetActive(alive);
            if (deadMarker != null) deadMarker.SetActive(!alive);

            if (nameLabel != null && data.monster != null)
                nameLabel.text = data.monster.name;

            if (tierLabel != null && data.monster != null)
                tierLabel.text = $"T{data.monster.tier}";
        }
    }
}
