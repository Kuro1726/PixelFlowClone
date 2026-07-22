using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Spawns and tracks the central pixel-block grid from a LevelDataSO.
    /// The playfield frame is fixed in the scene; GridSize only changes cell size/scale
    /// so blocks always fit inside that frame (inside the authored conveyor loop).
    /// </summary>
    public class GridManager : Singleton<GridManager>
    {
        private const int PendingConsumedBlockPoolSize = 128;
        [Header("Scene")]
        [SerializeField] private Transform _gridRoot;

        [Header("Fixed Playfield")]
        [Tooltip("World-space center of the block frame (inside the conveyor loop).")]
        [SerializeField] private Vector2 _playfieldCenter = Vector2.zero;
        [Tooltip("World-space size of the block frame. Blocks always fit inside this rect.")]
        [SerializeField] private Vector2 _playfieldSize = new(5f, 5f);
        [Tooltip("Prefab block scale 1 corresponds to this cell size (world units).")]
        [SerializeField] private float _referenceCellSize = 1f;
        [Tooltip("Extra scale so fills overlap slightly and hide inner outlines (Pixel Flow pack).")]
        [SerializeField] [Range(1f, 1.25f)] private float _blockPackScale = 1.14f;

        private readonly Dictionary<Vector2Int, PixelBlock> _blocks = new();
        private readonly List<PendingConsumedBlock> _pendingConsumedBlocks = new();
        private readonly Queue<PendingConsumedBlock> _pendingConsumedBlockPool = new();
        private LevelDataSO _currentLevel;
        private Vector2 _runtimeOrigin;
        private float _runtimeCellSize = 1f;
        private float _runtimeBlockScale = 1f;

        public IReadOnlyDictionary<Vector2Int, PixelBlock> Blocks => _blocks;

        public LevelDataSO CurrentLevel => _currentLevel;

        protected override void OnSingletonAwake()
        {
            EnsurePendingConsumedBlockPool(PendingConsumedBlockPoolSize);
        }

        public Vector2 PlayfieldCenter => _playfieldCenter;

        public Vector2 PlayfieldSize => _playfieldSize;

        public float RuntimeCellSize => _runtimeCellSize;

        public Vector2 RuntimeOrigin => _runtimeOrigin;

        /// <summary>
        /// Number of unconsumed blocks still tracked on the grid. Used for win checks (== 0).
        /// </summary>
        public int RemainingBlocks => _blocks.Count;

        /// <summary>
        /// World-space center of the active playfield (raycast inward target).
        /// </summary>
        public Vector2 GridCenterWorld =>
            _gridRoot != null
                ? (Vector2)_gridRoot.TransformPoint(_playfieldCenter)
                : _playfieldCenter;

        /// <summary>
        /// Instantiates one PixelBlock per non-empty cell, fitted into the fixed playfield frame.
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
            RecalculateRuntimeLayout(level);

            float scale = _runtimeBlockScale * Mathf.Max(1f, _blockPackScale);

            for (int y = 0; y < level.GridSize.y; y++)
            {
                for (int x = 0; x < level.GridSize.x; x++)
                {
                    ColorId color = level.GetBlockAt(x, y);
                    if (color == ColorId.None)
                        continue;

                    var gridPos = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorld(gridPos);

                    PixelBlock block = PoolManager.Instance.GetPixelBlock();
                    block.name = $"PixelBlock_{x}_{y}_i{y * level.GridSize.x + x}_{color}";
                    block.transform.SetParent(_gridRoot, true);
                    block.Initialize(color, gridPos, worldPos, scale);

                    _blocks[gridPos] = block;
                }
            }

            Debug.Log(
                $"[GridManager] Spawned grid {level.GridSize.x}x{level.GridSize.y} " +
                $"cell={_runtimeCellSize:0.###} scale={scale:0.###} into playfield {_playfieldSize}");
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

                for (int i = 0; i < _pendingConsumedBlocks.Count; i++)
                {
                    PixelBlock pendingBlock = _pendingConsumedBlocks[i].Block;
                    if (pendingBlock != null)
                        PoolManager.Instance.ReleasePixelBlock(pendingBlock);
                }
            }

            _blocks.Clear();
            for (int i = 0; i < _pendingConsumedBlocks.Count; i++)
                ReleasePendingConsumedBlock(_pendingConsumedBlocks[i]);

            _pendingConsumedBlocks.Clear();
        }

        /// <summary>
        /// Consumes the block at <paramref name="gridPosition"/> when its color matches.
        /// Returns false if no block exists, colors differ, or the block was already consumed.
        /// </summary>
        public bool TryConsumeBlock(ColorId color, Vector2Int gridPosition)
        {
            return TryConsumeBlock(color, gridPosition, 0f);
        }

        /// <summary>
        /// Logically consumes immediately, but can keep the block visual alive until a
        /// cosmetic collector shot reaches it. Collision is disabled during the delay.
        /// </summary>
        public bool TryConsumeBlock(ColorId color, Vector2Int gridPosition, float visualDelay)
        {
            if (!_blocks.TryGetValue(gridPosition, out PixelBlock block) || block == null)
                return false;

            if (block.IsConsumed || block.Color != color)
                return false;

            block.Consume();
            Vector3 worldPosition = block.transform.position;
            ColorId consumedColor = block.Color;
            _blocks.Remove(gridPosition);

            visualDelay = Mathf.Max(0f, visualDelay);
            if (visualDelay <= 0f)
                FinalizeConsumedBlock(block, worldPosition, consumedColor);
            else
            {
                PendingConsumedBlock pending = RentPendingConsumedBlock();
                pending.Configure(
                    block,
                    worldPosition,
                    consumedColor,
                    visualDelay,
                    ResolveBlockHitPunchScale(),
                    ResolveBlockHitPunchDuration(),
                    ResolveBlockHitShrinkDuration());
                _pendingConsumedBlocks.Add(pending);
            }

            return true;
        }

        private void Update()
        {
            if (_pendingConsumedBlocks.Count == 0)
                return;

            float deltaTime = Time.deltaTime;
            for (int i = _pendingConsumedBlocks.Count - 1; i >= 0; i--)
            {
                PendingConsumedBlock pending = _pendingConsumedBlocks[i];
                if (TickPendingConsumedBlock(pending, deltaTime))
                {
                    _pendingConsumedBlocks.RemoveAt(i);
                    FinalizeConsumedBlock(pending.Block, pending.WorldPosition, pending.ConsumedColor);
                    ReleasePendingConsumedBlock(pending);
                }
            }
        }
        private void EnsurePendingConsumedBlockPool(int targetAvailable)
        {
            while (_pendingConsumedBlockPool.Count < targetAvailable)
                _pendingConsumedBlockPool.Enqueue(new PendingConsumedBlock());
        }

        private PendingConsumedBlock RentPendingConsumedBlock()
        {
            if (_pendingConsumedBlockPool.Count == 0)
                EnsurePendingConsumedBlockPool(PendingConsumedBlockPoolSize);

            return _pendingConsumedBlockPool.Dequeue();
        }

        private void ReleasePendingConsumedBlock(PendingConsumedBlock pending)
        {
            if (pending == null)
                return;

            pending.Reset();
            _pendingConsumedBlockPool.Enqueue(pending);
        }


        private static bool TickPendingConsumedBlock(PendingConsumedBlock pending, float deltaTime)
        {
            if (pending.Block == null)
                return true;

            pending.Elapsed += deltaTime;
            if (pending.Elapsed < pending.Delay)
                return false;

            float animationTime = pending.Elapsed - pending.Delay;
            if (animationTime <= pending.PunchDuration)
            {
                float t = Mathf.Clamp01(animationTime / pending.PunchDuration);
                pending.Block.transform.localScale = Vector3.LerpUnclamped(
                    pending.RestScale,
                    pending.PunchScale,
                    Mathf.SmoothStep(0f, 1f, t));
                return false;
            }

            float shrinkTime = animationTime - pending.PunchDuration;
            if (shrinkTime <= pending.ShrinkDuration)
            {
                float t = Mathf.Clamp01(shrinkTime / pending.ShrinkDuration);
                pending.Block.transform.localScale = Vector3.LerpUnclamped(
                    pending.PunchScale,
                    Vector3.zero,
                    t * t);
                return false;
            }

            pending.Block.transform.localScale = Vector3.zero;
            return true;
        }

        private static void FinalizeConsumedBlock(
            PixelBlock block,
            Vector3 worldPosition,
            ColorId consumedColor)
        {
            if (block != null && PoolManager.Instance != null)
                PoolManager.Instance.ReleasePixelBlock(block);

            GameEvents.RaiseBlockConsumed(worldPosition, consumedColor);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            Vector2 local = _runtimeOrigin + new Vector2(
                gridPos.x * _runtimeCellSize,
                gridPos.y * _runtimeCellSize);
            return GridLocalToWorld(new Vector3(local.x, local.y, 0f));
        }

        /// <summary>Legacy helper — prefers runtime playfield layout when a level is active.</summary>
        public Vector3 GridToWorld(LevelDataSO level, Vector2Int gridPos)
        {
            if (_currentLevel == level && _runtimeCellSize > 0.0001f)
                return GridToWorld(gridPos);

            if (level == null)
                return Vector3.zero;

            float localX = level.GridOrigin.x + gridPos.x * level.CellSpacing.x;
            float localY = level.GridOrigin.y + gridPos.y * level.CellSpacing.y;
            return GridLocalToWorld(new Vector3(localX, localY, 0f));
        }

        public static Vector3 GridToWorldStatic(LevelDataSO level, Vector2Int gridPos)
        {
            float worldX = level.GridOrigin.x + gridPos.x * level.CellSpacing.x;
            float worldY = level.GridOrigin.y + gridPos.y * level.CellSpacing.y;
            return new Vector3(worldX, worldY, 0f);
        }
        private void EnsureGridRoot()
        {
            if (_gridRoot != null)
                return;

            GameObject root = new GameObject("GridRoot");
            root.transform.SetParent(transform, false);
            _gridRoot = root.transform;
        }


        private void RecalculateRuntimeLayout(LevelDataSO level)
        {
            int cols = Mathf.Max(1, level.GridSize.x);
            int rows = Mathf.Max(1, level.GridSize.y);
            float frameW = Mathf.Max(0.01f, _playfieldSize.x);
            float frameH = Mathf.Max(0.01f, _playfieldSize.y);

            _runtimeCellSize = Mathf.Min(frameW / cols, frameH / rows);
            float usedW = cols * _runtimeCellSize;
            float usedH = rows * _runtimeCellSize;

            // First cell center: bottom-left of the used rect inside the playfield frame.
            _runtimeOrigin = _playfieldCenter - new Vector2(usedW, usedH) * 0.5f
                             + new Vector2(_runtimeCellSize, _runtimeCellSize) * 0.5f;

            float reference = Mathf.Max(0.01f, _referenceCellSize);
            _runtimeBlockScale = _runtimeCellSize / reference;
        }

        private Vector3 GridLocalToWorld(Vector3 local)
        {
            return _gridRoot != null ? _gridRoot.TransformPoint(local) : local;
        }

        private Vector3 GridWorldToLocal(Vector3 world)
        {
            return _gridRoot != null ? _gridRoot.InverseTransformPoint(world) : world;
        }

        /// <summary>
        /// Discrete lane index along the belt: column X when moving horizontally, row Y when moving vertically.
        /// </summary>
        public int GetLaneIndex(Vector2 worldPos, bool movingVertically)
        {
            if (_runtimeCellSize < 0.0001f)
                return 0;

            Vector3 local = GridWorldToLocal(worldPos);
            if (movingVertically)
                return Mathf.RoundToInt((local.y - _runtimeOrigin.y) / _runtimeCellSize);

            return Mathf.RoundToInt((local.x - _runtimeOrigin.x) / _runtimeCellSize);
        }

        /// <summary>World position used for a lane raycast (snapped to that lane center on the move axis).</summary>
        public Vector2 GetLaneRayOrigin(Vector2 worldPos, int lane, bool movingVertically)
        {
            Vector3 local = GridWorldToLocal(worldPos);
            if (_runtimeCellSize > 0.0001f)
            {
                if (movingVertically)
                    local.y = _runtimeOrigin.y + lane * _runtimeCellSize;
                else
                    local.x = _runtimeOrigin.x + lane * _runtimeCellSize;
            }

            Vector3 world = GridLocalToWorld(local);
            return new Vector2(world.x, world.y);
        }

        private float ResolveBlockHitPunchScale()
        {
            GameConfigSO config = CollectorFlowCoordinator.HasInstance
                ? CollectorFlowCoordinator.Instance.Config
                : null;
            return config != null ? config.BlockHitPunchScale : 1.15f;
        }

        private float ResolveBlockHitPunchDuration()
        {
            GameConfigSO config = CollectorFlowCoordinator.HasInstance
                ? CollectorFlowCoordinator.Instance.Config
                : null;
            return config != null ? config.BlockHitPunchDuration : 0.07f;
        }

        private float ResolveBlockHitShrinkDuration()
        {
            GameConfigSO config = CollectorFlowCoordinator.HasInstance
                ? CollectorFlowCoordinator.Instance.Config
                : null;
            return config != null ? config.BlockHitShrinkDuration : 0.16f;
        }

        private sealed class PendingConsumedBlock
        {
            public PixelBlock Block { get; private set; }
            public Vector3 WorldPosition { get; private set; }
            public ColorId ConsumedColor { get; private set; }
            public float Delay { get; private set; }
            public float PunchDuration { get; private set; }
            public float ShrinkDuration { get; private set; }
            public Vector3 RestScale { get; private set; }
            public Vector3 PunchScale { get; private set; }
            public float Elapsed { get; set; }

            public void Configure(
                PixelBlock block,
                Vector3 worldPosition,
                ColorId consumedColor,
                float delay,
                float punchMultiplier,
                float punchDuration,
                float shrinkDuration)
            {
                Block = block;
                WorldPosition = worldPosition;
                ConsumedColor = consumedColor;
                Delay = delay;
                PunchDuration = Mathf.Max(0.01f, punchDuration);
                ShrinkDuration = Mathf.Max(0.01f, shrinkDuration);
                RestScale = block != null ? block.transform.localScale : Vector3.one;
                PunchScale = RestScale * punchMultiplier;
                Elapsed = 0f;
            }

            public void Reset()
            {
                Block = null;
                WorldPosition = default;
                ConsumedColor = default;
                Delay = 0f;
                PunchDuration = 0f;
                ShrinkDuration = 0f;
                RestScale = Vector3.one;
                PunchScale = Vector3.one;
                Elapsed = 0f;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = _gridRoot != null
                ? _gridRoot.TransformPoint(_playfieldCenter)
                : (Vector3)_playfieldCenter;
            Vector3 size = new(_playfieldSize.x, _playfieldSize.y, 0.01f);
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
