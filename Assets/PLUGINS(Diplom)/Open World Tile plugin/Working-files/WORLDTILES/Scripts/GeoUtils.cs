using UnityEngine;

public static class GeoUtils
{
    /// <summary>
    /// Преобразует широту и долготу в Unity-позицию (X,Z), высота задаётся отдельно.
    /// </summary>
    public static Vector3 GeoToUnity(double lat, double lon)
    {
        double scale = TileStat.BaseTileSize * (1 << TileStat.BaseZoom) / 360.0;

        float x = (float)(lon * scale);
        float z = (float)(lat * scale);

        return new Vector3(x, 0f, z); // высота отдельно через WorldPosition
    }

    /// <summary>
    /// Преобразует позицию в Unity обратно в широту и долготу.
    /// </summary>
    public static Vector2d UnityToGeo(Vector3 pos)
    {
        double scale = 360.0 / (TileStat.BaseTileSize * (1 << TileStat.BaseZoom));

        double lon = pos.x * scale;
        double lat = pos.z * scale;

        return new Vector2d(lat, lon);
    }
}
