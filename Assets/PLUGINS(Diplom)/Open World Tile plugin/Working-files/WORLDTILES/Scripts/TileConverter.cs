using UnityEngine;

public static class TileConverter
{
    private const int TileSize = 256; // в пикселях
    private const double EarthRadius = 6378137.0;
    private const double InitialResolution = 2.0 * Mathf.PI * EarthRadius / TileSize;
    private const double OriginShift = 2.0 * Mathf.PI * EarthRadius / 2.0;

    // Широта и долгота → тайл (x, y)
    public static Vector2Int LatLonToTile(Vector2d latLon, int zoom)
    {
        int x = (int)((latLon.y + 180.0) / 360.0 * (1 << zoom));
        float latRad = (float)(latLon.x * Mathf.Deg2Rad);
        float tan = Mathf.Tan(latRad);
        float sec = 1f / Mathf.Cos(latRad);
        int y = (int)((1.0 - Mathf.Log(tan + sec) / Mathf.PI) / 2.0 * (1 << zoom));
        return new Vector2Int(x, y);
    }

    // Тайл → центр координат в Unity
    public static Vector3 TileToUnityPosition(int x, int y, int zoom)
    {
        double res = Resolution(zoom);
        double minX = x * TileSize * res - OriginShift;
        double minY = y * TileSize * res - OriginShift;

        return new Vector3((float)minX, 0, (float)-minY); // Z инвертирован, чтобы север был вперёд
    }

    // Позиция в Unity → географические координаты
    public static Vector2d UnityToLatLon(Vector3 position, int zoom)
    {
        double res = Resolution(zoom);
        double mx = position.x;
        double my = -position.z;

        double lon = (mx / OriginShift) * 180.0;
        double lat = (my / OriginShift) * 180.0;

        lat = 180.0 / Mathf.PI * (2.0f * Mathf.Atan(Mathf.Exp((float)(lat * Mathf.Deg2Rad))) - Mathf.PI / 2.0f);
        return new Vector2d(lat, lon);
    }

    private static double Resolution(int zoom)
    {
        return InitialResolution / (1 << zoom);
    }
}

// Вспомогательная структура для двойной точности
[System.Serializable]
public struct Vector2d
{
    public double x;
    public double y;

    public Vector2d(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public static implicit operator Vector2(Vector2d d) => new Vector2((float)d.x, (float)d.y);
    public static implicit operator Vector2d(Vector2 v) => new Vector2d(v.x, v.y);
}
