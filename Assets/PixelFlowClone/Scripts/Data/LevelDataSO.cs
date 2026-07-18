using PixelFlowClone.Conveyor;
using UnityEngine;

namespace PixelFlowClone.Data
{
    [CreateAssetMenu(fileName = "Level_", menuName = "PixelFlowClone/Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        [Header("Identity")]
        public int LevelId;
        public string LevelName;

        [Header("Pixel Grid")]
        public Vector2Int GridSize = new(5, 5);
        public ColorId[] BlockMatrix;
        public Vector2 CellSpacing = new(1f, 1f);
        public Vector2 GridOrigin;

        [Header("Collectors")]
        [Tooltip("Independent waiting columns. Inside each column: index 0 = back, last = front.")]
        public CollectorSpawnColumn[] WaitingColumns;

        [Header("Layout")]
        [Tooltip("World units from conveyor bottom edge down to the queue row. ≤0 uses default.")]
        public float QueueGapBelowPath;

        [Tooltip("World units from queue row down to waiting front. ≤0 uses default.")]
        public float WaitingGapBelowQueue;

        [Tooltip("Spacing between collectors in the horizontal queue. ≤0 uses scene default (≈1.1).")]
        public float QueueUnitSpacing;

        [Tooltip("Spacing between collectors in a waiting column (vertical). ≤0 uses scene default (≈1.1).")]
        public float WaitingUnitSpacing;

        [Tooltip("Spacing between waiting columns (horizontal). ≤0 uses scene default (≈1.1).")]
        public float WaitingColumnSpacing;

        [Header("Conveyor")]
        public ConveyorPathSO PathReference;

        public int ExpectedCellCount => GridSize.x * GridSize.y;

        private void OnValidate()
        {
            if (GridSize.x < 1 || GridSize.y < 1)
            {
                Debug.LogWarning($"[{name}] GridSize must be at least 1x1.", this);
                return;
            }

            int expected = ExpectedCellCount;
            if (BlockMatrix == null || BlockMatrix.Length != expected)
            {
                Debug.LogWarning(
                    $"[{name}] BlockMatrix length ({BlockMatrix?.Length ?? 0}) must equal GridSize.x * GridSize.y ({expected}).",
                    this);
            }

            if (WaitingColumns == null || WaitingColumns.Length == 0)
            {
                Debug.LogWarning($"[{name}] WaitingColumns should contain at least one column.", this);
                return;
            }

            if (CountWaitingCollectors() == 0)
                Debug.LogWarning($"[{name}] WaitingColumns should contain at least one collector.", this);
        }

        public ColorId GetBlockAt(int x, int y)
        {
            if (x < 0 || x >= GridSize.x || y < 0 || y >= GridSize.y)
                return ColorId.None;

            int index = y * GridSize.x + x;
            if (BlockMatrix == null || index >= BlockMatrix.Length)
                return ColorId.None;

            return BlockMatrix[index];
        }

        public int CountNonEmptyBlocks()
        {
            if (BlockMatrix == null)
                return 0;

            int count = 0;
            foreach (ColorId cell in BlockMatrix)
            {
                if (cell != ColorId.None)
                    count++;
            }

            return count;
        }

        public int CountWaitingCollectors()
        {
            if (WaitingColumns == null)
                return 0;

            int count = 0;
            for (int i = 0; i < WaitingColumns.Length; i++)
                count += WaitingColumns[i].Count;

            return count;
        }
    }
}
