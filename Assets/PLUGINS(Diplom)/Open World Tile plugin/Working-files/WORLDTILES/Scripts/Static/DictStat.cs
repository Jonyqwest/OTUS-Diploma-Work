namespace WorldTile
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [System.Serializable]
    public class TileObject
    {
        public WorldPosition worldPosition;
        public GameObject gameObject;
        public TerrainTile terrainObject;
        public string Log;
        public bool isVisible;
        public bool isComposite;
        public bool isMeshReady;
        public bool isTextureReady;
        public float LastUsedTime;

        public TileObject(WorldPosition worldPosition, TerrainTile terrainObject, GameObject gameObject = null, bool isVisible = false, bool isMeshReady = false, bool isTextureReady = false)
        {
            this.worldPosition = worldPosition;
            if (gameObject != null)
                this.gameObject = gameObject;
            if (terrainObject != null)
                this.terrainObject = terrainObject;
            this.isVisible = isVisible;
            this.isMeshReady = isMeshReady;
            this.isTextureReady = isTextureReady;
            this.Log = string.Empty;
        }
    }

    public static class DictStat
    {
        public static SafeTileDictionary dictTiles = new SafeTileDictionary();

        public static void Init()
        {
            if (dictTiles == null)
                dictTiles = new SafeTileDictionary();
            else
                dictTiles.Clear();
        }
    }

    /// <summary>
    /// Оптимизированный словарь для хранения тайлов.
    /// </summary>
    public class SafeTileDictionary : IEnumerable<KeyValuePair<(int z, int x, int y), TileObject>>
    {
        // Теперь храним тайлы по Z для быстрого доступа
        private readonly Dictionary<int, Dictionary<(int x, int y), TileObject>> _tilesByZ = new();
        private int _count;

        public int Count => _count;
        public Dictionary<(int z, int x, int y), TileObject> Raw =>
            _tilesByZ.SelectMany(zKvp => zKvp.Value.Select(inner =>
                new KeyValuePair<(int z, int x, int y), TileObject>((zKvp.Key, inner.Key.x, inner.Key.y), inner.Value)))
                .ToDictionary(k => k.Key, v => v.Value);

        public TileObject this[(int z, int x, int y) key]
        {
            get => TryGetValue(key, out var v) ? v : null;
            set => AddOrReplace(value);
        }

        public IEnumerable<(int z, int x, int y)> Keys =>
            _tilesByZ.SelectMany(zKvp => zKvp.Value.Keys.Select(k => (zKvp.Key, k.x, k.y)));

        public IEnumerable<TileObject> Values => _tilesByZ.Values.SelectMany(d => d.Values);

        // Добавление или замена тайла
        public void AddOrReplace(TileObject tile)
        {
            var z = tile.worldPosition.zoom;
            var key = (tile.worldPosition.tileX, tile.worldPosition.tileY);

            if (!_tilesByZ.TryGetValue(z, out var dict))
            {
                dict = new Dictionary<(int x, int y), TileObject>();
                _tilesByZ[z] = dict;
            }

            if (!dict.ContainsKey(key))
                _count++;
            dict[key] = tile;
        }
        // TryGetValue
        public bool TryGetValue(ZXY key, out TileObject tile)
        {
            int z = key.z;
            int x = key.x;
            int y = key.y;
            return TryGetValue((z, x, y), out tile);
        }

        // TryGetValue
        public bool TryGetValue((int z, int x, int y) key, out TileObject tile)
        {
            if (_tilesByZ.TryGetValue(key.z, out var dict))
                return dict.TryGetValue((key.x, key.y), out tile);

            tile = null;
            return false;
        }

        // GetValue
        public TileObject GetValue((int z, int x, int y) key)
        {
            TileObject tile;
            if (_tilesByZ.TryGetValue(key.z, out var dict))
            {
                dict.TryGetValue((key.x, key.y), out tile);
                return tile;
            }

            tile = null;
            return tile;
        }
        // Try Get Terrain
        public bool TryGetTerrain(ZXY zxy, out TerrainTile terrain)
        {
            return TryGetTerrain((zxy.z, zxy.x, zxy.y), out terrain);
        }

        // Try Get Terrain
        public bool TryGetTerrain((int z, int x, int y) key, out TerrainTile terrain)
        {
            if (_tilesByZ.TryGetValue(key.z, out var dict))
            {
                 if(dict.TryGetValue((key.x, key.y), out var tile))
                    if (tile.terrainObject)
                    {
                        terrain = tile.terrainObject;
                        return true;
                    }
            }
            terrain = null;
            return false;
        }
        //// Get Terrain
        //public TerrainTile GetTerrain((int z, int x, int y) key)
        //{
        //    TileObject tile;
        //    if (_tilesByZ.TryGetValue(key.z, out var dict))
        //    {
        //        dict.TryGetValue((key.x, key.y), out tile);

        //        return tile.terrainObject;
        //    }
        //    return null;
        //}



        /// <summary>
        /// Добавляет текст в лог тайла. Совместим с вызовом DictStat.dictTiles.AddLog(z,x,y, text)
        /// </summary>
        public void AddLog(int zoom, int tileX, int tileY, string log)
        {
            if (TryGetValue((zoom, tileX, tileY), out var tile))
            {
                // безопасно: tile.Log уже инициализирован в конструкторе
                tile.Log += log;
            }
            else
            {
                Debug.LogWarning($"❌[AddLog] Нет тайла Z{zoom} [{tileX}, {tileY}]");
            }
        }

        /// <summary>
        /// Получить лог для тайла. Возвращает пустую строку если записи нет.
        /// </summary>
        public string GetLog(int zoom, int tileX, int tileY)
        {
            if (TryGetValue((zoom, tileX, tileY), out var tile))
            {
                return tile.Log ?? string.Empty;
            }

            Debug.LogWarning($"❌[GetLog] Нет тайла Z{zoom} [{tileX}, {tileY}]");
            return string.Empty;
        }
        /// <summary>
        /// Проверка isUsed (перегрузки для WorldPosition и для отдельных координат)
        /// </summary>
        public bool GetUsed(WorldPosition wp) => GetVisible(wp.zoom, wp.tileX, wp.tileY);

        public bool GetVisible(int zoom, int tileX, int tileY)
        {
            if (TryGetValue((zoom, tileX, tileY), out var tile))
                return tile.isVisible;

            // Ранее у тебя логировалось, оставляем тот же поведенческий контракт
            Debug.LogWarning($"❌[IsUsed] Нет тайла Z{zoom} [{tileX}, {tileY}]");
            return false;
        }
        public bool ContainsKey((int z, int x, int y) key)
            => _tilesByZ.TryGetValue(key.z, out var dict) && dict.ContainsKey((key.x, key.y));

        public bool Remove((int z, int x, int y) key)
        {
            if (_tilesByZ.TryGetValue(key.z, out var dict))
            {
                bool removed = dict.Remove((key.x, key.y));
                if (removed)
                    _count--;
                if (dict.Count == 0)
                    _tilesByZ.Remove(key.z);
                return removed;
            }
            return false;
        }

        public void Clear()
        {
            _tilesByZ.Clear();
            _count = 0;
        }

        // Получить тайлы на конкретном Z
        public IEnumerable<KeyValuePair<(int z, int x, int y), TileObject>> GetTilesAtZ(int z)
        {
            if (!_tilesByZ.TryGetValue(z, out var dict))
                yield break;

            foreach (var kv in dict)
                yield return new KeyValuePair<(int z, int x, int y), TileObject>((z, kv.Key.x, kv.Key.y), kv.Value);
        }

        // Все уровни Z отсортированные
        public IEnumerable<int> GetZoomLevelsSorted(bool ascending = true)
            => ascending ? _tilesByZ.Keys.OrderBy(z => z) : _tilesByZ.Keys.OrderByDescending(z => z);

        // Установить isVisible
        public bool SetVisibility((int z, int x, int y) key, bool isVisible)
        {
            if (TryGetValue(key, out var tile))
            {
                tile.isVisible = isVisible;
                if (tile.gameObject)
                    tile.gameObject.transform.GetChild(0).gameObject.SetActive(isVisible);
                return true;
            }
            return false;
        }
        public bool SetComposite((int z, int x, int y) key, bool isComposite)
        {
            if (TryGetValue(key, out var tile))
            {
                tile.isComposite = isComposite;
                return isComposite;
            }
            return false;
        }
        // Установить isMeshReady
        public bool SetMeshReady((int z, int x, int y) key, bool IsMeshReady)
        {
            if (TryGetValue(key, out var tile))
            {
                tile.isMeshReady = IsMeshReady;
                return IsMeshReady;
            }
            return false;
        }

        // Установить isTextureReady
        public bool SetTextureReady((int z, int x, int y) key, bool IsTextureReady)
        {
            if (TryGetValue(key, out var tile))
            {
                tile.isTextureReady = IsTextureReady;
                return IsTextureReady;
            }
            return false;
        }

        // ✅ Оптимизированный UpdateTileVisibility
        public int UpdateTileVisibility(int lowZoom, int baseZoom)
        {
            int resSum = 0;
            // Перебираем уровни зума по возрастанию
            foreach (var z in GetZoomLevelsSorted(true))
            {
                if (z < lowZoom || z > baseZoom)
                    continue;

                // Получаем тайлы только этого уровня
                if (!_tilesByZ.TryGetValue(z, out var tilesAtZ))
                    continue;

                foreach (var kv in tilesAtZ)
                {
                    var _key = (z, kv.Key.x, kv.Key.y);
                    // Для самых крупных тайлов выставляем видимость
                    if (z == lowZoom)
                    {
                        SetVisibility(_key, true);
                        resSum++;
                    }

                    // Пропускаем неготовые
                    if (kv.Value == null || kv.Value.gameObject == null ||
                        !kv.Value.isMeshReady || !kv.Value.isTextureReady)
                        continue;

                    // 🔹 Если все дети готовы → показать их, спрятать родителя
                    if (TryUseChildren(_key))
                    {
                        SetVisibility(_key, false);
                        SetComposite(_key, true);
                        resSum += 3;
                    }
                    else
                    {
                        // 🔹 Дети не все готовы или не нужны → прячем детей, показываем родителя
                        HideChildren(_key);
                        SetVisibility(_key, true);
                        SetComposite(_key, false);
                    }

                }
            }
            return resSum;
        }


        // Проверка детей
        public bool TryUseChildren((int z, int x, int y) parentKey)
        {
            var (z, x, y) = parentKey;
            int childZ = z + 1;

            var readyChildren = new List<(int z, int x, int y)>(4);

            for (int cx = 0; cx < 2; cx++)
            {
                for (int cy = 0; cy < 2; cy++)
                {
                    var childKey = (childZ, x * 2 + cx, y * 2 + cy);

                    if (!TryGetValue(childKey, out var childTile) /*|| !childTile.isMeshReady*/)
                        return false;

                    readyChildren.Add(childKey);
                }
            }
            //показываем детей, родителя прячем ставим пометку что тайл композитный
            SetComposite(parentKey, true);
            SetVisibility(parentKey, false);
            foreach (var ck in readyChildren)
                SetVisibility(ck, true);

            return true;
        }


        // Скрыть всех детей тайла
        private void HideChildren((int z, int x, int y) parentKey)
        {
            SetComposite(parentKey, false);
            for (int dx = 0; dx < 2; dx++)
            {
                for (int dy = 0; dy < 2; dy++)
                {
                    var childKey = (parentKey.z + 1,
                                    parentKey.x * 2 + dx,
                                    parentKey.y * 2 + dy);

                    if (TryGetValue(childKey, out var child))
                    {
                        SetVisibility(childKey, false);
                    }
                }
            }
        }

        ////private readonly HashSet<(int x, int y)> _visibleBuffer = new();
        ////⚡️
        //public List<(int z, int x, int y)> GetOutRangeTiles(HashSet<(int z, int x, int y)> currentTiles,
        //    int lowZoom = TileStat.LowZoom,
        //    int baseZoom = TileStat.BaseZoom)
        //{
        //    var toRemove = new List<(int z, int x, int y)>();

        //    for (int z = lowZoom; z <= baseZoom; z++)
        //    {
        //        if (!_tilesByZ.TryGetValue(z, out var tilesAtZ))
        //            continue;

        //        // пробегаем только по тайлам
        //        foreach (var kv in tilesAtZ)
        //        {
        //            if (!currentTiles.Contains((z, kv.Key.x, kv.Key.y)))
        //            {
        //                //ZXY key = new((byte)z, kv.Key.x, kv.Key.y);
        //                toRemove.Add((z, kv.Key.x, kv.Key.y));
        //            }
        //        }
        //    }

        //    return toRemove;
        //}


        //// Очередь на удаление
        //private readonly Queue<(int z, int x, int y)> _toRemoveQueue = new();

        //// Время последней проверки
        //private float _lastCleanupTime = 0f;

        //// Интервал очистки (раз в секунду)
        //public float cleanupInterval = 1f;

        //// Лимит удаления за один кадр
        //public int maxRemovalsPerTick = 6;


        //public void UpdateCleanup_(List<(int z, int x, int y)> removeTiles)
        //{
        //    if (removeTiles.Count == 0) return;

        //    // 1) Раз в секунду собираем кандидатов
        //    if (Time.time - _lastCleanupTime >= cleanupInterval)
        //    {
        //        _lastCleanupTime = Time.time;

        //        // Собираем список кандидатов на удаление
        //        foreach (var c in removeTiles)
        //            _toRemoveQueue.Enqueue(c);
        //    }
            
        //    // 2) Постепенно обрабатываем очередь
        //    int removed = 0;
        //    while (_toRemoveQueue.Count > 0 && removed < maxRemovalsPerTick)
        //    { var zxy = _toRemoveQueue.Dequeue();
        //        var key = (zxy.z, zxy.x, zxy.y);
        //        if (TryGetValue(key, out var tile) && tile != null && tile.gameObject != null)
        //        {
        //            TilePoolManager.Instance.ReleaseTile(tile.gameObject);
        //            Remove(key);
        //            removed++;
        //        }
        //    }
        //}

        // ---- IEnumerable implementation ----
        public IEnumerator<KeyValuePair<(int z, int x, int y), TileObject>> GetEnumerator()
        {
            foreach (var zKvp in _tilesByZ)
                foreach (var kv in zKvp.Value)
                    yield return new KeyValuePair<(int z, int x, int y), TileObject>(
                        (zKvp.Key, kv.Key.x, kv.Key.y), kv.Value
                    );
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
