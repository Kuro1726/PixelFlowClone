using System;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Screens
{
    /// <summary>
    /// Gameplay overlay HUD.
    /// P3-09: top bar Pause + "Level {0}".
    /// P3-10: conveyor free slots "{0}/{1}" updated by UIManager.
    /// P3-11: queue count HUD cancelled — waiting/queue units are the visual.
    /// </summary>
    public partial class GameplayHUD : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _topBarRoot;
        [SerializeField] private Sprite _pauseIcon;
        [SerializeField] private Sprite _levelBadgeSprite;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private TMP_Text _pauseButtonLabel;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _conveyorLabel;
        private RectTransform _safeAreaRoot;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _gameplayElementsVisible = true;

        public Button PauseButton => _pauseButton;
        public TMP_Text LevelLabel => _levelLabel;
        public TMP_Text ConveyorLabel => _conveyorLabel;

        public event Action PauseClicked;

        private void Awake()
        {
            EnsureEventSystem();
            if (!HasSerializedUi())
                RebuildUi();
            else
                WirePauseButton();
            EnsureSafeArea();
            ApplySafeArea();
            ApplyGameplayFonts();
            RefreshLevelLabel();
            RefreshConveyorIndicator();
        }

        private void Start()
        {
            // Conveyor/GameConfig may not exist yet during Awake — refresh HUD sizes once ready.
            SubscribeManagers();
            RefreshLevelLabel();
            RefreshPauseLabel();
            RefreshConveyorIndicator();
        }

        private void OnEnable()
        {
            EnsureSafeArea();
            ApplySafeArea();
            SubscribeManagers();
            WirePauseButton();
            RefreshLevelLabel();
            RefreshPauseLabel();
            RefreshConveyorIndicator();
        }

        private void OnDisable()
        {
            UnsubscribeManagers();
            UnwirePauseButton();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying || _safeAreaRoot == null)
                return;

            ApplySafeArea();
        }

        private void SubscribeManagers()
        {
            if (LevelManager.HasInstance)
            {
                LevelManager.Instance.LevelLoaded -= HandleLevelLoaded;
                LevelManager.Instance.LevelLoaded += HandleLevelLoaded;
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.StateChanged -= HandleGameStateChanged;
                GameManager.Instance.StateChanged += HandleGameStateChanged;
            }
        }

        private void UnsubscribeManagers()
        {
            if (LevelManager.HasInstance)
                LevelManager.Instance.LevelLoaded -= HandleLevelLoaded;

            if (GameManager.HasInstance)
                GameManager.Instance.StateChanged -= HandleGameStateChanged;
        }

        private void RebuildUi()
        {
            UnwirePauseButton();

            if (_topBarRoot != null)
            {
                DestroyUiObject(_topBarRoot.gameObject);
                _topBarRoot = null;
                _pauseButton = null;
                _pauseButtonLabel = null;
                _levelLabel = null;
                _conveyorLabel = null;
            }

            Transform canvasRoot = GetComponent<Canvas>() != null
                ? transform
                : (GetComponentInChildren<Canvas>() != null ? GetComponentInChildren<Canvas>().transform : transform);
            DestroyChildIfExists(canvasRoot, "QueueBar");
            DestroyChildIfExists(canvasRoot, "QueueLabel");
            DestroyChildIfExists(canvasRoot, "QueueCaption");

            BuildRuntimeUi();
            ApplyGameplayFonts();
            WirePauseButton();
            RefreshLevelLabel();
            RefreshPauseLabel();
            RefreshConveyorIndicator();
        }

        private bool HasSerializedUi()
        {
            return _topBarRoot != null &&
                   _pauseButton != null &&
                   _levelLabel != null &&
                   _conveyorLabel != null;
        }

        private void EnsureSafeArea()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
                return;

            RectTransform canvasRoot = canvas.transform as RectTransform;
            if (canvasRoot == null)
                return;

            if (_safeAreaRoot == null)
            {
                Transform existing = canvasRoot.Find("SafeArea");
                if (existing != null)
                    _safeAreaRoot = existing as RectTransform;
            }

            if (_safeAreaRoot == null)
            {
                var safeAreaObject = new GameObject("SafeArea", typeof(RectTransform));
                _safeAreaRoot = safeAreaObject.GetComponent<RectTransform>();
                _safeAreaRoot.SetParent(canvasRoot, false);
                _safeAreaRoot.anchorMin = Vector2.zero;
                _safeAreaRoot.anchorMax = Vector2.one;
                _safeAreaRoot.offsetMin = Vector2.zero;
                _safeAreaRoot.offsetMax = Vector2.zero;
            }

            if (_topBarRoot != null && _topBarRoot.parent != _safeAreaRoot)
                _topBarRoot.SetParent(_safeAreaRoot, false);
        }

        private void ApplySafeArea()
        {
            if (_safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (safeArea == _lastSafeArea && screenSize == _lastScreenSize)
                return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            _safeAreaRoot.anchorMin = anchorMin;
            _safeAreaRoot.anchorMax = anchorMax;
            _safeAreaRoot.offsetMin = Vector2.zero;
            _safeAreaRoot.offsetMax = Vector2.zero;
        }

        private void ApplyGameplayFonts()
        {
            GameplayFontUtility.Apply(_conveyorLabel);
            GameplayFontUtility.Apply(_levelLabel);
        }

#if UNITY_EDITOR
        public void BuildForEditor()
        {
            RebuildUi();
        }
#endif

        public void Show()
        {
            gameObject.SetActive(true);
            ShowGameplayElements();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        public void Hide()
        {
            _gameplayElementsVisible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        public void ShowGameplayElements()
        {
            _gameplayElementsVisible = true;
            if (_topBarRoot != null)
                _topBarRoot.gameObject.SetActive(true);
        }

        public void HideGameplayElements()
        {
            _gameplayElementsVisible = false;
            if (_topBarRoot != null)
                _topBarRoot.gameObject.SetActive(false);
        }

        public void SetLevelIndex(int zeroBasedIndex)
        {
            if (_levelLabel != null)
                _levelLabel.text = string.Format("Level {0}", zeroBasedIndex + 1);
        }

        /// <summary>
        /// Shows free conveyor slots: empty → 5/5, one unit → 4/5, etc.
        /// </summary>
        public void SetConveyorCount(int active, int max)
        {
            if (_conveyorLabel == null)
                return;

            max = Mathf.Max(0, max);
            int remaining = Mathf.Clamp(max - Mathf.Max(0, active), 0, max);
            _conveyorLabel.text = string.Format("{0}/{1}", remaining, max);
        }

        /// <summary>
        /// Pulls current conveyor occupancy from <see cref="ConveyorPathManager"/> (or config max).
        /// </summary>
        public void RefreshConveyorIndicator()
        {
            if (ConveyorPathManager.HasInstance)
            {
                ConveyorPathManager conveyor = ConveyorPathManager.Instance;
                SetConveyorCount(conveyor.ActiveCount, conveyor.MaxCapacity);
                return;
            }

            GameConfigSO config = ResolveConfig();
            int max = config != null ? Mathf.Max(1, config.MaxConveyorUnits) : 5;
            SetConveyorCount(0, max);
        }

        private void HandleLevelLoaded(LevelDataSO level, int index)
        {
            SetLevelIndex(index);
            RefreshConveyorIndicator();
        }

        private void HandleGameStateChanged(GameState previous, GameState next)
        {
            RefreshPauseLabel();
        }

        private void WirePauseButton()
        {
            if (_pauseButton == null)
                return;

            _pauseButton.onClick.RemoveListener(HandlePauseClicked);
            _pauseButton.onClick.AddListener(HandlePauseClicked);
        }

        private void UnwirePauseButton()
        {
            if (_pauseButton == null)
                return;

            _pauseButton.onClick.RemoveListener(HandlePauseClicked);
        }

        private void HandlePauseClicked()
        {
            PauseClicked?.Invoke();

            if (!GameManager.HasInstance)
            {
                Debug.LogWarning("[GameplayHUD] Pause clicked but GameManager is missing.");
                return;
            }

            GameManager game = GameManager.Instance;
            if (game.CurrentState == GameState.Playing)
            {
                if (game.Pause())
                    Debug.Log("[GameplayHUD] Paused.");
            }

            RefreshPauseLabel();
        }

        private void RefreshLevelLabel()
        {
            if (!LevelManager.HasInstance)
            {
                SetLevelIndex(0);
                return;
            }

            SetLevelIndex(LevelManager.Instance.CurrentLevelIndex);
        }

        private void RefreshPauseLabel()
        {
            if (_pauseButtonLabel == null)
                return;

            // Resume lives on PausePopup (P3-14); top-bar button only opens pause.
            _pauseButtonLabel.text = "Pause";
        }

        /// <summary>
        /// Builds a Screen Space Overlay HUD at runtime / for prefab baking.
        /// </summary>

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
