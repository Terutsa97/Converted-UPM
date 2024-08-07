using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Callbacks;

using UnityEngine;

using UnityHierarchyFolders.Runtime;

namespace Terutsa97.FolderUtilities
{
    public static class FolderUtilities
    {
        const string PARENT_FOLDER = "GameObject/Folder Utilities/";
        const int PRIORITY = 1;

        static int s_processedItemCount = 0;

        #region Convert Empty To Folder
        const string CONVERT_EMPTY_NAME = "Convert Empty To Folder";
        [MenuItem(PARENT_FOLDER + CONVERT_EMPTY_NAME, false, PRIORITY)]
        public static void ConvertEmptyToFolder()
        {
            var selectedParent = Selection.activeTransform;

            Undo.IncrementCurrentGroup();

            var newFolder = new GameObject(selectedParent.name);
            Undo.RegisterCreatedObjectUndo(newFolder, "Created New Folder");

            var index = Selection.activeTransform.GetSiblingIndex();

            if (selectedParent.parent != null)
            {
                Undo.SetTransformParent(newFolder.transform, selectedParent.parent, "Modify parent");
                ClearTransform(newFolder.transform);
            }

            newFolder.transform.SetSiblingIndex(index);

            while (selectedParent.childCount > 0)
            {
                var child = selectedParent.GetChild(0);
                Undo.SetTransformParent(child, newFolder.transform, $"Modify parent of: {child.name}");
            }

            newFolder.name = selectedParent.name;
            Undo.DestroyObjectImmediate(selectedParent.gameObject);
            Selection.activeTransform = newFolder.transform;

            Undo.AddComponent<Folder>(newFolder);

            Undo.SetCurrentGroupName("Reset Parent Transform");
        }

        [MenuItem(PARENT_FOLDER + CONVERT_EMPTY_NAME, true)]
        static bool Validate_ConvertEmptyToFolder()
            => Selection.activeTransform != null
            && Selection.activeTransform.childCount != 0
            && Selection.activeTransform.GetComponent<Folder>() == null;
        #endregion

        #region Convert Folder To Empty
        const string CONVERT_FOLDER_NAME = "Convert Folder To Empty";
        [MenuItem(PARENT_FOLDER + CONVERT_FOLDER_NAME, false, PRIORITY)]
        public static void ConvertFolderToEmpty()
        {
            var selectedParent = Selection.activeTransform;

            Undo.IncrementCurrentGroup();

            var newEmpty = new GameObject(selectedParent.name);
            Undo.RegisterCreatedObjectUndo(newEmpty, "Created New Empty");

            var index = Selection.activeTransform.GetSiblingIndex();

            if (selectedParent.parent != null)
            {
                Undo.SetTransformParent(newEmpty.transform, selectedParent.parent, "Modify parent");
                ClearTransform(newEmpty.transform);
            }

            newEmpty.transform.SetSiblingIndex(index);

            while (selectedParent.childCount > 0)
            {
                var child = selectedParent.GetChild(0);
                Undo.SetTransformParent(child, newEmpty.transform, $"Modify parent of: {child.name}");
            }

            newEmpty.name = selectedParent.name;
            Undo.DestroyObjectImmediate(selectedParent.gameObject);
            Selection.activeTransform = newEmpty.transform;

            Undo.SetCurrentGroupName("Reset Parent Transform");
        }

        [MenuItem(PARENT_FOLDER + CONVERT_FOLDER_NAME, true)]
        static bool Validate_ConvertFolderToEmpty()
            => Selection.activeTransform != null
            && Selection.activeTransform.GetComponent<Folder>() != null;
        #endregion

        #region Create Folder From Selection
        const string FOLDER_FROM_SELECTION = "Create A Folder From Selection";

        [MenuItem(PARENT_FOLDER + FOLDER_FROM_SELECTION, false, PRIORITY)]
        public static void CreateFolderFromSelection()
        {
            var selectedObjects = Selection.transforms;

            if (s_processedItemCount < selectedObjects.Length - 1)
            {
                s_processedItemCount++;
                return;
            }

            s_processedItemCount = 0;

            Undo.IncrementCurrentGroup();

            var newFolder = new GameObject("Folder");
            newFolder.AddComponent<Folder>();

            var commonParent = Selection.activeTransform.parent;
            var index = Selection.activeTransform.GetSiblingIndex();

            GameObjectUtility.SetParentAndAlign(newFolder, commonParent != null ? commonParent.gameObject : null);
            Undo.RegisterCreatedObjectUndo(newFolder, "Created New Folder");

            newFolder.transform.SetSiblingIndex(index);

            foreach (Transform child in selectedObjects)
            {
                Undo.SetTransformParent(child, newFolder.transform, "Modify parent of: " + child.name);
            }

            Undo.SetCurrentGroupName("Reset Parent Transform");
            Selection.activeObject = newFolder;
        }

        [MenuItem(PARENT_FOLDER + FOLDER_FROM_SELECTION, true)]
        static bool Validate_CreateFolderFromSelection()
            => Selection.transforms.Length != 0;
        #endregion

