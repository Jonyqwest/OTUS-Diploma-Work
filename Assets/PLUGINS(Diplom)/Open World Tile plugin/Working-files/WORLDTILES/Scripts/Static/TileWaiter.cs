namespace WorldTile
{
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;

    public static class TileWaiter
    {
        public static async UniTask<HashSet<(byte z,int x,int y)>> WaitAllReadyAsync(
        IEnumerable<TerrainTile> tiles,
        CancellationToken token,
        Action<float> onProgress = null)
        {
            if (tiles == null)
            {
                Debug.LogError("❌ WaitAllReadyAsync получил null коллекцию!");
                return new ();
            }
            HashSet<(byte z, int x, int y)> keys = new();
            var tileList = new List<(TerrainTile tile, int generation)>();
            foreach (var t in tiles)
            {
                if (t != null) tileList.Add((t, t.GenerationId));
            }
            int total = tileList.Count;
            int completed = 0;

            if (total == 0)
            {
                Debug.LogWarning("⚠️ Нет задач для ожидания в WaitAllReadyAsync");
                return new ();
            }

            // Do not attach a continuation to every tile readiness source. A cancelled
            // world request used to leave those continuations rooted by a stalled tile.
            // One generation-aware polling loop is cheap and is released on cancellation.
            while (completed < total)
            {
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < tileList.Count; i++)
                {
                    var item = tileList[i];
                    if (item.tile == null || item.tile.GenerationId != item.generation)
                        throw new OperationCanceledException("Tile was destroyed or reused while waiting.", token);

                    TileStateType state = item.tile.State.CurrentType;
                    if (state != TileStateType.Ready &&
                        state != TileStateType.CreatingSuperTile &&
                        state != TileStateType.SuperTile)
                        continue;

                    var key = item.tile.worldPosition.Key();
                    if (!DictStat.dictTiles.TryGetValue((key.Item1, key.Item2, key.Item3), out var tileObject) ||
                        tileObject == null || !tileObject.isMeshReady)
                        continue;

                    keys.Add(key);
                    tileList.RemoveAt(i--);
                    completed++;
                    onProgress?.Invoke((float)completed / total);
                }

                if (completed < total)
                    await UniTask.Yield();
            }

            return keys;
        }


    }

}
