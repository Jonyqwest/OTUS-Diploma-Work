namespace WorldTile
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using UnityEngine;

    /// <summary>
    /// Ридер бинарного атласа глобальных высот (.atlas / .bin).
    /// Позволяет мгновенно за O(1) считывать тайлы высот по смещению без распаковки гигантских архивов.
    /// Поддерживает автоматическое определение океанических тайлов (Offset = 0).
    /// Потокобезопасен и использует небольшой LRU-кэш для минимизации обращений к диску.
    /// </summary>
    public class ElevationAtlasReader : IDisposable
    {
        private const uint ATLAS_MAGIC = 0x4154574F; // "OWTA"
        private const ushort CURRENT_VERSION = 1;

        public byte Zoom { get; private set; }
        public ushort GridDim { get; private set; }
        public ushort TileResolution { get; private set; }
        public uint TotalTiles { get; private set; }

        private struct TileEntry
        {
            public ulong fileOffset;
            public ushort compressedSize;
            public ushort flags;
        }

        private TileEntry[] indexTable;
        private FileStream fileStream;
        private readonly object streamLock = new object();
        private readonly string filePath;

        // Небольшой LRU кэш для последних считанных тайлов (потокобезопасный)
        private readonly Dictionary<int, float[,]> tileLruCache = new Dictionary<int, float[,]>(64);
        private readonly LinkedList<int> lruOrder = new LinkedList<int>();
        private readonly object cacheLock = new object();
        private const int MAX_CACHE_SIZE = 32;

        public bool IsLoaded => indexTable != null && fileStream != null;

        public ElevationAtlasReader(string path)
        {
            filePath = path;
            LoadIndex();
        }

        private void LoadIndex()
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[ElevationAtlasReader] Файл атласа не найден: {filePath}");
                return;
            }

            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    uint magic = reader.ReadUInt32();
                    if (magic != ATLAS_MAGIC)
                    {
                        Debug.LogError($"[ElevationAtlasReader] Неверная сигнатура атласа: 0x{magic:X8}, ожидалось 0x{ATLAS_MAGIC:X8}");
                        Close();
                        return;
                    }

                    ushort version = reader.ReadUInt16();
                    if (version != CURRENT_VERSION)
                    {
                        Debug.LogError($"[ElevationAtlasReader] Неподдерживаемая версия атласа: {version}");
                        Close();
                        return;
                    }

                    Zoom = reader.ReadByte();
                    reader.ReadByte(); // reserved
                    GridDim = reader.ReadUInt16();
                    TileResolution = reader.ReadUInt16();
                    TotalTiles = reader.ReadUInt32();

                    // Пропуск оставшихся 16 байт заголовка (всего заголовок 32 байта)
                    fileStream.Seek(32, SeekOrigin.Begin);

                    int entryCount = (int)TotalTiles;
                    indexTable = new TileEntry[entryCount];

                    byte[] indexRaw = reader.ReadBytes(entryCount * 12);
                    for (int i = 0; i < entryCount; i++)
                    {
                        int baseOffset = i * 12;
                        indexTable[i].fileOffset = BitConverter.ToUInt64(indexRaw, baseOffset);
                        indexTable[i].compressedSize = BitConverter.ToUInt16(indexRaw, baseOffset + 8);
                        indexTable[i].flags = BitConverter.ToUInt16(indexRaw, baseOffset + 10);
                    }

                    Debug.Log($"[ElevationAtlasReader] Атлас успешно открыт: Z{Zoom}, сетка {GridDim}x{GridDim} ({TotalTiles} тайлов), размер индекса {indexRaw.Length / 1024} КБ.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElevationAtlasReader] Ошибка при чтении индекса атласа: {ex.Message}");
                Close();
            }
        }

        /// <summary>
        /// Попытаться получить карту высот 256x256 для тайла (x, y) на зуме атласа.
        /// Возвращает true при успехе. Для океанических тайлов возвращает массив нулей.
        /// </summary>
        public bool TryGetTile(int x, int y, out float[,] heightMap)
        {
            heightMap = null;
            if (!IsLoaded) return false;

            if (x < 0 || x >= GridDim || y < 0 || y >= GridDim)
                return false;

            int linearIndex = y * GridDim + x;

            // 1. Проверяем LRU-кэш
            lock (cacheLock)
            {
                if (tileLruCache.TryGetValue(linearIndex, out heightMap))
                {
                    lruOrder.Remove(linearIndex);
                    lruOrder.AddLast(linearIndex);
                    return true;
                }
            }

            TileEntry entry = indexTable[linearIndex];

            // 2. Если смещение 0 и размер 0 — это чистый океан (0 м)
            if (entry.fileOffset == 0 && entry.compressedSize == 0)
            {
                heightMap = new float[TileResolution, TileResolution];
                PutCache(linearIndex, heightMap);
                return true;
            }

            // 3. Читаем сжатый блок данных тайла с диска
            byte[] compressedData;
            lock (streamLock)
            {
                if (fileStream == null) return false;
                fileStream.Seek((long)entry.fileOffset, SeekOrigin.Begin);
                compressedData = new byte[entry.compressedSize];
                int read = fileStream.Read(compressedData, 0, entry.compressedSize);
                if (read != entry.compressedSize)
                {
                    Debug.LogWarning($"[ElevationAtlasReader] Не удалось прочитать полный блок для тайла ({x}, {y}): прочитано {read}/{entry.compressedSize}");
                    return false;
                }
            }

            // 4. Декомпрессия Deflate и восстановление дельта-высот
            try
            {
                heightMap = DecompressTileData(compressedData, TileResolution);
                if (heightMap != null)
                {
                    PutCache(linearIndex, heightMap);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElevationAtlasReader] Ошибка декомпрессии тайла ({x}, {y}): {ex.Message}");
            }

            return false;
        }

        private float[,] DecompressTileData(byte[] compressedBytes, int resolution)
        {
            using (var memoryStream = new MemoryStream(compressedBytes))
            using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
            using (var reader = new BinaryReader(deflateStream))
            {
                // Проверяем наличие стандартного заголовка .elv (ELEV)
                char[] magic = reader.ReadChars(4);
                float minHeight = 0f;
                float heightRange = 0f;
                int size = resolution;

                if (new string(magic) == "ELEV")
                {
                    byte version = reader.ReadByte();
                    size = reader.ReadUInt16();
                    minHeight = reader.ReadSingle();
                    heightRange = reader.ReadSingle();
                }
                else
                {
                    // Fallback если заголовок без ELEV: первые 4 байта были minHeight
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using var fallbackDeflate = new DeflateStream(memoryStream, CompressionMode.Decompress);
                    using var fallbackReader = new BinaryReader(fallbackDeflate);
                    minHeight = fallbackReader.ReadSingle();
                    heightRange = fallbackReader.ReadSingle();
                    return ReadHeights(fallbackReader, size, minHeight, heightRange);
                }

                return ReadHeights(reader, size, minHeight, heightRange);
            }
        }

        private static float[,] ReadHeights(BinaryReader reader, int size, float minHeight, float heightRange)
        {
            float[,] heights = new float[size, size];
            if (heightRange <= 0.0001f)
            {
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        heights[x, y] = minHeight;
                return heights;
            }

            for (int y = 0; y < size; y++)
            {
                ushort currentUshort = reader.ReadUInt16();
                heights[0, y] = minHeight + ((float)currentUshort / 65535f) * heightRange;

                for (int x = 1; x < size; x++)
                {
                    short delta = reader.ReadInt16();
                    currentUshort = (ushort)(currentUshort + delta);
                    heights[x, y] = minHeight + ((float)currentUshort / 65535f) * heightRange;
                }
            }

            return heights;
        }

        private void PutCache(int key, float[,] data)
        {
            lock (cacheLock)
            {
                if (tileLruCache.ContainsKey(key))
                {
                    lruOrder.Remove(key);
                }
                else if (tileLruCache.Count >= MAX_CACHE_SIZE)
                {
                    int oldest = lruOrder.First.Value;
                    lruOrder.RemoveFirst();
                    tileLruCache.Remove(oldest);
                }

                tileLruCache[key] = data;
                lruOrder.AddLast(key);
            }
        }

        private void Close()
        {
            lock (streamLock)
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                    fileStream = null;
                }
            }
            indexTable = null;
        }

        public void Dispose()
        {
            Close();
            lock (cacheLock)
            {
                tileLruCache.Clear();
                lruOrder.Clear();
            }
        }
    }
}
