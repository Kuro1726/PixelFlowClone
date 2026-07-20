using System.Collections;
using NUnit.Framework;
using PixelFlowClone.Core;
using PixelFlowClone.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Tests.PlayMode
{
    internal static class PlayModeTestHelpers
    {
        public const string GameplaySceneName = "SCN_Gameplay";

        public static IEnumerator LoadGameplayScene()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null,
                $"Failed to start loading scene '{GameplaySceneName}'. Is it in Build Settings?");

            while (!load.isDone)
                yield return null;

            yield return null;
            yield return null;
        }

        public static IEnumerator LoadGameplayAndApplyLevel(int levelIndex)
        {
            yield return LoadGameplayScene();

            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            Assume.That(levelManager, Is.Not.Null, "LevelManager missing in SCN_Gameplay.");

            Assume.That(
                levelManager.LoadLevel(levelIndex),
                Is.True,
                $"LoadLevel({levelIndex}) failed.");

            yield return null;

            if (GameManager.HasInstance &&
                GameManager.Instance.CurrentState == GameState.Loading)
            {
                GameManager.Instance.StartPlaying();
            }

            yield return null;
        }
    }
}
