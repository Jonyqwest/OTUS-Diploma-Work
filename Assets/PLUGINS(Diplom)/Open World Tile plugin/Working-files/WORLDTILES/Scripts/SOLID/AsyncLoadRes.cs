namespace WorldTile
{
    using System.Threading;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;



    //////////////Интерфейс

    //Асинхронная загрузка
    public interface IAsyncLoader<T>
    {
        /// <summary>
        /// Асинхронно загружает ресурс типа T.
        /// </summary>
        /// <param name="key">Уникальный идентификатор ресурса (может быть URL, путь или ключ тайла).</param>
        /// <param name="token">Токен отмены.</param>
        UniTask<T> LoadAsync(string key, CancellationToken token);
    }


    //////////////Реализация

    internal static class TextureDownloadThrottle
    {
        private const int MaxConcurrentTextureDownloads = 8; // Увеличено с 4 до 8 для параллелизма высот и текстур
        private static readonly SemaphoreSlim DownloadGate = new SemaphoreSlim(
            MaxConcurrentTextureDownloads,
            MaxConcurrentTextureDownloads);

        public static async UniTask<bool> EnterAsync(CancellationToken token)
        {
            try
            {
                await DownloadGate.WaitAsync(token);
                return true;
            }
            catch (System.OperationCanceledException)
            {
                return false;
            }
        }

        public static void Exit()
        {
            DownloadGate.Release();
        }

        public static async UniTask DestroyAfterUploadAsync(Texture2D texture)
        {
            if (texture == null)
                return;

            // DownloadHandlerTexture can finish native creation while the render
            // thread is still consuming the upload command. Do not destroy it in
            // that same frame, especially on Metal with multithreaded rendering.
            await UniTask.DelayFrame(2);
            if (texture != null)
                Object.Destroy(texture);
        }
    }

    //Асинхронная загрузка Texture2D по URL
    public class TextureLoaderFromURL : IAsyncLoader<Texture2D>
    {
        // Several nearby super tiles can request 16/64 images each. Letting all
        // DownloadHandlerTexture instances finish in one frame creates a burst
        // of Gfx.CreateTexture/Gfx.UploadTexture work on the render thread.
        private static float nextPlaceholderWarningTime;

        public async UniTask<Texture2D> LoadAsync(string url, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return null;

            if (!await TextureDownloadThrottle.EnterAsync(token))
                return null;

            try
            {
                await UniTask.SwitchToMainThread();
                if (token.IsCancellationRequested)
                    return null;

                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
                {
                    request.timeout = 5; // Защита от вечного зависания сети
                    var operation = request.SendWebRequest();

                    // Ждём окончания запроса
                    while (!operation.isDone)
                    {
                        if (token.IsCancellationRequested)
                        {
                            request.Abort();
                            return null;
                        }
                        await UniTask.Yield(); // отпускаем поток
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"Failed to load texture from {url}: {request.error}");
                        return null;
                    }
                    Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                    try
                    {
                        // When the response is a placeholder, ownership still belongs
                        // to this loader and the generated Texture2D must be destroyed.
                        if (ComparePictures.IsZaglushka(downloadedTexture))
                        {
                            // Avoid hundreds of stack traces and their managed allocations.
                            if (Time.realtimeSinceStartup >= nextPlaceholderWarningTime)
                            {
                                nextPlaceholderWarningTime = Time.realtimeSinceStartup + 2.0f;
                                Debug.LogWarning($"No detailed satellite texture near {url}");
                            }

                            Texture2D placeholder = downloadedTexture;
                            downloadedTexture = null;
                            await TextureDownloadThrottle.DestroyAfterUploadAsync(placeholder);
                            return null;
                        }

                        Texture2D result = downloadedTexture;
                        downloadedTexture = null; // ownership is transferred to the caller
                        return result;
                    }
                    finally
                    {
                        if (downloadedTexture != null)
                            await TextureDownloadThrottle.DestroyAfterUploadAsync(downloadedTexture);
                    }
                }
            }
            finally
            {
                TextureDownloadThrottle.Exit();
            }
        }
    }


    // Асинхронная загрузка Texture2D из Cache
    public class TextureLoaderFromCache : IAsyncLoader<Texture2D>
    {
        public async UniTask<Texture2D> LoadAsync(string path, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return null;

            // даём управление обратно в цикл, чтобы метод оставался "настоящим async"
            await UniTask.Yield();

            if (token.IsCancellationRequested)
                return null;

            return CacheTileUtility.LoadTileTextureFromCache(path);
        }
    }

    // Асинхронная загрузка Low Texture2D из Cache
    public class LowTexLoaderFromCache : IAsyncLoader<Texture2D>
    {
        public async UniTask<Texture2D> LoadAsync(string path, CancellationToken token)
        {
            await UniTask.SwitchToMainThread();
            // даём управление обратно в цикл, чтобы метод оставался "настоящим async"
            //await UniTask.Yield();

            if (token.IsCancellationRequested)
                return null;

            return CacheTileUtility.LoadTileTextureFromCache(path);
        }
    }
}
