
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TileStat
{
    // Базовый URL-ы без /zoom/x/y.png
    //public const string ElevationUrl = "https://opendem.info/"; только качать вручную архивами
    //public static string url = "https://a.tile-cyclosm.openstreetmap.fr/cyclosm"; //OpenStreetMap — CyclOSM (велокарта)

    //public static string url = "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/";
    //public const string ElevationUrl = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium";

    //КАМЕРАЦЕНТРИЧНЫЙ МИР
    public static bool isWORLDCENTER = false;

    public const byte LowTextureZoom = 8; // жестко 8! если менять надо генерить новый Low архив текстур
    public const byte LowElevationZoom = 9; // Zoom 9 для глобальных высот (4x4 опорные точки для тайла Z15)

    public const byte LowZoom = 8; 
    public const byte BaseZoom = 15; //19макс Tex / 15макс Для высот
    public static int BaseTileSize = 160;
    public static byte SuperZoomScale = 2; // от 0 до 3, но в сумме с BaseZoom не больше 19 

    //крутизна ландшафта (0 - плоский мир, 1 - нормальная крутизна )
    public static float heightFactor = 1.2f;

    // Блок устранения шума и сглаживания спутниковых высот (по умолчанию)
    public const bool EnableHeightDespike = true;
    public const float DespikeThreshold = 15f; // макс. допустимый перепад высоты с соседями в метрах
    public const bool EnableHeightSmoothing = true;
    public const float SmoothingSigma = 1.0f; // радиус / сила Гауссова сглаживания
    public const int SmoothingIterations = 1; // количество проходов сглаживания




    //все что ниже лучше НЕ трогать!
    public const int ElevationTileResolution = 256; // в пикселях (всегда для PNG)
    private const int equatorLength = 40075017; //длина экватора (м)
    ///максимальное количество тайлов по горизонатали
    public static int maxTileX => 1 << BaseZoom;
    /// <summary>
    /// Примерная длина одного тайла в метрах на экваторе при данном zoom.
    /// Основано на том, что вся Земля плоская и прямоугольная с длиной как экваторе
    /// </summary>
    public static float TileSizeInMeters => equatorLength / maxTileX;

    //Длина землю на экватаре делим на количество тайлов
    //  получаем сколько метров в тайле
    //      делим на размер тайла в юнити единицах 
    //          получаем сколько реальных метров в юнити единице

    //количесто метров в юните
    public static float ToMeter => TileSizeInMeters / BaseTileSize;

    //количество юнитов в метре
    public static float ToUnit => 1 / ToMeter;

    public static float ElevationScale => ToUnit * heightFactor; // масштаб для высоты в Unity-единицах


    //Компенсация позиции
    public static bool isCompensationPosition = true;
    public static Vector3 compensPos;
    //функция срабатывает вначале и задает смещение для всех тайлов
    public static void CompensatePosition(Vector3 offset)
    {
        // Компенсируем только X и Z, Y не трогаем —
        // иначе новые тайлы проваливаются на высоту первого тайла
        compensPos = new Vector3(offset.x, 0f, offset.z);
        isCompensationPosition = false;
    }

}
    
    



    