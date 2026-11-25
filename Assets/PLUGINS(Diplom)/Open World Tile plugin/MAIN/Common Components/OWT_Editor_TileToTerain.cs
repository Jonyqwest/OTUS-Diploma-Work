namespace WorldTile
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using UnityEngine;
    using UnityEngine.Networking;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [ExecuteAlways]
    public class OWT_Editor_TileToTerain : MonoBehaviour
    {
        public Terrain terrain;

        public int tileX;
        public int tileY;
        public byte zoom = 13;

        public double latitude = 35.285272;
        public double longitude = 81.373709;
        public string googleCoordinates = "35.285272, 81.373709";

        public float extraDepth = 200f;

        [Range(0, 200), HideInInspector]
        public int timelinePosition = 200;

        public void UpdateTileFromLatLon()
        {
            var wp = WorldPosition.FromLatLon(latitude, longitude, zoom);
            tileX = wp.tileX;
            tileY = wp.tileY;
            googleCoordinates = $"{latitude:F6}, {longitude:F6}";
        }

        public void UpdateLatLonFromTile()
        {
            var wp = new WorldPosition { tileX = tileX, tileY = tileY, zoom = zoom };
            var (lat, lon) = wp.ToLatLon();
            latitude = lat;
            longitude = lon;
            googleCoordinates = $"{lat:F6}, {lon:F6}";
        }

        public bool ApplyGoogleCoordinates(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string[] parts = input.Split(new char[] { ',', ';', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                if (double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    latitude = lat;
                    longitude = lon;
                    UpdateTileFromLatLon();
                    return true;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        public void SetFromSceneViewCamera()
        {
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                Vector3 camPos = sceneView.camera.transform.position;
                var wp = new WorldPosition().FromUnityPosition(camPos, zoom);
                tileX = wp.tileX;
                tileY = wp.tileY;
                UpdateLatLonFromTile();
                Debug.Log($"[OWT_EditableTile] Set from Scene Camera position {camPos} → Tile [{zoom}/{tileX}/{tileY}]");
            }
            else
            {
                Debug.LogWarning("[OWT_EditableTile] Active SceneView camera not found");
            }
        }
#endif

        [SerializeField, HideInInspector] private SubTileData[] _subTiles;

        private double _lastAutoSaveTime;
        private int _lastTimelinePosition = 200;
        private int _textureApplyIndex = -1;

        [System.NonSerialized] private TerrainLayer _cachedPathLayer;
        [System.NonSerialized] private TerrainLayer _cachedGrassLayer;
        [System.NonSerialized] private TerrainLayer _cachedDirtLayer;
        [System.NonSerialized] private TerrainLayer _cachedWaterLayer;

        [System.Serializable]
        public class SubTileData
        {
            public int subX;
            public int subY;
            public Terrain terrain;
            public GameObject gameObject;
            public float[,] originalHeights;
            public int snapCount;
            public float cachedTerrainY;
            public float cachedRange;
        }

        private int BaseZoom => TileStat.BaseZoom;
        public int GridSize => 1 << (BaseZoom - zoom);
        private int SubTileCount => GridSize * GridSize;
        public bool IsGrid => GridSize > 1;
        public bool HasData => _subTiles != null && _subTiles.Length > 0 && _subTiles[0].originalHeights != null;
        public bool HasDelta
        {
            get
            {
                if (!HasData) return false;
                foreach (var st in _subTiles)
                    if (IndividualDeltaExists(st.subX, st.subY))
                        return true;
                return false;
            }
        }

        public SubTileData[] SubTiles => _subTiles;
        public float[,] OriginalHeights
        {
            get
            {
                if (!HasData || _subTiles == null || _subTiles.Length == 0) return null;
                return _subTiles[0].originalHeights;
            }
        }

        private float SubTileWorldSize => TileStat.BaseTileSize;

        private static string GetCustomEditsFolder()
        {
#if UNITY_EDITOR
            string dir = Path.Combine(Application.dataPath, "CustomEdits");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
#else
            return Path.Combine(Application.persistentDataPath, "CustomEdits");
#endif
        }

        private string CustomEditsRelativeDeltaPath(int subX, int subY) =>
            $"{BaseZoom}/{subX}_{subY}_delta.bytes";

        private string CustomEditsRelativeDeltaPath(int x, int y, int z) =>
            $"{z}/{x}_{y}_delta.bytes";

        private string CustomEditsRelativePaintMaskPath(int subX, int subY) =>
            $"{BaseZoom}/{subX}_{subY}_paint.png";

        private string IndividualDeltaPath(int subX, int subY) =>
            Path.Combine(GetCustomEditsFolder(), CustomEditsRelativeDeltaPath(subX, subY));

        private string IndividualDeltaPath(int x, int y, int z) =>
            Path.Combine(GetCustomEditsFolder(), CustomEditsRelativeDeltaPath(x, y, z));

        private bool IndividualDeltaExists(int subX, int subY) =>
            CustomFileExists(CustomEditsRelativeDeltaPath(subX, subY));

        private string TempFolder(int subX, int subY) =>
            Path.Combine(Application.persistentDataPath, "CustomEdits_Temp", BaseZoom.ToString(), $"{subX}_{subY}");

        private string SnapPath(int subX, int subY, int index) =>
            Path.Combine(TempFolder(subX, subY), $"snap_{index:D3}.delta");

        private string ArchivePath =>
            Path.Combine(GetCustomEditsFolder(), zoom.ToString(), $"{tileX}_{tileY}_owt.bytes");

        private string PaintMaskPath(int subX, int subY) =>
            Path.Combine(GetCustomEditsFolder(), CustomEditsRelativePaintMaskPath(subX, subY));

        private byte[] ReadCustomFileBytes(string relativePath)
        {
            string primaryPath = Path.Combine(GetCustomEditsFolder(), relativePath);
            if (File.Exists(primaryPath))
            {
                Debug.Log($"[OWT_EditableTile] Loaded edit file from primary path: {primaryPath}");
                return File.ReadAllBytes(primaryPath);
            }

            if (relativePath.EndsWith("_delta.bytes"))
            {
                string baseName = relativePath.Substring(0, relativePath.Length - 12);
                foreach (var legacyExt in new[] { ".delta.bytes", ".delta" })
                {
                    string p1 = Path.Combine(GetCustomEditsFolder(), baseName + legacyExt);
                    if (File.Exists(p1))
                    {
                        Debug.Log($"[OWT_EditableTile] Loaded edit file from legacy path: {p1}");
                        return File.ReadAllBytes(p1);
                    }
                }
            }
            else if (relativePath.EndsWith("_owt.bytes"))
            {
                string baseName = relativePath.Substring(0, relativePath.Length - 10);
                foreach (var legacyExt in new[] { ".owtedit.bytes", ".owtedit" })
                {
                    string p1 = Path.Combine(GetCustomEditsFolder(), baseName + legacyExt);
                    if (File.Exists(p1))
                    {
                        Debug.Log($"[OWT_EditableTile] Loaded edit file from legacy path: {p1}");
                        return File.ReadAllBytes(p1);
                    }
                }
            }

            string persistentPath = Path.Combine(Application.persistentDataPath, "CustomEdits", relativePath);
            if (File.Exists(persistentPath))
            {
                Debug.Log($"[OWT_EditableTile] Loaded edit file from persistentDataPath: {persistentPath}");
                return File.ReadAllBytes(persistentPath);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            string streamingUrl = Path.Combine(Application.streamingAssetsPath, "CustomEdits", relativePath);
            if (!streamingUrl.Contains("://"))
                streamingUrl = "file://" + streamingUrl;

            using (var req = UnityWebRequest.Get(streamingUrl))
            {
                var asyncOp = req.SendWebRequest();
                while (!asyncOp.isDone) { }
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[OWT_EditableTile] Loaded edit file on Android via UnityWebRequest: {streamingUrl}");
                    return req.downloadHandler.data;
                }
            }
#endif
            return null;
        }

        private bool CustomFileExists(string relativePath)
        {
            string primaryPath = Path.Combine(GetCustomEditsFolder(), relativePath);
            if (File.Exists(primaryPath)) return true;

            if (relativePath.EndsWith("_delta.bytes"))
            {
                string baseName = relativePath.Substring(0, relativePath.Length - 12);
                foreach (var legacyExt in new[] { ".delta.bytes", ".delta" })
                {
                    if (File.Exists(Path.Combine(GetCustomEditsFolder(), baseName + legacyExt))) return true;
                    if (File.Exists(Path.Combine(Application.persistentDataPath, "CustomEdits", baseName + legacyExt))) return true;
                }
            }
            else if (relativePath.EndsWith("_owt.bytes"))
            {
                string baseName = relativePath.Substring(0, relativePath.Length - 10);
                foreach (var legacyExt in new[] { ".owtedit.bytes", ".owtedit" })
                {
                    if (File.Exists(Path.Combine(GetCustomEditsFolder(), baseName + legacyExt))) return true;
                    if (File.Exists(Path.Combine(Application.persistentDataPath, "CustomEdits", baseName + legacyExt))) return true;
                }
            }

            string persistentPath = Path.Combine(Application.persistentDataPath, "CustomEdits", relativePath);
            if (File.Exists(persistentPath)) return true;

#if UNITY_ANDROID && !UNITY_EDITOR
            string streamingUrl = Path.Combine(Application.streamingAssetsPath, "CustomEdits", relativePath);
            if (!streamingUrl.Contains("://"))
                streamingUrl = "file://" + streamingUrl;

            using (var req = UnityWebRequest.Head(streamingUrl))
            {
                var asyncOp = req.SendWebRequest();
                while (!asyncOp.isDone) { }
                return req.result == UnityEngine.Networking.UnityWebRequest.Result.Success;
            }
#else
            return false;
#endif
        }

        private void EnsureSubTileArray()
        {
            int gs = GridSize;
            int baseX = tileX * gs;
            int baseY = tileY * gs;

            if (_subTiles != null && _subTiles.Length == SubTileCount)
            {
                if (_subTiles.Length > 0 && _subTiles[0].subX == baseX && _subTiles[0].subY == baseY)
                    return;
            }

            if (_subTiles != null)
            {
                foreach (var st in _subTiles)
                {
                    if (st != null && st.terrain != null)
                    {
                        if (Application.isPlaying) Destroy(st.terrain.gameObject);
                        else DestroyImmediate(st.terrain.gameObject);
                        st.terrain = null;
                        st.gameObject = null;
                    }
                }
            }

            _subTiles = new SubTileData[SubTileCount];

            for (int i = 0; i < SubTileCount; i++)
            {
                int gx = i % gs;
                int gy = i / gs;
                _subTiles[i] = new SubTileData
                {
                    subX = baseX + gx,
                    subY = baseY + gy,
                };
            }
        }

        private SubTileData GetSubTile(int gx, int gy)
        {
            if (_subTiles == null) return null;
            int idx = gy * GridSize + gx;
            if (idx < 0 || idx >= _subTiles.Length) return null;
            return _subTiles[idx];
        }

        private void ForEachSubTile(Action<SubTileData, int, int> action)
        {
            if (_subTiles == null) return;
            int gs = GridSize;
            for (int gy = 0; gy < gs; gy++)
                for (int gx = 0; gx < gs; gx++)
                {
                    var st = _subTiles[gy * gs + gx];
                    if (st != null)
                        action(st, gx, gy);
                }
        }

        private void ForEachSubTile(Action<SubTileData> action)
        {
            if (_subTiles == null) return;
            foreach (var st in _subTiles)
                if (st != null)
                    action(st);
        }

        private void OnEnable()
        {
            if (zoom > BaseZoom)
                zoom = (byte)BaseZoom;

            EnsureSubTileArray();

            if (HasData)
            {
                ForEachSubTile(st => RecoverSnapCount(st));
            }

#if UNITY_EDITOR
            _lastAutoSaveTime = EditorApplication.timeSinceStartup;
#endif
        }

        private void RecoverSnapCount(SubTileData st)
        {
            string folder = TempFolder(st.subX, st.subY);
            if (!Directory.Exists(folder)) return;
            try
            {
                int maxIndex = -1;
                foreach (var f in Directory.GetFiles(folder, "snap_*.delta"))
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (name.StartsWith("snap_") && int.TryParse(name.Substring(5), out int idx))
                        if (idx >= maxIndex) maxIndex = idx;
                }
                if (maxIndex >= 0 && maxIndex + 1 > st.snapCount)
                {
                    st.snapCount = maxIndex + 1;
                    Debug.Log($"[OWT_EditableTile] Recovered snap count: {st.snapCount} for sub-tile [{st.subX},{st.subY}]");
                }
            }
            catch { }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            if (zoom > BaseZoom)
                zoom = (byte)BaseZoom;

            if (timelinePosition != _lastTimelinePosition && HasData)
            {
                _lastTimelinePosition = timelinePosition;
                ApplyTimelinePosition();
            }
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AutoSaveTick();
                return;
            }
#endif
        }

        public void Download()
        {
            int gs = GridSize;
            EnsureSubTileArray();

            bool anyFailed = false;

            for (int gy = 0; gy < gs; gy++)
                for (int gx = 0; gx < gs; gx++)
                {
                    var st = _subTiles[gy * gs + gx];

                    var original = LoadOrDownloadElevation(st.subX, st.subY, BaseZoom);
                    if (original == null)
                    {
                        Debug.LogError($"[OWT_EditableTile] Failed to load elevation for sub-tile [{st.subX},{st.subY}]");
                        anyFailed = true;
                        continue;
                    }

                    st.originalHeights = (float[,])original.Clone();
                    ClearTempFolder(st);
                    st.snapCount = 0;
                }

            if (anyFailed) return;

            CalculateGlobalElevationBounds(out float commonTerrainY, out float commonRange);

            for (int gy = 0; gy < gs; gy++)
                for (int gx = 0; gx < gs; gx++)
                {
                    var st = _subTiles[gy * gs + gx];
                    float[,] currentHeights = (float[,])st.originalHeights.Clone();

                    var delta = LoadIndividualDelta(st.subX, st.subY);
                    if (delta != null)
                    {
                        for (int z = 0; z < 256; z++)
                            for (int x = 0; x < 256; x++)
                                currentHeights[x, z] = currentHeights[x, z] + delta[x, z];

                        Debug.Log($"[OWT_EditableTile] Applied height delta to sub-tile [{st.subX},{st.subY}] from: {CustomEditsRelativeDeltaPath(st.subX, st.subY)}");
                    }

                    CreateTerrainForSubTile(st, currentHeights, gx, gy, commonTerrainY, commonRange);
                }

            SetNeighbors();

            terrain = (gs == 1 && _subTiles != null && _subTiles.Length > 0)
                ? _subTiles[0].terrain
                : null;

            timelinePosition = 200;
            _lastTimelinePosition = 200;

#if UNITY_EDITOR
            BeginAsyncTextureApply();
#endif

            Debug.Log($"[OWT_EditableTile] Downloaded composite tile [{zoom}/{tileX}/{tileY}] — {gs}×{gs} grid with unified bounds (Y:{commonTerrainY:F1}, Range:{commonRange:F1})");
        }

        public void Save()
        {
            if (!HasData)
            {
                Debug.LogError("[OWT_EditableTile] No original heights — download first");
                return;
            }

            int gs = GridSize;
            int baseX = tileX * gs;
            int baseY = tileY * gs;

            short[][,] subTileDeltas = new short[gs * gs][,];
            bool anyHeightChange = false;

            ForEachSubTile((st, gx, gy) =>
            {
                int idx = gy * gs + gx;
                if (st.terrain == null || st.terrain.terrainData == null) return;

                float[,] current = SampleSubTileHeights(st.terrain);
                short[,] delta = new short[256, 256];
                bool nonZero = false;

                for (int z = 0; z < 256; z++)
                    for (int x = 0; x < 256; x++)
                    {
                        short d = (short)Mathf.Clamp(Mathf.RoundToInt(current[x, z] - st.originalHeights[x, z]), short.MinValue, short.MaxValue);
                        delta[x, z] = d;
                        if (d != 0) nonZero = true;
                    }

                if (nonZero)
                {
                    subTileDeltas[idx] = delta;
                    anyHeightChange = true;
                }
            });

            bool anyPaint = false;
            ForEachSubTile(st =>
            {
                var bytes = SavePaintMask(st);
                if (bytes != null) anyPaint = true;
                ClearTempFolder(st);
                st.snapCount = 0;
            });

            if (!anyHeightChange && !anyPaint)
            {
                string arcPath = ArchivePath;
                if (File.Exists(arcPath))
                    File.Delete(arcPath);
                ForEachSubTile(st => DeleteIndividualDelta(st.subX, st.subY));
                Debug.Log("[OWT_EditableTile] No changes — archive removed");
                timelinePosition = 200;
                _lastTimelinePosition = 200;
                return;
            }

            string archivePath = ArchivePath;
            Directory.CreateDirectory(Path.GetDirectoryName(archivePath));

            using (FileStream fs = new FileStream(archivePath, FileMode.Create, FileAccess.Write))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                ForEachSubTile((st, gx, gy) =>
                {
                    int idx = gy * gs + gx;
                    var delta = subTileDeltas[idx];

                    if (delta != null)
                    {
                        AddDeltaToArchive(archive, $"delta_{BaseZoom}_{st.subX}_{st.subY}.delta", delta);
                        SaveIndividualDelta(st.subX, st.subY, delta);
                    }
                    else
                    {
                        DeleteIndividualDelta(st.subX, st.subY);
                    }

                    string paintPath = PaintMaskPath(st.subX, st.subY);
                    if (File.Exists(paintPath))
                    {
                        var paintEntry = archive.CreateEntry($"paint_{BaseZoom}_{st.subX}_{st.subY}.png",
                            System.IO.Compression.CompressionLevel.Optimal);
                        using (var stream = paintEntry.Open())
                        {
                            var pngBytes = File.ReadAllBytes(paintPath);
                            stream.Write(pngBytes, 0, pngBytes.Length);
                        }
                    }
                });

                if (anyHeightChange)
                {
                    for (int pz = BaseZoom - 1; pz >= zoom; pz--)
                    {
                        int ratio = 1 << (BaseZoom - pz);
                        int pgc = gs / ratio;

                        for (int pgy = 0; pgy < pgc; pgy++)
                            for (int pgx = 0; pgx < pgc; pgx++)
                            {
                                var parentDelta = AggregateParentDelta(subTileDeltas, gs, ratio, pgx, pgy);
                                if (parentDelta == null) continue;

                                int px = tileX * pgc + pgx;
                                int py = tileY * pgc + pgy;

                                string parentPath = IndividualDeltaPath(px, py, pz);
                                Directory.CreateDirectory(Path.GetDirectoryName(parentPath));
                                SaveDeltaFile(parentPath, parentDelta);
                                AddDeltaToArchive(archive, $"delta_{pz}_{px}_{py}.delta", parentDelta);
                            }
                    }
                }
            }

            Debug.Log($"[OWT_EditableTile] Saved archive [{zoom}/{tileX}/{tileY}] (.owtedit) at: {archivePath}\nFolder: {GetCustomEditsFolder()}" +
                      $" — z{BaseZoom} ({gs}×{gs})" +
                      $" + parent zooms z{zoom}..z{BaseZoom - 1}" +
                      $" — temp snapshots cleared");

            timelinePosition = 200;
            _lastTimelinePosition = 200;

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private static short[,] AggregateParentDelta(short[][,] subTileDeltas, int gs, int ratio, int pgx, int pgy)
        {
            short[,] result = new short[256, 256];
            bool anyNonZero = false;

            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                {
                    long sum = 0;
                    int count = 0;

                    for (int dz = 0; dz < ratio; dz++)
                        for (int dx = 0; dx < ratio; dx++)
                        {
                            int totalX = x * ratio + dx;
                            int totalZ = z * ratio + dz;

                            int gx = totalX / 256;
                            int gy = (ratio - 1) - (totalZ / 256);
                            int cx = totalX % 256;
                            int cz = totalZ % 256;

                            int stIdx = (pgy * ratio + gy) * gs + (pgx * ratio + gx);
                            var delta = subTileDeltas[stIdx];
                            if (delta != null)
                            {
                                sum += delta[cx, cz];
                                count++;
                            }
                        }

                    if (count > 0)
                    {
                        short val = (short)(sum / count);
                        result[x, z] = val;
                        if (val != 0) anyNonZero = true;
                    }
                }

            return anyNonZero ? result : null;
        }

        private static void AddDeltaToArchive(ZipArchive archive, string entryName, short[,] data)
        {
            var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write("DELT".ToCharArray());
                writer.Write((byte)1);
                writer.Write((ushort)256);
                for (int z = 0; z < 256; z++)
                    for (int x = 0; x < 256; x++)
                        writer.Write(data[x, z]);
            }
        }

        public void RevertHeights()
        {
            if (!HasData)
            {
                Debug.LogWarning("[OWT_EditableTile] No original data to reset to");
                return;
            }

            string arcPath = ArchivePath;
            if (File.Exists(arcPath))
                File.Delete(arcPath);

            int gs = GridSize;
            for (int pz = BaseZoom - 1; pz >= zoom; pz--)
            {
                int ratio = 1 << (BaseZoom - pz);
                int pgc = gs / ratio;
                for (int pgy = 0; pgy < pgc; pgy++)
                    for (int pgx = 0; pgx < pgc; pgx++)
                    {
                        int px = tileX * pgc + pgx;
                        int py = tileY * pgc + pgy;
                        string path = IndividualDeltaPath(px, py, pz);
                        if (File.Exists(path)) File.Delete(path);
                    }
            }

            ForEachSubTile(st =>
            {
                DeleteIndividualDelta(st.subX, st.subY);
                ClearTempFolder(st);
                st.snapCount = 0;

                if (st.terrain != null && st.terrain.terrainData != null)
                {
                    var td = st.terrain.terrainData;
                    var heights = Upsample256To257(st.originalHeights, st.cachedTerrainY, st.cachedRange, TileStat.ElevationScale);
                    td.SetHeights(0, 0, heights);
                }
            });

            timelinePosition = 200;
            _lastTimelinePosition = 200;

            Debug.Log("[OWT_EditableTile] Reset to original heights");
        }

        public void RevertTextures()
        {
            if (!HasData)
            {
                Debug.LogWarning("[OWT_EditableTile] No original data to reset to");
                return;
            }

            ForEachSubTile(st =>
            {
                DeletePaintMask(st.subX, st.subY);

                if (st.terrain != null && st.terrain.terrainData != null)
                {
                    var td = st.terrain.terrainData;
                    int res = td.alphamapResolution;
                    int layers = td.terrainLayers.Length;
                    if (layers > 0)
                    {
                        var alpha = new float[res, res, layers];
                        for (int z = 0; z < res; z++)
                            for (int x = 0; x < res; x++)
                                alpha[z, x, 0] = 1f;
                        td.SetAlphamaps(0, 0, alpha);
                    }
                }
            });

            Debug.Log("[OWT_EditableTile] Reset to original textures");
        }

        public void Revert()
        {
            RevertHeights();
            RevertTextures();
        }

        private void CalculateGlobalElevationBounds(out float commonTerrainY, out float commonRange)
        {
            float elevScale = TileStat.ElevationScale;
            float globalMinH_m = float.MaxValue;
            float globalMaxH_m = float.MinValue;

            ForEachSubTile(st =>
            {
                if (st.originalHeights != null)
                {
                    float min = GetMin(st.originalHeights);
                    float max = GetMax(st.originalHeights);
                    if (min < globalMinH_m) globalMinH_m = min;
                    if (max > globalMaxH_m) globalMaxH_m = max;
                }
            });

            if (globalMinH_m == float.MaxValue || globalMaxH_m == float.MinValue)
            {
                globalMinH_m = 0;
                globalMaxH_m = 1000;
            }

            float minH = globalMinH_m * elevScale;
            float maxH = globalMaxH_m * elevScale;
            float scaledExtra = extraDepth * elevScale;

            commonRange = (maxH - minH) + 2f * scaledExtra;
            if (commonRange < 1f) commonRange = 1f;
            commonTerrainY = minH - scaledExtra;
        }

        private void CreateTerrainForSubTile(SubTileData st, float[,] data, int gx, int gy, float commonTerrainY, float commonRange)
        {
            float elevScale = TileStat.ElevationScale;
            float tileSize = SubTileWorldSize;

            st.cachedRange = commonRange;
            st.cachedTerrainY = commonTerrainY;

            var heights257 = Upsample256To257(data, st.cachedTerrainY, st.cachedRange, elevScale);

            TerrainData td = (st.terrain != null) ? st.terrain.terrainData : null;
            bool isNewTd = td == null;
            if (isNewTd)
            {
                td = new TerrainData();
                td.heightmapResolution = 257;
            }

            td.size = new Vector3(tileSize, st.cachedRange, tileSize);
            td.SetHeights(0, 0, heights257);

            bool isNew = st.terrain == null;
            if (isNew)
            {
                var go = new GameObject($"Terrain [{st.subX},{st.subY}]");
                go.transform.SetParent(transform);
                st.terrain = go.AddComponent<Terrain>();
                go.AddComponent<TerrainCollider>();
                st.gameObject = go;
            }

            if (isNewTd || st.terrain.terrainData != td)
                st.terrain.terrainData = td;

            st.terrain.castShadows = false;
            var collider = st.terrain.GetComponent<TerrainCollider>();
            if (collider != null && (isNewTd || collider.terrainData != td))
                collider.terrainData = td;
            int gs = GridSize;
            st.terrain.transform.localPosition = new Vector3(gx * tileSize, st.cachedTerrainY, (gs - 1 - gy) * tileSize);

            if (st.terrain.materialTemplate == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                if (shader == null)
                    shader = Shader.Find("Nature/Terrain/Standard");
                if (shader != null)
                    st.terrain.materialTemplate = new Material(shader);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(st.terrain);
            if (td != null)
                UnityEditor.EditorUtility.SetDirty(td);
#endif
        }

        private void ApplySubTileTextures(SubTileData st)
        {
            float tileSize = SubTileWorldSize;
            var tex = LoadOrDownloadTexture(st.subX, st.subY, BaseZoom);
            if (tex != null)
            {
                var detail = GetDetailTexturesOrDefault();
                ApplyTerrainTextures(st.terrain, tex, tileSize, detail);

                var paintTex = LoadPaintMaskTex(st.subX, st.subY);
                if (paintTex != null)
                {
                    ApplyPaintMaskToTerrain(st.terrain, paintTex);
                    DestroyImmediate(paintTex);
                    Debug.Log($"[OWT_EditableTile] Applied paint mask to sub-tile [{st.subX},{st.subY}] from: {CustomEditsRelativePaintMaskPath(st.subX, st.subY)}");
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(st.terrain);
            if (st.terrain.terrainData != null)
                UnityEditor.EditorUtility.SetDirty(st.terrain.terrainData);
#endif
        }

        private struct DetailTextures
        {
            public Texture2D pathTex;
            public Vector2 pathScale;
            public Texture2D grassTex;
            public Vector2 grassScale;
            public Texture2D dirtTex;
            public Vector2 dirtScale;
            public Texture2D waterTex;
            public Vector2 waterScale;
            public float tileSize;
        }

        private static DetailTextures GetDetailTexturesOrDefault()
        {
            var tm = TileManager.Instance;
            if (tm == null)
            {
#if UNITY_EDITOR
                tm = FindAnyObjectByType<TileManager>();
#endif
            }
            var mat = tm != null ? tm.PaintOverlayMaterial : null;

            if (mat != null)
            {
                var tPath = mat.GetTexture("_PathTex") as Texture2D;
                var tGrass = mat.GetTexture("_GrassTex") as Texture2D;
                var tDirt = mat.GetTexture("_DirtTex") as Texture2D;
                var tWater = mat.GetTexture("_WaterTex") as Texture2D;
                float tSize = mat.HasProperty("_TileSize") ? mat.GetFloat("_TileSize") : 10f;

                Vector4 pathST = mat.HasProperty("_PathTex_ST") ? mat.GetVector("_PathTex_ST") : new Vector4(1, 1, 0, 0);
                Vector4 grassST = mat.HasProperty("_GrassTex_ST") ? mat.GetVector("_GrassTex_ST") : new Vector4(1, 1, 0, 0);
                Vector4 dirtST = mat.HasProperty("_DirtTex_ST") ? mat.GetVector("_DirtTex_ST") : new Vector4(1, 1, 0, 0);
                Vector4 waterST = mat.HasProperty("_WaterTex_ST") ? mat.GetVector("_WaterTex_ST") : new Vector4(1, 1, 0, 0);

                if (tPath != null && tGrass != null && tDirt != null && tWater != null)
                {
                    return new DetailTextures
                    {
                        pathTex = tPath,
                        pathScale = new Vector2(pathST.x > 0 ? pathST.x : 1f, pathST.y > 0 ? pathST.y : 1f),
                        grassTex = tGrass,
                        grassScale = new Vector2(grassST.x > 0 ? grassST.x : 1f, grassST.y > 0 ? grassST.y : 1f),
                        dirtTex = tDirt,
                        dirtScale = new Vector2(dirtST.x > 0 ? dirtST.x : 1f, dirtST.y > 0 ? dirtST.y : 1f),
                        waterTex = tWater,
                        waterScale = new Vector2(waterST.x > 0 ? waterST.x : 1f, waterST.y > 0 ? waterST.y : 1f),
                        tileSize = tSize
                    };
                }
            }

            return new DetailTextures
            {
                pathTex = TerrainTile.CreateDetailTexture(
                    new Color32(139, 119, 101, 255), new Color32(120, 100, 80, 255), "PathDetail"),
                pathScale = Vector2.one,
                grassTex = TerrainTile.CreateDetailTexture(
                    new Color32(76, 142, 50, 255), new Color32(60, 120, 35, 255), "GrassDetail"),
                grassScale = Vector2.one,
                dirtTex = TerrainTile.CreateDetailTexture(
                    new Color32(121, 85, 48, 255), new Color32(100, 70, 35, 255), "DirtDetail"),
                dirtScale = Vector2.one,
                waterTex = TerrainTile.CreateDetailTexture(
                    new Color32(30, 100, 180, 255), new Color32(20, 80, 160, 255), "WaterDetail"),
                waterScale = Vector2.one,
                tileSize = 10f
            };
        }

#if UNITY_EDITOR
        private void BeginAsyncTextureApply()
        {
            _textureApplyIndex = 0;
            EditorApplication.update -= AsyncTextureApplyTick;
            EditorApplication.update += AsyncTextureApplyTick;
        }

        private void AsyncTextureApplyTick()
        {
            if (_textureApplyIndex < 0 || _subTiles == null || _textureApplyIndex >= _subTiles.Length)
            {
                EditorApplication.update -= AsyncTextureApplyTick;
                _textureApplyIndex = -1;
                return;
            }

            var st = _subTiles[_textureApplyIndex];
            _textureApplyIndex++;

            if (st != null && st.terrain != null && st.terrain.terrainData != null)
                ApplySubTileTextures(st);

            if (_textureApplyIndex >= _subTiles.Length)
            {
                EditorApplication.update -= AsyncTextureApplyTick;
                _textureApplyIndex = -1;
                Debug.Log($"[OWT_EditableTile] Async texture apply complete");
            }
        }
#endif

        private void SetNeighbors()
        {
            int gs = GridSize;
            for (int gy = 0; gy < gs; gy++)
                for (int gx = 0; gx < gs; gx++)
                {
                    var t = GetSubTile(gx, gy)?.terrain;
                    if (t == null) continue;
                    var left = GetSubTile(gx - 1, gy)?.terrain;
                    var bottom = GetSubTile(gx, gy + 1)?.terrain;
                    var right = GetSubTile(gx + 1, gy)?.terrain;
                    var top = GetSubTile(gx, gy - 1)?.terrain;
                    t.SetNeighbors(left, top, right, bottom);
                }
        }

        // ── Timeline ──────────────────────────────────────────────

        private float[,] ComputeTimelineHeights(SubTileData st)
        {
            float t = timelinePosition / 200f;

            if (t >= 1f || st.snapCount == 0)
                return null;

            float state = t * st.snapCount;
            int idx = Mathf.FloorToInt(state);
            float blend = state - idx;
            idx = Mathf.Clamp(idx, 0, st.snapCount);

            float[,] a = GetStateAt(st, idx);
            float[,] b = GetStateAt(st, Mathf.Min(idx + 1, st.snapCount));

            if (a == null) return st.originalHeights;
            if (b == null) return a;

            float[,] result = new float[256, 256];
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    result[x, z] = a[x, z] + (b[x, z] - a[x, z]) * blend;

            return result;
        }

        private float[,] GetStateAt(SubTileData st, int index)
        {
            if (index == 0) return st.originalHeights;
            var snap = LoadSnapshot(st.subX, st.subY, index - 1);
            if (snap == null) return null;
            float[,] result = (float[,])st.originalHeights.Clone();
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    result[x, z] = result[x, z] + snap[x, z];

            return result;
        }

        private void ApplyTimelinePosition()
        {
            if (!HasData) return;
            ForEachSubTile(st =>
            {
                var heights = ComputeTimelineHeights(st);
                if (heights == null) return;
                if (st.terrain != null && st.terrain.terrainData != null)
                {
                    var td = st.terrain.terrainData;
                    var h257 = Upsample256To257(heights, st.cachedTerrainY, st.cachedRange, TileStat.ElevationScale);
                    td.SetHeights(0, 0, h257);
                }
            });
        }

        // ── Auto-save ─────────────────────────────────────────────

#if UNITY_EDITOR
        private void AutoSaveTick()
        {
            if (!HasData) return;
            if (timelinePosition != 200) return;

            if (EditorApplication.timeSinceStartup - _lastAutoSaveTime < 3.0)
                return;

            _lastAutoSaveTime = EditorApplication.timeSinceStartup;

            ForEachSubTile(st =>
            {
                if (st.terrain == null || st.terrain.terrainData == null) return;

                float[,] current = SampleSubTileHeights(st.terrain);

                var lastSnap = st.snapCount > 0 ? LoadSnapshot(st.subX, st.subY, st.snapCount - 1) : null;
                float[,] expected = st.originalHeights;
                if (lastSnap != null)
                {
                    expected = (float[,])st.originalHeights.Clone();
                    for (int z = 0; z < 256; z++)
                        for (int x = 0; x < 256; x++)
                            expected[x, z] = expected[x, z] + lastSnap[x, z];

                }

                for (int z = 0; z < 256; z++)
                    for (int x = 0; x < 256; x++)
                        if (Mathf.Abs(current[x, z] - expected[x, z]) > 0.01f)
                        {
                            CommitAutoSave(st, current);
                            return;
                        }
            });
        }

        public void ForceAutoSave()
        {
            if (!HasData) return;
            ForEachSubTile(st =>
            {
                if (st.terrain == null || st.terrain.terrainData == null) return;
                float[,] current = SampleSubTileHeights(st.terrain);
                CommitAutoSave(st, current);
            });
        }

        private void CommitAutoSave(SubTileData st, float[,] current)
        {
            if (st.snapCount >= 200)
                RotateSnapshotsForNewSlot(st);

            short[,] delta = new short[256, 256];
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    delta[x, z] = (short)Mathf.Clamp(Mathf.RoundToInt(current[x, z] - st.originalHeights[x, z]), short.MinValue, short.MaxValue);

            SaveSnapshot(st.subX, st.subY, delta, st.snapCount);
            SaveIndividualDelta(st.subX, st.subY, delta);
            st.snapCount++;
        }

        private void RotateSnapshotsForNewSlot(SubTileData st)
        {
            File.Delete(SnapPath(st.subX, st.subY, 0));
            for (int i = 1; i < st.snapCount; i++)
            {
                string src = SnapPath(st.subX, st.subY, i);
                if (File.Exists(src))
                    File.Move(src, SnapPath(st.subX, st.subY, i - 1));
            }
            st.snapCount--;
        }
#endif

        private float[,] SampleSubTileHeights(Terrain t)
        {
            var td = t.terrainData;
            int res = td.heightmapResolution;
            var heights = td.GetHeights(0, 0, res, res);
            float sizeY = td.size.y;
            float ty = t.transform.localPosition.y;
            float elevScale = TileStat.ElevationScale;

            float[,] result = new float[256, 256];
            if (res == 257)
                Downsample257To256(heights, result, sizeY, ty, elevScale);
            else
                CopyDirect(heights, result, sizeY, ty, res, elevScale);
            return result;
        }

        // ── Snapshot I/O ──────────────────────────────────────────

        private void SaveSnapshot(int subX, int subY, short[,] data, int index)
        {
            string path = SnapPath(subX, subY, index);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using DeflateStream deflate = new DeflateStream(fs, System.IO.Compression.CompressionLevel.Optimal);
            using BinaryWriter writer = new BinaryWriter(deflate);

            writer.Write("SNAP".ToCharArray());
            writer.Write((byte)1);
            writer.Write((ushort)256);
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    writer.Write(data[x, z]);
        }

        private short[,] LoadSnapshot(int subX, int subY, int index)
        {
            string path = SnapPath(subX, subY, index);
            if (!File.Exists(path)) return null;

            try
            {
                using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using DeflateStream deflate = new DeflateStream(fs, CompressionMode.Decompress);
                using BinaryReader reader = new BinaryReader(deflate);

                char[] magic = reader.ReadChars(4);
                if (new string(magic) != "SNAP") return null;
                if (reader.ReadByte() != 1) return null;
                int size = reader.ReadUInt16();
                if (size != 256) return null;

                short[,] data = new short[size, size];
                for (int z = 0; z < size; z++)
                    for (int x = 0; x < size; x++)
                        data[x, z] = reader.ReadInt16();
                return data;
            }
            catch
            {
                return null;
            }
        }

        private void ClearTempFolder(SubTileData st)
        {
            string folder = TempFolder(st.subX, st.subY);
            if (!Directory.Exists(folder)) return;
            try { Directory.Delete(folder, recursive: true); }
            catch { }
        }

        // ── Individual Delta I/O ──────────────────────────────────

        private void SaveIndividualDelta(int subX, int subY, short[,] data)
        {
            string path = IndividualDeltaPath(subX, subY);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using DeflateStream deflate = new DeflateStream(fs, System.IO.Compression.CompressionLevel.Optimal);
            using BinaryWriter writer = new BinaryWriter(deflate);

            writer.Write("DELT".ToCharArray());
            writer.Write((byte)1);
            writer.Write((ushort)256);
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    writer.Write(data[x, z]);
        }

        private static void SaveDeltaFile(string path, short[,] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using DeflateStream deflate = new DeflateStream(fs, System.IO.Compression.CompressionLevel.Optimal);
            using BinaryWriter writer = new BinaryWriter(deflate);
            writer.Write("DELT".ToCharArray());
            writer.Write((byte)1);
            writer.Write((ushort)256);
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    writer.Write(data[x, z]);
        }

        private short[,] LoadIndividualDelta(int subX, int subY)
        {
            byte[] bytes = ReadCustomFileBytes(CustomEditsRelativeDeltaPath(subX, subY));
            if (bytes == null || bytes.Length == 0) return null;

            try
            {
                using MemoryStream ms = new MemoryStream(bytes);
                using DeflateStream deflate = new DeflateStream(ms, CompressionMode.Decompress);
                using BinaryReader reader = new BinaryReader(deflate);

                char[] magic = reader.ReadChars(4);
                if (new string(magic) != "DELT") return null;
                if (reader.ReadByte() != 1) return null;
                int size = reader.ReadUInt16();
                if (size != 256) return null;

                short[,] data = new short[size, size];
                for (int z = 0; z < size; z++)
                    for (int x = 0; x < size; x++)
                        data[x, z] = reader.ReadInt16();
                return data;
            }
            catch
            {
                return null;
            }
        }

        private void DeleteIndividualDelta(int subX, int subY)
        {
            string rel = CustomEditsRelativeDeltaPath(subX, subY);
            string primaryPath = Path.Combine(GetCustomEditsFolder(), rel);
            if (File.Exists(primaryPath)) File.Delete(primaryPath);

            string persistentPath = Path.Combine(Application.persistentDataPath, "CustomEdits", rel);
            if (File.Exists(persistentPath)) File.Delete(persistentPath);

            string baseName = $"{BaseZoom}/{subX}_{subY}";
            foreach (var legacyExt in new[] { ".delta", ".delta.bytes" })
            {
                string p1 = Path.Combine(GetCustomEditsFolder(), baseName + legacyExt);
                if (File.Exists(p1)) File.Delete(p1);
                string p2 = Path.Combine(Application.persistentDataPath, "CustomEdits", baseName + legacyExt);
                if (File.Exists(p2)) File.Delete(p2);
            }
        }

        // ── Paint mask I/O ────────────────────────────────────────

        private byte[] EncodePaintMaskToPNG(SubTileData st)
        {
            if (st.terrain == null || st.terrain.terrainData == null) return null;
            var td = st.terrain.terrainData;
            int res = td.alphamapResolution;
            var alpha = td.GetAlphamaps(0, 0, res, res);

            var pixels = new Color32[res * res];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    // A channel inverted: 0 = water present, 255 = no water (land)
                    pixels[z * res + x] = new Color32(
                        (byte)(Mathf.Clamp01(alpha[z, x, 1]) * 255),
                        (byte)(Mathf.Clamp01(alpha[z, x, 2]) * 255),
                        (byte)(Mathf.Clamp01(alpha[z, x, 3]) * 255),
                        (byte)((1f - Mathf.Clamp01(alpha[z, x, 4])) * 255));
                }

            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            DestroyImmediate(tex);
            return bytes;
        }

        private byte[] SavePaintMask(SubTileData st)
        {
            var bytes = EncodePaintMaskToPNG(st);
            if (bytes == null) return null;
            string path = PaintMaskPath(st.subX, st.subY);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
            return bytes;
        }

        private void DeletePaintMask(int subX, int subY)
        {
            string rel = CustomEditsRelativePaintMaskPath(subX, subY);
            string primaryPath = Path.Combine(GetCustomEditsFolder(), rel);
            if (File.Exists(primaryPath)) File.Delete(primaryPath);

            string persistentPath = Path.Combine(Application.persistentDataPath, "CustomEdits", rel);
            if (File.Exists(persistentPath)) File.Delete(persistentPath);
        }

        private Texture2D LoadPaintMaskTex(int subX, int subY)
        {
            byte[] bytes = ReadCustomFileBytes(CustomEditsRelativePaintMaskPath(subX, subY));
            if (bytes == null || bytes.Length == 0) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            return tex;
        }

        private void ApplyPaintMaskToTerrain(Terrain terrain, Texture2D paintTex)
        {
            if (terrain == null || terrain.terrainData == null || paintTex == null) return;
            var td = terrain.terrainData;
            if (td.terrainLayers.Length < 5) return;

            int res = td.alphamapResolution;
            int texRes = paintTex.width;
            var pixels = paintTex.GetPixels32();
            float[,,] alpha = new float[res, res, 5];

            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    int tx = x * texRes / res;
                    int tz = z * texRes / res;
                    var p = pixels[tz * texRes + tx];
                    alpha[z, x, 1] = p.r / 255f;
                    alpha[z, x, 2] = p.g / 255f;
                    alpha[z, x, 3] = p.b / 255f;
                    alpha[z, x, 4] = 1f - p.a / 255f; // A inverted: 0=water, 255=land
                    float sum = alpha[z, x, 1] + alpha[z, x, 2] + alpha[z, x, 3] + alpha[z, x, 4];
                    alpha[z, x, 0] = Mathf.Clamp01(1f - sum);
                }

            td.SetAlphamaps(0, 0, alpha);
        }

        // ── Play Mode hooks ───────────────────────────────────────

#if UNITY_EDITOR
        private static bool _hooksRegistered;

        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterHooks()
        {
            if (_hooksRegistered) return;
            _hooksRegistered = true;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        private static void OnPlayModeChange(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
            {
                var tiles = FindObjectsOfType<OWT_Editor_TileToTerain>();
                foreach (var t in tiles)
                {
                    t.ForceAutoSave();
                    t.ForEachSubTile(st =>
                    {
                        if (st.terrain != null)
                            DestroyImmediate(st.terrain.gameObject);
                        st.terrain = null;
                        st.gameObject = null;
                    });
                    t.terrain = null;
                }
            }
            else if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    var tiles = FindObjectsOfType<OWT_Editor_TileToTerain>();
                    foreach (var t in tiles)
                        if (t.HasData)
                            t.RestoreAfterPlayMode();
                };
            }
        }

        private void RestoreAfterPlayMode()
        {
            CalculateGlobalElevationBounds(out float commonTerrainY, out float commonRange);

            ForEachSubTile((st, gx, gy) =>
            {
                float[,] heights = ComputeLatestSnapshotState(st);
                if (heights != null)
                {
                    CreateTerrainForSubTile(st, heights, gx, gy, commonTerrainY, commonRange);
                    return;
                }

                var finalDelta = LoadIndividualDelta(st.subX, st.subY);
                if (finalDelta != null)
                {
                    var merged = (float[,])st.originalHeights.Clone();
                    for (int z = 0; z < 256; z++)
                        for (int x = 0; x < 256; x++)
                            merged[x, z] = merged[x, z] + finalDelta[x, z];

                    CreateTerrainForSubTile(st, merged, gx, gy, commonTerrainY, commonRange);
                    return;
                }

                CreateTerrainForSubTile(st, st.originalHeights, gx, gy, commonTerrainY, commonRange);
            });

            int gs = GridSize;
            if (gs == 1 && _subTiles != null && _subTiles.Length > 0)
                terrain = _subTiles[0].terrain;
            else
                terrain = null;

            SetNeighbors();
            timelinePosition = 200;
            _lastTimelinePosition = 200;
        }

        private float[,] ComputeLatestSnapshotState(SubTileData st)
        {
            if (st.originalHeights == null || st.snapCount == 0)
                return null;

            var lastSnap = LoadSnapshot(st.subX, st.subY, st.snapCount - 1);
            if (lastSnap == null)
                return null;

            var merged = (float[,])st.originalHeights.Clone();
            for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                    merged[x, z] = merged[x, z] + lastSnap[x, z];

            return merged;
        }
#endif

        // ── Loading ───────────────────────────────────────────────

        private float[,] LoadOrDownloadElevation(int tX, int tY, int tZoom)
        {
            var cached = CacheTileUtility.LoadElevationDataCompressed(tX, tY, tZoom);
            if (cached != null)
            {
                Debug.Log($"[OWT_EditableTile] Loaded elevation from cache (.elv) [{tZoom}/{tX}/{tY}]");
                return cached;
            }

            string url = $"https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{tZoom}/{tX}/{tY}.png";

#if UNITY_EDITOR
            try
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Downloading Elevation", $"Tile [{tZoom}/{tX}/{tY}]", 0.5f);
                byte[] bytes;
                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (UnityEditor)");
                    bytes = client.DownloadData(url);
                }

                UnityEditor.EditorUtility.ClearProgressBar();

                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogError($"[OWT_EditableTile] Downloaded 0 bytes for elevation [{tZoom}/{tX}/{tY}]");
                    return null;
                }

                Texture2D tex = new Texture2D(2, 2);
                if (!tex.LoadImage(bytes))
                {
                    Debug.LogError($"[OWT_EditableTile] Failed to decode PNG for elevation [{tZoom}/{tX}/{tY}]");
                    DestroyImmediate(tex);
                    return null;
                }

                var decoded = TerrainTile.DecodeTerrarium(tex);
                CacheTileUtility.SaveElevationDataCompressed(decoded, tX, tY, tZoom);

                DestroyImmediate(tex);
                Debug.Log($"[OWT_EditableTile] Downloaded & cached elevation [{tZoom}/{tX}/{tY}] from Amazon Terrarium");
                return decoded;
            }
            catch (System.Exception ex)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
                Debug.LogError($"[OWT_EditableTile] Failed to download elevation [{tZoom}/{tX}/{tY}]: {ex.Message}");
                return null;
            }
