namespace WorldTile
{
    using System;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// Глобальный сервис запроса высот (Global Elevation Service).
    /// Позволяет в любой момент времени мгновенно получить высоту в метрах над уровнем моря
    /// в любой точке мира, используя базовый глобальный кэш высот (Zoom 8)
    /// с автоматическим наложением редакторских правок (Custom Delta), если они существуют.
    /// </summary>
    public static class GlobalElevation
    {
        /// <summary>
        /// Базовый зум для глобального покрытия высот (Zoom 8).
        /// </summary>
        public static byte LowElevationZoom => TileStat.LowElevationZoom;

        /// <summary>
        /// Получить высоту в метрах над уровнем моря по структуре WorldPosition.
        /// Автоматически учитывает базовую лоу-высоту (Zoom 8) + редакторские правки (если есть).
        /// </summary>
        public static float GetHeight(WorldPosition worldPosition)
        {
            // 1. Получаем базовую высоту на основе лоу-кеша (Zoom 8)
            float baseHeight = GetLowElevationHeight(worldPosition);

            // 2. Проверяем наличие редакторских высот (custom delta)
            float editorDelta = GetEditorDelta(worldPosition);

            return baseHeight + editorDelta;
        }

        /// <summary>
        /// Получить высоту в метрах над уровнем моря по Unity мировым координатам (Vector3).
        /// </summary>
        public static float GetHeight(Vector3 unityWorldPosition)
        {
            var wp = new WorldPosition().FromUnityPosition(unityWorldPosition, LowElevationZoom);
            return GetHeight(wp);
        }

        /// <summary>
        /// Получить высоту в метрах над уровнем моря по географическим координатам (широта, долгота).
        /// </summary>
        public static float GetHeight(double latitude, double longitude)
        {
            byte zoom = LowElevationZoom;
            int n = 1 << zoom; // 256 для Zoom 8

            double x = (longitude + 180.0) / 360.0 * n;
            x = ((x % n) + n) % n;

            double latRad = latitude * Math.PI / 180.0;
            double tanLat = Math.Tan(latRad);
            double cosLat = Math.Cos(latRad);
            double y = (1.0 - Math.Log(tanLat + 1.0 / cosLat) / Math.PI) / 2.0 * n;

            int tileX = (int)Math.Floor(x);
            int tileY = Mathf.Clamp((int)Math.Floor(y), 0, n - 1);

            float u = (float)(x - tileX);
            float v = 1f - (float)(y - tileY); // Инвертируем: 0 - южный край (South), 1 - северный (North)

            var wp = new WorldPosition(zoom, tileX, tileY, new Vector2(u, v));
            return GetHeight(wp);
        }

        /// <summary>
        /// Получить Unity Y координату точки (с учетом компенсации позиции и масштаба ElevationScale).
        /// </summary>
        public static float GetUnityHeight(Vector3 unityWorldPosition)
        {
            float heightMeters = GetHeight(unityWorldPosition);
            return heightMeters * TileStat.ElevationScale - TileStat.compensPos.y;
        }

        /// <summary>
        /// Получить Unity Y координату точки для заданной WorldPosition.
        /// </summary>
        public static float GetUnityHeight(WorldPosition worldPosition)
        {
            float heightMeters = GetHeight(worldPosition);
            return heightMeters * TileStat.ElevationScale - TileStat.compensPos.y;
        }

        /// <summary>
        /// Возвращает Vector3 с привязкой по высоте к рельефу (сохраняя X и Z).
        /// </summary>
        public static Vector3 GetGroundPosition(Vector3 unityWorldPosition)
        {
            float y = GetUnityHeight(unityWorldPosition);
            return new Vector3(unityWorldPosition.x, y, unityWorldPosition.z);
        }

        // ─────────────────────────────────────────────────────────────
        // Базовая лоу-высота (Zoom 8)
        // ─────────────────────────────────────────────────────────────

        private static float GetLowElevationHeight(WorldPosition wp)
        {
            byte zoom = LowElevationZoom;
            int tileX, tileY;
            float u, v;

            if (wp.zoom == zoom)
            {
                tileX = wp.tileX;
                tileY = wp.tileY;
                u = wp.localOffset.x;
                v = wp.localOffset.y;
            }
            else
            {
                Vector3 unityPos = wp.ToUnityPosition();
                var wpLow = new WorldPosition().FromUnityPosition(unityPos, zoom);
                tileX = wpLow.tileX;
                tileY = wpLow.tileY;
                u = wpLow.localOffset.x;
                v = wpLow.localOffset.y;
            }

            int maxTiles = 1 << zoom;
            int tx = ((tileX % maxTiles) + maxTiles) % maxTiles;
            int ty = Mathf.Clamp(tileY, 0, maxTiles - 1);

            float[,] map = LoadLowElevationTile(tx, ty, zoom);
            if (map == null)
                return 0f;

            return SampleBilinear(map, u, v);
        }

