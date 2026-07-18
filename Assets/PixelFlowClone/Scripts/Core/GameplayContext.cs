using PixelFlowClone.Managers;
using PixelFlowClone.UI.Popups;
using PixelFlowClone.UI.Screens;
using UnityEngine;

namespace PixelFlowClone.Core
{
    /// <summary>
    /// Scene-scoped reference holder for gameplay systems.
    /// Managers can be wired in the prefab instead of searched for by every consumer.
    /// </summary>
    public class GameplayContext : MonoBehaviour
    {
        [SerializeField] private GridManager _grid;
        [SerializeField] private ConveyorPathManager _conveyor;
        [SerializeField] private QueueManager _queue;
        [SerializeField] private InputManager _input;
        [SerializeField] private GameplayHUD _hud;
        [SerializeField] private VictoryPopup _victoryPopup;
        [SerializeField] private DefeatPopup _defeatPopup;
        [SerializeField] private PausePopup _pausePopup;

        public static GameplayContext Instance { get; private set; }

        public GridManager Grid => _grid;
        public ConveyorPathManager Conveyor => _conveyor;
        public QueueManager Queue => _queue;
        public InputManager Input => _input;
        public GameplayHUD Hud => _hud;
        public VictoryPopup VictoryPopup => _victoryPopup;
        public DefeatPopup DefeatPopup => _defeatPopup;
        public PausePopup PausePopup => _pausePopup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[GameplayContext] More than one context exists in the scene.", this);
                return;
            }

            Instance = this;
            ResolveMissingReferences();
            PersistentManagers.EnsureGameManager();
            PersistentManagers.EnsureLevelManager();
            EnsureHud();
            EnsureVictoryPopup();
            EnsureDefeatPopup();
            EnsurePausePopup();
        }

        /// <summary>
        /// Wires scene managers. Primarily used by the prefab/scene builder.
        /// </summary>
        public void Configure(
            GridManager grid,
            ConveyorPathManager conveyor,
            QueueManager queue,
            InputManager input,
            GameplayHUD hud = null)
        {
            _grid = grid;
            _conveyor = conveyor;
            _queue = queue;
            _input = input;
            if (hud != null)
                _hud = hud;
        }

        private void ResolveMissingReferences()
        {
            if (_grid == null)
                _grid = GetComponentInChildren<GridManager>(true);
            if (_conveyor == null)
                _conveyor = GetComponentInChildren<ConveyorPathManager>(true);
            if (_queue == null)
                _queue = GetComponentInChildren<QueueManager>(true);
            if (_input == null)
                _input = GetComponentInChildren<InputManager>(true);
            if (_hud == null)
                _hud = FindFirstObjectByType<GameplayHUD>();
        }

        private void EnsureHud()
        {
            if (_hud == null)
                _hud = FindFirstObjectByType<GameplayHUD>();

            if (_hud == null)
            {
                _hud = GameplayHUD.CreateRuntime();
                _hud.name = "PF_GameplayHUD";
                Debug.Log("[GameplayContext] Spawned runtime PF_GameplayHUD (scene had none).");
            }

            _hud.Show();
        }

        private void EnsureVictoryPopup()
        {
            if (_victoryPopup == null)
                _victoryPopup = FindFirstObjectByType<VictoryPopup>(FindObjectsInactive.Include);

            if (_victoryPopup != null)
                return;

            _victoryPopup = VictoryPopup.CreateRuntime(ResolveHudCanvasRoot());
            Debug.Log("[GameplayContext] Spawned runtime VictoryPopup.");
        }

        private void EnsureDefeatPopup()
        {
            if (_defeatPopup == null)
                _defeatPopup = FindFirstObjectByType<DefeatPopup>(FindObjectsInactive.Include);

            if (_defeatPopup != null)
                return;

            _defeatPopup = DefeatPopup.CreateRuntime(ResolveHudCanvasRoot());
            Debug.Log("[GameplayContext] Spawned runtime DefeatPopup.");
        }

        private void EnsurePausePopup()
        {
            if (_pausePopup == null)
                _pausePopup = FindFirstObjectByType<PausePopup>(FindObjectsInactive.Include);

            if (_pausePopup != null)
                return;

            _pausePopup = PausePopup.CreateRuntime(ResolveHudCanvasRoot());
            Debug.Log("[GameplayContext] Spawned runtime PausePopup.");
        }

        private Transform ResolveHudCanvasRoot()
        {
            Transform parent = _hud != null ? _hud.transform : transform;
            Canvas canvas = parent.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                parent = canvas.transform;
            return parent;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
