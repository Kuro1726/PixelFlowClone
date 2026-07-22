using System;
using PixelFlowClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlowClone.UI.Popups
{
    public partial class VictoryPopup
    {
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

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
