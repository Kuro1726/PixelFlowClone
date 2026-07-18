using System;
using PixelFlowClone.Core;
using PixelFlowClone.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Defeat overlay: "Jammed!", "Out of moves", "Retry" (P3-13).
    /// Shown when <see cref="GameEvents.OnDefeat"/> fires; GameManager already sets timeScale = 0.
    /// </summary>
    public class DefeatPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _retryButton;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _subtitleLabel;

        public event Action RetryClicked;

        private void Awake()
        {
            if (_retryButton == null || _titleLabel == null || _subtitleLabel == null)
                BuildRuntimeUi();

            EnsureCanvasGroup();
            WireButtons();
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.OnDefeat -= HandleDefeat;
            GameEvents.OnDefeat += HandleDefeat;
        }

        private void OnDisable()
        {
            GameEvents.OnDefeat -= HandleDefeat;
            UnwireButtons();
        }

        private void OnDestroy()
        {
            GameEvents.OnDefeat -= HandleDefeat;
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

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void HandleDefeat()
        {
            Show();
        }

        private void WireButtons()
        {
            if (_retryButton == null)
                return;

            _retryButton.onClick.RemoveListener(HandleRetryClicked);
            _retryButton.onClick.AddListener(HandleRetryClicked);
        }

        private void UnwireButtons()
        {
            if (_retryButton == null)
                return;

            _retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        private void HandleRetryClicked()
        {
            RetryClicked?.Invoke();
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

        /// <summary>
        /// Builds a defeat overlay under <paramref name="parent"/> (typically the gameplay HUD canvas).
        /// </summary>
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
                panel.transform, "RetryButton", "Retry",
                new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.36f));
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
