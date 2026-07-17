using System;
using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
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

        public int CurrentLevelIndex { get; private set; }
        public LevelDataSO CurrentLevel { get; private set; }
        public IReadOnlyList<LevelDataSO> Levels => _levels ?? Array.Empty<LevelDataSO>();

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
#endif
    }
}
