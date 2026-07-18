using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Core
{
    /// <summary>
    /// Async scene loading with a normalized progress callback (0–1).
    /// </summary>
    public static class SceneLoader
    {
        public const string BootstrapSceneName = "SCN_Bootstrap";
        public const string MainMenuSceneName = "SCN_MainMenu";
        public const string GameplaySceneName = "SCN_Gameplay";

        /// <summary>
        /// Loads a scene asynchronously. Invokes <paramref name="onProgress"/> with values in [0, 1].
        /// Unity reports AsyncOperation.progress up to 0.9 before activation; this maps that range to 0–1.
        /// When <paramref name="minDurationSeconds"/> &gt; 0, activation waits until both the load and the
        /// minimum display time finish, so the loading UI remains visible long enough to notice.
        /// </summary>
        public static IEnumerator LoadAsync(
            string sceneName,
            Action<float> onProgress = null,
            LoadSceneMode mode = LoadSceneMode.Single,
            float minDurationSeconds = 0f)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene name is empty.");
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneLoader] Scene '{sceneName}' is not in Build Settings or cannot be loaded.");
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] LoadSceneAsync returned null for '{sceneName}'.");
                yield break;
            }

            operation.allowSceneActivation = false;
            float startTime = Time.unscaledTime;
            float minDuration = Mathf.Max(0f, minDurationSeconds);

            while (true)
            {
                float loadNorm = operation.progress >= 0.9f
                    ? 1f
                    : Mathf.Clamp01(operation.progress / 0.9f);
                float timeNorm = minDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01((Time.unscaledTime - startTime) / minDuration);

                // Progress advances with the slower of load vs minimum display time.
                onProgress?.Invoke(Mathf.Min(loadNorm, timeNorm));

                if (loadNorm >= 1f && timeNorm >= 1f)
                    break;

                yield return null;
            }

            onProgress?.Invoke(1f);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            Debug.Log($"[SceneLoader] Loaded '{sceneName}'.");
        }
    }
}
