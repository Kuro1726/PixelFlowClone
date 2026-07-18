using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds SCN_Bootstrap with DontDestroyOnLoad manager shells + Bootstrapper.
    /// Menu: PixelFlowClone → Build Bootstrap Scene
    /// </summary>
    public static class BootstrapSceneBuilder
    {
        private const string ScenePath = "Assets/PixelFlowClone/Scenes/SCN_Bootstrap.unity";
        private const string LevelPath = "Assets/PixelFlowClone/ScriptableObjects/Levels/Level_001.asset";
        private const string CollectorPrefabPath = "Assets/PixelFlowClone/Prefabs/Entities/PF_CollectorUnit.prefab";
        private const string BlockPrefabPath = "Assets/PixelFlowClone/Prefabs/Entities/PF_PixelBlock.prefab";

        [MenuItem("PixelFlowClone/Build Bootstrap Scene")]
        public static void BuildBootstrapScene()
        {
            EnsureSceneFolder();

            LevelDataSO level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(LevelPath);
            CollectorUnit collectorPrefab =
                AssetDatabase.LoadAssetAtPath<CollectorUnit>(CollectorPrefabPath);
            PixelBlock blockPrefab = AssetDatabase.LoadAssetAtPath<PixelBlock>(BlockPrefabPath);

            if (collectorPrefab == null || blockPrefab == null)
            {
                Debug.LogError(
                    "[PixelFlowClone] Entity prefabs missing. Run entity prefab builders first.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureCamera();

            GameObject poolGo = new GameObject("PoolManager");
            PoolManager pool = poolGo.AddComponent<PoolManager>();
            SerializedObject poolSo = new SerializedObject(pool);
            poolSo.FindProperty("_collectorPrefab").objectReferenceValue = collectorPrefab;
            poolSo.FindProperty("_blockPrefab").objectReferenceValue = blockPrefab;
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject gameGo = new GameObject("GameManager");
            GameManager game = gameGo.AddComponent<GameManager>();
            SerializedObject gameSo = new SerializedObject(game);
            gameSo.FindProperty("_startPlayingOnStart").boolValue = false;
            gameSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject levelGo = new GameObject("LevelManager");
            LevelManager levelManager = levelGo.AddComponent<LevelManager>();
            SerializedObject levelSo = new SerializedObject(levelManager);
            SerializedProperty levels = levelSo.FindProperty("_levels");
            levels.arraySize = level != null ? 1 : 0;
            if (level != null)
                levels.GetArrayElementAtIndex(0).objectReferenceValue = level;
            levelSo.FindProperty("_loadSavedLevelOnStart").boolValue = false;
            levelSo.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("InputManager").AddComponent<InputManager>();

            GameObject bootstrapGo = new GameObject("Bootstrapper");
            Bootstrapper bootstrapper = bootstrapGo.AddComponent<Bootstrapper>();
            SerializedObject bootSo = new SerializedObject(bootstrapper);
            SerializedProperty bootLevels = bootSo.FindProperty("_levels");
            bootLevels.arraySize = level != null ? 1 : 0;
            if (level != null)
                bootLevels.GetArrayElementAtIndex(0).objectReferenceValue = level;
            bootSo.FindProperty("_loadMainMenuOnStart").boolValue = true;
            bootSo.FindProperty("_nextSceneName").stringValue = SceneLoader.MainMenuSceneName;
            bootSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddBootstrapToBuildSettings();
            AssetDatabase.Refresh();

            Debug.Log($"[PixelFlowClone] Bootstrap scene saved: {ScenePath}");
        }

        public static void BuildBootstrapSceneBatch()
        {
            BuildBootstrapScene();
            EditorApplication.Exit(0);
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PixelFlowClone/Scenes"))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Scenes");
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void AddBootstrapToBuildSettings()
        {
            const string mainMenuPath = "Assets/PixelFlowClone/Scenes/SCN_MainMenu.unity";
            const string gameplayPath = "Assets/PixelFlowClone/Scenes/SCN_Gameplay.unity";

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mainMenuPath) != null)
                scenes.Add(new EditorBuildSettingsScene(mainMenuPath, true));

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(gameplayPath) != null)
                scenes.Add(new EditorBuildSettingsScene(gameplayPath, true));

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                string path = existing[i].path;
                if (path == ScenePath || path == mainMenuPath || path == gameplayPath)
                    continue;
                scenes.Add(existing[i]);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[PixelFlowClone] Build Settings: Bootstrap → MainMenu → Gameplay.");
        }
    }
}
