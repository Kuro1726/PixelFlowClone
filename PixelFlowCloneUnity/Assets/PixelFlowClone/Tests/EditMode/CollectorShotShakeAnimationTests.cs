using System.Reflection;
using NUnit.Framework;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Tests.EditMode
{
    public class CollectorShotShakeAnimationTests
    {
        private GameObject _collectorObject;

        [TearDown]
        public void TearDown()
        {
            if (_collectorObject != null)
                Object.DestroyImmediate(_collectorObject);
        }

        [Test]
        public void Defaults_ForLightCollectorRecoil_MatchApprovedFeelBrief()
        {
            Assert.That(
                CollectorShotShakeAnimation.DefaultDurationSeconds,
                Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(
                CollectorShotShakeAnimation.DefaultAmplitude,
                Is.EqualTo(0.045f).Within(0.001f));
            Assert.That(
                CollectorShotShakeAnimation.DefaultOscillations,
                Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Evaluate_AtFirstAttackPeak_ProducesBackwardRecoilAndLateralShake()
        {
            float duration = CollectorShotShakeAnimation.DefaultDurationSeconds;
            CollectorShotShakeFrame frame = CollectorShotShakeAnimation.Evaluate(
                duration / 12f,
                duration,
                CollectorShotShakeAnimation.DefaultAmplitude,
                CollectorShotShakeAnimation.DefaultOscillations);

            Assert.That(frame.IsComplete, Is.False);
            Assert.That(frame.LocalOffset.y, Is.LessThan(0f));
            Assert.That(frame.LocalOffset.x, Is.GreaterThan(0f));
            Assert.That(
                frame.LocalOffset.magnitude,
                Is.LessThanOrEqualTo(CollectorShotShakeAnimation.DefaultAmplitude));
        }

        [Test]
        public void Evaluate_AtLaterMatchingPeak_ReturnsDampedOffset()
        {
            float duration = CollectorShotShakeAnimation.DefaultDurationSeconds;
            CollectorShotShakeFrame earlyFrame = CollectorShotShakeAnimation.Evaluate(
                duration / 12f,
                duration,
                CollectorShotShakeAnimation.DefaultAmplitude,
                CollectorShotShakeAnimation.DefaultOscillations);
            CollectorShotShakeFrame lateFrame = CollectorShotShakeAnimation.Evaluate(
                duration * 5f / 12f,
                duration,
                CollectorShotShakeAnimation.DefaultAmplitude,
                CollectorShotShakeAnimation.DefaultOscillations);

            Assert.That(
                lateFrame.LocalOffset.magnitude,
                Is.LessThan(earlyFrame.LocalOffset.magnitude));
        }

        [Test]
        public void Evaluate_AfterDuration_RestoresAuthoredPosition()
        {
            CollectorShotShakeFrame frame = CollectorShotShakeAnimation.Evaluate(
                CollectorShotShakeAnimation.DefaultDurationSeconds,
                CollectorShotShakeAnimation.DefaultDurationSeconds,
                CollectorShotShakeAnimation.DefaultAmplitude,
                CollectorShotShakeAnimation.DefaultOscillations);

            Assert.That(frame.IsComplete, Is.True);
            Assert.That(frame.LocalOffset, Is.EqualTo(Vector2.zero));
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(-1f, -1f, -1f)]
        public void Evaluate_WithNonPositiveTunables_ReturnsFiniteFrame(
            float duration,
            float amplitude,
            float oscillations)
        {
            CollectorShotShakeFrame frame = CollectorShotShakeAnimation.Evaluate(
                0.01f,
                duration,
                amplitude,
                oscillations);

            Assert.That(float.IsNaN(frame.LocalOffset.x), Is.False);
            Assert.That(float.IsNaN(frame.LocalOffset.y), Is.False);
            Assert.That(float.IsInfinity(frame.LocalOffset.x), Is.False);
            Assert.That(float.IsInfinity(frame.LocalOffset.y), Is.False);
        }

        [Test]
        public void Awake_WithRootSpriteRenderer_CreatesDedicatedVisualPivot()
        {
            CollectorUnit collector = CreateCollector();
            SpriteRenderer rootRenderer = collector.GetComponent<SpriteRenderer>();
            Transform visual = collector.transform.Find("ShotShakeVisual");

            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.GetComponent<SpriteRenderer>(), Is.Not.Null);
            Assert.That(rootRenderer.enabled, Is.False);
            Assert.That(collector.GetComponent<Rigidbody2D>(), Is.Not.Null);
        }

        [Test]
        public void PlayShotShake_WhenRepeated_RestartsWithoutStackingOffset()
        {
            CollectorUnit collector = CreateCollector();
            Transform visual = collector.transform.Find("ShotShakeVisual");
            MethodInfo playShotShake = typeof(CollectorUnit).GetMethod(
                "PlayShotShake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo elapsed = typeof(CollectorUnit).GetField(
                "_shotShakeElapsed",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(visual, Is.Not.Null);
            Assert.That(playShotShake, Is.Not.Null);
            Assert.That(elapsed, Is.Not.Null);

            visual.localPosition = new Vector3(0.02f, -0.03f, 0f);
            elapsed.SetValue(collector, 0.08f);
            playShotShake.Invoke(collector, null);

            Assert.That((float)elapsed.GetValue(collector), Is.EqualTo(0f).Within(0.001f));
            Assert.That(visual.localPosition, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void OnDisable_WhenInvokedDuringShotShake_RestoresVisualPivot()
        {
            CollectorUnit collector = CreateCollector();
            Transform visual = collector.transform.Find("ShotShakeVisual");
            MethodInfo onDisable = typeof(CollectorUnit).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(visual, Is.Not.Null);
            Assert.That(onDisable, Is.Not.Null);
            visual.localPosition = new Vector3(0.02f, -0.03f, 0f);

            onDisable.Invoke(collector, null);

            Assert.That(visual.localPosition, Is.EqualTo(Vector3.zero));
        }

        private CollectorUnit CreateCollector()
        {
            _collectorObject = new GameObject("CollectorShotShakeTest");
            CollectorUnit collector = _collectorObject.AddComponent<CollectorUnit>();
            MethodInfo awake = typeof(CollectorUnit).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null);
            awake.Invoke(collector, null);
            return collector;
        }
    }
}
