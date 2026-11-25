using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshPool
{
    private static readonly Queue<Mesh> s_pool = new Queue<Mesh>();
    private static readonly Dictionary<Mesh, int> s_usage = new Dictionary<Mesh, int>(); // учёт ссылок
    private static readonly HashSet<Mesh> s_owned = new HashSet<Mesh>();
    private static readonly object s_lock = new object();

    // Mesh.Clear can retain native capacity. Keep the idle pool deliberately
    // small so a streaming session cannot retain hundreds of full terrain meshes.
    private const int DefaultMaxPoolSize = 32;
    private static int s_maxPoolSize = DefaultMaxPoolSize;

    public static int MaxPoolSize
    {
        get { lock (s_lock) return s_maxPoolSize; }
        set
        {
            lock (s_lock)
            {
                s_maxPoolSize = value < 0 ? 0 : value;
                TrimUnlocked(s_maxPoolSize);
            }
        }
    }

    // 🔹 Статистика
    public static int CreatedCount { get; private set; } = 0;   // Сколько всего было создано
    public static int ReturnedCount { get; private set; } = 0;  // Сколько всего возвращено
    public static int DestroyedCount { get; private set; } = 0;
    public static int CurrentCount
    {
        get { lock (s_lock) return s_pool.Count; }
    }
    public static int ActiveCount
    {
        get { lock (s_lock) return s_usage.Count; }
    }

    /// <summary>
    /// True only for meshes created by this pool. Prefab/imported shared meshes
    /// are never adopted implicitly and must not be returned to this pool.
    /// </summary>
    public static bool IsOwnedByPool(Mesh mesh)
    {
        if (mesh == null) return false;
        lock (s_lock) return s_owned.Contains(mesh);
    }

    public static bool IsCheckedOut(Mesh mesh)
    {
        if (mesh == null) return false;
        lock (s_lock) return s_usage.ContainsKey(mesh);
    }

    // 🔹 Взять меш из пула
    public static Mesh GetMesh()
    {
        lock (s_lock)
        {
            Mesh m = null;

            if (s_pool.Count > 0)
            {
                m = s_pool.Dequeue();
                if (m == null)
                {
                    s_owned.Remove(m);
                    m = CreateNewMesh();
                }
            }
            else
            {
                m = CreateNewMesh();
            }

            // помечаем как занятый
            s_usage[m] = 1;
            return m;
        }
    }

    // 🔹 Увеличить счётчик ссылок (например, если меш используется в двух тайлах)
    public static void RetainMesh(Mesh m)
    {
        if (m == null) return;

        lock (s_lock)
        {
            if (!s_usage.TryGetValue(m, out int references))
            {
                Debug.LogWarning("[MeshPool] Retain ignored for a mesh that is not checked out from this pool.");
                return;
            }

            s_usage[m] = references + 1;
        }
    }

    // 🔹 Освободить меш (уменьшаем счётчик, и если он больше не нужен — возвращаем в пул)
    public static void ReleaseMesh(Mesh m)
    {
        if (object.ReferenceEquals(m, null)) return;

        lock (s_lock)
        {
            if (m == null)
            {
                // A Unity object can be externally destroyed while its managed
                // wrapper is still a dictionary key. Remove that stale ownership.
                s_usage.Remove(m);
                s_owned.Remove(m);
                return;
            }

            if (!s_usage.TryGetValue(m, out int references))
            {
                // A prefab/shared mesh may legitimately be present before the
                // first generated mesh is assigned. Never adopt or destroy it.
                if (s_owned.Contains(m))
                    Debug.LogWarning("[MeshPool] Release ignored for an owned mesh that is not checked out.");
                return;
            }

            references--;

            if (references > 0)
            {
                s_usage[m] = references;
                return;
            }

            s_usage.Remove(m);

            // Очистка перед возвратом
            m.Clear();
            m.indexFormat = IndexFormat.UInt32;
            m.MarkDynamic();

            if (s_pool.Count < s_maxPoolSize)
            {
                s_pool.Enqueue(m);
                ReturnedCount++;
            }
            else
            {
                DestroyOwnedMesh(m);
            }
        }
    }

    /// <summary>Destroys idle pooled meshes until at most retainedCount remain.</summary>
    public static void Trim(int retainedCount = 0)
    {
        lock (s_lock)
            TrimUnlocked(retainedCount < 0 ? 0 : retainedCount);
    }

    /// <summary>
    /// Clears idle capacity and counters. Checked-out meshes stay valid by default;
    /// pass true only during teardown when no renderer can still reference them.
    /// </summary>
    public static void Reset(bool destroyCheckedOutMeshes = false)
    {
        lock (s_lock)
        {
            TrimUnlocked(0);

            if (destroyCheckedOutMeshes)
            {
                foreach (var mesh in s_usage.Keys)
                    DestroyOwnedMesh(mesh);

                s_usage.Clear();
            }

            CreatedCount = destroyCheckedOutMeshes ? 0 : s_usage.Count;
            ReturnedCount = 0;
            DestroyedCount = 0;
        }
    }

    // 🔹 Создание нового меша
    private static Mesh CreateNewMesh()
    {
        var mesh = new Mesh
        {
            indexFormat = IndexFormat.UInt32,
            name = "PooledMesh"
        };
        mesh.MarkDynamic();
        s_owned.Add(mesh);
        CreatedCount++;
        return mesh;
    }

    private static void TrimUnlocked(int retainedCount)
    {
        while (s_pool.Count > retainedCount)
        {
            Mesh mesh = s_pool.Dequeue();
            if (mesh != null)
                DestroyOwnedMesh(mesh);
        }
    }

    private static void DestroyOwnedMesh(Mesh mesh)
    {
        if (mesh == null)
            return;

        s_owned.Remove(mesh);
        Object.Destroy(mesh);
        DestroyedCount++;
    }

    // 🔹 Отладочный вывод статистики
    public static string GetStats()
    {
        lock (s_lock)
        {
            return $"[MeshPool] InPool={s_pool.Count}, Active={s_usage.Count}, Created={CreatedCount}, Returned={ReturnedCount}, Destroyed={DestroyedCount}, Max={s_maxPoolSize}";
        }
    }
}
