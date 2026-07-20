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
        private const string PigLoadingPath =
            "Assets/PixelFlowClone/Art/UI/Loading/PigLoading_Cutout.png";
        private const string PigLoadingPurplePath =
            "Assets/PixelFlowClone/Art/UI/Loading/PigLoading_Purple_Cutout.png";
        private const string PigLoadingPurpleProjectilePath =
            "Assets/PixelFlowClone/Art/UI/Loading/PigLoading_PurpleProjectile_Cutout.png";

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
            AddPigLoading(
                screen.transform, PigLoadingPath, "PigLoading",
                new Vector2(0f, -20f), new Vector2(720f, 720f), 0f);
            AddPigLoading(
                screen.transform, PigLoadingPurplePath, "PigLoading_Purple",
                new Vector2(-270f, 180f), new Vector2(500f, 500f), -10f);
            AddPigLoading(
                screen.transform, PigLoadingPurpleProjectilePath, "PigLoading_PurpleProjectile",
                new Vector2(40f, 390f), new Vector2(520f, 520f), 0f);

            PrefabUtility.SaveAsPrefabAsset(screen.gameObject, PrefabPath);
            Object.DestroyImmediate(screen.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlowClone] Loading screen prefab saved: {PrefabPath}");
        }

        private static void AddPigLoading(
            Transform parent,
            string assetPath,
            string objectName,
            Vector2 position,
            Vector2 size,
            float rotation)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[PixelFlowClone] Loading sprite was not found: {assetPath}");
                return;
            }

            var pigObject = new GameObject(objectName, typeof(RectTransform));
            pigObject.transform.SetParent(parent, false);

            UnityEngine.UI.Image image = pigObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

            // Keep the pig between the logo and loading text by default.
            int statusIndex = parent.Find("Status")?.GetSiblingIndex() ?? parent.childCount;
            pigObject.transform.SetSiblingIndex(statusIndex);
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
