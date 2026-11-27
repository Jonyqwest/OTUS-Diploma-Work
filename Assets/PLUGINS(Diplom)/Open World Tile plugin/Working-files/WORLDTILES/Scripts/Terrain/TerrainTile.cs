namespace WorldTile
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.Rendering;
    using Cysharp.Threading.Tasks; // ← добавлено для UniTask
    using System.Collections.Generic;
    using System.Collections;




    //[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainTile : MonoBehaviour
    {
        [SerializeField] public Texture2D zaglushkaTexture;
        [SerializeField] public Texture2D zaglushkaElevation;

        public TileStateMachine State = new();

        [SerializeField]
        public WorldPosition worldPosition;

        [SerializeField] private Texture textureMap;
        [SerializeField] private Texture2D elivationMap;

        [SerializeField]
        private Texture lowTextureMap;
        [SerializeField]
        private Texture lowElevationMap;

        [SerializeField]
        private Texture superTextureMap;

        //[SerializeField]
        //public GameObject terrain;
        private int zoom => worldPosition.zoom;
        private float tileSize => TileStat.BaseTileSize * 1 << (TileStat.BaseZoom - zoom);
        private bool isSupertexture = false;
        public bool HasSuperTexture => isSupertexture;
        private byte SuperTextureQuality => TileStat.SuperZoomScale;

        [SerializeField]
        private GameObject terrain;
        private Transform PoolContainer;
        private Mesh  mM;
        private MeshFilter mF;
        private MeshRenderer mR;
        private MeshCollider mC;
        private MaterialPropertyBlock _terrainProperties;
        private Mesh _colliderSourceMesh;
        private bool _colliderBuildQueued;
        private int _colliderBuildRequestId;

        private static readonly int LowTextureId = Shader.PropertyToID("_LowTex");
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int SuperTextureId = Shader.PropertyToID("_SuperTex");
        private static readonly int MainBlendStartTimeId = Shader.PropertyToID("_MainBlendStartTime");
        private static readonly int MainBlendDirectionId = Shader.PropertyToID("_MainBlendDirection");
        private static readonly int BlendStartTimeId = Shader.PropertyToID("_BlendStartTime");
        private static readonly int BlendDirectionId = Shader.PropertyToID("_BlendDirection");
        private static readonly ITextureProvider SatelliteTextureProvider = new TextureServerArcGIS();
        private static readonly ITextureProvider ElevationTextureProvider = new TextureServerAmazonaws();
        private static readonly ITextureProvider TextureCacheProvider = new TextureChach();
        private static readonly ITextureProvider LowTextureCacheProvider = new LowTextureCache();
        private static readonly IAsyncLoader<Texture2D> UrlTextureLoader = new TextureLoaderFromURL();
        private static readonly IAsyncLoader<Texture2D> CacheTextureLoader = new TextureLoaderFromCache();
        private static readonly IAsyncLoader<Texture2D> LowTextureLoader = new LowTexLoaderFromCache();
        private static readonly List<Vector3> NeighborVertexBuffer = new List<Vector3>();

        private int generationId; // внутренний счётчик "поколения"
        public int GenerationId => generationId;
        private int superTextureRevision;
        private bool _resourcesReleased = true;
        private bool _isReleasingResources;
        //private int currentGeneration; // копия для async-проверок
        private CancellationTokenSource tileCts;
        private CancellationTokenSource superTexCts;

        public UniTaskCompletionSource _readyTcs;

        [SerializeField]
        private ComputeShader mergeShader; //шейдер для GPU склейки

        //[SerializeField]
        private bool isVisible = false;
        public bool IsVisible => isVisible;

        private GameObject _surfaceOverlay;
        private MeshFilter _surfaceOverlayMeshFilter;
        private MeshRenderer _surfaceOverlayRenderer;
        private GpuGrassRenderer _gpuGrassRenderer;
        private MaterialPropertyBlock _surfaceOverlayProperties;

        private const string SurfaceOverlayShaderName = "Custom/TerrainOverlays/TerrainSurfaceOverlay";
        private static Shader _surfaceOverlayShader;
        private static Material _sharedSurfaceOverlayMaterial;

        private GameObject _paintOverlay;
        private MeshFilter _paintOverlayMeshFilter;
        private MeshRenderer _paintOverlayRenderer;
        private MaterialPropertyBlock _paintOverlayProperties;
        private Texture2D _paintMaskTex;

        private const string PaintOverlayShaderName = "Custom/TerrainOverlays/CustomPaintOverlay";
        private static Shader _paintOverlayShader;
        private static Material _sharedPaintOverlayMaterial;
        private static readonly int PaintTexId = Shader.PropertyToID("_PaintTex");
        private static readonly int PathTexId = Shader.PropertyToID("_PathTex");
        private static readonly int GrassTexId = Shader.PropertyToID("_GrassTex");
        private static readonly int DirtTexId = Shader.PropertyToID("_DirtTex");
        private static readonly int WaterTexId = Shader.PropertyToID("_WaterTex");
        private static readonly int TileSizeId = Shader.PropertyToID("_TileSize");

        public class ParentSearchResult
        {
            public Texture2D OutputTex;
            public float[,] OutputHeight;
            public int OutputZoom;
            public int OutputX;
            public int OutputY;
            public int requiredZoom;
            public int requiredX;
            public int requiredY;
        }


        void Awake()
        {
            terrain = transform.GetChild(0).gameObject;
            mF = terrain.GetComponent<MeshFilter>();
            mR = terrain.GetComponent<MeshRenderer>();
            if (mR != null)
            {
                mR.shadowCastingMode = ShadowCastingMode.Off;
                mR.receiveShadows = true;
                _terrainProperties = new MaterialPropertyBlock();
                mR.GetPropertyBlock(_terrainProperties);
            }
            mC = terrain.GetComponent<MeshCollider>();
            if (mC == null)
                mC = terrain.AddComponent<MeshCollider>();
            mC.sharedMesh = null;
            mC.convex = false;  
        }
        



        // 📌 =======  ИНИТ ===========
        public void Init(int x, int y, int zoom, Transform poolContainer = null)
        {
            generationId++;
            _resourcesReleased = false;
            _isReleasingResources = false;
            this.worldPosition = new WorldPosition((byte)zoom, x, y);
            PoolContainer = poolContainer;
            TrySetVisible(false);

            //Устанавливает стат Инициализация
            State.ChangeState(this, new TileState_Initializing());
        }


        // 📌 =======  СОЗДАНИЕ ТАЙЛА ===========
        public void CreateTile()
        {
            tileCts?.Dispose();
            tileCts = new CancellationTokenSource();
            Vector3 _tilPos = worldPosition.ToUnityPosition();

            if (TileStat.isCompensationPosition)
            {
                var wp = worldPosition;
                wp.localOffset = new Vector2(.5f, .5f);
                TileStat.CompensatePosition(wp.ToUnityPosition());

                _tilPos = worldPosition.ToUnityPosition();
            };

            
            transform.position = _tilPos;
            if(PoolContainer)
                transform.SetParent(PoolContainer);

            


            //_ = LoadLowTextureAsync();
            //_ = LoadTextureAsync(tileCts.Token);
            //_ = InitMeshAsync(tileCts.Token);
        }
        

        // 📌 ======== получение LOW TEXTURE  =========
        public async UniTask LoadLowTextureAsync()
        {
            int operationGeneration = generationId;
            CancellationToken token = tileCts.Token;
            byte lowZoom = TileStat.LowTextureZoom;
            //ITextureProvider textureServer = new TextureServerArcGIS();
            ITextureProvider lowTexturePath = LowTextureCacheProvider;

            //IAsyncLoader<Texture2D> asyncServerLoader = new TextureLoaderFromURL();
            IAsyncLoader<Texture2D> asyncCachLoader = LowTextureLoader;

            var lowWp = worldPosition.ConvertNewZoom(lowZoom);
            ZXY key = lowWp.ToZXY();
            Texture tex = await asyncCachLoader.LoadAsync(lowTexturePath.GetTexturePath(key), token);
            if (tex == null)
                return;
            Texture texture = null;
            try
            {
                if (token.IsCancellationRequested || operationGeneration != generationId)
                    return;

                //теперь у нас есть текстура нам нужно вернуть в итоге только ее часть
                texture = TextureCropperUtilite.GetTextureZoomCroped(tex, lowZoom, worldPosition);
                if (texture == null)
                    Debug.LogWarning("Ошибка загрузки LOW!!!");

                if (texture != tex)
                    ReleaseRuntimeTexture(tex);
                tex = null;

                await UniTask.SwitchToMainThread();
                if (token.IsCancellationRequested || operationGeneration != generationId)
                    return;

                lowTextureMap = texture;
                texture = null; // ownership is transferred to the tile
                if (mR != null)
                {
                    _terrainProperties.SetTexture(LowTextureId, lowTextureMap);
                    mR.SetPropertyBlock(_terrainProperties);
                }
            }
            finally
            {
                ReleaseRuntimeTexture(tex);
                ReleaseRuntimeTexture(texture);
            }
        }



        // 📌 ======== Create ---- LOW ELEVATION --- Mesh  ========= //
        public async UniTask<float[,]> LoadLowElevation()
        {
            byte lowZoom = TileStat.LowElevationZoom;
            ITextureProvider lowElevationServer = ElevationTextureProvider;
            //ITextureProvider lowTexturePath = new LowTextureCache();

            IAsyncLoader<Texture2D> asyncServerLoader = UrlTextureLoader;
            //IAsyncLoader<Texture2D> asyncCachLoader = new LowTexLoaderFromCache();

            var lowWp = worldPosition.ConvertNewZoom(lowZoom);
            ZXY key = lowWp.ToZXY();
            Texture tex = await asyncServerLoader.LoadAsync(lowElevationServer.GetTexturePath(key), tileCts.Token);

            if (tex != null)
            {
                Debug.Log("[LoadLowElevation] получили LOW tex.width = " + tex.width);

                //петерь надо ее теранить и кропать под нужный зум

                lowElevationMap = tex;
                float[,] lowHeightMap = DecodeTerrarium(tex as Texture2D);
                var heightMap = ExtractChildTile(lowHeightMap, lowZoom, worldPosition.zoom, worldPosition.tileX, worldPosition.tileY, true);
                int width = heightMap.GetLength(0);

                var md = await Task.Run(() => MeshGenerator.GenerateMeshData(heightMap, tileSize, width, TileStat.ElevationScale));
                await QueueApplyMesh(md, worldPosition.tileX, worldPosition.tileY, worldPosition.zoom, cached: false); // ⏳ теперь ждём применения меша

                return heightMap;
            }
            else {
                Debug.Log("[LoadLowElevation] LOW Elevation Texture Z"+ key.z + "(" + key.x + "/" + key.y + ") not found");
                return null;
            }
        }


        // 📌 ======== получение TEXTURE TILE =========
        public async UniTask LoadTextureAsync()
        {
            int operationGeneration = generationId;
            CancellationToken token = tileCts.Token;
            // Если отмена — сразу выходим
            token.ThrowIfCancellationRequested();

            int baseX = worldPosition.tileX;
            int baseY = worldPosition.tileY;
            int maxTileX = 1 << zoom;
            baseX = Mod(baseX, maxTileX);

            // Режим отладки: использовать только low текстуры
            if (TileManager.UseOnlyLowTexture)
            {
                // Ожидаем готовности lowTextureMap (загружаемой параллельно в TileState_Low)
                int waitCount = 0;
                while (lowTextureMap == null && waitCount < 40 && !token.IsCancellationRequested)
                {
                    await UniTask.Delay(50, cancellationToken: token);
                    waitCount++;
                }

                if (token.IsCancellationRequested || operationGeneration != generationId)
                    return;

                if (lowTextureMap != null && mR != null && mR.sharedMaterial != null)
                {
                    _terrainProperties.SetTexture(MainTextureId, lowTextureMap);
                    _terrainProperties.SetFloat(MainBlendStartTimeId, Time.time);
                    _terrainProperties.SetFloat(MainBlendDirectionId, 1.0f);
                    mR.SetPropertyBlock(_terrainProperties);
                    CreateOrUpdateSurfaceOverlay(lowTextureMap);
                }

                if (DictStat.dictTiles.ContainsKey(worldPosition.GetDictIndex()))
                {
                    DictStat.dictTiles.SetTextureReady(worldPosition.GetDictIndex(), true);
                }
                else
                {
                    var _tileObject = new TileObject(worldPosition, this, gameObject: gameObject, isTextureReady: true);
                    DictStat.dictTiles.AddOrReplace(_tileObject);
                    DictStat.dictTiles.SetTextureReady(worldPosition.GetDictIndex(), true);
                }
                return;
            }

            // Ищем картинку в кеше в нужном зуме или выше 
            var parentResult = await FindTileTextureAsync(
                baseX,
                baseY,
                zoom,
                isElevationTexture: false,
                token: token);

            if (parentResult != null && parentResult.OutputTex != null)
            {
                Texture parentTexture = parentResult.OutputTex;
                parentResult.OutputTex = null;
                Texture cropped = null;
                try
                {
                    if (token.IsCancellationRequested || operationGeneration != generationId)
                        return;

                    cropped = TextureCropperUtilite.GetTextureZoomCroped(
                        parentTexture, parentResult.OutputZoom, worldPosition);
                    if (cropped != parentTexture)
                        ReleaseRuntimeTexture(parentTexture);
                    parentTexture = null;

                    if (token.IsCancellationRequested || operationGeneration != generationId)
                        return;

                    textureMap = cropped;
                    cropped = null; // ownership is transferred to the tile

                    if (mR != null && mR.sharedMaterial != null)
                    {
                        _terrainProperties.SetTexture(MainTextureId, textureMap);
                        _terrainProperties.SetFloat(MainBlendStartTimeId, Time.time);
                        _terrainProperties.SetFloat(MainBlendDirectionId, 1.0f);
                        mR.SetPropertyBlock(_terrainProperties);
                        CreateOrUpdateSurfaceOverlay(textureMap);

                        var paintMask = CustomDeltaApplier.LoadPaintMask(worldPosition.tileX, worldPosition.tileY, zoom);
                        if (paintMask != null)
                            SetPaintMask(paintMask);

                        //Полу-гвоздь но что поделать
                        if (DictStat.dictTiles.ContainsKey(worldPosition.GetDictIndex()))
                        {
                            //если в словаре есть запись. указываем что текстура готова
                            DictStat.dictTiles.SetTextureReady(worldPosition.GetDictIndex(), true);
                        }
                        else
                        {
                            //если в словаре нет записи. Создаем новую запись tile wp 
                            var _tileObject = new TileObject(worldPosition, this, gameObject: gameObject, isTextureReady: true);
                            //добавляем в основной словарь
                            DictStat.dictTiles.AddOrReplace(_tileObject);
                            DictStat.dictTiles.SetTextureReady(worldPosition.GetDictIndex(), true);
                        }
                        //Debug.Log("DictStat.dictTiles.SetTextureReady = " + DictStat.dictTiles.GetValue(worldPosition.GetDictIndex()).isTextureReady);
                    }
                    return;
                }
                finally
                {
                    ReleaseRuntimeTexture(parentTexture);
                    ReleaseRuntimeTexture(cropped);
                }
            }

            // Fallback: если спутниковую текстуру высокого разрешения не удалось скачать,
            // используем lowTextureMap (базовую текстуру), чтобы тайл не зависал и отображался
            if (lowTextureMap != null && mR != null && mR.sharedMaterial != null)
            {
                _terrainProperties.SetTexture(MainTextureId, lowTextureMap);
                _terrainProperties.SetFloat(MainBlendStartTimeId, Time.time);
                _terrainProperties.SetFloat(MainBlendDirectionId, 1.0f);
                mR.SetPropertyBlock(_terrainProperties);
                CreateOrUpdateSurfaceOverlay(lowTextureMap);
            }

            if (DictStat.dictTiles.ContainsKey(worldPosition.GetDictIndex()))
            {
                DictStat.dictTiles.SetTextureReady(worldPosition.GetDictIndex(), true);
            }
            else
            {
                var _tileObject = new TileObject(worldPosition, this, gameObject: gameObject, isTextureReady: true);
                DictStat.dictTiles.AddOrReplace(_tileObject);
                DictStat.dictTiles.SetTextureReady(worldPosition.GetDictIndex(), true);
            }

            Debug.LogWarning("Z" + zoom + "[" + baseX + "/" + baseY + "] TEXTURE not found in network/cache! Fallback to low texture applied.");
        }

        



        // 📌 ======== создание SUPER TEXTURE ============
        public async UniTask CreateSuperTextureAsync()
        {
            if (SuperTextureQuality <= 0 || TileManager.UseOnlyLowTexture) return;
            if (isSupertexture) return;

            int operationGeneration = generationId;
            // Invalidates a delayed fade-out belonging to the previous super texture.
            superTextureRevision++;
            superTexCts?.Cancel();
            superTexCts?.Dispose();
            superTexCts = new CancellationTokenSource();
            CancellationToken token = superTexCts.Token;

            Texture2D baseTex = TextureUtilite.ToTexture2D(textureMap);
            if (baseTex == null)
                baseTex = TextureUtilite.ToTexture2D(lowTextureMap);

            bool baseTexIsNew = baseTex != null && baseTex != textureMap && baseTex != lowTextureMap;
            try
            {
                for (int quality = SuperTextureQuality; quality >= 1; quality--)
                {
                    int tileCount = 1 << quality;
                    int childCount = tileCount * tileCount;

                    List<WorldPosition> childList = worldPosition.GetChildsListRecursive(quality);
                    var results = new (int x, int y, Texture2D tex)[childCount];
                    var errors = new Exception[childCount];
                    UniTask[] tasks = new UniTask[childCount];

                    for (int i = 0; i < childCount; i++)
                        tasks[i] = LoadSuperTextureSlotAsync(
                            childList[i], quality, token, results, errors, i);

                    try
                    {
                        // Each task writes into its own slot. If one sibling throws,
                        // its wrapper records the exception and lets all other tasks
                        // settle before ownership cleanup begins.
                        await UniTask.WhenAll(tasks);

                        for (int i = 0; i < errors.Length; i++)
                        {
                            if (errors[i] != null)
                                throw errors[i];
                        }

                        if (token.IsCancellationRequested || operationGeneration != generationId)
                            return;

                        var texList = new List<(int x, int y, Texture2D tex)>(childCount);
                        for (int i = 0; i < results.Length; i++)
                        {
                            if (results[i].tex != null)
                                texList.Add(results[i]);
                        }

                        if (texList.Count == 0)
                            continue;

                        Texture mergedTexture = TextureUtilite.ShaderMergeTextures(
                            texList,
                            tileCount,
                            mergeShader,
                            baseTex);

                        if (mergedTexture == null)
                            continue;

                        try
                        {
                            if (token.IsCancellationRequested || operationGeneration != generationId)
                                return;

                            _terrainProperties.SetTexture(SuperTextureId, mergedTexture);
                            _terrainProperties.SetFloat(BlendStartTimeId, Time.time);
                            _terrainProperties.SetFloat(BlendDirectionId, 1.0f);
                            mR.SetPropertyBlock(_terrainProperties);

                            superTextureMap = mergedTexture;
                            mergedTexture = null; // ownership is transferred to the tile
                            isSupertexture = true;
                            return;
                        }
                        finally
                        {
                            ReleaseRuntimeTexture(mergedTexture);
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < results.Length; i++)
                        {
                            ReleaseRuntimeTexture(results[i].tex);
                            results[i].tex = null;
                        }
                    }
                }
            }
            finally
            {
                if (baseTexIsNew)
                    ReleaseRuntimeTexture(baseTex);
            }
        }



        // 📌 ============ получение высот ELEVATION  ===============
        public async UniTask<ParentSearchResult> LoadElevationTile( )
        {
            CancellationToken token = tileCts.Token;
            int x = worldPosition.tileX;
            int y = worldPosition.tileY;
            int zoom = worldPosition.zoom;

            // Если отмена — сразу выходим
            token.ThrowIfCancellationRequested();

            bool onlyLowElevation = TileManager.UseOnlyLowElevation;

            // 1. Попробовать загрузить готовый бинарный кэш (если не включен отладочный режим "только low")
            if (!onlyLowElevation)
            {
                float[,] cached = CacheTileUtility.LoadElevationDataCompressed(x, y, zoom);
                token.ThrowIfCancellationRequested();

                if (IsValidHeightmap(cached))
                {
                    cached = CustomDeltaApplier.Apply(cached, x, y, zoom);
                    return new ParentSearchResult
                    {
                        OutputHeight = cached,
                        OutputZoom = zoom,
                        requiredZoom = zoom,
                        requiredX = x,
                        requiredY = y,
                    };
                }
            }
            else
            {
                // Принудительно берем высоты из базового лоу-кэша / Атласа
                byte lowZoom = TileStat.LowElevationZoom;
                if (zoom >= lowZoom)
                {
                    int factor = 1 << (zoom - lowZoom);
                    int lowX = x / factor;
                    int lowY = y / factor;
                    int maxLow = 1 << lowZoom;
                    int tx = ((lowX % maxLow) + maxLow) % maxLow;
                    int ty = Mathf.Clamp(lowY, 0, maxLow - 1);

                    float[,] lowMap = GlobalElevation.LoadLowElevationTile(tx, ty, lowZoom);
                    if (lowMap != null)
                    {
                        return new ParentSearchResult
                        {
                            OutputHeight = lowMap,
                            OutputZoom = lowZoom,
                            requiredZoom = zoom,
                            requiredX = x,
                            requiredY = y,
                        };
                    }
                }
            }

            var parentResult = await FindTileTextureAsync(
                x,
                y,
                zoom,
                isElevationTexture: true,
                token: token);

            // Если не удалось найти высоты в сети/кэше родителей — берем fallback из GlobalElevation (зум 9 / Атлас)
            if (parentResult == null || (parentResult.OutputHeight == null && parentResult.OutputTex == null))
            {
                byte lowZoom = TileStat.LowElevationZoom;
                int factor = zoom >= lowZoom ? (1 << (zoom - lowZoom)) : 1;
                int lowX = zoom >= lowZoom ? (x / factor) : (x << (lowZoom - zoom));
                int lowY = zoom >= lowZoom ? (y / factor) : (y << (lowZoom - zoom));
                int maxLow = 1 << lowZoom;
                int tx = ((lowX % maxLow) + maxLow) % maxLow;
                int ty = Mathf.Clamp(lowY, 0, maxLow - 1);

                float[,] fallbackMap = GlobalElevation.LoadLowElevationTile(tx, ty, lowZoom);
                if (fallbackMap == null)
                {
                    // Если океан или нет данных — создаем плоскую карту (0 метров), чтобы не блокировать загрузку
                    int res = TileStat.ElevationTileResolution;
                    fallbackMap = new float[res, res];
                }

                return new ParentSearchResult
                {
                    OutputHeight = fallbackMap,
                    OutputZoom = lowZoom,
                    requiredZoom = zoom,
                    requiredX = x,
                    requiredY = y,
                };
            }

            float[,] parentHeightMap = null;

            if (parentResult.OutputHeight != null)
            {
                //если мы нашли бинарные высоты у родителя
                parentHeightMap = parentResult.OutputHeight;
            }
            else
            {
                if (parentResult.OutputTex != null)
                {
                    try
                    {
                        // если мы нашли текстуры высот у родителей делаем терариум получаем бинарные высоты
                        parentHeightMap = DecodeTerrarium(parentResult.OutputTex);

                        // Сохраняем как бинарный кэш
                        if (IsValidHeightmap(parentHeightMap))
                            CacheTileUtility.SaveElevationDataCompressed(parentHeightMap, parentResult.OutputX, parentResult.OutputY, parentResult.OutputZoom);
                    }
                    finally
                    {
                        ReleaseRuntimeTexture(parentResult.OutputTex);
                        parentResult.OutputTex = null;
                    }
                }
            }

            if (parentHeightMap == null)
            {
                byte lowZoom = TileStat.LowElevationZoom;
                int factor = zoom >= lowZoom ? (1 << (zoom - lowZoom)) : 1;
                int lowX = zoom >= lowZoom ? (x / factor) : (x << (lowZoom - zoom));
                int lowY = zoom >= lowZoom ? (y / factor) : (y << (lowZoom - zoom));
                int maxLow = 1 << lowZoom;
                int tx = ((lowX % maxLow) + maxLow) % maxLow;
                int ty = Mathf.Clamp(lowY, 0, maxLow - 1);

                parentHeightMap = GlobalElevation.LoadLowElevationTile(tx, ty, lowZoom) ?? new float[TileStat.ElevationTileResolution, TileStat.ElevationTileResolution];
                return new ParentSearchResult
                {
                    OutputHeight = parentHeightMap,
                    OutputZoom = lowZoom,
                    requiredZoom = zoom,
                    requiredX = x,
                    requiredY = y,
                };
            }

            //отправляем
            return new ParentSearchResult
            {
                OutputHeight = parentHeightMap,
                OutputZoom = parentResult.OutputZoom,
                requiredZoom = zoom,
                requiredX = x,
                requiredY = y,
            };
        }



        // 📌 ====== Делаем MESH из высот  ========
        public async UniTask<bool> InitMeshAsync()
        {
            CancellationToken token = tileCts.Token;
            int x = worldPosition.tileX;
            int y = worldPosition.tileY;
            int zoom = worldPosition.zoom;
            token.ThrowIfCancellationRequested();

            int maxTileX = 1 << zoom;
            x = Mod(x, maxTileX);

            var _res = await LoadElevationTile();
            if (_res == null) return false;
            int resolution = TileStat.ElevationTileResolution;
            float[,] heightMapData = _res.OutputHeight;
            if (heightMapData == null)
            {
                Debug.LogWarning("[TerrainTile] heightMapData null for " + x + "," + y);
                return false;
            }

            MeshData md;
            if (_res.OutputZoom < zoom)
            {
                float[,] heightMap = ExtractChildTile(_res.OutputHeight, _res.OutputZoom, zoom, x, y);
                md = await Task.Run(() => MeshGenerator.GenerateMeshData(heightMap, tileSize, resolution, TileStat.ElevationScale));
            }
            else
            {
                md = await Task.Run(() => MeshGenerator.GenerateMeshData(heightMapData, tileSize, resolution, TileStat.ElevationScale));
            }

            await QueueApplyMesh(md, x, y, zoom, cached: false); // ⏳ теперь ждём применения меша
            return true;
        }


        // 📌 ======  МЕШ КОЛАЙДЕР ===========
        public void AddColliderMatch()
        {
            //MeshFilter mf = Tile.GetComponent<MeshFilter>();
            if (mF != null && mF.sharedMesh != null)
            {
                Mesh requestedSourceMesh = mF.sharedMesh;
                if (_colliderBuildQueued ||
                    (mC != null && mC.sharedMesh != null && _colliderSourceMesh == requestedSourceMesh))
                    return;

                int queuedGeneration = generationId;
                int requestId = ++_colliderBuildRequestId;
                _colliderBuildQueued = true;
                //добавляем в очередь на создание колайдера
                ColliderApplyScheduler.Enqueue(this, () =>
                {
                    try
                    {
                        if (_resourcesReleased || queuedGeneration != generationId ||
                            requestId != _colliderBuildRequestId || mF == null ||
                            mF.sharedMesh == null || mF.sharedMesh != requestedSourceMesh)
                            return;

                        if (mC == null)
                            mC = gameObject.AddComponent<MeshCollider>();

                        Mesh newColliderMesh = null;
                        bool assigned = false;
                        try
                        {
                            newColliderMesh = Instantiate(requestedSourceMesh);
                            Mesh oldColliderMesh = mC.sharedMesh;

                            mC.sharedMesh = null;
                            mC.sharedMesh = newColliderMesh;
                            assigned = true;
                            mM = newColliderMesh;
                            _colliderSourceMesh = requestedSourceMesh;

                            if (oldColliderMesh != null &&
                                oldColliderMesh != newColliderMesh &&
                                oldColliderMesh != mF.sharedMesh)
                                Destroy(oldColliderMesh);
                        }
                        finally
                        {
                            if (!assigned && newColliderMesh != null)
                                Destroy(newColliderMesh);
                        }
                    }
                    finally
                    {
                        if (requestId == _colliderBuildRequestId)
                            _colliderBuildQueued = false;
                    }
                }, () =>
                {
                    if (requestId == _colliderBuildRequestId)
                        _colliderBuildQueued = false;
                });
                //mM = Instantiate(mF.sharedMesh);
                //mC.sharedMesh = mM;

            }
            else
            {
                Debug.LogWarning($"⚠️ Нет MeshFilter или меша на тайле");
            }
        }

        
        // 📌 ======  ПЛАНИРОВЩИ МЕШ ===========
        public UniTask QueueApplyMesh(MeshData md, int x, int y, int zoom, bool cached)
        {
            var tcs = new UniTaskCompletionSource();
            int queuedGeneration = generationId;

            MeshApplyScheduler.Enqueue(this, () =>
            {
                Mesh acquiredMesh = null;
                bool meshAssigned = false;
                try
                {
                    if (_resourcesReleased || queuedGeneration != generationId || mF == null || mF.gameObject == null)
                    {
                        tcs.TrySetResult(); // Завершить даже если ничего не применили
                        return;
                    }

                    var mesh = MeshPool.GetMesh();
                    acquiredMesh = mesh;
                    mesh.Clear();
                    // The grass compute shader reads the already uploaded terrain
                    // vertices and indices directly on the GPU. Raw targets must
                    // be requested before vertex/index data is assigned.
                    mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                    mesh.indexFormat = IndexFormat.UInt32;
                    WeldEdges(md.vertices, x, y, zoom);
                    mesh.vertices = md.vertices;
                    mesh.uv = md.uvs;
                    mesh.triangles = md.triangles;

                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();

                    var oldMesh = mF.sharedMesh;
                    mF.sharedMesh = mesh;
                    meshAssigned = true;

                    if (_surfaceOverlayMeshFilter != null)
                        _surfaceOverlayMeshFilter.sharedMesh = mesh;

                    // Ownership of the previous mesh ends as soon as every mesh
                    // filter has been switched. Release it before optional overlay
                    // configuration, which can throw independently.
                    if (oldMesh != null && oldMesh != mesh)
                        MeshPool.ReleaseMesh(oldMesh);

                    if (_surfaceOverlayMeshFilter != null)
                    {
                        _gpuGrassRenderer?.Configure(_surfaceOverlayMeshFilter, textureMap);
                    }
                    else if (textureMap != null)
                        CreateOrUpdateSurfaceOverlay(textureMap);

                    if (_paintOverlayMeshFilter != null)
                        _paintOverlayMeshFilter.sharedMesh = mF.sharedMesh;

                    DictStat.dictTiles.SetMeshReady((zoom, x, y), true);
                    WorldEventHub.SayTileReady(zoom, x, y);

                    transform.SetParent(transform.parent.parent);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    if (!meshAssigned && acquiredMesh != null)
                        MeshPool.ReleaseMesh(acquiredMesh);
                    Debug.LogError("[TerrainTile] Apply mesh error: " + ex);
                    tcs.TrySetException(ex);
                }
            }, () => tcs.TrySetCanceled());

            return tcs.Task;
        }


        private int Mod(int value, int modulo)
        {
            return ((value % modulo) + modulo) % modulo;
        }


        private void WeldEdges(Vector3[] verts, int x, int y, int zoom)
        {
            if (verts == null)
                return;

            int size = (int)Mathf.Sqrt(verts.Length);
            if (size * size != verts.Length) return;

            int maxTileX = 1 << zoom;
            TerrainTile neighbor;

            // East (x+1, y) — current i=size-1, neighbor i=0
            if (DictStat.dictTiles.TryGetTerrain((zoom, Mod(x + 1, maxTileX), y), out neighbor)
                && TryReadNeighborVertices(neighbor, verts.Length))
            {
                int nSize = size;
                for (int j = 0; j < size; j++)
                    verts[(size - 1) + j * size].y = NeighborVertexBuffer[j * nSize].y;
            }

            // West (x-1, y) — current i=0, neighbor i=size-1
            if (DictStat.dictTiles.TryGetTerrain((zoom, Mod(x - 1, maxTileX), y), out neighbor)
                && TryReadNeighborVertices(neighbor, verts.Length))
            {
                int nSize = size;
                for (int j = 0; j < size; j++)
                    verts[j * size].y = NeighborVertexBuffer[(nSize - 1) + j * nSize].y;
            }

            // North (x, y-1) — current j=size-1, neighbor j=0
            if (DictStat.dictTiles.TryGetTerrain((zoom, x, y - 1), out neighbor)
                && TryReadNeighborVertices(neighbor, verts.Length))
            {
                int nSize = size;
                for (int i = 0; i < size; i++)
                    verts[i + (size - 1) * size].y = NeighborVertexBuffer[i].y;
            }

            // South (x, y+1) — current j=0, neighbor j=size-1
            if (DictStat.dictTiles.TryGetTerrain((zoom, x, y + 1), out neighbor)
                && TryReadNeighborVertices(neighbor, verts.Length))
            {
                int nSize = size;
                for (int i = 0; i < size; i++)
                    verts[i].y = NeighborVertexBuffer[i + (nSize - 1) * nSize].y;
            }
        }

        private static bool TryReadNeighborVertices(TerrainTile neighbor, int expectedVertexCount)
        {
            Mesh neighborMesh = neighbor?.mF?.sharedMesh;
            if (neighborMesh == null || neighborMesh.vertexCount != expectedVertexCount)
                return false;

            NeighborVertexBuffer.Clear();
            neighborMesh.GetVertices(NeighborVertexBuffer);
            return NeighborVertexBuffer.Count == expectedVertexCount;
        }


        //Ищет текстуру на текущем зум и выше 
        private async UniTask<ParentSearchResult> FindTileTextureAsync(
        int _baseX, int _baseY, int _baseZoom, bool isElevationTexture,
        bool saveMetaForSiblings = true,
        CancellationToken token = default)
        {
            ITextureProvider textureServer = SatelliteTextureProvider;     //сервер текстур
            ITextureProvider elevationServer = ElevationTextureProvider;  //сервер высот

            int startZoom = _baseZoom;
            if (isElevationTexture && TileManager.UseOnlyLowElevation)
            {
                startZoom = Mathf.Min(_baseZoom, (int)TileStat.LowElevationZoom);
            }
            else if (!isElevationTexture && TileManager.UseOnlyLowTexture)
            {
                startZoom = Mathf.Min(_baseZoom, (int)TileStat.LowTextureZoom);
            }

            // Поднимаемся по уровням зума вверх
            for (int currentZoom = startZoom; currentZoom >= 0; currentZoom--)
            {
                token.ThrowIfCancellationRequested();

                int levelDiff = _baseZoom - currentZoom;
                int factor = 1 << levelDiff; // сколько _base-тайлов по оси в одном parent
                int currentX = Mathf.FloorToInt(_baseX / (float)factor);
                int currentY = Mathf.FloorToInt(_baseY / (float)factor);

                // 1) Попробуем взять текущий уровень из кэша
                Texture2D parentTex;
                if (isElevationTexture)
                {
                    parentTex = CacheTileUtility.LoadTileElevationFromCache(currentX, currentY, currentZoom);
                }
                else
                {
                    parentTex = CacheTileUtility.LoadTileTextureFromCache(currentX, currentY, currentZoom);
                } 
                // 2) Если не в кеше — скачать
                if (parentTex == null)
                {

                    string url;
                    if (isElevationTexture)
                    {
                        url = elevationServer.GetTexturePath(new ZXY((byte)currentZoom, currentX, currentY));
                        //Debug.Log("ElevationUrl: " + url);
                    }
                    else
                    {
                        url = textureServer.GetTexturePath(new ZXY((byte)currentZoom, currentX, currentY ));
                        //Debug.Log("TextureUrl: " + url);
                    }
                    if (!await TextureDownloadThrottle.EnterAsync(token))
                        return null;

                    try
                    {
                        await UniTask.SwitchToMainThread();
                        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
                        {
                            www.timeout = 7; // Защита от вечного зависания сети
                            var op = www.SendWebRequest();
                            while (!op.isDone)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    www.Abort();
                                    token.ThrowIfCancellationRequested();
                                }
                                await UniTask.Yield();
                            }

                            if (www.result == UnityWebRequest.Result.Success)
                            {
                                parentTex = DownloadHandlerTexture.GetContent(www);
                                try
                                {
                                    // 3) убедимся, что родитель не заглушка
                                    if (isElevationTexture)
                                    {
                                        //для высот
                                        if (ComparePictures.IsZaglushkaElevation(parentTex))
                                        {
                                            Texture2D placeholder = parentTex;
                                            parentTex = null;
                                            await TextureDownloadThrottle.DestroyAfterUploadAsync(placeholder);
                                            continue;
                                        }
                                        else
                                        {
                                            // сохраняем в PNG кеш
                                            CacheTileUtility.SaveTileElevationToCache(parentTex, currentX, currentY, currentZoom);
                                        }
                                    }
                                    else
                                    {
                                        //для тексиур
                                        if (ComparePictures.IsZaglushka(parentTex))
                                        {
                                            Texture2D placeholder = parentTex;
                                            parentTex = null;
                                            await TextureDownloadThrottle.DestroyAfterUploadAsync(placeholder);
                                            continue;
                                        }
                                        else
                                        {
                                            // кэшируем родителя (полноразмерный)
                                            ZXY key;
                                            key.x = currentX;
                                            key.y = currentY;
                                            key.z = (byte)currentZoom;
                                            CacheTileUtility.SaveTileTextureToCache(parentTex, key);
                                        }
                                    }
                                }
                                catch
                                {
                                    Texture2D failedTexture = parentTex;
                                    parentTex = null;
                                    await TextureDownloadThrottle.DestroyAfterUploadAsync(failedTexture);
                                    throw;
                                }
                            }
                            else
                            {
                                // не смогли скачать — пробуем следующий уровень выше
                                parentTex = null;
                            }
                        }
                    }
                    finally
                    {
                        TextureDownloadThrottle.Exit();
                    }
                }
                //если текстура не найдена делаем тоже самое на уровень выше
                if (parentTex == null)
                    continue;
                if (parentTex.width <= 0 || parentTex.height <= 0)
                {
                    ReleaseRuntimeTexture(parentTex);
                    parentTex = null;
                    continue;
                }

                // Placeholder detection and PNG caching are complete. Satellite
                // pixels are no longer read by the CPU, so retain only the GPU copy.
                // Elevation textures must stay readable for DecodeTerrarium().
                if (!isElevationTexture && parentTex.isReadable)
                {
                    try
                    {
                        parentTex.Apply(false, true);
                    }
                    catch
                    {
                        ReleaseRuntimeTexture(parentTex);
                        parentTex = null;
                        throw;
                    }
                }

                //4) делаем кроп нужной части

                //если нашли сразу возвращаем полный кадр
                if (_baseZoom == currentZoom)
                { 
                    return new ParentSearchResult
                    {
                        OutputTex = parentTex,
                        OutputZoom = currentZoom,
                        OutputX = currentX,
                        OutputY = currentY,
                        requiredZoom = _baseZoom,
                        requiredX = _baseX,
                        requiredY = _baseY,

                    };
                }
                // 5) У нас есть валидная текстура родителя полнокадровая 



                //8) проставляем мета для с�        //Проверка вершин на валидность
                
                
                //8) проставляем мета для соседей
                if (isElevationTexture)
                {//пока отлаживаю без меты
                    //CacheTileUtility.GenerateMetaForChildren(currentX, currentY, currentZoom, _baseZoom, CacheStat.HeightCacheFolder);
                    ////Сохраняем в кэш ребёнка (для отладки)
                    //CacheTileUtility.SaveTileElevationToCache(texAfterCrop, _baseX, _baseY, _baseZoom);

                }
                else
                {//пока отлаживаю без меты
                    //CacheTileUtility.GenerateMetaForChildren(currentX, currentY, currentZoom, _baseZoom, CacheStat.TextureCacheFolder);
                    ////Сохраняем в кэш ребёнка (для отладки)
                    //CacheTileUtility.SaveTileTextureToCache(texAfterCrop, _baseX, _baseY, _baseZoom);

                }

                // Возвращаем результат высших зумов
                if (isElevationTexture)
                {
                    //TileStat.dictTiles[_baseZoom].AddLog(_baseX, _baseY, "Берем текстуру родителя. ");
                }
                return new ParentSearchResult
                {
                    OutputTex = parentTex,
                    OutputZoom = currentZoom,
                    OutputX = currentX,
                    OutputY = currentY,
                    requiredZoom = _baseZoom,
                    requiredX = _baseX,
                    requiredY = _baseY
                };
            }

            // ничего не найдено
            return null;
        }











        
        //Проверка вершин на валидность
        private static bool IsValidHeightmap(float[,] map)
        {
            if (map == null) return false;
            int size = TileStat.ElevationTileResolution;
            if (map.GetLength(0) != size || map.GetLength(1) != size) return false;

            // Быстрая проверка на NaN
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (float.IsNaN(map[x, y]) || float.IsInfinity(map[x, y]))
                        return false;
            return true;
        }


        public static float[,] DecodeTerrarium(Texture2D texture)
        {
            return DecodeTerrarium(
                texture,
                TileStat.EnableHeightDespike,
                TileStat.DespikeThreshold,
                TileStat.EnableHeightSmoothing,
                TileStat.SmoothingSigma,
                TileStat.SmoothingIterations
            );
        }

        public static float[,] DecodeTerrarium(
            Texture2D texture,
            bool enableDespike,
            float despikeThreshold,
            bool enableSmoothing,
            float smoothingSigma,
            int smoothingIterations)
        {
            if (texture == null) return null;

            int size = TileStat.ElevationTileResolution;
            float[,] floatHeights = new float[size, size];

            Color32[] pixels = null;
            try
            {
                pixels = texture.GetPixels32();
            }
            catch
            {
                // Fallback если текстура не поддерживает GetPixels32 напрямую
            }

            if (pixels != null && pixels.Length == size * size)
            {
                for (int y = 0; y < size; y++)
                {
                    int row = y * size;
                    for (int x = 0; x < size; x++)
                    {
                        Color32 p = pixels[row + x];
                        float elevation = (p.r * 256f + p.g + p.b / 256f) - 32768f;
                        floatHeights[x, y] = elevation;
                    }
                }
            }
            else
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Color pixel = texture.GetPixel(x, y);
                        int R = Mathf.FloorToInt(pixel.r * 255f);
                        int G = Mathf.FloorToInt(pixel.g * 255f);
                        int B = Mathf.FloorToInt(pixel.b * 255f);

                        float elevation = (R * 256f + G + B / 256f) - 32768f;
                        floatHeights[x, y] = elevation;
                    }
                }
            }

            // 1. Фильтр гашения резких выбросов (пиков и отвесных дыр)
            if (enableDespike)
            {
                HeightmapFilter.Despike(floatHeights, despikeThreshold);
            }

            // 2. Настраиваемое Гауссово сглаживание
            if (enableSmoothing)
            {
                HeightmapFilter.GaussianBlur(floatHeights, smoothingSigma, smoothingIterations);
            }

            return floatHeights;
        }

        public float[,] ExtractChildTile(
            float[,] parentTile,
            int parentZoom,
            int childZoom,
            int childX,
            int childY,
            bool isCropSize = false)
        {
            int factor = 1 << (childZoom - parentZoom);
            int parentSize = parentTile.GetLength(0);

            // Стандартный размер
            int baseSize = 256;

            // Выбираем размер фрагмента
            int childSize = isCropSize ? baseSize / factor : baseSize;

            // ДОБАВЛЯЕМ +1 пиксель только в режиме crop
            int resultSize = isCropSize ? childSize + 1 : childSize;

            float[,] result = new float[resultSize, resultSize];

            // Координаты родителя
            int parentX = childX / factor;
            int parentY = childY / factor;

            // Смещения внутри родителя
            int offsetY = childX % factor;
            int offsetX = (factor - 1) - (childY % factor);

            for (int y = 0; y < resultSize; y++)
            {
                for (int x = 0; x < resultSize; x++)
                {
                    // используем childSize (не resultSize!) для расчёта координат
                    float srcX = (x + offsetX * childSize) / (float)(childSize * factor) * parentSize;
                    float srcY = (y + offsetY * childSize) / (float)(childSize * factor) * parentSize;

                    // 🔥 Обязательно ограничиваем
                    if (srcX >= parentSize) srcX = parentSize - 1.0001f;
                    if (srcY >= parentSize) srcY = parentSize - 1.0001f;

                    int x0 = Mathf.FloorToInt(srcX);
                    int y0 = Mathf.FloorToInt(srcY);
                    int x1 = Mathf.Min(x0 + 1, parentSize - 1);
                    int y1 = Mathf.Min(y0 + 1, parentSize - 1);

                    float dx = srcX - x0;
                    float dy = srcY - y0;

                    float v00 = parentTile[y0, x0];
                    float v10 = parentTile[y0, x1];
                    float v01 = parentTile[y1, x0];
                    float v11 = parentTile[y1, x1];

                    float v0 = v00 * (1f - dx) + v10 * dx;
                    float v1 = v01 * (1f - dx) + v11 * dx;

                    result[y, x] = v0 * (1f - dy) + v1 * dy;
                }
            }

            return result;
        }


        /// <summary>
        /// загружаем супер-текстуртекстуру асинхронно
        /// </summary>
        /// <param name="wp"></param>
        /// <param name="zoomDif">Разнича между базовым и детализиторанным зумом</param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask LoadSuperTextureSlotAsync(
            WorldPosition wp,
            int zoomDif,
            CancellationToken token,
            (int x, int y, Texture2D tex)[] results,
            Exception[] errors,
            int resultIndex)
        {
            try
            {
                results[resultIndex] = await LoadTileCachOrServerAsync(wp, zoomDif, token);
            }
            catch (Exception exception)
            {
                errors[resultIndex] = exception;
            }
        }

        private async UniTask<(int x, int y, Texture2D tex)> LoadTileCachOrServerAsync(
            WorldPosition wp,
            int zoomDif,
            CancellationToken token)
        {
            ITextureProvider textureServer = SatelliteTextureProvider;
            ITextureProvider textureCach = TextureCacheProvider;

            IAsyncLoader<Texture2D> asyncServerLoader = UrlTextureLoader;
            IAsyncLoader<Texture2D> asyncCachLoader = CacheTextureLoader;

            ZXY key = wp.ToZXY();
            Texture2D tex;

            tex = await asyncCachLoader.LoadAsync(textureCach.GetTexturePath(key), token);
            if (tex == null)
            {
                tex = await asyncServerLoader.LoadAsync(textureServer.GetTexturePath(key), token);
                if (tex != null)
                {
                    try
                    {
                        CacheTileUtility.SaveTileTextureToCache(tex, key);
                    }
                    catch
                    {
                        Texture2D failedTexture = tex;
                        tex = null;
                        await TextureDownloadThrottle.DestroyAfterUploadAsync(failedTexture);
                        throw;
                    }
                }
            }

            // Do not call Apply() here: downloaded child textures are destroyed
            // immediately after the GPU merge, and Apply would enqueue a second
            // full Gfx.UploadTexture only to discard their short-lived CPU copy.

            // смещение тайлов относительно родителя
            int tileCount = 1 << zoomDif; // 2^zoomDif
            int x = wp.tileX % tileCount;
            int y = (tileCount - 1) - (wp.tileY % tileCount); // инверсия Y для правильного отображения

            return (x, y, tex);
        }


        public void CancelSuperTexture(float daley)
        {
            try
            {
                int fadeGeneration = generationId;
                int fadeRevision = ++superTextureRevision;

                // Отменяем загрузку, если она еще выполняется
                if (superTexCts != null)
                {
                    if (!superTexCts.IsCancellationRequested)
                        superTexCts.Cancel();
                    superTexCts.Dispose();
                    superTexCts = null;
                }
                if (daley > 0)
                //если есть анимационная задержка
                {
                    if (isSupertexture)
                    {
                        // Устанавливаем параметры плавного возврата
                        _terrainProperties.SetFloat(BlendStartTimeId, Time.time);
                        _terrainProperties.SetFloat(BlendDirectionId, -1.0f);
                        mR.SetPropertyBlock(_terrainProperties);

                        // ⚡ Отложенное уничтожение (через UniTask)
                        Texture texToDestroy = superTextureMap;
                        superTextureMap = null;

                        UniTask.Void(async () =>
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(daley));

                            // Do not let an old fade callback modify a pooled/reused
                            // tile or a newer super-texture transition.
                            if (fadeGeneration == generationId &&
                                fadeRevision == superTextureRevision &&
                                !_resourcesReleased &&
                                !isSupertexture &&
                                mR != null &&
                                _terrainProperties != null)
                            {
                                // MaterialPropertyBlock.SetTexture does not accept
                                // null in Unity 2023.2. The shader skips this sample
                                // when the blend reaches zero.
                                _terrainProperties.SetTexture(SuperTextureId, Texture2D.blackTexture);
                                _terrainProperties.SetFloat(BlendStartTimeId, 0.0f);
                                _terrainProperties.SetFloat(BlendDirectionId, -1.0f);
                                mR.SetPropertyBlock(_terrainProperties);
                            }
                            if (texToDestroy != null)
                                ReleaseRuntimeTexture(texToDestroy);
                        });
                    }
                }
                else
                {
                    //материал по дефаулту
                    if (_terrainProperties != null && mR != null)
                    {
                        _terrainProperties.SetTexture(SuperTextureId, Texture2D.blackTexture);
                        _terrainProperties.SetFloat(BlendStartTimeId, 0.0f);
                        _terrainProperties.SetFloat(BlendDirectionId, -1.0f);
                        mR.SetPropertyBlock(_terrainProperties);
                    }

                    if (superTextureMap != null)
                    {
                        ReleaseRuntimeTexture(superTextureMap);
                        superTextureMap = null;
                    }
                }

                // 4️⃣ Финальный сброс флага
                isSupertexture = false;
                //Debug.Log($"🧩 CancelSuperTexture запущен, ожидание {daley}с.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CancelSuperTexture] Ошибка: {ex.Message}");
            }
        }


        private static Material GetSurfaceOverlayMaterial()
        {
            if (TileManager.Instance != null && TileManager.Instance.SurfaceOverlayMaterial != null)
            {
                return TileManager.Instance.SurfaceOverlayMaterial;
            }

            if (_sharedSurfaceOverlayMaterial != null)
                return _sharedSurfaceOverlayMaterial;

            if (_surfaceOverlayShader == null)
                _surfaceOverlayShader = Shader.Find(SurfaceOverlayShaderName);

            if (_surfaceOverlayShader == null)
                return null;

            _sharedSurfaceOverlayMaterial = new Material(_surfaceOverlayShader)
            {
                name = "Terrain Surface Overlay (Shared)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _sharedSurfaceOverlayMaterial;
        }

        private void RecoverSurfaceOverlayReferences()
        {
            if (_surfaceOverlay == null && terrain != null)
            {
                Transform existingOverlay = terrain.transform.Find("SurfaceOverlay");
                if (existingOverlay != null)
                    _surfaceOverlay = existingOverlay.gameObject;
            }

            if (_surfaceOverlay == null)
                return;

            _surfaceOverlayMeshFilter ??= _surfaceOverlay.GetComponent<MeshFilter>();
            _surfaceOverlayRenderer ??= _surfaceOverlay.GetComponent<MeshRenderer>();
            _gpuGrassRenderer ??= _surfaceOverlay.GetComponent<GpuGrassRenderer>();

            if (_surfaceOverlayRenderer != null)
                _surfaceOverlayRenderer.receiveShadows = true;
        }

        private void EnsureGpuGrassRenderer(Texture satelliteTex)
        {
            MeshFilter targetMeshFilter = mF != null ? mF : (_surfaceOverlayMeshFilter != null ? _surfaceOverlayMeshFilter : null);
            GameObject targetGo = terrain != null ? terrain : _surfaceOverlay;

            if (targetGo == null || targetMeshFilter == null || satelliteTex == null)
                return;

            _gpuGrassRenderer ??= targetGo.GetComponent<GpuGrassRenderer>();
            if (_gpuGrassRenderer == null)
                _gpuGrassRenderer = targetGo.AddComponent<GpuGrassRenderer>();

            _gpuGrassRenderer.Configure(targetMeshFilter, satelliteTex);
            if (_paintMaskTex != null)
                _gpuGrassRenderer.SetPaintMask(_paintMaskTex);
        }

        private void SetSurfaceOverlayTexture(Texture satelliteTex)
        {
            RecoverSurfaceOverlayReferences();
            if (_surfaceOverlayRenderer != null)
                _surfaceOverlayRenderer.enabled = false;
            EnsureGpuGrassRenderer(satelliteTex);
        }

        private void CreateOrUpdateSurfaceOverlay(Texture satelliteTex)
        {
            if (satelliteTex == null)
                return;

            RecoverSurfaceOverlayReferences();
            if (_surfaceOverlay != null)
            {
                if (_surfaceOverlayMeshFilter != null && mF != null)
                    _surfaceOverlayMeshFilter.sharedMesh = mF.sharedMesh;
                EnsureGpuGrassRenderer(satelliteTex);
                SetSurfaceOverlayTexture(satelliteTex);
                return;
            }

            if (mF == null || mF.sharedMesh == null)
                return;

            var sharedMaterial = GetSurfaceOverlayMaterial();
            if (sharedMaterial == null)
            {
                Debug.LogError($"[TerrainTile] SurfaceOverlay FAIL: shader '{SurfaceOverlayShaderName}' not found.");
                return;
            }

            _surfaceOverlay = new GameObject("SurfaceOverlay");
            _surfaceOverlay.transform.SetParent(terrain.transform, false);

            _surfaceOverlayMeshFilter = _surfaceOverlay.AddComponent<MeshFilter>();
            _surfaceOverlayMeshFilter.sharedMesh = mF.sharedMesh;

            _surfaceOverlayRenderer = _surfaceOverlay.AddComponent<MeshRenderer>();
            _surfaceOverlayRenderer.sharedMaterial = sharedMaterial;
            _surfaceOverlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _surfaceOverlayRenderer.receiveShadows = true;
            _surfaceOverlayRenderer.lightProbeUsage = LightProbeUsage.Off;
            _surfaceOverlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // This component only schedules GPU work. Grass positions and blade
            // triangles are generated by TerrainGrassGenerator.compute.
            EnsureGpuGrassRenderer(satelliteTex);
            SetSurfaceOverlayTexture(satelliteTex);
        }

        private void DestroySurfaceOverlay()
        {
            if (_surfaceOverlay != null)
                Destroy(_surfaceOverlay);

            _surfaceOverlay = null;
            _surfaceOverlayMeshFilter = null;
            _surfaceOverlayRenderer = null;
            _gpuGrassRenderer = null;
            _surfaceOverlayProperties = null;
        }

        public void SetPaintMask(Texture2D paintTex)
        {
            if (_paintMaskTex != null && _paintMaskTex != paintTex)
                ReleaseRuntimeTexture(_paintMaskTex);
            _paintMaskTex = paintTex;
            CreateOrUpdatePaintOverlay(paintTex);
        }

        private void CreateOrUpdatePaintOverlay(Texture paintTex)
        {
            if (paintTex == null) return;

            if (_paintOverlay != null)
            {
                if (_paintOverlayMeshFilter != null && mF != null)
                    _paintOverlayMeshFilter.sharedMesh = mF.sharedMesh;
                SetPaintOverlayTexture(paintTex);
                return;
            }

            if (mF == null || mF.sharedMesh == null) return;

            var sharedMaterial = GetPaintOverlayMaterial();
            if (sharedMaterial == null) return;

            _paintOverlay = new GameObject("PaintOverlay");
            _paintOverlay.transform.SetParent(terrain.transform, false);

            _paintOverlayMeshFilter = _paintOverlay.AddComponent<MeshFilter>();
            _paintOverlayMeshFilter.sharedMesh = mF.sharedMesh;

            _paintOverlayRenderer = _paintOverlay.AddComponent<MeshRenderer>();
            _paintOverlayRenderer.sharedMaterial = sharedMaterial;
            _paintOverlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _paintOverlayRenderer.receiveShadows = true;
            _paintOverlayRenderer.lightProbeUsage = LightProbeUsage.Off;
            _paintOverlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            SetPaintOverlayTexture(paintTex);
        }

        private void SetPaintOverlayTexture(Texture paintTex)
        {
            if (paintTex is Texture2D t2d)
                _paintMaskTex = t2d;
            if (_gpuGrassRenderer != null && paintTex != null)
                _gpuGrassRenderer.SetPaintMask(paintTex);

            if (mR != null && paintTex != null)
            {
                _terrainProperties ??= new MaterialPropertyBlock();
                mR.GetPropertyBlock(_terrainProperties);
                _terrainProperties.SetTexture(PaintTexId, paintTex);
                _terrainProperties.SetFloat(Shader.PropertyToID("_EnableCustomPaint"), 1.0f);
                mR.SetPropertyBlock(_terrainProperties);
            }

            if (_paintOverlayRenderer != null)
                _paintOverlayRenderer.enabled = false;
        }

        private static Material GetPaintOverlayMaterial()
        {
            if (TileManager.Instance != null && TileManager.Instance.PaintOverlayMaterial != null)
                return TileManager.Instance.PaintOverlayMaterial;

            if (_sharedPaintOverlayMaterial != null)
                return _sharedPaintOverlayMaterial;

            if (_paintOverlayShader == null)
                _paintOverlayShader = Shader.Find(PaintOverlayShaderName);

            if (_paintOverlayShader == null) return null;

            _sharedPaintOverlayMaterial = new Material(_paintOverlayShader)
            {
                name = "Custom Paint Overlay (Shared)",
                hideFlags = HideFlags.HideAndDontSave
            };

            _sharedPaintOverlayMaterial.SetTexture(PathTexId, CreateDetailTexture(
                new Color32(139, 119, 101, 255), new Color32(120, 100, 80, 255), "PathDetail"));
            _sharedPaintOverlayMaterial.SetTexture(GrassTexId, CreateDetailTexture(
                new Color32(76, 142, 50, 255), new Color32(60, 120, 35, 255), "GrassDetail"));
            _sharedPaintOverlayMaterial.SetTexture(DirtTexId, CreateDetailTexture(
                new Color32(121, 85, 48, 255), new Color32(100, 70, 35, 255), "DirtDetail"));
            _sharedPaintOverlayMaterial.SetTexture(WaterTexId, CreateDetailTexture(
                new Color32(30, 100, 180, 255), new Color32(20, 80, 160, 255), "WaterDetail"));
            _sharedPaintOverlayMaterial.SetFloat(TileSizeId, 10f);

            return _sharedPaintOverlayMaterial;
        }

        public static Texture2D CreateDetailTexture(Color32 c1, Color32 c2, string name)
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / 8) + (y / 8)) % 2 == 0;
                    pixels[y * size + x] = even ? c1 : c2;
                }
            tex.SetPixels32(pixels);
            tex.Apply(true);
            tex.name = name;
            return tex;
        }

        private void DestroyPaintOverlay()
        {
            if (_paintOverlay != null)
                Destroy(_paintOverlay);
            _paintOverlay = null;
            _paintOverlayMeshFilter = null;
            _paintOverlayRenderer = null;
            _paintOverlayProperties = null;
        }

        public void CancelLoading()
        {
            if (tileCts != null && !tileCts.IsCancellationRequested)
                tileCts.Cancel();
            if (superTexCts != null && !superTexCts.IsCancellationRequested)
                superTexCts.Cancel();
        }

        private void BeginResourceRelease()
        {
            generationId++;
            superTextureRevision++;
            _colliderBuildRequestId++;
            _colliderBuildQueued = false;
            StopAllCoroutines();
            isVisible = false;

            if (terrain != null)
                terrain.SetActive(false);

            CancelLoading();

            isSupertexture = false;
            _readyTcs = null;

            // Renderer property blocks otherwise keep references to textures
            // even after the tile is no longer visible.
            if (mR != null && _terrainProperties != null)
            {
                _terrainProperties.Clear();
                mR.SetPropertyBlock(_terrainProperties);
            }

            if (_surfaceOverlayRenderer != null)
            {
                _surfaceOverlayProperties ??= new MaterialPropertyBlock();
                _surfaceOverlayProperties.Clear();
                _surfaceOverlayRenderer.SetPropertyBlock(_surfaceOverlayProperties);
            }

            if (_paintOverlayRenderer != null)
            {
                _paintOverlayProperties ??= new MaterialPropertyBlock();
                _paintOverlayProperties.Clear();
                _paintOverlayRenderer.SetPropertyBlock(_paintOverlayProperties);
            }

            DestroySurfaceOverlay();
            DestroyPaintOverlay();
        }

        private static void ReleaseRuntimeTexture(Texture texture)
        {
            if (texture == null)
                return;

            if (texture is RenderTexture renderTexture)
            {
                if (RenderTexture.active == renderTexture)
                    RenderTexture.active = null;
                if (renderTexture.IsCreated())
                    renderTexture.Release();
            }

            Destroy(texture);
        }

        private void ReleaseNativeResources()
        {
            ReleaseRuntimeTexture(elivationMap);
            ReleaseRuntimeTexture(textureMap);
            ReleaseRuntimeTexture(superTextureMap);
            ReleaseRuntimeTexture(lowTextureMap);
            ReleaseRuntimeTexture(lowElevationMap);

            elivationMap = null;
            textureMap = null;
            superTextureMap = null;
            lowTextureMap = null;
            lowElevationMap = null;

            Mesh terrainMesh = mF != null ? mF.sharedMesh : null;
            Mesh colliderMesh = mC != null ? mC.sharedMesh : null;

            if (mC != null)
            {
                mC.sharedMesh = null;
                mC.convex = false;
            }

            if (colliderMesh != null && colliderMesh != terrainMesh)
                Destroy(colliderMesh);

            mM = null;
            _colliderSourceMesh = null;

            if (mF != null)
                mF.sharedMesh = null;
            if (terrainMesh != null)
                MeshPool.ReleaseMesh(terrainMesh);

            tileCts?.Dispose();
            tileCts = null;
            superTexCts?.Dispose();
            superTexCts = null;
        }

        /// <summary>
        /// Cancels asynchronous work and releases all per-tile native resources.
        /// The pool must await this method before reusing the GameObject.
        /// </summary>
        public async UniTask PrepareForPoolAsync()
        {
            if (_resourcesReleased || _isReleasingResources)
                return;

            _isReleasingResources = true;
            try
            {
                BeginResourceRelease();
                // Let cancelled requests and deferred Destroy calls settle before
                // the same GameObject can be dequeued for a different tile.
                await UniTask.DelayFrame(1);
                if (!_resourcesReleased)
                    ReleaseNativeResources();
                _resourcesReleased = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Tile cleanup error: {ex.Message}");
                if (!_resourcesReleased)
                {
                    ReleaseNativeResources();
                    _resourcesReleased = true;
                }
            }
            finally
            {
                _isReleasingResources = false;
            }
        }

        // Compatibility for existing callers.
        public UniTask ReleaseTextureAsync()
        {
            return PrepareForPoolAsync();
        }

        private void OnDestroy()
        {
            if (_resourcesReleased)
                return;

            BeginResourceRelease();
            ReleaseNativeResources();
            _resourcesReleased = true;
        }

        //private IEnumerator DestroyLater(UnityEngine.Object obj)
        //{
        //    yield return new WaitForEndOfFrame(); // ждём пока GPU закончит кадр
        //    if (obj != null)
        //    {
        //        // Сбрасываем активный RenderTexture перед уничтожением
        //        if (RenderTexture.active == obj)
        //            RenderTexture.active = null;

        //        UnityEngine.Object.Destroy(obj);
        //    }
        //}


        


        private Coroutine _visibilityCoroutine; // ссылка на текущую карутину
        /// <summary>
        /// пытаемся плавно менять видимость поднимая и опуская его 
        /// </summary>
        /// <param name="visible"></param>
        /// <returns>возвращает true если смена удалась, false если тайл не поменял своего состояния</returns>
        public bool TrySetVisible(bool visible, float moveDuration = 0)
        {
             if (visible == isVisible & terrain.active == visible)
                return false;

            if (visible && textureMap != null)
            {
                CreateOrUpdateSurfaceOverlay(textureMap);
                if (_paintMaskTex != null)
                    CreateOrUpdatePaintOverlay(_paintMaskTex);
            }

            // Если уже запущена анимация — останавливаем её
            if (_visibilityCoroutine != null) {
                StopCoroutine(_visibilityCoroutine);
                moveDuration = 0;
            }
            if (moveDuration == 0)
            {
                _visibilityCoroutine = null;
                terrain.SetActive(visible);
                isVisible = visible;
                var pos = terrain.transform.localPosition;
                terrain.transform.localPosition = new Vector3(pos.x, 0, pos.z);
            }
            else
                _visibilityCoroutine = StartCoroutine(CoroutinSetVisible(visible, moveDuration));

            return true;
        }


        private IEnumerator CoroutinSetVisible(bool visible, float moveDuration)
        {
            float hiddenOffsetY = -40f;
            if (visible == true & isVisible == false)
            {
                isVisible = visible;
                terrain.SetActive(true);

                

                //правно поднимаем тайл
                yield return StartCoroutine(MoveTileY(terrain, hiddenOffsetY / (1+zoom), 0f, moveDuration));
                //Debug.Log("поднялся");
            }
            if (visible == false & isVisible == true)
            {
                isVisible = visible;

                // ждём 1 секунду, потом продолжаем анимацию
                yield return new WaitForSeconds(moveDuration);
                //плавно опускаем тайл
                yield return StartCoroutine(MoveTileY(terrain, 0f, hiddenOffsetY / (1+zoom), moveDuration));
                terrain.SetActive(false);
                //Debug.Log("опустился");
            }
        }

        private IEnumerator MoveTileY(GameObject tile, float fromY, float toY, float duration)
        {
            float t = 0f;
            Vector3 start = tile.transform.localPosition;

            while (t < duration)
            {
                t += Time.deltaTime;
                float y = Mathf.Lerp(fromY, toY, t / duration);
                tile.transform.localPosition = new Vector3(start.x, y, start.z);
                yield return null;
            }

            tile.transform.localPosition = new Vector3(start.x, toY, start.z);
            _visibilityCoroutine = null;
        }



        public UniTask WaitReadyAsync()
        {
            if (
                //State.CurrentType == TileStateType.Low ||
                //State.CurrentType == TileStateType.Initializing ||
                State.CurrentType == TileStateType.Ready ||
                State.CurrentType == TileStateType.CreatingSuperTile ||
                State.CurrentType == TileStateType.SuperTile)
            { 
                return UniTask.CompletedTask;
            }
                

            _readyTcs ??= new UniTaskCompletionSource();
            return _readyTcs.Task;

        }


        public void SetTileName(string text = "")
        {
            var wp = this.worldPosition;
            name = "Z" + wp.zoom + "[" + wp.tileX + "/" + wp.tileY + "]" + text;
        }
    }
}
