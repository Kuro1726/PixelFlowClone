using System;
using System.Collections.Generic;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.UI.Popups;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Screens
{
    /// <summary>
    /// Main menu: Play opens level pick, then loads gameplay. Also Instructions / Settings.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        public const string DefaultTitle = "Pixel Flow";

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _instructionsButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private RectTransform _mainButtonsRoot;
        [SerializeField] private RectTransform _levelSelectRoot;
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private string _title = DefaultTitle;

        public event Action PlayClicked;
        public event Action InstructionsClicked;
        public event Action SettingsClicked;
        public event Action<int> LevelChosen;

        private readonly List<Button> _levelButtons = new();

        private void Awake()
        {
            EnsureEventSystem();

            if (_playButton == null || _instructionsButton == null || _settingsButton == null)
                BuildRuntimeUi();

            EnsureSettingsPopup();

            if (_titleText != null)
                _titleText.text = _title;

            ShowMainButtons();
            WireButtons();
            RebuildLevelButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        private void WireButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(HandlePlayClicked);
                _playButton.onClick.AddListener(HandlePlayClicked);
            }

            if (_instructionsButton != null)
            {
                _instructionsButton.onClick.RemoveListener(HandleInstructionsClicked);
                _instructionsButton.onClick.AddListener(HandleInstructionsClicked);
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
            if (_instructionsButton != null)
                _instructionsButton.onClick.RemoveListener(HandleInstructionsClicked);
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

        private void HandleInstructionsClicked()
        {
            Debug.Log("[MainMenu] Instructions clicked");
            InstructionsClicked?.Invoke();
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
                _settingsPopup.Hide();
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
                _settingsPopup.Hide();
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

            _settingsPopup.Show();
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
                Button button = CreateMenuButton(
                    gridRoot,
                    $"LevelButton_{i}",
                    label,
                    Vector2.zero,
                    Vector2.one,
                    stretchToParent: false);

                RectTransform rect = button.transform as RectTransform;
                if (rect != null)
                {
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition = Vector2.zero;
                }

                Image image = button.GetComponent<Image>();
                if (image != null)
                    image.color = unlocked
                        ? new Color(0.22f, 0.45f, 0.72f, 1f)
                        : new Color(0.25f, 0.27f, 0.32f, 1f);

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

        public static MainMenuScreen CreateRuntime(Transform parent = null)
        {
            EnsureEventSystem();

            var root = new GameObject("PF_MainMenu", typeof(RectTransform));
            if (parent != null)
                root.transform.SetParent(parent, false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            root.AddComponent<GraphicRaycaster>();

            return root.AddComponent<MainMenuScreen>();
        }

        private void BuildRuntimeUi()
        {
            Transform root = transform;

            Canvas existingCanvas = GetComponentInChildren<Canvas>();
            if (existingCanvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(RectTransform));
                canvasGo.transform.SetParent(transform, false);
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                canvasGo.AddComponent<GraphicRaycaster>();
                root = canvasGo.transform;
            }
            else
            {
                root = existingCanvas.transform;
            }

            Image backdrop = CreateImage(root, "Backdrop", new Color(0.1f, 0.12f, 0.16f, 1f));
            StretchFull(backdrop.rectTransform);

            _titleText = CreateLabel(root, "Title", _title, 72f, FontStyles.Bold);
            _titleText.rectTransform.anchorMin = new Vector2(0.1f, 0.72f);
            _titleText.rectTransform.anchorMax = new Vector2(0.9f, 0.88f);
            _titleText.rectTransform.offsetMin = Vector2.zero;
            _titleText.rectTransform.offsetMax = Vector2.zero;

            var mainGo = new GameObject("MainButtons", typeof(RectTransform));
            mainGo.transform.SetParent(root, false);
            _mainButtonsRoot = mainGo.GetComponent<RectTransform>();
            StretchFull(_mainButtonsRoot);

            _playButton = CreateMenuButton(
                _mainButtonsRoot, "PlayButton", "Play", new Vector2(0.2f, 0.48f), new Vector2(0.8f, 0.58f));
            _instructionsButton = CreateMenuButton(
                _mainButtonsRoot, "InstructionsButton", "Instructions", new Vector2(0.2f, 0.36f), new Vector2(0.8f, 0.46f));
            _settingsButton = CreateMenuButton(
                _mainButtonsRoot, "SettingsButton", "Settings", new Vector2(0.2f, 0.24f), new Vector2(0.8f, 0.34f));

            var levelGo = new GameObject("LevelSelect", typeof(RectTransform));
            levelGo.transform.SetParent(root, false);
            _levelSelectRoot = levelGo.GetComponent<RectTransform>();
            StretchFull(_levelSelectRoot);
            levelGo.SetActive(false);

            _backButton = CreateMenuButton(
                _levelSelectRoot, "BackButton", "Back", new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.20f));

            EnsureSettingsPopup();
        }

        private static Button CreateMenuButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool stretchToParent = true)
        {
            Image image = CreateImage(parent, name, new Color(0.22f, 0.45f, 0.72f, 1f));
            if (stretchToParent)
            {
                image.rectTransform.anchorMin = anchorMin;
                image.rectTransform.anchorMax = anchorMax;
                image.rectTransform.offsetMin = Vector2.zero;
                image.rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                image.rectTransform.sizeDelta = new Vector2(280f, 110f);
            }

            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.55f, 0.85f, 1f);
            colors.pressedColor = new Color(0.15f, 0.35f, 0.55f, 1f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.22f, 0.85f);
            button.colors = colors;

            TMP_Text text = CreateLabel(image.transform, "Label", label, 36f, FontStyles.Bold);
            StretchFull(text.rectTransform);
            text.raycastTarget = false;

            return button;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
