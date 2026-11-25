// using Cysharp.Threading.Tasks;
// using UnityEngine;
// // using UnityEngine.AddressableAssets;
// using WorldTile;
//
// public class LowTilesMonobeh : MonoBehaviour
// {
//    
//     LowTilesInitializer lowTilesInitializer;
//
//     private string LowTileProgress;
//     public bool isNeedNextScene;
//     public string sceneToLoad = "World"; // Имя сцены для старта игры
//     private async void Awake()  // ✅ Добавили async
//     {
//         lowTilesInitializer = new LowTilesInitializer();
//         await lowTilesInitializer.Initialize();  // ✅ Добавили await
//     }
//
//     private void OnEnable()
//     {
//         if (lowTilesInitializer.IsTileComplite) OnLowTileComplite();
//         else
//             lowTilesInitializer.TileComplite += OnLowTileComplite;
//     }
//     private void OnDisable()
//     {
//         lowTilesInitializer.TileComplite -= OnLowTileComplite;
//     }
//
//     private async void OnLowTileComplite()
//     {
//         Debug.Log("🚀Low тайлы готовы!");
//         
//         // if (isNeedNextScene)
//         //     await LoadScene(sceneToLoad);
//     }
//
//     // private async UniTask LoadScene(string sceneName)
//     // {
//     //     Debug.Log("Запуск сцены - " + sceneName);
//     //     await Addressables.LoadSceneAsync(sceneName);
//     // }
//     private void OnGUI()
//     {
//         GUI.Label(new Rect(10, 10, 700, 400),
//             $"Скачивание текстур с сервера: {lowTilesInitializer.Progress}");
//     }
// }
