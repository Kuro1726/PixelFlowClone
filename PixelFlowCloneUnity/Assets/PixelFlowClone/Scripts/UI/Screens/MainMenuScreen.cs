using System;
using System.Collections.Generic;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.UI;
using PixelFlowClone.UI.Popups;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Screens
{
    /// <summary>
    /// Main menu: Play opens level pick, then loads gameplay. Also provides Settings.
    /// </summary>
    public partial class MainMenuScreen : MonoBehaviour
    {
        public const string DefaultTitle = "Pixel Flow";

        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Sprite _playButtonSprite;
        [SerializeField] private Sprite _levelButtonSprite;
        [SerializeField] private Button _levelButtonPrefab;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private RectTransform _mainButtonsRoot;
        [SerializeField] private RectTransform _levelSelectRoot;
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private string _title = DefaultTitle;

        public event Action PlayClicked;
        public event Action SettingsClicked;
        public event Action<int> LevelChosen;

        public Button LevelButtonPrefab => _levelButtonPrefab;

        private readonly List<Button> _levelButtons = new();

        private void Awake()
        {
            EnsureEventSystem();

            RemoveObsoleteInstructionsButton();

            if (_playButton == null || _settingsButton == null)
                BuildRuntimeUi();

            ConfigureMainButtonsLayout();
            ApplyConfiguredButtonArtwork();
            EnsureSettingsPopup();

            if (_titleText != null)
                _titleText.text = _title;

            ShowMainButtons();
            WireButtons();
            RebuildLevelButtons();
            RegisterWithUIManager();
        }

        private void OnDestroy()
        {
            UnregisterFromUIManager();
            UnwireButtons();
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Creates the complete UI hierarchy so it can be edited and serialized in the
        /// Unity Editor instead of only being generated when Play Mode starts.
        /// </summary>
        public void BuildEditableUi()
        {
            RemoveObsoleteInstructionsButton();

            if (_playButton == null || _settingsButton == null)
                BuildRuntimeUi();

            ConfigureMainButtonsLayout();
            ApplyConfiguredButtonArtwork();
            EnsureSettingsPopup();
            if (_settingsPopup != null)
                _settingsPopup.BuildEditableUi();
        }

        /// <summary>
        /// Supplies artwork when an editor utility creates this screen from scratch.
        /// Existing scene instances normally keep these values through serialization.
        /// </summary>
        public void SetArtwork(
            Sprite backgroundSprite,
            Sprite playButtonSprite,
            Sprite levelButtonSprite)
        {
            _backgroundSprite = backgroundSprite;
            _playButtonSprite = playButtonSprite;
            _levelButtonSprite = levelButtonSprite;
        }

        public void SetLevelButtonPrefab(Button levelButtonPrefab)
        {
            _levelButtonPrefab = levelButtonPrefab;
        }

        [ContextMenu("Make Main Menu Editable")]
        private void MakeMainMenuEditableFromComponentMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[MainMenu] Exit Play Mode before creating the editable UI hierarchy.", this);
                return;
            }

            BuildEditableUi();
            Debug.Log("[MainMenu] Editable UI hierarchy created. Save SCN_MainMenu (Ctrl+S).", this);
        }

        private void RegisterWithUIManager()
        {
            PersistentManagers.EnsureUIManager();
            if (!UIManager.HasInstance)
                return;

            UIManager ui = UIManager.Instance;
            ui.RegisterScreen(ScreenId.MainMenu, gameObject);
            ui.ShowScreen(ScreenId.MainMenu);
            if (_settingsPopup != null)
                ui.RegisterPopup(PopupId.Settings, _settingsPopup);
        }

        private void UnregisterFromUIManager()
        {
            if (!UIManager.HasInstance)
                return;

            UIManager ui = UIManager.Instance;
            ui.UnregisterPopup(PopupId.Settings, _settingsPopup);
            ui.UnregisterScreen(ScreenId.MainMenu);
        }

        private void WireButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(HandlePlayClicked);
                _playButton.onClick.AddListener(HandlePlayClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(HandleSettingsClicked);
                _settingsButton.onClick.AddListener(HandleSettingsClicked);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
                _backButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void UnwireButtons()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(HandlePlayClicked);
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            if (_backButton != null)
                _backButton.onClick.RemoveListener(HandleBackClicked);

            for (int i = 0; i < _levelButtons.Count; i++)
            {
                if (_levelButtons[i] != null)
                    _levelButtons[i].onClick.RemoveAllListeners();
            }
        }

        private void HandlePlayClicked()
        {
            Debug.Log("[MainMenu] Play clicked → show level select");
            PlayClicked?.Invoke();
            ShowLevelSelect();
        }

        private void HandleBackClicked()
        {
            ShowMainButtons();
        }

        private void HandleSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked");
            SettingsClicked?.Invoke();
            ShowSettings();
        }

        private void HandleLevelButtonClicked(int index)
        {
            Debug.Log($"[MainMenu] Level chosen index={index}");
            LevelChosen?.Invoke(index);

            if (!LevelManager.HasInstance)
            {
                Debug.LogError("[MainMenu] LevelManager missing — cannot play level.");
                return;
            }

            if (LevelManager.Instance.IsLoadingGameplay)
                return;

            LevelManager.Instance.PlayLevel(index);
        }

        private void ShowMainButtons()
        {
            if (_mainButtonsRoot != null)
                _mainButtonsRoot.gameObject.SetActive(true);
            if (_levelSelectRoot != null)
                _levelSelectRoot.gameObject.SetActive(false);
            if (_settingsPopup != null)
            {
                if (UIManager.HasInstance)
                    UIManager.Instance.HidePopup(PopupId.Settings);
                else
                    _settingsPopup.Hide();
            }
            if (_titleText != null)
                _titleText.text = _title;
        }

        private void ShowLevelSelect()
        {
            RebuildLevelButtons();
            if (_mainButtonsRoot != null)
                _mainButtonsRoot.gameObject.SetActive(false);
            if (_levelSelectRoot != null)
                _levelSelectRoot.gameObject.SetActive(true);
            if (_settingsPopup != null)
            {
                if (UIManager.HasInstance)
                    UIManager.Instance.HidePopup(PopupId.Settings);
                else
                    _settingsPopup.Hide();
            }
            if (_titleText != null)
                _titleText.text = "Select Level";
        }

        private void ShowSettings()
        {
            EnsureSettingsPopup();
            if (_settingsPopup == null)
                return;

            if (_levelSelectRoot != null)
                _levelSelectRoot.gameObject.SetActive(false);
            if (_mainButtonsRoot != null)
                _mainButtonsRoot.gameObject.SetActive(true);
            if (_titleText != null)
                _titleText.text = _title;

            if (UIManager.HasInstance)
            {
                UIManager.Instance.RegisterPopup(PopupId.Settings, _settingsPopup);
                UIManager.Instance.ShowPopup(PopupId.Settings);
            }
            else
            {
                _settingsPopup.Show();
            }
        }

        private void EnsureSettingsPopup()
        {
            if (_settingsPopup != null)
            {
                _settingsPopup.Hide();
                return;
            }

            Transform canvasRoot = transform;
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
                canvasRoot = canvas.transform;

            _settingsPopup = GetComponentInChildren<SettingsPopup>(true);
            if (_settingsPopup != null)
            {
                _settingsPopup.Hide();
                return;
            }

            _settingsPopup = SettingsPopup.CreateRuntime(canvasRoot);
            _settingsPopup.Hide();
        }

        private void RebuildLevelButtons()
        {
            if (_levelSelectRoot == null)
                return;

            for (int i = 0; i < _levelButtons.Count; i++)
            {
                if (_levelButtons[i] != null)
                    Destroy(_levelButtons[i].gameObject);
            }

            _levelButtons.Clear();

            Transform gridRoot = _levelSelectRoot.Find("LevelGrid");
            if (gridRoot == null)
            {
                var gridGo = new GameObject("LevelGrid", typeof(RectTransform));
                gridGo.transform.SetParent(_levelSelectRoot, false);
                RectTransform gridRect = gridGo.GetComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0.1f, 0.24f);
                gridRect.anchorMax = new Vector2(0.9f, 0.68f);
                gridRect.offsetMin = Vector2.zero;
                gridRect.offsetMax = Vector2.zero;

                GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(280f, 110f);
                grid.spacing = new Vector2(24f, 24f);
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                gridRoot = gridGo.transform;
            }

            GridLayoutGroup levelGrid = gridRoot.GetComponent<GridLayoutGroup>();
            RectTransform prefabRect = _levelButtonPrefab != null
                ? _levelButtonPrefab.transform as RectTransform
                : null;
            if (levelGrid != null && prefabRect != null &&
                prefabRect.sizeDelta.x > 0f && prefabRect.sizeDelta.y > 0f)
            {
                levelGrid.cellSize = prefabRect.sizeDelta;
            }

            if (!LevelManager.HasInstance)
                return;

            IReadOnlyList<LevelDataSO> levels = LevelManager.Instance.Levels;
            for (int i = 0; i < levels.Count; i++)
            {
                bool unlocked = LevelManager.Instance.IsLevelUnlocked(i);
                string label = string.Format("Level {0}", i + 1);
                if (!unlocked)
                    label += "\nLocked";

                int index = i;
                bool usesPrefab = _levelButtonPrefab != null;
                Button button = usesPrefab
                    ? Instantiate(_levelButtonPrefab, gridRoot, false)
                    : CreateMenuButton(
                        gridRoot,
                        $"LevelButton_{i}",
                        label,
                        Vector2.zero,
                        Vector2.one,
                        stretchToParent: false);
                button.name = $"LevelButton_{i}";

                TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
                if (labelText != null)
                    labelText.text = label;

                RectTransform rect = button.transform as RectTransform;
                if (rect != null)
                {
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition = Vector2.zero;
                }

                if (!usesPrefab)
                    ApplyLevelButtonArtwork(button, _levelButtonSprite, unlocked);

                button.interactable = unlocked;
                if (unlocked)
                    button.onClick.AddListener(() => HandleLevelButtonClicked(index));

                _levelButtons.Add(button);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

    }
}
