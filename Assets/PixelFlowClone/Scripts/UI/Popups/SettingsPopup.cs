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
        [SerializeField] private Button _audioToggleButton;
        [SerializeField] private TMP_Text _audioToggleLabel;
        [SerializeField] private Button _hapticToggleButton;
        [SerializeField] private TMP_Text _hapticToggleLabel;
        [SerializeField] private Button _closeButton;

        public event Action Closed;

        private void Awake()
        {
            if (_audioToggleButton == null || _hapticToggleButton == null || _closeButton == null)
                BuildRuntimeUi();

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
            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Settings);
            else
                Hide();

            Closed?.Invoke();
        }

        private void RefreshLabels()
        {
            if (_audioToggleLabel != null)
                _audioToggleLabel.text = GameSettings.AudioEnabled ? "Audio: ON" : "Audio: OFF";

            if (_hapticToggleLabel != null)
                _hapticToggleLabel.text = GameSettings.HapticEnabled ? "Haptic: ON" : "Haptic: OFF";

            ApplyToggleColor(_audioToggleButton, GameSettings.AudioEnabled);
            ApplyToggleColor(_hapticToggleButton, GameSettings.HapticEnabled);
        }

        private static void ApplyToggleColor(Button button, bool enabled)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            image.color = enabled
                ? new Color(0.22f, 0.45f, 0.72f, 1f)
                : new Color(0.35f, 0.28f, 0.28f, 1f);
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
                root = gameObject.AddComponent<RectTransform>();

            StretchFull(root);

            Image dim = CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.65f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            Image panel = CreateImage(transform, "Panel", new Color(0.12f, 0.14f, 0.18f, 1f));
            panel.rectTransform.anchorMin = new Vector2(0.12f, 0.28f);
            panel.rectTransform.anchorMax = new Vector2(0.88f, 0.72f);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            panel.raycastTarget = true;

            TMP_Text title = CreateLabel(panel.transform, "Title", "Settings", 48f, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            _audioToggleButton = CreateToggleButton(
                panel.transform, "AudioToggle", "Audio: ON",
                new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.68f), out _audioToggleLabel);

            _hapticToggleButton = CreateToggleButton(
                panel.transform, "HapticToggle", "Haptic: ON",
                new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.48f), out _hapticToggleLabel);

            _closeButton = CreateToggleButton(
                panel.transform, "CloseButton", "Close",
                new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.22f), out _);
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
