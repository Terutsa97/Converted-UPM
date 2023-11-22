using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEditor;

using UnityEngine;

namespace Terutsa97.GameObjectBrush
{
    [CreateAssetMenu(fileName = "New BrushCollection", menuName = "Tools/Gameobject Brush/Create BrushCollection")]
    public class BrushCollection : ScriptableObject
    {
        static string[] s_guids;

        public List<BrushObject> Brushes = new();

        // Currently selected/viewed brush (has to be public in order to be accessed by the FindProperty method)
        [HideInInspector] public BrushObject primarySelectedBrush = null;
        [HideInInspector] public List<BrushObject> selectedBrushes = new();

        [HideInInspector, NonSerialized] public List<GameObject> spawnedObjects = new();

        protected static string lastBrushCollection_EditPrefsKey = "GOB_LastUsedBrushCollection";
        public static string defaultBrushCollectionName = "Default Brush Collection";
        public static string newBrushCollectionName = "New Brush Collection";

        public static string s_defaultBrushPath = "GameObjectBrush Data";
        public string brushPath;

        /// <summary>
        /// Applies the spawned/cached objects of this brush collection
        /// </summary>
        public void ApplyCachedObjects()
            => spawnedObjects.Clear();

        /// <summary>
        /// Deletes all previously spawned objects
        /// </summary>
        public void DeleteSpawnedObjects()
        {
            foreach (GameObject obj in spawnedObjects) { DestroyImmediate(obj); }
            spawnedObjects.Clear();
        }

        /// <summary>
        /// Removes all brushes from this brush collection
        /// </summary>
        public void RemoveAllBrushes()
        {
            Brushes.Clear();
            primarySelectedBrush = null;
            selectedBrushes.Clear();
        }

        /// <summary>
        /// Saves the collection to disk
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Get the Asset GUID of this scriptable object
        /// </summary>
        /// <returns></returns>
        public string GetGUID() => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));

        /// <summary>
        /// Iterates over the list of current brushes and adds the name of each brush to a string.
        /// </summary>
        /// <returns></returns>
        public string GetCurrentBrushesString()
        {
            string str = "";
            foreach (BrushObject brush in selectedBrushes)
            {
                if (str != "")
                {
                    str += " ,";
                }
                str += brush.brushObject.name;
            }
            return str;
        }

        /// <summary>
        /// Get the greatest brush size value from the current brushes list
        /// </summary>
        /// <returns></returns>
        public float GetMaximumBrushSizeFromCurrentBrushes()
        {
            float maxBrushSize = 0f;
            foreach (BrushObject brush in selectedBrushes)
            {
                if (brush.brushSize > maxBrushSize)
                {
                    maxBrushSize = brush.brushSize;
                }
            }
            return maxBrushSize;
        }

        public float GetMinimumBrushSizeFromCurrentBrushes()
        {
            float minBrushSize = float.MaxValue;
            foreach (BrushObject brush in selectedBrushes)
            {
                if (brush.brushSize < minBrushSize)
                {
                    minBrushSize = brush.brushSize;
                }
            }

            return minBrushSize == float.MaxValue ? 0f : minBrushSize;
        }

        /// <summary>
        /// Gets the global index of this brush collection.
        /// The index refferres to the index of this brush collection in the array returned by "AssetDatabase.FindAssets("t:BrushCollection");"
        /// </summary>
        /// <returns></returns>
        public int GetIndex()
        {
            if (s_guids == null) { return 0; }

            var currentGUID = GetGUID();
            for (int i = 0; i < s_guids.Length; i++)
            {
                if (s_guids[i] == currentGUID)
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// Sets the last used brush collection to be this brush collection
        /// (this is done to save this collection as the currently open one)
        /// </summary>
        public void SetLastUsedBrushCollection()
            => EditorPrefs.SetString(lastBrushCollection_EditPrefsKey, GetGUID());

        /// <summary>
        /// Checks if an .asset with the same name already ecists in the default brush collection dir
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool CheckIfBrushCollectionAlreadyExists(string dir, string name)
        {
            string path = $"{dir}{name}.asset";
            return AssetDatabase.AssetPathToGUID(path) != "";
        }

        /// <summary>
        /// Returns the last used brush collection, if none it returns a new one
        /// </summary>
        /// <returns></returns>
        public static KeyValuePair<int, BrushCollection>? GetLastUsedBrushCollection()
        {
            //try to find the last used brush collection and return it
            if (EditorPrefs.HasKey(lastBrushCollection_EditPrefsKey))
            {
                string guid = EditorPrefs.GetString(lastBrushCollection_EditPrefsKey, "");
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BrushCollection lastUsedCollection = AssetDatabase.LoadAssetAtPath<BrushCollection>(path);

                //return found one or create one
                if (lastUsedCollection != null)
                {
                    return new(lastUsedCollection.GetIndex(), lastUsedCollection);
                }
            }

            return null;
            // Create one if none found
            //BrushCollection brushCollection = CreateInstance(s_defaultBrushPath, defaultBrushCollectionName);
            //return new(brushCollection.GetIndex(), brushCollection);
        }

        /// <summary>
        /// Gets all guids of all brushCollections present in this project
        /// </summary>
        /// <returns></returns>
        public static string[] GetAllBrushCollectionGUIDs()
            => s_guids = AssetDatabase.FindAssets("t:BrushCollection");

        /// <summary>
        /// Returns a struct of all BrushCollection assets found in this project
        /// </summary>
        /// <returns></returns>
        public static BrushCollectionList GetBrushCollectionsInProject()
            => new(GetAllBrushCollectionGUIDs());

        /// <summary>
        /// Creates a new BrushList asset
        /// </summary>
        /// <returns></returns>
        public static BrushCollection CreateInstance(string dir, string name)
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + dir))
            {
                AssetDatabase.CreateFolder("Assets", dir);
                AssetDatabase.SaveAssets();
            }

            string path = $"Assets/{dir}/{name}.asset";

            // Create asset
            BrushCollection collection = CreateInstance<BrushCollection>();
            AssetDatabase.CreateAsset(collection, path);
            AssetDatabase.SaveAssets();

            // Update last used one to be this collection (this is done to save this collection as the currently open one)
            collection.SetLastUsedBrushCollection();

            // Switch to newly created brush collection
            GameObjectBrushEditor.SwitchToBrushCollection(collection);
            collection.brushPath = dir;

            return collection;
        }

        public bool DeleteInstance()
        {
            string path = AssetDatabase.GUIDToAssetPath(GetGUID());
            return AssetDatabase.DeleteAsset(path);
        }

        /// <summary>
        /// Gets the class name as string
        /// </summary>
        /// <returns></returns>
        public static string GetClassName()
            => (typeof(BrushCollection).ToString() + ".cs")
            .Replace("GameObjectBrush.", "");

        public struct BrushCollectionList
        {
            public List<BrushCollection> brushCollections;

            public BrushCollectionList(string[] guids)
            {
                brushCollections = new List<BrushCollection>();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    brushCollections.Add(AssetDatabase.LoadAssetAtPath<BrushCollection>(path));
                }
            }

            /// <summary>
            /// Gets the name of each brush collection as an array
            /// </summary>
            /// <returns></returns>
            public string[] GetNameList()
            {
                var names = new List<string>();
                foreach (BrushCollection brush in brushCollections)
                {
                    if (brush != null)
                    {
                        names.Add(brush.name);
                    }
                    else
                    {
                        brushCollections.Remove(brush);
                    }
                }
                return names.ToArray();
            }
        }
    }
}