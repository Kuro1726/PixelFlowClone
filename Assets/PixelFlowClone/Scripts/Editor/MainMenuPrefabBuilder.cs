using PixelFlowClone.UI.Screens;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds PF_MainMenu prefab. Menu: PixelFlowClone → Build Main Menu Prefab
    /// </summary>
    public static class MainMenuPrefabBuilder
    {
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/PF_MainMenu.prefab";

        [MenuItem("PixelFlowClone/Build Main Menu Prefab")]
        public static void BuildMainMenuPrefab()
        {
            EnsurePrefabFolder();

            MainMenuScreen screen = MainMenuScreen.CreateRuntime();
            screen.name = "PF_MainMenu";

            PrefabUtility.SaveAsPrefabAsset(screen.gameObject, PrefabPath);
            Object.DestroyImmediate(screen.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlowClone] Main menu prefab saved: {PrefabPath}");
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
