using System;
using PixelFlowClone.UI.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Applies a dedicated TMP outline material only to the requested UI.
    /// </summary>
    public static class TextBorderApplier
    {
        private const string MainMenuScenePath =
            "Assets/PixelFlowClone/Scenes/SCN_MainMenu.unity";
        private const string MainMenuFontPath =
            "Assets/PixelFlowClone/Art/Resources/Fonts/LilitaOne_SDF.asset";
        private const string OutlinedMaterialPath =
            "Assets/PixelFlowClone/Art/Resources/Fonts/LilitaOne_Outlined.mat";
        private const string LevelButtonPrefabPath =
            "Assets/PixelFlowClone/Prefabs/UI/PF_LevelButton.prefab";
        private const string GameplaySettingsPrefabPath =
            "Assets/PixelFlowClone/Prefabs/Resources/UI/PF_PausePopup.prefab";
        private const float OutlineWidth = 0.18f;
        private static readonly Color OutlineColor = new(0.04f, 0.04f, 0.06f, 1f);

        [MenuItem("PixelFlowClone/UI/Apply Requested Text Borders")]
        public static void ApplyRequestedTextBorders()
        {
            Material outlinedMaterial = EnsureOutlinedMaterial();
            if (outlinedMaterial == null)
                return;

            ApplyToPrefab(LevelButtonPrefabPath, outlinedMaterial, _ => true);
            ApplyToPrefab(
                GameplaySettingsPrefabPath,
                outlinedMaterial,
                text => text.name == "Title" &&
                        string.Equals(text.text.Trim(), "SETTINGS", StringComparison.OrdinalIgnoreCase));

            Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
            bool closeSceneAfterSave = !scene.isLoaded;
            if (closeSceneAfterSave)
                scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

            MainMenuScreen screen = FindMainMenuScreen(scene);
            if (screen != null && ApplyToTexts(
                    screen.GetComponentsInChildren<TMP_Text>(true),
                    outlinedMaterial,
                    _ => true))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (closeSceneAfterSave)
                EditorSceneManager.CloseScene(scene, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[PixelFlowClone] Text borders applied to SCN_MainMenu, PF_LevelButton, " +
                "and the gameplay Settings title.");
        }

        private static MainMenuScreen FindMainMenuScreen(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MainMenuScreen screen = root.GetComponentInChildren<MainMenuScreen>(true);
                if (screen != null)
                    return screen;
            }

            return null;
        }

        private static Material EnsureOutlinedMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(OutlinedMaterialPath);
            if (existing != null)
                return existing;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainMenuFontPath);
            if (font == null || font.material == null)
            {
                Debug.LogError(
                    $"[PixelFlowClone] TMP font/material was not found at {MainMenuFontPath}.");
                return null;
            }

            var material = new Material(font.material)
            {
                name = "LilitaOne_Outlined"
            };
            material.SetFloat("_OutlineWidth", OutlineWidth);
            material.SetColor("_OutlineColor", OutlineColor);
            AssetDatabase.CreateAsset(material, OutlinedMaterialPath);
            return material;
        }

        private static void ApplyToPrefab(
            string prefabPath,
            Material material,
            Func<TMP_Text, bool> predicate)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null)
            {
                Debug.LogWarning($"[PixelFlowClone] Prefab was not found: {prefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                if (ApplyToTexts(texts, material, predicate))
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool ApplyToTexts(
            TMP_Text[] texts,
            Material material,
            Func<TMP_Text, bool> predicate)
        {
            bool changed = false;
            foreach (TMP_Text text in texts)
            {
                if (text == null || !predicate(text) || text.fontSharedMaterial == material)
                    continue;

                text.fontSharedMaterial = material;
                EditorUtility.SetDirty(text);
                changed = true;
            }

            return changed;
        }
    }
}
