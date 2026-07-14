using System.Collections.Generic;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;

namespace PixelFlowClone.Queue
{
    /// <summary>
    /// Waiting stack with optional multi-column layout.
    /// Convention: array index 0 = tail, last index = nearest to conveyor.
    /// Front row (all units with depth &lt; ColumnCount) are tappable — e.g. with 2 columns, A|B both Front.
    /// </summary>
    public class WaitingSlotController : MonoBehaviour
    {
        [SerializeField] private Transform _stackRoot;
        [SerializeField] private float _slotSpacing = 1.1f;
        [SerializeField] private float _columnSpacing = 1.1f;
        [Tooltip("Number of parallel vertical columns (e.g. 2 = two hàng dọc side by side).")]
        [SerializeField] private int _columnCount = 2;
        [SerializeField] private Vector2 _stackDirection = Vector2.down;
        [SerializeField] private Vector2 _columnDirection = Vector2.right;

        private readonly List<CollectorUnit> _stack = new();

        public int Count => _stack.Count;
        public bool IsEmpty => _stack.Count == 0;
        public IReadOnlyList<CollectorUnit> Units => _stack;
        public int ColumnCount => Mathf.Max(1, _columnCount);

        /// <summary>Primary front (depth 0, left column). Prefer <see cref="IsFront"/> for tap checks.</summary>
        public CollectorUnit Front => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        /// <summary>How many units sit on the front row and may be tapped.</summary>
        public int FrontRowCount => Mathf.Min(ColumnCount, _stack.Count);

        public void SpawnFromLevel(LevelDataSO level)
        {
            Clear();

            if (level == null || level.WaitingQueue == null || level.WaitingQueue.Length == 0)
            {
                Debug.LogWarning("[WaitingSlotController] No WaitingQueue entries to spawn.");
                return;
            }

            if (!PoolManager.HasInstance)
            {
                Debug.LogError("[WaitingSlotController] PoolManager missing.");
                return;
            }

            EnsureStackRoot();

            // Preserve array order: index 0 stays at back of List, last stays nearest front.
            for (int i = 0; i < level.WaitingQueue.Length; i++)
            {
                CollectorSpawnEntry entry = level.WaitingQueue[i];
                CollectorUnit unit = PoolManager.Instance.GetCollector();
                unit.Initialize(entry.Color, entry.InitialCapacity);
                unit.ForceState(CollectorState.InWaitingStack);
                unit.transform.SetParent(_stackRoot, false);
                _stack.Add(unit);
            }

            RefreshLayout();
            RefreshFrontTappable();
        }

        public CollectorUnit PeekFront() => Front;

        /// <summary>Fills buffer with every unit currently on the front row (left→right by depth).</summary>
        public void GetFronts(List<CollectorUnit> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();
            int n = FrontRowCount;
            for (int depth = 0; depth < n; depth++)
                buffer.Add(_stack[_stack.Count - 1 - depth]);
        }

        /// <summary>
        /// Removes the primary front (depth 0). Prefer <see cref="TryPop"/> when the player taps a specific Front.
        /// </summary>
        public CollectorUnit PopFront()
        {
            return TryPop(Front);
        }

        /// <summary>
        /// Removes a front-row unit after successful conveyor dispatch.
        /// Does not release to pool — ownership moves to ConveyorPathManager.
        /// </summary>
        public CollectorUnit TryPop(CollectorUnit unit)
        {
            if (!IsFront(unit))
                return null;

            _stack.Remove(unit);
            unit.transform.SetParent(null, true);

            RefreshLayout();
            RefreshFrontTappable();
            return unit;
        }

        public bool Contains(CollectorUnit unit) => unit != null && _stack.Contains(unit);

        /// <summary>True if unit is on the front row (any column) and may be tapped.</summary>
        public bool IsFront(CollectorUnit unit)
        {
            if (unit == null)
                return false;

            int index = _stack.IndexOf(unit);
            if (index < 0)
                return false;

            return DepthFromFront(index) < ColumnCount;
        }

        public void Clear()
        {
            if (PoolManager.HasInstance)
            {
                for (int i = 0; i < _stack.Count; i++)
                {
                    if (_stack[i] != null)
                        PoolManager.Instance.ReleaseCollector(_stack[i]);
                }
            }

            _stack.Clear();
        }

        public void RefreshLayout()
        {
            EnsureStackRoot();
            Vector2 down = _stackDirection.sqrMagnitude > 0.0001f
                ? _stackDirection.normalized
                : Vector2.down;
            Vector2 across = _columnDirection.sqrMagnitude > 0.0001f
                ? _columnDirection.normalized
                : Vector2.right;
            int columns = ColumnCount;

            // Front row (depth 0..columns-1) is fully tappable. Example columns=2:
            //   A | B   ← both Front
            //   C | D
            for (int i = 0; i < _stack.Count; i++)
            {
                CollectorUnit unit = _stack[i];
                if (unit == null)
                    continue;

                int depthFromFront = DepthFromFront(i);
                int column = depthFromFront % columns;
                int row = depthFromFront / columns;
                Vector2 local = across * (column * _columnSpacing) + down * (row * _slotSpacing);
                unit.SetWorldPosition((Vector2)_stackRoot.position + local);
            }
        }

        /// <summary>
        /// Every front-row unit keeps a pickable collider; deeper units cannot be tapped.
        /// </summary>
        public void RefreshFrontTappable()
        {
            int columns = ColumnCount;
            for (int i = 0; i < _stack.Count; i++)
            {
                CollectorUnit unit = _stack[i];
                if (unit == null)
                    continue;

                bool isFrontRow = DepthFromFront(i) < columns;
                Collider2D col = unit.GetComponent<Collider2D>();
                if (col != null)
                    col.enabled = isFrontRow;
            }
        }

        private int DepthFromFront(int index) => _stack.Count - 1 - index;

        private void EnsureStackRoot()
        {
            if (_stackRoot == null)
            {
                _stackRoot = new GameObject("WaitingStackRoot").transform;
                _stackRoot.SetParent(transform, false);
            }
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
