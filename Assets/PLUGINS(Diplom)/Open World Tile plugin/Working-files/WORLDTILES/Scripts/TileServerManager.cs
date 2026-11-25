using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class TileServerManager : MonoBehaviour
{
    [Header("Настройки сервера")]
    public string serverUrl = "https://your-tile-server.com"; // без конечного "/"
    public string imageryFolder = "tiles/imagery";
    public string elevationFolder = "tiles/elevation";
    public bool yOriginTop = true; // true — Y идёт сверху вниз (как в OSM)

    [Header("Кеширование доступных zoom'ов")]
    public Dictionary<Vector2Int, int> maxZoomCache = new(); // координаты -> max zoom

    private HttpClient httpClient = new();

    public string GetTileUrl(int x, int y, int z, bool isImagery)
    {
        string folder = isImagery ? imageryFolder : elevationFolder;
        if (!yOriginTop)
        {
            int maxTiles = 1 << z;
            y = maxTiles - 1 - y; // инверсия Y
        }
        return $"{serverUrl}/{folder}/{z}/{x}/{y}.png";
    }

    public async Task<bool> CheckTileExists(int x, int y, int z, bool isImagery)
    {
        string url = GetTileUrl(x, y, z, isImagery);
        try
        {
            using HttpRequestMessage req = new(HttpMethod.Head, url);
            using HttpResponseMessage res = await httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TileServerManager] Ошибка при проверке тайла: {e.Message}");
            return false;
        }
    }

    public async Task<int> FindMaxAvailableZoom(int x, int y, int maxZoom = 22, int minZoom = 0, bool isImagery = true)
    {
        for (int z = maxZoom; z >= minZoom; z--)
        {
            if (await CheckTileExists(x, y, z, isImagery))
                return z;
        }
        return -1;
    }

    public async Task PrecacheMaxZoomForRegion(Vector2Int centerTile, int radius, bool isImagery = true)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int coord = new(centerTile.x + dx, centerTile.y + dy);
                int maxZoom = await FindMaxAvailableZoom(coord.x, coord.y, isImagery: isImagery);
                maxZoomCache[coord] = maxZoom;
            }
    }

    public int? GetCachedMaxZoom(Vector2Int tileCoord)
    {
        if (maxZoomCache.TryGetValue(tileCoord, out int maxZoom))
            return maxZoom;
        return null;
    }
}
