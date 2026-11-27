using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Представляет мировую позицию в координатах тайлов и нормализованного смещения внутри тайла.
/// </summary>
[System.Serializable]
public struct WorldPosition
{
    public byte zoom;
    public int tileX;
    public int tileY;
    public Vector2 localOffset; // нормализованное смещение (0..1)
    public float Height;      //   в метрах над уровнем моря
    private float realHeight => Height * TileStat.heightFactor;      // в метрах над уровнем моря



    public static float ElevationScale => TileStat.ElevationScale;
    public static float TileSizeMeters => TileStat.TileSizeInMeters; // сторона тайла в метрах

    private int maxTileX => 1 << zoom;
    private int scale => 1 <<(TileStat.BaseZoom - zoom);
    private int tileSize => TileStat.BaseTileSize * scale;

    public WorldPosition((byte z, int x, int y) key)
    {
        this.zoom = key.z;
        this.tileX = key.x;
        this.tileY = key.y;
        this.localOffset = new Vector2(0, 0);
        this.Height = 0;
    }
    public WorldPosition(byte zoom, int tileX, int tileY, Vector2 localOffset, float Height = 0)
    {
        this.zoom = zoom;
        this.tileX = tileX;
        this.tileY = tileY;
        this.localOffset = localOffset;
        this.Height = Height;
    }
    // Перегруженный конструктор с дефолтным localOffset
    public WorldPosition(byte zoom, int tileX, int tileY, float realHeight = 0)
        : this(zoom, tileX, tileY, new Vector2(0.0f, 0.0f), realHeight) {}

    public Vector2 WrappedTiles()
    {
         int wrappedX = ((tileX % maxTileX) + maxTileX) % maxTileX;
    return new Vector2(wrappedX, tileY);
    }


    /// <summary>
    /// Преобразует в Unity-позицию
    /// </summary>
    public Vector3 ToUnityPosition()
    {
        double x = (tileX + localOffset.x) * tileSize - TileStat.compensPos.x;
        double z = -(tileY+ 1 - localOffset.y) * tileSize - TileStat.compensPos.z; // Инвертируем ось Y
        double y = realHeight * TileStat.ToUnit - TileStat.compensPos.y; ;
        var _curentPos = new Vector3((float)x, (float)y, (float)z) ;
        return _curentPos;
    }

    /// <summary>
    /// Создаёт WorldPosition из Unity-позиции
    /// </summary>
     public WorldPosition FromUnityPosition(Vector3 pos, byte zoom)
    {
        pos = pos + TileStat.compensPos;

        int ts = TileStat.BaseTileSize * (1 << (TileStat.BaseZoom - zoom));

        int tx = Mathf.FloorToInt(pos.x / ts);
        int ty = Mathf.FloorToInt(-pos.z / ts);

        float offsetX = (pos.x - tx * ts) / ts;
        float offsetY = 1f - (-pos.z - ty * ts) / ts;

        float realH = pos.y / ElevationScale;

        return new WorldPosition
        {
            tileX = tx,
            tileY = ty,
            localOffset = new Vector2(offsetX, offsetY),
            Height = realH,
            zoom = zoom
        };
    }

    /// <summary>
    /// Смещает позицию на заданный вектор (в метрах), учитывая размер тайла
    /// </summary>
    public void MoveByMeters(Vector3 moveMeters)
    {
        float dxNorm = moveMeters.x / TileSizeMeters;
        float dyNorm = moveMeters.z / TileSizeMeters;
        float dheightNorm = moveMeters.y / ElevationScale;

        localOffset = new Vector2(localOffset.x+ dxNorm, localOffset.y + dyNorm);
        Height += dheightNorm;
        // Переполнение по X
        while (localOffset.x >= 1f) { tileX += 1; localOffset.x -= 1f; }
        while (localOffset.x < 0f) { tileX -= 1; localOffset.x += 1f; }

        // Переполнение по Y
        while (localOffset.y >= 1f) { tileY -= 1; localOffset.y -= 1f; }
        while (localOffset.y < 0f) { tileY += 1; localOffset.y += 1f; }
    }

