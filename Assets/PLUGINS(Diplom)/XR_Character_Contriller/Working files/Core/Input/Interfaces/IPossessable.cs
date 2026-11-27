using UnityEngine;

namespace Core.Input.Interfaces
{
    /// <summary>
    /// Контракт любого объекта, над которым Контроллер может взять управление.
    /// </summary>
    public interface IPossessable
    {
        /// <summary>
        /// Точка слежения камеры (голова или трансформ объекта).
        /// </summary>
        Transform CameraTarget { get; }

        /// <summary>
        /// Передача намерений движения (клавиатура / VR / ИИ).
        /// </summary>
        void ApplyMovement(ref MovementIntent intent, float dt);

        /// <summary>
        /// Вызывается при передаче контроля объекту.
        /// </summary>
        void OnPossessed();

        /// <summary>
        /// Вызывается при снятии контроля с объекта.
        /// </summary>
        void OnUnpossessed();
    }
}
