using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Screens
{
    /// <summary>
    /// Full-screen loading UI: game title, progress bar (0–1), and status text ("Loading...").
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public const string DefaultTitle = "Pixel Flow";
        public const string DefaultStatus = "Loading...";

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Image _progressFill;
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
            if (_progressFill != null)
                _progressFill.fillAmount = Progress;
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

            Image backdrop = CreateImage(root.transform, "Backdrop", new Color(0.08f, 0.09f, 0.12f, 1f));
            StretchFull(backdrop.rectTransform);

            TMP_Text title = CreateLabel(root.transform, "Title", DefaultTitle, 64f, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.55f);
            title.rectTransform.anchorMax = new Vector2(0.9f, 0.7f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            Image track = CreateImage(root.transform, "ProgressTrack", new Color(0.2f, 0.22f, 0.28f, 1f));
            track.rectTransform.anchorMin = new Vector2(0.15f, 0.42f);
            track.rectTransform.anchorMax = new Vector2(0.85f, 0.46f);
            track.rectTransform.offsetMin = Vector2.zero;
            track.rectTransform.offsetMax = Vector2.zero;

            Image fill = CreateImage(track.transform, "ProgressFill", new Color(0.35f, 0.75f, 0.95f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            StretchFull(fill.rectTransform);

            TMP_Text status = CreateLabel(root.transform, "Status", DefaultStatus, 36f, FontStyles.Normal);
            status.rectTransform.anchorMin = new Vector2(0.1f, 0.34f);
            status.rectTransform.anchorMax = new Vector2(0.9f, 0.4f);
            status.rectTransform.offsetMin = Vector2.zero;
            status.rectTransform.offsetMax = Vector2.zero;

            LoadingScreen screen = root.AddComponent<LoadingScreen>();
            screen._canvasGroup = group;
            screen._titleText = title;
            screen._statusText = status;
            screen._progressFill = fill;
            screen._title = DefaultTitle;
            screen._statusMessage = DefaultStatus;
            return screen;
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
