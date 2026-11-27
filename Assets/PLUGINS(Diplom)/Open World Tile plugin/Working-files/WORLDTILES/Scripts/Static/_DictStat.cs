//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;


//[System.Serializable]
//public class TileObject
//{
//    public WorldPosition worldPosition;
//    public GameObject gameObject;
//    public string Log;
//    public bool isUsed;
//    public bool isMeshReady;
//    public bool isTextureReady;
//    public TileObject(WorldPosition worldPosition, GameObject gameObject = null, bool isUsed = false, bool isMeshReady = false, bool isTextureReady = false)
//    {
//        this.worldPosition = worldPosition;
//        if (gameObject != null)
//            this.gameObject = gameObject;
//        this.isUsed = isUsed;
//        this.isMeshReady = isMeshReady;
//        this.isTextureReady = isTextureReady;
//    }
//}

//public static class DictStat
//{
//    public static Dictionary<(int z, int x, int y), TileObject> dictTiles = new();// создаём словарь 


//    //TileStat.dictTiles[i].diction = new();       
//    // Инициализация массива словарей

//    public static void dictTilesAddLog(int zoom, int tileX, int tileY, string log)
//    {

//        if (dictTiles.TryGetValue((zoom, tileX, tileY), out var tile))
//        {
//            tile.Log += log;
//        }
//        else
//        {
//            Debug.Log($"❌[AddLog] В слокаре нет тайла Z{zoom} [{tileX}, {tileY}]");
//        }
//    }
//    public static bool dictTilesUsed(WorldPosition wp)
//    {
//        return dictTilesUsed(wp.zoom, wp.tileX, wp.tileY);
//    }

//    public static bool dictTilesUsed(int zoom, int tileX, int tileY)
//    {

//        if (dictTiles.TryGetValue((zoom, tileX, tileY), out var tile))
//        {
//            return tile.isUsed;
//        }
//        else
//        {
//            Debug.Log($"❌[dictTilesUsed] В слокаре нет тайла Z{zoom} [{tileX}, {tileY}]");
//        }
//        return false;
//    }
//    public static string dictTilesGetLog(int zoom, int tileX, int tileY)
//    {

//        if (dictTiles.TryGetValue((zoom, tileX, tileY), out var tile))
//        {
//            return tile.Log;
//        }
//        else
//        {
//            Debug.Log($"❌[GetLog] В слокаре нет тайла Z{zoom} [{tileX}, {tileY}]");
//            return "";
//        }
//    }
//    public static List<GameObject> dictTilesGetChildReady(int zoom, int tileX, int tileY)
//    {

//        if (dictTiles.TryGetValue((zoom, tileX, tileY), out var tileParent))
//        {
//            var listChild = new List<GameObject>();
//            //если родитель есть проверяем детей
//            int summ = 0;
//            for (int x = 0; x <= 1; x++)
//            {
//                for (int y = 0; y <= 1; y++)
//                {
//                    int __tileX = tileX * 2 + x;
//                    int __tileY = tileY * 2 + y;
//                    byte __zoom = (byte)(zoom + 1);
//                    if (dictTiles.TryGetValue((__zoom, __tileX, __tileY), out var tileChild))
//                    {
//                        if (tileChild.isMeshReady)
//                        {
//                            listChild.Add(tileChild.gameObject);
//                            summ++;
//                        }
//                    }
//                }
//            }
//            if (summ == 4)
//            {
//                Debug.Log($"[GetChildReady] у родителя Z{zoom} [{tileX}, {tileY}] найдены все 4 реберка!");

//            }
//            else
//            {
//                //Debug.Log($"[GetChildReady] Найдены {summ} дитёв Z{zoom} ({tileX}, {tileY})");

//            }
//            return listChild;
//        }
//        else
//        {
//            Debug.Log($"❌[GetChildReady] В слокаре нет тайла Z{zoom} [{tileX}, {tileY}]");
//        }
//        return null;
//    }

//    public static void ProcessTiles(int limit = 100)
//    {
//        // Берём все уникальные уровни z, сортируем от максимального к минимальному
//        var levels = dictTiles.Keys
//            .Select(k => k.z)
//            .Distinct()
//            .OrderByDescending(z => z);

//        int processedCount = 0;

//        foreach (var z in levels)
//        {
//            // Выбираем все тайлы с этим z
//            var tilesAtZ = dictTiles
//                .Where(kv => kv.Key.z == z)
//                .Select(kv => kv.Value);

//            foreach (var tile in tilesAtZ)
//            {
//                // здесь твоя логика обработки
//                Debug.Log($"Process tile at Z={z}");

//                processedCount++;
//                if (processedCount >= limit)
//                {
//                    Debug.Log("Limit reached, stopping");
//                    return;
//                }
//            }
//        }
//    }

//    public static void TryChange(TileObject obj)
//    {
//        if (obj != null)
//        {
//            if (dictTiles.TryGetValue((obj.worldPosition.zoom, obj.worldPosition.tileX, obj.worldPosition.tileY), out var tile))
//            {
//                tile = obj;
//            }
//            else
//            {
//                Debug.Log($"❌[TryChange] В словаре нет записи Z{obj.worldPosition.zoom} [{obj.worldPosition.tileX}, {obj.worldPosition.tileY}]");
//            }
//        }
//    }

//    public static bool SetUsed((int z, int x, int y) key, bool isUsed)
//    {
//        if (dictTiles.TryGetValue(key, out var tile))
//        {
//            dictTiles[key].isUsed = isUsed;
//            if (dictTiles[key].gameObject)
//                dictTiles[key].gameObject.transform.GetChild(0).gameObject.SetActive(isUsed);
//            return true;
//        }
//        return false;
//    }

//    public static void DestroyTile((int z, int x, int y) key)
//    {
//        if (!dictTiles.TryGetValue(key, out var tile))
//            return;
//        if (!tile.isMeshReady)
//            return;

//        if (tile.gameObject != null)
//        {
//            var mf = tile.gameObject.GetComponent<MeshFilter>();
//            if (mf && mf.mesh != null)
//            {
//                Object.Destroy(mf.mesh); // освобождаем память только у runtime mesh
//                mf.mesh = null;
//            }

//            var mr = tile.gameObject.GetComponent<MeshRenderer>();
//            if (mr && mr.material != null)
//            {
//                Object.Destroy(mr.material); // уничтожаем только уникальный материал
//                mr.material = null;
//            }

//            Object.Destroy(tile.gameObject); // уничтожаем сам объект
//            tile.gameObject = null;
//        }

//        //dictTiles.Remove(key); // если нужно реально удалить из словаря
//    }


//}
