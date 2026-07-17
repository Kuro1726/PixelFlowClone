using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Creates global DontDestroyOnLoad managers as scene-root objects.
    /// They must not live under PF_GameplayContext — nesting + DontDestroyOnLoad causes Missing Script.
    /// </summary>
    public static class PersistentManagers
    {
        public static GameManager EnsureGameManager()
        {
            if (GameManager.HasInstance)
                return GameManager.Instance;

            var go = new GameObject("GameManager");
            return go.AddComponent<GameManager>();
        }

        public static LevelManager EnsureLevelManager(params LevelDataSO[] levels)
        {
            if (LevelManager.HasInstance)
            {
                if (levels != null && levels.Length > 0)
                    LevelManager.Instance.ConfigureLevels(levels);
                return LevelManager.Instance;
            }

            var go = new GameObject("LevelManager");
            LevelManager manager = go.AddComponent<LevelManager>();
            if (levels != null && levels.Length > 0)
                manager.ConfigureLevels(levels);
            return manager;
        }
    }
}
