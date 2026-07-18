using System.Collections.Generic;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using PixelFlowClone.Utils;
using UnityEngine;

namespace PixelFlowClone.Queue
{
    /// <summary>
    /// Five horizontal queue slots (index 0–4). Units land here after a lap when capacity &gt; 0.
    /// Every occupied slot is tappable for manual re-dispatch to the conveyor.
    /// </summary>
    public class QueueSlotController : MonoBehaviour
    {
        [SerializeField] private Transform _slotsRoot;
        [SerializeField] private int _slotCount = 5;
        [SerializeField] private float _slotSpacing = 1.1f;
        [SerializeField] private Vector2 _layoutDirection = Vector2.right;
        [SerializeField] private Transform[] _slotAnchors;

        private CollectorUnit[] _occupants;
        private float _defaultSlotSpacing = -1f;

        public int MaxSlots => _slotCount;
        public int OccupiedCount
        {
            get
            {
                int count = 0;
                if (_occupants == null)
                    return 0;

                for (int i = 0; i < _occupants.Length; i++)
                {
                    if (_occupants[i] != null)
                        count++;
                }

                return count;
            }
        }

        public bool IsFull => OccupiedCount >= _slotCount;
        public bool HasEmptySlot => OccupiedCount < _slotCount;
        public IReadOnlyList<CollectorUnit> Units => _occupants;

        /// <summary>
        /// Applies per-level spacing and moves the queue row to
        /// <see cref="LevelLayout.GetPlayfieldQueueWorldPosition"/> when a GridManager playfield exists.
        /// </summary>
        public void AnchorToLevel(LevelDataSO level, float pathMargin = -1f)
        {
            if (level == null)
                return;

            EnsureInitialized();
            GameConfigSO config = ResolveConfig();
            ApplySpacingFromLevel(level, config);
            EnsureAnchors();

            if (GridManager.HasInstance)
            {
                float margin = pathMargin > 0.01f
                    ? pathMargin
                    : LevelLayout.ResolveConveyorPathMargin(config);

                Vector2 target = LevelLayout.GetPlayfieldQueueWorldPosition(
                    GridManager.Instance.GridCenterWorld,
                    GridManager.Instance.PlayfieldSize,
                    level,
                    margin,
                    config);

                if (_slotsRoot != null)
                    _slotsRoot.localPosition = Vector3.zero;

                transform.position = new Vector3(target.x, target.y, transform.position.z);
            }

            RefreshLayout();
        }

        /// <summary>Applies queue spacing from GameConfig (Level SO >0 overrides).</summary>
        public void ApplySpacingFromLevel(LevelDataSO level, GameConfigSO config = null)
        {
            if (_defaultSlotSpacing < 0f)
                _defaultSlotSpacing = _slotSpacing;

            if (config == null)
                config = ResolveConfig();

            _slotSpacing = LevelLayout.ResolveQueueUnitSpacing(level, config, _defaultSlotSpacing);
        }

        private static GameConfigSO ResolveConfig()
        {
            if (ConveyorPathManager.HasInstance)
                return ConveyorPathManager.Instance.Config;
            return null;
        }

        public bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < _slotCount;

        public CollectorUnit GetUnit(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || _occupants == null)
                return null;

            return _occupants[slotIndex];
        }

        public bool IsOccupied(int slotIndex) => GetUnit(slotIndex) != null;

        public int IndexOf(CollectorUnit unit)
        {
            if (unit == null || _occupants == null)
                return -1;

            for (int i = 0; i < _occupants.Length; i++)
            {
                if (_occupants[i] == unit)
                    return i;
            }

            return -1;
        }

        public bool Contains(CollectorUnit unit) => IndexOf(unit) >= 0;

        /// <summary>Returns the first empty slot index, or -1 when full.</summary>
        public int FindFirstEmptySlot()
        {
            if (_occupants == null)
                return -1;

            for (int i = 0; i < _occupants.Length; i++)
            {
                if (_occupants[i] == null)
                    return i;
            }

            return -1;
        }

        /// <summary>Places unit in the first empty slot. Returns assigned index, or -1 on failure.</summary>
        public int TryAssignFirstEmpty(CollectorUnit unit)
        {
            int slot = FindFirstEmptySlot();
            return TryAssign(slot, unit) ? slot : -1;
        }

        /// <summary>
        /// Places unit in a specific slot. Caller should use <see cref="TryAssignFirstEmpty"/> after lap complete.
        /// </summary>
        public bool TryAssign(int slotIndex, CollectorUnit unit)
        {
            if (unit == null || !IsValidSlot(slotIndex))
                return false;

            EnsureInitialized();
            if (_occupants[slotIndex] != null)
                return false;

            if (Contains(unit))
                return false;

            Transform anchor = GetAnchor(slotIndex);
            _occupants[slotIndex] = unit;
            unit.ForceState(CollectorState.InQueueSlot);
            unit.transform.SetParent(anchor, false);
            unit.SetWorldPosition(anchor.position);
            RefreshSlotTappable(slotIndex);
            return true;
        }

        /// <summary>
        /// Removes unit from slot after successful conveyor dispatch.
        /// Does not release to pool — ownership moves to ConveyorPathManager.
        /// </summary>
        public CollectorUnit TryRemove(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || _occupants == null)
                return null;

            CollectorUnit unit = _occupants[slotIndex];
            if (unit == null)
                return null;

