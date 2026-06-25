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
        [Tooltip("Index 0 = tail (back), last index = front (tappable).")]
        public CollectorSpawnEntry[] WaitingQueue;

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

            if (WaitingQueue == null || WaitingQueue.Length == 0)
                Debug.LogWarning($"[{name}] WaitingQueue should contain at least one collector.", this);
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
    }
}
