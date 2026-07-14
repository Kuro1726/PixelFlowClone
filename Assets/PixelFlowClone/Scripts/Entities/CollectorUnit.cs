using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.Queue;
using TMPro;
using UnityEngine;

namespace PixelFlowClone.Entities
{
    /// <summary>
    /// Collector entity that travels the conveyor and consumes matching blocks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CollectorUnit : MonoBehaviour, ITappable
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text _capacityLabel;
        [SerializeField] private float _exitDuration = 0.25f;

        private readonly CollectorStateMachine _stateMachine = new();
        private Coroutine _exitRoutine;
        private Vector3 _defaultScale = Vector3.one;
        private float _nextConsumeTime;

        public ColorId Color { get; private set; }
        public int Capacity { get; private set; }
        public CollectorState State => _stateMachine.CurrentState;

        public Rigidbody2D Body => _rigidbody;

        /// <summary>Normalized direction of the current movement segment (for perpendicular raycast).</summary>
        public Vector2 CurrentMoveDirection { get; private set; } = Vector2.right;

        public bool TrySetState(CollectorState target) => _stateMachine.TryTransition(target);

        public void ForceState(CollectorState state) => _stateMachine.ResetTo(state);

        /// <summary>
        /// Player tap. Waiting stack → direct to conveyor; queue slot → manual re-dispatch.
        /// Front-of-stack / slot ownership are enforced by QueueManager (P2-05+).
        /// </summary>
        public void OnTap()
        {
            switch (State)
            {
                case CollectorState.InWaitingStack:
                    TryDispatchToConveyorFromTap();
                    break;
                case CollectorState.InQueueSlot:
                    TryDispatchToConveyorFromTap();
                    break;
                case CollectorState.OnConveyor:
                case CollectorState.Exiting:
                case CollectorState.Pooled:
                default:
                    // Not tappable in these states.
                    break;
            }
        }

