using System;
using PixelFlowClone.Core;
using PixelFlowClone.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    /// <summary>
    /// Victory overlay: "Victory!", confetti burst, "Next Level" (P3-12).
    /// Shown when <see cref="GameEvents.OnVictory"/> fires; GameManager already sets timeScale = 0.
    /// </summary>
    public class VictoryPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private ParticleSystem _confetti;

        public event Action NextLevelClicked;

        private void Awake()
        {
            if (_nextLevelButton == null || _titleLabel == null)
                BuildRuntimeUi();

            EnsureCanvasGroup();
            WireButtons();
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.OnVictory -= HandleVictory;
            GameEvents.OnVictory += HandleVictory;
        }

        private void OnDisable()
        {
            GameEvents.OnVictory -= HandleVictory;
            UnwireButtons();
        }

        private void OnDestroy()
        {
            GameEvents.OnVictory -= HandleVictory;
            UnwireButtons();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureCanvasGroup();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            PlayConfetti();
        }

        public void Hide()
        {
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

        private void HandleVictory()
        {
            Show();
        }

        private void WireButtons()
        {
            if (_nextLevelButton == null)
                return;

            _nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
            _nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
        }

        private void UnwireButtons()
        {
            if (_nextLevelButton == null)
                return;

            _nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
        }

        private void HandleNextLevelClicked()
        {
            NextLevelClicked?.Invoke();
            Hide();

            if (!LevelManager.HasInstance)
            {
                Debug.LogWarning("[VictoryPopup] Next Level clicked but LevelManager is missing.");
                return;
            }

            if (!LevelManager.Instance.LoadNextLevel())
                Debug.LogWarning("[VictoryPopup] LoadNextLevel failed.");
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
        public static VictoryPopup CreateRuntime(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var root = new GameObject("VictoryPopup", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.AddComponent<VictoryPopup>();
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

            Image panel = CreateImage(transform, "Panel", new Color(0.12f, 0.16f, 0.14f, 1f));
            panel.rectTransform.anchorMin = new Vector2(0.12f, 0.32f);
            panel.rectTransform.anchorMax = new Vector2(0.88f, 0.68f);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            panel.raycastTarget = true;

            _titleLabel = CreateLabel(panel.transform, "Title", "Victory!", 56f, FontStyles.Bold);
            _titleLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.55f);
            _titleLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
            _titleLabel.rectTransform.offsetMin = Vector2.zero;
            _titleLabel.rectTransform.offsetMax = Vector2.zero;
            _titleLabel.color = new Color(0.45f, 0.92f, 0.55f, 1f);

            _nextLevelButton = CreateButton(
                panel.transform, "NextLevelButton", "Next Level",
                new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.42f));

            _confetti = CreateConfetti(transform);
        }

        private static ParticleSystem CreateConfetti(Transform parent)
        {
            // World-space burst so it renders correctly above Screen Space Overlay UI.
            var go = new GameObject("VictoryConfetti");
            go.transform.SetParent(parent.root, true);
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
            Vector2 anchorMax)
        {
            Image image = CreateImage(parent, name, new Color(0.22f, 0.55f, 0.35f, 1f));
            image.rectTransform.anchorMin = anchorMin;
            image.rectTransform.anchorMax = anchorMax;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.45f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.25f, 1f);
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
