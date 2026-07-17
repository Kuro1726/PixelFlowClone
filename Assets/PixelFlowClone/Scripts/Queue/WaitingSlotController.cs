using System.Collections.Generic;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;

namespace PixelFlowClone.Queue
{
    /// <summary>
    /// Multi-column waiting stacks. Each LevelDataSO waiting column maps directly to one
    /// independent runtime column, so units never jump between columns.
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

        private readonly List<List<CollectorUnit>> _columns = new();
        private readonly List<CollectorUnit> _flatCache = new();
        private int _restoreColumn = -1;

        public int Count
        {
            get
            {
                int total = 0;
                for (int c = 0; c < _columns.Count; c++)
                    total += _columns[c].Count;
                return total;
            }
        }

        public bool IsEmpty => Count == 0;

        public IReadOnlyList<CollectorUnit> Units
        {
            get
            {
                RebuildFlatCache();
                return _flatCache;
            }
        }

        public int ColumnCount => Mathf.Max(1, _columnCount);

        /// <summary>Front of column 0 (primary). Prefer <see cref="IsFront"/> for tap checks.</summary>
        public CollectorUnit Front
        {
            get
            {
                EnsureColumnCount();
                List<CollectorUnit> col = _columns[0];
                return col.Count > 0 ? col[col.Count - 1] : null;
            }
        }

        /// <summary>How many columns currently have a tappable front unit.</summary>
        public int FrontRowCount
        {
            get
            {
                EnsureColumnCount();
                int n = 0;
                for (int c = 0; c < _columns.Count; c++)
                {
                    if (_columns[c].Count > 0)
                        n++;
                }

                return n;
            }
        }

        public void SpawnFromLevel(LevelDataSO level)
        {
            Clear();

            if (level == null ||
                level.WaitingColumns == null ||
                level.WaitingColumns.Length == 0 ||
                level.CountWaitingCollectors() == 0)
            {
                Debug.LogWarning("[WaitingSlotController] No WaitingColumns entries to spawn.");
                return;
            }

            if (!PoolManager.HasInstance)
            {
                Debug.LogError("[WaitingSlotController] PoolManager missing.");
                return;
            }

            EnsureStackRoot();
            _columnCount = level.WaitingColumns.Length;
            EnsureColumnCount();

            for (int columnIndex = 0; columnIndex < level.WaitingColumns.Length; columnIndex++)
            {
                CollectorSpawnColumn column = level.WaitingColumns[columnIndex];
                if (column.Collectors == null || column.Collectors.Length == 0)
                    continue;

                for (int i = 0; i < column.Collectors.Length; i++)
                {
                    CollectorSpawnEntry entry = column.Collectors[i];
                    CollectorUnit unit = PoolManager.Instance.GetCollector();
                    unit.Initialize(entry.Color, entry.InitialCapacity);
                    unit.ForceState(CollectorState.InWaitingStack);
                    unit.transform.SetParent(_stackRoot, false);

                    // Asset order is back → front, matching the runtime column list.
                    _columns[columnIndex].Add(unit);
                }
            }

            RefreshLayout();
            RefreshFrontTappable();
        }

        public CollectorUnit PeekFront() => Front;

        /// <summary>Fills buffer with every column-front unit (left→right).</summary>
        public void GetFronts(List<CollectorUnit> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();
            EnsureColumnCount();
            for (int c = 0; c < _columns.Count; c++)
            {
                List<CollectorUnit> col = _columns[c];
                if (col.Count > 0)
                    buffer.Add(col[col.Count - 1]);
            }
        }

        /// <summary>
        /// Removes the primary front (column 0). Prefer <see cref="TryPop"/> when tapping a specific front.
        /// </summary>
        public CollectorUnit PopFront()
        {
            return TryPop(Front);
        }

        /// <summary>
        /// Removes a column-front unit after successful conveyor dispatch.
        /// Only that column slides forward; other columns stay put.
        /// </summary>
        public CollectorUnit TryPop(CollectorUnit unit)
        {
            if (!TryFindFront(unit, out int column, out int indexInColumn))
                return null;

            _columns[column].RemoveAt(indexInColumn);
            _restoreColumn = column;
            unit.transform.SetParent(null, true);

            RefreshLayout();
            RefreshFrontTappable();
            return unit;
        }

        public bool Contains(CollectorUnit unit)
        {
            return unit != null && TryFind(unit, out _, out _);
        }

        /// <summary>True if unit is the front of its column and may be tapped.</summary>
        public bool IsFront(CollectorUnit unit)
        {
            return TryFindFront(unit, out _, out _);
        }

