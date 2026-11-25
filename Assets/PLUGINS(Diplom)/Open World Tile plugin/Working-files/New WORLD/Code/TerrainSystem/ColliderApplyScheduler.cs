namespace WorldTile
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Планировщик применения коллайдеров (строго ограниченный — обычно 1 за кадр).
    /// Хранит только последнюю задачу для каждого поколения тайла.
    /// </summary>
    public static class ColliderApplyScheduler
    {
        public static float MaxFrameTimeMs { get; set; } = 0.5f;
        public static int HardMaxTasksPerFrame { get; set; } = 1;
        public static int MaxPendingTasks { get; set; } = 256;
        private static bool enabled = true;
        public static bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value) return;
                enabled = value;
                if (!enabled)
                    ClearQueue();
            }
        }

        public static int LastAppliedCount { get; private set; }
        public static float LastFrameSpentMs { get; private set; }
        public static int PendingCount { get { lock (sync) return queue.Count; } }

        private sealed class ApplyTask
        {
            public TerrainTile tile;
            public int generation;
            public Action action;
            public Action onCanceled;
        }

        private static readonly LinkedList<ApplyTask> queue = new();
        private static readonly Dictionary<TerrainTile, LinkedListNode<ApplyTask>> pendingByTile = new();
        private static readonly object sync = new();

        public static void Enqueue(TerrainTile tile, Action applyAction)
        {
            Enqueue(tile, applyAction, null);
        }

        public static void Enqueue(TerrainTile tile, Action applyAction, Action onCanceled)
        {
            if (tile == null || applyAction == null)
            {
                InvokeCanceled(onCanceled);
                return;
            }

            List<Action> canceled = null;
            lock (sync)
            {
                if (pendingByTile.TryGetValue(tile, out var existingNode))
                {
                    queue.Remove(existingNode);
                    pendingByTile.Remove(tile);
                    if (existingNode.Value.onCanceled != null)
                    {
                        canceled ??= new List<Action>(2);
                        canceled.Add(existingNode.Value.onCanceled);
                    }
                }

                while (queue.Count >= Mathf.Max(1, MaxPendingTasks))
                {
                    var oldest = queue.First;
                    queue.RemoveFirst();
                    if (!ReferenceEquals(oldest.Value.tile, null))
                        pendingByTile.Remove(oldest.Value.tile);
                    if (oldest.Value.onCanceled != null)
                    {
                        canceled ??= new List<Action>(2);
                        canceled.Add(oldest.Value.onCanceled);
                    }
                }

                var task = new ApplyTask
                {
                    tile = tile,
                    generation = tile.GenerationId,
                    action = applyAction,
                    onCanceled = onCanceled
                };
                pendingByTile[tile] = queue.AddLast(task);
            }

            if (canceled != null)
                foreach (var callback in canceled)
                    InvokeCanceled(callback);
        }

        public static void Process()
        {
            LastAppliedCount = 0;
            LastFrameSpentMs = 0f;
            if (!Enabled) return;

            float startMs = Time.realtimeSinceStartup * 1000f;
            int processed = 0;

            while (TryDequeue(out var task))
            {
                try
                {
                    if (task.tile == null || task.tile.GenerationId != task.generation)
                        InvokeCanceled(task.onCanceled);
                    else
                    {
                        task.action.Invoke();
                        LastAppliedCount++;
                    }
                }
                catch (MissingReferenceException)
                {
                    InvokeCanceled(task.onCanceled);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[ColliderApplyScheduler] Apply collider error: " + exception);
                    InvokeCanceled(task.onCanceled);
                }

                processed++;
                LastFrameSpentMs = (Time.realtimeSinceStartup * 1000f) - startMs;
                if (HardMaxTasksPerFrame > 0 && processed >= HardMaxTasksPerFrame) break;
                if (MaxFrameTimeMs > 0f && LastFrameSpentMs >= MaxFrameTimeMs) break;
            }
        }

        public static void Updater() => Process();

        public static void ClearQueue()
        {
            List<Action> callbacks = new();
            lock (sync)
            {
                foreach (var task in queue)
                    if (task.onCanceled != null)
                        callbacks.Add(task.onCanceled);
                queue.Clear();
                pendingByTile.Clear();
            }

            foreach (var callback in callbacks)
                InvokeCanceled(callback);
        }

        private static bool TryDequeue(out ApplyTask task)
        {
            lock (sync)
            {
                if (queue.First == null)
                {
                    task = null;
                    return false;
                }

                var node = queue.First;
                queue.RemoveFirst();
                task = node.Value;
                if (!ReferenceEquals(task.tile, null) &&
                    pendingByTile.TryGetValue(task.tile, out var current) &&
                    ReferenceEquals(current, node))
                    pendingByTile.Remove(task.tile);
                return true;
            }
        }

        private static void InvokeCanceled(Action callback)
        {
            if (callback == null) return;
            try { callback(); }
            catch (Exception exception)
            {
                Debug.LogWarning("[ColliderApplyScheduler] Cancellation callback failed: " + exception.Message);
            }
        }
    }
}
