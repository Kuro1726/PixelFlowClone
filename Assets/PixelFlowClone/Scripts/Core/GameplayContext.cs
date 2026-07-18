using PixelFlowClone.Managers;
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

        public static GameplayContext Instance { get; private set; }

        public GridManager Grid => _grid;
        public ConveyorPathManager Conveyor => _conveyor;
        public QueueManager Queue => _queue;
        public InputManager Input => _input;
        public GameplayHUD Hud => _hud;

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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
