namespace WorldTile
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(OWT_PositionCopier))]
    public class OWT_PositionCopierEditor : Editor
    {
        private WorldPosition _preview;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var copier = (OWT_PositionCopier)target;
            var pos = copier.transform.position;

            var wp = new WorldPosition();
            wp.zoom = TileStat.BaseZoom;
            wp = wp.FromUnityPosition(pos, TileStat.BaseZoom);
            _preview = wp;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("World Position Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Copy World Position → Clipboard", GUILayout.Height(30)))
            {
                string json = JsonUtility.ToJson(wp, true);
                GUIUtility.systemCopyBuffer = json;
                Debug.Log($"[OWT_PositionCopier] Copied to clipboard:\n{json}", copier);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Current Transform → WorldPosition", EditorStyles.miniLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Transform Position", pos);
            EditorGUILayout.IntField("Tile X", _preview.tileX);
            EditorGUILayout.IntField("Tile Y", _preview.tileY);
            EditorGUILayout.Vector2Field("Local Offset", _preview.localOffset);
            EditorGUILayout.FloatField("Height", _preview.Height);
            EditorGUI.EndDisabledGroup();
        }
    }
}
