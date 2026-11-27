using System.Collections.Generic;

namespace WorldTile
{
    class ColliderSystem
    {
        private byte quadrantX;
        private byte quadrantY;
        private static byte lastQuadrantX;
        private static byte lastQuadrantY;
        private static HashSet<ZXY> curentTiles, lastTiles;

        //привязки
        private SafeTileDictionary dict => DictStat.dictTiles;
        private WorldPosition curentWP => PlayerWorldStat.GetCurrentWorldPosition();

        public ColliderSystem()
        {
            curentTiles = new();
            lastTiles = new();
        }

        //проверяем кому нужен колайдер
        //1. перебираем все визуальные тайлы на данный момент (если тайлы еще не говы и перерисовка не наступает) кидаем коладер на соседа
        //2. процентная подгрузка  колайдеров ближайших создаваемых тайлов
        //3. при работе в офлайн работаем с low

    }
}