 
using System.Collections.Generic;
using UnityEngine;

public static class Mas 
{
    public static List<Vector3> spiralOffeset1 = GenerateSpiralOffsets(1);
    public static List<Vector3> spiralOffeset2 = GenerateSpiralOffsets(2);
    public static List<Vector3> spiralOffeset3 = GenerateSpiralOffsets(3);

    /// <summary>
    /// </summary>
    /// <param name="maxLayer">радиус спирали</param>
    /// <returns>Возвращает одномерный масив точек закрученных в квадратную спираль. 0 центральная точка</returns>
    /// 
    public static List<Vector3> GenerateSpiralOffsets(int maxLayer)
    {
        var result = new List<Vector3>();

        // Центр — смещение (0,0), слой 0
        result.Add(new Vector3(0, 0, 0));

        for (int layer = 1; layer <= maxLayer; layer++)
        {
            int x = -layer;
            int y = layer; // старт сверху слоя

            // Двигаемся по часовой стрелке вокруг квадрата слоя:

            // 1) верх
            for (int i = -layer; i < layer; i++)
            {
                x = i;
                y = layer;
                result.Add(new Vector3(x, y, layer));
            }
            // 2) право
            for (int i = -layer; i < layer; i++)
            {
                x = layer;
                y = -i;
                result.Add(new Vector3(x, y, layer));
            }
            // 3) низ
            for (int i = -layer; i < layer; i++)
            {
                x = -i;
                y = -layer;
                result.Add(new Vector3(x, y, layer));
            }
            // 3) лево
            for (int i = -layer; i < layer; i++)
            {
                x = -layer;
                y = i;
                result.Add(new Vector3(x, y, layer));
            }
        }
        return result;
    }



    

    /// <summary>
    /// Получаем ключи всех тайлов для выбраного базового тайла
    /// </summary>
    /// <param name="range">Глубина охвата зума</param>
    /// <param name="baseZoom">Базовый зум</param>
    /// <returns></returns>
    public static HashSet<(int z, int x, int y)> GetGlobalTiles(int tileX, int tileY, int range = 99, int baseZoom = TileStat.BaseZoom, bool onlyVisible = true)
    {
        var worldPosition = new WorldPosition((byte)baseZoom, tileX, tileY);
        return GetGlobalTiles(worldPosition, range, baseZoom);
    }
    public static HashSet<(int z, int x, int y)> GetGlobalTiles(WorldPosition worldPosition, int range = 99, int baseZoom = TileStat.BaseZoom, bool onlyVisible = true)
    {
        HashSet<(int z, int x, int y)> res = new();
        var lowZoom = baseZoom - range + 1;
        if (lowZoom < TileStat.LowZoom) lowZoom = TileStat.LowZoom;
        if (lowZoom > baseZoom) lowZoom = baseZoom;

        //1 шаг - база (25 тайлов)

        //берем мировую позицию
        var _playerWP = worldPosition;

        //вычисляем номер тайла на Low zoom
        var _wpGlobal = _playerWP.ConvertNewZoom(lowZoom);

        //делаем для каждого тайла в глобальном радиусе
      
        for (int i = -2; i <= 2; i++)
            for (int j = -2; j <= 2; j++)
            {
                var _tileX = (int)(_wpGlobal.tileX + i);
                var _tileY = (int)(_wpGlobal.tileY + j);
                var _zoom = _wpGlobal.zoom;

                //добавляем в словарь
                res.Add((_zoom, _tileX, _tileY));
            }
        //База готова

        //2 шаг - итерации по детализации (36 тайлов каждый цикл)
        for (int z = lowZoom; z < baseZoom; z++) {
            //центр
            var wp = _playerWP.ConvertNewZoom(z);
            
            //делаем для каждого тайла в радиусе 1
            for (int i = -1; i <= 1; i++)
            { 
                for (int j = -1; j <= 1; j++)
                {
                    int _tileX = wp.tileX + i;
                    int _tileY = wp.tileY + j;
                    int _zoom = wp.zoom;
                    if(onlyVisible)
                        res.Remove((z, _tileX, _tileY));

                    //вручную грубо разбиваем на 4
                    for (int x = 0; x <= 1; x++)
                    {
                        for (int y = 0; y <= 1; y++)
                        {
                            int __tileX = _tileX * 2 + x;
                            int __tileY = _tileY * 2 + y;
                            byte __zoom = (byte)(_zoom + 1);

                            var _wp = new WorldPosition(__zoom, __tileX, __tileY, 0);

                            //добавляем в словарь
                            res.Add((__zoom, __tileX, __tileY));
                        }

                    }
                }
            }
        }
        return res;
    }


    /// <summary>
    /// Возвращает тайлы, которые есть в currentTiles, но отсутствуют в allTiles.
    /// (Новые тайлы, которые нужно добавить.)
    /// </summary>
    public static HashSet<(int z, int x, int y)> GetExceptionTiles(
        HashSet<(int z, int x, int y)> allTiles,
        HashSet<(int z, int x, int y)> currentTiles)
    {
        var result = new HashSet<(int z, int x, int y)>(currentTiles);
        result.ExceptWith(allTiles); // вычитаем всё, что уже было
        return result; // остаются только уникальные (новые)
    }

    ///// <summary>
    ///// Возвращает тайлы, которые есть в allTiles, но отсутствуют в currentTiles.
    ///// (Лишние тайлы, которые нужно удалить.)
    ///// </summary>
    //public static HashSet<(int z, int x, int y)> GetObsoleteTiles(
    //    HashSet<(int z, int x, int y)> allTiles,
    //    HashSet<(int z, int x, int y)> currentTiles)
    //{
    //    var result = new HashSet<(int z, int x, int y)>(allTiles);
    //    result.ExceptWith(currentTiles); // убираем всё, что всё ещё актуально
    //    return result; // остаются те, что нужно удалить
    //}



    public static List<(int z, int x, int y)> HashSetToList(HashSet<(int z, int x, int y)> source)
    {
        return new List<(int z, int x, int y)>(source);
    }

}
