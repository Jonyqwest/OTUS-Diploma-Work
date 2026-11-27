namespace WorldTile
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(OWT_Container))]
    public class OWT_ContainerEditor : Editor
    {
        private WorldPosition _previewWP;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            OWT_Container container = (OWT_Container)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("World Position Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy World Position → Clipboard", GUILayout.Height(30)))
            {
                CopyWorldPosition(container);
            }

            if (GUILayout.Button("← Paste from Clipboard", GUILayout.Height(30)))
            {
                PasteWorldPosition(container);
            }
            EditorGUILayout.EndHorizontal();

            var pos = container.transform.position;
            var wp = new WorldPosition();
            wp.zoom = TileStat.BaseZoom;
            wp = wp.FromUnityPosition(pos, TileStat.BaseZoom);
            _previewWP = wp;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Current Transform → WorldPosition", EditorStyles.miniLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Transform Position", pos);
            EditorGUILayout.IntField("Tile X", _previewWP.tileX);
            EditorGUILayout.IntField("Tile Y", _previewWP.tileY);
            EditorGUILayout.Vector2Field("Local Offset", _previewWP.localOffset);
            EditorGUILayout.FloatField("Height", _previewWP.Height);
            EditorGUI.EndDisabledGroup();
        }

        private void CopyWorldPosition(OWT_Container container)
        {
            var pos = container.transform.position;
            var wp = new WorldPosition();
            wp.zoom = TileStat.BaseZoom;
            wp = wp.FromUnityPosition(pos, TileStat.BaseZoom);

            var roundTrip = wp.ToUnityPosition();
            var delta = pos - roundTrip;

            Debug.Log($"[OWT_Container] " +
                $"pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) " +
                $"compens={TileStat.compensPos} " +
                $"→ Tile({wp.tileX}/{wp.tileY}) " +
                $"offset({wp.localOffset.x:F4}/{wp.localOffset.y:F4}) " +
                $"Z:{wp.zoom}\n" +
                $"RoundTrip: ({roundTrip.x:F2}, {roundTrip.y:F2}, {roundTrip.z:F2}) " +
                $"delta: ({delta.x:F4}, {delta.y:F4}, {delta.z:F4})",
                container);

            string json = JsonUtility.ToJson(wp, true);
            GUIUtility.systemCopyBuffer = json;

            Debug.Log($"[OWT_Container] WorldPosition copied to clipboard:\n{json}", container);
        }

        private void PasteWorldPosition(OWT_Container container)
        {
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[OWT_Container] Clipboard is empty.");
                return;
            }

            var wp = JsonUtility.FromJson<WorldPosition>(json);
            if (wp.Equals(default(WorldPosition)) && json != JsonUtility.ToJson(default(WorldPosition)))
            {
                Debug.LogWarning("[OWT_Container] Failed to parse WorldPosition from clipboard.");
                return;
            }

            Undo.RecordObject(container, "Paste WorldPosition");
            container.worldPosition = wp;
            EditorUtility.SetDirty(container);

            Debug.Log($"[OWT_Container] Pasted WorldPosition: Tile({wp.tileX}, {wp.tileY}) Z:{wp.zoom}", container);
        }
    }
}
