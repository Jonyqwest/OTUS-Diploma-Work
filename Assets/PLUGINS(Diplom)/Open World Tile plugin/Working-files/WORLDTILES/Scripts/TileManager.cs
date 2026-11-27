using System.Threading.Tasks;

namespace WorldTile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class TileManager : MonoBehaviour
    { 
        [Header("Ссылки")]
        //public Transform cameraTransform;
        //public Camera worldCamera;
        public GameObject terrainTilePrefab;
        public Transform PoolContainer;

        [Header("Настройки")]
        [Range(1, 8)] public int rangeOfZoom = 8; // Количество ступеней зума
        [SerializeField] private int baseTileSize = 300;
        [SerializeField, Range(0, 3)] private int superZoomScale = 2;
        [SerializeField] private Material surfaceOverlayMaterial;
        [SerializeField] private Material paintOverlayMaterial;
        [SerializeField] private Material gpuGrassBladeMaterial;

        [Header("Отладка (Debug)")]
        [Tooltip("Использовать только базовый атлас/кэш высот (Low Elevation), пропуская загрузку детальных высот зума 15")]
        public bool useOnlyLowElevation = false;

        [Tooltip("Использовать только базовые текстуры низкого разрешения (Low Texture), пропуская загрузку спутниковых текстур зума 15")]
        public bool useOnlyLowTexture = false;

        public static TileManager Instance { get; private set; }
        public static bool UseOnlyLowElevation => Instance != null && Instance.useOnlyLowElevation;
        public static bool UseOnlyLowTexture => Instance != null && Instance.useOnlyLowTexture;
        public Material SurfaceOverlayMaterial => surfaceOverlayMaterial;
        public Material PaintOverlayMaterial => paintOverlayMaterial;
        public Material GpuGrassBladeMaterial => gpuGrassBladeMaterial;

        private int baseZoom => TileStat.BaseZoom;
        //private int LowZoom => TileStat.LowZoom;
        public Texture2D zaglushkaTexture;
        public Texture2D zaglushkaElevation;

        //private Dictionary<(int, int), GameObject> loadedTiles = new();
        private int lastCenterX = int.MinValue;
        private int lastCenterY = int.MinValue;
        private Vector2 lastOffset;

        //private Color32 zaglushkaColor;

        private HashSet<(int z, int x, int y)> currentTiles, futureTiles, centerTiles;
        private HashSet<(int z, int x, int y)> tileToRemove;
        private HashSet<(byte z, int x, int y)> visualTiles;
        private HashSet<ZXY> superTiles;

        private bool needVisibilityUpdate = false; // Флаг для пересчёта видимости если тайлы меняются
        private bool needRestructTiles = false; // Флаг для перестройки тайлов
        Quaternion lastCameraRotation = new();

        private string DebugText;//временный текст для отладки
        private string Log1;     //временный текст для отладки
        private string Log2;     //временный текст для отладки
        private string Log3;     //временный текст для отладки
        private GUIContent debugGuiContent;

         
        LowTilesInitializer lowTilesInitializer;
        
        //UI прогресс бар
        [SerializeField]
        private TileProgressUI progressUI;

        //управляющие классы (C#)
        NearTileSistem nearTileSistem; 

        private CancellationTokenSource lifecycleCts;
        private int lifecycleGeneration;
        private long waitGeneration;
        private bool hasStarted;
        private bool worldStarted;

        
        private void Awake()
        {
            Instance = this;
            TileStat.BaseTileSize = baseTileSize;
            TileStat.SuperZoomScale = (byte)superZoomScale;

            if (zaglushkaTexture) ComparePictures.InitZaglushkaTexture(zaglushkaTexture);
            else Debug.LogError("Заглушка не найдена, возможно будут проблемы с текстурами-пустышками");

            if (zaglushkaElevation) ComparePictures.InitZaglushkaTexture(zaglushkaElevation);
            else Debug.LogWarning("Нет заглушкi для высот, должно и так работать");
            
            nearTileSistem = new();
            debugGuiContent = new GUIContent();

        }
        
        private void OnEnable()
        {
            lifecycleCts?.Dispose();
            lifecycleCts = new CancellationTokenSource();
            lifecycleGeneration++;

            WorldEventHub.OnAllTileReady += OnAllTilesReady;
            WorldEventHub.OnAllTileCancel += OnAllTilesCancel;

            if (hasStarted && !worldStarted)
                InitializeWorldAsync(lifecycleGeneration, lifecycleCts.Token).Forget();
        }
        private void OnDisable()
        {
            WorldEventHub.OnAllTileReady -= OnAllTilesReady;
            WorldEventHub.OnAllTileCancel -= OnAllTilesCancel;
            if (lowTilesInitializer != null)
                lowTilesInitializer.TileComplite -= OnLowTileComplite;

            lifecycleGeneration++;
            waitGeneration++;
            lifecycleCts?.Cancel();

            CancellationTokenSource activeWait = waitCts;
            waitCts = null;
            activeWait?.Cancel();

            if (TilePoolManager.Instance == null || TilePoolManager.Instance.gameObject == gameObject)
            {
                MeshApplyScheduler.ClearQueue();
                DictStat.Init();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            lifecycleCts?.Dispose();
            lifecycleCts = null;
        }

        private void OnLowTileComplite() =>  WorldTileStart();

        private void Start()
        {
            hasStarted = true;
            if (!worldStarted && lifecycleCts != null)
                InitializeWorldAsync(lifecycleGeneration, lifecycleCts.Token).Forget();
        }

        private async UniTaskVoid InitializeWorldAsync(int generation, CancellationToken token)
        {
            lowTilesInitializer = new();
            try
            {
                await lowTilesInitializer.InitializeAsinc(token);
                if (token.IsCancellationRequested || generation != lifecycleGeneration || !isActiveAndEnabled)
                    return;

                if (!lowTilesInitializer.IsTileComplite)
                {
                    Debug.LogError("LowTiles initialization failed; world startup was stopped.");
                    return;
                }

                worldStarted = true;
                OnLowTileComplite();
            }
            catch (OperationCanceledException)
            {
                // Normal when this manager is disabled or replaced.
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TileManager] World initialization failed: {exception}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            TileStat.BaseTileSize = baseTileSize;
            TileStat.SuperZoomScale = (byte)superZoomScale;
        }
#endif

        private void WorldTileStart()
        {
            WorldPosition playerWP = PlayerWorldStat.GetCurrentWorldPosition();
           
            currentTiles = new();
            tileToRemove = new();
            visualTiles  = new();
            centerTiles = Mas.GetGlobalTiles(playerWP, rangeOfZoom);//получаем HashSet списоk всех тайлов для центра 
            currentTiles.UnionWith(centerTiles);
            futureTiles = GetPercentTiles(playerWP);
            currentTiles.UnionWith(futureTiles);//добавляем список от потенциальных центров

            

            AddDictAndSpawnTiles(currentTiles);//добавляем в словарь и спавним tiles

            Log1 = "Current Tiles: " + currentTiles.Count.ToString() + "\n";
            RefreshDebugText();

            needRestructTiles = true;
            //WorldEventHub.SayAllTileCancel();
        }

        void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GUI.Label(new Rect(10, 10, 700, 400), debugGuiContent);
#endif
        }



        private void LateUpdate()
        {
            MeshApplyScheduler.Updater();
            ColliderApplyScheduler.Updater();
            if (currentTiles != null)
                DelSystem.Updater__Cleanup(currentTiles);

            if (progressUI != null)
                progressUI.UpdateProgress(progressTile);

            //грузим супертекстуры
            nearTileSistem?.Updater();

            //перестройка мира
            if ((needRestructTiles || Input.GetKeyDown(KeyCode.R)) && centerTiles != null)
            {
                _ = WaitAllTilesReadyAsync(centerTiles);
                needRestructTiles = false;
            }

        }

        private void RefreshDebugText()
        {
            DebugText = string.Concat(Log1, Log2, Log3);
            if (debugGuiContent != null)
                debugGuiContent.text = DebugText;
        }


      
        void Update()
        {
            WorldPosition playerWP = PlayerWorldStat.GetCurrentWorldPosition();

            //if (playerWP)
            //если камера повернулась или пошевелилась
            //if (lastCenterX != playerWP.tileX ||
            //    lastCenterY != playerWP.tileY ||
            //    lastOffset != playerWP.localOffset ||
            //    lastCameraRotation != Camera.main.transform.rotation)
            //{
            //    //if (visualTiles.Count > 0)
            //    //{ 
            //    //    foreach (var key in visualTiles)
            //    //    {
            //    //        if (DictStat.dictTiles.TryGetTerrain(key, out var terrain))
            //    //        {
            //    //            var wp = new WorldPosition(key);
            //    //            if (IsPointVisible(wp.ToUnityPosition()))
            //    //                terrain.TrySetVisible(true);
            //    //            else
            //    //                terrain.TrySetVisible(false);
            //    //        }
            //    //    }
            //    //    lastCameraRotation = Camera.main.transform.rotation;
            //    //}
            //}
            //если позиция пошевелилась
            if (lastCenterX != playerWP.tileX ||
                lastCenterY != playerWP.tileY ||
                lastOffset != playerWP.localOffset)
            {
                //если центральный тайл поменялся
                if (lastCenterX != playerWP.tileX ||
                    lastCenterY != playerWP.tileY)
                {
                    //пересчитаем центровые тайлы
                    centerTiles = Mas.GetGlobalTiles(playerWP, rangeOfZoom);//получаем HashSet списоk всех тайлов для центра

                    needRestructTiles = true;
                }
                else
                {
                    futureTiles = GetPercentTiles(playerWP); //заполняем тайлы будующих центров по мере приближения к ним

                    //формируем новые текущие тайлы вместе с тайлами будущего
                    currentTiles.Clear();
                    currentTiles.UnionWith(futureTiles);
                    currentTiles.UnionWith(centerTiles);

                    AddDictAndSpawnTiles(currentTiles);//добавляем в словарь и спавним новые тайлы 
                }
                Log1 = "видимых тайлов(centerTiles): " + centerTiles.Count.ToString() + "\n";
                Log3 = "Всего тайлов в словаре: " + DictStat.dictTiles.Count.ToString();
                RefreshDebugText();

                lastCenterX = playerWP.tileX;
                lastCenterY = playerWP.tileY;
                lastOffset = playerWP.localOffset;
            }
        }



        bool IsPointVisible(Vector3 worldPos)
        {
           
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPos);

            // Если точка позади камеры — сразу false
            if (viewportPos.z < 0) return false;

            // Проверка попадания в окно камеры (0..1 по X и Y)
            return viewportPos.x >= 0 && viewportPos.x <= 1 &&
                   viewportPos.y >= 0 && viewportPos.y <= 1;
        }


        public HashSet<(int z, int x, int y)> GetPercentTiles(WorldPosition worldPosition)
        {
            WorldPosition wp = worldPosition.ConvertNewZoom(baseZoom);
            var _tileSize = wp.TileSize();
            Vector3 camUnPos = worldPosition.ToUnityPosition();

            //получаем ключи всех тайлов в сентре
            var _currentGlobalKeys = Mas.GetGlobalTiles(wp, rangeOfZoom);

            HashSet<(int, int)> currentTiles = new();
            //Plane[] planes = GeometryUtility.CalculateFrustumPlanes(worldCamera);

            //int maxTileX = 1 << baseZoom; // число тайлов по X на данном зуме (для замкнутости)


            //список расстояний до ближайших тайлов 
            Dictionary<(int x, int y), float> _dic = new();

            //дистанция старта подгрузки
            float _start_dist = _tileSize * 1.5f;//коэффициент от 1 до 1.5
                                                 //максимально допустимое расстояние до следующего тайла при котором он может рассмастриватся как потенциально возможные новый центр
            float _maxSqrdist = _start_dist * _start_dist;
            //длина диагонали тайла
            float _diag = _tileSize * 0.5f * 1.42f;//(~ cos A)
                                                   //минимальная дистанция до следующего центр-тайла на которой все необходимые тайлы уже должны быть подгружуны
            float _limit_dist = _diag * 1.1f; //немного отодвинем передел от границы чтобы диагональные тайлы успели прогрузится

            //перебираем все точки вокруг 
            foreach (Vector3 point in Mas.spiralOffeset1)
            {
                if (point.z == 0) continue;// пропускает центральный тайл

                int _tileX = wp.tileX + (int)point.x;
                int _tileY = wp.tileY + (int)point.y;
                var _wp = new WorldPosition((byte)baseZoom, _tileX, _tileY); //WP-тайлы вокруг центра

                var _camPos = new Vector2(camUnPos.x, camUnPos.z);


                if ((_camPos - _wp.UnityTileCenter()).sqrMagnitude < _maxSqrdist)
                {
                    //расчет расстояние до соседа
                    float _dist = (_camPos - _wp.UnityTileCenter()).magnitude;
                    // Нормализация
                    float _norm = Mathf.Clamp01(Mathf.InverseLerp(_start_dist, _limit_dist, _dist));

                    //запоминаем координаты и прогресс
                    _dic[(_tileX, _tileY)] = _norm;
                }
            }

            // Перебор словаря от меньшего к большему
            // рассматриваем каждый потенциальный центр


            HashSet<(int z, int x, int y)> resZXY = new();

            foreach (var kvp in _dic)
            {

                var key = kvp.Key;     // (int x, int y)
                var progress = kvp.Value; // float
                                          //получаем выборку тайлов для нового Центра
                var _newGlobalKeys = Mas.GetGlobalTiles(key.x, key.y, rangeOfZoom);

                //исключаем те что уже есть в словаре
                HashSet<(int z, int x, int y)> newActualFutureKey = new();

                foreach (var _key in _newGlobalKeys)
                {
                    if (!_currentGlobalKeys.Contains(_key))
                    {
                        newActualFutureKey.Add(_key);
                    }
                }

                if (newActualFutureKey.Count <= 0) continue; //нет новых тайлов для подгрузки? => пропускаем

                float stepProgress = 1 / (float)newActualFutureKey.Count;

                HashSet<(int z, int x, int y)> progressZXY = new();
                float sumP = 0;
                foreach (var _key in newActualFutureKey /*.OrderBy(k => k.z)*/)
                {
                    sumP += stepProgress;
                    progressZXY.Add(_key);

                    if (progress < sumP) break;
                };
                resZXY.UnionWith(progressZXY);

            }

            return resZXY;

        }

        public void AddDictAndSpawnTiles(int zoom, int tileX, int tileY)
        {
            HashSet<(int z, int x, int y)> tile = new();
            tile.Add((zoom, tileX, tileY));
            AddDictAndSpawnTiles(tile);
        }
        public void AddDictAndSpawnTiles((int z, int x, int y) key)
        {
            HashSet<(int z, int x, int y)> tile = new();
            tile.Add(key);
            AddDictAndSpawnTiles(tile);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tiles">Маасим ключей</param>
        public void AddDictAndSpawnTiles(HashSet<(int z, int x, int y)> tilesKey)
        {
            //первым делом создаем стартовый тайл
            var pwp = PlayerWorldStat.GetCurrentWorldPosition();
            var zxy = pwp.ToZXY();
            if (!DictStat.dictTiles.ContainsKey((zxy.z, zxy.x, zxy.y)))
                CreateTiles(new WorldPosition((byte)zxy.z, zxy.x, zxy.y));

            //потом добавляем в словарь и создае тайлы

            foreach (var key in tilesKey)
            {
                if (DictStat.dictTiles.ContainsKey(key))
                    continue;   //если есть тайл в словаре --> следующий

                //действие с новыми записями
                var _wp = new WorldPosition((byte)key.z, key.x, key.y);
                CreateTiles(_wp);
            }
        }


        public void CreateTiles(WorldPosition worldPosition )
        {
          
            Vector3 _pos = worldPosition.ToUnityPosition();

            GameObject go = TilePoolManager.Instance.GetTile();
            
            go.transform.SetParent(transform);
            go.transform.position = _pos;
            go.transform.rotation = Quaternion.identity;

            go.tag = "World";
            TerrainTile terrainTile = go.GetComponent<TerrainTile>();
            
            if (terrainTile != null)
            {
                ///////!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\\\\\\
                terrainTile.Init(worldPosition.tileX, worldPosition.tileY, worldPosition.zoom, PoolContainer);

                //Создаём и инициализируем тайл
                var _tileObject = new TileObject(worldPosition, terrainTile, go);
                //добавляем в основной словарь
                DictStat.dictTiles.AddOrReplace(_tileObject);
            }
            else
            {
                Debug.LogError("TileManager: prefab должен содержать компонент TerrainTile");
            }
            //записываем в словарь тайл

           
        }







        public float progressTile;


        /// <summary>
        /// Ждем пока у всех тайлов из множества будет статус Ready 
        /// </summary>
        /// <param name="tilesKey">HashSet Ключи тайлов</param>
        /// <returns></returns>
        private CancellationTokenSource waitCts;
        public async UniTask WaitAllTilesReadyAsync(HashSet<(int z, int x, int y)> tilesKey)
        {
                if (tilesKey == null || lifecycleCts == null || lifecycleCts.IsCancellationRequested)
                    return;

                long requestGeneration = ++waitGeneration;
                var requestKeys = new HashSet<(int z, int x, int y)>(tilesKey);

                waitCts?.Cancel();
                CancellationTokenSource currentWait =
                    CancellationTokenSource.CreateLinkedTokenSource(lifecycleCts.Token);
                waitCts = currentWait;
                CancellationToken waitToken = currentWait.Token;

                List<TerrainTile> readyTiles = new();
                foreach (var key in requestKeys)
                {
                    if (DictStat.dictTiles.TryGetValue(key, out var tile) &&
                        tile?.terrainObject != null)
                        readyTiles.Add(tile.terrainObject);
                }

                Debug.Log($"✅ Ждём {readyTiles.Count} тайлов");

                // защитимся — если UI не прикреплён, попробуем найти его
                if (progressUI == null)
                    progressUI = FindObjectOfType<TileProgressUI>();

                try
                {
                    if (readyTiles.Count != requestKeys.Count)
                    {
                        Debug.LogWarning($"[TileManager] Only {readyTiles.Count}/{requestKeys.Count} requested tiles exist; retrying.");
                        // The requested snapshot is authoritative. Repair missing
                        // entries instead of silently treating them as ready.
                        AddDictAndSpawnTiles(requestKeys);
                        await UniTask.Yield();
                        if (requestGeneration == waitGeneration && !waitToken.IsCancellationRequested)
                            needRestructTiles = true;
                        return;
                    }

                    var result = await TileWaiter.WaitAllReadyAsync(
                        readyTiles,
                        waitToken,
                        p =>
                        {
                            if (requestGeneration == waitGeneration && !waitToken.IsCancellationRequested)
                                progressTile = p;
                        });

                    if (waitToken.IsCancellationRequested || requestGeneration != waitGeneration)
                        return;

                    visualTiles = result;
                    needVisibilityUpdate = true;
                    WorldEventHub.SayAllTileReady();
                    ApplyVisibility(requestKeys);
                    Debug.Log("✅ Все тайлы готовы!");
                }
                catch (OperationCanceledException)
                {
                    // A superseded request must not broadcast OnAllTileCancel:
                    // this manager subscribes to it and used to restart forever.
                    if (requestGeneration == waitGeneration && isActiveAndEnabled)
                        Debug.Log("⏹ Ожидание тайлов отменено");
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[TileManager] Tile readiness failed: {exception}");
                    if (requestGeneration == waitGeneration && !waitToken.IsCancellationRequested && isActiveAndEnabled)
                        needRestructTiles = true;
                }
                finally
                {
                    if (ReferenceEquals(waitCts, currentWait))
                        waitCts = null;

                    currentWait.Dispose();
                }

            }

        private void ApplyVisibility(HashSet<(int z, int x, int y)> requestKeys)
        {
            if (!needVisibilityUpdate || requestKeys == null)
                return;

            const float daley = 0.4f;
            foreach (var item in DictStat.dictTiles)
            {
                if (DictStat.dictTiles.TryGetTerrain(item.Key, out var terrain))
                    terrain.TrySetVisible(requestKeys.Contains(item.Key), daley);
            }

            if (currentTiles != null)
                tileToRemove = DelSystem.DelTilesActivator(currentTiles);
            needVisibilityUpdate = false;
        }




        // Вызывается когда ожидание обновленных тайлов принудительно сброшено
        public void OnAllTilesCancel()
        {
            //обновление всех тайлов сброшено принудительно -> запускаем новое ожидание
            needRestructTiles = true;
        }



        // Вызывается когда ожидание обновленных тайлов завершено
        public void OnAllTilesReady()
        {
            needVisibilityUpdate = true;
        }

        
    }
}
