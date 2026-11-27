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
//    public static SafeTileDictionary dictTiles = new SafeTileDictionary();
//}

///// <summary>
///// Безопасный словарь для хранения тайлов с встроенными функциями.
///// Теперь реализует IEnumerable, имеет индексатор и дополнительные удобные методы.
///// </summary>
//public class SafeTileDictionary : IEnumerable<KeyValuePair<(int z, int x, int y), TileObject>>
//{
//    private readonly Dictionary<(int z, int x, int y), TileObject> _tiles = new();

//    // Прямой доступ к исходному словарю (если нужно)
//    public Dictionary<(int z, int x, int y), TileObject> Raw => _tiles;

//    // Количество записей — и как метод, и как свойство (для совместимости)
//    public int Count => _tiles.Count;
//    public int CountMethod() => _tiles.Count;

//    // Индексатор (безопасный геттер)
//    public TileObject this[(int z, int x, int y) key]
//    {
//        get => _tiles.TryGetValue(key, out var v) ? v : null;
//        set => _tiles[key] = value;
//    }

//    // Keys / Values
//    public IEnumerable<(int z, int x, int y)> Keys => _tiles.Keys;
//    public IEnumerable<TileObject> Values => _tiles.Values;

//    // Add/Update
//    public void AddOrReplace(TileObject tile)
//    {
//        var key = (tile.worldPosition.zoom, tile.worldPosition.tileX, tile.worldPosition.tileY);
//        _tiles[key] = tile;
//    }

//    // TryGetValue / ContainsKey / Remove / Clear
//    public bool TryGetValue((int z, int x, int y) key, out TileObject tile) => _tiles.TryGetValue(key, out tile);
//    public bool ContainsKey((int z, int x, int y) key) => _tiles.ContainsKey(key);
//    public bool Remove((int z, int x, int y) key) => _tiles.Remove(key);
//    public void Clear() => _tiles.Clear();

//    // Добавить лог
//    public void AddLog(int zoom, int tileX, int tileY, string log)
//    {
//        if (_tiles.TryGetValue((zoom, tileX, tileY), out var tile))
//            tile.Log += log;
//        else
//            Debug.LogWarning($"❌[AddLog] Нет тайла Z{zoom} [{tileX}, {tileY}]");
//    }

//    // Проверка isUsed
//    public bool IsUsed(WorldPosition wp) => IsUsed(wp.zoom, wp.tileX, wp.tileY);
//    public bool IsUsed(int zoom, int tileX, int tileY)
//    {
//        if (_tiles.TryGetValue((zoom, tileX, tileY), out var tile))
//            return tile.isUsed;

//        Debug.LogWarning($"❌[IsUsed] Нет тайла Z{zoom} [{tileX}, {tileY}]");
//        return false;
//    }

//    // Получить лог
//    public string GetLog(int zoom, int tileX, int tileY)
//    {
//        if (_tiles.TryGetValue((zoom, tileX, tileY), out var tile))
//            return tile.Log;

//        Debug.LogWarning($"❌[GetLog] Нет тайла Z{zoom} [{tileX}, {tileY}]");
//        return "";
//    }

//    // Получить список готовых детей
//    public List<GameObject> GetChildReady(int zoom, int tileX, int tileY)
//    {
//        if (!_tiles.TryGetValue((zoom, tileX, tileY), out var tileParent))
//        {
//            Debug.LogWarning($"❌[GetChildReady] Нет родителя Z{zoom} [{tileX}, {tileY}]");
//            return null;
//        }

//        var listChild = new List<GameObject>();
//        int summ = 0;
//        for (int x = 0; x <= 1; x++)
//        {
//            for (int y = 0; y <= 1; y++)
//            {
//                int __tileX = tileX * 2 + x;
//                int __tileY = tileY * 2 + y;
//                int __zoom = zoom + 1; // сделаем int, чтобы не было неоднозначности типов
//                if (_tiles.TryGetValue((__zoom, __tileX, __tileY), out var tileChild))
//                {
//                    if (tileChild.isMeshReady)
//                    {
//                        listChild.Add(tileChild.gameObject);
//                        summ++;
//                    }
//                }
//            }
//        }

//        if (summ == 4)
//            Debug.Log($"[GetChildReady] у родителя Z{zoom} [{tileX}, {tileY}] все 4 ребёнка готовы!");

//        return listChild;
//    }

//    // Процессинг тайлов
//    public void ProcessTiles(int limit = 100)
//    {
//        var levels = _tiles.Keys
//            .Select(k => k.z)
//            .Distinct()
//            .OrderByDescending(z => z);

