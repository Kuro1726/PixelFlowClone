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

            GameManager existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null)
                return existing;

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

            LevelManager existing = Object.FindFirstObjectByType<LevelManager>();
            if (existing != null)
            {
                if (levels != null && levels.Length > 0)
                    existing.ConfigureLevels(levels);
                return existing;
            }

            var go = new GameObject("LevelManager");
            LevelManager manager = go.AddComponent<LevelManager>();
            if (levels != null && levels.Length > 0)
                manager.ConfigureLevels(levels);
            return manager;
        }

        public static InputManager EnsureInputManager()
        {
            if (InputManager.HasInstance)
                return InputManager.Instance;

            InputManager existing = Object.FindFirstObjectByType<InputManager>();
            if (existing != null)
                return existing;

            var go = new GameObject("InputManager");
            return go.AddComponent<InputManager>();
        }

        public static UIManager EnsureUIManager()
        {
            if (UIManager.HasInstance)
                return UIManager.Instance;

            UIManager existing = Object.FindFirstObjectByType<UIManager>();
            if (existing != null)
                return existing;

            var go = new GameObject("UIManager");
            return go.AddComponent<UIManager>();
        }

        public static AudioManager EnsureAudioManager()
        {
            if (AudioManager.HasInstance)
                return AudioManager.Instance;

            AudioManager existing = Object.FindFirstObjectByType<AudioManager>();
            if (existing != null)
                return existing;

            var go = new GameObject("AudioManager");
            return go.AddComponent<AudioManager>();
        }
    }
}
