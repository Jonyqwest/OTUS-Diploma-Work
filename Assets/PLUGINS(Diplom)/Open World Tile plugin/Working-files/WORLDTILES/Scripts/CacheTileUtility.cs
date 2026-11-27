namespace WorldTile
{
    using System.IO;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using UnityEngine;
    using System;
    using System.IO.Compression;
    using Cysharp.Threading.Tasks;

    public class TileMetaData
    {
        public int pZ;
        public int pX;
        public int pY;
    }

    interface ICachFolder
    {
        public string GetName();
        public string GetExtension();
    }
    public class TextureCacheFolder : ICachFolder
    {
        public string GetName()
        {
            return CacheStat.TextureCacheFolder;
        }
        public string GetExtension()
        {
            return "png";
        }
    }
    public class HeightCacheFolder : ICachFolder
    {
        public string GetName()
        {
            return CacheStat.HeightCacheFolder;
        }
        public string GetExtension()
        {
            //return "elv";
            return "png";

        }
    }

    public static class CacheTileUtility
    {
        public static string dirRoot => CacheStat.CacheRoot;
        public static string dirTexture => CacheStat.TextureCacheFolder;
        public static string dirHeight => CacheStat.HeightCacheFolder;
        public static string dirElevation => CacheStat.ElevationCacheFolder;
        public static string metaExtention = "txt";


        public static readonly string TextureCacheDir = CacheStat.TextureCacheFolder;


        //простраиваем путь до кэша
        //public static string GetTextureCachPath(int x, int y, int zoom)
        //{
        //    return GetTileTexturePath(x, y, zoom, string dir, string extension = "png");
        //}

        public static string GetTextureCachPath(int x, int y, int zoom)
        {
            ICachFolder cachFolder = new TextureCacheFolder();

            var dir = Path.Combine(UnityEngine.Application.persistentDataPath, dirRoot, cachFolder.GetName(), $"{zoom}");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{x}_{y}.{cachFolder.GetExtension()}");
        }

        private static string GetHeightPath(int x, int y, int zoom)
        {
            ICachFolder cachFolder = new HeightCacheFolder();

            var dir = Path.Combine(UnityEngine.Application.persistentDataPath, dirRoot, cachFolder.GetName(), $"{zoom}");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{x}_{y}.{cachFolder.GetExtension()}");
        }
        private static string GetELVPath(int x, int y, int zoom)
        {
            string CacheFolder = CacheStat.GetElevationTilePath(zoom, x, y, ELVfileExtension);
            //Debug.Log("!!!! " + CacheFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFolder));
            return CacheFolder;
        }







        public static void SaveTileTextureToCache(Texture2D tex, ZXY key)
        {
            string path = GetTextureCachPath(key.x, key.y, key.z);
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        public static Texture2D LoadTileTextureFromCache(int x, int y, int zoom)
        {
            string path = GetTextureCachPath(x, y, zoom);
            return LoadTileTextureFromCache(path);
        }
        public static Texture2D LoadTileTextureFromCache(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            // Satellite tiles are only sampled/copied by the GPU after loading.
            // Marking them non-readable drops Unity's retained CPU pixel copy.
            if (!tex.LoadImage(bytes, true))
            {
                UnityEngine.Object.Destroy(tex);
                return null;
            }

            return tex;
        }

        //public static bool TryLoadTileMetaFromCache(int x, int y, int zoom, out TileMetaData meta)
        //{
        //    meta = null;
        //    string metaPath = GetTextureCachPath(x, y, zoom, metaExtention);
        //    if (!File.Exists(metaPath)) return false;

        //    try
        //    {
        //        string json = File.ReadAllText(metaPath);
        //        meta = JsonConvert.DeserializeObject<TileMetaData>(json);
        //        return true;
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Debug.LogWarning($"[CacheTileUtility] Failed to read meta: {ex}");
        //        return false;
        //    }
        //}



        // Генерация мета для всех детей родителя
        public static void GenerateMetaForChildren(int parentX, int parentY, int parentZoom, int targetZoom, string dir)
        {
            int levelDiff = targetZoom - parentZoom;
            if (levelDiff <= 0) return;

            int childrenPerSide = 1 << levelDiff; // 2^levelDiff
            int startX = parentX * childrenPerSide;
            int startY = parentY * childrenPerSide;

            for (int dx = 0; dx < childrenPerSide; dx++)
            {
                for (int dy = 0; dy < childrenPerSide; dy++)
                {
                    int childX = startX + dx;
                    int childY = startY + dy;

                    // Сохраняем .meta для child тайла
                    var meta = new TileMetaData
                    {
                        pZ = parentZoom,
                        pX = parentX,
                        pY = parentY
                    };
                    string json = JsonConvert.SerializeObject(meta);
                    //string metaPath = GetTextureCachPath(childX, childY, targetZoom, dir, metaExtention);
                    //File.WriteAllText(metaPath, json);
                }
            }
        }
        //// Загрузка тайла с учетом возможного fallback
        //public static async Task<Texture2D> LoadTileTextureWithFallbackAsync(int x, int y, int zoom)
        //{
        //    // Проверим естьли PNG кеш
        //    string tilePath = GetTextureCachPath(x, y, zoom);
        //    if (File.Exists(tilePath))
        //    {
        //        byte[] bytes = File.ReadAllBytes(tilePath);
        //        var tex = new Texture2D(2, 2);
        //        tex.LoadImage(bytes);
        //        return tex;
        //    }

        //    // Проверим есть ли .meta
        //    if (TryLoadTileMetaFromCache(x, y, zoom, out TileMetaData meta))
        //    {
        //        // Рекурсивно пробуем загрузить тайл из fallback
        //        Texture2D parentTex = await LoadTileTextureWithFallbackAsync(meta.pX, meta.pY, meta.pZ);
        //        if (parentTex == null)
        //            return null;
        //        Texture2D result = CropTextureTile(parentTex);

        //        // Тут надо вырезать из parentTex кусок (если надо) и масштабировать, сейчас возвращаем целиком для простоты
        //        // TODO: добавить ресемпл нужного куска, если нужно

        //        return result;
        //    }

        //    return null;
        //}

        //вырезание куска
        private static Texture2D CropTextureTile(Texture2D tex)
        {
            //tex = null;//
            return tex;
        }










        ///!!!!!!!!!!!!!!!!!!!!!!ELEVATIONS!!!!!!!!!!!!!!!!!!!!!!!!!!!//



        private const string ELVfileExtension = "elv";
        private const string ELVMagic = "ELEV"; // 4 байта
        private const byte ELVversion = 2; // Версия 2: Min-Max + 16-бит адаптивное дельта-сжатие

        public static Texture2D LoadTileElevationFromCache(int x, int y, int zoom)
        {
            string path = GetHeightPath(x, y, zoom);
            if (!File.Exists(path)) return null;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
                return tex;

            UnityEngine.Object.Destroy(tex);
            return null;
        }

        public static void SaveTileElevationToCache(Texture2D tex, int x, int y, int zoom)
        {
            string path = GetHeightPath(x, y, zoom);

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        public static void SaveElevationDataCompressed(float[,] data, int x, int y, int zoom)
        {
            if (data == null) return;
            string path = GetELVPath(x, y, zoom);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            int size = data.GetLength(0);

            // Находим точный локальный диапазон высот тайла
            float minH = float.MaxValue;
            float maxH = float.MinValue;
            for (int yIdx = 0; yIdx < size; yIdx++)
            {
                for (int xIdx = 0; xIdx < size; xIdx++)
                {
                    float v = data[xIdx, yIdx];
                    if (v < minH) minH = v;
                    if (v > maxH) maxH = v;
                }
            }

            float range = maxH - minH;
            if (range < 0f) range = 0f;

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using DeflateStream deflate = new DeflateStream(fs, System.IO.Compression.CompressionLevel.Optimal);
            using BinaryWriter writer = new BinaryWriter(deflate);

            writer.Write(ELVMagic.ToCharArray());       // 4 bytes
            writer.Write(ELVversion);                   // 1 byte: 2
            writer.Write((ushort)size);                 // 2 bytes: 256
            writer.Write(minH);                         // 4 bytes: min height
            writer.Write(range);                        // 4 bytes: height range

            if (range > 0.0001f)
            {
                float invRange = 65535f / range;
                for (int yIdx = 0; yIdx < size; yIdx++)
                {
                    int prevU = 0;
                    for (int xIdx = 0; xIdx < size; xIdx++)
                    {
                        float norm = (data[xIdx, yIdx] - minH) * invRange;
                        int u = Mathf.Clamp(Mathf.RoundToInt(norm), 0, 65535);
                        if (xIdx == 0)
                        {
                            // Первый пиксель строки — абсолютное значение (ushort),
                            // чтобы избежать переполнения signed short при u > 32767
                            writer.Write((ushort)u);
                        }
                        else
                        {
                            short delta = (short)(u - prevU);
                            writer.Write(delta);
                        }
                        prevU = u;
                    }
                }
            }
        }

        public static void SaveElevationDataCompressed(short[,] data, int x, int y, int zoom)
        {
            if (data == null) return;
            int size = data.GetLength(0);
            float[,] floatData = new float[size, size];
            for (int yIdx = 0; yIdx < size; yIdx++)
                for (int xIdx = 0; xIdx < size; xIdx++)
                    floatData[xIdx, yIdx] = data[xIdx, yIdx];
            SaveElevationDataCompressed(floatData, x, y, zoom);
        }

        public static float[,] LoadElevationDataCompressed(int x, int y, int zoom)
        {
            string path = GetELVPath(x, y, zoom);
            return LoadElevationDataCompressed(path);
        }

        public static float[,] LoadElevationDataCompressed(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using DeflateStream deflate = new DeflateStream(fs, CompressionMode.Decompress);
                using BinaryReader reader = new BinaryReader(deflate);

                char[] magic = reader.ReadChars(4);
                if (new string(magic) != ELVMagic)
                    throw new Exception("Invalid file signature");

                byte version = reader.ReadByte();
                int size = reader.ReadUInt16();
                if (size != TileStat.ElevationTileResolution)
                    throw new InvalidDataException($"Invalid elevation size: {size}");

                float[,] data = new float[size, size];

                if (version == 1)
                {
                    // Легаси формат (версия 1): целые short[,]
                    for (int yIdx = 0; yIdx < size; yIdx++)
                        for (int xIdx = 0; xIdx < size; xIdx++)
                            data[xIdx, yIdx] = reader.ReadInt16();
                }
                else if (version == 2)
                {
                    // Адаптивный субметровый формат (версия 2): Min-Max + 16-бит дельта
                    float minH = reader.ReadSingle();
                    float range = reader.ReadSingle();

                    if (range <= 0.0001f)
                    {
                        for (int yIdx = 0; yIdx < size; yIdx++)
                            for (int xIdx = 0; xIdx < size; xIdx++)
                                data[xIdx, yIdx] = minH;
                    }
                    else
                    {
                        float step = range / 65535f;
                        for (int yIdx = 0; yIdx < size; yIdx++)
                        {
                            int u = 0;
                            for (int xIdx = 0; xIdx < size; xIdx++)
                            {
                                if (xIdx == 0)
                                {
                                    // Первый пиксель строки — абсолютное значение (ushort)
                                    u = reader.ReadUInt16();
                                }
                                else
                                {
                                    short delta = reader.ReadInt16();
                                    u += delta;
                                }
                                data[xIdx, yIdx] = minH + u * step;
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unsupported ELV version: {version}");
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ElevationCacheUtility] Error loading compressed data: {ex.Message}");
                return null;
            }
        }


    }
}
