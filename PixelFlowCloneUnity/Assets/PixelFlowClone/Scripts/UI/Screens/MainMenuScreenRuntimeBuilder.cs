using PixelFlowClone.Data;
using PixelFlowClone.UI.Popups;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Screens
{
    public partial class MainMenuScreen
    {
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
            if (_backgroundSprite != null)
            {
                backdrop.sprite = _backgroundSprite;
                backdrop.type = Image.Type.Simple;
                backdrop.preserveAspect = false;
                backdrop.color = Color.white;
            }

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
                _mainButtonsRoot, "PlayButton", "Play",
                new Vector2(0.2f, 0.455f), new Vector2(0.8f, 0.58f),
                labelFontSize: 64f);
            ApplyMenuButtonArtwork(_playButton, _playButtonSprite);
            _settingsButton = CreateMenuButton(
                _mainButtonsRoot, "SettingsButton", "Settings",
                new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.42f),
                labelFontSize: 64f);

            var levelGo = new GameObject("LevelSelect", typeof(RectTransform));
            levelGo.transform.SetParent(root, false);
            _levelSelectRoot = levelGo.GetComponent<RectTransform>();
            StretchFull(_levelSelectRoot);
            levelGo.SetActive(false);

            _backButton = CreateMenuButton(
                _levelSelectRoot, "BackButton", "Back",
                new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.20f),
                labelFontSize: 64f);

            EnsureSettingsPopup();
        }

        private void RemoveObsoleteInstructionsButton()
        {
            if (_mainButtonsRoot == null)
                return;

            Transform instructions = _mainButtonsRoot.Find("InstructionsButton");
            if (instructions == null)
                return;

            if (Application.isPlaying)
                Destroy(instructions.gameObject);
            else
                DestroyImmediate(instructions.gameObject);
        }

        private void ConfigureMainButtonsLayout()
        {
            if (_settingsButton == null)
                return;

            RectTransform settingsRect = _settingsButton.transform as RectTransform;
            if (settingsRect == null)
                return;

            settingsRect.anchorMin = new Vector2(0.2f, 0.32f);
            settingsRect.anchorMax = new Vector2(0.8f, 0.42f);
            settingsRect.offsetMin = Vector2.zero;
            settingsRect.offsetMax = Vector2.zero;
        }

        private void ApplyConfiguredButtonArtwork()
        {
            ApplyMenuButtonArtwork(_playButton, _playButtonSprite);
            ApplyMenuButtonArtwork(_settingsButton, _playButtonSprite);
            ApplyMenuButtonArtwork(_backButton, _playButtonSprite);
        }

        private static void ApplyMenuButtonArtwork(Button button, Sprite artworkSprite)
        {
            if (button == null || artworkSprite == null)
                return;

            Image hitArea = button.GetComponent<Image>();
            if (hitArea != null)
            {
                hitArea.sprite = null;
                hitArea.color = Color.clear;
            }

            Transform artworkTransform = button.transform.Find("Artwork");
            Image artwork = artworkTransform != null
                ? artworkTransform.GetComponent<Image>()
                : null;
            if (artwork == null)
            {
                artwork = artworkTransform != null
                    ? artworkTransform.gameObject.AddComponent<Image>()
                    : CreateImage(button.transform, "Artwork", Color.white);
            }

            artwork.sprite = artworkSprite;
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = true;
            artwork.color = Color.white;
            artwork.raycastTarget = false;

            // The generated PNG contains transparent padding. This size makes its visible
            // 1121x415 alpha bounds fill the 648x240 Play button without stretching.
            RectTransform artworkRect = artwork.rectTransform;
            artworkRect.anchorMin = new Vector2(0.5f, 0.5f);
            artworkRect.anchorMax = new Vector2(0.5f, 0.5f);
            artworkRect.anchoredPosition = Vector2.zero;
            artworkRect.sizeDelta = new Vector2(888f, 592f);
            artwork.transform.SetAsFirstSibling();

            button.targetGraphic = artwork;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.98f, 0.85f, 1f);
            colors.pressedColor = new Color(0.9f, 0.78f, 0.42f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            TMP_Text label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
                label.transform.SetAsLastSibling();
        }

        private static void ApplyLevelButtonArtwork(
            Button button,
            Sprite artworkSprite,
            bool unlocked)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            if (artworkSprite == null)
            {
                image.color = unlocked
                    ? new Color(0.22f, 0.45f, 0.72f, 1f)
                    : new Color(0.25f, 0.27f, 0.32f, 1f);
                return;
            }

            image.sprite = artworkSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.8f, 1f);
            colors.pressedColor = new Color(0.86f, 0.72f, 0.38f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.9f);
            button.colors = colors;
        }

        private static Button CreateMenuButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool stretchToParent = true,
            float labelFontSize = 36f)
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

            TMP_Text text = CreateLabel(
                image.transform, "Label", label, labelFontSize, FontStyles.Bold);
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
