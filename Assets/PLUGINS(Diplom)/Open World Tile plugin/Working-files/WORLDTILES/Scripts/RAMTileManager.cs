using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер памяти для высотных тайлов (short[,]).
/// Хранит в памяти ограниченное число тайлов и выгружает старые.
/// </summary>
public class ElevationTileMemoryManager
{
    private class TileData
    {
        public short[,] Heights;
        public DateTime LastAccess;
        public long MemorySizeBytes;
    }

    // Ключ = (z, x, y) тайла
    private readonly Dictionary<(int z, int x, int y), TileData> _cache = new();

    // Лимит памяти в байтах
    private readonly long _memoryLimitBytes;

    // Текущий объём памяти
    private long _currentMemoryBytes;

    public ElevationTileMemoryManager(long memoryLimitMB = 256)
    {
        _memoryLimitBytes = memoryLimitMB * 1024L * 1024L;
    }

    /// <summary>
    /// Добавить тайл в память.
    /// </summary>
    public void AddTile(int z, int x, int y, short[,] heights)
    {
        var key = (z, x, y);

        // Если тайл уже есть — обновим
        if (_cache.ContainsKey(key))
        {
            _currentMemoryBytes -= _cache[key].MemorySizeBytes;
            _cache.Remove(key);
        }

        long sizeBytes = sizeof(short) * heights.Length;

        _cache[key] = new TileData
        {
            Heights = heights,
            LastAccess = DateTime.UtcNow,
            MemorySizeBytes = sizeBytes
        };

        _currentMemoryBytes += sizeBytes;

        EnforceMemoryLimit();
    }

    /// <summary>
    /// Получить тайл из памяти (или null если нет).
    /// </summary>
    public short[,] GetTile(int z, int x, int y)
    {
        var key = (z, x, y);
        if (_cache.TryGetValue(key, out var tile))
        {
            tile.LastAccess = DateTime.UtcNow;
            return tile.Heights;
        }
        return null;
    }

    /// <summary>
    /// Удалить самые старые тайлы, пока память не впишется в лимит.
    /// </summary>
    private void EnforceMemoryLimit()
    {
        if (_currentMemoryBytes <= _memoryLimitBytes)
            return;

        // Отсортируем по времени последнего доступа (Least Recently Used)
        var ordered = new List<KeyValuePair<(int, int, int), TileData>>(_cache);
        ordered.Sort((a, b) => a.Value.LastAccess.CompareTo(b.Value.LastAccess));

        foreach (var kv in ordered)
        {
            _cache.Remove(kv.Key);
            _currentMemoryBytes -= kv.Value.MemorySizeBytes;

            if (_currentMemoryBytes <= _memoryLimitBytes)
                break;
        }
    }

    /// <summary>
    /// Текущий объём памяти (в МБ).
    /// </summary>
    public float CurrentMemoryMB => _currentMemoryBytes / (1024f * 1024f);

    /// <summary>
    /// Количество тайлов в памяти.
    /// </summary>
    public int TileCount => _cache.Count;
}








/// <summary>
/// Менеджер кэша высот тайлов. Работает по принципу LRU (least recently used).
/// </summary>
public class TileElevationCacheManager
{
    private class CacheEntry
    {
        public float[,] Heights;   // Сам массив высот
        public long LastAccess;    // Когда последний раз обращались
        public int MemorySize;     // Примерный размер в байтах
    }

    // 🔹 Ключ — уникальный ID тайла (zoom/x/y)
    private Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();

    private long currentMemoryUsage = 0;
    private readonly long memoryLimitBytes; // Лимит памяти

    public TileElevationCacheManager(long memoryLimitBytes = 256 * 1024 * 1024) // 256 Мб по умолчанию
    {
        this.memoryLimitBytes = memoryLimitBytes;
    }

    private string GetKey(int zoom, int x, int y) => $"{zoom}_{x}_{y}";

    /// <summary>
    /// Добавляем массив высот в кэш.
    /// </summary>
    public void Add(int zoom, int x, int y, float[,] heights)
    {
        string key = GetKey(zoom, x, y);

        if (cache.ContainsKey(key))
        {
            // обновляем существующую запись
            currentMemoryUsage -= cache[key].MemorySize;
            cache.Remove(key);
        }

        int size = sizeof(float) * heights.Length;

        var entry = new CacheEntry
        {
            Heights = heights,
            LastAccess = DateTime.UtcNow.Ticks,
            MemorySize = size
        };

        cache[key] = entry;
        currentMemoryUsage += size;

        TrimIfNeeded();
    }

    /// <summary>
    /// Получаем массив высот, если он есть в кэше.
    /// </summary>
    public bool TryGet(int zoom, int x, int y, out float[,] heights)
    {
        string key = GetKey(zoom, x, y);

        if (cache.TryGetValue(key, out var entry))
        {
            entry.LastAccess = DateTime.UtcNow.Ticks;
            heights = entry.Heights;
            return true;
        }

        heights = null;
        return false;
    }

    /// <summary>
    /// Удаляет самые старые данные, пока память не уложится в лимит.
    /// </summary>
    private void TrimIfNeeded()
    {
        if (currentMemoryUsage <= memoryLimitBytes) return;

        // Сортируем по LastAccess (старые вначале)
        List<KeyValuePair<string, CacheEntry>> list = new List<KeyValuePair<string, CacheEntry>>(cache);
        list.Sort((a, b) => a.Value.LastAccess.CompareTo(b.Value.LastAccess));

        foreach (var kvp in list)
        {
            cache.Remove(kvp.Key);
            currentMemoryUsage -= kvp.Value.MemorySize;

            if (currentMemoryUsage <= memoryLimitBytes)
                break;
        }
    }

    /// <summary>
    /// Очистить всё.
    /// </summary>
    public void Clear()
    {
        cache.Clear();
        currentMemoryUsage = 0;
    }

    public long CurrentUsage => currentMemoryUsage;
    public long MemoryLimit => memoryLimitBytes;
}
