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
    public partial class GameplayHUD
    {
        public static GameplayHUD CreateRuntime(Transform parent = null)
        {
            EnsureEventSystem();

            var root = new GameObject("PF_GameplayHUD", typeof(RectTransform));
            if (parent != null)
                root.transform.SetParent(parent, false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<CanvasGroup>();

            return root.AddComponent<GameplayHUD>();
        }

        private void BuildRuntimeUi()
        {
            Transform root = transform;
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = GetComponentInChildren<Canvas>();

            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            root = canvas.transform;

            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup = group;

            DestroyChildIfExists(root, "QueueBar");
            DestroyChildIfExists(root, "QueueLabel");
            DestroyChildIfExists(root, "QueueCaption");
            DestroyChildIfExists(root, "ConveyorLabel");
            DestroyChildIfExists(root, "ConveyorCaption");
            DestroyChildIfExists(root, "TopBar");

            GameConfigSO config = ResolveConfig();
            float barHeight = config != null ? Mathf.Clamp(config.HudBarHeight, 0.04f, 0.14f) : 0.08f;
            float fontSize = config != null ? Mathf.Clamp(config.HudFontSize, 18f, 48f) : 32f;

            var topGo = new GameObject("TopBar", typeof(RectTransform));
            topGo.transform.SetParent(root, false);
            _topBarRoot = topGo.GetComponent<RectTransform>();
            _topBarRoot.anchorMin = new Vector2(0f, 1f - barHeight);
            _topBarRoot.anchorMax = new Vector2(1f, 1f);
            _topBarRoot.offsetMin = Vector2.zero;
            _topBarRoot.offsetMax = Vector2.zero;

            _pauseButton = CreateHudButton(
                _topBarRoot, "PauseButton", "Pause", _pauseIcon,
                new Vector2(0.02f, 0.14f), new Vector2(0.22f, 0.86f), fontSize: fontSize * 0.9f,
                out _pauseButtonLabel);

            var conveyorBadgeGo = new GameObject("ConveyorBadge", typeof(RectTransform));
            conveyorBadgeGo.transform.SetParent(_topBarRoot, false);
            RectTransform conveyorBadge = conveyorBadgeGo.GetComponent<RectTransform>();
            conveyorBadge.anchorMin = new Vector2(0.28f, 0.14f);
            conveyorBadge.anchorMax = new Vector2(0.58f, 0.86f);
            conveyorBadge.offsetMin = Vector2.zero;
            conveyorBadge.offsetMax = Vector2.zero;

            _conveyorLabel = CreateLabel(
                conveyorBadge, "ConveyorLabel", "5/5", fontSize * 1.35f, FontStyles.Bold);
            StretchFull(_conveyorLabel.rectTransform);

            Image levelBadge = CreateImage(
                _topBarRoot,
                "LevelBadge",
                _levelBadgeSprite != null ? Color.white : Color.clear);
            levelBadge.sprite = _levelBadgeSprite;
            levelBadge.preserveAspect = true;
            levelBadge.rectTransform.anchorMin = new Vector2(0.62f, 0.1f);
            levelBadge.rectTransform.anchorMax = new Vector2(0.98f, 0.9f);
            levelBadge.rectTransform.offsetMin = Vector2.zero;
            levelBadge.rectTransform.offsetMax = Vector2.zero;

            _levelLabel = CreateLabel(
                levelBadge.transform, "LevelLabel", "Level 1", fontSize, FontStyles.Bold);
            StretchFull(_levelLabel.rectTransform);

            _topBarRoot.gameObject.SetActive(_gameplayElementsVisible);
        }

        private static GameConfigSO ResolveConfig()
        {
            if (ConveyorPathManager.HasInstance && ConveyorPathManager.Instance.Config != null)
                return ConveyorPathManager.Instance.Config;
            return null;
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                DestroyUiObject(child.gameObject);
        }

        private static void DestroyUiObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private static Button CreateHudButton(
            Transform parent,
            string name,
            string label,
            Sprite icon,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            out TMP_Text labelText)
        {
            Image image = CreateImage(
                parent,
                name,
                icon != null ? Color.white : new Color(0.22f, 0.45f, 0.72f, 1f));
            image.rectTransform.anchorMin = anchorMin;
            image.rectTransform.anchorMax = anchorMax;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = true;
            image.sprite = icon;
            image.preserveAspect = icon != null;

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = icon != null
                ? new Color(0.92f, 0.92f, 0.92f, 1f)
                : new Color(0.3f, 0.55f, 0.85f, 1f);
            colors.pressedColor = icon != null
                ? new Color(0.78f, 0.78f, 0.78f, 1f)
                : new Color(0.15f, 0.35f, 0.55f, 1f);
            button.colors = colors;

            labelText = null;
            if (icon == null)
            {
                labelText = CreateLabel(image.transform, "Label", label, fontSize, FontStyles.Bold);
                StretchFull(labelText.rectTransform);
                labelText.raycastTarget = false;
            }

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
