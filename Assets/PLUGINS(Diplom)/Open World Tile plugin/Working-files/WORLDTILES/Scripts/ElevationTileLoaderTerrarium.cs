//using System.IO;
//using System.Net.Http;
//using System.Threading.Tasks;
//using UnityEngine;

//public static class ElevationTileLoaderTerrarium
//{
    
     

//    public static async Task<float[,]> LoadElevationTile(int zoom, int x, int y, Texture2D texture = null)
//    {
//        if (texture == null)
//        {
//            // 1. Попробовать загрузить бинарный кэш
//            float[,] cached = CacheTileUtility.LoadElevationDataCompressed(x, y, zoom);
//            if (IsValidHeightmap(cached))
//                return cached;

//            // 2. Попробовать загрузить PNG (или из кэша)
//            string cachePath = CacheStat.GetHeightTilePath(zoom, x, y);
            

//            if (File.Exists(cachePath))
//            {
//                byte[] pngData = File.ReadAllBytes(cachePath);
//                texture = new Texture2D(
//                    TileStat.ElevationTileResolution,
//                    TileStat.ElevationTileResolution,
//                    TextureFormat.RGB24,
//                    false
//                );
//                texture.LoadImage(pngData);
//            }
//            else
//            {
//                string url = $"{TileStat.ElevationUrl}/{zoom}/{x}/{y}.png";
//                using HttpClient client = new HttpClient();
//                try
//                {
//                    byte[] data = await client.GetByteArrayAsync(url);
//                    texture = new Texture2D(
//                        TileStat.ElevationTileResolution,
//                        TileStat.ElevationTileResolution,
//                        TextureFormat.RGB24,
//                        false
//                    );
//                    texture.LoadImage(data);

//                    //сохраняем как PNG
//                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
//                    File.WriteAllBytes(cachePath, data);
//                }
//                catch
//                {
//                    Debug.LogWarning($"[ElevationTileLoader] Failed to load tile {zoom}/{x}/{y} — using flat.");
//                    //return GenerateFlatHeightmap();
//                }
//            }
//        }
//        float[,] heightMap = DecodeTerrarium(texture);
//        // 3. Сохраняем как бинарный кэш
//        if (IsValidHeightmap(heightMap))
//            CacheTileUtility.SaveElevationDataCompressed(heightMap, x, y, zoom);

//        return heightMap;
//    }


//    private static bool IsValidHeightmap(float[,] map)
//    {
//        if (map == null) return false;
//        int size = TileStat.ElevationTileResolution;
//        if (map.GetLength(0) != size || map.GetLength(1) != size) return false;

//        // Быстрая проверка на NaN
//        for (int y = 0; y < size; y++)
//            for (int x = 0; x < size; x++)
//                if (float.IsNaN(map[x, y]) || float.IsInfinity(map[x, y]))
//                    return false;

//        return true;
//    }


//    public static float[,] DecodeTerrarium(Texture2D texture)
//    {
//        int size = TileStat.ElevationTileResolution;
//        float[,] heightData = new float[size, size];

//        for (int y = 0; y < size; y++)
//        {
//            for (int x = 0; x < size; x++)
//            {
//                Color pixel = texture.GetPixel(x, y);
//                int R = Mathf.FloorToInt(pixel.r * 255f);
//                int G = Mathf.FloorToInt(pixel.g * 255f);
//                int B = Mathf.FloorToInt(pixel.b * 255f);

//                float elevation = (R * 256 + G + B / 256f) - 32768f;
//                heightData[x, y] = elevation;
//            }
//        }

//        return heightData;
//    }

//    //private static float[,] GenerateFlatHeightmap()
//    //{
//    //    int size = TileStat.ElevationTileResolution;
//    //    float[,] flat = new float[size, size];
//    //    for (int y = 0; y < size; y++)
//    //        for (int x = 0; x < size; x++)
//    //            flat[x, y] = 0f;
//    //    return flat;
//    //}

    

//}
