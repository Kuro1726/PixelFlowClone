using PixelFlowClone.Data;
using UnityEditor;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    /// <summary>Labels WaitingColumns array entries as Column 0, Column 1, ... in the Inspector.</summary>
    [CustomPropertyDrawer(typeof(CollectorSpawnColumn))]
    public class CollectorSpawnColumnDrawer : PropertyDrawer
    {
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.PropertyField(
                position,
                property,
                new GUIContent(GetColumnLabel(property.propertyPath)),
                true);
        }

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private static string GetColumnLabel(string propertyPath)
        {
            int open = propertyPath.LastIndexOf('[');
            int close = propertyPath.LastIndexOf(']');
            if (open >= 0 && close > open)
                return $"Column {propertyPath.Substring(open + 1, close - open - 1)}";

            return "Column";
        }
    }
}
