//// TileLODProvider.cs
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using UnityEngine;

///// <summary>
///// Провайдер heightmap'ов с LOD/апскейлом/кешем.
///// </summary>
//public class TileLODProvider
//{
//    public static TileLODProvider Instance { get; private set; }

//    // Параметры
//    private readonly int maxConcurrentTasks = 6;           // ограничение фоновой работы
//    private readonly SemaphoreSlim workerSemaphore;
//    private readonly ConcurrentDictionary<string, Task<float[,]>> inProgress = new();

//    public int targetZoom = TileStat.zoom;                 // желаемый максимальный zoom
//    public int maxSearchZoomBack = 8;                      // сколько уровней вниз пытаться найти данные

//    public TileLODProvider(int maxParallel = 6)
//    {
//        workerSemaphore = new SemaphoreSlim(maxParallel);
//        Instance = this;
//    }

//    // Формирует ключ для кэша/словарей
//    private static string Key(int zoom, int x, int y) => $"{zoom}_{x}_{y}";

//    /// <summary>
//    /// Основной метод: получает (или генерирует) heightmap для заданного zoom/x/y.
//    /// Сначала проверяет бинарный кэш (ElevationCacheUtility), затем PNG (если нужно), затем апскейл.
//    /// </summary>
//    public Task<float[,]> GetHeightMapAsync(int zoom, int x, int y, CancellationToken ct = default)
//    {
//        string k = Key(zoom, x, y);
//        // Если уже в процессе — возвращаем тот же таск
//        if (inProgress.TryGetValue(k, out var existing))
//            return existing;

//        var task = InternalGetHeightMapAsync(zoom, x, y, ct);
//        inProgress.TryAdd(k, task);

//        // когда завершится — вычистим запись
//        _ = task.ContinueWith(t => { inProgress.TryRemove(k, out _); }, TaskScheduler.Default);
//        return task;
//    }

//    private async Task<float[,]> InternalGetHeightMapAsync(int zoom, int x, int y, CancellationToken ct)
//    {
//        await workerSemaphore.WaitAsync(ct).ConfigureAwait(false);
//        try
//        {
//            // 1) Попробовать бинарный/byte cache (fast)
//            var fromBinary = CacheTileUtility.LoadElevationDataCompressed(x, y, zoom);
//            if (fromBinary != null)
//                return fromBinary;

//            // 2) Попробовать PNG-кеш/скачать (террариум)
//            try
//            {
//                //ElevationTileLoaderTerrarium уже умеет кешировать PNG и возвращать heightmap
//                // но если у тебя есть отдельный метод для чтения PNG-кэша, можно вызвать его.
//                var hm = await ElevationTileLoaderTerrarium.LoadElevationTile(zoom, x, y);
//                if (hm != null)
//                {
//                    // Сохраним в бинарный кэш для ускорения в будущем
//                    CacheTileUtility.SaveElevationDataCompressed(hm, x, y, zoom);
//                    return hm;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.LogWarning($"[TileLOD] failed load native {zoom}/{x}/{y}: {ex.Message}");
//            }

//            // 3) Попробовать апскейл с ближайшего доступного (zoomDown <= zoom)
//            for (int dz = 1; dz <= maxSearchZoomBack; dz++)
//            {
//                int lowerZoom = zoom - dz;
//                if (lowerZoom < 0) break;

//                // Для тайла (zoom,x,y) нужно выбрать набор тайлов в lowerZoom, покрывающих ту же площадь.
//                // factor = 2^dz
//                int factor = 1 << dz;
//                int lx = x / factor;
//                int ly = y / factor;

//                // Попробуем загрузить lowerZoom tile (или несколько, если upscale требует соседей)
//                float[,] baseHM = CacheTileUtility.LoadElevationDataCompressed(lx, ly, lowerZoom);
//                if (baseHM == null)
//                {
//                    // Попробуем вызвать загрузчик (png -> heightmap) для lowerZoom
//                    try
//                    {
//                        baseHM = await ElevationTileLoaderTerrarium.LoadElevationTile(lowerZoom, lx, ly);
//                        if (baseHM != null)
//                            CacheTileUtility.SaveElevationDataCompressed(baseHM, lx, ly, lowerZoom);
//                    }
//                    catch { baseHM = null; }
//                }

