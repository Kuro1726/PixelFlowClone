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
        private const float SceneLoadTimeoutSeconds = 60f;

        public static IEnumerator LoadGameplayScene()
        {
            // Belt-and-suspenders if Editor suppressor did not run yet.
            BootstrapAutoLoad.Suppress = true;

            if (SceneManager.GetActiveScene().name == GameplaySceneName)
            {
                yield return null;
                yield return null;
                yield break;
            }

            Debug.Log($"[PlayModeTest] Loading scene '{GameplaySceneName}' (active={SceneManager.GetActiveScene().name})…");

            AsyncOperation load = SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null,
                $"Failed to start loading scene '{GameplaySceneName}'. Is it in Build Settings? " +
                "If Bootstrapper was also loading, stop Play and re-run after domain reload.");

            float elapsed = 0f;
            while (!load.isDone)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed > SceneLoadTimeoutSeconds)
                {
                    Assert.Fail(
                        $"Timed out loading '{GameplaySceneName}' after {SceneLoadTimeoutSeconds}s. " +
                        $"Active scene={SceneManager.GetActiveScene().name}. " +
                        "Usually Bootstrapper MainMenu load raced this call — re-run Play Mode tests after scripts recompile.");
                    yield break;
                }

                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(GameplaySceneName),
                $"Expected active scene '{GameplaySceneName}' but got '{SceneManager.GetActiveScene().name}'.");

            yield return null;
            yield return null;
            Debug.Log($"[PlayModeTest] Scene '{GameplaySceneName}' ready.");
        }

        public static IEnumerator LoadGameplayAndApplyLevel(int levelIndex)
        {
            yield return LoadGameplayScene();

            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            Assume.That(levelManager, Is.Not.Null, "LevelManager missing after loading SCN_Gameplay.");

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
