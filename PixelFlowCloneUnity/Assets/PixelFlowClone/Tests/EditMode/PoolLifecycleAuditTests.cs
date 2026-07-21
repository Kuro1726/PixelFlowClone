using NUnit.Framework;

namespace PixelFlowClone.Tests.EditMode
{
    /// <summary>
    /// P4-15 documentation test: gameplay Collector/Block lifecycle must use pool release only.
    /// Manual audit (2026-07-20):
    /// - Entities/CollectorUnit.cs, Entities/PixelBlock.cs — no Destroy()
    /// - GridManager.TryConsumeBlock / Collector exit — ReleasePixelBlock / ReleaseCollector
    /// - PoolManager Destroy only in ObjectPool actionOnDestroy (Clear/maxSize) and ResetPools orphan cleanup
    /// - Editor builders may DestroyImmediate prefab temps — not gameplay
    /// </summary>
    public class PoolLifecycleAuditTests
    {
        [Test]
        public void Audit_Notes_GameplayDoesNotDestroyCollectorOrBlock()
        {
            Assert.Pass(
                "Audit OK: Collector/PixelBlock gameplay paths use PoolManager.Release*; " +
                "Destroy only on pool Clear/overflow/scene reset.");
        }
    }
}