//                if (baseHM != null)
//                {
//                    // У нас есть скачанный lower-resolution heightmap — сделаем ресемпл к целевому разрешению
//                    int targetSize = TileStat.ElevationTileResolution; // например 100
//                    int srcSize = baseHM.GetLength(0);

//                    // Важно: когда factor>1, один lower-tile покрывает factor×factor high tiles.
//                    // Нужно сместить координаты внутри lower tile, чтобы получить именно тот участок.
//                    int localTileX = x % factor;
//                    int localTileY = y % factor;

//                    // Upsample: сначала создаём tiled matrix, где базовый lower tile масштабируется.
//                    // Чтобы повысить качество, можно объединять соседние lower tiles — но для упрощения возьмём только этот.
//                    var upsamled = HeightmapResampler.UpsampleFromLower(baseHM, srcSize, targetSize, localTileX, localTileY, factor);

//                    // Сохраним в бинарный кэш
//                    CacheTileUtility.SaveElevationDataCompressed(upsamled, x, y, zoom);
//                    return upsamled;
//                }
//            }

//            // 4) если ничего не найдено — возвращаем плоский (flat)
//            var flat = GenerateFlat(targetSize: TileStat.ElevationTileResolution);
//            //CacheElevationUtility.SaveElevationDataCompressed(flat, x, y, zoom);
//            return flat;
//        }
//        finally
//        {
//            workerSemaphore.Release();
//        }
//    }

//    private static float[,] GenerateFlat(int targetSize)
//    {
//        var flat = new float[targetSize, targetSize];
//        return flat;
//    }
//}

///// <summary>
///// Небольшая утилита для ресемплинга heightmap.
///// Предполагаем, что исходный lower-res heightmap представляет целый lower-тайл.
///// </summary>
//public static class HeightmapResampler
//{
//    // UpsampleFromLower:
//    // baseHM: массив size srcSize x srcSize (обычно TileStat.ElevationTileResolution lowerZoom)
//    // targetSize: размер целевого (TileStat.ElevationTileResolution)
//    // localTileX/localTileY: индекс high-tile внутри lower tile (0..factor-1)
//    // factor: сколько high tiles помещается в одном lower tile (2^dz)
//    public static float[,] UpsampleFromLower(float[,] baseHM, int srcSize, int targetSize, int localTileX, int localTileY, int factor)
//    {
//        // Мы считаем, что один lower tile покрывает factor×factor high tiles.
//        // Подразумеваем, что высоты в baseHM покрывают область шириной 1.0 (в нормированных координатах).
//        // Нужно взять подблок, соответствующий localTileX/localTileY:
//        // Каждый high-tile будет занимать 1/factor долю по ширине.

//        float[,] outHM = new float[targetSize, targetSize];

//        for (int j = 0; j < targetSize; j++)
//        {
//            for (int i = 0; i < targetSize; i++)
//            {
//                // нормализованные координаты внутри high-tile: u,v ∈ [0,1)
//                float u = (i + 0.5f) / targetSize;
//                float v = (j + 0.5f) / targetSize;

//                // нормализованные координаты внутри lower-tile:
//                float globalU = (localTileX + u) / factor;
//                float globalV = (localTileY + v) / factor;

//                // преобразуем в индекс baseHM
//                float srcX = globalU * (srcSize - 1);
//                float srcY = globalV * (srcSize - 1);

//                outHM[i, j] = BilinearSample(baseHM, srcX, srcY);
//            }
//        }

//        return outHM;
//    }

//    private static float BilinearSample(float[,] arr, float x, float y)
//    {
//        int sx = (int)Mathf.Floor(x);
//        int sy = (int)Mathf.Floor(y);
//        float fx = x - sx;
//        float fy = y - sy;
//        int max = arr.GetLength(0) - 1;

//        int x0 = Mathf.Clamp(sx, 0, max);
//        int x1 = Mathf.Clamp(sx + 1, 0, max);
//        int y0 = Mathf.Clamp(sy, 0, max);
//        int y1 = Mathf.Clamp(sy + 1, 0, max);

//        float v00 = arr[x0, y0];
//        float v10 = arr[x1, y0];
//        float v01 = arr[x0, y1];
//        float v11 = arr[x1, y1];

//        float a = Mathf.Lerp(v00, v10, fx);
//        float b = Mathf.Lerp(v01, v11, fx);
//        return Mathf.Lerp(a, b, fy);
//    }
//}
