using UnityEngine;
using System.Collections;
using EternalKingdoms.Core;
using EternalKingdoms.Networking;
using EternalKingdoms.Networking.DTOs;
using EternalKingdoms.World.Grid;
using EternalKingdoms.World.Streaming;
using EternalKingdoms.World.Entities;
using EternalKingdoms.World.UI;

namespace EternalKingdoms.World
{
    /// <summary>
    /// Scene controller for World.unity.
    ///
    /// Bootstrap sequence:
    ///   1. Fetch GET /api/worlds (resolve active world + seed)
    ///   2. Fetch GET /api/kingdoms/mine (resolve own kingdom coords)
    ///   3. Fetch GET /api/worlds/:id/map (all entities)
    ///   4. Initialize WorldGrid + OccupancyManager + SpatialIndex
    ///   5. Center camera on own kingdom
    ///   6. Start WorldStreamingManager
    ///   7. Spawn entities via WorldEntitySpawner
    ///   8. Start polling loops (60s kingdoms, 30s spawns)
    ///
    /// All scene references are wired in the Inspector.
    /// </summary>
    public class WorldSceneController : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private WorldStreamingManager streamingManager;
        [SerializeField] private WorldEntitySpawner    entitySpawner;
        [SerializeField] private FogOfWar.FogOfWarManager fogOfWar;

        [Header("Camera")]
        [SerializeField] private WorldCameraController worldCamera;

        [Header("UI")]
        [SerializeField] private WorldHUD     worldHUD;
        [SerializeField] private WorldTopBar  worldTopBar;
        [SerializeField] private WorldBottomBar worldBottomBar;
        [SerializeField] private GameObject   loadingOverlay;

        [Header("Canvas")]
        [SerializeField] private UnityEngine.Canvas hudCanvas;
        [SerializeField] private UnityEngine.Canvas popupCanvas;

        [Header("Polling")]
        [SerializeField] private float mapPollIntervalSeconds   = 60f;
        [SerializeField] private float spawnPollIntervalSeconds = 30f;

        // Cached world data
        private WorldDto         _world;
        private KingdomDto       _myKingdom;
        private WorldMapResponseDto _worldMap;

        private WorldGrid        _grid;
        private OccupancyManager _occupancy;
        private SpatialIndex     _spatial;

