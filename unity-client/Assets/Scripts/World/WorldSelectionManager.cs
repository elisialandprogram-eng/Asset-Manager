using UnityEngine;
using EternalKingdoms.World.Entities;
using EternalKingdoms.World.UI;
using EternalKingdoms.Core;

namespace EternalKingdoms.World
{
    /// <summary>
    /// World scene selection manager.
    ///
    /// At most one entity selected at a time (kingdom OR monster OR crystal).
    /// Drives WorldInfoPanel display.
    /// Delegates to WorldSceneController for kingdom navigation.
    ///
    /// Entity classes call SelectKingdom/SelectMonster/SelectCrystal
    /// via their SelectableEntity.OnSelected callback.
    /// </summary>
    public class WorldSelectionManager : MonoBehaviour
    {
        public static WorldSelectionManager Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private WorldInfoPanel infoPanel;

        private BaseWorldEntity _selected;

        public event System.Action<BaseWorldEntity> OnEntitySelected;
        public event System.Action OnEntityDeselected;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            // Escape to deselect
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                DeselectAll();
        }

        // ── Called by entity SelectableEntity events ──────────────────────────

        public void SelectKingdom(KingdomEntity entity)
        {
            DeselectCurrent();
            _selected = entity;
            infoPanel?.ShowKingdom(entity.Data);
            OnEntitySelected?.Invoke(entity);
        }

        public void SelectMonster(MonsterEntity entity)
        {
            DeselectCurrent();
            _selected = entity;
            infoPanel?.ShowMonster(entity.Data);
            OnEntitySelected?.Invoke(entity);
        }

        public void SelectCrystal(CrystalEntity entity)
        {
            DeselectCurrent();
            _selected = entity;
            infoPanel?.ShowCrystal(entity.Data);
            OnEntitySelected?.Invoke(entity);
        }

        public void DeselectAll()
        {
            DeselectCurrent();
            infoPanel?.Hide();
            OnEntityDeselected?.Invoke();
        }

        // ── Actions available from WorldInfoPanel ─────────────────────────────

        /// <summary>Navigate to own kingdom scene from kingdom selection.</summary>
        public void EnterSelectedKingdom()
        {
            if (_selected is KingdomEntity ke && ke.IsOwnKingdom)
            {
                SaveManager.Instance.SetString(SaveManager.KEY_KINGDOM_ID, ke.Data.id);
                SceneController.Instance.GoToKingdom();
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void DeselectCurrent()
        {
            _selected?.GetComponent<Interaction.SelectableEntity>()?.Deselect();
            _selected = null;
        }
    }
}