    public void MoveByMeters(float dx, float dy)
    {
        //MoveByMeters(new Vector2(dx, dy));
    }

    public string ToSaveString()
    {
        return $"{tileX},{tileY},{localOffset.x},{localOffset.y},{Height},{zoom}";
    }

    public static WorldPosition FromSaveString(string str)
    {
        var parts = str.Split(',');
        if (parts.Length < 6)
        {
            Debug.LogWarning("[WorldPosition] Invalid save string.");
            return new WorldPosition();
        }

        return new WorldPosition
        {
            tileX = int.Parse(parts[0]),
            tileY = int.Parse(parts[1]),
            localOffset = new Vector2(float.Parse(parts[2]), float.Parse(parts[3])),
            Height = float.Parse(parts[4]),
            zoom = byte.Parse(parts[5])
        };
    }

    public override string ToString()
    {
        return $"Tile ({tileX}, {tileY}) offset01({localOffset.x:0.00}, {localOffset.y:0.00}) height {Height:0.0}m Z: {zoom}";
    }


    public void UpdateFromUnityPosition(Vector3 pos)
    {
        int ts = TileStat.BaseTileSize * (1 << (TileStat.BaseZoom - zoom));

        tileX = Mathf.FloorToInt(pos.x / ts);
        tileY = Mathf.FloorToInt(-pos.z / ts);

        float offsetX = (pos.x - tileX * ts) / ts;
        float offsetY = 1f - (-pos.z - tileY * ts) / ts;
        Height = pos.y / ElevationScale;

        localOffset = new Vector2(offsetX, offsetY);
    }

    public static WorldPosition FromLatLon(double lat, double lon, byte zoom, float height = 0)
    {
        int n = 1 << zoom;

        double x = (lon + 180.0) / 360.0 * n;

        float latRad = (float)(lat * Mathf.Deg2Rad);
        float tanLat = Mathf.Tan(latRad);
        float cosLat = Mathf.Cos(latRad);
        float y = (1f - Mathf.Log(tanLat + 1f / cosLat) / Mathf.PI) / 2f * n;

        int tileX = Mathf.FloorToInt((float)x);
        int tileY = Mathf.FloorToInt((float)y);

        float offsetX = (float)(x - tileX);
        float offsetY = (float)(y - tileY);

        return new WorldPosition
        {
            tileX = tileX,
            tileY = tileY,
            localOffset = new Vector2(offsetX, offsetY),
            Height = height,
            zoom = zoom
        };
    }

    public (double lat, double lon) ToLatLon()
    {
        int n = 1 << zoom;
        double x = tileX + localOffset.x;
        double y = tileY + localOffset.y;

        double lon = x / n * 360.0 - 180.0;
        double latRad = Mathf.Atan((float)Math.Sinh(Mathf.PI * (1 - 2 * y / n)));
        double lat = latRad * Mathf.Rad2Deg;

        return (lat, lon);
    }

    public string ToGoogleString(int decimals = 6)
    {
        var (lat, lon) = ToLatLon();
        return $"{lat.ToString($"F{decimals}")},{lon.ToString($"F{decimals}")}";
    }

    public void OpenInGoogleMaps(int zoomLevel = 17)
    {
        var (lat, lon) = ToLatLon();
        string url = $"https://www.google.com/maps/@{lat},{lon},{zoomLevel}z";
        Application.OpenURL(url);
    }

     
    public static string WorldPositionToCoordinate(int tileX, int tileY, int zoom)
    {
        float n = Mathf.Pow(2f, zoom);
        float lon = tileX / n * 360f - 180f;
        float latRad = Mathf.Atan((float)Math.Sinh(Mathf.PI * (1 - 2 * tileY / n)));
        float lat = latRad * Mathf.Rad2Deg;

        return lat.ToString("F6") + "," + lon.ToString("F6");
    }