        private static ElevationAtlasReader atlasReader;
        private static bool atlasChecked = false;
        private static readonly object atlasLock = new object();

        private static ElevationAtlasReader GetAtlasReader()
        {
            if (atlasChecked) return atlasReader;

            lock (atlasLock)
            {
                if (atlasChecked) return atlasReader;

                string[] candidatePaths =
                {
                    Path.Combine(Application.streamingAssetsPath, "LowElevation", $"world_z{LowElevationZoom}.atlas"),
                    Path.Combine(Application.streamingAssetsPath, "LowElevation", "world_z9.atlas"),
                    Path.Combine(Application.persistentDataPath, "LowTiles", $"world_z{LowElevationZoom}.atlas"),
                    Path.Combine(Application.persistentDataPath, "LowTiles", "world_z9.atlas"),
#if UNITY_EDITOR
                    Path.Combine(Application.dataPath, "LowElevation", $"world_z{LowElevationZoom}.atlas"),
                    Path.Combine(Application.dataPath, "LowElevation", "world_z9.atlas")
#endif
                };

                foreach (var path in candidatePaths)
                {
                    if (File.Exists(path))
                    {
                        atlasReader = new ElevationAtlasReader(path);
                        if (atlasReader.IsLoaded)
                        {
                            Debug.Log($"[GlobalElevation] Подключен Атлас Высот: {path}");
                            break;
                        }
                        else
                        {
                            atlasReader = null;
                        }
                    }
                }

                atlasChecked = true;
                return atlasReader;
            }
        }

        public static float[,] LoadLowElevationTile(int x, int y, int zoom)
        {
            // 0. Если есть смонтированный Атлас Высот нужного зума — берем из него мгновенно
            var atlas = GetAtlasReader();
            if (atlas != null && atlas.IsLoaded && atlas.Zoom == zoom)
            {
                if (atlas.TryGetTile(x, y, out float[,] atlasHeights))
                {
                    return atlasHeights;
                }
            }

            // 1. Попробовать основной путь кэша .elv
            float[,] data = CacheTileUtility.LoadElevationDataCompressed(x, y, zoom);
            if (data != null) return data;

            // 2. Попробовать альтернативные пути (.elv в LowTiles или StreamingAssets)
            string[] elvFallbacks =
            {
                Path.Combine(Application.persistentDataPath, "LowTiles", "LowElevation", $"{zoom}/{x}_{y}.elv"),
                Path.Combine(Application.persistentDataPath, "LowTiles", $"{zoom}/{x}_{y}.elv"),
                Path.Combine(Application.streamingAssetsPath, "LowElevation", $"{zoom}/{x}_{y}.elv"),
#if UNITY_EDITOR
                Path.Combine(Application.dataPath, "LowElevation", $"{zoom}/{x}_{y}.elv")
#endif
            };

            foreach (var fb in elvFallbacks)
            {
                if (File.Exists(fb))
                {
                    data = CacheTileUtility.LoadElevationDataCompressed(fb);
                    if (data != null) return data;
                }
            }

            // 3. Fallback: terrarium PNG/JPG изображение
            string[] imgFallbacks =
            {
                CacheStat.GetHeightTilePath(zoom, x, y, "png"),
                CacheStat.GetHeightTilePath(zoom, x, y, "jpg"),
                Path.Combine(Application.persistentDataPath, "LowTiles", "LowElevation", $"{zoom}/{x}_{y}.png"),
                Path.Combine(Application.persistentDataPath, "LowTiles", "LowElevation", $"{zoom}/{x}_{y}.jpg"),
                Path.Combine(Application.streamingAssetsPath, "LowElevation", $"{zoom}/{x}_{y}.png"),
                Path.Combine(Application.streamingAssetsPath, "LowElevation", $"{zoom}/{x}_{y}.jpg")
            };

            foreach (var imgPath in imgFallbacks)
            {
                if (File.Exists(imgPath))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(imgPath);
                        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(bytes))
                        {
                            var decoded = TerrainTile.DecodeTerrarium(tex);
                            UnityEngine.Object.DestroyImmediate(tex);
                            if (decoded != null)
                            {
                                // Кэшируем для последующих быстрых обращений
                                CacheTileUtility.SaveElevationDataCompressed(decoded, x, y, zoom);
                                return decoded;
                            }
                        }
                        else
                        {
                            UnityEngine.Object.DestroyImmediate(tex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[GlobalElevation] Error loading fallback image {imgPath}: {ex.Message}");
                    }
                }
            }

