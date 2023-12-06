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
        GameObject _obj2Add;

        public List<BrushObject> brushes;
        public EditorWindow parent;

        public static AddObjectPopup instance;

        // initialize the popup window
        public static void Init(List<BrushObject> brushes, EditorWindow parent)
        {
            if (instance != null) { return; }

            instance = CreateInstance<AddObjectPopup>();

            instance.brushes = brushes;
            instance.parent = parent;

            var mousePos = Event.current.mousePosition;
            var heightOffset = EditorGUIUtility.singleLineHeight * 1.25f;

            instance.position = GUIUtility.GUIToScreenRect(new (mousePos.x - 185, mousePos.y - heightOffset/2, 200, 2 * heightOffset));
            instance.ShowPopup();
        }

        void OnGUI()
        {
            _obj2Add = (GameObject)EditorGUILayout.ObjectField(_obj2Add, typeof(GameObject), false);

            if (_obj2Add != null)
            {
                brushes.Add(new BrushObject(_obj2Add));
                parent.Repaint();

                Close();
            }

            if (GUILayout.Button("Close")) { Close(); }
        }

        void OnDestroy()
        {
            if (instance == this) { instance = null; }
        }
    }
}