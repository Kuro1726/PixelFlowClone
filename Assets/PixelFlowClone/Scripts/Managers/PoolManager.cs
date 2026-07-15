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
            DontDestroyOnLoad(gameObject);
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
                    unit.gameObject.SetActive(true);
                    unit.OnSpawnFromPool();
                },
                actionOnRelease: unit =>
                {
                    unit.ResetFromPool();
                    unit.gameObject.SetActive(false);
                },
                actionOnDestroy: unit =>
                {
                    if (unit != null) Destroy(unit.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: _collectorDefaultCapacity,
                maxSize: _collectorMaxSize);

            _blockPool = new ObjectPool<PixelBlock>(
                createFunc: CreateBlock,
                actionOnGet: block => block.gameObject.SetActive(true),
                actionOnRelease: block =>
                {
                    block.ResetFromPool();
                    block.gameObject.SetActive(false);
                },
                actionOnDestroy: block =>
                {
                    if (block != null) Destroy(block.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: _blockDefaultCapacity,
                maxSize: _blockMaxSize);
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

            int blockCount = level.CountNonEmptyBlocks();
            int waiting = level.WaitingQueue != null ? level.WaitingQueue.Length : 0;
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
                buffer.Add(pool.Get());

            for (int i = 0; i < buffer.Count; i++)
                pool.Release(buffer[i]);
        }

        public CollectorUnit GetCollector() => _collectorPool.Get();

        public void ReleaseCollector(CollectorUnit unit)
        {
            if (unit != null) _collectorPool.Release(unit);
        }

        public PixelBlock GetPixelBlock() => _blockPool.Get();

        public void ReleasePixelBlock(PixelBlock block)
        {
            if (block != null) _blockPool.Release(block);
        }
    }
}
