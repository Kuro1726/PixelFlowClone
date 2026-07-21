using System.Reflection;
using NUnit.Framework;
using PixelFlowClone.UI.Screens;
using UnityEngine;

namespace PixelFlowClone.Tests.EditMode
{
    public class GameplayInstructionTapPulseTests
    {
        private GameObject _instructionObject;

        [TearDown]
        public void TearDown()
        {
            if (_instructionObject != null)
                Object.DestroyImmediate(_instructionObject);
        }

        [Test]
        public void Evaluate_AtHalfCycle_CompressesFingerAndStartsPulse()
        {
            float duration = TapPulseAnimation.DefaultDurationSeconds;
            TapPulseFrame frame = TapPulseAnimation.Evaluate(duration * 0.5f, duration);

            Assert.That(duration, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(frame.Press, Is.EqualTo(1f).Within(0.001f));
            Assert.That(frame.CircleScale, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(frame.CircleAlpha, Is.EqualTo(0.62f).Within(0.001f));
        }

        [Test]
        public void Evaluate_AfterFullDuration_RepeatsFirstFrame()
        {
            float duration = TapPulseAnimation.DefaultDurationSeconds;
            TapPulseFrame first = TapPulseAnimation.Evaluate(0f, duration);
            TapPulseFrame repeated = TapPulseAnimation.Evaluate(duration, duration);

            Assert.That(repeated.Press, Is.EqualTo(first.Press).Within(0.001f));
            Assert.That(repeated.CircleScale, Is.EqualTo(first.CircleScale).Within(0.001f));
            Assert.That(repeated.CircleAlpha, Is.EqualTo(first.CircleAlpha).Within(0.001f));
        }

        [Test]
        public void Evaluate_AtQuarterCycle_SynchronizesCircleWithFinger()
        {
            float duration = TapPulseAnimation.DefaultDurationSeconds;
            TapPulseFrame frame = TapPulseAnimation.Evaluate(duration * 0.25f, duration);

            Assert.That(frame.Press, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(frame.CircleScale, Is.EqualTo(1.035f).Within(0.001f));
            Assert.That(frame.CircleAlpha, Is.EqualTo(0.31f).Within(0.001f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Evaluate_WithNonPositiveDuration_ReturnsFiniteFrame(float duration)
        {
            TapPulseFrame frame = TapPulseAnimation.Evaluate(0.05f, duration);

            Assert.That(float.IsNaN(frame.Press), Is.False);
            Assert.That(float.IsInfinity(frame.Press), Is.False);
            Assert.That(frame.Press, Is.InRange(0f, 1f));
            Assert.That(frame.CircleScale, Is.GreaterThan(0f));
            Assert.That(frame.CircleAlpha, Is.InRange(0f, 1f));
        }

        [Test]
        public void SetFingerVisible_WhenShown_ShowsFingerAndTapPulse()
        {
            GameplayInstruction instruction = CreateInstruction();
            Transform finger = instruction.transform.Find("TargetAnchor/FingerImage");
            Transform pulse = instruction.transform.Find("TargetAnchor/TapPulse");

            InvokeSetFingerVisible(instruction, true);

            Assert.That(finger, Is.Not.Null);
            Assert.That(pulse, Is.Not.Null);
            Assert.That(finger.gameObject.activeSelf, Is.True);
            Assert.That(pulse.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void SetFingerVisible_WhenHidden_HidesFingerAndTapPulse()
        {
            GameplayInstruction instruction = CreateInstruction();
            Transform finger = instruction.transform.Find("TargetAnchor/FingerImage");
            Transform pulse = instruction.transform.Find("TargetAnchor/TapPulse");
            Assert.That(finger, Is.Not.Null);
            Assert.That(pulse, Is.Not.Null);
            InvokeSetFingerVisible(instruction, false);

            Assert.That(finger.gameObject.activeSelf, Is.False);
            Assert.That(pulse.gameObject.activeSelf, Is.False);
        }

        private GameplayInstruction CreateInstruction()
        {
            _instructionObject = new GameObject(
                "GameplayInstruction",
                typeof(RectTransform),
                typeof(CanvasGroup));

            var targetObject = new GameObject("TargetAnchor", typeof(RectTransform));
            targetObject.transform.SetParent(_instructionObject.transform, false);

            var fingerObject = new GameObject(
                "FingerImage",
                typeof(RectTransform));
            fingerObject.transform.SetParent(targetObject.transform, false);

            GameplayInstruction instruction =
                _instructionObject.AddComponent<GameplayInstruction>();
            MethodInfo resolveUi = typeof(GameplayInstruction).GetMethod(
                "ResolveUi",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resolveUi, Is.Not.Null);
            resolveUi.Invoke(instruction, null);
            return instruction;
        }

        private static void InvokeSetFingerVisible(
            GameplayInstruction instruction,
            bool isVisible)
        {
            MethodInfo setFingerVisible = typeof(GameplayInstruction).GetMethod(
                "SetFingerVisible",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(setFingerVisible, Is.Not.Null);
            setFingerVisible.Invoke(instruction, new object[] { isVisible });
        }
    }
}
