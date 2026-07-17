using System.Collections.Generic;
using System.Linq;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Scene-scoped manager for the closed conveyor loop: waypoint graph, capacity,
    /// and unit registration. Fires lap-complete when a unit returns to the entry waypoint.
    /// </summary>
    public class ConveyorPathManager : Singleton<ConveyorPathManager>
    {
        [Header("Scene References")]
        [SerializeField] private Transform _pathRoot;
        [SerializeField] private ConveyorPathSO _pathData;
        [SerializeField] private GameConfigSO _config;

        private readonly List<ConveyorWaypoint> _waypoints = new();
        private readonly List<CollectorUnit> _activeUnits = new();
        private readonly Dictionary<CollectorUnit, int> _unitWaypointIndices = new();
        private readonly Dictionary<CollectorUnit, bool> _hasLeftEntrySinceDispatch = new();

        private int _entryListIndex;

        public int ActiveCount => _activeUnits.Count;

        public int MaxCapacity => _config != null ? _config.MaxConveyorUnits : 5;

        public GameConfigSO Config => _config;

        public Transform PathRoot => _pathRoot;

        public IReadOnlyList<CollectorUnit> ActiveUnits => _activeUnits;

        public IReadOnlyList<ConveyorWaypoint> Waypoints => _waypoints;

        public float MoveSpeed => _pathData != null && _config != null
            ? _pathData.ResolveMoveSpeed(_config)
            : _config != null ? _config.CollectorMoveSpeed : 3f;

        public float WaypointReachEpsilon => _config != null ? _config.LapCompleteEpsilon : 0.05f;

        protected override void OnSingletonAwake()
        {
            CacheWaypoints();
        }

        /// <summary>
        /// Binds path metadata and rebuilds the waypoint list from the scene hierarchy.
        /// </summary>
        public void Configure(Transform pathRoot, ConveyorPathSO pathData, GameConfigSO config)
        {
            _pathRoot = pathRoot;
            _pathData = pathData;
            _config = config;
            CacheWaypoints();
        }

        public void ConfigureFromLevel(LevelDataSO level, Transform pathRoot, GameConfigSO config)
        {
            Configure(pathRoot, level != null ? level.PathReference : null, config);
        }

        public void CacheWaypoints()
        {
            _waypoints.Clear();

            if (_pathRoot == null)
            {
                Debug.LogWarning("[ConveyorPathManager] Path root is not assigned.");
                _entryListIndex = 0;
                return;
            }

            _waypoints.AddRange(
                _pathRoot.GetComponentsInChildren<ConveyorWaypoint>().OrderBy(w => w.Index));

            _entryListIndex = ResolveEntryListIndex();
        }

        public ConveyorWaypoint GetEntryWaypoint()
        {
            if (_waypoints.Count == 0)
                return null;

            return _waypoints[Mathf.Clamp(_entryListIndex, 0, _waypoints.Count - 1)];
        }

        /// <summary>
        /// Current ordered-list index of the waypoint the unit is moving toward / sitting on.
        /// </summary>
        public bool TryGetWaypointIndex(CollectorUnit unit, out int listIndex)
        {
            return _unitWaypointIndices.TryGetValue(unit, out listIndex);
        }

        public ConveyorWaypoint GetWaypointAtListIndex(int listIndex)
        {
            if (listIndex < 0 || listIndex >= _waypoints.Count)
                return null;

            return _waypoints[listIndex];
        }

        public bool HasCapacity => ActiveCount < MaxCapacity;

        public bool DispatchToConveyor(CollectorUnit unit)
        {
            if (unit == null)
                return false;

            if (ActiveCount >= MaxCapacity)
                return false;

            if (_activeUnits.Contains(unit))
                return false;

            if (_waypoints.Count == 0)
            {
                Debug.LogWarning("[ConveyorPathManager] Cannot dispatch — no waypoints cached.");
                return false;
            }

            RegisterUnit(unit, GetInitialMovementIndex());

            ConveyorWaypoint entry = GetEntryWaypoint();
            if (entry != null)
            {
                Vector2 entryPos = entry.Position;
                unit.SetWorldPosition(entryPos);

                ConveyorWaypoint next = GetWaypointAtListIndex(GetInitialMovementIndex());
                if (next != null)
                    unit.SetMoveDirection(next.Position - entryPos);
            }

            // Prevent same-frame FixedUpdate from consuming with stale physics state.
            Physics2D.SyncTransforms();

            // One physics step of grace so FixedUpdate never consumes with a stale body pose.
            unit.SuppressConsumeFor(Time.fixedDeltaTime * 2f);

            unit.TrySetState(CollectorState.OnConveyor);
            _hasLeftEntrySinceDispatch[unit] = false;
            return true;
        }

        private int GetInitialMovementIndex()
        {
            if (_waypoints.Count == 0)
                return 0;

            return (_entryListIndex + 1) % _waypoints.Count;
        }

        private void FixedUpdate()
        {
            if (GameManager.HasInstance && !GameManager.Instance.AcceptsGameplayInput)
                return;

            if (_waypoints.Count == 0 || _activeUnits.Count == 0)
                return;

            float deltaTime = Time.fixedDeltaTime;
            float speed = MoveSpeed;
            float reachEpsilon = WaypointReachEpsilon;

            // Iterate backwards because lap/exit callbacks may unregister the current unit.
            for (int i = _activeUnits.Count - 1; i >= 0; i--)
            {
                CollectorUnit unit = _activeUnits[i];
                if (unit == null || unit.State != CollectorState.OnConveyor)
                    continue;

                if (!_unitWaypointIndices.TryGetValue(unit, out int waypointIndex))
                    continue;

                int previousIndex = waypointIndex;
                bool reachedWaypoint = unit.TickMovement(
                    deltaTime, _waypoints, ref waypointIndex, speed, reachEpsilon);
                _unitWaypointIndices[unit] = waypointIndex;

                if (!reachedWaypoint)
                    continue;

                if (previousIndex != _entryListIndex)
                    _hasLeftEntrySinceDispatch[unit] = true;
                else if (_hasLeftEntrySinceDispatch.TryGetValue(unit, out bool hasLeftEntry) && hasLeftEntry)
                    HandleLapComplete(unit);
            }
        }

        private void HandleLapComplete(CollectorUnit unit)
        {
            unit.OnLapComplete();
            GameEvents.RaiseCollectorLapComplete(unit);
        }

        public void RegisterUnit(CollectorUnit unit)
        {
            RegisterUnit(unit, _entryListIndex);
        }

        public void RegisterUnit(CollectorUnit unit, int waypointListIndex)
        {
            if (unit == null || _activeUnits.Contains(unit))
                return;

            _activeUnits.Add(unit);
            _unitWaypointIndices[unit] = Mathf.Clamp(waypointListIndex, 0, Mathf.Max(0, _waypoints.Count - 1));
            _hasLeftEntrySinceDispatch.TryAdd(unit, false);
            NotifyConveyorCountChanged();
        }

        public void UnregisterUnit(CollectorUnit unit)
        {
            if (unit == null)
                return;

            if (!_activeUnits.Remove(unit))
                return;

            _unitWaypointIndices.Remove(unit);
            _hasLeftEntrySinceDispatch.Remove(unit);
            NotifyConveyorCountChanged();
        }

        /// <summary>Removes all active collectors when rebuilding or leaving a level.</summary>
        public void ClearActiveUnits()
        {
            for (int i = _activeUnits.Count - 1; i >= 0; i--)
            {
                CollectorUnit unit = _activeUnits[i];
                if (unit != null && PoolManager.HasInstance)
                    PoolManager.Instance.ReleaseCollector(unit);
            }

            _activeUnits.Clear();
            _unitWaypointIndices.Clear();
            _hasLeftEntrySinceDispatch.Clear();
            NotifyConveyorCountChanged();
        }

        /// <summary>
        /// Updates the tracked waypoint list index for a unit (used by movement in P1-25+).
        /// </summary>
        public void SetUnitWaypointIndex(CollectorUnit unit, int waypointListIndex)
        {
            if (unit == null || !_activeUnits.Contains(unit))
                return;

            _unitWaypointIndices[unit] = Mathf.Clamp(waypointListIndex, 0, Mathf.Max(0, _waypoints.Count - 1));
        }

        private int ResolveEntryListIndex()
        {
            if (_waypoints.Count == 0)
                return 0;

            int desiredIndex = _pathData != null ? _pathData.EntryWaypointIndex : 0;

            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i].Index == desiredIndex)
                    return i;
            }

            return _pathData != null
                ? _pathData.ClampEntryIndex(_waypoints.Count)
                : 0;
        }

        private void NotifyConveyorCountChanged()
        {
            GameEvents.RaiseConveyorCountChanged(ActiveCount, MaxCapacity);
        }
    }
}
