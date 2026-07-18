using System.Collections.Generic;
using PixelFlowClone.Conveyor;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using PixelFlowClone.Utils;
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
        private const string ConfigPath = "Assets/PixelFlowClone/ScriptableObjects/Config/GameConfig.asset";

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
            ConfigureCamera(level);
            Transform pathRoot = BuildConveyorPath(level);
            WireConveyorPathManager(pathRoot, level);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlowClone] Gameplay scene saved: {ScenePath} (8 waypoints around grid).");
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PixelFlowClone/Scenes"))
                AssetDatabase.CreateFolder("Assets/PixelFlowClone", "Scenes");
        }

        private static void ConfigureCamera(LevelDataSO level)
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            LevelLayout.FitCameraToLevel(camera, level);
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static Transform BuildConveyorPath(LevelDataSO level)
        {
            IReadOnlyList<Vector2> positions = LevelLayout.ComputeConveyorLoopPositions(level);

            var pathRoot = new GameObject("ConveyorPath");
            for (int i = 0; i < positions.Count; i++)
            {
                var go = new GameObject($"Waypoint_{i:D2}");
                go.transform.SetParent(pathRoot.transform, false);
                go.transform.position = new Vector3(positions[i].x, positions[i].y, 0f);

                var waypoint = go.AddComponent<ConveyorWaypoint>();
                waypoint.SetIndex(i);
            }

            return pathRoot.transform;
        }

        private static void WireConveyorPathManager(Transform pathRoot, LevelDataSO level)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath);
            var systems = new GameObject("GameplaySystems");
            var manager = systems.AddComponent<ConveyorPathManager>();

            var so = new SerializedObject(manager);
            so.FindProperty("_pathRoot").objectReferenceValue = pathRoot;
            so.FindProperty("_pathData").objectReferenceValue = level.PathReference;
            so.FindProperty("_config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static IReadOnlyList<Vector2> ComputeLoopPositions(LevelDataSO level)
        {
            return LevelLayout.ComputeConveyorLoopPositions(level);
        }
    }
}
