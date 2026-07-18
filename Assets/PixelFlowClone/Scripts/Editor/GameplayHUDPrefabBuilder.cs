using PixelFlowClone.Core;
using PixelFlowClone.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds PF_GameplayHUD and places it in SCN_Gameplay (P3-08).
    /// Menu: PixelFlowClone → Build Gameplay HUD Prefab
    /// </summary>
    public static class GameplayHUDPrefabBuilder
    {
        private const string ScenePath = "Assets/PixelFlowClone/Scenes/SCN_Gameplay.unity";
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/PF_GameplayHUD.prefab";

        [MenuItem("PixelFlowClone/Build Gameplay HUD Prefab")]
        public static void BuildGameplayHUDPrefab()
        {
            EnsurePrefabFolder();

            GameplayHUD hud = GameplayHUD.CreateRuntime();
            hud.name = "PF_GameplayHUD";

            // Force Awake-built UI before saving.
            hud.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(hud.gameObject, PrefabPath);
            Object.DestroyImmediate(hud.gameObject);

            PlaceHudInGameplayScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlowClone] Gameplay HUD prefab saved: {PrefabPath}");
        }

        public static void BuildGameplayHUDPrefabBatch()
        {
            BuildGameplayHUDPrefab();
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Ensures SCN_Gameplay has a PF_GameplayHUD instance (prefab instance preferred).
        /// </summary>
        [MenuItem("PixelFlowClone/Place Gameplay HUD In Scene")]
        public static void PlaceHudInGameplayScene()
        {
            EnsurePrefabFolder();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponent<GameplayHUD>() == null)
            {
                GameplayHUD temp = GameplayHUD.CreateRuntime();
                temp.name = "PF_GameplayHUD";
                temp.gameObject.SetActive(false);
                temp.gameObject.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(temp.gameObject, PrefabPath);
                Object.DestroyImmediate(temp.gameObject);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveExistingHudInstances(scene);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "PF_GameplayHUD";

            GameplayHUD hud = instance.GetComponent<GameplayHUD>();
            GameplayContext context = Object.FindFirstObjectByType<GameplayContext>();
            if (context != null && hud != null)
            {
                SerializedObject so = new SerializedObject(context);
                so.FindProperty("_hud").objectReferenceValue = hud;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(context);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PixelFlowClone] PF_GameplayHUD placed in {ScenePath}.");
        }

        private static void RemoveExistingHudInstances(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                    continue;

                if (root.name == "PF_GameplayHUD" || root.GetComponent<GameplayHUD>() != null)
                    Object.DestroyImmediate(root);
            }
        }

        private static void EnsurePrefabFolder()
        {
            const string prefabs = "Assets/PixelFlowClone/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabs))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(prefabs, "UI");
        }
    }
}
