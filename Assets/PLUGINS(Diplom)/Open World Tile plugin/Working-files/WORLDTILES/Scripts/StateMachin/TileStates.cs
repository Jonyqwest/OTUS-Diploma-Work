namespace WorldTile
{
    using System;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using UnityEngine;


    /// <summary>
    /// Список возможных состояний тайла.
    /// </summary>
    public enum TileStateType
    {
        None,
        InPool,             // 1. Просто создан и не активен
        Initializing,       // 2. Инициализация
        Low,                // 3. Загружен плоский меш + low текстура
        LoadServerData,     // 4. Ожидание данных с сервера
        Ready,              // 5. Полностью готов
        CreatingSuperTile,  // 6. Создание супертайла
        SuperTile           // 7. Это сам супертайл
    }




    /// <summary>
    /// 1. Тайл находится в пуле (неактивен, скрыт).
    /// </summary>
    public class TileState_InPool : ITileState
    {
        public TileStateType StateType => TileStateType.InPool;

        public void OnEnter(TerrainTile tile)
        {
            tile.SetTileName("inPool");
            //Debug.Log("Плавно опускаем тайл");
        }

        public void OnExit(TerrainTile tile)
        {
            tile.SetTileName("inPool");
        }
    }

    /// <summary>
    /// 2. Инициализация тайла (создание объектов, назначение компонентов).
    /// </summary>
    public class TileState_Initializing : ITileState
    {
        public TileStateType StateType => TileStateType.Initializing;

        public void OnEnter(TerrainTile terrainTile)
        {
            terrainTile.SetTileName("Init");
            terrainTile.CreateTile();

            //Переходим на стат загрузка в Низком разрешении
            terrainTile.State.ChangeState(terrainTile, new TileState_Low());
        }

        public void OnExit(TerrainTile tile)
        {
        }
    }

    /// <summary>
    /// 3. Загружен плоский меш и низкокачественная текстура.
    /// </summary>
    public class TileState_Low : ITileState
    {
        public TileStateType StateType => TileStateType.Low;

        public void OnEnter(TerrainTile terrainTile)
        {
            terrainTile.SetTileName("Low");
            LoadLowTextureSafely(terrainTile, terrainTile.GenerationId).Forget();
            //_ = terrainTile.LoadLowElevation();

            //Переходим к загрузке спутниковых данных
            terrainTile.State.ChangeState(terrainTile, new TileState_LoadServerData());
            //terrainTile.State.ChangeState(terrainTile, new TileState_Ready());
        }

        public void OnExit(TerrainTile tile)
        {
        }

        private static async UniTaskVoid LoadLowTextureSafely(TerrainTile tile, int generation)
        {
            try
            {
                await tile.LoadLowTextureAsync();
            }
            catch (OperationCanceledException)
            {
                // Normal when the tile is removed or reused.
            }
            catch (Exception exception)
            {
                if (tile != null && tile.GenerationId == generation)
                    Debug.LogError($"[LowTexture] Tile load failed: {exception}");
            }
        }
    }

    /// <summary>
    /// 4. Ожидание данных от сервера (высоты, текстуры, и т.д.)
    /// </summary>
    public class TileState_LoadServerData : ITileState
    {
        public TileStateType StateType => TileStateType.LoadServerData;

        public void OnEnter(TerrainTile tile)
        {
            tile.SetTileName("MeshLoad..");

            LoadTextureSafely(tile, tile.GenerationId).Forget();
            
            LoaderServerData loader = new();
            loader.GetServerData(tile);

            
        }

        public void OnExit(TerrainTile tile)
        {
        }

        private static async UniTaskVoid LoadTextureSafely(TerrainTile tile, int generation)
        {
            try
            {
                await tile.LoadTextureAsync();
            }
            catch (OperationCanceledException)
            {
                // Normal when the tile is removed or reused.
            }
            catch (Exception exception)
            {
                if (tile != null && tile.GenerationId == generation)
                    Debug.LogError($"[Texture] Tile load failed: {exception}");
            }
        }
    }

    /// <summary>
    /// 5. Полностью готов и может быть показан.
    /// </summary>
    public class TileState_Ready : ITileState
    {
        public TileStateType StateType => TileStateType.Ready;

        public void OnEnter(TerrainTile terrain)
        {
            terrain.SetTileName("_Ready ");

            //terrain.AddColliderMatch();

            if (terrain._readyTcs != null)
            {
                terrain._readyTcs.TrySetResult();
                terrain._readyTcs = null;
            }
               
        }

        public void OnExit(TerrainTile tile)
        {
        }
    }

    /// <summary>
    /// 6. Создание супертайла (сбор нескольких под-тайлов).
    /// </summary>
    public class TileState_CreatingSuperTile : ITileState
    {
        public TileStateType StateType => TileStateType.CreatingSuperTile;

        public async void OnEnter(TerrainTile terrainTile)
        {
            try
            {
                await terrainTile.CreateSuperTextureAsync();

                // Cancellation/removal may have already returned this tile to
                // Ready or to the pool while the requests were in flight.
                if (terrainTile != null &&
                    terrainTile.State.CurrentType == TileStateType.CreatingSuperTile)
                {
                    if (terrainTile.HasSuperTexture)
                        terrainTile.State.ChangeState(terrainTile, new TileState_SuperTile());
                    else
                        terrainTile.State.ChangeState(terrainTile, new TileState_Ready());
                }
            }
            catch (OperationCanceledException)
            {
                if (terrainTile != null &&
                    terrainTile.State.CurrentType == TileStateType.CreatingSuperTile)
                    terrainTile.State.ChangeState(terrainTile, new TileState_Ready());
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SuperTexture] Creation failed: {exception}");
                if (terrainTile != null &&
                    terrainTile.State.CurrentType == TileStateType.CreatingSuperTile)
                    terrainTile.State.ChangeState(terrainTile, new TileState_Ready());
            }
        }

        public void OnExit(TerrainTile terrainTile)
        {
            
        }
    }

    /// <summary>
    /// 7. Сам супертайл (собранный из нескольких).
    /// </summary>
    public class TileState_SuperTile : ITileState
    {
        public TileStateType StateType => TileStateType.SuperTile;

        public void OnEnter(TerrainTile tile)
        {
            tile.SetTileName("_SuperTexture");
        }

        public void OnExit(TerrainTile terrainTile)
        {
            
        }
    }

    public class LoaderServerData
    {
        public async void GetServerData(TerrainTile tile)
        {
            if (tile == null)
                return;

            int loadGeneration = tile.GenerationId;

            int retryCount = 0;
            while (IsCurrentLoad(tile, loadGeneration))
            {
                try
                {
                    bool meshApplied = await tile.InitMeshAsync();
                    if (!meshApplied)
                        throw new InvalidOperationException("No valid elevation data was available for the tile mesh.");

                    if (IsCurrentLoad(tile, loadGeneration))
                        tile.State.ChangeState(tile, new TileState_Ready());
                    return;
                }
                catch (OperationCanceledException)
                {
                    if (!IsCurrentLoad(tile, loadGeneration))
                        return;
                }
                catch (Exception exception)
                {
                    if (!IsCurrentLoad(tile, loadGeneration))
                        return;

                    Debug.LogError($"[LoaderServerData] Tile load failed; retrying: {exception}");
                }

                retryCount++;
                if (retryCount >= 3)
                {
                    Debug.LogWarning($"[LoaderServerData] Tile {tile.worldPosition.tileX},{tile.worldPosition.tileY} failed after {retryCount} attempts. Marking Ready to unblock loading.");
                    if (IsCurrentLoad(tile, loadGeneration))
                        tile.State.ChangeState(tile, new TileState_Ready());
                    return;
                }
                int retryDelayMs = Mathf.Min(10000, 500 << Mathf.Min(retryCount - 1, 3));
                await UniTask.Delay(retryDelayMs);
            }
        }

        private static bool IsCurrentLoad(TerrainTile tile, int generation)
        {
            return tile != null &&
                   tile.GenerationId == generation &&
                   tile.State.CurrentType == TileStateType.LoadServerData;
        }
    }
}
