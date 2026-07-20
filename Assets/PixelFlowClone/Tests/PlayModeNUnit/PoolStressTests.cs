using System.Collections;
using NUnit.Framework;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixelFlowClone.Tests.PlayMode
{
    /// <summary>
    /// P4-14: 100 get/release cycles. Open Profiler → GC Alloc while this runs to confirm
    /// no Instantiate spike after the pool is warm (first gets may allocate up to pool size).
    /// </summary>
    public class PoolStressTests
    {
        private const int Cycles = 100;

        [UnityTest]
        public IEnumerator Pool_Stress_100GetReleaseCycles_LeavesZeroActive()
        {
            yield return PlayModeTestHelpers.LoadGameplayAndApplyLevel(0);

            Assume.That(PoolManager.HasInstance, Is.True);
            PoolManager pool = PoolManager.Instance;

            // Return level-spawned entities so the pool is fully inactive before stress.
            ConveyorPathManager.Instance?.ClearActiveUnits();
            QueueManager.Instance?.ClearAll();
            GridManager.Instance?.ClearGrid();
            yield return null;

            Assume.That(pool.CollectorCountActive, Is.EqualTo(0),
                "Collectors still active after clearing gameplay — cannot stress-test pool.");
            Assume.That(pool.BlockCountActive, Is.EqualTo(0),
                "Blocks still active after clearing gameplay — cannot stress-test pool.");

            // Warm / stabilize before measuring cycles.
            for (int i = 0; i < 8; i++)
            {
                CollectorUnit warmCollector = pool.GetCollector();
                PixelBlock warmBlock = pool.GetPixelBlock();
                Assume.That(warmCollector, Is.Not.Null);
                Assume.That(warmBlock, Is.Not.Null);

                warmCollector.Initialize(ColorId.Red, 1);
                warmBlock.Initialize(ColorId.Blue, Vector2Int.zero, Vector3.zero, 1f);
                pool.ReleaseCollector(warmCollector);
                pool.ReleasePixelBlock(warmBlock);
            }

            int collectorsBefore = pool.CollectorCountAll;
            int blocksBefore = pool.BlockCountAll;

            for (int i = 0; i < Cycles; i++)
            {
                CollectorUnit unit = pool.GetCollector();
                PixelBlock block = pool.GetPixelBlock();

                Assert.That(unit, Is.Not.Null);
                Assert.That(block, Is.Not.Null);

                unit.Initialize(ColorId.Green, 2);
                block.Initialize(ColorId.Yellow, new Vector2Int(i % 5, i % 5), Vector3.zero, 1f);

                pool.ReleaseCollector(unit);
                pool.ReleasePixelBlock(block);
            }

            Assert.That(pool.CollectorCountActive, Is.EqualTo(0));
            Assert.That(pool.BlockCountActive, Is.EqualTo(0));
            Assert.That(pool.CollectorCountAll, Is.EqualTo(collectorsBefore),
                "Collector pool grew during stress — unexpected Instantiate.");
            Assert.That(pool.BlockCountAll, Is.EqualTo(blocksBefore),
                "Block pool grew during stress — unexpected Instantiate.");

            Debug.Log(
                $"[P4-14] Pool stress OK: {Cycles} cycles, " +
                $"collectorsAll={pool.CollectorCountAll}, blocksAll={pool.BlockCountAll}. " +
                "Profiler: expect flat GC Alloc after warm-up (no per-cycle Instantiate).");

            yield return null;
        }
    }
}
