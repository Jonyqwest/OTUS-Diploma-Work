namespace WorldTile
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public static class DelSystem
    {
        private static readonly Queue<(int z, int x, int y)> toRemoveQueue = new();
        private static readonly HashSet<(int z, int x, int y)> queuedForRemoval = new();

        // Лимит запусков очистки за один кадр.
        public static int maxRemovalsPerTick = 6;
        public static int PendingRemovalCount => toRemoveQueue.Count;

        public static HashSet<(int z, int x, int y)> DelTilesActivator(
            HashSet<(int z, int x, int y)> currentTiles)
        {
            var keysToRemove = new HashSet<(int z, int x, int y)>();

            if (DictStat.dictTiles == null || DictStat.dictTiles.Count == 0)
                return keysToRemove;

            foreach (var item in DictStat.dictTiles)
            {
                var terrainObject = item.Value?.terrainObject;
                if (terrainObject == null)
                    continue;

                if (!terrainObject.IsVisible && !currentTiles.Contains(item.Key))
                {
                    // МГНОВЕННАЯ ОТМЕНА: обрываем сетевую загрузку сразу, не дожидаясь очереди удаления!
                    terrainObject.CancelLoading();

                    keysToRemove.Add(item.Key);
                    if (queuedForRemoval.Add(item.Key))
                        toRemoveQueue.Enqueue(item.Key);
                }
            }

            return keysToRemove;
        }

        public static void Updater__Cleanup(HashSet<(int z, int x, int y)> currentTiles)
        {
            if (toRemoveQueue.Count == 0 || currentTiles == null)
                return;

            // A tile may have become current again while waiting in the queue.
            // Rotate the existing queue once instead of allocating and copying a
            // replacement Queue every frame while a cleanup backlog exists.
            int queuedAtStart = toRemoveQueue.Count;
            for (int i = 0; i < queuedAtStart; i++)
            {
                var key = toRemoveQueue.Dequeue();
                if (!currentTiles.Contains(key))
                    toRemoveQueue.Enqueue(key);
                else
                    queuedForRemoval.Remove(key);
            }

            int removed = 0;
            while (toRemoveQueue.Count > 0 && removed < maxRemovalsPerTick)
            {
                var key = toRemoveQueue.Dequeue();
                queuedForRemoval.Remove(key);

                if (DictStat.dictTiles.TryGetValue(key, out var tile) &&
                    tile?.terrainObject != null)
                {
                    TerrainTile terrainTile = tile.terrainObject;

                    // Stop new systems from finding the old tile immediately.
                    // The GameObject enters the pool only after cleanup finishes.
                    DictStat.dictTiles.Remove(key);
                    ReleaseAndPoolAsync(terrainTile).Forget();
                    removed++;
                }
            }
        }

        public static void Reset()
        {
            toRemoveQueue.Clear();
            queuedForRemoval.Clear();
        }

        private static async UniTask ReleaseAndPoolAsync(TerrainTile terrainTile)
        {
            try
            {
                await terrainTile.PrepareForPoolAsync();
                if (terrainTile == null)
                    return;

                terrainTile.State.ChangeState(terrainTile, new TileState_InPool());

                if (TilePoolManager.Instance != null)
                    TilePoolManager.Instance.ReleaseTile(terrainTile.gameObject);
                else
                    Object.Destroy(terrainTile.gameObject);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[DelSystem] Tile release failed: {exception}");
                if (terrainTile != null)
                    Object.Destroy(terrainTile.gameObject);
            }
        }
    }
}
