using System;
using PixelFlowClone.Core;
using PixelFlowClone.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Pause overlay: Resume, Restart, Home (P3-14).
    /// Shown while <see cref="GameState.Paused"/>; Resume restores timeScale via GameManager.
    /// </summary>
    public class PausePopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private TMP_Text _titleLabel;

        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action HomeClicked;

        private void Awake()
        {
            if (_resumeButton == null || _restartButton == null || _homeButton == null)
                BuildRuntimeUi();

            EnsureCanvasGroup();
            WireButtons();
            Hide();
        }

        private void OnEnable()
        {
            SubscribeGameManager();
            SyncVisibilityToState();
        }

        private void OnDisable()
        {
            UnsubscribeGameManager();
            UnwireButtons();
        }

        private void OnDestroy()
        {
            UnsubscribeGameManager();
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

        private void SubscribeGameManager()
        {
            if (!GameManager.HasInstance)
                return;

            GameManager.Instance.StateChanged -= HandleGameStateChanged;
            GameManager.Instance.StateChanged += HandleGameStateChanged;
        }

        private void UnsubscribeGameManager()
        {
            if (!GameManager.HasInstance)
                return;

            GameManager.Instance.StateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.Paused)
                Show();
            else
                Hide();
        }

        private void SyncVisibilityToState()
        {
            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Paused)
                Show();
            else
                Hide();
        }

        private void WireButtons()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
                _resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
                _restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(HandleHomeClicked);
                _homeButton.onClick.AddListener(HandleHomeClicked);
            }
        }

        private void UnwireButtons()
        {
            if (_resumeButton != null)
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            if (_homeButton != null)
                _homeButton.onClick.RemoveListener(HandleHomeClicked);
        }

        private void HandleResumeClicked()
        {
            ResumeClicked?.Invoke();

            if (!GameManager.HasInstance)
            {
                Debug.LogWarning("[PausePopup] Resume clicked but GameManager is missing.");
                return;
            }

            if (GameManager.Instance.Resume())
                Debug.Log("[PausePopup] Resumed.");
        }

        private void HandleRestartClicked()
        {
            RestartClicked?.Invoke();
            Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[PausePopup] Restart clicked but LevelManager is missing.");
                return;
            }

            LevelManager levels = LevelManager.Instance;
            if (!levels.LoadLevel(levels.CurrentLevelIndex))
                Debug.LogWarning("[PausePopup] Restart LoadLevel failed.");
        }

        private void HandleHomeClicked()
        {
            HomeClicked?.Invoke();
            Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[PausePopup] Home clicked but LevelManager is missing.");
                return;
            }

            LevelManager.Instance.ReturnToMainMenu();
        }

        /// <summary>
        /// Builds a pause overlay under <paramref name="parent"/> (typically the gameplay HUD canvas).
        /// </summary>
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
            panel.rectTransform.anchorMin = new Vector2(0.14f, 0.28f);
            panel.rectTransform.anchorMax = new Vector2(0.86f, 0.72f);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            panel.raycastTarget = true;

            _titleLabel = CreateLabel(panel.transform, "Title", "Paused", 52f, FontStyles.Bold);
            _titleLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.78f);
            _titleLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            _titleLabel.rectTransform.offsetMin = Vector2.zero;
            _titleLabel.rectTransform.offsetMax = Vector2.zero;

            _resumeButton = CreateButton(
                panel.transform, "ResumeButton", "Resume",
                new Vector2(0.12f, 0.52f), new Vector2(0.88f, 0.72f),
                new Color(0.22f, 0.55f, 0.35f, 1f));

            _restartButton = CreateButton(
                panel.transform, "RestartButton", "Restart",
                new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.48f),
                new Color(0.22f, 0.45f, 0.72f, 1f));

            _homeButton = CreateButton(
                panel.transform, "HomeButton", "Home",
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.26f),
                new Color(0.45f, 0.35f, 0.28f, 1f));
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
