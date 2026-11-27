using Code.Infrastracture.Services;
using UnityEngine;

namespace Code.Infrastracture.Factory
{
    public interface IGameFactory : IService
    {
        GameObject CreateHero();
    }
}