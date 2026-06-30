using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Spawns and tracks the central pixel-block grid from a LevelDataSO.
    /// Scene-scoped (not DontDestroyOnLoad). Block release / RemainingBlocks land in P1-18/P1-19.
    /// </summary>
    public class GridManager : Singleton<GridManager>
    {
        [SerializeField] private Transform _gridRoot;

        private readonly Dictionary<Vector2Int, PixelBlock> _blocks = new();
        private LevelDataSO _currentLevel;

        public IReadOnlyDictionary<Vector2Int, PixelBlock> Blocks => _blocks;

        /// <summary>
        /// Instantiates one PixelBlock per non-empty cell, positioned in world space
        /// using GridOrigin + (x * CellSpacing.x, y * CellSpacing.y).
        /// </summary>
        public void SpawnGrid(LevelDataSO level)
        {
            if (level == null)
            {
                Debug.LogWarning("[GridManager] SpawnGrid called with null level.");
                return;
            }

            if (PoolManager.Instance == null)
            {
                Debug.LogError("[GridManager] PoolManager.Instance is null. Ensure it exists before SpawnGrid.");
                return;
            }

            ClearGrid();
            _currentLevel = level;

            EnsureGridRoot();

            for (int y = 0; y < level.GridSize.y; y++)
            {
                for (int x = 0; x < level.GridSize.x; x++)
                {
                    ColorId color = level.GetBlockAt(x, y);
                    if (color == ColorId.None)
                        continue;

                    var gridPos = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorld(level, gridPos);

                    PixelBlock block = PoolManager.Instance.GetPixelBlock();
                    block.transform.SetParent(_gridRoot, false);
                    block.Initialize(color, gridPos, worldPos);

                    _blocks[gridPos] = block;
                }
            }
        }

        /// <summary>
        /// Releases all active blocks back to the pool and clears tracking.
        /// </summary>
        public void ClearGrid()
        {
            if (PoolManager.Instance != null)
            {
                foreach (PixelBlock block in _blocks.Values)
                {
                    if (block != null)
                        PoolManager.Instance.ReleasePixelBlock(block);
                }
            }

            _blocks.Clear();
        }

        public static Vector3 GridToWorld(LevelDataSO level, Vector2Int gridPos)
        {
            float worldX = level.GridOrigin.x + gridPos.x * level.CellSpacing.x;
            float worldY = level.GridOrigin.y + gridPos.y * level.CellSpacing.y;
            return new Vector3(worldX, worldY, 0f);
        }

        private void EnsureGridRoot()
        {
            if (_gridRoot == null)
            {
                _gridRoot = new GameObject("GridRoot").transform;
                _gridRoot.SetParent(transform, false);
            }
        }
    }
}
