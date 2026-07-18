using PixelFlowClone.Conveyor;
using PixelFlowClone.Data;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    public static class DefaultDataAssetCreator
    {
        private const string ConfigPath = "Assets/PixelFlowClone/ScriptableObjects/Config/GameConfig.asset";
        private const string PathAssetPath = "Assets/PixelFlowClone/ScriptableObjects/Paths/ConveyorPath_Level01.asset";
        private const string Level001Path = "Assets/PixelFlowClone/ScriptableObjects/Levels/Level_001.asset";

        [MenuItem("PixelFlowClone/Create Default Data Assets")]
        public static void CreateDefaultAssets()
        {
            EnsureFolders();
            var config = CreateOrLoadGameConfig();
            var path = CreateOrLoadConveyorPath();
            CreateOrLoadLevel001(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PixelFlowClone] Default data assets created/updated.");
        }

        public static void CreateDefaultAssetsBatch()
        {
            CreateDefaultAssets();
            EditorApplication.Exit(0);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/PixelFlowClone/ScriptableObjects");
            EnsureFolder("Assets/PixelFlowClone/ScriptableObjects/Config");
            EnsureFolder("Assets/PixelFlowClone/ScriptableObjects/Paths");
            EnsureFolder("Assets/PixelFlowClone/ScriptableObjects/Levels");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int lastSlash = path.LastIndexOf('/');
            string parent = path[..lastSlash];
            string folderName = path[(lastSlash + 1)..];
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static GameConfigSO CreateOrLoadGameConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath);
            if (existing != null)
                return existing;

            var config = ScriptableObject.CreateInstance<GameConfigSO>();
            config.MaxConveyorUnits = 5;
            config.MaxQueueSlots = 5;
            config.CollectorMoveSpeed = 3f;
            config.LapCompleteEpsilon = 0.05f;
            config.EndgameCollectorThreshold = 5;
            config.EndgameMoveSpeedMultiplier = 1.75f;
            config.EndgameSkipQueueOnLap = true;
            config.RaycastDistance = 2f;
            config.PixelBlockLayer = PhysicsLayers.GetLayerMask(PhysicsLayers.PixelBlock);
            config.RaycastSide = PerpendicularSide.Inward;
            config.TapCooldownSeconds = 0.15f;
            config.CollectorPoolPrewarm = 20;
            config.BlockPoolPrewarm = 100;

            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static ConveyorPathSO CreateOrLoadConveyorPath()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ConveyorPathSO>(PathAssetPath);
            if (existing != null)
                return existing;

            var path = ScriptableObject.CreateInstance<ConveyorPathSO>();
            path.EntryWaypointIndex = 0;
            path.MoveSpeed = 0f;

            AssetDatabase.CreateAsset(path, PathAssetPath);
            return path;
        }

        private static LevelDataSO CreateOrLoadLevel001(GameConfigSO config, ConveyorPathSO path)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(Level001Path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDataSO>();
                AssetDatabase.CreateAsset(level, Level001Path);
            }

            level.LevelId = 1;
            level.LevelName = "Level 001";
            level.GridSize = new Vector2Int(5, 5);
            level.CellSpacing = new Vector2(1f, 1f);
            level.GridOrigin = new Vector2(-2f, -2f);
            level.BlockMatrix = CreateLevel001BlockMatrix();
            level.WaitingColumns = new[]
            {
                new CollectorSpawnColumn
                {
                    Collectors = new[]
                    {
                        new CollectorSpawnEntry { Color = ColorId.Blue, InitialCapacity = 8 },
                        new CollectorSpawnEntry { Color = ColorId.Red, InitialCapacity = 10 }
                    }
                },
                new CollectorSpawnColumn
                {
                    Collectors = new[]
                    {
                        new CollectorSpawnEntry { Color = ColorId.Red, InitialCapacity = 5 }
                    }
                }
            };
            level.PathReference = path;

            EditorUtility.SetDirty(level);
            _ = config;
            return level;
        }

        /// <summary>
        /// 5x5 row-major matrix. Pattern: red cross center + blue corners (2 colors).
        /// y=0 bottom row, y=4 top row.
        /// </summary>
        private static ColorId[] CreateLevel001BlockMatrix()
        {
            const int size = 5;
            var matrix = new ColorId[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isRedCross = x == 2 && y is >= 1 and <= 3
                                      || y == 2 && x is >= 1 and <= 3;
                    bool isBlueCorner = (x == 1 && y == 1) || (x == 3 && y == 3);

                    matrix[y * size + x] = isRedCross ? ColorId.Red
                        : isBlueCorner ? ColorId.Blue
                        : ColorId.None;
                }
            }

            return matrix;
        }
    }
}
