using PixelFlowClone.UI.Popups;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>Creates editable Pause and Defeat popup prefabs used by GameplayContext.</summary>
    public static class PopupPrefabBuilder
    {
        private const string PrefabFolder = "Assets/PixelFlowClone/Prefabs/Resources/UI";
        private const string PausePrefabPath = PrefabFolder + "/PF_PausePopup.prefab";
        private const string DefeatPrefabPath = PrefabFolder + "/PF_DefeatPopup.prefab";

        [InitializeOnLoadMethod]
        private static void RegisterInitialBuild()
        {
            EditorApplication.delayCall += BuildMissingPrefabs;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += BuildMissingPrefabs;
        }

        [MenuItem("PixelFlowClone/UI/Rebuild Editable Pause and Defeat Prefabs")]
        public static void RebuildPrefabs()
        {
            BuildPausePrefab();
            BuildDefeatPrefab();
            SaveAndRefresh();
        }

        private static void BuildMissingPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool changed = false;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath) == null)
            {
                BuildPausePrefab();
                changed = true;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(DefeatPrefabPath) == null)
            {
                BuildDefeatPrefab();
                changed = true;
            }

            if (UpgradePausePrefabIfNeeded())
                changed = true;

            if (changed)
                SaveAndRefresh();
        }

        private static bool UpgradePausePrefabIfNeeded()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PausePrefabPath);
            try
            {
                PausePopup popup = contents.GetComponent<PausePopup>();
                if (popup == null ||
                    (popup.HasSettingsControls && popup.HasRestartArtwork &&
                     popup.HasHomeArtwork && popup.HasCloseArtwork && popup.UsesDirectHeaderEditing))
                    return false;

                popup.BuildEditableUi();
                popup.EnsureDirectHeaderEditing();
                PrefabUtility.SaveAsPrefabAsset(contents, PausePrefabPath);
                Debug.Log("[PixelFlowClone] Upgraded editable controls and button artwork in PF_PausePopup.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void BuildPausePrefab()
        {
            var root = new GameObject("PF_PausePopup", typeof(RectTransform));
            PausePopup popup = root.AddComponent<PausePopup>();
            popup.BuildEditableUi();
            PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[PixelFlowClone] Editable Pause popup saved: {PausePrefabPath}");
        }

        private static void BuildDefeatPrefab()
        {
            var root = new GameObject("PF_DefeatPopup", typeof(RectTransform));
            DefeatPopup popup = root.AddComponent<DefeatPopup>();
            popup.BuildEditableUi();
            PrefabUtility.SaveAsPrefabAsset(root, DefeatPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[PixelFlowClone] Editable Defeat popup saved: {DefeatPrefabPath}");
        }

        private static void SaveAndRefresh()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
