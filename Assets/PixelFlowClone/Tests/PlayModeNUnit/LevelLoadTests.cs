using System.Collections;
using NUnit.Framework;
using PixelFlowClone.Managers;
using UnityEngine.TestTools;

namespace PixelFlowClone.Tests.PlayMode
{
    public class LevelLoadTests
    {
        [UnityTest]
        public IEnumerator SpawnedBlockCount_MatchesLevelDataNonEmptyCells()
        {
            yield return PlayModeTestHelpers.LoadGameplayAndApplyLevel(0);

            Assume.That(LevelManager.HasInstance, Is.True);
            Assume.That(LevelManager.Instance.CurrentLevel, Is.Not.Null);
            Assume.That(GridManager.HasInstance, Is.True);

            int expected = LevelManager.Instance.CurrentLevel.CountNonEmptyBlocks();
            int actual = GridManager.Instance.RemainingBlocks;

            Assert.That(actual, Is.EqualTo(expected),
                $"Spawned RemainingBlocks={actual} but LevelData non-empty cells={expected}.");
        }
    }
}
