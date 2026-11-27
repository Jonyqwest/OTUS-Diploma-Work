// using System;
// using System.IO;
// using System.IO.Compression;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.Networking;
// using WorldTile;
//
//
// internal class LowTilesLoader
//     {
//         [System.NonSerialized]
//         public Action TileComplite;
//         public bool IsTileComplite;
//         public string Progress = "Установка";
//         
//         private string archiveUrl => LowTileLoadUtilites.GetLowTexServerPath();
//         private string archiveName => LowTileLoadUtilites.GetLowTexArchiveName();
//         private string localZipPath => Path.Combine(Application.persistentDataPath, archiveName);
//         private string targetPath => LowTileLoadUtilites.GetLowTexLocalPath();
//
//         
//         // ✅ Публичный метод для вызова извне
//         public async UniTask InitializeAsinc()
//         {
//             if (!Directory.Exists(targetPath))
//             {
//                 Debug.Log("📦 LowTiles не найдены, начинаем загрузку архива...");
//                 await DownloadAndExtractTiles(targetPath);
//             }
//             else
//             {
//                 Debug.Log("✅ LowTiles уже есть, пропускаем загрузку.");
//             }
//             LowTileLoadUtilites.isLowTilesExist = true;
//             IsTileComplite = true;
//             TileComplite?.Invoke();
//         }
//
//         private async UniTask DownloadAndExtractTiles(string targetPath)
//         {
//             try
//             {
//                 await DownloadFileAsync(archiveUrl, localZipPath);
//                 Debug.Log("🧩 Распаковка архива...");
//                 ZipFile.ExtractToDirectory(localZipPath, targetPath);
//                 Debug.Log($"✅ LowTiles успешно распакованы в: {targetPath}");
//                 File.Delete(localZipPath);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"❌ Ошибка при загрузке или распаковке: {ex.Message}");
//             }
//         }
//
//         private async UniTask DownloadFileAsync(string url, string outputPath)
//         {
//             Debug.Log($"⬇️ Загрузка архива с {url}");
//             using (UnityWebRequest request = UnityWebRequest.Get(url))
//             {
//                 var operation = request.SendWebRequest();
//                 while (!operation.isDone)
//                 {
//                     await UniTask.Yield();
//                     Progress = $"{(request.downloadProgress * 100f):F1}%";
//                     Debug.Log($"Загрузка: {Progress}");
//                 }
//
//                 if (request.result != UnityWebRequest.Result.Success)
//                 {
//                     throw new Exception($"Ошибка загрузки: {request.error}");
//                 }
//
//                 File.WriteAllBytes(outputPath, request.downloadHandler.data);
//                 Debug.Log($"💾 Архив сохранён: {outputPath}");
//             }
//         }
//     }
//  