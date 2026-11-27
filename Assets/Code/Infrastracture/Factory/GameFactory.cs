using Code.Infrastracture.AssetManagment;
using UnityEngine;

namespace Code.Infrastracture.Factory
{
    public class GameFactory : IGameFactory
    {
        private IAssets _assetProvider;
        public GameFactory (IAssets assetProvider)
        {
            _assetProvider = assetProvider;
        }
        
        
        public GameObject CreateHero()
        {
            return InstantiateHero();
        }
    
        private GameObject InstantiateHero()
        {
            var prefab = _assetProvider.GetHeroPefab();
            var go = GameObject.FindGameObjectWithTag("InitialPoint");
            return GameObject.Instantiate(prefab, go.transform.position, Quaternion.identity);
        }
    }
}