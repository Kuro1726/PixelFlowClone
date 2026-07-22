using System;
using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Managers;
using PixelFlowClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Victory overlay: trophy, completed-level banner, confetti, and navigation actions.
    /// Visibility is driven by <see cref="UIManager"/> (P3-15).
    /// </summary>
    public partial class VictoryPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Sprite _panelSprite;
        [SerializeField] private Sprite _titleBannerSprite;
        [SerializeField] private Sprite _trophySprite;
        [SerializeField] private Sprite _trophyWingSprite;
        [SerializeField] private Sprite _continueButtonSprite;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _subtitleLabel;
        [SerializeField] private ParticleSystem _confetti;
        [SerializeField] private Image _dimImage;
        [SerializeField] private RectTransform _animatedContent;
        [SerializeField] private RectTransform _effectsRoot;
        [SerializeField, HideInInspector] private int _presentationVersion;

        private const int CurrentPresentationVersion = 1;
        private const float DimAlpha = 0.72f;
        private Coroutine _showAnimation;
        private readonly List<CelebrationEffect> _effectPool = new List<CelebrationEffect>(24);
        private readonly List<CelebrationEffect> _activeEffects = new List<CelebrationEffect>(24);
        private Vector2 _contentRestPosition;
        private static Sprite _coinSprite;
        private static Sprite _sparkleSprite;

        public event Action NextLevelClicked;
        public event Action MainMenuClicked;

        private void Awake()
        {
            if (!HasSerializedUi())
                BuildRuntimeUi();

            EnsureCanvasGroup();
            EnsureVictoryPresentation();
            WireButtons();
            Hide();
        }

        private void OnEnable()
        {
            WireButtons();
        }

        private void Update()
        {
            TickCelebrationEffects(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            StopCelebrationAnimation();
            UnwireButtons();
        }

        private void OnDestroy()
        {
            if (UIManager.HasInstance)
                UIManager.Instance.UnregisterPopup(PopupId.Victory, this);
            UnwireButtons();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureCanvasGroup();
            EnsureVictoryPresentation();
            RefreshTitle();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            PlayConfetti();
            StartCelebrationAnimation();
        }

        public void Hide()
        {
            StopCelebrationAnimation();
            StopConfetti();
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

        private bool HasSerializedUi()
        {
            return _nextLevelButton != null &&
                   _mainMenuButton != null &&
                   _titleLabel != null &&
                   _subtitleLabel != null;
        }

        public void ConfigureVisuals(
            Sprite panelSprite,
            Sprite titleBannerSprite,
            Sprite trophySprite,
            Sprite trophyWingSprite,
            Sprite continueButtonSprite)
        {
            _panelSprite = panelSprite;
            _titleBannerSprite = titleBannerSprite;
            _trophySprite = trophySprite;
            _trophyWingSprite = trophyWingSprite;
            _continueButtonSprite = continueButtonSprite;
        }

        public bool NeedsPresentationUpgrade => _presentationVersion < CurrentPresentationVersion;

#if UNITY_EDITOR
        public void BuildForEditor()
        {
            if (!HasSerializedUi())
                BuildRuntimeUi();

            EnsureCanvasGroup();
            EnsureVictoryPresentation();
            RefreshTitle();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            SetDimAlpha(DimAlpha);
            ResetAnimatedContent();
            StopConfetti();
            _presentationVersion = CurrentPresentationVersion;
        }
#endif

        private void WireButtons()
        {
            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
                _nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
                _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            }
        }

        private void UnwireButtons()
        {
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        private void HandleNextLevelClicked()
        {
            NextLevelClicked?.Invoke();

            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Victory);
            else
                Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[VictoryPopup] Next Level clicked but LevelManager is missing.");
                return;
            }

            if (!LevelManager.Instance.LoadNextLevel())
                Debug.LogWarning("[VictoryPopup] LoadNextLevel failed.");
        }

        private void HandleMainMenuClicked()
        {
            MainMenuClicked?.Invoke();

            if (UIManager.HasInstance)
                UIManager.Instance.HidePopup(PopupId.Victory);
            else
                Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[VictoryPopup] Main Menu clicked but LevelManager is missing.");
                return;
            }

            LevelManager.Instance.ReturnToMainMenu();
        }

        private void RefreshTitle()
        {
            if (_titleLabel == null)
                return;

            int levelNumber = LevelManager.HasInstance
                ? LevelManager.Instance.CurrentLevelIndex + 1
                : 1;
            _titleLabel.text = $"LEVEL {levelNumber}\nCOMPLETED!";
        }

        private void EnsureVictoryPresentation()
        {
            ResolveDimImage();
            EnsureAnimationHierarchy();
            ConfigureLabels();

            if (_dimImage != null)
            {
                _dimImage.color = new Color(0f, 0f, 0f, DimAlpha);
                _dimImage.raycastTarget = true;
                _dimImage.transform.SetAsFirstSibling();
            }

            _presentationVersion = CurrentPresentationVersion;
        }

        private void ResolveDimImage()
        {
            if (_dimImage != null)
                return;

            Transform dim = transform.Find("Dim");
            if (dim != null)
                _dimImage = dim.GetComponent<Image>();
        }

        private void EnsureAnimationHierarchy()
        {
            if (_animatedContent == null)
            {
                Transform existing = transform.Find("AnimatedContent");
                if (existing != null)
                    _animatedContent = existing as RectTransform;
            }

            if (_animatedContent == null)
            {
                var content = new GameObject("AnimatedContent", typeof(RectTransform));
                content.transform.SetParent(transform, false);
                _animatedContent = content.GetComponent<RectTransform>();
                StretchFull(_animatedContent);

                var childrenToMove = new List<Transform>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child == _animatedContent ||
                        (_dimImage != null && child == _dimImage.transform) ||
                        (_confetti != null && child == _confetti.transform) ||
                        child.name == "CelebrationEffects")
                    {
                        continue;
                    }

                    childrenToMove.Add(child);
                }

                for (int i = 0; i < childrenToMove.Count; i++)
                    childrenToMove[i].SetParent(_animatedContent, false);
            }

            if (_effectsRoot == null)
            {
                Transform existing = transform.Find("CelebrationEffects");
                if (existing != null)
                    _effectsRoot = existing as RectTransform;
            }

            if (_effectsRoot == null)
            {
                var effects = new GameObject("CelebrationEffects", typeof(RectTransform));
                effects.transform.SetParent(transform, false);
                _effectsRoot = effects.GetComponent<RectTransform>();
                StretchFull(_effectsRoot);
            }

            if (_dimImage != null)
                _animatedContent.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
            _effectsRoot.SetAsLastSibling();
            EnsureCelebrationEffectPool();
            _contentRestPosition = _animatedContent.anchoredPosition;
        }

        private void ConfigureLabels()
        {
            ApplyTextOutline(_titleLabel, 0.2f);
            ApplyTextOutline(_subtitleLabel, 0.14f);

            TMP_Text continueLabel = _nextLevelButton != null
                ? _nextLevelButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (continueLabel != null)
            {
                continueLabel.text = "CONTINUE";
                ApplyTextOutline(continueLabel, 0.18f);
            }

            TMP_Text mainMenuLabel = _mainMenuButton != null
                ? _mainMenuButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            ApplyTextOutline(mainMenuLabel, 0.18f);
        }

        private static void ApplyTextOutline(TMP_Text label, float width)
        {
            if (label == null)
                return;

            label.outlineColor = new Color32(8, 12, 22, 255);
            label.outlineWidth = width;
        }

        private void StartCelebrationAnimation()
        {
            StopCelebrationAnimation();
            if (_animatedContent == null)
                return;

            SetDimAlpha(0f);
            _animatedContent.anchoredPosition = _contentRestPosition;
            _animatedContent.localScale = Vector3.one * 0.68f;
            _showAnimation = StartCoroutine(PlayShowAnimation());
        }

        private void StopCelebrationAnimation()
        {
            if (_showAnimation != null)
            {
                StopCoroutine(_showAnimation);
                _showAnimation = null;
            }

            StopAllCoroutines();
            ClearCelebrationEffects();
            ResetAnimatedContent();
        }

        private void ResetAnimatedContent()
        {
            if (_animatedContent == null)
                return;

            _animatedContent.localScale = Vector3.one;
            _animatedContent.localRotation = Quaternion.identity;
            _animatedContent.anchoredPosition = _contentRestPosition;
        }

        private IEnumerator PlayShowAnimation()
        {
            Canvas.ForceUpdateCanvases();
            ScheduleCelebrationEffects();

            float elapsed = 0f;
            const float popDuration = 0.34f;
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float scale = Mathf.LerpUnclamped(0.68f, 1f, EaseOutBack(t));
                _animatedContent.localScale = Vector3.one * scale;
                SetDimAlpha(Mathf.Lerp(0f, DimAlpha, Mathf.Clamp01(t * 1.8f)));
                yield return null;
            }

            _animatedContent.localScale = Vector3.one;
            SetDimAlpha(DimAlpha);

            elapsed = 0f;
            const float shakeDuration = 0.32f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / shakeDuration);
                float strength = (1f - t) * 8f;
                float x = Mathf.Sin(t * Mathf.PI * 9f) * strength;
                float y = Mathf.Cos(t * Mathf.PI * 7f) * strength * 0.45f;
                _animatedContent.anchoredPosition = _contentRestPosition + new Vector2(x, y);
                yield return null;
            }

            ResetAnimatedContent();
            _showAnimation = null;
        }

        private void ScheduleCelebrationEffects()
        {
            if (_effectsRoot == null)
                return;

            EnsureCelebrationEffectPool();

            Rect rect = _effectsRoot.rect;
            float width = rect.width > 1f ? rect.width : 1080f;
            float height = rect.height > 1f ? rect.height : 1920f;

            for (int i = 0; i < 14; i++)
            {
                SpawnFlyingCoin(width, height, i * 0.025f);
                if (i < 10)
                    SpawnSparkle(width, height, i * 0.045f);
            }
        }

        private void SpawnFlyingCoin(float width, float height, float delay)
        {
            CelebrationEffect effect = RentCelebrationEffect(GetCoinSprite(), "FlyingCoin");
            if (effect == null)
                return;

            float size = UnityEngine.Random.Range(42f, 70f);
            effect.Rect.sizeDelta = new Vector2(size, size);
            effect.Start = new Vector2(
                UnityEngine.Random.Range(-0.12f, 0.12f) * width,
                UnityEngine.Random.Range(0.02f, 0.12f) * height);
            effect.End = new Vector2(
                UnityEngine.Random.Range(-0.44f, 0.44f) * width,
                UnityEngine.Random.Range(0.34f, 0.52f) * height);
            effect.Rect.anchoredPosition = effect.Start;
            effect.Rect.localScale = Vector3.one;
            effect.Rect.localRotation = Quaternion.identity;
            effect.Image.color = Color.white;
            effect.Group.alpha = 0f;
            effect.Delay = delay;
            effect.Elapsed = 0f;
            effect.Duration = UnityEngine.Random.Range(0.75f, 1.05f);
            effect.ArcHeight = UnityEngine.Random.Range(90f, 180f);
            effect.IsCoin = true;
            ActivateCelebrationEffect(effect);
        }

        private void SpawnSparkle(float width, float height, float delay)
        {
            CelebrationEffect effect = RentCelebrationEffect(GetSparkleSprite(), "Sparkle");
            if (effect == null)
                return;

            float size = UnityEngine.Random.Range(30f, 72f);
            effect.Rect.sizeDelta = new Vector2(size, size);
            effect.Rect.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-0.42f, 0.42f) * width,
                UnityEngine.Random.Range(0.12f, 0.44f) * height);
            effect.Rect.localScale = Vector3.zero;
            effect.Rect.localRotation = Quaternion.identity;
            effect.Image.color = UnityEngine.Random.value > 0.45f
                ? new Color(1f, 0.9f, 0.25f, 1f)
                : Color.white;
            effect.Group.alpha = 0f;
            effect.Delay = delay;
            effect.Elapsed = 0f;
            effect.Duration = 0.58f;
            effect.IsCoin = false;
            ActivateCelebrationEffect(effect);
        }
        private void EnsureCelebrationEffectPool()
        {
            if (_effectsRoot == null)
                return;

            const int targetCount = 28;
            while (_effectPool.Count < targetCount)
            {
                Image image = CreateEffectImage("CelebrationEffect", null);
                CanvasGroup group = image.GetComponent<CanvasGroup>();
                if (group == null)
                    group = image.gameObject.AddComponent<CanvasGroup>();

                CelebrationEffect effect = new CelebrationEffect(image, group);
                ReleaseCelebrationEffect(effect);
                _effectPool.Add(effect);
            }
        }

        private CelebrationEffect RentCelebrationEffect(Sprite sprite, string objectName)
        {
            EnsureCelebrationEffectPool();

            for (int i = 0; i < _effectPool.Count; i++)
            {
                CelebrationEffect effect = _effectPool[i];
                if (effect.IsActive)
                    continue;

                effect.Root.name = objectName;
                effect.Image.sprite = sprite;
                effect.Image.enabled = sprite != null;
                effect.Root.SetActive(true);
                return effect;
            }

            Image image = CreateEffectImage(objectName, sprite);
            CanvasGroup group = image.GetComponent<CanvasGroup>();
            if (group == null)
                group = image.gameObject.AddComponent<CanvasGroup>();

            CelebrationEffect expanded = new CelebrationEffect(image, group);
            _effectPool.Add(expanded);
            return expanded;
        }

        private void ActivateCelebrationEffect(CelebrationEffect effect)
        {
            if (effect == null || effect.IsActive)
                return;

            effect.IsActive = true;
            _activeEffects.Add(effect);
        }

        private void TickCelebrationEffects(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                CelebrationEffect effect = _activeEffects[i];
                if (effect == null || TickCelebrationEffect(effect, deltaTime))
                {
                    _activeEffects.RemoveAt(i);
                    ReleaseCelebrationEffect(effect);
                }
            }
        }

        private static bool TickCelebrationEffect(CelebrationEffect effect, float deltaTime)
        {
            effect.Elapsed += deltaTime;
            if (effect.Elapsed < effect.Delay)
                return false;

            float duration = Mathf.Max(0.01f, effect.Duration);
            float t = Mathf.Clamp01((effect.Elapsed - effect.Delay) / duration);
            if (effect.IsCoin)
            {
                Vector2 position = Vector2.Lerp(effect.Start, effect.End, EaseOutCubic(t));
                position.y += Mathf.Sin(t * Mathf.PI) * effect.ArcHeight;
                effect.Rect.anchoredPosition = position;
                effect.Rect.localScale = new Vector3(
                    Mathf.Lerp(0.18f, 1f, Mathf.Abs(Mathf.Cos(t * Mathf.PI * 5f))),
                    1f,
                    1f);
                effect.Rect.Rotate(0f, 0f, deltaTime * 150f);
                effect.Group.alpha = 1f - Mathf.Clamp01((t - 0.72f) / 0.28f);
            }
            else
            {
                float pulse = Mathf.Sin(t * Mathf.PI);
                effect.Rect.localScale = Vector3.one * pulse;
                effect.Rect.Rotate(0f, 0f, deltaTime * 110f);
                effect.Group.alpha = pulse;
            }

            return t >= 1f;
        }

        private static void ReleaseCelebrationEffect(CelebrationEffect effect)
        {
            if (effect == null)
                return;

            effect.IsActive = false;
            effect.Root.SetActive(false);
            effect.Group.alpha = 0f;
            effect.Rect.anchoredPosition = Vector2.zero;
            effect.Rect.localScale = Vector3.one;
            effect.Rect.localRotation = Quaternion.identity;
        }


        private Image CreateEffectImage(string objectName, Sprite sprite)
        {
            Image image = CreateImage(_effectsRoot, objectName, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }

        private void ClearCelebrationEffects()
        {
            _activeEffects.Clear();
            for (int i = 0; i < _effectPool.Count; i++)
                ReleaseCelebrationEffect(_effectPool[i]);
        }
        private void SetDimAlpha(float alpha)
        {
            if (_dimImage == null)
                return;

            Color color = _dimImage.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = alpha;
            _dimImage.color = color;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private static float EaseOutCubic(float t)
        {
            float x = 1f - t;
            return 1f - x * x * x;
        }

        private void PlayConfetti()
        {
            if (_confetti == null)
                return;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 origin = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.85f, Mathf.Abs(cam.transform.position.z)));
                _confetti.transform.position = origin;
            }

            _confetti.Clear(true);
            _confetti.Play(true);
        }

        private void StopConfetti()
        {
            if (_confetti == null)
                return;

            _confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private sealed class CelebrationEffect
        {
            public CelebrationEffect(Image image, CanvasGroup group)
            {
                Image = image;
                Group = group;
                Rect = image.rectTransform;
                Root = image.gameObject;
            }

            public GameObject Root { get; }
            public Image Image { get; }
            public CanvasGroup Group { get; }
            public RectTransform Rect { get; }
            public Vector2 Start { get; set; }
            public Vector2 End { get; set; }
            public float Delay { get; set; }
            public float Elapsed { get; set; }
            public float Duration { get; set; }
            public float ArcHeight { get; set; }
            public bool IsCoin { get; set; }
            public bool IsActive { get; set; }
        }


    }
}
