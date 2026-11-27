namespace WorldTile
{
    using System.Collections.Generic;
    using UnityEngine;

    class NearTileSistem
    {
        private byte quadrantX;
        private byte quadrantY;
        private static byte lastQuadrantX;
        private static byte lastQuadrantY;
        private static HashSet<ZXY> curentTiles, lastTiles;

        //привязки
        private SafeTileDictionary dict => DictStat.dictTiles;
        private WorldPosition curentWP => PlayerWorldStat.GetCurrentWorldPosition();

        public NearTileSistem()
        {
            curentTiles = new();
            lastTiles = new();
        }

        /// <summary>
        ///сравниваем предыдущую позицию если квадрант поменялся возвращаем true
        /// </summary>
        /// <param name="wp">указывается мировая позиция в базовом зуме</param>
        /// <returns></returns>
        public bool NeedNearUpdate()
        {
            if (curentWP.localOffset.x - .5f > 0)
                quadrantX = 2;
            else
                quadrantX = 1;

            if (curentWP.localOffset.y - .5f > 0)
                quadrantY = 1;
            else
                quadrantY = 2;

            if (lastQuadrantX == quadrantX & lastQuadrantY == quadrantY)
                return false;

            lastQuadrantX = quadrantX;
            lastQuadrantY = quadrantY;
            return true;
        }


        /// <summary>
        ///смотрим каким тайлам нужны супертекстуры (центр + 3 крайних с учетом оффсета)
        /// </summary>
        /// <param name="wp">указывается мировая позиция в базовом зуме</param>
        /// <returns>получаем индексы базовых тайлов</returns>
        public HashSet<ZXY> GetQuadrantTiles()
        {
            if (curentWP.localOffset.x - .5f > 0)
                quadrantX = 0;
            else
                quadrantX = 1;

            if (curentWP.localOffset.y - .5f > 0)
                quadrantY = 0;
            else
                quadrantY = 1;

            HashSet<ZXY> list = new();
            for (int i = 0 - quadrantX; i <= 1 - quadrantX; i++)
            {
                for (int j = 0 - quadrantY; j <= 1 - quadrantY; j++)
                {
                    list.Add(new ZXY(curentWP.zoom, curentWP.tileX + i, curentWP.tileY - j));
                }
            }
            return list;
        }


        public void InitSupetTextures(HashSet<ZXY> tiles)
        {
            if(TileStat.SuperZoomScale > 0)
                foreach (ZXY key in tiles)
                {
                    if (dict.TryGetValue((key.z, key.x, key.y), out var tile) && tile != null && tile.terrainObject != null)
                    {
                        var terrainTile = tile.terrainObject;
                        if (terrainTile)
                        {
                            if (terrainTile.State.CurrentType == TileStateType.Ready)
                            { 
                                terrainTile.State.ChangeState(terrainTile, new TileState_CreatingSuperTile());
                                Debug.Log($"Z{key.z}[{key.x}/{key.y}] - супертекстура запущена!");
                            }
                        }
                    }
                }
        }



        public void RemoveSupetTextures(HashSet<ZXY> tiles)
        {
            if (TileStat.SuperZoomScale > 0)
                foreach (var key in tiles)
                {
                    if (dict.TryGetValue((key.z, key.x, key.y), out var tile) && tile != null && tile.terrainObject != null)
                    {
                        var terrainTile = tile.terrainObject;
                        if (terrainTile)
                        {
                            if (terrainTile.State.CurrentType == TileStateType.SuperTile ||
                                terrainTile.State.CurrentType == TileStateType.CreatingSuperTile)
                            { 
                                terrainTile.CancelSuperTexture(2);
                                terrainTile.State.ChangeState(terrainTile, new TileState_Ready());
                                Debug.Log($"[SuperTextureSistem]: Z{key.z}[{key.x}/{key.y}] - супертекстура отменена c задержкой 2сек");
                            }
                        }
                    }
                }
        }


        public void InitColliders(HashSet<ZXY> tiles)
        {
            foreach (var key in tiles)
            {
                if (dict.TryGetValue((key.z, key.x, key.y), out var tile) && tile != null && tile.terrainObject != null)
                {
                    var terrainTile = tile.terrainObject;
                    if (terrainTile)
                    {
                        if (terrainTile.State.CurrentType == TileStateType.Ready ||
                            terrainTile.State.CurrentType == TileStateType.CreatingSuperTile ||
                            terrainTile.State.CurrentType == TileStateType.SuperTile
                            )
                        {
                            terrainTile.AddColliderMatch();
                            Debug.Log($"[SuperTextureSistem]: Z{key.z}[{key.x}/{key.y}] - Добавляем колайдер!");
                        }
                    }
                }
            }
        }
        


        public HashSet<ZXY> GetTilsForRemove()
        {
            HashSet<ZXY> removeTiles = new();
            //if (lastTiles != null)
            {
                foreach (var key in lastTiles)
                {
                    if (!curentTiles.Contains(key))
                    {
                        removeTiles.Add(key);
                    }
                }
            }
            return removeTiles;
        }




        public void Updater()
        {
            if (NeedNearUpdate())
            {
                curentTiles = GetQuadrantTiles();
                //работа с супертекстурами

                InitSupetTextures(curentTiles);
                RemoveSupetTextures(GetTilsForRemove());

                //работат с колайдерами
                InitColliders(curentTiles);

                lastTiles = curentTiles;
            }
        }

    }
}