//        int processedCount = 0;
//        foreach (var z in levels)
//        {
//            var tilesAtZ = _tiles.Where(kv => kv.Key.z == z).Select(kv => kv.Value);

//            foreach (var tile in tilesAtZ)
//            {
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

//    // Попробовать заменить объект
//    public void TryChange(TileObject obj)
//    {
//        if (obj == null) return;

//        var key = (obj.worldPosition.zoom, obj.worldPosition.tileX, obj.worldPosition.tileY);
//        if (_tiles.ContainsKey(key))
//            _tiles[key] = obj;
//        else
//            Debug.LogWarning($"❌[TryChange] Нет записи Z{obj.worldPosition.zoom} [{obj.worldPosition.tileX}, {obj.worldPosition.tileY}]");
//    }

//    // Установить isUsed
//    public bool SetUsed((int z, int x, int y) key, bool isUsed)
//    {
//        if (_tiles.TryGetValue(key, out var tile))
//        {
//            tile.isUsed = isUsed;
//            if (tile.gameObject)
//                tile.gameObject.transform.GetChild(0).gameObject.SetActive(isUsed);
//            return true;
//        }
//        return false;
//    }
//    ///Установить isMeshReady. вернет иитину тольо если запись есть и меш готов
//    public bool SetMeshReady((int z, int x, int y) key, bool IsMeshReady)
//    {
//        if (_tiles.TryGetValue(key, out var tile))
//        {
//            tile.isMeshReady = IsMeshReady;
//            return IsMeshReady;
//        }
//        return false;
//    }

//    public void UpdateTileVisibility(int lowZoom, int baseZoom)
//    {
//        // Перебираем тайлы по возрастанию Z
//        for (int z = lowZoom; z <= baseZoom; z++)
//        {
//            // Фильтруем только тайлы этого уровня
//            var tilesAtZoom = _tiles
//                .Where(kv => kv.Key.z == z)
//                .ToList();

//            foreach (var kv in tilesAtZoom)
//            {
//                if (kv.Key.z == lowZoom) SetUsed(kv.Key, true);//для самых крупных тайлов выставляем видимость

//                //пропускаем неготовые
//                if (kv.Value == null || kv.Value.gameObject == null || !kv.Value.isMeshReady || !kv.Value.isUsed)
//                    continue;


//                var parentKey = kv.Key;

//                // 🔹 Если дети все готовы → показать их, спрятать родителя
//                if (TryUseChildren(parentKey))
//                {
//                    SetUsed(parentKey, false);
//                }
//            }
//        }
//    }

//    /// <summary>
//    /// Проверяет, есть ли у тайла все 4 ребёнка с готовым мешем.
//    /// Если есть — включает их (SetUsed=true) и возвращает true.
//    /// Если нет — ничего не трогает и возвращает false.
//    /// </summary>
//    public bool TryUseChildren((int z, int x, int y) parentKey)
//    {
//        var (z, x, y) = parentKey;
//        int childZ = z + 1;

//        var readyChildren = new List<(int z, int x, int y)>(4);

//        for (int cx = 0; cx < 2; cx++)
//        {
//            for (int cy = 0; cy < 2; cy++)
//            {
//                var childKey = (childZ, x * 2 + cx, y * 2 + cy);

//                if (!_tiles.TryGetValue(childKey, out var childTile) || !childTile.isMeshReady)
//                    return false; // Один не готов → сразу false

//                readyChildren.Add(childKey);
//            }
//        }

//        // ✅ Все 4 готовы → включаем детей
//        foreach (var ck in readyChildren)
//            SetUsed(ck, true);

//        return true;
//    }




//    // Уничтожить тайл (не удаляя из словаря)
//    public void DestroyTile((int z, int x, int y) key)
//    {
//        if (!_tiles.TryGetValue(key, out var tile))
//            return;
//        if (!tile.isMeshReady)
//            return;

//        if (tile.gameObject != null)
//        {
//            var mf = tile.gameObject.GetComponent<MeshFilter>();
//            if (mf && mf.mesh != null)
//            {
//                Object.Destroy(mf.mesh);
//                mf.mesh = null;
//            }

//            var mr = tile.gameObject.GetComponent<MeshRenderer>();
//            if (mr && mr.material != null)
//            {
//                Object.Destroy(mr.material);
//                mr.material = null;
//            }

//            Object.Destroy(tile.gameObject);
//            tile.gameObject = null;
//        }
//    }

//    // ---- IEnumerable implementation ----
//    public IEnumerator<KeyValuePair<(int z, int x, int y), TileObject>> GetEnumerator()
//    {
//        return _tiles.GetEnumerator();
//    }

//    IEnumerator IEnumerable.GetEnumerator()
//    {
//        return GetEnumerator();
//    }
//}
