using System;
using UnityEngine;

public static  class PlayerWorldStat{
    private static byte zoom => TileStat.BaseZoom;

    //запоминаем позицию игрока в мире
    private static WorldPosition currentPosition;

    //запоминаем мировую позицию
    public static void SetCurrentWorldPosition(WorldPosition wp)
    {
        if (wp.zoom != zoom)
            currentPosition = wp.ConvertNewZoom(zoom);
        currentPosition = wp;
    }
    //возвращает позицию игрока в BaseZoom
    public static WorldPosition GetCurrentWorldPosition()
    {
        //if (currentPosition.zoom != zoom)
        //    return currentPosition.ConvertNewZoom(zoom);
        return currentPosition;
    }

}
