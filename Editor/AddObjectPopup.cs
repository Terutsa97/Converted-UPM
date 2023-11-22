using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Terutsa97.GameObjectBrush
{
    /// <summary>
    /// Class that is responsible for the addition of new brushes to the list of brushObjects in the main editor windo class: "GameObjectBrushEditor"
    /// </summary>
    public class AddObjectPopup : EditorWindow
    {
        static string s_windowName = "Add brush";

        GameObject _obj2Add;

        public List<BrushObject> brushes;
        public EditorWindow parent;

        public static AddObjectPopup instance;

        // initialize the popup window
        public static void Init(List<BrushObject> brushes, EditorWindow parent)
        {
            // Check if a window is already open
            if (instance != null) { return; }

            // Create window
            instance = ScriptableObject.CreateInstance<AddObjectPopup>();

            // Cache the brushes from the main editor window for later use
            instance.brushes = brushes;
            // Cache the reference to the parent for later repaint
            instance.parent = parent;

            // Calculate window position (center of the parent window)
            float x = parent.position.x + (parent.position.width - 350) * 0.5f;
            float y = parent.position.y + (parent.position.height - 75) * 0.5f;
            instance.position = new Rect(x, y, 350, 75);

            // Show window as "utility"
            instance.ShowUtility();
            instance.name = s_windowName;
        }

        /// <summary>
        /// Creates the gui when called
        /// </summary>
        void OnGUI()
        {
            // Create the "title" label
            EditorGUILayout.LabelField("Add GameObject to Brushes", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Create the object field for the gameobject
            _obj2Add = (GameObject)EditorGUILayout.ObjectField("GameObject", _obj2Add, typeof(GameObject), false);

            // Make sure we have some nice (?) spacing and all button next to each other (horizontally)
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            // Close the popup
            GUI.backgroundColor = GameObjectBrushEditor.red;
            if (GUILayout.Button("Cancel")) { Close(); }

            // Adds the gameobject to the brushes from the main window and closes the popup
            GUI.backgroundColor = GameObjectBrushEditor.green;
            if (GUILayout.Button("Add"))
            {
                if (_obj2Add != null)
                {
                    brushes.Add(new BrushObject(_obj2Add));
                    parent.Repaint();
                }
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        void OnDestroy()
        {
            if (instance == this) { instance = null; }
        }
    }
}