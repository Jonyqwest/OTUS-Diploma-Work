using Code.Infrastracture.Services;
using UnityEngine;

namespace Code.Infrastracture.AssetManagment
{
    public interface IAssets: IService
    {
        public GameObject GetHeroPefab();
    }
}