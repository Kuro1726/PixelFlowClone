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
    public class VictoryPopup : MonoBehaviour
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

        private static Sprite GetCoinSprite()
        {
            if (_coinSprite != null)
                return _coinSprite;

            const int size = 64;
            Texture2D texture = CreateEffectTexture("VictoryCoinTexture", size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(
                        (x + 0.5f) / size * 2f - 1f,
                        (y + 0.5f) / size * 2f - 1f);
                    float radius = p.magnitude;
                    float alpha = 1f - Mathf.SmoothStep(0.9f, 1f, radius);
                    Color color;
                    if (radius > 0.82f)
                        color = new Color(0.9f, 0.42f, 0.02f, alpha);
                    else if (radius > 0.68f)
                        color = new Color(1f, 0.68f, 0.05f, alpha);
                    else
                        color = new Color(1f, 0.84f, 0.18f, alpha);

                    if (Vector2.Distance(p, new Vector2(-0.28f, 0.32f)) < 0.18f)
                        color = Color.Lerp(color, Color.white, 0.8f);
                    if (radius >= 1f)
                        color.a = 0f;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _coinSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f);
            _coinSprite.name = "VictoryCoinSprite";
            _coinSprite.hideFlags = HideFlags.HideAndDontSave;
            return _coinSprite;
        }

        private static Sprite GetSparkleSprite()
        {
            if (_sparkleSprite != null)
                return _sparkleSprite;

            const int size = 64;
            Texture2D texture = CreateEffectTexture("VictorySparkleTexture", size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                    float py = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                    float vertical = 1f - (px * 5.5f + py);
                    float horizontal = 1f - (px + py * 5.5f);
                    float alpha = Mathf.Clamp01(Mathf.Max(vertical, horizontal) * 2.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _sparkleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f);
            _sparkleSprite.name = "VictorySparkleSprite";
            _sparkleSprite.hideFlags = HideFlags.HideAndDontSave;
            return _sparkleSprite;
        }

        private static Texture2D CreateEffectTexture(string textureName, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            return texture;
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

        /// <summary>
        /// Builds a victory overlay under <paramref name="parent"/> (typically the gameplay HUD canvas).
        /// </summary>
        public static VictoryPopup CreateRuntime(
            Transform parent,
            Sprite panelSprite = null,
            Sprite titleBannerSprite = null,
            Sprite trophySprite = null,
            Sprite trophyWingSprite = null,
            Sprite continueButtonSprite = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var root = new GameObject("VictoryPopup", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.SetActive(false);

            VictoryPopup popup = root.AddComponent<VictoryPopup>();
            popup.ConfigureVisuals(
                panelSprite,
                titleBannerSprite,
                trophySprite,
                trophyWingSprite,
                continueButtonSprite);

            root.SetActive(true);
            return popup;
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

            Image panel = CreateImage(
                transform,
                "Panel",
                _panelSprite != null ? Color.white : new Color(0.12f, 0.16f, 0.14f, 1f));
            panel.sprite = _panelSprite;
            panel.type = _panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panel.rectTransform.anchorMin = new Vector2(0.1f, 0.22f);
            panel.rectTransform.anchorMax = new Vector2(0.9f, 0.76f);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            panel.raycastTarget = true;

            Image trophyWing = CreateImage(
                transform,
                "TrophyWing",
                _trophyWingSprite != null ? Color.white : Color.clear);
            trophyWing.sprite = _trophyWingSprite;
            trophyWing.preserveAspect = true;
            trophyWing.rectTransform.anchorMin = new Vector2(0.12f, 0.69f);
            trophyWing.rectTransform.anchorMax = new Vector2(0.88f, 0.95f);
            trophyWing.rectTransform.offsetMin = Vector2.zero;
            trophyWing.rectTransform.offsetMax = Vector2.zero;

            Image trophy = CreateImage(
                transform,
                "Trophy",
                _trophySprite != null ? Color.white : Color.clear);
            trophy.sprite = _trophySprite;
            trophy.preserveAspect = true;
            trophy.rectTransform.anchorMin = new Vector2(0.28f, 0.72f);
            trophy.rectTransform.anchorMax = new Vector2(0.72f, 0.95f);
            trophy.rectTransform.offsetMin = Vector2.zero;
            trophy.rectTransform.offsetMax = Vector2.zero;

            Image titleBanner = CreateImage(
                transform,
                "TitleBanner",
                _titleBannerSprite != null ? Color.white : Color.clear);
            titleBanner.sprite = _titleBannerSprite;
            titleBanner.preserveAspect = true;
            titleBanner.rectTransform.anchorMin = new Vector2(0.04f, 0.64f);
            titleBanner.rectTransform.anchorMax = new Vector2(0.96f, 0.8f);
            titleBanner.rectTransform.offsetMin = Vector2.zero;
            titleBanner.rectTransform.offsetMax = Vector2.zero;

            _titleLabel = CreateLabel(titleBanner.transform, "Title", "LEVEL 1\nCOMPLETED!", 48f, FontStyles.Bold);
            _titleLabel.rectTransform.anchorMin = new Vector2(0.15f, 0.12f);
            _titleLabel.rectTransform.anchorMax = new Vector2(0.85f, 0.88f);
            _titleLabel.rectTransform.offsetMin = Vector2.zero;
            _titleLabel.rectTransform.offsetMax = Vector2.zero;
            _titleLabel.color = Color.white;
            _titleLabel.enableAutoSizing = true;
            _titleLabel.fontSizeMin = 30f;
            _titleLabel.fontSizeMax = 48f;

            _subtitleLabel = CreateLabel(panel.transform, "Subtitle", "Great job!", 44f, FontStyles.Bold);
            _subtitleLabel.rectTransform.anchorMin = new Vector2(0.15f, 0.56f);
            _subtitleLabel.rectTransform.anchorMax = new Vector2(0.85f, 0.7f);
            _subtitleLabel.rectTransform.offsetMin = Vector2.zero;
            _subtitleLabel.rectTransform.offsetMax = Vector2.zero;

            _nextLevelButton = CreateButton(
                panel.transform, "NextLevelButton", "NEXT LEVEL",
                new Vector2(0.15f, 0.29f), new Vector2(0.85f, 0.49f),
                Color.white,
                Color.white,
                new Color(0.78f, 0.9f, 0.78f, 1f),
                _continueButtonSprite);

            _mainMenuButton = CreateButton(
                panel.transform, "MainMenuButton", "MAIN MENU",
                new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.26f),
                Color.white,
                Color.white,
                new Color(0.78f, 0.9f, 0.78f, 1f),
                _continueButtonSprite);

            _confetti = CreateConfetti(transform);
        }

        private static ParticleSystem CreateConfetti(Transform parent)
        {
            // World-space burst so it renders correctly above Screen Space Overlay UI.
            var go = new GameObject("VictoryConfetti");
            go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.2f;
            main.useUnscaledTime = true;
            main.startLifetime = 1.8f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.2f, 1f),
                new Color(0.3f, 0.9f, 1f, 1f));
            main.gravityModifier = 0.6f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 48) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.8f), 0.5f),
                    new GradientColorKey(new Color(0.4f, 0.8f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 250;

            return ps;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color normalColor,
            Color highlightedColor,
            Color pressedColor,
            Sprite buttonSprite)
        {
            Image image = CreateImage(parent, name, normalColor);
            image.sprite = buttonSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = buttonSprite != null;
            image.rectTransform.anchorMin = anchorMin;
            image.rectTransform.anchorMax = anchorMax;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            button.colors = colors;

            if (buttonSprite == null)
            {
                Outline outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.01f, 0.08f, 0.16f, 0.95f);
                outline.effectDistance = new Vector2(4f, -4f);
            }

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


        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