        #region Merge Folders Together
        const string MERGE_FOLDERS_NAME = "Merge Folders";

        [MenuItem(PARENT_FOLDER + MERGE_FOLDERS_NAME, false, PRIORITY)]
        public static void MergeFoldersTogether()
        {
            var selectedObjects = Selection.transforms;

            if (s_processedItemCount < selectedObjects.Length - 1)
            {
                s_processedItemCount++;
                return;
            }

            s_processedItemCount = 0;

            Undo.IncrementCurrentGroup();

            var newFolder = new GameObject("Folder");
            newFolder.AddComponent<Folder>();

            var commonParent = Selection.activeTransform.parent;
            var index = Selection.activeTransform.GetSiblingIndex();

            GameObjectUtility.SetParentAndAlign(newFolder, commonParent != null ? commonParent.gameObject : null);
            Undo.RegisterCreatedObjectUndo(newFolder, "Created New Folder");

            newFolder.transform.SetSiblingIndex(index);

            // Note: Reversed since removing from a list works wonky when starting from 0
            for (int i = selectedObjects.Length - 1; i >= 0; i--)
            {
                for (int j = selectedObjects[i].childCount - 1; j >= 0; j--)
                {
                    Transform child = selectedObjects[i].GetChild(j);
                    child.SetAsFirstSibling();
                    Undo.SetTransformParent(child, newFolder.transform, "Modify parent of: " + child.name);
                }

                Undo.DestroyObjectImmediate(selectedObjects[i].gameObject);
            }

            Undo.SetCurrentGroupName("Reset Parent Transform");
            Selection.activeObject = newFolder;
        }

        [MenuItem(PARENT_FOLDER + MERGE_FOLDERS_NAME, true)]
        static bool Validate_MergeFoldersTogether()
            => Selection.transforms.Length > 1 
            && Selection.transforms.All(s => s.GetComponent<Folder>() != null);
        #endregion

        #region Folder Sorting
        const string SORT_FOLDERS_NAME = "Sort Folders";

        [MenuItem(PARENT_FOLDER + SORT_FOLDERS_NAME + " [A-Z]", false, PRIORITY)]
        public static void SortFolderAZ()
            => SortWithAction((tl) => tl
                .OrderBy(t => t.name)
                .Reverse());

        [MenuItem(PARENT_FOLDER + SORT_FOLDERS_NAME + " [A-Z]", true)]
        static bool Validate_SortFolderAZ()
            => Selection.activeTransform != null
            && Selection.activeTransform.GetComponent<Folder>() != null;

        [MenuItem(PARENT_FOLDER + SORT_FOLDERS_NAME + " [Z-A]", false, PRIORITY)]
        public static void SortFolderZA()
            => SortWithAction((tl) => tl
                .OrderBy(t => t.name));

        [MenuItem(PARENT_FOLDER + SORT_FOLDERS_NAME + " [Z-A]", true)]
        static bool Validate_SortFolderZA()
            => Selection.activeTransform != null
            && Selection.activeTransform.GetComponent<Folder>() != null;

        static void SortWithAction(Func<IEnumerable<Transform>, IEnumerable<Transform>> action)
        {
            var selectedObjects = Selection.transforms;

            if (s_processedItemCount < selectedObjects.Length - 1)
            {
                s_processedItemCount++;
                return;
            }

            s_processedItemCount = 0;

            Undo.IncrementCurrentGroup();

            foreach (var selectedObject in selectedObjects)
            {
                var clonedList = new List<Transform>();
                foreach (Transform obj in selectedObject) { clonedList.Add(obj); }
                foreach (Transform obj in action(clonedList))
                {
                    Undo.SetSiblingIndex(obj, 0, "Move index for: " + obj.name);
                }
            }

            Undo.SetCurrentGroupName("Reset Parent Transform");
        }
        #endregion

        #region Project Window Folder utilities
        /// <summary>
        /// Taken from: https://www.youtube.com/watch?v=d7vsQ8AkpMY
        /// Adds the capability to shift double click on a folder in the project window to
        /// open it in the hierarchy
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId)
        {
            Event e = Event.current;
            if (e == null || !e.shift)
                return false;

            var obj = EditorUtility.InstanceIDToObject(instanceId);
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path)) { EditorUtility.RevealInFinder(path); }

            return true;
        }
        #endregion

        #region Helper Methods
        static void ClearTransform(Transform transform)
        {
            Undo.RegisterFullObjectHierarchyUndo(transform, transform.name + "Reset Parent Scale");
            transform.localScale = Vector3.one;

            Undo.RegisterFullObjectHierarchyUndo(transform, transform.name + "Reset Parent Position");
            transform.localPosition = Vector3.zero;

            Undo.RegisterFullObjectHierarchyUndo(transform, transform.name + "Reset Parent Rotation");
            transform.localRotation = Quaternion.identity;
        }
        #endregion
    }
}