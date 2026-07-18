using System.Collections;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.UI.Screens;
using UnityEngine;

namespace PixelFlowClone.Core
{
    /// <summary>
    /// Entry-scene orchestrator. Ensures global DontDestroyOnLoad managers exist, then
    /// async-loads the next scene (default: Main Menu) with a progress callback.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private LevelDataSO[] _levels;
        [SerializeField] private bool _ensureInputManager = true;
        [SerializeField] private bool _loadMainMenuOnStart = true;
        [SerializeField] private string _nextSceneName = SceneLoader.MainMenuSceneName;
        [SerializeField] private LoadingScreen _loadingScreen;
        [SerializeField] private LoadingScreen _loadingScreenPrefab;
        [Tooltip("Minimum time the loading screen stays visible (Editor/dev-friendly).")]
        [SerializeField] private float _minLoadingSeconds = 3.5f;

        public bool IsReady { get; private set; }
        public float LoadProgress { get; private set; }

        private void Awake()
        {
            // Only create missing shells here. PoolManager must live in the scene with prefabs.
            // Readiness is evaluated in Start so scene singleton Awakes have already run.
            PersistentManagers.EnsureGameManager();
            PersistentManagers.EnsureLevelManager(_levels);

            if (_ensureInputManager)
                PersistentManagers.EnsureInputManager();
        }

        private void Start()
        {
            IsReady = EvaluateReady();

            if (IsReady)
                Debug.Log("[Bootstrapper] Persistent managers ready (Game, Level, Pool, Input).");
            else
                Debug.LogError("[Bootstrapper] Bootstrap incomplete — check missing managers.");

            if (!IsReady || !_loadMainMenuOnStart)
                return;

            if (string.IsNullOrEmpty(_nextSceneName))
            {
                Debug.LogError("[Bootstrapper] Next scene name is empty.");
                return;
            }

            StartCoroutine(LoadNextSceneRoutine());
        }

        private static bool EvaluateReady()
        {
            if (!PoolManager.HasInstance)
            {
                Debug.LogError(
                    "[Bootstrapper] PoolManager is missing. Place a PoolManager in SCN_Bootstrap " +
                    "with collector/block prefabs assigned.");
                return false;
            }

            return GameManager.HasInstance && LevelManager.HasInstance;
        }

        private IEnumerator LoadNextSceneRoutine()
        {
            LoadingScreen loading = ResolveLoadingScreen();
            loading.SetTitle(LoadingScreen.DefaultTitle);
            loading.SetStatus(LoadingScreen.DefaultStatus);
            loading.SetProgress(0f);
            loading.Show();

            Debug.Log($"[Bootstrapper] Async loading '{_nextSceneName}'...");

#if UNITY_EDITOR
            // Bootstrap scene unloads on Single load — clear Inspector selection so UI Toolkit
            // ListView does not bind disposed _levels SerializedProperties.
            GameObject selected = UnityEditor.Selection.activeGameObject;
            if (selected != null && selected.scene == gameObject.scene)
                UnityEditor.Selection.activeObject = null;
#endif

            yield return SceneLoader.LoadAsync(
                _nextSceneName,
                progress =>
                {
                    LoadProgress = progress;
                    loading.SetProgress(progress);
                },
                minDurationSeconds: Mathf.Max(0f, _minLoadingSeconds));
        }

        private LoadingScreen ResolveLoadingScreen()
        {
            if (_loadingScreen != null)
                return _loadingScreen;

            if (_loadingScreenPrefab != null)
            {
                _loadingScreen = Instantiate(_loadingScreenPrefab);
                return _loadingScreen;
            }

            _loadingScreen = LoadingScreen.CreateRuntime();
            return _loadingScreen;
        }
    }
}
