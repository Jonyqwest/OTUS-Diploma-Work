using Core.Camera;
using Core.Input.Interfaces;
using UnityEngine;

namespace Core.Character.Capsule
{
    /// <summary>
    /// Внешняя точка входа юнита для систем: драйвер (невидимая физическая сфера)
    /// и цель камеры (голова капсулы). Сенсоры/конфиг/здоровье — внутренности юнита.
    /// </summary>
    public interface IControllable
    {
        IPossessable Driver { get; }
        Transform CameraTarget { get; }
        CameraViewMode CurrentViewMode { get; set; }
    }
}