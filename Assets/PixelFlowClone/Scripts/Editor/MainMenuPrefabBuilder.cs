using PixelFlowClone.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds an editable PF_MainMenu prefab and can bake the same hierarchy into SCN_MainMenu.
    /// </summary>
    public static class MainMenuPrefabBuilder
    {
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/PF_MainMenu.prefab";
        private const string MainMenuScenePath = "Assets/PixelFlowClone/Scenes/SCN_MainMenu.unity";
        private const string BackgroundPath = "Assets/PixelFlowClone/Art/UI/MainMenu/MainMenuBackground.png";
        private const string PlayButtonPath = "Assets/PixelFlowClone/Art/UI/MainMenu/PlayButton.png";

        // Keep the original menu path so existing project instructions still work.
        [MenuItem("PixelFlowClone/Build Main Menu Prefab")]
        [MenuItem("PixelFlowClone/Main Menu/Build Editable PF_MainMenu Prefab")]
        public static void BuildMainMenuPrefab()
        {
            EnsurePrefabFolder();

            MainMenuScreen screen = MainMenuScreen.CreateRuntime();
            screen.name = "PF_MainMenu";
            AssignArtwork(screen);
            screen.BuildEditableUi();

            PrefabUtility.SaveAsPrefabAsset(screen.gameObject, PrefabPath);
            Object.DestroyImmediate(screen.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlowClone] Editable main menu prefab saved: {PrefabPath}");
        }

        [MenuItem("PixelFlowClone/Make SCN_MainMenu Editable", priority = 1)]
        [MenuItem("PixelFlowClone/Main Menu/Make SCN_MainMenu Editable")]
        public static void MakeMainMenuSceneEditable()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainMenuScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            }

            MainMenuScreen screen = Object.FindFirstObjectByType<MainMenuScreen>(FindObjectsInactive.Include);
            if (screen == null)
            {
                Debug.LogError($"[PixelFlowClone] MainMenuScreen was not found in {MainMenuScenePath}.");
                return;
            }

            MakeScreenEditable(screen);
        }

        public static void MakeScreenEditable(MainMenuScreen screen)
        {
            if (screen == null)
                return;

            Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Make Main Menu Editable");
            AssignArtwork(screen);
            screen.BuildEditableUi();

            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
            Selection.activeGameObject = screen.gameObject;
            EditorGUIUtility.PingObject(screen.gameObject);

            Debug.Log("[PixelFlowClone] SCN_MainMenu UI is editable in the Hierarchy. Review it and save the scene (Ctrl+S).");
        }

        private static void AssignArtwork(MainMenuScreen screen)
        {
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            Sprite playButton = AssetDatabase.LoadAssetAtPath<Sprite>(PlayButtonPath);
            screen.SetArtwork(background, playButton);
            EditorUtility.SetDirty(screen);
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
