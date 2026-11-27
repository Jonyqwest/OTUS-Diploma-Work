namespace WorldTile
{
    using System;
    using UnityEngine;

    public static class WorldEventHub
    {

        // Глобальное событие: любой может слушать
        public static event Action OnAllTileReady;
        public static event Action OnAllTileCancel;

        public static event Action<ZXY> OnTileReady;
        public static event Action OnTileDestroy;
      

        // Метод для вызова события (ребёнок будет его дергать)

        public static void SayTileReady(int z, int x, int y)
        {
            ZXY pos = new ZXY((byte)z, x, y);
            SayTileReady(pos);
        }

        public static void SayTileReady(ZXY pos)
        {
            OnTileReady?.Invoke(pos);
        }


        public static void SayAllTileReady()
        {
            OnAllTileReady?.Invoke();
        }

        public static void SayAllTileCancel()
        {
            OnAllTileCancel?.Invoke();
        }

        public static void SayTileDestroy()
        {
            OnTileDestroy?.Invoke();
        }
    }
}
