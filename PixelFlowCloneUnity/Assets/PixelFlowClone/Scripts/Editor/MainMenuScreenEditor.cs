using PixelFlowClone.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelFlowClone.Editor
{
    [CustomEditor(typeof(MainMenuScreen))]
    public sealed class MainMenuScreenEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);

            if (GUILayout.Button("Make Main Menu Editable", GUILayout.Height(34f)))
            {
                MainMenuScreen screen = (MainMenuScreen)target;
                Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Make Main Menu Editable");
                screen.BuildEditableUi();
                EditorUtility.SetDirty(screen);
                EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
                Selection.activeGameObject = screen.gameObject;
                Debug.Log("[MainMenu] Editable UI hierarchy created. Save SCN_MainMenu (Ctrl+S).", screen);
            }

            EditorGUILayout.HelpBox(
                "Creates Canvas, buttons, Level Select and Settings Popup as editable scene objects.",
                MessageType.Info);
        }
    }
}
