using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.ShortcutManagement;

using UnityEditorInternal;

using UnityEngine;

namespace Terutsa97.EditorUtilities
{
    public static class EditorUtilities
    {
        #region
        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.contextualPropertyMenu += Vector3ContextMenu;
            EditorApplication.contextualPropertyMenu += Vector2ContextMenu;
        }

        static void Vector2ContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Vector2) { return; }

            menu.AddItem(new GUIContent("Set => (0,0)"), false, () =>
            {
                property.vector2Value = Vector2.zero;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(new GUIContent("Set => (1,1)"), false, () =>
            {
                property.vector2Value = Vector2.one;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(new GUIContent("Invert"), false, () =>
            {
                property.vector2Value = -property.vector2Value;
                property.serializedObject.ApplyModifiedProperties();
            });
        }
        static void Vector3ContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Vector3) { return; }

            menu.AddItem(new GUIContent("Set => (0,0,0)"), false, () =>
            {
                property.vector3Value = Vector3.zero;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(new GUIContent("Set => (1,1,1)"), false, () =>
            {
                property.vector3Value = Vector3.one;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(new GUIContent("Invert"), false, () =>
            {
                property.vector3Value = -property.vector3Value;
                property.serializedObject.ApplyModifiedProperties();
            });
        }
        #endregion
    }
}
