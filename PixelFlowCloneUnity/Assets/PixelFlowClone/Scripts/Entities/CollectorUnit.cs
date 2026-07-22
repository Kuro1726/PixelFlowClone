using System;
using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Queue;
using PixelFlowClone.Utils;
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
        private const string ShotShakeVisualName = "ShotShakeVisual";

        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text _capacityLabel;
        [SerializeField] private float _exitDuration = 0.35f;
        [SerializeField] private float _exitFlyDistance = 1.75f;
        [SerializeField] private float _rejectShakeDuration = 0.28f;
        [SerializeField] private float _rejectShakeAmplitude = 0.18f;
        [Tooltip("World degrees the sprite nose points at local rotation 0. Right=0, Up=90, Left=180, Down=-90.")]
        [SerializeField] private float _spriteNoseAngleAtRest = 90f;
        [Tooltip("Seconds used to blend the collector's rotation while turning on the conveyor.")]
        [SerializeField] [Min(0f)] private float _turnSmoothTime = 0.08f;
        [SerializeField] [Min(0.01f)] private float _shotShakeDuration = 0.12f;
        [SerializeField] [Min(0f)] private float _shotShakeAmplitude = 0.045f;
        [SerializeField] [Min(1f)] private float _shotShakeOscillations = 3f;

        private readonly CollectorStateMachine _stateMachine = new();
        private Coroutine _exitRoutine;
        private Coroutine _rejectShakeRoutine;
        private Vector3 _defaultScale = Vector3.one;
        private Vector3 _capacityLabelWorldOffset;
        private Vector3 _rejectShakeOrigin;
        private float _exitDurationOverride = -1f;
        private float _exitFlyDistanceOverride = -1f;
        private bool _hasExitFlyDirectionOverride;
        private Vector2 _exitFlyDirectionOverride = Vector2.zero;
        private bool _isOnRoundedCorner;
        private float _turnAngularVelocity;
        private Transform _shotShakeVisual;
        private Vector3 _shotShakeRestLocalPosition;
        private float _shotShakeElapsed;
        private bool _isShotShakeActive;
        /// <summary>After first successful consume this lap, face inward until lap ends.</summary>
        private bool _faceInwardForRestOfLap;
        private Vector2 _inwardFacingDirection = Vector2.zero;

        public static event Action<CollectorUnit> Tapped;
        public static event Action<CollectorUnit, Vector2, Vector2> MovementAdvanced;
        public static event Action<CollectorUnit> ConsumePreviewRequested;
        public static event Action<CollectorUnit, float> ConsumeSuppressed;
        public static event Action<CollectorUnit> LapCompleted;
        public static event Action<CollectorUnit> ExitCompleted;

        public ColorId Color { get; private set; }
        public int Capacity { get; private set; }
        public CollectorState State => _stateMachine.CurrentState;

        public Rigidbody2D Body => _rigidbody;

        /// <summary>Cardinal gameplay direction used for lane selection and perpendicular raycasts.</summary>
        public Vector2 CurrentMoveDirection { get; private set; } = Vector2.right;

        public bool IsOnRoundedCorner => _isOnRoundedCorner;

        public bool TrySetState(CollectorState target) => _stateMachine.TryTransition(target);

        public void ForceState(CollectorState state)
        {
            _stateMachine.ResetTo(state);
            if (state == CollectorState.InWaitingStack || state == CollectorState.InQueueSlot)
                ResetFacing();
        }

        /// <summary>
        /// Player tap. Waiting stack → direct to conveyor; queue slot → manual re-dispatch.
        /// Front-of-stack / slot ownership are enforced by the collector flow coordinator (P2-05+).
        /// </summary>
        public void OnTap()
        {
            Debug.Log($"[CollectorUnit] OnTap color={Color} capacity={Capacity} state={State}");
            Tapped?.Invoke(this);
        }

        /// <summary>
        /// P3-20: brief horizontal shake when conveyor dispatch is rejected.
        /// </summary>
        public void PlayRejectShake()
        {
            if (State == CollectorState.Exiting || State == CollectorState.Pooled)
                return;

            if (!isActiveAndEnabled)
                return;

            if (_rejectShakeRoutine != null)
            {
                StopCoroutine(_rejectShakeRoutine);
                transform.position = _rejectShakeOrigin;
                _rejectShakeRoutine = null;
            }

            _rejectShakeOrigin = transform.position;
            _rejectShakeRoutine = StartCoroutine(RejectShakeSequence());
        }

        private IEnumerator RejectShakeSequence()
        {
            float duration = Mathf.Max(0.05f, _rejectShakeDuration);
            float amplitude = Mathf.Max(0.01f, _rejectShakeAmplitude);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float damping = 1f - u;
                float offset = Mathf.Sin(elapsed * 55f) * amplitude * damping;
                transform.position = _rejectShakeOrigin + new Vector3(offset, 0f, 0f);
                yield return null;
            }

            transform.position = _rejectShakeOrigin;
            _rejectShakeRoutine = null;
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
            ResolveShotShakeVisual();
            GameplayFontUtility.Apply(_capacityLabel);
            if (_capacityLabel != null)
            {
                Vector2 authoredPosition = _capacityLabel.rectTransform.anchoredPosition;
                _capacityLabelWorldOffset = new Vector3(
                    authoredPosition.x,
                    authoredPosition.y,
                    _capacityLabel.transform.localPosition.z);
            }
            _defaultScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (!_isShotShakeActive || _shotShakeVisual == null)
                return;

            _shotShakeElapsed += Time.unscaledDeltaTime;
            CollectorShotShakeFrame frame = CollectorShotShakeAnimation.Evaluate(
                _shotShakeElapsed,
                _shotShakeDuration,
                _shotShakeAmplitude,
                _shotShakeOscillations);

            _shotShakeVisual.localPosition =
                _shotShakeRestLocalPosition + (Vector3)frame.LocalOffset;

            if (frame.IsComplete)
                ResetShotShake();
        }

        private void OnDisable()
        {
            ResetShotShake();
        }

        private void FixedUpdate()
        {
            if (State == CollectorState.OnConveyor)
                ConsumePreviewRequested?.Invoke(this);
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
            Vector2 tangent = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.right;
            CurrentMoveDirection = SnapToCardinal(tangent);
            _isOnRoundedCorner = IsDiagonal(tangent);
        }

        /// <summary>
        /// Call when the unit is placed on the belt: face right (initial path direction).
        /// </summary>
        public void PrepareConveyorFacing()
        {
            _faceInwardForRestOfLap = false;
            _inwardFacingDirection = Vector2.zero;
            _isOnRoundedCorner = false;
            ApplyFacingFromMoveDirection(Vector2.right, true);
        }

        public void SuppressConsumeFor(float seconds)
        {
            ConsumeSuppressed?.Invoke(this, seconds);
        }

        public void SetCapacity(int capacity)
        {
            Capacity = Mathf.Max(0, capacity);
            RefreshCapacityLabel();
        }

        public void OnSpawnFromPool()
        {
            ResetShotShake();
            ResetExitOverrides();
            CurrentMoveDirection = Vector2.right;
            _isOnRoundedCorner = false;
            transform.localScale = _defaultScale;
            ResetFacing();
            _exitRoutine = null;
        }

        /// <summary>
        /// Moves toward the current waypoint, then fires one inward raycast per grid lane crossed.
        /// Lane = column index when moving horizontally, row index when moving vertically.
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

            Vector2 before = _rigidbody.position;

            waypointListIndex = Mathf.Clamp(waypointListIndex, 0, waypoints.Count - 1);
            Vector2 target = waypoints[waypointListIndex].Position;
            Vector2 toTarget = target - before;
            float distance = toTarget.magnitude;
            bool reachedWaypoint = false;

            if (distance <= reachEpsilon)
            {
                ApplyKinematicPosition(target);
                RefreshConsumeDirection(GetSegmentDirection(waypoints, waypointListIndex));
                AdvanceWaypointIndex(ref waypointListIndex, waypoints.Count);
                reachedWaypoint = true;
            }
            else
            {
                Vector2 dir = toTarget / distance;
                RefreshConsumeDirection(dir);
                float step = speed * deltaTime;
                if (distance <= step)
                {
                    ApplyKinematicPosition(target);
                    AdvanceWaypointIndex(ref waypointListIndex, waypoints.Count);
                    reachedWaypoint = true;
                }
                else
                {
                    ApplyKinematicPosition(before + dir * step);
                }
            }

            Vector2 after = _rigidbody.position;
            MovementAdvanced?.Invoke(this, before, after);
            return reachedWaypoint;
        }

        private void RefreshConsumeDirection(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f)
                return;

            Vector2 tangent = dir.normalized;
            Vector2 cardinalDirection = SnapToCardinal(tangent);
            _isOnRoundedCorner = IsDiagonal(tangent);

            CurrentMoveDirection = cardinalDirection;
            RefreshVisualFacing(tangent);
        }

        private static Vector2 SnapToCardinal(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return Vector2.right;

            return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2(direction.x >= 0f ? 1f : -1f, 0f)
                : new Vector2(0f, direction.y >= 0f ? 1f : -1f);
        }

        private static bool IsDiagonal(Vector2 direction)
        {
            const float axisEpsilon = 0.001f;
            return Mathf.Abs(direction.x) > axisEpsilon && Mathf.Abs(direction.y) > axisEpsilon;
        }

        /// <summary>
        /// Before first shot this lap: face along the belt.
        /// After first successful consume: keep facing inward toward the grid until lap ends.
        /// </summary>
        private void RefreshVisualFacing(Vector2 pathTangent)
        {
            if (_faceInwardForRestOfLap)
            {
                Vector2 inward = _inwardFacingDirection;
                if (inward.sqrMagnitude > 0.0001f)
                {
                    ApplyFacingFromMoveDirection(inward);
                    return;
                }
            }

            ApplyFacingFromMoveDirection(pathTangent);
        }

        public void FaceInwardForRestOfLap(Vector2 inwardDirection)
        {
            if (inwardDirection.sqrMagnitude < 0.0001f)
                return;

            _faceInwardForRestOfLap = true;
            _inwardFacingDirection = inwardDirection.normalized;
            ApplyFacingFromMoveDirection(_inwardFacingDirection);
        }

        public void ResetLapFacing()
        {
            _faceInwardForRestOfLap = false;
            _inwardFacingDirection = Vector2.zero;
        }

        /// <summary>
        /// Points the sprite nose along <paramref name="dir"/> (belt move or shoot inward).
        /// </summary>
        private void ApplyFacingFromMoveDirection(Vector2 dir, bool snap = false)
        {
            if (dir.sqrMagnitude < 0.0001f)
                return;

            // Atan2: angle of desired nose from +X. Subtract art's rest nose angle.
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - _spriteNoseAngleAtRest;
            SetFacingAngle(angle, snap);
        }

        private void ResetFacing()
        {
            _faceInwardForRestOfLap = false;
            _inwardFacingDirection = Vector2.zero;
            // Waiting / queue / pool: authored art pose (nose up at rest).
            SetFacingAngle(0f, true);
        }

        private void SetFacingAngle(float zDegrees, bool snap = false)
        {
            float appliedAngle = zDegrees;
            if (!snap && _turnSmoothTime > 0.0001f)
            {
                float currentAngle = _rigidbody != null
                    ? _rigidbody.rotation
                    : transform.eulerAngles.z;
                appliedAngle = Mathf.SmoothDampAngle(
                    currentAngle,
                    zDegrees,
                    ref _turnAngularVelocity,
                    _turnSmoothTime,
                    Mathf.Infinity,
                    Time.fixedDeltaTime);
            }
            else
            {
                _turnAngularVelocity = 0f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, appliedAngle);
            if (_rigidbody != null)
                _rigidbody.rotation = appliedAngle;

            SnapCapacityLabelAbove();
        }

        private void SnapCapacityLabelAbove()
        {
            if (_capacityLabel == null)
                return;

            _capacityLabel.transform.position = transform.position + _capacityLabelWorldOffset;
            _capacityLabel.transform.rotation = Quaternion.identity;
        }

        private void ApplyKinematicPosition(Vector2 worldPos)
        {
            if (_rigidbody != null)
            {
                _rigidbody.position = worldPos;
                _rigidbody.MovePosition(worldPos);
            }

            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            SnapCapacityLabelAbove();
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

        /// <summary>Legacy no-op; lane consume runs inside <see cref="TickMovement"/>.</summary>
        public void TryConsumeAlongMovement(Vector2 from, Vector2 to)
        {
        }

        public void TryConsumeBlocks()
        {
            Vector2 pos = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            MovementAdvanced?.Invoke(this, pos, pos);
        }

        public void TryConsumeOnce()
        {
            TryConsumeBlocks();
        }

        /// <summary>
        /// Called when the unit reaches the path's configured lap-complete waypoint.
        /// Capacity == 0 → exit. Otherwise → queue, unless endgame rush keeps it circulating.
        /// </summary>
        public void OnLapComplete()
        {
            ResetLapFacing();
            LapCompleted?.Invoke(this);
        }

        /// <summary>
        /// Transitions OnConveyor → Exiting, leaves the conveyor roster, plays exit tween, then pool-releases.
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

            Capacity = 0;
            RefreshCapacityLabel();

            if (_exitRoutine != null)
                StopCoroutine(_exitRoutine);

            _exitRoutine = StartCoroutine(ExitSequence());
            return true;
        }

        /// <summary>
        /// P3-18: scale-down + fly-off tween before release intent.
        /// </summary>
        private IEnumerator ExitSequence()
        {
            ResolveExitTunables(out float duration, out float flyDistance);

            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            UnityEngine.Color startSpriteColor = _spriteRenderer != null
                ? _spriteRenderer.color
                : UnityEngine.Color.white;
            Vector3 flyDir = ResolveExitFlyDirection();
            Vector3 endPos = startPos + flyDir * flyDistance;

            if (_rigidbody != null)
                _rigidbody.simulated = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                // Ease-in-out for scale; ease-out for travel so it pops away quickly at the end.
                float scaleT = u * u * (3f - 2f * u);
                float moveT = 1f - (1f - u) * (1f - u);

                transform.position = Vector3.Lerp(startPos, endPos, moveT);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);

                if (_spriteRenderer != null)
                {
                    UnityEngine.Color c = startSpriteColor;
                    c.a = Mathf.Lerp(startSpriteColor.a, 0f, scaleT);
                    _spriteRenderer.color = c;
                }

                yield return null;
            }

            transform.position = endPos;
            transform.localScale = Vector3.zero;
            if (_spriteRenderer != null)
            {
                UnityEngine.Color c = startSpriteColor;
                c.a = 0f;
                _spriteRenderer.color = c;
            }

            CompleteExitAndRelease();
        }

        private void ResolveExitTunables(out float duration, out float flyDistance)
        {
            duration = _exitDurationOverride > 0.01f
                ? _exitDurationOverride
                : Mathf.Max(0.05f, _exitDuration);
            flyDistance = _exitFlyDistanceOverride >= 0f
                ? _exitFlyDistanceOverride
                : Mathf.Max(0f, _exitFlyDistance);
        }

        private Vector3 ResolveExitFlyDirection()
        {
            if (_hasExitFlyDirectionOverride)
                return new Vector3(_exitFlyDirectionOverride.x, _exitFlyDirectionOverride.y, 0f);

            Vector2 outward = CurrentMoveDirection;
            if (outward.sqrMagnitude < 0.0001f)
                outward = Vector2.up;
            else
                outward.Normalize();

            return new Vector3(outward.x, outward.y, 0f);
        }

        public void ApplyExitTunables(GameConfigSO config)
        {
            _exitDurationOverride = config != null && config.CollectorExitDuration > 0.01f
                ? config.CollectorExitDuration
                : -1f;
            _exitFlyDistanceOverride = config != null && config.CollectorExitFlyDistance >= 0f
                ? config.CollectorExitFlyDistance
                : -1f;
        }

        public void SetExitFlyDirectionFrom(Vector2 playfieldCenter)
        {
            Vector2 fromCenter = (Vector2)transform.position - playfieldCenter;
            if (fromCenter.sqrMagnitude <= 0.0001f)
                return;

            _exitFlyDirectionOverride = fromCenter.normalized;
            _hasExitFlyDirectionOverride = true;
        }

        private void ResetExitOverrides()
        {
            _exitDurationOverride = -1f;
            _exitFlyDistanceOverride = -1f;
            _hasExitFlyDirectionOverride = false;
            _exitFlyDirectionOverride = Vector2.zero;
        }
        private void CompleteExitAndRelease()
        {
            _exitRoutine = null;

            if (_rigidbody != null)
                _rigidbody.simulated = true;

            if (_spriteRenderer != null)
            {
                UnityEngine.Color c = _spriteRenderer.color;
                c.a = 1f;
                _spriteRenderer.color = c;
            }

            if (!TrySetState(CollectorState.Pooled))
                ForceState(CollectorState.Pooled);

            ExitCompleted?.Invoke(this);
        }

        public void ResetFromPool()
        {
            ResetShotShake();
            ResetExitOverrides();

            if (_exitRoutine != null)
            {
                StopCoroutine(_exitRoutine);
                _exitRoutine = null;
            }

            if (_rejectShakeRoutine != null)
            {
                StopCoroutine(_rejectShakeRoutine);
                _rejectShakeRoutine = null;
            }

            Color = ColorId.None;
            Capacity = 0;
            CurrentMoveDirection = Vector2.right;
            transform.localScale = _defaultScale;
            ResetFacing();
            _isOnRoundedCorner = false;
            _stateMachine.ResetTo(CollectorState.Pooled);
            if (_capacityLabel != null) _capacityLabel.text = string.Empty;

            if (_rigidbody != null)
                _rigidbody.simulated = true;

            if (_spriteRenderer != null)
            {
                UnityEngine.Color c = _spriteRenderer.color;
                c.a = 1f;
                _spriteRenderer.color = c;
            }
        }

        private void RefreshCapacityLabel()
        {
            if (_capacityLabel != null)
                _capacityLabel.text = Capacity.ToString();
        }

        private void ResolveShotShakeVisual()
        {
            if (_spriteRenderer == null)
                return;

            if (_spriteRenderer.transform != transform)
            {
                CacheShotShakeVisual(_spriteRenderer.transform);
                return;
            }

            Transform existingVisual = transform.Find(ShotShakeVisualName);
            if (existingVisual != null &&
                existingVisual.TryGetComponent(out SpriteRenderer existingRenderer))
            {
                _spriteRenderer.enabled = false;
                _spriteRenderer = existingRenderer;
                CacheShotShakeVisual(existingVisual);
                return;
            }

            SpriteRenderer sourceRenderer = _spriteRenderer;
            var visualObject = new GameObject(ShotShakeVisualName);
            visualObject.layer = gameObject.layer;
            visualObject.transform.SetParent(transform, false);

            SpriteRenderer visualRenderer = visualObject.AddComponent<SpriteRenderer>();
            CopySpriteRenderer(sourceRenderer, visualRenderer);
            sourceRenderer.enabled = false;

            _spriteRenderer = visualRenderer;
            CacheShotShakeVisual(visualObject.transform);
        }

        private void CacheShotShakeVisual(Transform visual)
        {
            _shotShakeVisual = visual;
            _shotShakeRestLocalPosition = visual.localPosition;
        }

        private static void CopySpriteRenderer(SpriteRenderer source, SpriteRenderer target)
        {
            target.sprite = source.sprite;
            target.color = source.color;
            target.sharedMaterial = source.sharedMaterial;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.drawMode = source.drawMode;
            target.size = source.size;
            target.maskInteraction = source.maskInteraction;
            target.spriteSortPoint = source.spriteSortPoint;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
            target.enabled = source.enabled;
        }

        public void PlayShotShake()
        {
            if (_shotShakeVisual == null || !isActiveAndEnabled)
                return;

            _shotShakeVisual.localPosition = _shotShakeRestLocalPosition;
            _shotShakeElapsed = 0f;
            _isShotShakeActive = true;
        }

        private void ResetShotShake()
        {
            _isShotShakeActive = false;
            _shotShakeElapsed = 0f;

            if (_shotShakeVisual != null)
                _shotShakeVisual.localPosition = _shotShakeRestLocalPosition;
        }
    }
}
