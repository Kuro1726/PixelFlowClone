using System;
using PixelFlowClone.Managers;
using PixelFlowClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Defeat overlay: "Jammed!", "Out of moves", "Retry" (P3-13).
    /// Visibility is driven by <see cref="UIManager"/> (P3-15).
    /// </summary>
    public partial class DefeatPopup : MonoBehaviour
    {
        private const string PanelResourcePath = "UI/Popups/Defeat/DefeatPanel_Cutout";
        private const string RestartButtonResourcePath =
            "UI/Popups/Pause/RestartLevelButton_Cutout";
        private const string CloseButtonResourcePath =
            "UI/Popups/Pause/CloseButton_Cutout";

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _subtitleLabel;

        public event Action RetryClicked;

        public bool HasMainMenuButton => _mainMenuButton != null;

        public bool UsesRestartLevelButtonArtwork
        {
            get
            {
                if (_retryButton == null || _retryButton.name != "RestartLevelButton")
                    return false;

                Image image = _retryButton.GetComponent<Image>();
                return image != null &&
                       image.sprite != null &&
                       image.sprite.name == "RestartLevelButton_Cutout";
            }
        }
        public bool UsesDefeatPanelArtwork
        {
            get
            {
                Transform panel = transform.Find("Panel");
                Image image = panel != null ? panel.GetComponent<Image>() : null;
                return image != null &&
                       image.sprite != null &&
                       image.sprite.name == "DefeatPanel_Cutout";
            }
        }

        public bool UsesMainMenuCloseButtonArtwork
        {
            get
            {
                if (_closeButton == null || _closeButton.name != "CloseButton")
                    return false;

                Image image = _closeButton.GetComponent<Image>();
                return image != null &&
                       image.sprite != null &&
                       image.sprite.name == "CloseButton_Cutout";
            }
        }

        private void Awake()
        {
            ResolveEditableUiReferences();
            // A scene instance may intentionally omit optional controls while editing its
            // existing hierarchy. Only create a full overlay when no panel exists at all.
            if (transform.Find("Panel") == null)
                BuildRuntimeUi();

            ApplyPanelArtwork();
            ApplyRestartButtonArtwork();
            ApplyCloseButtonArtwork();
            EnsureCanvasGroup();
            WireButtons();
            Hide();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void OnDestroy()
        {
            if (UIManager.HasInstance)
                UIManager.Instance.UnregisterPopup(PopupId.Defeat, this);
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

        /// <summary>Builds and serializes the hierarchy for prefab editing.</summary>
        public void BuildEditableUi()
        {
            ResolveEditableUiReferences();
            if (transform.Find("Panel") == null)
                BuildRuntimeUi();

            ApplyPanelArtwork();
            ApplyRestartButtonArtwork();
            ApplyCloseButtonArtwork();
            EnsureCanvasGroup();
        }

        private void ResolveEditableUiReferences()
        {
            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            if (_titleLabel == null)
                _titleLabel = panel.Find("Title")?.GetComponent<TMP_Text>();
            if (_subtitleLabel == null)
                _subtitleLabel = panel.Find("Subtitle")?.GetComponent<TMP_Text>();

            if (_retryButton == null)
            {
                Transform retry = panel.Find("RestartLevelButton") ?? panel.Find("RetryButton");
                _retryButton = retry != null ? retry.GetComponent<Button>() : null;
            }

            if (_closeButton == null)
                _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();
            if (_mainMenuButton == null)
            {
                Transform home = panel.Find("MainMenuButton") ?? panel.Find("HomeButton");
                _mainMenuButton = home != null ? home.GetComponent<Button>() : null;
            }
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void WireButtons()
        {
            if (_retryButton == null)
                return;

            _retryButton.onClick.RemoveListener(HandleRetryClicked);
            _retryButton.onClick.AddListener(HandleRetryClicked);

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
                _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            }
        }

        private void UnwireButtons()
        {
            if (_retryButton == null)
            {
                if (_closeButton != null)
                    _closeButton.onClick.RemoveListener(HandleCloseClicked);
                if (_mainMenuButton != null)
                    _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
                return;
            }

            _retryButton.onClick.RemoveListener(HandleRetryClicked);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        private void HandleRetryClicked()
        {
            RetryClicked?.Invoke();

            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Defeat);
            else
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

        private void HandleCloseClicked()
        {
            HandleMainMenuClicked();
        }

        private void HandleMainMenuClicked()
        {
            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Defeat);
            else
                Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[DefeatPopup] Close clicked but LevelManager is missing.");
                return;
            }

            LevelManager.Instance.ReturnToMainMenu();
        }

        /// <summary>
        /// Builds a defeat overlay under <paramref name="parent"/> (typically the gameplay HUD canvas).
        /// </summary>
        public static DefeatPopup Create(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            return CreateRuntime(parent);
        }

    }
}
