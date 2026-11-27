using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
 
using UnityEngine.Networking;
using WorldTile;

public class LowTilesInitializer 
{
    private const string CompletionMarkerName = ".lowtiles-complete";

    [System.NonSerialized]
    public Action TileComplite;
    public bool IsTileComplite;
    public string Progress = "Complite";
        
    private string archiveUrl => LowTileLoadUtilites.GetLowTexServerPath();
    private string archiveName => LowTileLoadUtilites.GetLowTexArchiveName();
    private string localZipPath => Path.Combine(Application.persistentDataPath, archiveName);
    private string targetPath => LowTileLoadUtilites.GetLowTexLocalPath();
    
    
    // ✅ Публичный метод для вызова извне
    public async UniTask InitializeAsinc(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (!IsValidLowTilesDirectory(targetPath))
        {
            Debug.Log("📦 LowTiles не найдены, начинаем загрузку архива...");
            if (!await DownloadAndExtractTiles(targetPath, token))
                return;
        }
        else
        {
            Debug.Log("✅ LowTiles уже есть, пропускаем загрузку.");
        }

        token.ThrowIfCancellationRequested();
        LowTileLoadUtilites.isLowTilesExist = true;
        IsTileComplite = true;
        TileComplite?.Invoke();
    }
    private async UniTask<bool> DownloadAndExtractTiles(string targetPath, CancellationToken token)
    {
     string temporaryTargetPath = targetPath + ".downloading";
     try
     {
         await DownloadFileAsync(archiveUrl, localZipPath, token);
         token.ThrowIfCancellationRequested();
         Debug.Log("🧩 Распаковка архива...");

         if (Directory.Exists(temporaryTargetPath))
             Directory.Delete(temporaryTargetPath, true);

         ZipFile.ExtractToDirectory(localZipPath, temporaryTargetPath);
         token.ThrowIfCancellationRequested();
         if (!IsValidLowTilesDirectory(temporaryTargetPath, requireMarker: false))
             throw new InvalidDataException("Распакованный архив LowTiles не содержит JPG-тайлов.");

         File.WriteAllText(Path.Combine(temporaryTargetPath, CompletionMarkerName), "complete");
         if (Directory.Exists(targetPath))
             Directory.Delete(targetPath, true);
         Directory.Move(temporaryTargetPath, targetPath);

         Debug.Log($"✅ LowTiles успешно распакованы в: {targetPath}");
         File.Delete(localZipPath);
         return true;
     }
     catch (OperationCanceledException)
     {
         throw;
     }
     catch (Exception ex)
     {
         Debug.LogError($"❌ Ошибка при загрузке или распаковке: {ex.Message}");
         return false;
     }
     finally
     {
         try
         {
             if (Directory.Exists(temporaryTargetPath))
                 Directory.Delete(temporaryTargetPath, true);
         }
         catch (Exception cleanupException)
         {
             Debug.LogWarning($"Не удалось удалить временную папку LowTiles: {cleanupException.Message}");
         }
     }
    }

    private static bool IsValidLowTilesDirectory(string path, bool requireMarker = true)
    {
        if (!Directory.Exists(path))
            return false;

        if (requireMarker && File.Exists(Path.Combine(path, CompletionMarkerName)))
            return true;

        foreach (string unused in Directory.EnumerateFiles(path, "*.jpg", SearchOption.AllDirectories))
            return true;

        return false;
    }
    private async UniTask DownloadFileAsync(string url, string outputPath, CancellationToken token)
     {
         Debug.Log($"⬇️ Загрузка архива с {url}");
         using (UnityWebRequest request = UnityWebRequest.Get(url))
         {
             var operation = request.SendWebRequest();
             int lastReportedPercent = -1;
             while (!operation.isDone)
             {
                 if (token.IsCancellationRequested)
                 {
                     request.Abort();
                     token.ThrowIfCancellationRequested();
                 }

                 await UniTask.Yield();
                 int percent = Mathf.FloorToInt(request.downloadProgress * 100f);
                 if (percent != lastReportedPercent)
                 {
                     lastReportedPercent = percent;
                     Progress = percent + "%";
                     Debug.Log($"Загрузка: {Progress}");
                 }
             }

             token.ThrowIfCancellationRequested();
             if (request.result != UnityWebRequest.Result.Success)
             {
                 throw new Exception($"Ошибка загрузки: {request.error}");
             }

             File.WriteAllBytes(outputPath, request.downloadHandler.data);
             Debug.Log($"💾 Архив сохранён: {outputPath}");
         }
     }
}
