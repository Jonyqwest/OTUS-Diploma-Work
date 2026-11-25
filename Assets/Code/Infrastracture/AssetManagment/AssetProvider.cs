using UnityEngine;

namespace Code.Infrastracture.AssetManagment
{
    public class AssetProvider: IAssets
    {
        private string _heroPath = "Hero/Hero";

        public GameObject GetHeroPefab()
        {
            return Resources.Load<GameObject>(_heroPath);
        }
    }
}