        private Coroutine _mapPollCoroutine;
        private Coroutine _spawnPollCoroutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            GameManager.Instance?.SetState(GameState.World);
            EternalKingdoms.UI.UIManager.Instance?.RegisterSceneCanvases(hudCanvas, popupCanvas);
        }

        private void Start()
        {
            ShowLoading(true);
            StartCoroutine(Bootstrap());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            streamingManager?.Shutdown();
        }

        // ── Bootstrap sequence ────────────────────────────────────────────────

        private IEnumerator Bootstrap()
        {
            // Step 1 — resolve world
            yield return FetchWorld();
            if (_world == null) { ShowError("Failed to load world data."); yield break; }

            // Step 2 — resolve own kingdom
            yield return FetchMyKingdom();

            // Step 3 — fetch full world map
            yield return FetchWorldMap();

            // Step 4 — initialize grid systems
            InitializeSystems();

            // Step 5 — center camera
            if (_myKingdom != null)
            {
                var coord = WorldCoordinate.FromBackend(_myKingdom.mapX, _myKingdom.mapY);
                worldCamera?.FlyTo(coord.ToUnityCenter(), immediate: true);
                worldHUD?.SetCoords(coord);
            }

            // Step 6 — start streaming
            if (streamingManager != null)
                streamingManager.Initialize(_grid, _world.seed, worldCamera?.GetCamera());

            // Step 7 — spawn entities
            if (_worldMap != null && entitySpawner != null)
                entitySpawner.SpawnAll(_worldMap, _spatial, _myKingdom?.id);

            ShowLoading(false);

            // Step 8 — polling
            _mapPollCoroutine   = StartCoroutine(PollMap());
            _spawnPollCoroutine = StartCoroutine(PollSpawns());

            Debug.Log($"[WorldSceneController] Bootstrap complete — world '{_world.name}', seed={_world.seed}");
        }

        // ── Data fetches ──────────────────────────────────────────────────────

        private IEnumerator FetchWorld()
        {
            bool done = false;
            var service = new EternalKingdoms.Networking.WorldService(NetworkManager.Instance.Api);
            yield return service.GetWorlds(
                onSuccess: response =>
                {
                    var worlds = response?.worlds;
                    if (worlds != null)
                        foreach (var w in worlds)
                            if (w.status == "active") { _world = w; break; }
                    if (_world == null && worlds?.Length > 0) _world = worlds[0];
                    done = true;
                },
                onError: err => { Debug.LogError($"[WorldSceneController] GetWorlds failed: {err}"); done = true; }
            );
            yield return new WaitUntil(() => done);
        }

        private IEnumerator FetchMyKingdom()
        {
            bool done = false;
            var service = new EternalKingdoms.Networking.KingdomService(NetworkManager.Instance.Api);
            yield return service.GetMyKingdom(
                onSuccess: k => { _myKingdom = k; done = true; },
                onError:   _ => done = true
            );
            yield return new WaitUntil(() => done);
        }

        private IEnumerator FetchWorldMap()
        {
            if (_world == null) yield break;
            bool done = false;
            var service = new EternalKingdoms.Networking.WorldService(NetworkManager.Instance.Api);
            yield return service.GetWorldMap(
                _world.id,
                onSuccess: map => { _worldMap = map; done = true; },
                onError:   err => { Debug.LogWarning($"[WorldSceneController] GetWorldMap failed: {err}"); done = true; }
            );
            yield return new WaitUntil(() => done);
        }

        private void InitializeSystems()
        {
            _grid      = new WorldGrid();
            _occupancy = new OccupancyManager();
            _spatial   = new SpatialIndex();

            if (_worldMap != null)
            {
                _occupancy.PopulateFromWorldMap(_worldMap);
                PopulateSpatialIndex(_worldMap);
            }

            fogOfWar?.Initialize(_grid);
        }

        private void PopulateSpatialIndex(WorldMapResponseDto map)
        {
            if (map.kingdoms != null)
                foreach (var k in map.kingdoms)
                {
                    var coord = WorldCoordinate.FromBackend(k.mapX, k.mapY);
                    _spatial.Insert(k.id, coord, TileOccupancy.Kingdom, k);
                }

            if (map.spawns != null)
                foreach (var s in map.spawns)
                {
                    var coord = WorldCoordinate.FromBackend(s.x, s.y);
                    _spatial.Insert(s.id, coord, TileOccupancy.Monster, s);
                }

            if (map.crystalNodes != null)
                foreach (var c in map.crystalNodes)
                {
                    var coord = WorldCoordinate.FromBackend(c.x, c.y);
                    _spatial.Insert(c.id, coord, TileOccupancy.Crystal, c);
                }
        }

        // ── Poll loops ────────────────────────────────────────────────────────

        private IEnumerator PollMap()
        {
            while (true)
            {
                yield return new WaitForSeconds(mapPollIntervalSeconds);
                yield return FetchWorldMap();
                if (_worldMap != null && entitySpawner != null)
                {
                    _occupancy.PopulateFromWorldMap(_worldMap);
                    PopulateSpatialIndex(_worldMap);
                    entitySpawner.RefreshAll(_worldMap, _spatial, _myKingdom?.id);
                }
            }
        }

        private IEnumerator PollSpawns()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnPollIntervalSeconds);
                if (_world == null) continue;

                bool done = false;
                var service = new EternalKingdoms.Networking.WorldService(NetworkManager.Instance.Api);
                yield return service.GetWorldSpawns(
                    _world.id,
                    onSuccess: response =>
                    {
                        entitySpawner?.RefreshMonsters(response?.spawns);
                        done = true;
                    },
                    onError: _ => done = true
                );
                yield return new WaitUntil(() => done);
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        public void ShowLoading(bool visible)
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(visible);
        }

        private void ShowError(string msg)
        {
            ShowLoading(false);
            EternalKingdoms.UI.NotificationManager.Instance?.ShowError(msg);
            Debug.LogError($"[WorldSceneController] {msg}");
        }

        // ── Navigation ────────────────────────────────────────────────────────

        public void GoToKingdom(string kingdomId)
        {
            SaveManager.Instance.SetString(SaveManager.KEY_KINGDOM_ID, kingdomId);
            SceneController.Instance.GoToKingdom();
        }

        public WorldDto CurrentWorld => _world;
        public WorldGrid Grid => _grid;
        public SpatialIndex Spatial => _spatial;
    }
}
