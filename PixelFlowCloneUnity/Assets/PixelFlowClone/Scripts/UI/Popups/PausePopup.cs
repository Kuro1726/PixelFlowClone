using System;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Pause overlay: Resume, Restart, Home (P3-14).
    /// Visibility is driven by <see cref="UIManager"/> (P3-15).
    /// </summary>
    public partial class PausePopup : MonoBehaviour
    {
        private const string PausePanelResourcePath = "UI/Popups/Pause/PausePanel_Cutout";
        private const string RestartButtonResourcePath =
            "UI/Popups/Pause/RestartLevelButton_Cutout";
        private const string HomeButtonResourcePath =
            "UI/Popups/Pause/ReturnHomeButton_Cutout";
        private const string CloseButtonResourcePath =
            "UI/Popups/Pause/CloseButton_Cutout";
        private const string SoundIconResourcePath =
            "UI/Settings/SoundIcon_Cutout";
        private const string HapticIconResourcePath =
            "UI/Settings/HapticIcon_Cutout";
        private const string ToggleOffResourcePath =
            "UI/Settings/ToggleOff_Cutout";
        private const string ToggleOnResourcePath =
            "UI/Settings/ToggleOn_Cutout";
        private const string PrefabResourcePath = "UI/PF_PausePopup";

        [SerializeField] private CanvasGroup _canvasGroup;
        [FormerlySerializedAs("_resumeButton")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private Button _audioToggleButton;
        [SerializeField] private TMP_Text _audioToggleLabel;
        [SerializeField] private Image _audioIconImage;
        [SerializeField] private Image _audioToggleStateImage;
        [SerializeField] private Button _hapticToggleButton;
        [SerializeField] private TMP_Text _hapticToggleLabel;
        [SerializeField] private Image _hapticIconImage;
        [SerializeField] private Image _hapticToggleStateImage;

        private Sprite _toggleOffSprite;
        private Sprite _toggleOnSprite;

        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action HomeClicked;

        public bool HasSettingsControls =>
            _audioToggleButton != null && _hapticToggleButton != null;
        public bool HasSettingsArtwork =>
            _audioIconImage != null && _audioIconImage.sprite != null &&
            _audioToggleStateImage != null &&
            _hapticIconImage != null && _hapticIconImage.sprite != null &&
            _hapticToggleStateImage != null &&
            Resources.Load<Sprite>(ToggleOffResourcePath) != null &&
            Resources.Load<Sprite>(ToggleOnResourcePath) != null;
        public bool HasRestartArtwork =>
            _restartButton != null && _restartButton.GetComponent<Image>()?.sprite != null;
        public bool HasHomeArtwork =>
            _homeButton != null && _homeButton.GetComponent<Image>()?.sprite != null;
        public bool HasCloseArtwork =>
            _closeButton != null &&
            _closeButton.name == "CloseButton" &&
            _closeButton.GetComponent<Image>()?.sprite != null;
        public bool UsesDirectHeaderEditing => transform.Find("Panel/HeaderRoot") == null;

        private void Awake()
        {
            if (_closeButton == null || _restartButton == null || _homeButton == null)
                BuildRuntimeUi();

            EnsureSettingsControls();
            EnsureSettingsArtwork();
            ApplyRestartButtonArtwork();
            ApplyHomeButtonArtwork();
            ApplyCloseButtonArtwork();
            EnsureDirectHeaderEditing();
            EnsureCanvasGroup();
            WireButtons();
            RefreshSettingsLabels();
            Hide();
        }

        private void OnEnable()
        {
            GameSettings.SettingsChanged -= RefreshSettingsLabels;
            GameSettings.SettingsChanged += RefreshSettingsLabels;
            WireButtons();
            RefreshSettingsLabels();
        }

        private void OnDisable()
        {
            GameSettings.SettingsChanged -= RefreshSettingsLabels;
            UnwireButtons();
        }

        private void OnDestroy()
        {
            if (UIManager.HasInstance)
                UIManager.Instance.UnregisterPopup(PopupId.Pause, this);
            GameSettings.SettingsChanged -= RefreshSettingsLabels;
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
            if (_closeButton == null || _restartButton == null || _homeButton == null)
                BuildRuntimeUi();

            EnsureSettingsControls();
            EnsureSettingsArtwork();
            ApplyRestartButtonArtwork();
            ApplyHomeButtonArtwork();
            ApplyCloseButtonArtwork();
            EnsureDirectHeaderEditing();
            EnsureCanvasGroup();
            RefreshSettingsLabels();
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
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleResumeClicked);
                _closeButton.onClick.AddListener(HandleResumeClicked);
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
        }

        private void UnwireButtons()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleResumeClicked);
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            if (_homeButton != null)
                _homeButton.onClick.RemoveListener(HandleHomeClicked);
            if (_audioToggleButton != null)
                _audioToggleButton.onClick.RemoveListener(HandleAudioClicked);
            if (_hapticToggleButton != null)
                _hapticToggleButton.onClick.RemoveListener(HandleHapticClicked);
        }

        private void HandleAudioClicked()
        {
            GameSettings.ToggleAudio();
            RefreshSettingsLabels();
        }

        private void HandleHapticClicked()
        {
            GameSettings.ToggleHaptic();
            RefreshSettingsLabels();
            if (GameSettings.HapticEnabled)
                GameSettings.TryHaptic();
        }

        private void RefreshSettingsLabels()
        {
            if (_audioToggleLabel != null)
                _audioToggleLabel.text = GameSettings.AudioEnabled ? "Sound: ON" : "Sound: OFF";
            if (_hapticToggleLabel != null)
                _hapticToggleLabel.text = GameSettings.HapticEnabled ? "Haptic: ON" : "Haptic: OFF";

            ApplyToggleArtwork(_audioToggleStateImage, GameSettings.AudioEnabled);
            ApplyToggleArtwork(_hapticToggleStateImage, GameSettings.HapticEnabled);
        }

        private void ApplyToggleArtwork(Image stateImage, bool enabled)
        {
            if (stateImage == null)
                return;

            Sprite sprite = enabled ? _toggleOnSprite : _toggleOffSprite;
            if (sprite != null)
                stateImage.sprite = sprite;
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

            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Pause);
            else
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

            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Pause);
            else
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
        public static PausePopup Create(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            PausePopup prefab = Resources.Load<PausePopup>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[PausePopup] Prefab was not found at Resources/{PrefabResourcePath}; using runtime UI.");
                return CreateRuntime(parent);
            }

            PausePopup instance = Instantiate(prefab, parent, false);
            instance.name = "PausePopup";
            return instance;
        }

    }
}
