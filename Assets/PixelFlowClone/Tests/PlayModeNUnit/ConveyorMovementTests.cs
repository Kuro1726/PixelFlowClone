using System.Collections;
using NUnit.Framework;
using PixelFlowClone.Core;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixelFlowClone.Tests.PlayMode
{
    public class ConveyorMovementTests
    {
        private const float LapTimeoutSeconds = 60f;

        [UnityTest]
        public IEnumerator Collector_CompletesOneLap_WithinTimeout()
        {
            yield return PlayModeTestHelpers.LoadGameplayAndApplyLevel(0);

            Assume.That(QueueManager.HasInstance, Is.True);
            Assume.That(ConveyorPathManager.HasInstance, Is.True);
            Assume.That(GridManager.HasInstance, Is.True);

            // No blocks → capacity stays > 0 so lap goes to queue instead of exiting mid-path.
            GridManager.Instance.ClearGrid();

            CollectorUnit subject = QueueManager.Instance.GetWaitingQueueFront();
            Assume.That(subject, Is.Not.Null, "No waiting front to dispatch.");
            Assume.That(subject.Capacity, Is.GreaterThan(0));

            bool lapCompleted = false;
            void OnLap(CollectorUnit unit)
            {
                if (unit == subject)
                    lapCompleted = true;
            }

            GameEvents.OnCollectorLapComplete += OnLap;
            try
            {
                Assert.That(
                    QueueManager.Instance.TryDispatchFromWaiting(subject),
                    Is.True,
                    "Waiting → conveyor dispatch failed.");

                Assert.That(subject.State, Is.EqualTo(CollectorState.OnConveyor));

                float elapsed = 0f;
                while (!lapCompleted && elapsed < LapTimeoutSeconds)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(lapCompleted, Is.True,
                    $"Collector did not complete a lap within {LapTimeoutSeconds}s " +
                    $"(state={subject.State}, elapsed={elapsed:0.0}s).");
            }
            finally
            {
                GameEvents.OnCollectorLapComplete -= OnLap;
            }
        }
    }
}
