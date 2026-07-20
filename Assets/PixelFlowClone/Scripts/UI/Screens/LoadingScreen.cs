using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Screens
{
    /// <summary>
    /// Full-screen loading UI: game title, decorative artwork, and status text ("Loading...").
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public const string DefaultTitle = "Pixel Flow";
        public const string DefaultStatus = "Loading...";

        private const string BackgroundResourcePath = "Backgrounds/Background_loading";
        private const string LogoResourcePath = "Logos/Logo";
        private const string LoadingFontResourcePath = "Fonts/LilitaOne_SDF";
        private const string PrefabResourcePath = "UI/PF_LoadingScreen";

        private static readonly string[] LoadingBlockResourcePaths =
        {
            "LoadingBlocks/Block_Blue",
            "LoadingBlocks/Block_Cyan",
            "LoadingBlocks/Block_Magenta",
            "LoadingBlocks/Block_Pink",
            "LoadingBlocks/Block_Purple",
            "LoadingBlocks/Block_Yellow",
            "LoadingBlocks/Block_Orange",
            "LoadingBlocks/Block_Indigo",
        };

        private static readonly Vector2[] LoadingBlockAnchors =
        {
            new Vector2(0.08f, 0.10f),
            new Vector2(0.86f, 0.67f),
            new Vector2(0.12f, 0.56f),
            new Vector2(1.02f, 0.37f),
            new Vector2(0.22f, -0.015f),
            new Vector2(0.43f, 0.09f),
            new Vector2(0.70f, 0.015f),
            new Vector2(0.95f, 0.10f),
        };

        private static readonly float[] LoadingBlockSizes =
        {
            320f,
            100f,
            130f,
            250f,
            380f,
            260f,
            400f,
            330f,
        };

        private static readonly float[] LoadingBlockRotations =
        {
            -7f,
            14f,
            -12f,
            8f,
            9f,
            -6f,
            -10f,
            9f,
        };

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private string _title = DefaultTitle;
        [SerializeField] private string _statusMessage = DefaultStatus;

        public float Progress { get; private set; }

        private void Awake()
        {
            if (_titleText != null)
                _titleText.text = _title;
            if (_statusText != null)
                _statusText.text = _statusMessage;
            SetProgress(0f);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = false;
            }
        }

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        public void SetTitle(string title)
        {
            _title = string.IsNullOrEmpty(title) ? DefaultTitle : title;
            if (_titleText != null)
                _titleText.text = _title;
        }

        public void SetStatus(string status)
        {
            _statusMessage = string.IsNullOrEmpty(status) ? DefaultStatus : status;
            if (_statusText != null)
                _statusText.text = _statusMessage;
        }

        public void SetProgress(float progress01)
        {
            Progress = Mathf.Clamp01(progress01);
        }

        /// <summary>
        /// Instantiates the editable loading-screen prefab, with a code-built fallback for recovery.
        /// </summary>
        public static LoadingScreen Create(Transform parent = null)
        {
            LoadingScreen prefab = Resources.Load<LoadingScreen>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[LoadingScreen] Prefab was not found at Resources/{PrefabResourcePath}; " +
                    "using the runtime fallback.");
                return CreateRuntime(parent);
            }

            LoadingScreen instance = parent == null
                ? Instantiate(prefab)
                : Instantiate(prefab, parent, false);
            instance.name = "PF_LoadingScreen";
            return instance;
        }

        /// <summary>
        /// Builds a minimal loading canvas at runtime when no prefab is assigned (bootstrap fallback).
        /// </summary>
        public static LoadingScreen CreateRuntime(Transform parent = null)
        {
            var root = new GameObject("PF_LoadingScreen", typeof(RectTransform));
            if (parent != null)
                root.transform.SetParent(parent, false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            TMP_FontAsset loadingFont = Resources.Load<TMP_FontAsset>(LoadingFontResourcePath);
            if (loadingFont == null)
                Debug.LogWarning($"[LoadingScreen] Font was not found at Resources/{LoadingFontResourcePath}.");

            RawImage backdrop = CreateBackground(root.transform);
            StretchFull(backdrop.rectTransform);

            CreateLoadingBlocks(root.transform);

            RawImage logo = CreateLogo(root.transform);
            if (logo != null)
            {
                logo.rectTransform.anchorMin = new Vector2(0.08f, 0.43f);
                logo.rectTransform.anchorMax = new Vector2(0.92f, 0.87f);
                logo.rectTransform.offsetMin = Vector2.zero;
                logo.rectTransform.offsetMax = Vector2.zero;
            }

            TMP_Text title = CreateLabel(
                root.transform,
                "Title",
                DefaultTitle,
                64f,
                FontStyles.Bold,
                loadingFont);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.55f);
            title.rectTransform.anchorMax = new Vector2(0.9f, 0.7f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            title.gameObject.SetActive(logo == null);

            TMP_Text status = CreateLabel(
                root.transform,
                "Status",
                DefaultStatus,
                80f,
                FontStyles.Normal,
                loadingFont);
            status.rectTransform.anchorMin = new Vector2(0.1f, 0.34f);
            status.rectTransform.anchorMax = new Vector2(0.9f, 0.4f);
            status.rectTransform.offsetMin = Vector2.zero;
            status.rectTransform.offsetMax = Vector2.zero;
            status.outlineColor = new Color32(101, 0, 135, 255);
            status.outlineWidth = 0.24f;

            LoadingScreen screen = root.AddComponent<LoadingScreen>();
            screen._canvasGroup = group;
            screen._titleText = title;
            screen._statusText = status;
            screen._title = DefaultTitle;
            screen._statusMessage = DefaultStatus;
            return screen;
        }

        private static RawImage CreateBackground(Transform parent)
        {
            var go = new GameObject("Backdrop", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RawImage image = go.AddComponent<RawImage>();
            image.raycastTarget = false;

            Texture2D texture = Resources.Load<Texture2D>(BackgroundResourcePath);
            if (texture == null)
            {
                image.color = new Color(0.08f, 0.09f, 0.12f, 1f);
                Debug.LogWarning(
                    $"[LoadingScreen] Background was not found at Resources/{BackgroundResourcePath}.");
                return image;
            }

            image.texture = texture;
            image.color = Color.white;

            AspectRatioFitter fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)texture.width / texture.height;
            return image;
        }

        private static void CreateLoadingBlocks(Transform parent)
        {
            for (int i = 0; i < LoadingBlockResourcePaths.Length; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>(LoadingBlockResourcePaths[i]);
                if (texture == null)
                {
                    Debug.LogWarning(
                        $"[LoadingScreen] Block was not found at Resources/{LoadingBlockResourcePaths[i]}.");
                    continue;
                }

                var go = new GameObject($"LoadingBlock_{i + 1:00}", typeof(RectTransform));
                go.transform.SetParent(parent, false);

                RawImage image = go.AddComponent<RawImage>();
                image.texture = texture;
                image.color = Color.white;
                image.raycastTarget = false;

                RectTransform rect = image.rectTransform;
                rect.anchorMin = LoadingBlockAnchors[i];
                rect.anchorMax = LoadingBlockAnchors[i];
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.one * LoadingBlockSizes[i];
                rect.localRotation = Quaternion.Euler(0f, 0f, LoadingBlockRotations[i]);
            }
        }

        private static RawImage CreateLogo(Transform parent)
        {
            Texture2D texture = Resources.Load<Texture2D>(LogoResourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[LoadingScreen] Logo was not found at Resources/{LogoResourcePath}.");
                return null;
            }

            var go = new GameObject("Logo", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RawImage image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;

            AspectRatioFitter fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)texture.width / texture.height;
            return image;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style,
            TMP_FontAsset font)
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
            if (font != null)
                label.font = font;
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
