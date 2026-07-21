using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using UnityEngine;
using UnityEngine.Pool;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Global object pool for CollectorUnit and PixelBlock. Entities are never destroyed
    /// during gameplay; they are recycled via SetActive(false) + reset hooks.
    /// Released instances are always reparented under this persistent manager so scene
    /// unloads cannot destroy pooled objects while the pool still holds references.
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Prefabs")]
        [SerializeField] private CollectorUnit _collectorPrefab;
        [SerializeField] private PixelBlock _blockPrefab;

        [Header("Pool Roots (optional)")]
        [SerializeField] private Transform _collectorPoolRoot;
        [SerializeField] private Transform _blockPoolRoot;

        [Header("Default Sizes")]
        [SerializeField] private int _collectorDefaultCapacity = 20;
        [SerializeField] private int _collectorMaxSize = 50;
        [SerializeField] private int _blockDefaultCapacity = 100;
        [SerializeField] private int _blockMaxSize = 300;

        private ObjectPool<CollectorUnit> _collectorPool;
        private ObjectPool<PixelBlock> _blockPool;

        public int CollectorCountAll => _collectorPool?.CountAll ?? 0;
        public int CollectorCountActive => _collectorPool?.CountActive ?? 0;
        public int BlockCountAll => _blockPool?.CountAll ?? 0;
        public int BlockCountActive => _blockPool?.CountActive ?? 0;

        protected override void OnSingletonAwake()
        {
            MakePersistent();
            EnsurePoolRoots();
            BuildPools();
        }

        private void EnsurePoolRoots()
        {
            if (_collectorPoolRoot == null)
                _collectorPoolRoot = new GameObject("CollectorPool").transform;
            if (_blockPoolRoot == null)
                _blockPoolRoot = new GameObject("BlockPool").transform;

            // Keep world pose when parenting — scene-assigned roots often sit at a custom offset.
            if (_collectorPoolRoot.parent != transform)
                _collectorPoolRoot.SetParent(transform, true);
            if (_blockPoolRoot.parent != transform)
                _blockPoolRoot.SetParent(transform, true);
        }

        private void BuildPools()
        {
            _collectorPool = new ObjectPool<CollectorUnit>(
                createFunc: CreateCollector,
                actionOnGet: unit =>
                {
                    if (unit == null)
                        return;
                    unit.gameObject.SetActive(true);
                    unit.OnSpawnFromPool();
                },
                actionOnRelease: unit =>
                {
                    if (unit == null)
                        return;
                    unit.ResetFromPool();
                    unit.transform.SetParent(_collectorPoolRoot, false);
                    unit.gameObject.SetActive(false);
                },
                actionOnDestroy: unit =>
                {
                    if (unit != null)
                        Destroy(unit.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: _collectorDefaultCapacity,
                maxSize: _collectorMaxSize);

            _blockPool = new ObjectPool<PixelBlock>(
                createFunc: CreateBlock,
                actionOnGet: block =>
                {
                    if (block == null)
                        return;
                    block.gameObject.SetActive(true);
                },
                actionOnRelease: block =>
                {
                    if (block == null)
                        return;
                    block.ResetFromPool();
                    block.transform.SetParent(_blockPoolRoot, false);
                    block.gameObject.SetActive(false);
                },
                actionOnDestroy: block =>
                {
                    if (block != null)
                        Destroy(block.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: _blockDefaultCapacity,
                maxSize: _blockMaxSize);
        }

        /// <summary>
        /// Drops any pool entries (including destroyed scene leftovers) and rebuilds empty pools.
        /// Call before spawning into a fresh gameplay scene after an unload.
        /// </summary>
        public void ResetPools()
        {
            EnsurePoolRoots();

            if (_collectorPool != null)
            {
                _collectorPool.Clear();
                _collectorPool = null;
            }

            if (_blockPool != null)
            {
                _blockPool.Clear();
                _blockPool = null;
            }

            // Destroy any leftover children under pool roots (orphans not tracked by ObjectPool).
            DestroyChildren(_collectorPoolRoot);
            DestroyChildren(_blockPoolRoot);

            BuildPools();
            Debug.Log("[PoolManager] Pools reset.");
        }

        private static void DestroyChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private CollectorUnit CreateCollector()
        {
            var unit = Instantiate(_collectorPrefab, _collectorPoolRoot);
            unit.gameObject.SetActive(false);
            return unit;
        }

        private PixelBlock CreateBlock()
        {
            var block = Instantiate(_blockPrefab, _blockPoolRoot);
            block.gameObject.SetActive(false);
            return block;
        }

        /// <summary>
        /// Pre-instantiates enough entities for the given level so no runtime allocations occur mid-play.
        /// </summary>
        public void Prewarm(LevelDataSO level, GameConfigSO config)
        {
            if (level == null || config == null)
            {
                Debug.LogWarning("[PoolManager] Prewarm called with null level or config.");
                return;
            }

            if (_blockPool == null || _collectorPool == null)
                BuildPools();

            int blockCount = level.CountNonEmptyBlocks();
            int waiting = level.CountWaitingCollectors();
            int collectorCount = waiting + config.MaxConveyorUnits + config.MaxQueueSlots;

            PrewarmPool(_blockPool, blockCount);
            PrewarmPool(_collectorPool, collectorCount);
        }

        private static void PrewarmPool<T>(ObjectPool<T> pool, int targetCount) where T : Object
        {
            if (pool == null || targetCount <= 0)
                return;

            var buffer = new List<T>(targetCount);
            while (pool.CountAll < targetCount && buffer.Count < targetCount)
            {
                T item = pool.Get();
                if (item == null)
                    break;
                buffer.Add(item);
            }

            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] != null)
                    pool.Release(buffer[i]);
            }
        }

        public CollectorUnit GetCollector()
        {
            if (_collectorPool == null)
                BuildPools();

            for (int attempt = 0; attempt < 32; attempt++)
            {
                CollectorUnit unit = _collectorPool.Get();
                if (unit != null)
                    return unit;
            }

            Debug.LogWarning("[PoolManager] Stale CollectorUnit entries in pool — rebuilding.");
            ResetPools();
            return _collectorPool.Get();
        }

        public void ReleaseCollector(CollectorUnit unit)
        {
            if (unit == null || _collectorPool == null)
                return;

            _collectorPool.Release(unit);
        }

        public PixelBlock GetPixelBlock()
        {
            if (_blockPool == null)
                BuildPools();

            for (int attempt = 0; attempt < 32; attempt++)
            {
                PixelBlock block = _blockPool.Get();
                if (block != null)
                    return block;
            }

            Debug.LogWarning("[PoolManager] Stale PixelBlock entries in pool — rebuilding.");
            ResetPools();
            return _blockPool.Get();
        }

        public void ReleasePixelBlock(PixelBlock block)
        {
            if (block == null || _blockPool == null)
                return;

            _blockPool.Release(block);
        }
    }
}
