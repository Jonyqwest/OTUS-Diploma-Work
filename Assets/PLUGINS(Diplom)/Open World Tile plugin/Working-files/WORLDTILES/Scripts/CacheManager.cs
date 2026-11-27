using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CacheManager
{

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (CacheStat.AutoCleanOnStartup)
        {
            Debug.LogWarning("[CacheManager] Автоматическая очистка кэша...");
            CleanUpCache();
        }
    }

    public static void CleanUpCache()
    {
        CleanFolder(CacheStat.TextureCacheFolder, CacheStat.MaxTextureCacheSizeMB, "текстур");
        CleanFolder(CacheStat.HeightCacheFolder, CacheStat.MaxHeightCacheSizeMB, "высот");
    }

    private static void CleanFolder(string folderPath, int maxSizeMB, string label)
    {
        if (!Directory.Exists(folderPath)) return;

        var files = new DirectoryInfo(folderPath)
            .GetFiles("*", SearchOption.AllDirectories)
            .OrderBy(f => f.LastAccessTimeUtc)
            .ToList();

        long maxSizeBytes = maxSizeMB * 1024L * 1024L;
        long totalSize = files.Sum(f => f.Length);

        if (totalSize <= maxSizeBytes) return;

        Debug.LogWarning($"[CacheManager] Кэш {label} превышает лимит ({totalSize / (1024 * 1024)} MB), начинаем очистку...");

        foreach (var file in files)
        {
            try
            {
                totalSize -= file.Length;
                file.Delete();

                if (totalSize <= maxSizeBytes)
                {
                    Debug.Log($"[CacheManager] Очистка {label} завершена. Размер: {totalSize / (1024 * 1024)} MB");
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CacheManager] Не удалось удалить файл: {file.FullName} — {e.Message}");
            }
        }
    }
}
