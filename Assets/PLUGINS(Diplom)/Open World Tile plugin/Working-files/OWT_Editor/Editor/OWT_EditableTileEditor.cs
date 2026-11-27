namespace WorldTile
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(OWT_Editor_TileToTerain))]
    public class OWT_EditableTileEditor : Editor
    {
        private OWT_Editor_TileToTerain _tileToTerain;
        private SerializedProperty _timelineProp;
        private string _googleInputBuffer = "";

        private void OnEnable()
        {
            _tileToTerain = (OWT_Editor_TileToTerain)target;
            _timelineProp = serializedObject.FindProperty("timelinePosition");
            if (_tileToTerain != null)
            {
                _googleInputBuffer = _tileToTerain.googleCoordinates;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("GPS & Location Quick Setup", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _googleInputBuffer = EditorGUILayout.TextField(new GUIContent("Google Maps Coordinates", "Paste lat, lon string from Google Maps (e.g. 35.285272, 81.373709)"), _googleInputBuffer);
            if (EditorGUI.EndChangeCheck())
            {
                _tileToTerain.googleCoordinates = _googleInputBuffer;
                if (_tileToTerain.ApplyGoogleCoordinates(_googleInputBuffer))
                {
                    EditorUtility.SetDirty(_tileToTerain);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Parse Google String"))
            {
                if (_tileToTerain.ApplyGoogleCoordinates(_googleInputBuffer))
                {
                    EditorUtility.SetDirty(_tileToTerain);
                }
                else
                {
                    EditorUtility.DisplayDialog("Coordinate Parse Error", "Could not parse latitude and longitude from string. Format should be 'lat, lon'.", "OK");
                }
            }
            if (GUILayout.Button("Get From Scene Camera"))
            {
                _tileToTerain.SetFromSceneViewCamera();
                _googleInputBuffer = _tileToTerain.googleCoordinates;
                EditorUtility.SetDirty(_tileToTerain);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUI.BeginChangeCheck();
            _tileToTerain.latitude = EditorGUILayout.DoubleField("Latitude (deg)", _tileToTerain.latitude);
            _tileToTerain.longitude = EditorGUILayout.DoubleField("Longitude (deg)", _tileToTerain.longitude);
            if (EditorGUI.EndChangeCheck())
            {
                _tileToTerain.UpdateTileFromLatLon();
                _googleInputBuffer = _tileToTerain.googleCoordinates;
                EditorUtility.SetDirty(_tileToTerain);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Tile & Grid Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int newZoom = EditorGUILayout.IntSlider("Zoom Level", _tileToTerain.zoom, 1, TileStat.BaseZoom);
            int newTileX = EditorGUILayout.IntField("Tile X", _tileToTerain.tileX);
            int newTileY = EditorGUILayout.IntField("Tile Y", _tileToTerain.tileY);

            if (EditorGUI.EndChangeCheck())
            {
                _tileToTerain.zoom = (byte)Mathf.Clamp(newZoom, 1, TileStat.BaseZoom);
                _tileToTerain.tileX = newTileX;
                _tileToTerain.tileY = newTileY;
                _tileToTerain.UpdateLatLonFromTile();
                _googleInputBuffer = _tileToTerain.googleCoordinates;
                EditorUtility.SetDirty(_tileToTerain);
            }

            _tileToTerain.extraDepth = EditorGUILayout.FloatField("Extra Depth (m)", _tileToTerain.extraDepth);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync Tile → Lat/Lon"))
            {
                _tileToTerain.UpdateLatLonFromTile();
                _googleInputBuffer = _tileToTerain.googleCoordinates;
                EditorUtility.SetDirty(_tileToTerain);
            }
            if (GUILayout.Button("Open in Google Maps"))
            {
                string url = $"https://www.google.com/maps/@{_tileToTerain.latitude},{_tileToTerain.longitude},{_tileToTerain.zoom + 2}z";
                Application.OpenURL(url);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (_tileToTerain.HasData)
            {
                string gridInfo = _tileToTerain.IsGrid
                    ? $"Composite Grid: {_tileToTerain.zoom}z → {TileStat.BaseZoom}z ({_tileToTerain.GridSize}×{_tileToTerain.GridSize} sub-tiles)"
                    : "Single Base Tile (z15)";
                EditorGUILayout.LabelField(gridInfo, EditorStyles.boldLabel);

                if (_tileToTerain.HasDelta)
                    EditorGUILayout.HelpBox("Edits saved on disk (.owtedit / .delta)", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("No edits saved yet", MessageType.Info);
            }

            EditorGUILayout.Space(6);
            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                string downloadText = _tileToTerain.HasData ? "Re-Download Tile Data" : "Download Tile Data";
                if (GUILayout.Button(downloadText, GUILayout.Height(34)))
                {
                    _tileToTerain.Download();
                    SceneView.RepaintAll();
                }
            }

            if (_tileToTerain.HasData)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.PropertyField(_timelineProp, new GUIContent("History Timeline"));
                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.Space(6);
                if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
                {
                    _tileToTerain.Save();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Heights", GUILayout.Height(30)))
                {
                    _tileToTerain.RevertHeights();
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Reset Textures", GUILayout.Height(30)))
                {
                    _tileToTerain.RevertTextures();
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    float gmin = float.MaxValue, gmax = float.MinValue;
                    bool any = false;
                    if (_tileToTerain.SubTiles != null)
                        foreach (var st in _tileToTerain.SubTiles)
                            if (st?.originalHeights != null)
                            {
                                any = true;
                                foreach (var v in st.originalHeights)
                                {
                                    if (v < gmin) gmin = v;
                                    if (v > gmax) gmax = v;
                                }
                            }
                    if (any)
                        EditorGUILayout.LabelField("Original Elevation Range", $"{gmin:F1}m – {gmax:F1}m");
                }
            }
        }
    }
}
