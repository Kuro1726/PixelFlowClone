using PixelFlowClone.Core;
using PixelFlowClone.UI.Popups;
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
        private const string PauseIconPath = "Assets/PixelFlowClone/Art/UI/HUD/SettingTransparent.png";
        private const string LevelBadgePath = "Assets/PixelFlowClone/Art/UI/HUD/LevelTransparent.png";
        private const string VictoryPanelPath = "Assets/PixelFlowClone/Art/UI/Popups/Victory/VictoryPanel.png";
        private const string VictoryTitleBannerPath = "Assets/PixelFlowClone/Art/UI/Popups/Victory/TitleBanner.png";
        private const string VictoryTrophyPath = "Assets/PixelFlowClone/Art/UI/Popups/Victory/Trophy.png";
        private const string VictoryTrophyWingPath = "Assets/PixelFlowClone/Art/UI/Popups/Victory/TrophyWing.png";
        private const string VictoryContinueButtonPath = "Assets/PixelFlowClone/Art/UI/Popups/Victory/ContinueButton.png";

        [InitializeOnLoadMethod]
        private static void BuildEditableHudWhenMissing()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += BuildEditableHudIfNeeded;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += BuildEditableHudIfNeeded;
        }

        private static void BuildEditableHudIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                VictoryPopup popup = prefab.GetComponentInChildren<VictoryPopup>(true);
                if (popup != null)
                {
                    if (popup.NeedsPresentationUpgrade)
                        UpgradeVictoryPopupPresentation();
                    return;
                }
            }

            BuildGameplayHUDPrefab();
        }

        private static void UpgradeVictoryPopupPresentation()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                VictoryPopup popup = contents.GetComponentInChildren<VictoryPopup>(true);
                if (popup == null)
                    return;

                popup.BuildForEditor();
                EditorUtility.SetDirty(popup);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[PixelFlowClone] Victory Popup presentation upgraded.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [MenuItem("PixelFlowClone/Build Gameplay HUD Prefab")]
        public static void BuildGameplayHUDPrefab()
        {
            EnsurePrefabFolder();

            GameplayHUD hud = CreateEditableHud();

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
            if (prefab == null ||
                prefab.GetComponent<GameplayHUD>() == null ||
                prefab.GetComponentInChildren<VictoryPopup>(true) == null)
            {
                GameplayHUD temp = CreateEditableHud();
                PrefabUtility.SaveAsPrefabAsset(temp.gameObject, PrefabPath);
                Object.DestroyImmediate(temp.gameObject);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            RemoveExistingHudInstances(scene);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "PF_GameplayHUD";

            GameplayHUD hud = instance.GetComponent<GameplayHUD>();
            if (hud != null)
                AssignHudSprites(hud);

            GameplayContext context = FindInScene<GameplayContext>(scene);
            if (context != null && hud != null)
            {
                SerializedObject so = new SerializedObject(context);
                so.FindProperty("_hud").objectReferenceValue = hud;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(context);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            if (openedTemporarily)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }

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

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }

        private static void EnsurePrefabFolder()
        {
            const string prefabs = "Assets/PixelFlowClone/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabs))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(prefabs, "UI");
        }

        private static void AssignHudSprites(GameplayHUD hud)
        {
            Sprite pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>(PauseIconPath);
            Sprite levelBadge = AssetDatabase.LoadAssetAtPath<Sprite>(LevelBadgePath);
            if (pauseIcon == null)
                Debug.LogWarning($"[PixelFlowClone] Pause icon not found: {PauseIconPath}");
            if (levelBadge == null)
                Debug.LogWarning($"[PixelFlowClone] Level badge not found: {LevelBadgePath}");

            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("_pauseIcon").objectReferenceValue = pauseIcon;
            so.FindProperty("_levelBadgeSprite").objectReferenceValue = levelBadge;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameplayHUD CreateEditableHud()
        {
            GameplayHUD hud = GameplayHUD.CreateRuntime();
            hud.name = "PF_GameplayHUD";
            AssignHudSprites(hud);
            hud.BuildForEditor();
            CreateVictoryPopup(hud.transform);
            return hud;
        }

        private static void CreateVictoryPopup(Transform parent)
        {
            VictoryPopup popup = VictoryPopup.CreateRuntime(
                parent,
                LoadSprite(VictoryPanelPath, "Victory panel"),
                LoadSprite(VictoryTitleBannerPath, "Victory title banner"),
                LoadSprite(VictoryTrophyPath, "Victory trophy"),
                LoadSprite(VictoryTrophyWingPath, "Victory trophy wing"),
                LoadSprite(VictoryContinueButtonPath, "Victory continue button"));
            popup.name = "VictoryPopup";
            popup.BuildForEditor();
            popup.gameObject.SetActive(true);
        }

        private static Sprite LoadSprite(string path, string label)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[PixelFlowClone] {label} not found: {path}");
            return sprite;
        }
    }
}
