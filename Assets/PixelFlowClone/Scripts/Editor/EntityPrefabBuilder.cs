using System.IO;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds PF_PixelBlock and PF_CollectorUnit prefabs programmatically so GUIDs,
    /// component wiring and the shared square sprite are generated correctly by Unity.
    /// Run via menu: PixelFlowClone -> Build Entity Prefabs.
    /// </summary>
    public static class EntityPrefabBuilder
    {
        private const string SpriteDir = "Assets/PixelFlowClone/Art/Sprites";
        private const string SpritePath = SpriteDir + "/Square.png";
        private const string PrefabDir = "Assets/PixelFlowClone/Prefabs/Entities";
        private const string BlockPrefabPath = PrefabDir + "/PF_PixelBlock.prefab";
        private const string CollectorPrefabPath = PrefabDir + "/PF_CollectorUnit.prefab";

        [MenuItem("PixelFlowClone/Build Entity Prefabs")]
        public static void BuildEntityPrefabs()
        {
            Sprite square = EnsureSquareSprite();
            BuildBlockPrefab(square);
            BuildCollectorPrefab(square);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PixelFlowClone] Entity prefabs built: PF_PixelBlock, PF_CollectorUnit.");
        }

        private static Sprite EnsureSquareSprite()
        {
            if (!Directory.Exists(SpriteDir))
                Directory.CreateDirectory(SpriteDir);

            if (!File.Exists(SpritePath))
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply();

                File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static void BuildBlockPrefab(Sprite square)
        {
            EnsurePrefabFolder();

            var go = new GameObject("PF_PixelBlock");
            go.layer = PhysicsLayers.PixelBlock;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = Color.white;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.isTrigger = false;

            var block = go.AddComponent<PixelBlock>();
            var so = new SerializedObject(block);
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("_collider").objectReferenceValue = box;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, BlockPrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void BuildCollectorPrefab(Sprite square)
        {
            EnsurePrefabFolder();
            CleanupLeftover("PF_CollectorUnit");

            GameObject go = null;
            try
            {
                go = new GameObject("PF_CollectorUnit");
                go.layer = PhysicsLayers.Collector;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = square;
                sr.color = Color.white;
                sr.sortingOrder = 1;

                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = false;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;

                var col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one * 0.9f;
                col.isTrigger = true;

                var collector = go.AddComponent<CollectorUnit>();

                var labelGo = new GameObject("CapacityLabel");
                labelGo.transform.SetParent(go.transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);

                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.text = "0";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 6;
                tmp.color = Color.black;
                tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);
                tmp.sortingOrder = 2;

                TMP_FontAsset defaultFont = GetDefaultFontSafe();
                if (defaultFont != null)
                    tmp.font = defaultFont;

                var so = new SerializedObject(collector);
                so.FindProperty("_rigidbody").objectReferenceValue = rb;
                so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
                so.FindProperty("_capacityLabel").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(go, CollectorPrefabPath);
            }
            finally
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        private static TMP_FontAsset GetDefaultFontSafe()
        {
            try
            {
                return TMP_Settings.defaultFontAsset;
            }
            catch
            {
                return null;
            }
        }

        private static void CleanupLeftover(string objectName)
        {
            var leftover = GameObject.Find(objectName);
            if (leftover != null && !PrefabUtility.IsPartOfPrefabAsset(leftover))
                Object.DestroyImmediate(leftover);
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PixelFlowClone/Prefabs"))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone/Prefabs", "Entities");
        }
    }
}
