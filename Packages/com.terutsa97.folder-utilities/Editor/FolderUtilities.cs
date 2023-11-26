using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using UnityEngine;

using UnityHierarchyFolders.Runtime;

public class FolderUtilities : ScriptableObject
{
    [MenuItem("GameObject/Folder Utilities/Convert Empty To Folder", false, 1)]
    public static void ConvertEmptyToFolder(MenuCommand command)
    {
        var selectedParent = ((GameObject)command.context).transform;
        var childTransforms = ((GameObject)command.context).GetComponentsInChildren<Transform>();

        if (selectedParent == null) { return; }

        if (selectedParent.childCount == 0) { return; }

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

        int i = 0;
        foreach (var child in childTransforms)
        {
            Undo.SetTransformParent(child, newFolder.transform, $"Modify parent of: {child.name} {i}");
            i++;
        }

        Undo.DestroyObjectImmediate(selectedParent.gameObject);
        Selection.activeTransform = newFolder.transform;

        Undo.AddComponent<Folder>(newFolder);

        Undo.SetCurrentGroupName("Reset Parent Transform");
    }

    private static int _processedItemCount = 0;

    [MenuItem("GameObject/Folder Utilities/Create Folder From Selection", false, 1)]
    public static void CreateFolderFromSelection()
    {
        var selectedObjects = Selection.transforms;

        if (selectedObjects.Length == 0) { return; }

        if (_processedItemCount < selectedObjects.Length - 1)
        {
            _processedItemCount++;
            return;
        }

        _processedItemCount = 0;

        Undo.IncrementCurrentGroup();

        var newFolder = new GameObject("Folder");
        newFolder.AddComponent<Folder>();

        var commonParent = Selection.activeTransform.parent.gameObject;
        var index = Selection.activeTransform.GetSiblingIndex();

        GameObjectUtility.SetParentAndAlign(newFolder, commonParent);
        Undo.RegisterCreatedObjectUndo(newFolder, "Created New Folder");

        newFolder.transform.SetSiblingIndex(index);

        foreach (Transform child in selectedObjects)
        {
            Undo.SetTransformParent(child, newFolder.transform, "Modify parent of: " + child.name);
        }

        Undo.SetCurrentGroupName("Reset Parent Transform");
        Selection.activeObject = newFolder;
    }

    //[MenuItem("GameObject/Folder Utilities/Merge Folders", false, 111)]
    public static void MergeFoldersTogether()
    {
        // TODO: Implement Logic

        //var selectedObjects = Selection.transforms;

        //if (selectedObjects.Length == 0) { return; }

        //if (_processedItemCount < selectedObjects.Length - 1)
        //{
        //    _processedItemCount++;
        //    return;
        //}

        //_processedItemCount = 0;

        //Undo.IncrementCurrentGroup();

        //var newFolder = new GameObject("Folder");
        //newFolder.AddComponent<Folder>();

        //var commonParent = Selection.activeTransform.parent.gameObject;
        //var index = Selection.activeTransform.GetSiblingIndex();

        //GameObjectUtility.SetParentAndAlign(newFolder, commonParent);
        //Undo.RegisterCreatedObjectUndo(newFolder, "Created New Folder");

        //newFolder.transform.SetSiblingIndex(index);

        //foreach (Transform child in selectedObjects)
        //{
        //    Undo.SetTransformParent(child, newFolder.transform, "Modify parent of: " + child.name);
        //}

        //Undo.SetCurrentGroupName("Reset Parent Transform");
        //Selection.activeObject = newFolder;
    }

    public static void ClearTransform(Transform transform)
    {
        Undo.RegisterFullObjectHierarchyUndo(transform, transform.name + "Reset Parent Scale");
        transform.localScale = Vector3.one;

        Undo.RegisterFullObjectHierarchyUndo(transform, transform.name + "Reset Parent Position");
        transform.localPosition = Vector3.zero;

        Undo.RegisterFullObjectHierarchyUndo(transform, transform.name + "Reset Parent Rotation");
        transform.localRotation = Quaternion.identity;
    }
}
