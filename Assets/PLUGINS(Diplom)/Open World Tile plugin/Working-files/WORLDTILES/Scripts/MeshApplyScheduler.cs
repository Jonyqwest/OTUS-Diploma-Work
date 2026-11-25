namespace WorldTile
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Планировщик применения мешей на сцену с ограничением времени на кадр (time-budget).
    /// Вызывай MeshApplyScheduler.Updater() в Update любого MonoBehaviour.
    /// </summary>
    public static class MeshApplyScheduler
    {
        public static float MaxFrameTimeMs { get; set; } = 1f;
        public static int HardMaxTasksPerFrame { get; set; } = 32;
        public static int MaxPendingTasks { get; set; } = 512;
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

        /// <summary>
        /// Compatibility overload. New awaitable producers should pass onCanceled,
        /// so replacement/shutdown can complete their promise without applying a mesh.
        /// </summary>
        public static void Enqueue(TerrainTile tile, Action applyAction)
        {
            Enqueue(tile, applyAction, null);
        }

        public static void Enqueue(TerrainTile tile, Action applyAction, Action onCanceled)
        {
            if (tile == null || applyAction == null)
            {
                onCanceled?.Invoke();
                return;
            }

            Action canceledOutsideLock = null;
            bool reject = false;

            lock (sync)
            {
                if (pendingByTile.TryGetValue(tile, out var existingNode))
                {
                    // Latest generation wins when the previous producer supplied a
                    // completion callback. Legacy entries cannot be discarded safely.
                    if (existingNode.Value.onCanceled != null)
                    {
                        queue.Remove(existingNode);
                        pendingByTile.Remove(tile);
                        canceledOutsideLock = existingNode.Value.onCanceled;
                    }
                    else
                    {
                        reject = true;
                    }
                }

                if (!reject && queue.Count >= Mathf.Max(1, MaxPendingTasks))
                    reject = true;

                if (!reject)
                {
                    var task = new ApplyTask
                    {
                        tile = tile,
                        generation = tile.GenerationId,
                        action = applyAction,
                        onCanceled = onCanceled
                    };
                    pendingByTile[tile] = queue.AddLast(task);
                }
            }

            InvokeCanceled(canceledOutsideLock);

            if (!reject)
                return;

            // A synchronous failure propagates through QueueApplyMesh instead of
            // returning a UniTaskCompletionSource which can never be completed.
            throw new InvalidOperationException(
                $"[MeshApplyScheduler] Queue is full or tile already has a pending mesh ({PendingCount}/{MaxPendingTasks}).");
        }

        public static void Process()
        {
            LastAppliedCount = 0;
            LastFrameSpentMs = 0f;

            if (!Enabled) return;
            if (MaxFrameTimeMs <= 0f && HardMaxTasksPerFrame <= 0) return;

            float startMs = Time.realtimeSinceStartup * 1000f;
            int processed = 0;

            while (TryDequeue(out var task))
            {
                bool stale = task.tile == null || task.tile.GenerationId != task.generation;
                if (stale)
                    CancelTask(task);
                else
                    ApplyTaskNow(task);

                processed++;
                LastFrameSpentMs = (Time.realtimeSinceStartup * 1000f) - startMs;

                if (HardMaxTasksPerFrame > 0 && processed >= HardMaxTasksPerFrame) break;
                if (MaxFrameTimeMs > 0f && LastFrameSpentMs >= MaxFrameTimeMs) break;
            }
        }

        public static void Updater() => Process();

        /// <summary>
        /// Cancels all queued work. Legacy entries have no cancellation callback;
        /// invoking their guarded action is the only compatible way to complete an
        /// enclosed UniTaskCompletionSource.
        /// </summary>
        public static void ClearQueue()
        {
            List<ApplyTask> canceled;
            lock (sync)
            {
                canceled = new List<ApplyTask>(queue);
                queue.Clear();
                pendingByTile.Clear();
            }

            foreach (var task in canceled)
                CancelTask(task);

            LastAppliedCount = 0;
            LastFrameSpentMs = 0f;
        }

        public static void Configure(float maxFrameTimeMs, int hardMaxTasksPerFrame = 64, bool enabled = true)
        {
            MaxFrameTimeMs = Mathf.Max(0f, maxFrameTimeMs);
            HardMaxTasksPerFrame = Mathf.Max(0, hardMaxTasksPerFrame);
            Enabled = enabled;
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

        private static void ApplyTaskNow(ApplyTask task)
        {
            try
            {
                task.action?.Invoke();
                LastAppliedCount++;
            }
            catch (MissingReferenceException)
            {
                InvokeCanceled(task.onCanceled);
            }
            catch (Exception exception)
            {
                Debug.LogError("[MeshApplyScheduler] Apply mesh error: " + exception);
                InvokeCanceled(task.onCanceled);
            }
        }

        private static void CancelTask(ApplyTask task)
        {
            if (task.onCanceled != null)
            {
                InvokeCanceled(task.onCanceled);
                return;
            }

            // Compatibility for the old TerrainTile closure: its generation/resource
            // guard completes the enclosed TCS without doing the heavy mesh apply.
            try
            {
                task.action?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[MeshApplyScheduler] Legacy canceled task failed: " + exception.Message);
            }
        }

        private static void InvokeCanceled(Action callback)
        {
            if (callback == null) return;
            try { callback(); }
            catch (Exception exception)
            {
                Debug.LogWarning("[MeshApplyScheduler] Cancellation callback failed: " + exception.Message);
            }
        }
    }
}
