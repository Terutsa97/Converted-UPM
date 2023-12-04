using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace Terutsa97.GameObjectBrush
{
    /// <summary>
    /// The main class of this extension/tool that handles the ui and the brush/paint functionality
    /// </summary>
    public class GameObjectBrushEditor : EditorWindow
    {
        #region Properties
        static BrushCollection.BrushCollectionList? s_brushCollectionList;

        static string version = "v4.0.1";

        public static Color red = ColorFromRGB(239, 80, 80);
        public static Color green = ColorFromRGB(93, 173, 57);
        public static Color yellow = ColorFromRGB(237, 199, 61);
        public static Color SelectedColor = ColorFromRGB(139, 150, 165);
        public static Color PrimarySelectedColor = ColorFromRGB(10, 153, 220);

        //some utility vars used to determine if the editor window is open
        public static GameObjectBrushEditor Instance { get; private set; }
        public static bool IsOpen => Instance != null;

        public BrushCollection brushes;
        public int selectedBrushCollectionIndex = 0;

        float _zoomValue = 100;

        Vector2 _scrollViewScrollPosition = new();
        BrushObject _copy = null;

        bool _isErasingEnabled = true;
        bool _isPlacingEnabled = true;

        Dictionary<GameObject, Vector3> _lastPlacementPositions = new();
        #endregion

        #region Editor Window Functionality

        [MenuItem("Tools/GameObject Brush")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            DontDestroyOnLoad(EditorWindow.GetWindow<GameObjectBrushEditor>("GO Brush " + version));
        }

        void OnEnable()
        {
            //Get last used brush collection
            KeyValuePair<int, BrushCollection>? lastUsedBCollInfo = BrushCollection.GetLastUsedBrushCollection();

            //add theme delegate
#if UNITY_2021_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
            Instance = this;
            this.autoRepaintOnSceneChange = true;

            if (lastUsedBCollInfo != null)
            {
                selectedBrushCollectionIndex = lastUsedBCollInfo.Value.Key;
                brushes = lastUsedBCollInfo.Value.Value;
            }
        }

        void OnDestroy()
        {
            if (brushes != null)
            {
                brushes.Save();
            }
            //remove the delegate
#if UNITY_2021_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
        }

        void OnGUI()
        {
            if (Application.isPlaying) { return; }

            if (Tools.current != Tool.None)
            {
                EditorGUILayout.LabelField("Click on window to enable this tool.", EditorStyles.boldLabel);
                return;
            }

            SerializedObject serializedObject_brushObject = null;
            int prevSelectedBrushCollectionIndex = selectedBrushCollectionIndex;
            if (brushes != null)
            {
                serializedObject_brushObject = new SerializedObject(brushes);
            }
            EditorGUIUtility.wideMode = true;

            #region Header
            if (s_brushCollectionList is null)
            {
                EditorGUILayout.LabelField("No Brush collections found.", EditorStyles.boldLabel);
                s_brushCollectionList = BrushCollection.GetBrushCollectionsInProject();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Collection:", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh"))
            {
                s_brushCollectionList = BrushCollection.GetBrushCollectionsInProject();
            }

            var brushCollectionList = s_brushCollectionList.Value;

            if (brushCollectionList.brushCollections.Count == 0)
            {
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Brush Collection"))
                {
                    StringPopupWindow.Init(this, CreateInstance, BrushCollection.newBrushCollectionName, "Create new BrushCollection", "Asset Name");
                }

                EditorGUILayout.EndHorizontal();
                return;
            }

            var brushNameList = brushCollectionList.GetNameList();
            if (brushNameList.Length == 0)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }

            selectedBrushCollectionIndex = EditorGUILayout.Popup(selectedBrushCollectionIndex, brushNameList);
            if (prevSelectedBrushCollectionIndex != selectedBrushCollectionIndex)
            {
                //select only when brush collection changed
                brushes = brushCollectionList.brushCollections[selectedBrushCollectionIndex];
                brushes.SetLastUsedBrushCollection();
            }

            if (GUILayout.Button("+"))
            {
                StringPopupWindow.Init(this, CreateInstance, BrushCollection.newBrushCollectionName, "Create new BrushCollection", "Asset Name");
            }

            if (GUILayout.Button("-"))
            {
                brushCollectionList.brushCollections.Remove(brushes);
                brushes.DeleteInstance();

                if (brushCollectionList.brushCollections.Count == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                brushes = brushCollectionList.brushCollections[selectedBrushCollectionIndex];
                brushes.SetLastUsedBrushCollection();
            }
            EditorGUILayout.EndHorizontal();

            //The active BrushList asset
            //brushes = EditorGUILayout.ObjectField(brushes, typeof(Object), true) as BrushCollection;
            #endregion

            #region Scroll view
            //scroll view
            _scrollViewScrollPosition = EditorGUILayout.BeginScrollView(_scrollViewScrollPosition, false, false);
            int rowLength = 1;
            int maxRowLength = Mathf.FloorToInt((this.position.width - 35) / _zoomValue);
            if (maxRowLength < 1)
            {
                maxRowLength = 1;
            }

            foreach (BrushObject brObj in brushes.Brushes)
            {
                //check if brushObject is null, if so skip this brush
                if (brObj == null || brObj.brushObject == null)
                {
                    continue;
                }

                //check if row is longer than max row length
                if (rowLength > maxRowLength)
                {
                    rowLength = 1;
                    EditorGUILayout.EndHorizontal();
                }
                //begin row if rowLength == 1
                if (rowLength == 1)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                //change color
                Color guiColor = GUI.backgroundColor;
                if (brushes.selectedBrushes.Contains(brObj))
                {
                    GUI.backgroundColor = SelectedColor;
                    if (brushes.primarySelectedBrush == brObj)
                    {
                        GUI.backgroundColor = PrimarySelectedColor;
                    }
                }

                EditorGUILayout.BeginVertical(GUILayout.Width(1));

                //Create the brush entry in the scroll view and check if the user clicked on the created button (change the currently selected/edited brush accordingly and add it to the current brushes if possible)
                GUIContent btnContent = new GUIContent(AssetPreview.GetAssetPreview(brObj.brushObject), brObj.brushObject.name);
                if (GUILayout.Button(btnContent, GUILayout.Width(_zoomValue), GUILayout.Height(_zoomValue)))
                {
                    //Add and remove brushes from the current brushes list
                    if (Event.current.control && !brushes.selectedBrushes.Contains(brObj))
                    {
                        brushes.selectedBrushes.Add(brObj);
                    }
                    else if (brushes.selectedBrushes.Contains(brObj))
                    {
                        brushes.selectedBrushes.Remove(brObj);
                    }

                    //select the currently edited brush and deselect all selected brushes
                    if (!Event.current.control)
                    {
                        brushes.selectedBrushes.Clear();
                        brushes.primarySelectedBrush = brObj;
                        brushes.selectedBrushes.Add(brObj);
                    }
                }

                brObj.brushObject = (GameObject)EditorGUILayout.ObjectField(brObj.brushObject, typeof(GameObject), false, GUILayout.Width(_zoomValue));
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = guiColor;
                rowLength++;
            }

            //check if row is longer than max row length
            if (rowLength > maxRowLength)
            {
                rowLength = 1;
                EditorGUILayout.EndHorizontal();
            }
            //begin row if rowLength == 1
            if (rowLength == 1)
            {
                EditorGUILayout.BeginHorizontal();
            }

            //add button
            if (GUILayout.Button("+", GUILayout.Width(_zoomValue), GUILayout.Height(_zoomValue)))
            {
                AddObjectPopup.Init(brushes.Brushes, this);
            }
            Color guiColorBGC = GUI.backgroundColor;

            //end horizontal and scroll view again
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            _zoomValue = EditorGUILayout.Slider("Zoom", _zoomValue, 50, 200);
            #endregion

            #region Actions Group
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = green;
            if (GUILayout.Button(new GUIContent("Add Brush", "Add a new brush to the selection.")))
            {
                AddObjectPopup.Init(brushes.Brushes, this);
            }

            EditorGUI.BeginDisabledGroup(brushes.selectedBrushes.Count == 0 || brushes.primarySelectedBrush == null);
            GUI.backgroundColor = red;

            if (GUILayout.Button(new GUIContent("Remove Current Brush(es)", "Removes the currently selected brush.")))
            {
                if (brushes.selectedBrushes != null)
                {
                    foreach (BrushObject brush in brushes.selectedBrushes)
                    {
                        brushes.Brushes.Remove(brush);
                    }
                    brushes.selectedBrushes = new List<BrushObject>();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(brushes.Brushes.Count == 0);
            if (GUILayout.Button(new GUIContent("Clear Brushes", "Removes all brushes.")) && RemoveAllBrushes_Dialog(brushes.Brushes.Count))
            {
                brushes.RemoveAllBrushes();
                _copy = null;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = guiColorBGC;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _isPlacingEnabled = EditorGUILayout.Toggle(new GUIContent("Painting enabled", "Should painting of gameobjects via left click be enabled?"), _isPlacingEnabled);
            _isErasingEnabled = EditorGUILayout.Toggle(new GUIContent("Erasing enabled", "Should erasing of gameobjects via right click be enabled?"), _isErasingEnabled);
            EditorGUILayout.EndHorizontal();
            guiColorBGC = GUI.backgroundColor;

            if (brushes.selectedBrushes.Count > 0)
            {
                EditorGUI.BeginDisabledGroup(brushes.spawnedObjects.Count == 0);

                GUI.backgroundColor = green;
                if (GUILayout.Button(new GUIContent("Permanently Apply Spawned GameObjects (" + brushes.spawnedObjects.Count + ")", "Permanently apply the gameobjects that have been spawned with GO brush, so they can not be erased by accident anymore.")))
                {
                    brushes.ApplyCachedObjects();
                    _lastPlacementPositions.Clear();
                }

                GUI.backgroundColor = red;
                if (GUILayout.Button(new GUIContent("Remove All Spawned GameObjects (" + brushes.spawnedObjects.Count + ")", "Removes all spawned objects from the scene that have not been applied before.")) && RemoveAllCachedObjects_Dialog(brushes.spawnedObjects.Count))
                {
                    brushes.DeleteSpawnedObjects();
                    _lastPlacementPositions.Clear();
                }
                EditorGUI.EndDisabledGroup();
            }

            GUI.backgroundColor = guiColorBGC;
            #endregion

            #region Brush Details
            //don't show the details of the current brush if we do not have selected a current brush
            if (brushes.selectedBrushes != null && brushes.primarySelectedBrush != null && brushes.Brushes.Count > 0 && brushes.selectedBrushes.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = yellow;
                EditorGUILayout.LabelField("Brush Details" + " - (" + brushes.primarySelectedBrush.brushObject.name + ")", EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("Copy", "Copies the brush."), GUILayout.MaxWidth(50)))
                {
                    _copy = brushes.primarySelectedBrush;
                }
                EditorGUI.BeginDisabledGroup(_copy == null);
                if (GUILayout.Button(new GUIContent("Paste", "Pastes the details of the brush in the clipboard."), GUILayout.MaxWidth(50)))
                {
                    brushes.primarySelectedBrush.PasteDetails(_copy);
                }
                GUI.backgroundColor = guiColorBGC;
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button(new GUIContent("Reset", "Restores the defaults settings of the brush details."), GUILayout.MaxWidth(50)))
                {
                    brushes.primarySelectedBrush.ResetDetails();
                }
                EditorGUILayout.EndHorizontal();

                brushes.primarySelectedBrush.parentContainer = EditorGUILayout.ObjectField("Parent", brushes.primarySelectedBrush.parentContainer, typeof(Transform), true) as Transform;
                brushes.primarySelectedBrush.density = EditorGUILayout.Slider(new GUIContent("Density", "Changes the density of the brush, i.e. how many gameobjects are spawned inside the radius of the brush."), brushes.primarySelectedBrush.density, 0f, 5f);
                brushes.primarySelectedBrush.brushSize = EditorGUILayout.Slider(new GUIContent("Brush Size", "The radius of the brush."), brushes.primarySelectedBrush.brushSize, 0f, 25f);
                brushes.primarySelectedBrush.offsetFromPivot = EditorGUILayout.Vector3Field(new GUIContent("Offset from Pivot", "Changes the offset of the spawned gameobject from the calculated position. This allows you to correct the position of the spawned objects, if you find they are floating for example due to a not that correct pivot on the gameobject/prefab."), brushes.primarySelectedBrush.offsetFromPivot);
                brushes.primarySelectedBrush.rotOffsetFromPivot = EditorGUILayout.Vector3Field(new GUIContent("Rotational Offset", "Changes the rotational offset that is applied to the prefab/gameobject when spawning it. This allows you to current the rotation of the spawned objects."), brushes.primarySelectedBrush.rotOffsetFromPivot);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Min and Max Scale", "The min and max range of the spawned gameobject. If they are not the same value a random value in between the min and max is going to be picked."));
                EditorGUILayout.MinMaxSlider(ref brushes.primarySelectedBrush.minScale, ref brushes.primarySelectedBrush.maxScale, 0.001f, 50);
                brushes.primarySelectedBrush.minScale = EditorGUILayout.FloatField(brushes.primarySelectedBrush.minScale);
                brushes.primarySelectedBrush.maxScale = EditorGUILayout.FloatField(brushes.primarySelectedBrush.maxScale);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                brushes.primarySelectedBrush.randomizeXRotation = EditorGUILayout.Toggle(new GUIContent("Randomize X Rotation", "Should the rotation be randomized on the x axis?"), brushes.primarySelectedBrush.randomizeXRotation);
                brushes.primarySelectedBrush.randomizeYRotation = EditorGUILayout.Toggle(new GUIContent("Randomize Y Rotation", "Should the rotation be randomized on the y axis?"), brushes.primarySelectedBrush.randomizeYRotation);
                brushes.primarySelectedBrush.randomizeZRotation = EditorGUILayout.Toggle(new GUIContent("Randomize Z Rotation", "Should the rotation be randomized on the z axis?"), brushes.primarySelectedBrush.randomizeZRotation);
                EditorGUILayout.EndHorizontal();

                brushes.primarySelectedBrush.alignToSurface = EditorGUILayout.Toggle(new GUIContent("Align to Surface", "This option allows you to align the instantiated gameobjects to the surface you are painting on."), brushes.primarySelectedBrush.alignToSurface);

                brushes.primarySelectedBrush.allowIntercollision = EditorGUILayout.Toggle(new GUIContent("Allow Intercollision", "Should the spawned objects be considered for the spawning of new objects? If so, newly spawned objects can be placed on top of previously (not yet applied) objects."), brushes.primarySelectedBrush.allowIntercollision);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                GUI.backgroundColor = yellow;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Filters" + " - (" + brushes.primarySelectedBrush.brushObject.name + ")", EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("Copy", "Copies the brush."), GUILayout.MaxWidth(50)))
                {
                    _copy = brushes.primarySelectedBrush;
                }
                EditorGUI.BeginDisabledGroup(_copy == null);
                if (GUILayout.Button(new GUIContent("Paste", "Pastes the filters of the brush in the clipboard."), GUILayout.MaxWidth(50)))
                {
                    brushes.primarySelectedBrush.PasteFilters(_copy);
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = guiColorBGC;
                if (GUILayout.Button(new GUIContent("Reset", "Restores the defaults settings of the brush filters."), GUILayout.MaxWidth(50)))
                {
                    brushes.primarySelectedBrush.ResetFilters();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Min and Max Slope", "The range of slope that is required for an object to be placed. If the slope is not in that range, no object is going to be placed."));
                EditorGUILayout.MinMaxSlider(ref brushes.primarySelectedBrush.minSlope, ref brushes.primarySelectedBrush.maxSlope, 0, 360);
                brushes.primarySelectedBrush.minSlope = EditorGUILayout.FloatField(brushes.primarySelectedBrush.minSlope);
                brushes.primarySelectedBrush.maxSlope = EditorGUILayout.FloatField(brushes.primarySelectedBrush.maxSlope);
                EditorGUILayout.EndHorizontal();

                SerializedProperty sp = serializedObject_brushObject.FindProperty("primarySelectedBrush").FindPropertyRelative("layerFilter");

                EditorGUILayout.BeginHorizontal();
                brushes.primarySelectedBrush.isTagFilteringEnabled = EditorGUILayout.Toggle("Enable Tag Filtering", brushes.primarySelectedBrush.isTagFilteringEnabled);
                if (brushes.primarySelectedBrush.isTagFilteringEnabled)
                {
                    brushes.primarySelectedBrush.tagFilter = EditorGUILayout.TagField(new GUIContent("Tag Filter", "Limits the painting to objects that have a specific tag on them."), brushes.primarySelectedBrush.tagFilter);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                serializedObject_brushObject.ApplyModifiedProperties();
            }

            //save AssetDatabase on any change
            if (GUI.changed && brushes != null)
            {
                brushes.Save();
            }

            #endregion
        }

        void OnFocus()
        {
            Tools.current = Tool.None;
        }


        Color _color = Color.green;
        void OnSceneGUI(SceneView sceneView)
        {
            if (!IsOpen || !hasFocus || Tools.current != Tool.None) { return; }

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!_isPlacingEnabled || brushes.selectedBrushes == null || !Physics.Raycast(ray, out RaycastHit hit)) { return; }

            Handles.color = Color.white;
            Handles.DrawLine(hit.point, hit.point + hit.normal * 5);

            var pressedDown = Event.current.rawType == EventType.MouseDown || Event.current.rawType == EventType.MouseDrag;
            if (Event.current.modifiers != EventModifiers.Shift)
            {
                _color = Color.green;

                if (pressedDown && Event.current.button == 0 && PlaceObjects())
                {
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    Event.current.Use();
                }
            }
            else
            {
                _color = Color.red;

                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                if (pressedDown && Event.current.button == 0 && RemoveObjects())
                {
                    Event.current.Use();
                }
            }

            _color.a = 0.25f;

            Handles.color = _color;

            float maxBrushSize = brushes.GetMaximumBrushSizeFromCurrentBrushes();
            float minBrushSize = brushes.GetMinimumBrushSizeFromCurrentBrushes();

            Handles.DrawSolidDisc(hit.point, hit.normal, maxBrushSize);

            Handles.color = (_color * 2) + (Color.black * 2);
            Handles.DrawWireDisc(hit.point, hit.normal, maxBrushSize, 5f);
            Handles.DrawWireDisc(hit.point, hit.normal, minBrushSize, 3f);

            sceneView.Repaint();
        }

        /// <summary>
        /// Switches to the given brushCollection, to edit it. (now this collection it the currently displayed one in the editor window)
        /// </summary>
        /// <param name="collection"></param>
        public static void SwitchToBrushCollection(BrushCollection collection)
        {
            if (Instance == null || collection == null) { return; }

            Instance.selectedBrushCollectionIndex = collection.GetIndex();
            Instance.brushes = collection;
        }

        bool RemoveAllBrushes_Dialog(int brushCount) => EditorUtility.DisplayDialog(
                "Remove all Brushes?",
                $"Are you sure you want to remove all brushes ({brushCount}) from this brushcollection?",
                "Remove all",
                "Cancel");

        bool RemoveAllCachedObjects_Dialog(int count) => EditorUtility.DisplayDialog(
                "Delete all cached GameObjects?",
                $"Are you sure you want to delete all cached GameObjects ({count}) from the scene?",
                "Delete all",
                "Cancel");
        #endregion

        #region GO Brush functionality methods
        /// <summary>
        /// Places the objects
        /// returns true if objects were placed, false otherwise
        /// </summary>
        private bool PlaceObjects()
        {
            if (!_isPlacingEnabled) { return false; }

            bool hasPlacedObjects = false;

            foreach (BrushObject brush in brushes.selectedBrushes)
            {
                int spawnCount = Mathf.RoundToInt(brush.density * brush.brushSize);
                if (spawnCount < 1) { spawnCount = 1; }

                for (int i = 0; i < spawnCount; i++)
                {
                    if (brush.brushObject == null || !IsOpen) { continue; }

                    //raycast from the scene camera to find the position of the brush and create objects there
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    ray.origin += new Vector3(Random.Range(-brush.brushSize/2, brush.brushSize/2), Random.Range(-brush.brushSize/2, brush.brushSize/2), Random.Range(-brush.brushSize/2, brush.brushSize/2));
                    Vector3 startPoint = ray.origin;

                    if (!Physics.Raycast(ray, out RaycastHit hit)) { continue; }

                    //return if we are too close to the previous placement position
                    if (ArePositionsWithinRange(_lastPlacementPositions, hit.point, brush.brushSize, brush.density))
                    {
                        continue;
                    }

                    //return if we are hitting an object that we have just spawned or don't if allowIntercollisionPlacement is enabled on the current brush
                    if (brushes.spawnedObjects.Contains(hit.collider.gameObject) && !brush.allowIntercollision)
                    {
                        continue;
                    }

                    //calculate the angle and abort if it is not in the specified range/filter
                    float angle = Vector3.Angle(Vector3.up, hit.normal);
                    if (angle < brush.minSlope || angle > brush.maxSlope)
                    {
                        continue;
                    }

                    //check if the layer of the hit object is in our layermask filter
                    if (brush.layerFilter != (brush.layerFilter | (1 << hit.transform.gameObject.layer)))
                    {
                        continue;
                    }

                    //check if tag filtering is active, if so check the tags
                    if (brush.isTagFilteringEnabled && !hit.transform.CompareTag(brush.tagFilter))
                    {
                        continue;
                    }

                    //randomize position
                    Vector3 position = hit.point + brush.offsetFromPivot;

                    //instantiate prefab or clone object
                    GameObject obj;
                    if (brush.brushObject.scene.name != null)
                    {
                        obj = Instantiate(brush.brushObject, position, Quaternion.identity);
                    }
                    else
                    {
                        obj = PrefabUtility.InstantiatePrefab(brush.brushObject) as GameObject;
                        obj.transform.SetPositionAndRotation(position, Quaternion.identity);
                    }

                    //check for parent container
                    if (brush.parentContainer != null)
                    {
                        obj.transform.parent = brush.parentContainer;
                    }

                    hasPlacedObjects = true;

                    //register created objects to the undo stack
                    Undo.RegisterCreatedObjectUndo(obj, "Created " + obj.name + " with brush");

                    //check if we should align the object to the surface we are "painting" on
                    if (brush.alignToSurface)
                    {
                        obj.transform.up = hit.normal;
                    }

                    //Randomize rotation
                    Vector3 rot = brush.rotOffsetFromPivot;
                    if (brush.randomizeXRotation)
                        rot.x = Random.Range(0, 360);
                    if (brush.randomizeYRotation)
                        rot.y = Random.Range(0, 360);
                    if (brush.randomizeZRotation)
                        rot.z = Random.Range(0, 360);

                    //apply rotation
                    obj.transform.Rotate(rot, Space.Self);

                    //randomize scale
                    float scale = Random.Range(brush.minScale, brush.maxScale);
                    obj.transform.localScale = new Vector3(scale, scale, scale);

                    //Add object to list so it can be removed later on
                    brushes.spawnedObjects.AddRange(GetAllChildren(obj));

                    //save placement position
                    _lastPlacementPositions.Add(obj, hit.point);
                }
            }

            return hasPlacedObjects;
        }

        /// <summary>
        /// remove objects that are in the brush radius around the brush.
        /// It returns true if it removed something, false otherwise
        /// </summary>
        private bool RemoveObjects()
        {
            //return if erasing is disabled
            if (!_isErasingEnabled) { return false; }

            bool hasRemovedSomething = false;

            foreach (BrushObject brush in brushes.selectedBrushes)
            {
                //raycast to fin brush position
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                List<GameObject> objsToRemove = new List<GameObject>();
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    //loop over all spawned objects to find objects that can be removed
                    foreach (GameObject obj in brushes.spawnedObjects)
                    {
                        if (obj != null && Vector3.Distance(obj.transform.position, hit.point) < brush.brushSize)
                        {
                            objsToRemove.Add(obj);
                        }
                    }

                    //delete the before found objects
                    foreach (GameObject obj in objsToRemove)
                    {
                        brushes.spawnedObjects.Remove(obj);
                        if (_lastPlacementPositions.ContainsKey(obj))
                            _lastPlacementPositions.Remove(obj);

                        DestroyImmediate(obj);
                        hasRemovedSomething = true;
                    }
                    objsToRemove.Clear();
                }
            }

            return hasRemovedSomething;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Generates a Color object by r g b values (Range 0, 256)
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color ColorFromRGB(int r, int g, int b)
        {
            return new Color((float)r / 256, (float)g / 256, (float)b / 256);
        }

        /// <summary>
        /// Get all child game objects parrented to this one.
        /// Includes the object itself
        /// </summary>
        /// <param name="obj">The object to look for child objects</param>
        /// <returns></returns>
        public static GameObject[] GetAllChildren(GameObject obj)
        {
            List<GameObject> children = new List<GameObject>();
            if (obj != null)
            {
                foreach (Transform child in obj.transform)
                {
                    children.Add(child.gameObject);
                }
            }
            children.Add(obj);
            return children.ToArray();
        }

        /// <summary>
        /// Checks if any of the provided positions is in range to a given point
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="point"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static bool ArePositionsWithinRange(Dictionary<GameObject, Vector3> positions, Vector3 point, float range, float density)
        {
            float adjustedRange = (float)range / density;

            foreach (var kv in positions)
            {
                var go = kv.Key;
                var position = kv.Value;
                if (go != null && Vector3.Distance(position, point) <= adjustedRange)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        BrushCollection CreateInstance(string dir, string name)
        {
            var instance = BrushCollection.CreateInstance(dir, name);
            s_brushCollectionList = BrushCollection.GetBrushCollectionsInProject();
            return instance;
        }
    }
}