            _occupants[slotIndex] = null;
            unit.transform.SetParent(null, true);
            return unit;
        }

        public CollectorUnit TryRemove(CollectorUnit unit)
        {
            int index = IndexOf(unit);
            return index < 0 ? null : TryRemove(index);
        }

        public void RefreshLayout()
        {
            EnsureInitialized();
            for (int i = 0; i < _slotCount; i++)
            {
                CollectorUnit unit = _occupants[i];
                if (unit == null)
                    continue;

                Transform anchor = GetAnchor(i);
                unit.SetWorldPosition(anchor.position);
            }
        }

        /// <summary>Every occupied slot keeps a pickable collider.</summary>
        public void RefreshTappable()
        {
            EnsureInitialized();
            for (int i = 0; i < _slotCount; i++)
                RefreshSlotTappable(i);
        }

        public void Clear()
        {
            if (_occupants == null)
                return;

            if (PoolManager.HasInstance)
            {
                for (int i = 0; i < _occupants.Length; i++)
                {
                    if (_occupants[i] != null)
                        PoolManager.Instance.ReleaseCollector(_occupants[i]);
                }
            }

            for (int i = 0; i < _occupants.Length; i++)
                _occupants[i] = null;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            _slotCount = Mathf.Max(1, _slotCount);
            if (_occupants == null || _occupants.Length != _slotCount)
            {
                var next = new CollectorUnit[_slotCount];
                if (_occupants != null)
                {
                    int copy = Mathf.Min(_occupants.Length, next.Length);
                    for (int i = 0; i < copy; i++)
                        next[i] = _occupants[i];
                }

                _occupants = next;
            }

            EnsureAnchors();
        }

        private void EnsureAnchors()
        {
            if (_slotsRoot == null)
            {
                _slotsRoot = new GameObject("QueueSlotsRoot").transform;
                _slotsRoot.SetParent(transform, false);
            }

            if (_slotAnchors == null || _slotAnchors.Length != _slotCount)
            {
                var anchors = new Transform[_slotCount];
                for (int i = 0; i < _slotCount; i++)
                {
                    if (_slotAnchors != null && i < _slotAnchors.Length && _slotAnchors[i] != null)
                        anchors[i] = _slotAnchors[i];
                }

                _slotAnchors = anchors;
            }

            for (int i = 0; i < _slotCount; i++)
            {
                if (_slotAnchors[i] == null)
                {
                    var go = new GameObject($"QueueSlot_{i}");
                    go.transform.SetParent(_slotsRoot, false);
                    _slotAnchors[i] = go.transform;
                }
                else if (_slotAnchors[i].parent != _slotsRoot)
                {
                    _slotAnchors[i].SetParent(_slotsRoot, false);
                }

                Vector2 local = GetSlotLocalOffset(i);
                _slotAnchors[i].localPosition = new Vector3(local.x, local.y, 0f);
            }
        }

        private Vector2 GetSlotLocalOffset(int slotIndex)
        {
            Vector2 dir = _layoutDirection.sqrMagnitude > 0.0001f
                ? _layoutDirection.normalized
                : Vector2.right;
            float centerOffset = (Mathf.Max(1, _slotCount) - 1) * 0.5f;
            float offset = (slotIndex - centerOffset) * _slotSpacing;
            return dir * offset;
        }

        private Vector3 GetLayoutOrigin()
        {
            if (_slotsRoot != null)
                return _slotsRoot.position;
            return transform.position;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _slotCount = Mathf.Max(1, _slotCount);
            if (Application.isPlaying)
                return;

            // Delay so we can create persistent child anchors in Edit Mode safely.
            UnityEditor.EditorApplication.delayCall -= EditorRebuildAnchorsDelayed;
            UnityEditor.EditorApplication.delayCall += EditorRebuildAnchorsDelayed;
        }

        private void EditorRebuildAnchorsDelayed()
        {
            if (this == null)
                return;

            EnsureAnchors();
            UnityEditor.EditorUtility.SetDirty(this);
            if (_slotsRoot != null)
                UnityEditor.EditorUtility.SetDirty(_slotsRoot.gameObject);
        }

        [ContextMenu("Rebuild Slot Anchors")]
        private void ContextRebuildAnchors()
        {
            EnsureAnchors();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private Transform GetAnchor(int slotIndex)
        {
            EnsureAnchors();
            return _slotAnchors[slotIndex];
        }

        private void RefreshSlotTappable(int slotIndex)
        {
            CollectorUnit unit = GetUnit(slotIndex);
            if (unit == null)
                return;

            Collider2D col = unit.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;
        }

        private void OnDrawGizmos()
        {
            int count = Mathf.Max(1, _slotCount);
            Vector3 origin = GetLayoutOrigin();

            for (int i = 0; i < count; i++)
            {
                Vector3 pos;
                if (_slotAnchors != null && i < _slotAnchors.Length && _slotAnchors[i] != null)
                    pos = _slotAnchors[i].position;
                else
                {
                    Vector2 local = GetSlotLocalOffset(i);
                    pos = origin + new Vector3(local.x, local.y, 0f);
                }

                bool occupied = _occupants != null && i < _occupants.Length && _occupants[i] != null;
                Gizmos.color = occupied
                    ? new Color(0.2f, 0.9f, 0.4f, 0.9f)
                    : new Color(1f, 0.85f, 0.15f, 0.85f);
                Gizmos.DrawWireSphere(pos, 0.28f);
                Gizmos.DrawLine(origin, pos);
            }

            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(origin, Vector3.one * 0.2f);
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
