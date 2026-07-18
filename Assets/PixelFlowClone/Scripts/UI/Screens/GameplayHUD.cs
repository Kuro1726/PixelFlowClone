using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Screens
{
    /// <summary>
    /// Gameplay overlay HUD: top bar + conveyor free-slot indicator (remaining/max).
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _topBarRoot;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _conveyorLabel;

        public Button PauseButton => _pauseButton;
        public TMP_Text LevelLabel => _levelLabel;
        public TMP_Text ConveyorLabel => _conveyorLabel;

        private void Awake()
        {
            EnsureEventSystem();
            RebuildUi();
            SetLevelIndex(0);
            SetConveyorCount(0, 5);
        }

        private void Start()
        {
            // Conveyor/GameConfig may not exist yet during Awake — refresh HUD sizes once ready.
            if (ResolveConfig() != null)
                RebuildUi();
        }

        private void RebuildUi()
        {
            if (_topBarRoot != null)
            {
                Destroy(_topBarRoot.gameObject);
                _topBarRoot = null;
                _pauseButton = null;
                _levelLabel = null;
                _conveyorLabel = null;
            }

            BuildRuntimeUi();
        }

        private void OnEnable()
        {
            GameEvents.OnConveyorCountChanged -= HandleConveyorCountChanged;
            GameEvents.OnConveyorCountChanged += HandleConveyorCountChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnConveyorCountChanged -= HandleConveyorCountChanged;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
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

        private void HandleConveyorCountChanged(int active, int max)
        {
            SetConveyorCount(active, max);
        }

        /// <summary>
        /// Builds a Screen Space Overlay HUD at runtime / for prefab baking.
        /// </summary>
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

            // Clear leftover Queue / old Conveyor widgets from earlier builds.
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

            Image topBg = topGo.AddComponent<Image>();
            topBg.color = new Color(0.08f, 0.1f, 0.14f, 0.82f);
            topBg.raycastTarget = false;

            _pauseButton = CreateHudButton(
                _topBarRoot, "PauseButton", "Pause",
                new Vector2(0.02f, 0.14f), new Vector2(0.22f, 0.86f), fontSize: fontSize * 0.9f);

            Image conveyorBadge = CreateImage(
                _topBarRoot, "ConveyorBadge", new Color(0.16f, 0.2f, 0.28f, 1f));
            conveyorBadge.rectTransform.anchorMin = new Vector2(0.28f, 0.14f);
            conveyorBadge.rectTransform.anchorMax = new Vector2(0.58f, 0.86f);
            conveyorBadge.rectTransform.offsetMin = Vector2.zero;
            conveyorBadge.rectTransform.offsetMax = Vector2.zero;
            conveyorBadge.raycastTarget = false;

            _conveyorLabel = CreateLabel(
                conveyorBadge.transform, "ConveyorLabel", "5/5", fontSize, FontStyles.Bold);
            StretchFull(_conveyorLabel.rectTransform);

            _levelLabel = CreateLabel(
                _topBarRoot, "LevelLabel", "Level 1", fontSize, FontStyles.Bold);
            _levelLabel.rectTransform.anchorMin = new Vector2(0.62f, 0.1f);
            _levelLabel.rectTransform.anchorMax = new Vector2(0.98f, 0.9f);
            _levelLabel.rectTransform.offsetMin = Vector2.zero;
            _levelLabel.rectTransform.offsetMax = Vector2.zero;
            _levelLabel.alignment = TextAlignmentOptions.MidlineRight;
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
                Destroy(child.gameObject);
        }

        private static Button CreateHudButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize = 28f)
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

            TMP_Text text = CreateLabel(image.transform, "Label", label, fontSize, FontStyles.Bold);
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

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
