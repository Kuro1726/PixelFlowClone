using PixelFlowClone.Data;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>
    /// Designer-friendly LevelData inspector. BlockMatrix remains a flat array at runtime,
    /// but is edited as a visual 2D grid here.
    /// </summary>
    [CustomEditor(typeof(LevelDataSO))]
    public class LevelDataSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _levelId;
        private SerializedProperty _levelName;
        private SerializedProperty _gridSize;
        private SerializedProperty _blockMatrix;
        private SerializedProperty _cellSpacing;
        private SerializedProperty _gridOrigin;
        private SerializedProperty _waitingColumns;
        private SerializedProperty _queueGapBelowPath;
        private SerializedProperty _waitingGapBelowQueue;
        private SerializedProperty _queueUnitSpacing;
        private SerializedProperty _waitingUnitSpacing;
        private SerializedProperty _waitingColumnSpacing;
        private SerializedProperty _pathReference;

        private Vector2Int _previousGridSize;
        private bool _showBlockGrid = true;

        private void OnEnable()
        {
            _levelId = serializedObject.FindProperty("LevelId");
            _levelName = serializedObject.FindProperty("LevelName");
            _gridSize = serializedObject.FindProperty("GridSize");
            _blockMatrix = serializedObject.FindProperty("BlockMatrix");
            _cellSpacing = serializedObject.FindProperty("CellSpacing");
            _gridOrigin = serializedObject.FindProperty("GridOrigin");
            _waitingColumns = serializedObject.FindProperty("WaitingColumns");
            _queueGapBelowPath = serializedObject.FindProperty("QueueGapBelowPath");
            _waitingGapBelowQueue = serializedObject.FindProperty("WaitingGapBelowQueue");
            _queueUnitSpacing = serializedObject.FindProperty("QueueUnitSpacing");
            _waitingUnitSpacing = serializedObject.FindProperty("WaitingUnitSpacing");
            _waitingColumnSpacing = serializedObject.FindProperty("WaitingColumnSpacing");
            _pathReference = serializedObject.FindProperty("PathReference");
            _previousGridSize = ((LevelDataSO)target).GridSize;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawIdentity();
            EditorGUILayout.Space();
            DrawPixelGrid();
            EditorGUILayout.Space();
            DrawCollectors();
            EditorGUILayout.Space();
            DrawLayout();
            EditorGUILayout.Space();
            DrawConveyor();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdentity()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_levelId);
            EditorGUILayout.PropertyField(_levelName);
        }

        private void DrawPixelGrid()
        {
            EditorGUILayout.LabelField("Pixel Grid", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_gridSize);
            if (EditorGUI.EndChangeCheck())
            {
                Vector2Int next = _gridSize.vector2IntValue;
                next.x = Mathf.Max(1, next.x);
                next.y = Mathf.Max(1, next.y);
                _gridSize.vector2IntValue = next;
                ResizeMatrix(_previousGridSize, next);
                _previousGridSize = next;
            }

            EnsureMatrixSize();

            _showBlockGrid = EditorGUILayout.Foldout(
                _showBlockGrid,
                $"Block Grid ({_gridSize.vector2IntValue.x} × {_gridSize.vector2IntValue.y})",
                true);

            if (_showBlockGrid)
            {
                EditorGUILayout.HelpBox(
                    "Top row is highest Y. Click a cell to choose its color.",
                    MessageType.Info);
                DrawBlockGrid();
            }

            EditorGUILayout.PropertyField(_cellSpacing);
            EditorGUILayout.PropertyField(_gridOrigin);
        }

        private void DrawBlockGrid()
        {
            Vector2Int size = _gridSize.vector2IntValue;
            float availableWidth = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 55f);
            float cellSize = Mathf.Clamp(availableWidth / size.x, 18f, 42f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24f);
            for (int x = 0; x < size.x; x++)
            {
                GUILayout.Label(
                    x.ToString(),
                    EditorStyles.centeredGreyMiniLabel,
                    GUILayout.Width(cellSize));
            }
            EditorGUILayout.EndHorizontal();

            for (int y = size.y - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(
                    y.ToString(),
                    EditorStyles.centeredGreyMiniLabel,
                    GUILayout.Width(24f),
                    GUILayout.Height(cellSize));

                for (int x = 0; x < size.x; x++)
                    DrawCell(x, y, size.x, cellSize);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCell(int x, int y, int width, float size)
        {
            int index = y * width + x;
            SerializedProperty cell = _blockMatrix.GetArrayElementAtIndex(index);
            var colorId = (ColorId)cell.enumValueIndex;

            Color previousBackground = GUI.backgroundColor;
            Color previousContent = GUI.contentColor;
            GUI.backgroundColor = colorId == ColorId.None
                ? new Color(0.35f, 0.35f, 0.35f)
                : ColorPalette.ToColor(colorId);
            GUI.contentColor = colorId == ColorId.Yellow ? Color.black : Color.white;

            string shortName = GetShortName(colorId);
            if (GUILayout.Button(
                    new GUIContent(shortName, $"({x}, {y}) {colorId}"),
                    EditorStyles.miniButton,
                    GUILayout.Width(size),
                    GUILayout.Height(size)))
            {
                ShowColorMenu(index);
            }

            GUI.backgroundColor = previousBackground;
            GUI.contentColor = previousContent;
        }

        private void ShowColorMenu(int matrixIndex)
        {
            var menu = new GenericMenu();
            var level = (LevelDataSO)target;

            foreach (ColorId color in System.Enum.GetValues(typeof(ColorId)))
            {
                ColorId selected = color;
                bool isCurrent =
                    level.BlockMatrix != null &&
                    matrixIndex < level.BlockMatrix.Length &&
                    level.BlockMatrix[matrixIndex] == selected;

                menu.AddItem(new GUIContent(selected.ToString()), isCurrent, () =>
                {
                    Undo.RecordObject(level, "Change Block Color");
                    level.BlockMatrix[matrixIndex] = selected;
                    EditorUtility.SetDirty(level);
                    serializedObject.Update();
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        private void EnsureMatrixSize()
        {
            Vector2Int size = _gridSize.vector2IntValue;
            int expected = size.x * size.y;
            if (_blockMatrix.arraySize != expected)
                ResizeMatrix(size, size);
        }

        private void ResizeMatrix(Vector2Int oldSize, Vector2Int newSize)
        {
            int oldCount = _blockMatrix.arraySize;
            var oldValues = new int[oldCount];
            for (int i = 0; i < oldCount; i++)
                oldValues[i] = _blockMatrix.GetArrayElementAtIndex(i).enumValueIndex;

            _blockMatrix.arraySize = newSize.x * newSize.y;
            for (int y = 0; y < newSize.y; y++)
            {
                for (int x = 0; x < newSize.x; x++)
                {
                    int value = 0;
                    if (x < oldSize.x && y < oldSize.y)
                    {
                        int oldIndex = y * oldSize.x + x;
                        if (oldIndex >= 0 && oldIndex < oldValues.Length)
                            value = oldValues[oldIndex];
                    }

                    _blockMatrix.GetArrayElementAtIndex(y * newSize.x + x).enumValueIndex = value;
                }
            }
        }

        private void DrawCollectors()
        {
            EditorGUILayout.LabelField("Collectors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_waitingColumns, true);
        }

        private void DrawLayout()
        {
            EditorGUILayout.LabelField("Layout (Queue / Waiting)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "≤0 uses scene defaults (~1.1). Unit Spacing = gap between collectors in the same row/column.",
                MessageType.None);
            EditorGUILayout.PropertyField(_queueGapBelowPath, new GUIContent("Queue Gap Below Path"));
            EditorGUILayout.PropertyField(_waitingGapBelowQueue, new GUIContent("Waiting Gap Below Queue"));
            EditorGUILayout.PropertyField(_queueUnitSpacing, new GUIContent("Queue Unit Spacing"));
            EditorGUILayout.PropertyField(_waitingUnitSpacing, new GUIContent("Waiting Unit Spacing"));
            EditorGUILayout.PropertyField(_waitingColumnSpacing, new GUIContent("Waiting Column Spacing"));
        }

        private void DrawConveyor()
        {
            EditorGUILayout.LabelField("Conveyor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_pathReference);
        }

        private static string GetShortName(ColorId color)
        {
            return color switch
            {
                ColorId.None => "·",
                ColorId.Purple => "Pu",
                ColorId.Orange => "O",
                ColorId.Yellow => "Y",
                _ => color.ToString().Substring(0, 1)
            };
        }
    }
}
