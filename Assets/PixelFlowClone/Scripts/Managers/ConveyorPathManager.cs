using System.Collections.Generic;
using System.Linq;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Utils;
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
        [SerializeField] private float _pathMargin = LevelLayout.DefaultPathMargin;

        [Header("Belt Visual")]
        [SerializeField] private Sprite _beltSprite;
        [SerializeField] private int _beltSortingOrder = -8;
        [Tooltip("Fraction of belt sprite size from outer edge to the track centerline. " +
                 "Raise if collectors sit inside/outside the painted track.")]
        [SerializeField] [Range(0.02f, 0.25f)] private float _beltTrackInset = 0.11f;

        private readonly List<ConveyorWaypoint> _waypoints = new();
        private readonly List<CollectorUnit> _activeUnits = new();
        private readonly Dictionary<CollectorUnit, int> _unitWaypointIndices = new();
        private readonly Dictionary<CollectorUnit, bool> _hasLeftEntrySinceDispatch = new();

        private int _entryListIndex;
        private float _effectiveRaycastDistance;
        private Transform _beltVisual;

        public int ActiveCount => _activeUnits.Count;

        public int MaxCapacity => _config != null ? _config.MaxConveyorUnits : 5;

        public GameConfigSO Config => _config;

        public Transform PathRoot => _pathRoot;

        public float PathMargin => LevelLayout.ResolveConveyorPathMargin(_config);

        public IReadOnlyList<CollectorUnit> ActiveUnits => _activeUnits;

        public IReadOnlyList<ConveyorWaypoint> Waypoints => _waypoints;

        /// <summary>
        /// Raycast reach used for consume. Scales with the active level grid when configured.
        /// </summary>
        public float EffectiveRaycastDistance
        {
            get
            {
                if (_effectiveRaycastDistance > 0f)
                    return _effectiveRaycastDistance;
                return _config != null ? _config.RaycastDistance : 20f;
            }
        }

        public float MoveSpeed
        {
            get
            {
                float baseSpeed = _pathData != null && _config != null
                    ? _pathData.ResolveMoveSpeed(_config)
                    : _config != null ? _config.CollectorMoveSpeed : 3f;

                if (IsEndgameRushActive())
                {
                    float multiplier = _config != null ? _config.EndgameMoveSpeedMultiplier : 1f;
                    return baseSpeed * Mathf.Max(0.01f, multiplier);
                }

                return baseSpeed;
            }
        }

        public float WaypointReachEpsilon => _config != null ? _config.LapCompleteEpsilon : 0.05f;

        /// <summary>
        /// True when total collectors across conveyor + waiting + queue is at/below the config threshold.
        /// </summary>
        public bool IsEndgameRushActive()
        {
            if (_config == null)
                return false;

            int threshold = Mathf.Max(0, _config.EndgameCollectorThreshold);
            return CountAliveCollectors() <= threshold;
        }

        public static int CountAliveCollectors()
        {
            int total = 0;
            if (HasInstance)
                total += Instance.ActiveCount;

            if (QueueManager.HasInstance)
            {
                QueueManager queue = QueueManager.Instance;
                if (queue.Waiting != null)
                    total += queue.Waiting.Count;
                total += queue.OccupiedSlots;
            }

            return total;
        }

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

        /// <summary>
        /// Binds level path metadata and rebuilds waypoints around the fixed playfield
        /// using <see cref="GameConfigSO.ConveyorPathMargin"/>.
        /// </summary>
        public void ConfigureFromLevel(LevelDataSO level, Transform pathRoot, GameConfigSO config)
        {
            if (pathRoot != null)
                _pathRoot = pathRoot;
            if (config != null)
                _config = config;
            if (level != null)
                _pathData = level.PathReference;

            if (GridManager.HasInstance)
            {
                RebuildPathAroundPlayfield(
                    GridManager.Instance.GridCenterWorld,
                    GridManager.Instance.PlayfieldSize,
                    PathMargin);
            }
            else
            {
                CacheWaypoints();
            }

            float configured = _config != null ? _config.RaycastDistance : 20f;
            float playfieldReach = 8f;
            if (GridManager.HasInstance)
            {
                Vector2 size = GridManager.Instance.PlayfieldSize;
                playfieldReach = Mathf.Max(size.x, size.y) * 0.5f + PathMargin + 1f;
            }

            _effectiveRaycastDistance = Mathf.Max(configured, playfieldReach);
            NotifyConveyorCountChanged();
            Debug.Log(
                $"[ConveyorPathManager] Path rebuilt around playfield; margin={PathMargin:0.##}, " +
                $"raycast={_effectiveRaycastDistance:0.##}");
        }

        /// <summary>
        /// Moves (or creates) the 8 loop waypoints around the fixed playfield frame.
        /// </summary>
        public void RebuildPathAroundPlayfield(
            Vector2 playfieldCenter,
            Vector2 playfieldSize,
            float pathMargin)
        {
            if (_pathRoot == null)
            {
                var go = new GameObject("ConveyorPath");
                _pathRoot = go.transform;
            }

            IReadOnlyList<Vector2> positions = LevelLayout.ComputeConveyorLoopPositionsForPlayfield(
                playfieldCenter,
                playfieldSize,
                pathMargin);
            ApplyWaypointPositions(positions);
            CacheWaypoints();
            RefreshBeltVisual();
        }

        /// <summary>
        /// Moves (or creates) the 8 loop waypoints to frame the level grid (SO-space).
        /// </summary>
        public void RebuildPathAroundLevel(LevelDataSO level)
        {
            if (level == null)
                return;

            if (_pathRoot == null)
            {
                var go = new GameObject("ConveyorPath");
                _pathRoot = go.transform;
            }

            IReadOnlyList<Vector2> positions = LevelLayout.ComputeConveyorLoopPositions(
                level,
                Mathf.Max(_pathMargin, LevelLayout.DefaultPathMargin));
            ApplyWaypointPositions(positions);
            CacheWaypoints();
            RefreshBeltVisual();
        }

        private void RefreshBeltVisual()
        {
            if (!Application.isPlaying || _beltSprite == null)
                return;

            if (_waypoints == null || _waypoints.Count == 0)
                return;

            EnsureBeltVisualObject();
            if (_beltVisual == null)
                return;

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i] == null)
                    continue;
                Vector2 p = _waypoints[i].Position;
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }

            if (float.IsInfinity(minX) || float.IsInfinity(minY))
                return;

            Vector2 center = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            float pathW = Mathf.Max(0.01f, maxX - minX);
            float pathH = Mathf.Max(0.01f, maxY - minY);

            // Waypoints sit on the track centerline. Expand the sprite so that centerline
            // (inset from the art's outer edge) lines up with the waypoint rectangle.
            float inset = Mathf.Clamp(_beltTrackInset, 0.02f, 0.25f);
            float denom = Mathf.Max(0.2f, 1f - 2f * inset);
            float worldW = pathW / denom;
            float worldH = pathH / denom;

            SpriteRenderer sr = _beltVisual.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = _beltVisual.gameObject.AddComponent<SpriteRenderer>();

            sr.sprite = _beltSprite;
            sr.sortingOrder = _beltSortingOrder;
            sr.color = Color.white;
            sr.enabled = true;

            Vector2 spriteSize = new(
                _beltSprite.rect.width / _beltSprite.pixelsPerUnit,
                _beltSprite.rect.height / _beltSprite.pixelsPerUnit);
            if (spriteSize.x < 0.001f || spriteSize.y < 0.001f)
                return;

            _beltVisual.position = new Vector3(center.x, center.y, 0f);
            _beltVisual.rotation = Quaternion.identity;
            _beltVisual.localScale = new Vector3(
                worldW / spriteSize.x,
                worldH / spriteSize.y,
                1f);
        }

        private void EnsureBeltVisualObject()
        {
            if (_beltVisual != null)
                return;

            // Parent to this manager (not pathRoot) so waypoint rebuild never touches it.
            Transform existing = transform.Find("BeltVisual");
            if (existing != null)
            {
                _beltVisual = existing;
                return;
            }

            var go = new GameObject("BeltVisual");
            _beltVisual = go.transform;
            _beltVisual.SetParent(transform, false);
        }

        private void ApplyWaypointPositions(IReadOnlyList<Vector2> positions)
        {
            ConveyorWaypoint[] existing = _pathRoot.GetComponentsInChildren<ConveyorWaypoint>(true)
                .OrderBy(w => w.Index)
                .ToArray();

            for (int i = 0; i < positions.Count; i++)
            {
                ConveyorWaypoint waypoint;
                if (i < existing.Length)
                {
                    waypoint = existing[i];
                }
                else
                {
                    var go = new GameObject($"Waypoint_{i:D2}");
                    go.transform.SetParent(_pathRoot, false);
                    waypoint = go.AddComponent<ConveyorWaypoint>();
                    SetWaypointIndex(waypoint, i);
                }

                waypoint.transform.position = new Vector3(positions[i].x, positions[i].y, 0f);
                SetWaypointIndex(waypoint, i);
                waypoint.gameObject.SetActive(true);
            }

            for (int i = positions.Count; i < existing.Length; i++)
            {
                if (existing[i] != null)
                    existing[i].gameObject.SetActive(false);
            }
        }

        private static void SetWaypointIndex(ConveyorWaypoint waypoint, int index)
        {
            if (waypoint != null)
                waypoint.SetIndex(index);
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

            unit.PrepareConveyorFacing();

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

            // Endgame rush keeps the unit on the belt — reset lap flag so the next circuit can fire.
            if (unit != null &&
                unit.State == CollectorState.OnConveyor &&
                _hasLeftEntrySinceDispatch.ContainsKey(unit))
            {
                _hasLeftEntrySinceDispatch[unit] = false;
            }

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