            return null;
        }

        // ─────────────────────────────────────────────────────────────
        // Редакторские высоты (Custom Delta)
        // ─────────────────────────────────────────────────────────────

        private static float GetEditorDelta(WorldPosition wp)
        {
            if (!HasAnyCustomEdits())
                return 0f;

            Vector3 unityPos = wp.ToUnityPosition();

            // Ищем дельту от наивысшего разрешения (BaseZoom = 15) вниз до LowElevationZoom (8).
            // Первое найденное совпадение — наиболее детальная оригинальная правка.
            for (int z = TileStat.BaseZoom; z >= TileStat.LowElevationZoom; z--)
            {
                if (!HasEditsFolderForZoom(z))
                    continue;

                WorldPosition wpZ;
                if (wp.zoom == z)
                {
                    wpZ = wp;
                }
                else
                {
                    wpZ = new WorldPosition().FromUnityPosition(unityPos, (byte)z);
                }

                short[,] delta = CustomDeltaApplier.LoadDelta(wpZ.tileX, wpZ.tileY, z);
                if (delta != null)
                {
                    float u = Mathf.Clamp01(wpZ.localOffset.x);
                    float v = Mathf.Clamp01(wpZ.localOffset.y);
                    return SampleBilinear(delta, u, v);
                }
            }

            return 0f;
        }

        private static bool HasAnyCustomEdits()
        {
#if UNITY_EDITOR
            if (Directory.Exists(Path.Combine(Application.dataPath, "CustomEdits")))
                return true;
#endif
            return Directory.Exists(Path.Combine(Application.persistentDataPath, "CustomEdits"));
        }

        private static bool HasEditsFolderForZoom(int z)
        {
            string zStr = z.ToString();
#if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "CustomEdits", zStr);
            if (Directory.Exists(editorPath)) return true;
#endif
            string persistentPath = Path.Combine(Application.persistentDataPath, "CustomEdits", zStr);
            return Directory.Exists(persistentPath);
        }

        // ─────────────────────────────────────────────────────────────
        // Билинейная интерполяция
        // ─────────────────────────────────────────────────────────────

        private static float SampleBilinear(float[,] map, float u, float v)
        {
            if (map == null) return 0f;
            int size = map.GetLength(0);
            float px = Mathf.Clamp(u * (size - 1), 0f, size - 1);
            float py = Mathf.Clamp(v * (size - 1), 0f, size - 1);

            int x0 = Mathf.FloorToInt(px);
            int y0 = Mathf.FloorToInt(py);
            int x1 = Mathf.Min(x0 + 1, size - 1);
            int y1 = Mathf.Min(y0 + 1, size - 1);

            float fx = px - x0;
            float fy = py - y0;

            float h00 = map[x0, y0];
            float h10 = map[x1, y0];
            float h01 = map[x0, y1];
            float h11 = map[x1, y1];

            float h0 = h00 * (1f - fx) + h10 * fx;
            float h1 = h01 * (1f - fx) + h11 * fx;

            return h0 * (1f - fy) + h1 * fy;
        }

        private static float SampleBilinear(short[,] map, float u, float v)
        {
            if (map == null) return 0f;
            int size = map.GetLength(0);
            float px = Mathf.Clamp(u * (size - 1), 0f, size - 1);
            float py = Mathf.Clamp(v * (size - 1), 0f, size - 1);

            int x0 = Mathf.FloorToInt(px);
            int y0 = Mathf.FloorToInt(py);
            int x1 = Mathf.Min(x0 + 1, size - 1);
            int y1 = Mathf.Min(y0 + 1, size - 1);

            float fx = px - x0;
            float fy = py - y0;

            float h00 = map[x0, y0];
            float h10 = map[x1, y0];
            float h01 = map[x0, y1];
            float h11 = map[x1, y1];

            float h0 = h00 * (1f - fx) + h10 * fx;
            float h1 = h01 * (1f - fx) + h11 * fx;

            return h0 * (1f - fy) + h1 * fy;
        }
    }
}
