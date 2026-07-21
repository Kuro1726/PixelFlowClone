using System;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Settings overlay: Audio ON/OFF and Haptic ON/OFF, persisted via <see cref="GameSettings"/>.
    /// </summary>
    public class SettingsPopup : MonoBehaviour
    {
        private const string PanelResourcePath = "UI/Popups/Pause/PausePanel_Cutout";
        private const string SoundIconResourcePath = "UI/Settings/SoundIcon_Cutout";
        private const string HapticIconResourcePath = "UI/Settings/HapticIcon_Cutout";
        private const string ToggleOffResourcePath = "UI/Settings/ToggleOff_Cutout";
        private const string ToggleOnResourcePath = "UI/Settings/ToggleOn_Cutout";

        [SerializeField] private Image _panelImage;
        [SerializeField] private Button _audioToggleButton;
        [SerializeField] private TMP_Text _audioToggleLabel;
        [SerializeField] private Image _audioIconImage;
        [SerializeField] private Image _audioToggleStateImage;
        [SerializeField] private Button _hapticToggleButton;
        [SerializeField] private TMP_Text _hapticToggleLabel;
        [SerializeField] private Image _hapticIconImage;
        [SerializeField] private Image _hapticToggleStateImage;
        [SerializeField] private Button _closeButton;

        private Sprite _toggleOffSprite;
        private Sprite _toggleOnSprite;

        public event Action Closed;

        public bool HasSettingsArtwork =>
            _panelImage != null && _panelImage.sprite != null &&
            _audioIconImage != null && _audioIconImage.sprite != null &&
            _audioToggleStateImage != null &&
            _hapticIconImage != null && _hapticIconImage.sprite != null &&
            _hapticToggleStateImage != null &&
            Resources.Load<Sprite>(ToggleOffResourcePath) != null &&
            Resources.Load<Sprite>(ToggleOnResourcePath) != null;

        private void Awake()
        {
            RecoverReferencesFromHierarchy();
            if (_audioToggleButton == null || _hapticToggleButton == null || _closeButton == null)
                BuildRuntimeUi();

            EnsurePanelArtwork();
            EnsureSettingsArtwork();
            WireButtons();
            RefreshLabels();
        }

        private void OnEnable()
        {
            GameSettings.SettingsChanged -= RefreshLabels;
            GameSettings.SettingsChanged += RefreshLabels;
            RefreshLabels();
        }

        private void OnDisable()
        {
            GameSettings.SettingsChanged -= RefreshLabels;
        }

        private void OnDestroy()
        {
            UnwireButtons();
            GameSettings.SettingsChanged -= RefreshLabels;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshLabels();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Close()
        {
            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Settings);

            // Also hide this instance directly when it is not registered in UIManager.
            if (gameObject.activeSelf)
                Hide();

            Closed?.Invoke();
        }

        /// <summary>
        /// Creates the popup hierarchy ahead of Play Mode so its RectTransforms and
        /// graphics can be adjusted directly in the Unity Editor.
        /// </summary>
        public void BuildEditableUi()
        {
            RecoverReferencesFromHierarchy();
            if (_audioToggleButton == null || _hapticToggleButton == null || _closeButton == null)
                BuildRuntimeUi();

            EnsurePanelArtwork();
            EnsureSettingsArtwork();
            RefreshLabels();
        }

        private void WireButtons()
        {
            if (_audioToggleButton != null)
            {
                _audioToggleButton.onClick.RemoveListener(HandleAudioClicked);
                _audioToggleButton.onClick.AddListener(HandleAudioClicked);
            }

            if (_hapticToggleButton != null)
            {
                _hapticToggleButton.onClick.RemoveListener(HandleHapticClicked);
                _hapticToggleButton.onClick.AddListener(HandleHapticClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }
        }

        private void UnwireButtons()
        {
            if (_audioToggleButton != null)
                _audioToggleButton.onClick.RemoveListener(HandleAudioClicked);
            if (_hapticToggleButton != null)
                _hapticToggleButton.onClick.RemoveListener(HandleHapticClicked);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        private void HandleAudioClicked()
        {
            GameSettings.ToggleAudio();
            RefreshLabels();
        }

        private void HandleHapticClicked()
        {
            GameSettings.ToggleHaptic();
            RefreshLabels();
            if (GameSettings.HapticEnabled)
                GameSettings.TryHaptic();
        }

        private void HandleCloseClicked()
        {
            Close();
        }

        private void RefreshLabels()
        {
            if (_audioToggleLabel != null)
                _audioToggleLabel.text = GameSettings.AudioEnabled ? "Audio: ON" : "Audio: OFF";

            if (_hapticToggleLabel != null)
                _hapticToggleLabel.text = GameSettings.HapticEnabled ? "Haptic: ON" : "Haptic: OFF";

            ApplyToggleArtwork(_audioToggleStateImage, GameSettings.AudioEnabled);
            ApplyToggleArtwork(_hapticToggleStateImage, GameSettings.HapticEnabled);
        }

        private void ApplyToggleArtwork(Image stateImage, bool enabled)
        {
            if (stateImage == null)
                return;

            Sprite sprite = enabled ? _toggleOnSprite : _toggleOffSprite;
            if (sprite != null)
                stateImage.sprite = sprite;
        }

        private void EnsureSettingsArtwork()
        {
            Sprite soundIcon = Resources.Load<Sprite>(SoundIconResourcePath);
            Sprite hapticIcon = Resources.Load<Sprite>(HapticIconResourcePath);
            _toggleOffSprite = Resources.Load<Sprite>(ToggleOffResourcePath);
            _toggleOnSprite = Resources.Load<Sprite>(ToggleOnResourcePath);

            if (soundIcon == null || hapticIcon == null ||
                _toggleOffSprite == null || _toggleOnSprite == null)
            {
                Debug.LogWarning("[SettingsPopup] One or more settings sprites could not be loaded.");
                return;
            }

            _audioIconImage = EnsureArtworkImage(
                _audioToggleButton, "SoundIcon", soundIcon,
                new Vector2(0.23f, 0f), new Vector2(0.43f, 1f));
            _audioToggleStateImage = EnsureArtworkImage(
                _audioToggleButton, "ToggleState", _toggleOnSprite,
                new Vector2(0.49f, 0f), new Vector2(0.79f, 1f));

            _hapticIconImage = EnsureArtworkImage(
                _hapticToggleButton, "HapticIcon", hapticIcon,
                new Vector2(0.23f, 0f), new Vector2(0.43f, 1f));
            _hapticToggleStateImage = EnsureArtworkImage(
                _hapticToggleButton, "ToggleState", _toggleOnSprite,
                new Vector2(0.49f, 0f), new Vector2(0.79f, 1f));

            ConfigureArtworkButton(
                _audioToggleButton, _audioToggleLabel, _audioToggleStateImage);
            ConfigureArtworkButton(
                _hapticToggleButton, _hapticToggleLabel, _hapticToggleStateImage);
        }

        private void EnsurePanelArtwork()
        {
            if (_panelImage == null)
            {
                Transform panelTransform = transform.Find("Panel");
                if (panelTransform != null)
                    _panelImage = panelTransform.GetComponent<Image>();
            }

            Sprite panelSprite = Resources.Load<Sprite>(PanelResourcePath);
            if (_panelImage == null || panelSprite == null)
            {
                Debug.LogWarning(
                    $"[SettingsPopup] Panel sprite was not found at Resources/{PanelResourcePath}.");
                return;
            }

            _panelImage.sprite = panelSprite;
            _panelImage.type = Image.Type.Simple;
            _panelImage.preserveAspect = false;
            _panelImage.color = Color.white;
            _panelImage.raycastTarget = true;
        }

        private void RecoverReferencesFromHierarchy()
        {
            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            if (_panelImage == null)
                _panelImage = panel.GetComponent<Image>();

            RecoverToggleReferences(
                panel, "AudioToggle", ref _audioToggleButton, ref _audioToggleLabel);
            RecoverToggleReferences(
                panel, "HapticToggle", ref _hapticToggleButton, ref _hapticToggleLabel);

            if (_closeButton == null)
                _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();
        }

        private static void RecoverToggleReferences(
            Transform panel,
            string buttonName,
            ref Button button,
            ref TMP_Text label)
        {
            if (button == null)
                button = panel.Find(buttonName)?.GetComponent<Button>();

            if (label == null && button != null)
                label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
        }

        private static Image EnsureArtworkImage(
            Button button,
            string name,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            if (button == null || sprite == null)
                return null;

            Transform existing = button.transform.Find(name);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
                image = CreateImage(button.transform, name, Color.white);

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureArtworkButton(
            Button button,
            TMP_Text label,
            Image toggleStateImage)
        {
            if (button == null || toggleStateImage == null)
                return;

            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = null;
                background.color = Color.clear;
            }

            if (label != null)
                label.gameObject.SetActive(false);

            button.targetGraphic = toggleStateImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.72f, 0.82f, 1f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        /// <summary>
        /// Builds a settings overlay under <paramref name="parent"/> (typically the menu canvas).
        /// </summary>
        public static SettingsPopup CreateRuntime(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var root = new GameObject("SettingsPopup", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            SettingsPopup popup = root.AddComponent<SettingsPopup>();
            return popup;
        }

        private void BuildRuntimeUi()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
                StretchFull(root);
            }
            else if (transform.Find("Dim") == null && transform.Find("Panel") == null)
            {
                StretchFull(root);
            }

            Transform dimTransform = transform.Find("Dim");
            if (dimTransform == null)
            {
                Image dim = CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.65f));
                StretchFull(dim.rectTransform);
                dim.raycastTarget = true;
            }

            Transform panelTransform = transform.Find("Panel");
            bool createdPanel = panelTransform == null;
            Image panel = createdPanel
                ? CreateImage(transform, "Panel", Color.white)
                : panelTransform.GetComponent<Image>() ??
                  panelTransform.gameObject.AddComponent<Image>();
            _panelImage = panel;

            if (createdPanel)
            {
                panel.rectTransform.anchorMin = new Vector2(0.12f, 0.28f);
                panel.rectTransform.anchorMax = new Vector2(0.88f, 0.72f);
                panel.rectTransform.offsetMin = Vector2.zero;
                panel.rectTransform.offsetMax = Vector2.zero;
                panel.raycastTarget = true;
            }

            if (panel.transform.Find("Title") == null)
            {
                TMP_Text title = CreateLabel(
                    panel.transform, "Title", "Settings", 48f, FontStyles.Bold);
                title.rectTransform.anchorMin = new Vector2(0.08f, 0.78f);
                title.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
                title.rectTransform.offsetMin = Vector2.zero;
                title.rectTransform.offsetMax = Vector2.zero;
            }

            RecoverReferencesFromHierarchy();

            if (_audioToggleButton == null)
            {
                _audioToggleButton = CreateToggleButton(
                    panel.transform, "AudioToggle", "Audio: ON",
                    new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.68f), out _audioToggleLabel);
            }

            if (_hapticToggleButton == null)
            {
                _hapticToggleButton = CreateToggleButton(
                    panel.transform, "HapticToggle", "Haptic: ON",
                    new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.48f), out _hapticToggleLabel);
            }

            if (_closeButton == null)
            {
                _closeButton = CreateToggleButton(
                    panel.transform, "CloseButton", "Close",
                    new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.22f), out _);
            }
        }

        private static Button CreateToggleButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out TMP_Text labelText)
        {
            Image image = CreateImage(parent, name, new Color(0.22f, 0.45f, 0.72f, 1f));
            image.rectTransform.anchorMin = anchorMin;
            image.rectTransform.anchorMax = anchorMax;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.55f, 0.85f, 1f);
            colors.pressedColor = new Color(0.15f, 0.35f, 0.55f, 1f);
            button.colors = colors;

            labelText = CreateLabel(image.transform, "Label", label, 36f, FontStyles.Bold);
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
