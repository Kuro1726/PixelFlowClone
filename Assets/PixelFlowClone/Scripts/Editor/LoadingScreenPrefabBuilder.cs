using PixelFlowClone.UI.Screens;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds PF_LoadingScreen prefab (title + progress bar + "Loading...").
    /// Menu: PixelFlowClone → Build Loading Screen Prefab
    /// </summary>
    public static class LoadingScreenPrefabBuilder
    {
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/PF_LoadingScreen.prefab";

        [MenuItem("PixelFlowClone/Build Loading Screen Prefab")]
        public static void BuildLoadingScreenPrefab()
        {
            EnsurePrefabFolder();

            LoadingScreen screen = LoadingScreen.CreateRuntime();
            screen.name = "PF_LoadingScreen";

            PrefabUtility.SaveAsPrefabAsset(screen.gameObject, PrefabPath);
            Object.DestroyImmediate(screen.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlowClone] Loading screen prefab saved: {PrefabPath}");
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
