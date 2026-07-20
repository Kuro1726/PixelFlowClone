using System;
using PixelFlowClone.Managers;
using PixelFlowClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Defeat overlay: "Jammed!", "Out of moves", "Retry" (P3-13).
    /// Visibility is driven by <see cref="UIManager"/> (P3-15).
    /// </summary>
    public class DefeatPopup : MonoBehaviour
    {
        private const string PanelResourcePath = "UI/Popups/Defeat/DefeatPanel_Cutout";
        private const string RestartButtonResourcePath =
            "UI/Popups/Pause/RestartLevelButton_Cutout";
        private const string CloseButtonResourcePath =
            "UI/Popups/Pause/CloseButton_Cutout";

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _subtitleLabel;

        public event Action RetryClicked;

        public bool HasMainMenuButton => _mainMenuButton != null;

        public bool UsesRestartLevelButtonArtwork
        {
            get
            {
                if (_retryButton == null || _retryButton.name != "RestartLevelButton")
                    return false;

                Image image = _retryButton.GetComponent<Image>();
                return image != null &&
                       image.sprite != null &&
                       image.sprite.name == "RestartLevelButton_Cutout";
            }
        }
        public bool UsesDefeatPanelArtwork
        {
            get
            {
                Transform panel = transform.Find("Panel");
                Image image = panel != null ? panel.GetComponent<Image>() : null;
                return image != null &&
                       image.sprite != null &&
                       image.sprite.name == "DefeatPanel_Cutout";
            }
        }

        public bool UsesMainMenuCloseButtonArtwork
        {
            get
            {
                if (_closeButton == null || _closeButton.name != "CloseButton")
                    return false;

                Image image = _closeButton.GetComponent<Image>();
                return image != null &&
                       image.sprite != null &&
                       image.sprite.name == "CloseButton_Cutout";
            }
        }

        private void Awake()
        {
            ResolveEditableUiReferences();
            // A scene instance may intentionally omit optional controls while editing its
            // existing hierarchy. Only create a full overlay when no panel exists at all.
            if (transform.Find("Panel") == null)
                BuildRuntimeUi();

            EnsureCloseButton();
            EnsureMainMenuButton();
            ApplyPanelArtwork();
            ApplyRestartButtonArtwork();
            ApplyCloseButtonArtwork();
            EnsureCanvasGroup();
            WireButtons();
            Hide();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void OnDestroy()
        {
            if (UIManager.HasInstance)
                UIManager.Instance.UnregisterPopup(PopupId.Defeat, this);
            UnwireButtons();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureCanvasGroup();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void Hide()
        {
            EnsureCanvasGroup();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        /// <summary>Builds and serializes the hierarchy for prefab editing.</summary>
        public void BuildEditableUi()
        {
            ResolveEditableUiReferences();
            if (transform.Find("Panel") == null)
                BuildRuntimeUi();

            EnsureCloseButton();
            EnsureMainMenuButton();
            ApplyPanelArtwork();
            ApplyRestartButtonArtwork();
            ApplyCloseButtonArtwork();
            EnsureCanvasGroup();
        }

        private void ResolveEditableUiReferences()
        {
            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            if (_titleLabel == null)
                _titleLabel = panel.Find("Title")?.GetComponent<TMP_Text>();
            if (_subtitleLabel == null)
                _subtitleLabel = panel.Find("Subtitle")?.GetComponent<TMP_Text>();

            if (_retryButton == null)
            {
                Transform retry = panel.Find("RestartLevelButton") ?? panel.Find("RetryButton");
                _retryButton = retry != null ? retry.GetComponent<Button>() : null;
            }

            if (_closeButton == null)
                _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();
            if (_mainMenuButton == null)
                _mainMenuButton = panel.Find("MainMenuButton")?.GetComponent<Button>();
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void WireButtons()
        {
            if (_retryButton == null)
                return;

            _retryButton.onClick.RemoveListener(HandleRetryClicked);
            _retryButton.onClick.AddListener(HandleRetryClicked);

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
                _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            }
        }

        private void UnwireButtons()
        {
            if (_retryButton == null)
            {
                if (_closeButton != null)
                    _closeButton.onClick.RemoveListener(HandleCloseClicked);
                if (_mainMenuButton != null)
                    _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
                return;
            }

            _retryButton.onClick.RemoveListener(HandleRetryClicked);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        private void HandleRetryClicked()
        {
            RetryClicked?.Invoke();

            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Defeat);
            else
                Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[DefeatPopup] Retry clicked but LevelManager is missing.");
                return;
            }

            LevelManager levels = LevelManager.Instance;
            if (!levels.LoadLevel(levels.CurrentLevelIndex))
                Debug.LogWarning("[DefeatPopup] Retry LoadLevel failed.");
        }

        private void HandleCloseClicked()
        {
            HandleMainMenuClicked();
        }

        private void HandleMainMenuClicked()
        {
            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Defeat);
            else
                Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[DefeatPopup] Close clicked but LevelManager is missing.");
                return;
            }

            LevelManager.Instance.ReturnToMainMenu();
        }

        /// <summary>
        /// Builds a defeat overlay under <paramref name="parent"/> (typically the gameplay HUD canvas).
        /// </summary>
        public static DefeatPopup Create(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            return CreateRuntime(parent);
        }

        public static DefeatPopup CreateRuntime(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var root = new GameObject("DefeatPopup", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.AddComponent<DefeatPopup>();
        }

        private void BuildRuntimeUi()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            StretchFull(root);
            EnsureCanvasGroup();

            Image dim = CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.72f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            Image panel = CreateImage(transform, "Panel", new Color(0.18f, 0.12f, 0.12f, 1f));
            panel.rectTransform.anchorMin = new Vector2(0.12f, 0.30f);
            panel.rectTransform.anchorMax = new Vector2(0.88f, 0.70f);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            panel.raycastTarget = true;

            _titleLabel = CreateLabel(panel.transform, "Title", "Jammed!", 56f, FontStyles.Bold);
            _titleLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.62f);
            _titleLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
            _titleLabel.rectTransform.offsetMin = Vector2.zero;
            _titleLabel.rectTransform.offsetMax = Vector2.zero;
            _titleLabel.color = new Color(1f, 0.45f, 0.4f, 1f);

            _subtitleLabel = CreateLabel(panel.transform, "Subtitle", "Out of moves", 32f, FontStyles.Normal);
            _subtitleLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
            _subtitleLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.62f);
            _subtitleLabel.rectTransform.offsetMin = Vector2.zero;
            _subtitleLabel.rectTransform.offsetMax = Vector2.zero;
            _subtitleLabel.color = new Color(0.85f, 0.8f, 0.78f, 1f);

            _retryButton = CreateButton(
                panel.transform, "RestartLevelButton", "Restart Level",
                new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.36f));

            _closeButton = CreateButton(
                panel.transform, "CloseButton", string.Empty,
                new Vector2(0.80f, 0.78f), new Vector2(0.96f, 0.96f));

            SetAnchors(_retryButton, new Vector2(0.15f, 0.24f), new Vector2(0.85f, 0.44f));
            _mainMenuButton = CreateMainMenuButton(panel.transform);

            ApplyRestartButtonArtwork();
            ApplyCloseButtonArtwork();
        }

        private void EnsureCloseButton()
        {
            if (_closeButton != null)
                return;

            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            Transform existing = panel.Find("CloseButton");
            if (existing != null)
                _closeButton = existing.GetComponent<Button>();

            if (_closeButton == null)
            {
                _closeButton = CreateButton(
                    panel, "CloseButton", string.Empty,
                    new Vector2(0.80f, 0.78f), new Vector2(0.96f, 0.96f));
            }
        }
        private void EnsureMainMenuButton()
        {
            if (_mainMenuButton != null)
                return;

            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            Transform existing = panel.Find("MainMenuButton");
            if (existing != null)
                _mainMenuButton = existing.GetComponent<Button>();

            if (_mainMenuButton == null)
            {
                SetAnchors(_retryButton, new Vector2(0.15f, 0.24f), new Vector2(0.85f, 0.44f));
                _mainMenuButton = CreateMainMenuButton(panel);
            }
        }

        private static Button CreateMainMenuButton(Transform parent)
        {
            Button button = CreateButton(
                parent, "MainMenuButton", "MAIN MENU",
                new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.22f));

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.18f, 0.55f, 0.92f, 1f);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.72f, 0.84f, 1f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            return button;
        }

        private static void SetAnchors(Button button, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (button == null)
                return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ApplyPanelArtwork()

        {
            Transform panelTransform = transform.Find("Panel");
            if (panelTransform == null)
                return;

            Image panel = panelTransform.GetComponent<Image>();
            if (panel == null)
                return;

            Sprite sprite = Resources.Load<Sprite>(PanelResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[DefeatPopup] Panel artwork was not found at Resources/{PanelResourcePath}.");
                return;
            }

            panel.sprite = sprite;
            panel.type = Image.Type.Simple;
            panel.preserveAspect = true;
            panel.color = Color.white;
        }
        private void ApplyRestartButtonArtwork()
        {
            if (_retryButton == null)
                return;

            Sprite sprite = Resources.Load<Sprite>(RestartButtonResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[DefeatPopup] Restart artwork was not found at Resources/{RestartButtonResourcePath}.");
                return;
            }

            _retryButton.name = "RestartLevelButton";

            Image image = _retryButton.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            _retryButton.targetGraphic = image;

            TMP_Text label = _retryButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
                label.gameObject.SetActive(false);

            ColorBlock colors = _retryButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.78f, 0.86f, 1f, 1f);
            colors.selectedColor = Color.white;
            _retryButton.colors = colors;
        }

        private void ApplyCloseButtonArtwork()
        {
            if (_closeButton == null)
                return;

            Sprite sprite = Resources.Load<Sprite>(CloseButtonResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[DefeatPopup] Close artwork was not found at Resources/{CloseButtonResourcePath}.");
                return;
            }

            _closeButton.name = "CloseButton";

            Image image = _closeButton.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            _closeButton.targetGraphic = image;

            TMP_Text label = _closeButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
                label.gameObject.SetActive(false);

            ColorBlock colors = _closeButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(1f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            _closeButton.colors = colors;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Image image = CreateImage(parent, name, new Color(0.65f, 0.28f, 0.28f, 1f));
            image.rectTransform.anchorMin = anchorMin;
            image.rectTransform.anchorMax = anchorMax;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.8f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.45f, 0.18f, 0.18f, 1f);
            button.colors = colors;

            TMP_Text labelText = CreateLabel(image.transform, "Label", label, 36f, FontStyles.Bold);
            StretchFull(labelText.rectTransform);
            labelText.raycastTarget = false;
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
