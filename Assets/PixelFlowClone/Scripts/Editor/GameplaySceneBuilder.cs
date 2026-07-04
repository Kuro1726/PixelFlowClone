using System.Collections.Generic;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Builds SCN_Gameplay with a closed-loop conveyor path (8 waypoints) around Level_001 grid bounds.
    /// Menu: PixelFlowClone -> Build Gameplay Scene.
    /// </summary>
    public static class GameplaySceneBuilder
    {
        private const string ScenePath = "Assets/PixelFlowClone/Scenes/SCN_Gameplay.unity";
        private const string LevelPath = "Assets/PixelFlowClone/ScriptableObjects/Levels/Level_001.asset";
        private const float PathMargin = 1.5f;

        [MenuItem("PixelFlowClone/Build Gameplay Scene")]
        public static void BuildGameplayScene()
        {
            BuildGameplaySceneInternal();
        }

        public static void BuildGameplaySceneBatch()
        {
            BuildGameplaySceneInternal();
            EditorApplication.Exit(0);
        }

        private static void BuildGameplaySceneInternal()
        {
            EnsureSceneFolder();

            var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(LevelPath);
            if (level == null)
            {
                Debug.LogError($"[PixelFlowClone] Level not found at {LevelPath}. Run Create Default Data Assets first.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureCamera();
            BuildConveyorPath(level);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlowClone] Gameplay scene saved: {ScenePath} (8 waypoints around grid).");
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PixelFlowClone/Scenes"))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Scenes");
        }

        private static void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void BuildConveyorPath(LevelDataSO level)
        {
            IReadOnlyList<Vector2> positions = ComputeLoopPositions(level);

            var pathRoot = new GameObject("ConveyorPath");
            for (int i = 0; i < positions.Count; i++)
            {
                var go = new GameObject($"Waypoint_{i:D2}");
                go.transform.SetParent(pathRoot.transform, false);
                go.transform.position = new Vector3(positions[i].x, positions[i].y, 0f);

                var waypoint = go.AddComponent<ConveyorWaypoint>();
                var so = new SerializedObject(waypoint);
                so.FindProperty("_index").intValue = i;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// 8-point rectangle loop clockwise around the level grid (entry at index 0, bottom-left).
        /// </summary>
        public static IReadOnlyList<Vector2> ComputeLoopPositions(LevelDataSO level)
        {
            float minX = level.GridOrigin.x - PathMargin;
            float minY = level.GridOrigin.y - PathMargin;
            float maxX = level.GridOrigin.x + (level.GridSize.x - 1) * level.CellSpacing.x + PathMargin;
            float maxY = level.GridOrigin.y + (level.GridSize.y - 1) * level.CellSpacing.y + PathMargin;
            float midX = (minX + maxX) * 0.5f;
            float midY = (minY + maxY) * 0.5f;

            return new List<Vector2>
            {
                new(minX, minY),   // 0 entry — bottom-left
                new(midX, minY),   // 1 bottom center
                new(maxX, minY),   // 2 bottom-right
                new(maxX, midY),   // 3 right center
                new(maxX, maxY),   // 4 top-right
                new(midX, maxY),   // 5 top center
                new(minX, maxY),   // 6 top-left
                new(minX, midY)    // 7 left center
            };
        }
    }
}