#else
            Debug.LogError("[OWT_EditableTile] Downloads only supported in Editor");
            return null;
#endif
        }

        private Texture2D LoadOrDownloadTexture(int tX, int tY, int tZoom)
        {
            var cached = CacheTileUtility.LoadTileTextureFromCache(tX, tY, tZoom);
            if (cached != null)
                return cached;

            string url = $"https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{tZoom}/{tY}/{tX}.png";

#if UNITY_EDITOR
            try
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Downloading Satellite Texture", $"Tile [{tZoom}/{tX}/{tY}]", 0.5f);
                byte[] bytes;
                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (UnityEditor)");
                    bytes = client.DownloadData(url);
                }

                UnityEditor.EditorUtility.ClearProgressBar();

                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogWarning($"[OWT_EditableTile] Downloaded 0 bytes for texture [{tZoom}/{tX}/{tY}]");
                    return null;
                }

                Texture2D tex = new Texture2D(2, 2);
                if (!tex.LoadImage(bytes))
                {
                    Debug.LogWarning($"[OWT_EditableTile] Failed to decode PNG for texture [{tZoom}/{tX}/{tY}]");
                    DestroyImmediate(tex);
                    return null;
                }

                var key = new ZXY((byte)tZoom, tX, tY);
                CacheTileUtility.SaveTileTextureToCache(tex, key);

                Texture2D result = CacheTileUtility.LoadTileTextureFromCache(tX, tY, tZoom);
                DestroyImmediate(tex);

                Debug.Log($"[OWT_EditableTile] Downloaded & cached satellite texture [{tZoom}/{tX}/{tY}] from ArcGIS");
                return result;
            }
            catch (System.Exception ex)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
                Debug.LogWarning($"[OWT_EditableTile] Failed to download texture [{tZoom}/{tX}/{tY}]: {ex.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        private TerrainLayer GetOrCreateDetailLayer(TerrainData td, Texture2D tex, Vector2 scale, float baseTileSize, ref TerrainLayer cached)
        {
            Vector2 tileSize = new Vector2(baseTileSize / scale.x, baseTileSize / scale.y);

            if (td != null && td.terrainLayers != null)
            {
                foreach (var layer in td.terrainLayers)
                {
                    if (layer != null && layer.diffuseTexture == tex)
                    {
                        layer.tileSize = tileSize;
                        if (cached == null) cached = layer;
                        return layer;
                    }
                }
            }

            if (cached != null && cached.diffuseTexture == tex)
            {
                cached.tileSize = tileSize;
                return cached;
            }

            cached = new TerrainLayer
            {
                diffuseTexture = tex,
                tileSize = tileSize
            };
            return cached;
        }

        private void ApplyTerrainTextures(Terrain t, Texture2D satelliteTex, float tileSize,
            DetailTextures detail)
        {
            var td = t.terrainData;
            td.alphamapResolution = 2048;

            TerrainLayer satLayer = null;
            if (td.terrainLayers != null && td.terrainLayers.Length > 0 && td.terrainLayers[0] != null && td.terrainLayers[0].diffuseTexture == satelliteTex)
            {
                satLayer = td.terrainLayers[0];
                satLayer.tileSize = new Vector2(tileSize, tileSize);
            }
            else
            {
                satLayer = new TerrainLayer
                {
                    diffuseTexture = satelliteTex,
                    tileSize = new Vector2(tileSize, tileSize)
                };
            }

            float baseTileSize = detail.tileSize;

            var pathLayer  = GetOrCreateDetailLayer(td, detail.pathTex,  detail.pathScale,  baseTileSize, ref _cachedPathLayer);
            var grassLayer = GetOrCreateDetailLayer(td, detail.grassTex, detail.grassScale, baseTileSize, ref _cachedGrassLayer);
            var dirtLayer  = GetOrCreateDetailLayer(td, detail.dirtTex,  detail.dirtScale,  baseTileSize, ref _cachedDirtLayer);
            var waterLayer = GetOrCreateDetailLayer(td, detail.waterTex, detail.waterScale, baseTileSize, ref _cachedWaterLayer);

            bool needsNewLayers = td.terrainLayers == null || td.terrainLayers.Length < 5 ||
                                  td.terrainLayers[0] != satLayer ||
                                  td.terrainLayers[1] != pathLayer ||
                                  td.terrainLayers[2] != grassLayer ||
                                  td.terrainLayers[3] != dirtLayer ||
                                  td.terrainLayers[4] != waterLayer;

            if (needsNewLayers)
            {
                td.terrainLayers = new[] { satLayer, pathLayer, grassLayer, dirtLayer, waterLayer };

                int res = td.alphamapResolution;
                var alpha = new float[res, res, 5];
                for (int z = 0; z < res; z++)
                    for (int x = 0; x < res; x++)
                        alpha[z, x, 0] = 1f;

                td.SetAlphamaps(0, 0, alpha);
            }

            t.basemapDistance = float.MaxValue;
        }

        // ── Height conversion helpers ─────────────────────────────

        private static float[,] Upsample256To257(float[,] src, float terrainY, float range, float elevScale)
        {
            float[,] dst = new float[257, 257];
            float invRange = 1f / range;

            for (int z = 0; z < 257; z++)
            {
                float vz = z / 256f * 255f;
                int z0 = Mathf.Clamp((int)vz, 0, 254);
                int z1 = z0 + 1;
                float fz = vz - z0;

                for (int x = 0; x < 257; x++)
                {
                    float vx = x / 256f * 255f;
                    int x0 = Mathf.Clamp((int)vx, 0, 254);
                    int x1 = x0 + 1;
                    float fx = vx - x0;

                    float h_m = src[x0, z0] * (1 - fx) * (1 - fz) +
                                src[x1, z0] * fx * (1 - fz) +
                                src[x0, z1] * (1 - fx) * fz +
                                src[x1, z1] * fx * fz;

                    float h = h_m * elevScale;
                    dst[z, x] = (h - terrainY) * invRange;
                }
            }

            return dst;
        }

        private static void Downsample257To256(float[,] src, float[,] dst, float sizeY, float terrainY, float elevScale)
        {
            for (int z = 0; z < 256; z++)
            {
                float vz = z / 255f * 256f;
                int z0 = Mathf.Clamp((int)vz, 0, 255);
                int z1 = Mathf.Min(z0 + 1, 256);
                float fz = vz - z0;

                for (int x = 0; x < 256; x++)
                {
                    float vx = x / 255f * 256f;
                    int x0 = Mathf.Clamp((int)vx, 0, 255);
                    int x1 = Mathf.Min(x0 + 1, 256);
                    float fx = vx - x0;

                    float h = src[z0, x0] * (1 - fx) * (1 - fz) +
                              src[z0, x1] * fx * (1 - fz) +
                              src[z1, x0] * (1 - fx) * fz +
                              src[z1, x1] * fx * fz;

                    float elevScaled = h * sizeY + terrainY;
                    float elevMeters = elevScaled / elevScale;
                    dst[x, z] = elevMeters;
                }
            }
        }

        private static void CopyDirect(float[,] src, float[,] dst, float sizeY, float terrainY, int srcRes, float elevScale)
        {
            float scale = 255f / (srcRes - 1);

            for (int z = 0; z < 256; z++)
            {
                int sz = Mathf.Clamp((int)(z * scale), 0, srcRes - 1);
                for (int x = 0; x < 256; x++)
                {
                    int sx = Mathf.Clamp((int)(x * scale), 0, srcRes - 1);
                    float elevScaled = src[sz, sx] * sizeY + terrainY;
                    float elevMeters = elevScaled / elevScale;
                    dst[x, z] = elevMeters;
                }
            }
        }

        private static float GetMin(float[,] data)
        {
            float min = float.MaxValue;
            foreach (var v in data)
                if (v < min) min = v;
            return min;
        }

        private static float GetMax(float[,] data)
        {
            float max = float.MinValue;
            foreach (var v in data)
                if (v > max) max = v;
            return max;
        }

        private static int CountNonZero(short[,] data)
        {
            int count = 0;
            foreach (var v in data)
                if (v != 0) count++;
            return count;
        }
    }
}
