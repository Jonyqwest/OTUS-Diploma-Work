namespace WorldTile
{
    using System.Collections.Generic;
    using UnityEngine;

    public class TilePoolManager : MonoBehaviour
    {
        public static TilePoolManager Instance { get; private set; }

        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform PoolContainer;
        [SerializeField] private int initialPoolSize = 350;
        [SerializeField] private int maxPoolSize = 1000; // 🔹 Лимит (по желанию)

        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        // 🔹 Статистика
        public int CreatedCount { get; private set; } = 0;   // Сколько всего было создано
        public int ReturnedCount { get; private set; } = 0;  // Сколько всего возвращено
        public int CurrentCount => _pool.Count;              // Сколько в пуле сейчас

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PrewarmPool();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            _pool.Clear();
            DelSystem.Reset();
            MeshPool.Reset(destroyCheckedOutMeshes: true);
        }

        // Создаём несколько тайлов заранее
        private void PrewarmPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var obj = CreateNewTile();
                obj.transform.SetParent(PoolContainer);
                //obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        private GameObject CreateNewTile()
        {
            var obj = Instantiate(tilePrefab);
            obj.name = "Tile (Pooled)";
            if (PoolContainer)
                obj.transform.SetParent(PoolContainer);
            CreatedCount++;
            return obj;
        }

        // Получаем тайл из пула
        public GameObject GetTile()
        {
            if (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                //obj.SetActive(true);
                return obj;
            }
            return CreateNewTile();
        }

        // Возвращаем тайл в пул
        public void ReleaseTile(GameObject tile)
        {
            if (tile == null) return;

            if (_pool.Count < maxPoolSize)
            {
                if (PoolContainer != null)
                    tile.transform.SetParent(PoolContainer, false);
                _pool.Enqueue(tile);
                ReturnedCount++;
            }
            else
            {
                Destroy(tile);
            }
        }

        // 🔹 Отладочный вывод статистики
        public string GetStats()
        {
            return $"[TilePool] InPool={CurrentCount}, Created={CreatedCount}, Returned={ReturnedCount}, Max={maxPoolSize}";
        }
    }
}
