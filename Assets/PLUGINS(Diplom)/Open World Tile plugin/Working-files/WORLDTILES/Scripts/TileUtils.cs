using System;
using UnityEngine;

public static class TileUtils
{
    public static int LonToTileX(double lon, int zoom)
    {
        return (int)((lon + 180.0) / 360.0 * (1 << zoom));
    }

    public static int LatToTileY(double lat, int zoom)
    {
        Debug.Log("lat=" + lat);
        double latRad = lat * Mathf.Deg2Rad;
        Debug.Log("latRad=" + latRad);
        var result = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * (1 << zoom);
        Debug.Log("result=" + result);
        return (int)(result);
    }
    public static double TileXToLon(int x, int zoom)
    {
        return x / (double)(1 << zoom) * 360.0 - 180;
    }

    public static double TileYToLat(int y, int zoom)
    {
        double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << zoom);
        return Mathf.Rad2Deg * (float)Math.Atan(Math.Sinh(n));
    }



     

    // --- Новый функционал ---

    /// <summary>
    /// Преобразует долготу в дробную координату X в tile-системе
    /// </summary>
    public static double LonToWorldX(double lon, int zoom)
    {
        return (lon + 180.0) / 360.0 * (1 << zoom);
    }

    /// <summary>
    /// Преобразует широту в дробную координату Y в tile-системе
    /// </summary>
    public static double LatToWorldY(double lat, int zoom)
    {
        lat = Math.Clamp(lat, -85.05112878, 85.05112878);
        double latRad = lat * Mathf.Deg2Rad;
        double alfa = Math.Tan(Math.PI / 4 + latRad / 2);
        double mercN = Math.Log(alfa);
        var result = (1.0 - mercN / Math.PI) / 2.0 * (1 << zoom);
        return result;
    }

    /// <summary>
    /// Преобразует дробную tile-X координату обратно в долготу
    /// </summary>
    public static double WorldXToLon(double worldX, int zoom)
    {
        return worldX / (1 << zoom) * 360.0 - 180.0;
    }

    /// <summary>
    /// Преобразует дробную tile-Y координату обратно в широту
    /// </summary>
    public static double WorldYToLat(double worldY, int zoom)
    {
        double n = Math.PI - 2.0 * Math.PI * worldY / (1 << zoom);
        return Mathf.Rad2Deg * (float)Math.Atan(Math.Sinh(n));
    }
}



//public static class TileUtils
//{
//    public static int LonToTileX(double lon, int zoom)
//    {
//        return (int)((lon + 180.0) / 360.0 * (1 << zoom));
//    }

//public static int LatToTileY(double lat, int zoom)
//{
//    double latRad = lat * Mathf.Deg2Rad;
//    return (int)((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * (1 << zoom));
//}

//    public static double TileXToLon(int x, int zoom)
//    {
//        return x / (double)(1 << zoom) * 360.0 - 180;
//    }

//    public static double TileYToLat(int y, int zoom)
//    {
//        double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << zoom);
//        return Mathf.Rad2Deg * (float)Math.Atan(Math.Sinh(n));
//    }
//}
