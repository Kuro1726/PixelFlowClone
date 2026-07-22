using System;
using PixelFlowClone.Data;
using PixelFlowClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    public partial class PausePopup
    {
        public static PausePopup CreateRuntime(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var root = new GameObject("PausePopup", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.AddComponent<PausePopup>();
        }

        private void BuildRuntimeUi()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            StretchFull(root);
            EnsureCanvasGroup();

            Image dim = CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            Image panel = CreateImage(transform, "Panel", new Color(0.12f, 0.14f, 0.18f, 1f));
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchoredPosition = Vector2.zero;
            panel.rectTransform.sizeDelta = new Vector2(820f, 1230f);
            panel.raycastTarget = true;

            Sprite panelSprite = Resources.Load<Sprite>(PausePanelResourcePath);
            if (panelSprite != null)
            {
                panel.sprite = panelSprite;
                panel.type = Image.Type.Simple;
                panel.preserveAspect = false;
                panel.color = Color.white;
            }
            else
            {
                Debug.LogWarning(
                    $"[PausePopup] Panel sprite was not found at Resources/{PausePanelResourcePath}.");
            }

            _titleLabel = CreateLabel(panel.transform, "Title", "Paused", 52f, FontStyles.Bold);
            _titleLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.78f);
            _titleLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            _titleLabel.rectTransform.offsetMin = Vector2.zero;
            _titleLabel.rectTransform.offsetMax = Vector2.zero;

            _closeButton = CreateButton(
                panel.transform, "CloseButton", string.Empty,
                new Vector2(0.80f, 0.84f), new Vector2(0.94f, 0.94f),
                Color.white);
            ConfigureManualCloseRect();

            _restartButton = CreateButton(
                panel.transform, "RestartButton", "Restart",
                new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.48f),
                new Color(0.22f, 0.45f, 0.72f, 1f));

            _homeButton = CreateButton(
                panel.transform, "HomeButton", "Home",
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.26f),
                new Color(0.45f, 0.35f, 0.28f, 1f));

            EnsureSettingsControls();
        }

        private void EnsureSettingsControls()
        {
            if (_audioToggleButton != null && _hapticToggleButton != null)
                return;

            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            // Reflow only when upgrading the old three-button layout.
            SetAnchors(_titleLabel, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f));
            SetAnchors(_restartButton, new Vector2(0.18f, 0.27f), new Vector2(0.82f, 0.46f));
            SetAnchors(_homeButton, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.23f));

            if (_audioToggleButton == null)
            {
                _audioToggleButton = CreateButton(
                    panel, "SoundToggle", "Sound: ON",
                    new Vector2(0.08f, 0.69f), new Vector2(0.48f, 0.81f),
                    new Color(0.25f, 0.72f, 0.42f, 1f));
                _audioToggleLabel = _audioToggleButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            }

            if (_hapticToggleButton == null)
            {
                _hapticToggleButton = CreateButton(
                    panel, "HapticToggle", "Haptic: ON",
                    new Vector2(0.52f, 0.69f), new Vector2(0.92f, 0.81f),
                    new Color(0.25f, 0.72f, 0.42f, 1f));
                _hapticToggleLabel = _hapticToggleButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            }
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
                Debug.LogWarning("[PausePopup] One or more settings sprites could not be loaded.");
                return;
            }

            _audioIconImage = EnsureArtworkImage(
                _audioToggleButton, "SoundIcon", soundIcon,
                new Vector2(0f, 0f), new Vector2(0.42f, 1f));
            _audioToggleStateImage = EnsureArtworkImage(
                _audioToggleButton, "ToggleState", _toggleOnSprite,
                new Vector2(0.38f, 0f), new Vector2(1f, 1f));

            _hapticIconImage = EnsureArtworkImage(
                _hapticToggleButton, "HapticIcon", hapticIcon,
                new Vector2(0f, 0f), new Vector2(0.42f, 1f));
            _hapticToggleStateImage = EnsureArtworkImage(
                _hapticToggleButton, "ToggleState", _toggleOnSprite,
                new Vector2(0.38f, 0f), new Vector2(1f, 1f));

            ConfigureArtworkButton(
                _audioToggleButton, _audioToggleLabel, _audioToggleStateImage);
            ConfigureArtworkButton(
                _hapticToggleButton, _hapticToggleLabel, _hapticToggleStateImage);
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

        private void ApplyRestartButtonArtwork()
        {
            if (_restartButton == null)
                return;

            Sprite sprite = Resources.Load<Sprite>(RestartButtonResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[PausePopup] Restart artwork was not found at Resources/{RestartButtonResourcePath}.");
                return;
            }

            Image image = _restartButton.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            _restartButton.targetGraphic = image;

            TMP_Text label = _restartButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
                label.gameObject.SetActive(false);

            ColorBlock colors = _restartButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.78f, 0.86f, 1f, 1f);
            colors.selectedColor = Color.white;
            _restartButton.colors = colors;
        }

        private void ApplyHomeButtonArtwork()
        {
            if (_homeButton == null)
                return;

            Sprite sprite = Resources.Load<Sprite>(HomeButtonResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[PausePopup] Home artwork was not found at Resources/{HomeButtonResourcePath}.");
                return;
            }

            Image image = _homeButton.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            _homeButton.targetGraphic = image;

            TMP_Text label = _homeButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
                label.gameObject.SetActive(false);

            ColorBlock colors = _homeButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(1f, 0.76f, 0.76f, 1f);
            colors.selectedColor = Color.white;
            _homeButton.colors = colors;
        }

        private void ApplyCloseButtonArtwork()
        {
            if (_closeButton == null)
                return;

            Sprite sprite = Resources.Load<Sprite>(CloseButtonResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[PausePopup] Close artwork was not found at Resources/{CloseButtonResourcePath}.");
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

        public void EnsureDirectHeaderEditing()
        {
            if (_titleLabel == null || _closeButton == null)
                return;

            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            Transform header = panel.Find("HeaderRoot");
            LayoutElement titleLayout = _titleLabel.GetComponent<LayoutElement>();
            LayoutElement closeLayout = _closeButton.GetComponent<LayoutElement>();

            if (header != null)
            {
                _titleLabel.transform.SetParent(panel, false);
                _closeButton.transform.SetParent(panel, false);

                SetAnchors(_titleLabel, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f));
                _titleLabel.rectTransform.localScale = Vector3.one;
                ConfigureManualCloseRect();

                _titleLabel.transform.SetAsLastSibling();
                _closeButton.transform.SetAsLastSibling();
            }

            DestroyLayoutObject(titleLayout);
            DestroyLayoutObject(closeLayout);

            if (header != null)
                DestroyLayoutObject(header.gameObject);
        }

        private void ConfigureManualCloseRect()
        {
            RectTransform rect = _closeButton != null
                ? _closeButton.transform as RectTransform
                : null;
            if (rect == null)
                return;

            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-120f, -105f);
            rect.sizeDelta = new Vector2(84f, 84f);
            rect.localScale = Vector3.one;
        }

        private static void DestroyLayoutObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private static void SetAnchors(Component component, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = component != null ? component.transform as RectTransform : null;
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            Image image = CreateImage(parent, name, color);
            image.rectTransform.anchorMin = anchorMin;
            image.rectTransform.anchorMax = anchorMax;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.75f;
            button.colors = colors;

            TMP_Text labelText = CreateLabel(image.transform, "Label", label, 34f, FontStyles.Bold);
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
