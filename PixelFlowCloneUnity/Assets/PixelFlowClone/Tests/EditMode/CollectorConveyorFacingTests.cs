using System.Reflection;
using NUnit.Framework;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Tests.EditMode
{
    public class CollectorConveyorFacingTests
    {
        private GameObject _collectorObject;
        private GameObject _waypointObject;

        [TearDown]
        public void TearDown()
        {
            if (_waypointObject != null)
                Object.DestroyImmediate(_waypointObject);

            if (_collectorObject != null)
                Object.DestroyImmediate(_collectorObject);
        }

        [Test]
        public void FaceInwardForRestOfLap_OnFirstConsume_SnapsToShootDirection()
        {
            CollectorUnit collector = CreateCollector();
            collector.PrepareConveyorFacing();

            collector.FaceInwardForRestOfLap(Vector2.up);

            Assert.That(
                Vector2.Dot(collector.transform.up, Vector2.up),
                Is.GreaterThan(0.999f));
        }

        [Test]
        public void TickMovement_AfterConsumeOnStraightSegment_HoldsPerpendicularFacing()
        {
            CollectorUnit collector = CreateCollector();
            collector.SetWorldPosition(new Vector2(-2f, -2f));
            collector.PrepareConveyorFacing();
            collector.FaceInwardForRestOfLap(Vector2.up);
            collector.ForceState(CollectorState.OnConveyor);

            ConveyorWaypoint waypoint = CreateWaypoint(new Vector2(-1f, -2f));
            int waypointIndex = 0;

            for (int i = 0; i < 5; i++)
            {
                collector.TickMovement(
                    0.02f,
                    new[] { waypoint },
                    ref waypointIndex,
                    1f,
                    0.001f);
            }

            Assert.That(
                Vector2.Dot(collector.transform.up, Vector2.up),
                Is.GreaterThan(0.999f));
        }

        [Test]
        public void TickMovement_AfterConsumeOnCornerWaypointSegment_BlendsTowardLeftNormal()
        {
            CollectorUnit collector = CreateCollector();
            collector.SetWorldPosition(Vector2.zero);
            collector.PrepareConveyorFacing();
            collector.FaceInwardForRestOfLap(Vector2.up);
            collector.ForceState(CollectorState.OnConveyor);

            ConveyorWaypoint waypoint = CreateWaypoint(Vector2.one);
            int waypointIndex = 0;

            collector.TickMovement(
                0.02f,
                new[] { waypoint },
                ref waypointIndex,
                1f,
                0.001f);

            float turnFromUp = Vector2.SignedAngle(Vector2.up, collector.transform.up);
            Assert.That(turnFromUp, Is.GreaterThan(0f));
            Assert.That(turnFromUp, Is.LessThan(45f));
        }

        [Test]
        public void FaceInwardForRestOfLap_AfterFirstConsume_DoesNotInterruptWaypointTurn()
        {
            CollectorUnit collector = CreateCollector();
            collector.SetWorldPosition(Vector2.zero);
            collector.PrepareConveyorFacing();
            collector.FaceInwardForRestOfLap(Vector2.up);
            collector.ForceState(CollectorState.OnConveyor);

            ConveyorWaypoint waypoint = CreateWaypoint(Vector2.one);
            int waypointIndex = 0;
            collector.TickMovement(
                0.02f,
                new[] { waypoint },
                ref waypointIndex,
                1f,
                0.001f);
            Quaternion rotationBeforeRepeatedConsume = collector.transform.rotation;

            collector.FaceInwardForRestOfLap(Vector2.left);

            Assert.That(
                Quaternion.Angle(collector.transform.rotation, rotationBeforeRepeatedConsume),
                Is.EqualTo(0f).Within(0.001f));
        }

        private CollectorUnit CreateCollector()
        {
            _collectorObject = new GameObject("CollectorConveyorFacingTest");
            CollectorUnit collector = _collectorObject.AddComponent<CollectorUnit>();
            InvokeAwake(collector);
            return collector;
        }

        private ConveyorWaypoint CreateWaypoint(Vector2 position)
        {
            _waypointObject = new GameObject("ConveyorFacingWaypoint");
            _waypointObject.transform.position = position;
            return _waypointObject.AddComponent<ConveyorWaypoint>();
        }

        private static void InvokeAwake(CollectorUnit collector)
        {
            MethodInfo awake = typeof(CollectorUnit).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null);
            awake.Invoke(collector, null);
        }
    }
}
