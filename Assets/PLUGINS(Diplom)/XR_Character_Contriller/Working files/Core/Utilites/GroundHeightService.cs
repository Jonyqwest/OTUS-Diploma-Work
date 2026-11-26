using System;
using UnityEngine;

namespace Core.Character.Spectator
{
    /// <summary>
    /// Глобальный сервис запроса высоты земли для контроллера.
    /// Полностью изолирован от OWT. Сторонние плагины могут подключить сюда свой делегат.
    /// По умолчанию использует Physics.Raycast.
    /// </summary>
    public static class GroundHeightService
    {
        private static Func<Vector3, float> _customProvider;

        public static void SetProvider(Func<Vector3, float> provider)
        {
            _customProvider = provider;
        }

        public static void ClearProvider()
        {
            _customProvider = null;
        }

        /// <summary>
        /// Запрашивает высоту земли по всем слоям (~0).
        /// </summary>
        public static float GetHeight(Vector3 worldPosition)
        {
            return GetHeight(worldPosition, ~0);
        }

        /// <summary>
        /// Запрашивает высоту земли с фильтрацией по слоям.
        /// </summary>
        public static float GetHeight(Vector3 worldPosition, LayerMask groundLayer)
        {
            // 1. Если подключен внешний провайдер (например, OWT GlobalElevation)
            if (_customProvider != null)
            {
                return _customProvider(worldPosition);
            }

            // 2. Стандартный физический луч (Fallback)
            Vector3 rayOrigin = new Vector3(worldPosition.x, worldPosition.y + 500f, worldPosition.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1000f, groundLayer))
            {
                return hit.point.y;
            }

            // 3. Если под нами вообще ничего нет — считаем за уровень моря (Y = 0)
            return 0f;
        }
    }
}