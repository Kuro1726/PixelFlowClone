using System;
using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.UI.Screens;
using PixelFlowClone.Utils;
using PixelFlowClone.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Owns level selection/progression and applies the selected LevelDataSO to an active gameplay scene.
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        public const string CurrentLevelPrefsKey = "PFC_CurrentLevel";
        public const string HighestUnlockedPrefsKey = "PFC_HighestUnlockedLevel";

        [SerializeField] private LevelDataSO[] _levels;
        [SerializeField] private bool _loadSavedLevelOnStart = true;
        [Tooltip("Minimum time the loading screen remains visible for gameplay/menu transitions.")]
        [SerializeField, Min(0f)] private float _gameplayLoadMinSeconds = 3f;

        private Coroutine _playRoutine;

        /// <summary>
        /// Runtime override from Bootstrapper. Kept separate from <see cref="_levels"/> so the
        /// Inspector ListView is not invalidated when levels are configured during Play Mode.
        /// </summary>
        private LevelDataSO[] _activeLevels;

        public int CurrentLevelIndex { get; private set; }
        public LevelDataSO CurrentLevel { get; private set; }
        public int HighestUnlockedLevelIndex { get; private set; }
        public IReadOnlyList<LevelDataSO> Levels => GetLevels();
        public bool IsLoadingGameplay => _playRoutine != null;

        public event Action<LevelDataSO, int> LevelLoaded;

        public void ConfigureLevels(params LevelDataSO[] levels)
        {
            // Do not assign to serialized _levels while playing — Unity's UI Toolkit ListView
            // throws ObjectDisposedException when Array.data[i] bindings disappear mid-frame.
            _activeLevels = levels != null && levels.Length > 0 ? levels : null;
        }

        private LevelDataSO[] GetLevels()
        {
            if (_activeLevels != null && _activeLevels.Length > 0)
                return _activeLevels;
            return _levels ?? Array.Empty<LevelDataSO>();
        }

        protected override void OnSingletonAwake()
        {
            MakePersistent();
            CurrentLevelIndex = Mathf.Max(0, PlayerPrefs.GetInt(CurrentLevelPrefsKey, 0));
            HighestUnlockedLevelIndex = Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedPrefsKey, 0));
        }

        private void OnEnable()
        {
            GameEvents.OnVictory -= HandleVictory;
            GameEvents.OnVictory += HandleVictory;
        }

        private void OnDisable()
        {
            GameEvents.OnVictory -= HandleVictory;
        }

        private void Start()
        {
            LevelDataSO[] levels = GetLevels();
            if (!_loadSavedLevelOnStart || levels.Length == 0)
                return;

            int savedIndex = Mathf.Clamp(CurrentLevelIndex, 0, levels.Length - 1);
            LoadLevel(savedIndex);
        }

        public bool IsLevelUnlocked(int index)
        {
            return index >= 0 && index <= HighestUnlockedLevelIndex;
        }

        /// <summary>
        /// Ensures levels up to <paramref name="index"/> are playable (inclusive).
        /// </summary>
        public void UnlockUpTo(int index)
        {
            if (index < 0)
                return;

            LevelDataSO[] levels = GetLevels();
            if (levels.Length > 0)
                index = Mathf.Min(index, levels.Length - 1);

            if (index <= HighestUnlockedLevelIndex)
                return;

            HighestUnlockedLevelIndex = index;
            PlayerPrefs.SetInt(HighestUnlockedPrefsKey, HighestUnlockedLevelIndex);
            PlayerPrefs.Save();
            Debug.Log($"[LevelManager] Unlocked up to level index={HighestUnlockedLevelIndex}");
        }

        /// <summary>
        /// After clearing the current level, unlocks the next one (if any).
        /// </summary>
        public void UnlockNextLevel()
        {
            LevelDataSO[] levels = GetLevels();
            if (levels.Length == 0)
                return;

            int next = Mathf.Min(CurrentLevelIndex + 1, levels.Length - 1);
            UnlockUpTo(next);
        }

        private void HandleVictory()
        {
            UnlockNextLevel();
        }

        /// <summary>
        /// Main Menu: persist level index then async-load gameplay and apply it.
        /// </summary>
        public void PlayLevel(int index)
        {
            if (_playRoutine != null)
            {
                Debug.LogWarning("[LevelManager] PlayLevel ignored — already loading.");
                return;
            }

            LevelDataSO[] levels = GetLevels();
            if (levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                return;
            }

            if (index < 0 || index >= levels.Length)
            {
                Debug.LogWarning($"[LevelManager] Invalid level index {index}.");
                return;
            }

            if (!IsLevelUnlocked(index))
            {
                Debug.LogWarning($"[LevelManager] Level index={index} is locked.");
                return;
            }

            CurrentLevelIndex = index;
            _playRoutine = StartCoroutine(PlayCurrentLevelRoutine());
        }

        /// <summary>
        /// Plays the saved/current level index.
        /// </summary>
        public void PlayCurrentLevel()
        {
            LevelDataSO[] levels = GetLevels();
            if (levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                return;
            }

            PlayLevel(Mathf.Clamp(CurrentLevelIndex, 0, levels.Length - 1));
        }

        private IEnumerator PlayCurrentLevelRoutine()
        {
            LevelDataSO[] levels = GetLevels();
            if (levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                _playRoutine = null;
                yield break;
            }

            int index = Mathf.Clamp(CurrentLevelIndex, 0, levels.Length - 1);
            if (!LoadLevel(index))
            {
                _playRoutine = null;
                yield break;
            }

            if (GameManager.HasInstance)
                GameManager.Instance.BeginLoading();

            ReclaimGameplayEntities();

            LoadingScreen loading = LoadingScreen.Create(transform);
            loading.SetTitle(LoadingScreen.DefaultTitle);
            loading.SetStatus(LoadingScreen.DefaultStatus);
            loading.SetProgress(0f);
            loading.Show();

            Debug.Log(
                $"[LevelManager] Play level index={index}, id={CurrentLevel.LevelId} → " +
                $"{SceneLoader.GameplaySceneName}");

#if UNITY_EDITOR
            ClearEditorSelectionInActiveScene();
#endif
            yield return SceneLoader.LoadAsync(
                SceneLoader.GameplaySceneName,
                loading.SetProgress,
                minDurationSeconds: Mathf.Max(0f, _gameplayLoadMinSeconds));

            ApplyCurrentLevelToGameplay();

            if (loading != null)
                Destroy(loading.gameObject);

            _playRoutine = null;
        }

        /// <summary>
        /// Pause / Home: leave gameplay and return to the main menu scene.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (_playRoutine != null)
            {
                Debug.LogWarning("[LevelManager] ReturnToMainMenu ignored — already loading.");
                return;
            }

            _playRoutine = StartCoroutine(ReturnToMainMenuRoutine());
        }

        private IEnumerator ReturnToMainMenuRoutine()
        {
            ReclaimGameplayEntities();

            if (GameManager.HasInstance)
                GameManager.Instance.BeginLoading();

            LoadingScreen loading = LoadingScreen.Create(transform);
            loading.SetTitle(LoadingScreen.DefaultTitle);
            loading.SetStatus(LoadingScreen.DefaultStatus);
            loading.SetProgress(0f);
            loading.Show();

            Debug.Log($"[LevelManager] Returning to {SceneLoader.MainMenuSceneName}");

#if UNITY_EDITOR
            ClearEditorSelectionInActiveScene();
#endif
            yield return SceneLoader.LoadAsync(
                SceneLoader.MainMenuSceneName,
                loading.SetProgress,
                minDurationSeconds: Mathf.Max(0f, _gameplayLoadMinSeconds));

            if (loading != null)
                Destroy(loading.gameObject);

            if (PoolManager.HasInstance)
                PoolManager.Instance.ResetPools();

            _playRoutine = null;
        }

        /// <summary>
        /// Releases scene-parented pooled entities back under PoolManager before a scene unload.
        /// </summary>
        private static void ReclaimGameplayEntities()
        {
            GameplayContext context = GameplayContext.Instance;
            if (context == null)
                return;

            context.Conveyor?.ClearActiveUnits();
            context.Queue?.ClearAll();
            context.Grid?.ClearGrid();
        }

        /// <summary>
        /// Selects and persists a level. If a GameplayContext is active, its runtime systems are rebuilt.
        /// </summary>
        public bool LoadLevel(int index)
        {
            LevelDataSO[] levels = GetLevels();
            if (levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                return false;
            }

            if (index < 0 || index >= levels.Length)
            {
                Debug.LogWarning($"[LevelManager] Invalid level index {index}.");
                return false;
            }

            LevelDataSO level = levels[index];
            if (level == null)
            {
                Debug.LogWarning($"[LevelManager] Level at index {index} is null.");
                return false;
            }

            CurrentLevelIndex = index;
            CurrentLevel = level;
            PlayerPrefs.SetInt(CurrentLevelPrefsKey, index);
            PlayerPrefs.Save();

            if (GameplayContext.Instance != null)
                ApplyCurrentLevelToGameplay();

            LevelLoaded?.Invoke(level, index);
            Debug.Log($"[LevelManager] Loaded level index={index}, id={level.LevelId}, name={level.LevelName}");
            return true;
        }

        public bool LoadSavedLevel()
        {
            int index = PlayerPrefs.GetInt(CurrentLevelPrefsKey, 0);
            return LoadLevel(Mathf.Clamp(index, 0, Mathf.Max(0, Levels.Count - 1)));
        }

        public bool LoadNextLevel()
        {
            LevelDataSO[] levels = GetLevels();
            if (levels.Length == 0)
                return false;

            int next = Mathf.Min(CurrentLevelIndex + 1, levels.Length - 1);
            return LoadLevel(next);
        }

        /// <summary>
        /// Rebuilds pool capacity, binds fixed conveyor metadata, spawns grid into the playfield frame,
        /// and loads waiting collectors.
        /// </summary>
        public bool ApplyCurrentLevelToGameplay()
        {
            GameplayContext context = GameplayContext.Instance;
            if (CurrentLevel == null || context == null)
                return false;

            if (GameManager.HasInstance &&
                GameManager.Instance.CurrentState != GameState.Loading)
            {
                GameManager.Instance.BeginLoading();
            }

            if (PoolManager.HasInstance)
            {
                // Drop any stale refs left from a previous gameplay scene unload.
                PoolManager.Instance.ResetPools();
                if (context.Conveyor != null)
                    PoolManager.Instance.Prewarm(CurrentLevel, context.Conveyor.Config);
            }

            if (context.Conveyor != null)
            {
                context.Conveyor.ClearActiveUnits();
                context.Conveyor.ConfigureFromLevel(
                    CurrentLevel,
                    context.Conveyor.PathRoot,
                    context.Conveyor.Config);
            }

            context.Grid?.SpawnGrid(CurrentLevel);
            context.Queue?.LoadLevel(CurrentLevel);

            FitGameplayCamera(CurrentLevel);
            FitGameplayBackground();

            if (context.Hud != null)
                context.Hud.SetLevelIndex(CurrentLevelIndex);

            if (GameManager.HasInstance &&
                GameManager.Instance.CurrentState == GameState.Loading)
            {
                GameManager.Instance.StartPlaying();
            }

            return true;
        }

        private static void FitGameplayCamera(LevelDataSO level)
        {
            Camera camera = Camera.main;
            if (camera == null || level == null)
                return;

            GameConfigSO config = null;
            if (GameplayContext.Instance != null && GameplayContext.Instance.Conveyor != null)
                config = GameplayContext.Instance.Conveyor.Config;

            if (GridManager.HasInstance)
            {
                float waitingY = LevelLayout.GetWaitingStackWorldPosition(level).y;
                if (QueueManager.HasInstance && QueueManager.Instance.Waiting != null)
                    waitingY = QueueManager.Instance.Waiting.StackAnchorWorld.y;

                LevelLayout.FitCameraToPlayfield(
                    camera,
                    GridManager.Instance.GridCenterWorld,
                    GridManager.Instance.PlayfieldSize,
                    waitingY,
                    config);
                return;
            }

            LevelLayout.FitCameraToLevel(camera, level);
        }

        private static void FitGameplayBackground()
        {
            GameplayBackground background = null;
            if (GameplayContext.Instance != null)
                background = GameplayContext.Instance.Background;
            if (background == null)
                background = UnityEngine.Object.FindFirstObjectByType<GameplayBackground>(FindObjectsInactive.Include);

            background?.FitToCamera(Camera.main);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Avoids UI Toolkit ListView binding to SerializedProperties on a GameObject about to be destroyed.
        /// </summary>
        private static void ClearEditorSelectionInActiveScene()
        {
            GameObject selected = UnityEditor.Selection.activeGameObject;
            if (selected == null)
                return;

            Scene active = SceneManager.GetActiveScene();
            if (selected.scene == active)
                UnityEditor.Selection.activeObject = null;
        }

        [ContextMenu("Debug/Load Level 0")]
        private void DebugLoadLevel0() => LoadLevel(0);

        [ContextMenu("Debug/Load Saved Level")]
        private void DebugLoadSavedLevel() => LoadSavedLevel();

        [ContextMenu("Debug/Load Next Level")]
        private void DebugLoadNextLevel() => LoadNextLevel();

        [ContextMenu("Debug/Play Current Level")]
        private void DebugPlayCurrentLevel() => PlayCurrentLevel();

        [ContextMenu("Debug/Unlock All Levels")]
        private void DebugUnlockAllLevels()
        {
            LevelDataSO[] levels = GetLevels();
            if (levels.Length == 0)
                return;
            UnlockUpTo(levels.Length - 1);
        }

        [ContextMenu("Debug/Reset Unlock Progress")]
        private void DebugResetUnlockProgress()
        {
            HighestUnlockedLevelIndex = 0;
            PlayerPrefs.SetInt(HighestUnlockedPrefsKey, 0);
            PlayerPrefs.Save();
            Debug.Log("[LevelManager] Unlock progress reset to level 0 only.");
        }
#endif
    }
}
