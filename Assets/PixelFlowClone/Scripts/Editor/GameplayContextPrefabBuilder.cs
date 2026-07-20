using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Converts the gameplay systems hierarchy into PF_GameplayContext and wires its manager references.
    /// GameManager / LevelManager stay as scene roots (DontDestroyOnLoad) — never nested under the prefab.
    /// </summary>
    public static class GameplayContextPrefabBuilder
    {
        private const string ScenePath = "Assets/PixelFlowClone/Scenes/SCN_Gameplay.unity";
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/Gameplay";
        private const string PrefabPath = PrefabFolder + "/PF_GameplayContext.prefab";
        private const string DefaultLevelPath =
            "Assets/PixelFlowClone/ScriptableObjects/Levels/Level_001.asset";
        private const string VictoryPanelPath =
            "Assets/PixelFlowClone/Art/UI/Popups/Victory/VictoryPanel.png";
        private const string VictoryTitleBannerPath =
            "Assets/PixelFlowClone/Art/UI/Popups/Victory/TitleBanner.png";
        private const string VictoryTrophyPath =
            "Assets/PixelFlowClone/Art/UI/Popups/Victory/Trophy.png";
        private const string VictoryTrophyWingPath =
            "Assets/PixelFlowClone/Art/UI/Popups/Victory/TrophyWing.png";
        private const string VictoryContinueButtonPath =
            "Assets/PixelFlowClone/Art/UI/Popups/Victory/ContinueButton.png";

        [InitializeOnLoadMethod]
        private static void BuildMissingPrefabWhenGameplaySceneIsOpen()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += EnsureGameplayContextPrefab;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += EnsureGameplayContextPrefab;
        }

        private static void EnsureGameplayContextPrefab()
        {
            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += EnsureGameplayContextPrefab;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null && prefab.GetComponent<GameplayContext>() != null)
            {
                if (prefab.GetComponentInChildren<CollectorShotVfx>(true) == null)
                    AddShotVfxToExistingPrefab();
                return;
            }

            if (SceneManager.GetActiveScene().path == ScenePath)
                BuildGameplayContextPrefab();
        }

        private static void AddShotVfxToExistingPrefab()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                GameplayContext context = contents.GetComponent<GameplayContext>();
                if (context == null || contents.GetComponentInChildren<CollectorShotVfx>(true) != null)
                    return;

                var shotObject = new GameObject("CollectorShotVfx");
                shotObject.transform.SetParent(contents.transform, false);
                CollectorShotVfx shotVfx = shotObject.AddComponent<CollectorShotVfx>();

                SerializedObject contextSo = new SerializedObject(context);
                contextSo.FindProperty("_shotVfx").objectReferenceValue = shotVfx;
                contextSo.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(context);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[PixelFlowClone] CollectorShotVfx added to Gameplay Context prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [MenuItem("PixelFlowClone/Build Gameplay Context Prefab")]
        public static void BuildGameplayContextPrefab()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindRoot(scene, "PF_GameplayContext") ??
                              FindRoot(scene, "GameplaySystems");

            if (root == null)
            {
                Debug.LogError("[PixelFlowClone] GameplaySystems root was not found.");
                return;
            }

            DetachPersistentManagersFromContext(root);

            GridManager grid = root.GetComponentInChildren<GridManager>(true);
            ConveyorPathManager conveyor = root.GetComponentInChildren<ConveyorPathManager>(true);
            QueueManager queue = root.GetComponentInChildren<QueueManager>(true);

            if (grid == null || conveyor == null || queue == null)
            {
                Debug.LogError(
                    "[PixelFlowClone] Gameplay context requires GridManager, " +
                    "ConveyorPathManager, and QueueManager under its root.");
                return;
            }

            InputManager input = root.GetComponentInChildren<InputManager>(true);
            if (input == null)
            {
                var inputObject = new GameObject("InputManager");
                inputObject.transform.SetParent(root.transform, false);
                input = inputObject.AddComponent<InputManager>();
            }

            EnsureSceneRootPersistentManagers(scene);

            GameplayContext context = root.GetComponent<GameplayContext>();
            if (context == null)
                context = root.AddComponent<GameplayContext>();

            context.Configure(grid, conveyor, queue, input);

            Sprite victoryPanel = AssetDatabase.LoadAssetAtPath<Sprite>(VictoryPanelPath);
            Sprite victoryTitleBanner = AssetDatabase.LoadAssetAtPath<Sprite>(VictoryTitleBannerPath);
            Sprite victoryTrophy = AssetDatabase.LoadAssetAtPath<Sprite>(VictoryTrophyPath);
            Sprite victoryTrophyWing = AssetDatabase.LoadAssetAtPath<Sprite>(VictoryTrophyWingPath);
            Sprite victoryContinueButton = AssetDatabase.LoadAssetAtPath<Sprite>(VictoryContinueButtonPath);
            SerializedObject contextSo = new SerializedObject(context);
            contextSo.FindProperty("_victoryPanelSprite").objectReferenceValue = victoryPanel;
            contextSo.FindProperty("_victoryTitleBannerSprite").objectReferenceValue = victoryTitleBanner;
            contextSo.FindProperty("_victoryTrophySprite").objectReferenceValue = victoryTrophy;
            contextSo.FindProperty("_victoryTrophyWingSprite").objectReferenceValue = victoryTrophyWing;
            contextSo.FindProperty("_victoryContinueButtonSprite").objectReferenceValue = victoryContinueButton;
            contextSo.ApplyModifiedPropertiesWithoutUndo();

            if (victoryPanel == null)
                Debug.LogWarning($"[PixelFlowClone] Victory panel not found: {VictoryPanelPath}");
            if (victoryTitleBanner == null)
                Debug.LogWarning($"[PixelFlowClone] Victory title banner not found: {VictoryTitleBannerPath}");
            if (victoryTrophy == null)
                Debug.LogWarning($"[PixelFlowClone] Victory trophy not found: {VictoryTrophyPath}");
            if (victoryTrophyWing == null)
                Debug.LogWarning($"[PixelFlowClone] Victory trophy wing not found: {VictoryTrophyWingPath}");
            if (victoryContinueButton == null)
                Debug.LogWarning($"[PixelFlowClone] Victory continue button not found: {VictoryContinueButtonPath}");

            root.name = "PF_GameplayContext";

            EnsurePrefabFolder();
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                root,
                PrefabPath,
                InteractionMode.AutomatedAction);

            EditorUtility.SetDirty(context);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[PixelFlowClone] Gameplay context prefab saved: {PrefabPath}; " +
                $"scene wired: {ScenePath}.");
        }

        public static void BuildGameplayContextPrefabBatch()
        {
            BuildGameplayContextPrefab();
            EditorApplication.Exit(0);
        }

        private static void DetachPersistentManagersFromContext(GameObject root)
        {
            GameManager[] games = root.GetComponentsInChildren<GameManager>(true);
            for (int i = 0; i < games.Length; i++)
            {
                if (games[i] != null)
                    Object.DestroyImmediate(games[i].gameObject);
            }

            LevelManager[] levels = root.GetComponentsInChildren<LevelManager>(true);
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null)
                    Object.DestroyImmediate(levels[i].gameObject);
            }
        }

        private static void EnsureSceneRootPersistentManagers(Scene scene)
        {
            LevelDataSO defaultLevel = AssetDatabase.LoadAssetAtPath<LevelDataSO>(DefaultLevelPath);

            GameManager game = Object.FindFirstObjectByType<GameManager>();
            if (game == null)
            {
                var go = new GameObject("GameManager");
                SceneManager.MoveGameObjectToScene(go, scene);
                go.AddComponent<GameManager>();
            }
            else if (game.transform.parent != null)
            {
                game.transform.SetParent(null, true);
            }

            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            if (levelManager == null)
            {
                var go = new GameObject("LevelManager");
                SceneManager.MoveGameObjectToScene(go, scene);
                levelManager = go.AddComponent<LevelManager>();
            }
            else if (levelManager.transform.parent != null)
            {
                levelManager.transform.SetParent(null, true);
            }

            var serialized = new SerializedObject(levelManager);
            SerializedProperty levels = serialized.FindProperty("_levels");
            if (levels.arraySize == 0 && defaultLevel != null)
            {
                levels.arraySize = 1;
                levels.GetArrayElementAtIndex(0).objectReferenceValue = defaultLevel;
            }

            // SmokeTest owns initial spawn in SCN_Gameplay; avoid double LoadLevel on Start.
            serialized.FindProperty("_loadSavedLevelOnStart").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }

            return null;
        }

        private static void EnsurePrefabFolder()
        {
            const string prefabs = "Assets/PixelFlowClone/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabs))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(prefabs, "Gameplay");
        }
    }
}
