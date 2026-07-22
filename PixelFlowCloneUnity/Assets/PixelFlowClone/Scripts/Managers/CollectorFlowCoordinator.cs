using System.Collections.Generic;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Utils;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    public interface ICollectorConveyorFlow
    {
        int ActiveCount { get; }
        int MaxCapacity { get; }
        bool HasCapacity { get; }
        GameConfigSO Config { get; }
        bool DispatchToConveyor(CollectorUnit unit);
        void UnregisterUnit(CollectorUnit unit);
    }

    public interface ICollectorQueueFlow
    {
        int OccupiedSlots { get; }
        int MaxSlots { get; }
        int WaitingCount { get; }
    }

    public readonly struct CollectorPlayfieldSnapshot
    {
        public CollectorPlayfieldSnapshot(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
            IsValid = true;
        }

        public bool IsValid { get; }
        public Vector2 Center { get; }
        public Vector2 Size { get; }
    }

    public class CollectorFlowCoordinator : Singleton<CollectorFlowCoordinator>
    {
        [SerializeField] private QueueManager _queue;
        [SerializeField] private ConveyorPathManager _conveyor;
        [SerializeField] private GridManager _grid;

        private readonly Dictionary<CollectorUnit, CollectorConsumeState> _consumeStates = new();

        public GameConfigSO Config => _conveyor != null ? _conveyor.Config : null;

        public float EffectiveRaycastDistance =>
            _conveyor != null ? _conveyor.EffectiveRaycastDistance : 20f;

        public bool IsEndgameRushActive()
        {
            return _conveyor != null && _conveyor.IsEndgameRushActive();
        }

        private void OnEnable()
        {
            CollectorUnit.Tapped -= HandleCollectorTapped;
            CollectorUnit.Tapped += HandleCollectorTapped;
            CollectorUnit.MovementAdvanced -= HandleCollectorMovementAdvanced;
            CollectorUnit.MovementAdvanced += HandleCollectorMovementAdvanced;
            CollectorUnit.ConsumePreviewRequested -= HandleConsumePreviewRequested;
            CollectorUnit.ConsumePreviewRequested += HandleConsumePreviewRequested;
            CollectorUnit.ConsumeSuppressed -= HandleConsumeSuppressed;
            CollectorUnit.ConsumeSuppressed += HandleConsumeSuppressed;
            CollectorUnit.LapCompleted -= HandleCollectorLapCompleted;
            CollectorUnit.LapCompleted += HandleCollectorLapCompleted;
            CollectorUnit.ExitCompleted -= HandleCollectorExitCompleted;
            CollectorUnit.ExitCompleted += HandleCollectorExitCompleted;
        }

        private void OnDisable()
        {
            CollectorUnit.Tapped -= HandleCollectorTapped;
            CollectorUnit.MovementAdvanced -= HandleCollectorMovementAdvanced;
            CollectorUnit.ConsumePreviewRequested -= HandleConsumePreviewRequested;
            CollectorUnit.ConsumeSuppressed -= HandleConsumeSuppressed;
            CollectorUnit.LapCompleted -= HandleCollectorLapCompleted;
            CollectorUnit.ExitCompleted -= HandleCollectorExitCompleted;
        }

        public void Configure(
            QueueManager queue,
            ConveyorPathManager conveyor,
            GridManager grid)
        {
            _queue = queue;
            _conveyor = conveyor;
            _grid = grid;
        }

        public int CountAliveCollectors()
        {
            int total = _conveyor != null ? _conveyor.ActiveCount : 0;

            if (_queue != null)
            {
                total += _queue.WaitingCount;
                total += _queue.OccupiedSlots;
            }

            return total;
        }

        public bool TryGetPlayfield(out CollectorPlayfieldSnapshot playfield)
        {
            if (_grid == null)
            {
                playfield = default;
                return false;
            }

            playfield = new CollectorPlayfieldSnapshot(
                _grid.GridCenterWorld,
                _grid.PlayfieldSize);
            return true;
        }

        public bool TryDispatchFromWaiting(CollectorUnit unit)
        {
            if (_queue == null || _conveyor == null)
                return false;

            if (!_queue.CanDispatchFromWaiting(unit))
                return false;

            if (!QueueStateLogic.CanDispatchFromWaiting(
                    unit.State,
                    isWaitingFront: true,
                    _conveyor.ActiveCount,
                    _conveyor.MaxCapacity))
            {
                _queue.PlayConveyorFullFeedback(unit);
                return false;
            }

            CollectorUnit popped = _queue.PopWaitingUnit(unit);
            if (popped == null)
                return false;

            if (!_conveyor.DispatchToConveyor(popped))
            {
                _queue.RestoreWaitingUnit(popped);
                _queue.PlayConveyorFullFeedback(popped);
                return false;
            }

            ResetConsumeState(popped);
            GameEvents.RaiseCollectorDispatchedFromWaiting(popped);
            return true;
        }

        public bool TryEnqueueFromLap(CollectorUnit unit)
        {
            if (_queue == null || _conveyor == null)
                return false;

            if (!_queue.CanEnqueueFromLap(unit))
                return false;

            if (!QueueStateLogic.CanEnqueueFromLap(
                    unit.State,
                    unit.Capacity,
                    _queue.OccupiedSlots,
                    _queue.MaxSlots))
            {
                DeclareDefeatForFullQueue();
                return false;
            }

            int slotIndex = _queue.AssignFirstQueueSlot(unit);
            if (slotIndex < 0)
            {
                DeclareDefeatForFullQueue();
                return false;
            }

            _conveyor.UnregisterUnit(unit);
            _consumeStates.Remove(unit);
            _queue.NotifyQueueStateChanged();
            return true;
        }

        public bool TryDispatchFromQueue(int slotIndex)
        {
            if (_queue == null)
                return false;

            return _queue.TryGetQueueUnit(slotIndex, out CollectorUnit unit) &&
                   TryDispatchFromQueue(unit);
        }

        public bool TryDispatchFromQueue(CollectorUnit unit)
        {
            if (_queue == null || _conveyor == null)
                return false;

            if (!_queue.CanDispatchFromQueue(unit))
                return false;

            if (!QueueStateLogic.CanDispatchFromQueue(
                    unit.State,
                    isInQueueSlot: true,
                    _conveyor.ActiveCount,
                    _conveyor.MaxCapacity))
            {
                _queue.PlayConveyorFullFeedback(unit);
                return false;
            }

            int slotIndex = _queue.IndexOfQueueUnit(unit);
            CollectorUnit removed = _queue.RemoveQueueUnit(unit);
            if (removed == null)
                return false;

            if (!_conveyor.DispatchToConveyor(removed))
            {
                _queue.RestoreQueueUnit(slotIndex, removed);
                _queue.NotifyQueueStateChanged();
                _queue.PlayConveyorFullFeedback(removed);
                return false;
            }

            ResetConsumeState(removed);
            _queue.NotifyQueueStateChanged();
            GameEvents.RaiseCollectorDispatchedFromQueue(removed);
            return true;
        }

        private void HandleCollectorTapped(CollectorUnit unit)
        {
            if (unit == null)
                return;

            switch (unit.State)
            {
                case CollectorState.InWaitingStack:
                {
                    bool ok = _queue != null
                        ? TryDispatchFromWaiting(unit)
                        : TryDispatchDirectlyFromTap(unit);
                    Debug.Log($"[CollectorFlowCoordinator] Waiting dispatch -> {(ok ? "OK" : "REJECTED")}");
                    break;
                }
                case CollectorState.InQueueSlot:
                {
                    bool ok = _queue != null
                        ? TryDispatchFromQueue(unit)
                        : TryDispatchDirectlyFromTap(unit);
                    Debug.Log($"[CollectorFlowCoordinator] Queue dispatch -> {(ok ? "OK" : "REJECTED")}");
                    break;
                }
                case CollectorState.OnConveyor:
                case CollectorState.Exiting:
                case CollectorState.Pooled:
                default:
                    Debug.Log($"[CollectorFlowCoordinator] Tap ignored in state {unit.State}");
                    break;
            }
        }

        private bool TryDispatchDirectlyFromTap(CollectorUnit unit)
        {
            if (_conveyor == null)
                return false;

            bool ok = _conveyor.DispatchToConveyor(unit);
            if (ok)
            {
                ResetConsumeState(unit);
                return true;
            }

            unit.PlayRejectShake();
            return false;
        }

        private void HandleCollectorMovementAdvanced(CollectorUnit unit, Vector2 from, Vector2 to)
        {
            TryConsumeForLanesCrossed(unit, from, to);
        }

        private void HandleConsumePreviewRequested(CollectorUnit unit)
        {
            if (unit == null || unit.State != CollectorState.OnConveyor || unit.IsOnRoundedCorner)
                return;

            GameConfigSO config = Config;
            if (config == null)
                return;

            Vector2 origin = unit.Body != null ? unit.Body.position : (Vector2)unit.transform.position;
            Vector2 gridCenter = _grid != null ? _grid.GridCenterWorld : Vector2.zero;
            PerpendicularRaycastSensor.DrawDebugRayPreview(
                origin,
                unit.CurrentMoveDirection,
                config,
                gridCenter);
        }

        private void HandleConsumeSuppressed(CollectorUnit unit, float seconds)
        {
            if (unit == null)
                return;

            CollectorConsumeState state = GetConsumeState(unit);
            state.NextConsumeTime = Time.time + Mathf.Max(0f, seconds);
        }

        private void HandleCollectorLapCompleted(CollectorUnit unit)
        {
            if (unit == null)
                return;

            ResetConsumeState(unit);

            if (unit.Capacity <= 0)
            {
                RequestBeginExit(unit);
                return;
            }

            GameConfigSO config = Config;
            if (_conveyor != null &&
                _conveyor.IsEndgameRushActive() &&
                config != null &&
                config.EndgameSkipQueueOnLap)
            {
                Debug.Log(
                    $"[CollectorFlowCoordinator] Endgame rush - stay on conveyor " +
                    $"(color={unit.Color}, capacity={unit.Capacity}, alive={CountAliveCollectors()}).");
                return;
            }

            TryEnqueueFromLap(unit);
        }

        private void HandleCollectorExitCompleted(CollectorUnit unit)
        {
            if (unit == null)
                return;

            _consumeStates.Remove(unit);
            GameEvents.RaiseCollectorExited(unit);

            if (PoolManager.HasInstance)
                PoolManager.Instance.ReleaseCollector(unit);
            else
                unit.gameObject.SetActive(false);
        }

        private void TryConsumeForLanesCrossed(CollectorUnit unit, Vector2 from, Vector2 to)
        {
            if (unit == null || unit.State != CollectorState.OnConveyor || unit.Capacity <= 0)
                return;

            CollectorConsumeState state = GetConsumeState(unit);
            Vector2 moveDirection = unit.CurrentMoveDirection;
            if (Vector2.Dot(moveDirection, state.MoveDirection) < 0.3f)
                state.HasLane = false;
            state.MoveDirection = moveDirection;

            if (unit.IsOnRoundedCorner)
                return;

            if (Time.time < state.NextConsumeTime || _grid == null)
                return;

            bool movingVertically = Mathf.Abs(moveDirection.y) >= Mathf.Abs(moveDirection.x);
            int laneFrom = _grid.GetLaneIndex(from, movingVertically);
            int laneTo = _grid.GetLaneIndex(to, movingVertically);

            if (laneFrom == laneTo)
            {
                if (!state.HasLane || state.LastLane != laneTo)
                    FireLaneConsume(unit, to, laneTo, state);
                return;
            }

            int step = laneTo > laneFrom ? 1 : -1;
            for (int lane = laneFrom + step; ; lane += step)
            {
                Vector2 origin = _grid.GetLaneRayOrigin(to, lane, movingVertically);
                if (movingVertically)
                    origin.x = to.x;
                else
                    origin.y = to.y;

                FireLaneConsume(unit, origin, lane, state);

                if (lane == laneTo || unit.Capacity <= 0 || unit.State != CollectorState.OnConveyor)
                    break;
            }
        }

        private void FireLaneConsume(
            CollectorUnit unit,
            Vector2 origin,
            int lane,
            CollectorConsumeState state)
        {
            if (unit.State != CollectorState.OnConveyor || unit.Capacity <= 0)
                return;

            if (state.HasLane && state.LastLane == lane)
                return;

            state.LastLane = lane;
            state.HasLane = true;

            GameConfigSO config = Config;
            if (config == null || _grid == null)
                return;

            Vector2 gridCenter = _grid.GridCenterWorld;
            Vector2 shootDirection = PerpendicularRaycastSensor.ComputePerpendicular(
                unit.CurrentMoveDirection.normalized,
                config.RaycastSide,
                origin,
                gridCenter);

            bool canShoot = PerpendicularRaycastSensor.TryDetectConsumable(
                origin,
                unit.CurrentMoveDirection,
                unit.Color,
                config,
                gridCenter,
                out PixelBlock hitBlock);

            if (!canShoot)
                return;

            ColorId blockColor = hitBlock.Color;
            Vector3 shotOrigin = unit.Body != null
                ? (Vector3)unit.Body.position
                : unit.transform.position;
            Vector3 shotTarget = hitBlock.transform.position;
            float visualDelay = Mathf.Max(0.03f, config.CollectorShotTravelDuration);
            if (!_grid.TryConsumeBlock(unit.Color, hitBlock.GridPosition, visualDelay))
                return;

            GameEvents.RaiseCollectorShot(shotOrigin, shotTarget);
            unit.FaceInwardForRestOfLap(shootDirection);
            unit.PlayShotShake();

            if (!CapacityLogic.TryConsume(unit.Capacity, unit.Color, blockColor, out int newCapacity))
                return;

            unit.SetCapacity(newCapacity);

            if (unit.Capacity == 0)
                RequestBeginExit(unit);
        }

        private void RequestBeginExit(CollectorUnit unit)
        {
            if (unit == null)
                return;

            bool wasOnConveyor = unit.State == CollectorState.OnConveyor;
            unit.ApplyExitTunables(Config);
            if (TryGetPlayfield(out CollectorPlayfieldSnapshot playfield))
                unit.SetExitFlyDirectionFrom(playfield.Center);

            if (!unit.BeginExit())
                return;

            if (wasOnConveyor && _conveyor != null)
                _conveyor.UnregisterUnit(unit);

            _consumeStates.Remove(unit);
        }

        private CollectorConsumeState GetConsumeState(CollectorUnit unit)
        {
            if (!_consumeStates.TryGetValue(unit, out CollectorConsumeState state))
            {
                state = new CollectorConsumeState();
                _consumeStates[unit] = state;
            }

            return state;
        }

        private void ResetConsumeState(CollectorUnit unit)
        {
            if (unit == null)
                return;

            CollectorConsumeState state = GetConsumeState(unit);
            state.NextConsumeTime = 0f;
            state.LastLane = 0;
            state.HasLane = false;
            state.MoveDirection = Vector2.right;
        }

        private static void DeclareDefeatForFullQueue()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.DeclareDefeat();
        }

        private sealed class CollectorConsumeState
        {
            public float NextConsumeTime;
            public int LastLane;
            public bool HasLane;
            public Vector2 MoveDirection = Vector2.right;
        }
    }
}