using System;
using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.UI.Screens;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Owns level selection/progression and applies the selected LevelDataSO to an active gameplay scene.
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        public const string CurrentLevelPrefsKey = "PFC_CurrentLevel";

        [SerializeField] private LevelDataSO[] _levels;
        [SerializeField] private bool _loadSavedLevelOnStart = true;
        [SerializeField] private float _gameplayLoadMinSeconds = 0.75f;

        private Coroutine _playRoutine;

        public int CurrentLevelIndex { get; private set; }
        public LevelDataSO CurrentLevel { get; private set; }
        public IReadOnlyList<LevelDataSO> Levels => _levels ?? Array.Empty<LevelDataSO>();
        public bool IsLoadingGameplay => _playRoutine != null;

        public event Action<LevelDataSO, int> LevelLoaded;

        public void ConfigureLevels(params LevelDataSO[] levels)
        {
            _levels = levels ?? Array.Empty<LevelDataSO>();
        }

        protected override void OnSingletonAwake()
        {
            MakePersistent();
            CurrentLevelIndex = Mathf.Max(0, PlayerPrefs.GetInt(CurrentLevelPrefsKey, 0));
        }

        private void Start()
        {
            if (!_loadSavedLevelOnStart || _levels == null || _levels.Length == 0)
                return;

            int savedIndex = Mathf.Clamp(CurrentLevelIndex, 0, _levels.Length - 1);
            LoadLevel(savedIndex);
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

            if (_levels == null || _levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                return;
            }

            if (index < 0 || index >= _levels.Length)
            {
                Debug.LogWarning($"[LevelManager] Invalid level index {index}.");
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
            if (_levels == null || _levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                return;
            }

            PlayLevel(Mathf.Clamp(CurrentLevelIndex, 0, _levels.Length - 1));
        }

        private IEnumerator PlayCurrentLevelRoutine()
        {
            if (_levels == null || _levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                _playRoutine = null;
                yield break;
            }

            int index = Mathf.Clamp(CurrentLevelIndex, 0, _levels.Length - 1);
            if (!LoadLevel(index))
            {
                _playRoutine = null;
                yield break;
            }

            if (GameManager.HasInstance)
                GameManager.Instance.BeginLoading();

            LoadingScreen loading = LoadingScreen.CreateRuntime(transform);
            loading.SetTitle(LoadingScreen.DefaultTitle);
            loading.SetStatus(LoadingScreen.DefaultStatus);
            loading.SetProgress(0f);
            loading.Show();

            Debug.Log(
                $"[LevelManager] Play level index={index}, id={CurrentLevel.LevelId} → " +
                $"{SceneLoader.GameplaySceneName}");

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
        /// Selects and persists a level. If a GameplayContext is active, its runtime systems are rebuilt.
        /// </summary>
        public bool LoadLevel(int index)
        {
            if (_levels == null || _levels.Length == 0)
            {
                Debug.LogWarning("[LevelManager] No levels are assigned.");
                return false;
            }

            if (index < 0 || index >= _levels.Length)
            {
                Debug.LogWarning($"[LevelManager] Invalid level index {index}.");
                return false;
            }

            LevelDataSO level = _levels[index];
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
            if (_levels == null || _levels.Length == 0)
                return false;

            int next = Mathf.Min(CurrentLevelIndex + 1, _levels.Length - 1);
            return LoadLevel(next);
        }

        /// <summary>
        /// Rebuilds pool capacity, path metadata, grid, and waiting collectors for CurrentLevel.
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

            if (PoolManager.HasInstance && context.Conveyor != null)
                PoolManager.Instance.Prewarm(CurrentLevel, context.Conveyor.Config);

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

            if (GameManager.HasInstance &&
                GameManager.Instance.CurrentState == GameState.Loading)
            {
                GameManager.Instance.StartPlaying();
            }

            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug/Load Level 0")]
        private void DebugLoadLevel0() => LoadLevel(0);

        [ContextMenu("Debug/Load Saved Level")]
        private void DebugLoadSavedLevel() => LoadSavedLevel();

        [ContextMenu("Debug/Load Next Level")]
        private void DebugLoadNextLevel() => LoadNextLevel();

        [ContextMenu("Debug/Play Current Level")]
        private void DebugPlayCurrentLevel() => PlayCurrentLevel();
#endif
    }
}
