using PixelFlowClone.UI.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds an editable PF_MainMenu prefab and can bake the same hierarchy into SCN_MainMenu.
    /// </summary>
    public static class MainMenuPrefabBuilder
    {
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/PF_MainMenu.prefab";
        private const string LevelButtonPrefabPath = PrefabFolder + "/PF_LevelButton.prefab";
        private const string MainMenuScenePath = "Assets/PixelFlowClone/Scenes/SCN_MainMenu.unity";
        private const string BackgroundPath = "Assets/PixelFlowClone/Art/UI/MainMenu/MainMenuBackground.png";
        private const string PlayButtonPath = "Assets/PixelFlowClone/Art/UI/MainMenu/PlayButton.png";
        private const string LevelButtonPath = "Assets/PixelFlowClone/Art/UI/HUD/LevelTransparent.png";
        private const string MainMenuFontPath =
            "Assets/PixelFlowClone/Art/Resources/Fonts/LilitaOne_SDF.asset";
        private const string VisualCleanupSessionKey =
            "PixelFlowClone.MainMenu.VisualCleanup.20260721";

        [InitializeOnLoadMethod]
        private static void ScheduleVisualCleanup()
        {
            EditorApplication.delayCall += ApplyVisualCleanupToOpenMainMenuScene;
            EditorApplication.delayCall += EnsureLevelButtonPrefabForOpenScene;
        }

        private static void EnsureLevelButtonPrefabForOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Button levelButtonPrefab = EnsureLevelButtonPrefab();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != MainMenuScenePath)
                return;

            MainMenuScreen screen =
                Object.FindFirstObjectByType<MainMenuScreen>(FindObjectsInactive.Include);
            if (screen == null || screen.LevelButtonPrefab == levelButtonPrefab)
                return;

            Undo.RecordObject(screen, "Assign PF_LevelButton");
            screen.SetLevelButtonPrefab(levelButtonPrefab);
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PixelFlowClone] PF_LevelButton assigned to SCN_MainMenu.");
        }

        private static void ApplyVisualCleanupToOpenMainMenuScene()
        {
            if (SessionState.GetBool(VisualCleanupSessionKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != MainMenuScenePath)
                return;

            MainMenuScreen screen =
                Object.FindFirstObjectByType<MainMenuScreen>(FindObjectsInactive.Include);
            if (screen == null)
                return;

            SessionState.SetBool(VisualCleanupSessionKey, true);
            MakeScreenEditable(screen);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PixelFlowClone] Main Menu visual cleanup applied and scene saved.");
        }

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
            Sprite levelButton = AssetDatabase.LoadAssetAtPath<Sprite>(LevelButtonPath);
            screen.SetArtwork(background, playButton, levelButton);
            screen.SetLevelButtonPrefab(EnsureLevelButtonPrefab());
            EditorUtility.SetDirty(screen);
        }

        [MenuItem("PixelFlowClone/Main Menu/Create PF_LevelButton If Missing")]
        public static void CreateLevelButtonPrefabIfMissing()
        {
            Button button = EnsureLevelButtonPrefab();
            Selection.activeObject = button != null ? button.gameObject : null;
            if (button != null)
                EditorGUIUtility.PingObject(button.gameObject);
        }

        private static Button EnsureLevelButtonPrefab()
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(LevelButtonPrefabPath);
            if (existing != null)
                return existing.GetComponent<Button>();

            EnsurePrefabFolder();

            Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(LevelButtonPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainMenuFontPath);

            var root = new GameObject("PF_LevelButton", typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(280f, 110f);

            Image image = root.AddComponent<Image>();
            image.sprite = artwork;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.8f, 1f);
            colors.pressedColor = new Color(0.86f, 0.72f, 0.38f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.9f);
            button.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "LEVEL 1";
            label.fontSize = 36f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            if (font != null)
                label.font = font;

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(root, LevelButtonPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PixelFlowClone] Editable Level button prefab saved: {LevelButtonPrefabPath}");
            return saved != null ? saved.GetComponent<Button>() : null;
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