    //для камеро центричной модели
    public Vector3 GetWorldOffset()
    {
        // Смещение мира в Unity-единицах (TileSize)
        float x = (tileX + localOffset.x) * tileSize;
        float z = -(tileY + localOffset.y) * tileSize;
        float y = Height * ElevationScale;

        return new Vector3(x, y, z);
    }

    //прересчитать для нового zoom
    public WorldPosition ConvertNewZoom(int newZoom)
    {
        if (newZoom == zoom)
            return this;

        // --- переводим в глобальные координаты (0..1)
        double globalX = (tileX + localOffset.x) / (double)(1 << zoom);
        double globalY = (tileY + localOffset.y) / (double)(1 << zoom);

        // --- переводим обратно в тайлы нового масштаба
        double scaledX = globalX * (1 << newZoom);
        double scaledY = globalY * (1 << newZoom);

        int newTileX = (int)Math.Floor(scaledX);
        int newTileY = (int)Math.Floor(scaledY);

        float newOffsetX = (float)(scaledX - newTileX);
        float newOffsetY = (float)(scaledY - newTileY);

        return new WorldPosition(
            (byte)newZoom,
            newTileX,
            newTileY,
            new Vector2(newOffsetX, newOffsetY),
            Height
        );
    }

    //public Vector3 ToUnityPositionAtBaseZoom(byte baseZoom)
    //{
    //    // масштаб между текущим zoom и базовым
    //    int scale = 1 << (baseZoom - zoom);

    //    // берём Unity-позицию в своём масштабе
    //    Vector3 pos = ToUnityPosition();

    //    // масштабируем её так, чтобы совпала с baseZoom
    //    return pos * scale;
    //}

    public List<WorldPosition> GetChildsList()
    {
        var list = new List<WorldPosition>();
        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                int _tileX = tileX * 2 + x;
                int _tileY = tileY * 2 + y;
                byte _zoom = (byte)(zoom + 1);

                var _wp = new WorldPosition(_zoom, _tileX, _tileY, 0);
                list.Add(_wp);
            }
        }
        return list;
    }

    public List<WorldPosition> GetChildsListRecursive(int depth)
    {
        var list = new List<WorldPosition>();
        if (depth <= 0) return list;

        int factor = 1 << depth; // 2^depth
        byte newZoom = (byte)(zoom + depth);

        for (int x = 0; x < factor; x++)
        {
            for (int y = 0; y < factor; y++)
            {
                int _tileX = tileX * factor + x;
                int _tileY = tileY * factor + y;

                var _wp = new WorldPosition(newZoom, _tileX, _tileY, 0);
                list.Add(_wp);
            }
        }

        return list;
    }


    public (int x, int y, int z) GetDictIndex()
    {
        var XYZ = (tileX, tileY, zoom);
        return XYZ;
    }

    /// <summary>
    /// возвращает координату центра тайла в Юнити единицах
    /// </summary>
    /// <param name="TileSize">Размер тайла в юнити</param>
    /// <returns></returns>
    public Vector2 UnityTileCenter()
    {
        var _size = tileSize;
        var _tilePos = ToUnityPosition();
        var res = new Vector2(_tilePos.x+ _size/2, _tilePos.z + _size / 2);
        return res;
    }


    public float TileSize()
    {
        return tileSize;
    }

    public ZXY ToZXY()
    {
        ZXY key;
        key.z = zoom;
        key.x = tileX;
        key.y = tileY;
        return key;
    }
    public (byte z, int x, int y) Key()
    {
        return (zoom, tileX, tileY);
    }
}

public struct ZXY
{
    public byte z;
    public int x;
    public int y;

    public ZXY(byte z, int x, int y)
    {
        this.z = z;
        this.x = x;
        this.y = y;
    }

    public ZXY toZXY(int z, int x, int y)
    {
        ZXY zxy = new((byte)z, x, y);
        return zxy;
    }

}