        private void TryDispatchToConveyorFromTap()
        {
            if (!ConveyorPathManager.HasInstance)
            {
                Debug.LogWarning("[CollectorUnit] OnTap: ConveyorPathManager missing.");
                return;
            }

            bool ok = ConveyorPathManager.Instance.DispatchToConveyor(this);
            if (!ok)
            {
                // P3-19: shake + reject SFX when conveyor is full.
                Debug.Log($"[CollectorUnit] OnTap rejected ({State}, conveyor full or already active).");
            }
        }

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            _defaultScale = transform.localScale;
        }

        private void FixedUpdate()
        {
            if (State != CollectorState.OnConveyor)
                return;

            DrawPerpendicularRayPreview();
            TryConsumeBlocks();
        }

        /// <summary>
        /// Keeps Debug.DrawRay visible every FixedUpdate while on the conveyor (P1-31).
        /// </summary>
        private void DrawPerpendicularRayPreview()
        {
            if (!ConveyorPathManager.HasInstance)
                return;

            GameConfigSO config = ConveyorPathManager.Instance.Config;
            if (config == null)
                return;

            Vector2 origin = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            Vector2 gridCenter = GridManager.HasInstance
                ? GridManager.Instance.GridCenterWorld
                : Vector2.zero;

            PerpendicularRaycastSensor.DrawDebugRayPreview(origin, CurrentMoveDirection, config, gridCenter);
        }

        /// <summary>
        /// Configures color and capacity after being pulled from the pool.
        /// </summary>
        public void Initialize(ColorId color, int capacity)
        {
            Color = color;
            Capacity = capacity;

            if (_spriteRenderer != null) _spriteRenderer.color = ColorPalette.ToColor(color);
            RefreshCapacityLabel();
            _nextConsumeTime = 0f;
        }

        /// <summary>
        /// Sets both Transform and Rigidbody2D position. Required for kinematic bodies —
        /// assigning transform alone leaves Rigidbody2D.position stale (pool/default),
        /// so raycasts would still fire from the old spot (often near the grid center).
        /// </summary>
        public void SetWorldPosition(Vector2 worldPos)
        {
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            if (_rigidbody != null)
            {
                _rigidbody.position = worldPos;
                _rigidbody.velocity = Vector2.zero;
            }
        }

        public void SetMoveDirection(Vector2 direction)
        {
            CurrentMoveDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.right;
        }

        public void SuppressConsumeFor(float seconds)
        {
            _nextConsumeTime = Time.time + Mathf.Max(0f, seconds);
        }

        public void SetCapacity(int capacity)
        {
            Capacity = Mathf.Max(0, capacity);
            RefreshCapacityLabel();

            if (Capacity == 0 && State == CollectorState.OnConveyor)
                BeginExit();
        }

        public void OnSpawnFromPool()
        {
            CurrentMoveDirection = Vector2.right;
            transform.localScale = _defaultScale;
            _exitRoutine = null;
            _nextConsumeTime = 0f;
        }

        /// <summary>
        /// Moves toward <paramref name="waypointListIndex"/> along the conveyor loop using kinematic physics.
        /// Returns true when the target waypoint was reached and the index advanced to the next segment.
        /// </summary>
        public bool TickMovement(
            float deltaTime,
            IReadOnlyList<ConveyorWaypoint> waypoints,
            ref int waypointListIndex,
            float speed,
            float reachEpsilon)
        {
            if (State != CollectorState.OnConveyor || _rigidbody == null)
                return false;

            if (waypoints == null || waypoints.Count == 0)
                return false;

            waypointListIndex = Mathf.Clamp(waypointListIndex, 0, waypoints.Count - 1);
            Vector2 target = waypoints[waypointListIndex].Position;
            Vector2 pos = _rigidbody.position;
            Vector2 toTarget = target - pos;
            float distance = toTarget.magnitude;

            if (distance <= reachEpsilon)
            {
                _rigidbody.MovePosition(target);
                CurrentMoveDirection = GetSegmentDirection(waypoints, waypointListIndex);
                AdvanceWaypointIndex(ref waypointListIndex, waypoints.Count);
                return true;
            }

            Vector2 dir = toTarget / distance;
            CurrentMoveDirection = dir;
            float step = speed * deltaTime;

            if (distance <= step)
            {
                _rigidbody.MovePosition(target);
                AdvanceWaypointIndex(ref waypointListIndex, waypoints.Count);
                return true;
            }

            _rigidbody.MovePosition(pos + dir * step);
            return false;
        }

        private static Vector2 GetSegmentDirection(IReadOnlyList<ConveyorWaypoint> waypoints, int listIndex)
        {
            int nextIndex = (listIndex + 1) % waypoints.Count;
            Vector2 delta = waypoints[nextIndex].Position - waypoints[listIndex].Position;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
        }

        private static void AdvanceWaypointIndex(ref int waypointListIndex, int waypointCount)
        {
            waypointListIndex = (waypointListIndex + 1) % waypointCount;
        }

        /// <summary>
        /// Raycasts perpendicular to movement and consumes a matching block when hit.
        /// </summary>
        public void TryConsumeBlocks()
        {
            if (State != CollectorState.OnConveyor || Capacity <= 0)
                return;

            if (Time.time < _nextConsumeTime)
                return;

            if (!ConveyorPathManager.HasInstance || !GridManager.HasInstance)
                return;

            GameConfigSO config = ConveyorPathManager.Instance.Config;
            if (config == null)
                return;

            Vector2 origin = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            Vector2 gridCenter = GridManager.Instance.GridCenterWorld;

            if (!PerpendicularRaycastSensor.TryDetectConsumable(
                    origin,
                    CurrentMoveDirection,
                    Color,
                    config,
                    gridCenter,
                    out PixelBlock hitBlock))
                return;

            if (!GridManager.Instance.TryConsumeBlock(Color, hitBlock.GridPosition))
                return;

            Capacity = Mathf.Max(0, Capacity - 1);
            RefreshCapacityLabel();

            float cooldown = Mathf.Max(0f, config.ConsumeCooldownSeconds);
            _nextConsumeTime = Time.time + cooldown;

            if (Capacity == 0)
                BeginExit();
        }

        /// <summary>
        /// Called when the unit completes one full conveyor loop and returns to the entry waypoint.
        /// Capacity &gt; 0 → queue (Phase 2); Capacity == 0 → exit.
        /// </summary>
        public void OnLapComplete()
        {
            if (Capacity <= 0)
                BeginExit();
            // Phase 2: else → InQueueSlot via QueueManager.
        }

        /// <summary>
        /// Transitions OnConveyor → Exiting, leaves the conveyor roster, plays exit stub, then pool-releases.
        /// </summary>
        public bool BeginExit()
        {
            if (State == CollectorState.Exiting || State == CollectorState.Pooled)
                return false;

            if (State == CollectorState.OnConveyor)
            {
                if (!TrySetState(CollectorState.Exiting))
                    return false;
            }
            else if (State == CollectorState.InQueueSlot || State == CollectorState.InWaitingStack)
            {
                ForceState(CollectorState.Exiting);
            }
            else
            {
                return false;
            }

            if (ConveyorPathManager.HasInstance)
                ConveyorPathManager.Instance.UnregisterUnit(this);

            Capacity = 0;
            RefreshCapacityLabel();

            if (_exitRoutine != null)
                StopCoroutine(_exitRoutine);

            _exitRoutine = StartCoroutine(ExitSequence());
            return true;
        }

        /// <summary>
        /// Stub exit animation (scale down). Phase 3 replaces with full tween / fly-off VFX.
        /// </summary>
        private IEnumerator ExitSequence()
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _exitDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            transform.localScale = Vector3.zero;
            CompleteExitAndRelease();
        }

        private void CompleteExitAndRelease()
        {
            _exitRoutine = null;

            if (!TrySetState(CollectorState.Pooled))
                ForceState(CollectorState.Pooled);

            GameEvents.RaiseCollectorExited(this);

            if (PoolManager.HasInstance)
                PoolManager.Instance.ReleaseCollector(this);
            else
                gameObject.SetActive(false);
        }

        public void ResetFromPool()
        {
            if (_exitRoutine != null)
            {
                StopCoroutine(_exitRoutine);
                _exitRoutine = null;
            }

            Color = ColorId.None;
            Capacity = 0;
            CurrentMoveDirection = Vector2.right;
            transform.localScale = _defaultScale;
            _nextConsumeTime = 0f;
            _stateMachine.ResetTo(CollectorState.Pooled);
            if (_capacityLabel != null) _capacityLabel.text = string.Empty;
        }

        private void RefreshCapacityLabel()
        {
            if (_capacityLabel != null)
                _capacityLabel.text = Capacity.ToString();
        }
    }
}
