using PixelFlowClone.Managers;
using PixelFlowClone.UI;
using PixelFlowClone.UI.Popups;
using PixelFlowClone.UI.Screens;
using PixelFlowClone.VFX;
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
        [SerializeField] private Sprite _victoryPanelSprite;
        [SerializeField] private Sprite _victoryTitleBannerSprite;
        [SerializeField] private Sprite _victoryTrophySprite;
        [SerializeField] private Sprite _victoryTrophyWingSprite;
        [SerializeField] private Sprite _victoryContinueButtonSprite;
        [SerializeField] private VictoryPopup _victoryPopup;
        [SerializeField] private DefeatPopup _defeatPopup;
        [SerializeField] private PausePopup _pausePopup;
        [SerializeField] private BlockConsumeVfx _consumeVfx;
        [SerializeField] private CollectorShotVfx _shotVfx;
        [SerializeField] private GameplayBackground _background;

        public static GameplayContext Instance { get; private set; }

        public GridManager Grid => _grid;
        public ConveyorPathManager Conveyor => _conveyor;
        public QueueManager Queue => _queue;
        public InputManager Input => _input;
        public GameplayHUD Hud => _hud;
        public VictoryPopup VictoryPopup => _victoryPopup;
        public DefeatPopup DefeatPopup => _defeatPopup;
        public PausePopup PausePopup => _pausePopup;
        public BlockConsumeVfx ConsumeVfx => _consumeVfx;
        public CollectorShotVfx ShotVfx => _shotVfx;
        public GameplayBackground Background => _background;

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
            EnsureShotVfx();
            EnsureBackground();
            RegisterWithUIManager();
        }

        private void OnDestroy()
        {
            UnregisterFromUIManager();
            if (Instance == this)
                Instance = null;
        }

        private void RegisterWithUIManager()
        {
            PersistentManagers.EnsureUIManager();
            if (!UIManager.HasInstance)
                return;

            UIManager ui = UIManager.Instance;
            ui.RegisterScreen(ScreenId.Gameplay, gameObject);
            ui.ShowScreen(ScreenId.Gameplay);

            if (_hud != null)
                ui.RegisterHud(_hud);
            if (_victoryPopup != null)
                ui.RegisterPopup(PopupId.Victory, _victoryPopup);
            if (_defeatPopup != null)
                ui.RegisterPopup(PopupId.Defeat, _defeatPopup);
            if (_pausePopup != null)
                ui.RegisterPopup(PopupId.Pause, _pausePopup);
        }

        private void UnregisterFromUIManager()
        {
            if (!UIManager.HasInstance)
                return;

            UIManager ui = UIManager.Instance;
            ui.UnregisterHud(_hud);
            ui.UnregisterPopup(PopupId.Victory, _victoryPopup);
            ui.UnregisterPopup(PopupId.Defeat, _defeatPopup);
            ui.UnregisterPopup(PopupId.Pause, _pausePopup);
            ui.UnregisterScreen(ScreenId.Gameplay);
            ui.HideAllPopups();
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

            _victoryPopup = VictoryPopup.CreateRuntime(
                ResolveHudCanvasRoot(),
                _victoryPanelSprite,
                _victoryTitleBannerSprite,
                _victoryTrophySprite,
                _victoryTrophyWingSprite,
                _victoryContinueButtonSprite);
            Debug.Log("[GameplayContext] Spawned runtime VictoryPopup.");
        }

        private void EnsureDefeatPopup()
        {
            if (_defeatPopup == null)
                _defeatPopup = FindFirstObjectByType<DefeatPopup>(FindObjectsInactive.Include);

            if (_defeatPopup != null)
                return;

            _defeatPopup = DefeatPopup.CreateRuntime(ResolveHudCanvasRoot());
            Debug.Log("[GameplayContext] Spawned runtime DefeatPopup because none was assigned in the Hierarchy.");
        }

        private void EnsurePausePopup()
        {
            if (_pausePopup == null)
                _pausePopup = FindFirstObjectByType<PausePopup>(FindObjectsInactive.Include);

            if (_pausePopup != null)
                return;

            _pausePopup = PausePopup.Create(ResolveHudCanvasRoot());
            Debug.Log("[GameplayContext] Spawned PausePopup.");
        }

        private void EnsureConsumeVfx()
        {
            if (_consumeVfx == null)
                _consumeVfx = GetComponentInChildren<BlockConsumeVfx>(true);

            if (_consumeVfx == null)
                _consumeVfx = FindFirstObjectByType<BlockConsumeVfx>(FindObjectsInactive.Include);

            if (_consumeVfx != null)
                return;

            _consumeVfx = BlockConsumeVfx.CreateRuntime(transform);
            Debug.Log("[GameplayContext] Spawned runtime BlockConsumeVfx.");
        }

        private void EnsureShotVfx()
        {
            if (_shotVfx == null)
                _shotVfx = GetComponentInChildren<CollectorShotVfx>(true);

            if (_shotVfx == null)
                _shotVfx = FindFirstObjectByType<CollectorShotVfx>(FindObjectsInactive.Include);

            if (_shotVfx != null)
                return;

            _shotVfx = CollectorShotVfx.CreateRuntime(transform);
            Debug.Log("[GameplayContext] Spawned runtime CollectorShotVfx.");
        }

        private void EnsureBackground()
        {
            if (_background == null)
                _background = GetComponentInChildren<GameplayBackground>(true);

            if (_background == null)
                _background = FindFirstObjectByType<GameplayBackground>(FindObjectsInactive.Include);
        }

        private Transform ResolveHudCanvasRoot()
        {
            Transform parent = _hud != null ? _hud.transform : transform;
            Canvas canvas = parent.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                parent = canvas.transform;
            return parent;
        }
    }
}