        public void Clear()
        {
            if (PoolManager.HasInstance)
            {
                for (int c = 0; c < _columns.Count; c++)
                {
                    List<CollectorUnit> col = _columns[c];
                    for (int i = 0; i < col.Count; i++)
                    {
                        if (col[i] != null)
                            PoolManager.Instance.ReleaseCollector(col[i]);
                    }
                }
            }

            for (int c = 0; c < _columns.Count; c++)
                _columns[c].Clear();

            _flatCache.Clear();
            _restoreColumn = -1;
        }

        public void RefreshLayout()
        {
            EnsureStackRoot();
            EnsureColumnCount();

            Vector2 down = _stackDirection.sqrMagnitude > 0.0001f
                ? _stackDirection.normalized
                : Vector2.down;
            Vector2 across = _columnDirection.sqrMagnitude > 0.0001f
                ? _columnDirection.normalized
                : Vector2.right;

            for (int c = 0; c < _columns.Count; c++)
            {
                List<CollectorUnit> col = _columns[c];
                for (int i = 0; i < col.Count; i++)
                {
                    CollectorUnit unit = col[i];
                    if (unit == null)
                        continue;

                    // i = 0 is back (furthest down); last index is front (row 0).
                    int row = col.Count - 1 - i;
                    Vector2 local = across * (c * _columnSpacing) + down * (row * _slotSpacing);
                    unit.SetWorldPosition((Vector2)_stackRoot.position + local);
                }
            }
        }

        /// <summary>
        /// Only each column's front unit keeps a pickable collider.
        /// </summary>
        public void RefreshFrontTappable()
        {
            EnsureColumnCount();
            for (int c = 0; c < _columns.Count; c++)
            {
                List<CollectorUnit> col = _columns[c];
                for (int i = 0; i < col.Count; i++)
                {
                    CollectorUnit unit = col[i];
                    if (unit == null)
                        continue;

                    bool isFront = i == col.Count - 1;
                    Collider2D collider = unit.GetComponent<Collider2D>();
                    if (collider != null)
                        collider.enabled = isFront;
                }
            }
        }

        /// <summary>
        /// Puts a unit back onto the column it was popped from after a failed conveyor dispatch.
        /// </summary>
        public void RestoreFront(CollectorUnit unit)
        {
            if (unit == null || Contains(unit))
                return;

            EnsureStackRoot();
            EnsureColumnCount();

            int col = _restoreColumn >= 0 && _restoreColumn < _columns.Count
                ? _restoreColumn
                : 0;

            _columns[col].Add(unit);
            _restoreColumn = -1;

            unit.ForceState(CollectorState.InWaitingStack);
            unit.transform.SetParent(_stackRoot, false);
            RefreshLayout();
            RefreshFrontTappable();
        }

        private bool TryFindFront(CollectorUnit unit, out int column, out int indexInColumn)
        {
            if (!TryFind(unit, out column, out indexInColumn))
                return false;

            return indexInColumn == _columns[column].Count - 1;
        }

        private bool TryFind(CollectorUnit unit, out int column, out int indexInColumn)
        {
            column = -1;
            indexInColumn = -1;
            if (unit == null)
                return false;

            EnsureColumnCount();
            for (int c = 0; c < _columns.Count; c++)
            {
                int index = _columns[c].IndexOf(unit);
                if (index < 0)
                    continue;

                column = c;
                indexInColumn = index;
                return true;
            }

            return false;
        }

        private void EnsureColumnCount()
        {
            int needed = ColumnCount;
            while (_columns.Count < needed)
                _columns.Add(new List<CollectorUnit>());

            // If inspector reduced column count after spawn, fold extras into last kept column.
            while (_columns.Count > needed)
            {
                List<CollectorUnit> extra = _columns[_columns.Count - 1];
                _columns.RemoveAt(_columns.Count - 1);
                if (extra.Count == 0)
                    continue;

                List<CollectorUnit> target = _columns[_columns.Count - 1];
                for (int i = 0; i < extra.Count; i++)
                    target.Insert(i, extra[i]);
            }
        }

        private void RebuildFlatCache()
        {
            _flatCache.Clear();
            EnsureColumnCount();

            // Back→front, left→right within each depth-ish order for debug/readout.
            int maxRows = 0;
            for (int c = 0; c < _columns.Count; c++)
                maxRows = Mathf.Max(maxRows, _columns[c].Count);

            for (int backOffset = maxRows - 1; backOffset >= 0; backOffset--)
            {
                for (int c = 0; c < _columns.Count; c++)
                {
                    List<CollectorUnit> col = _columns[c];
                    int index = col.Count - 1 - backOffset;
                    if (index >= 0)
                        _flatCache.Add(col[index]);
                }
            }
        }

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
