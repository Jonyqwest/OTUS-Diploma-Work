using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class OWT_PlayerPositionStatic
{
    private static WorldPosition PlayerWorldPosition;
    private static Vector3 UnityPosition;
    private static Vector3 LastUnityPosition;

    private static bool isStarting = true;

    public static void InitPlayerPosition()
    {
        isStarting = true;
        PlayerWorldPosition = PlayerWorldStat.GetCurrentWorldPosition();
    }

    public static void ChangePlayerWorldPosition(Vector3 unityPosition)
    {
        LastUnityPosition = UnityPosition;
        UnityPosition = unityPosition;

        if (isStarting)
        {   //пропускаем первый вызов
            isStarting = false;
            return;
        }

        var delta = UnityPosition - LastUnityPosition;
        if (delta == Vector3.zero) return;

        var wp = PlayerWorldStat.GetCurrentWorldPosition();
        wp.MoveByMeters(delta * TileStat.ToMeter);
        PlayerWorldStat.SetCurrentWorldPosition(wp);


    }




}
