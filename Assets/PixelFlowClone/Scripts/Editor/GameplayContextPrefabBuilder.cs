using PixelFlowClone.Core;
using PixelFlowClone.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Converts the gameplay systems hierarchy into PF_GameplayContext and wires its manager references.
    /// </summary>
    public static class GameplayContextPrefabBuilder
    {
        private const string ScenePath = "Assets/PixelFlowClone/Scenes/SCN_Gameplay.unity";
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/Gameplay";
        private const string PrefabPath = PrefabFolder + "/PF_GameplayContext.prefab";

        [InitializeOnLoadMethod]
        private static void BuildMissingPrefabWhenGameplaySceneIsOpen()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                    return;

                if (SceneManager.GetActiveScene().path == ScenePath)
                    BuildGameplayContextPrefab();
            };
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

            GameplayContext context = root.GetComponent<GameplayContext>();
            if (context == null)
                context = root.AddComponent<GameplayContext>();

            context.Configure(grid, conveyor, queue, input);
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
