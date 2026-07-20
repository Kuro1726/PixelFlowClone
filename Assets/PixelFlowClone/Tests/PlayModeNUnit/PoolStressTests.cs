using System.Collections;
using NUnit.Framework;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PixelFlowClone.Tests.PlayMode
{
    /// <summary>
    /// P4-14: 100 get/release cycles against PoolManager.
    /// Explicitly loads SCN_Bootstrap — Play Mode Test Runner often enters Play on an empty
    /// scene ("No cameras rendering"), so PoolManager would never appear otherwise.
    /// </summary>
    public class PoolStressTests
    {
        private const int Cycles = 100;
        private const int YieldEvery = 10;
        private const float SceneLoadTimeoutSeconds = 30f;

        [UnityTest]
        public IEnumerator Pool_Stress_100GetReleaseCycles_LeavesZeroActive()
        {
            BootstrapAutoLoad.Suppress = true;
            Debug.Log(
                $"[P4-14] Pool stress starting — activeScene={SceneManager.GetActiveScene().name}, " +
                $"hasPool={PoolManager.HasInstance}");

            yield return EnsureBootstrapWithPoolManager();

            PoolManager pool = PoolManager.Instance;
            Assert.That(pool, Is.Not.Null);

            if (ConveyorPathManager.HasInstance)
                ConveyorPathManager.Instance.ClearActiveUnits();
            if (QueueManager.HasInstance)
                QueueManager.Instance.ClearAll();
            if (GridManager.HasInstance)
                GridManager.Instance.ClearGrid();
            yield return null;

            Assert.That(pool.CollectorCountActive, Is.EqualTo(0),
                "Collectors still active — cannot stress-test pool.");
            Assert.That(pool.BlockCountActive, Is.EqualTo(0),
                "Blocks still active — cannot stress-test pool.");

            Debug.Log("[P4-14] Cleared active pool users — warming pool…");

            for (int i = 0; i < 8; i++)
            {
                CollectorUnit warmCollector = pool.GetCollector();
                PixelBlock warmBlock = pool.GetPixelBlock();
                Assert.That(warmCollector, Is.Not.Null);
                Assert.That(warmBlock, Is.Not.Null);

                warmCollector.Initialize(ColorId.Red, 1);
                warmBlock.Initialize(ColorId.Blue, Vector2Int.zero, Vector3.zero, 1f);
                pool.ReleaseCollector(warmCollector);
                pool.ReleasePixelBlock(warmBlock);
            }

            yield return null;

            int collectorsBefore = pool.CollectorCountAll;
            int blocksBefore = pool.BlockCountAll;
            Debug.Log(
                $"[P4-14] Warm done — start {Cycles} cycles " +
                $"(collectorsAll={collectorsBefore}, blocksAll={blocksBefore}). " +
                "Profiler: measure GC Alloc from here.");

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

                if ((i + 1) % YieldEvery == 0)
                    yield return null;
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

        private static IEnumerator EnsureBootstrapWithPoolManager()
        {
            if (PoolManager.HasInstance)
            {
                Debug.Log("[P4-14] PoolManager already present.");
                yield break;
            }

            string bootstrap = SceneLoader.BootstrapSceneName;
            Assert.That(
                Application.CanStreamedLevelBeLoaded(bootstrap),
                Is.True,
                $"Scene '{bootstrap}' is not in Build Settings.");

            Debug.Log(
                $"[P4-14] PoolManager missing (likely empty Play Mode scene / no cameras). " +
                $"Loading '{bootstrap}'…");

            AsyncOperation load = SceneManager.LoadSceneAsync(bootstrap, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, $"LoadSceneAsync('{bootstrap}') returned null.");

            int frames = 0;
            int maxFrames = Mathf.CeilToInt(SceneLoadTimeoutSeconds * 60f);
            while (!load.isDone)
            {
                frames++;
                if (frames > maxFrames)
                {
                    Assert.Fail(
                        $"Timed out loading '{bootstrap}' after ~{SceneLoadTimeoutSeconds}s. " +
                        $"Active={SceneManager.GetActiveScene().name}");
                    yield break;
                }

                yield return null;
            }

            // Awake/Start of PoolManager run after scene activation.
            yield return null;
            yield return null;

            if (!PoolManager.HasInstance)
            {
                PoolManager found = Object.FindFirstObjectByType<PoolManager>();
                Assert.That(
                    found,
                    Is.Not.Null,
                    $"'{bootstrap}' loaded but no PoolManager in scene. Check SCN_Bootstrap setup.");
            }

            Debug.Log(
                $"[P4-14] Bootstrap ready — active={SceneManager.GetActiveScene().name}, " +
                $"hasPool={PoolManager.HasInstance}");
        }
    }
}
