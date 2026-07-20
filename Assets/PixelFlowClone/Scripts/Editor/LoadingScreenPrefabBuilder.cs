using PixelFlowClone.UI.Screens;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds PF_LoadingScreen prefab (artwork + title fallback + "Loading...").
    /// Menu: PixelFlowClone → Build Loading Screen Prefab
    /// </summary>
    public static class LoadingScreenPrefabBuilder
    {
        private const string PrefabRoot = "Assets/PixelFlowClone/Prefabs";
        private const string ResourceFolder = PrefabRoot + "/Resources";
        private const string PrefabFolder = ResourceFolder + "/UI";
        private const string PrefabPath = PrefabFolder + "/PF_LoadingScreen.prefab";

        [InitializeOnLoadMethod]
        private static void RegisterInitialPrefabBuild()
        {
            EditorApplication.delayCall += BuildIfMissing;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

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

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += BuildIfMissing;
        }

        private static void BuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                BuildLoadingScreenPrefab();
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder(PrefabRoot))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Prefabs");
            if (!AssetDatabase.IsValidFolder(ResourceFolder))
                AssetDatabase.CreateFolder(PrefabRoot, "Resources");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(ResourceFolder, "UI");
        }
    }
}
