using Core.Character.Spectator;
using UnityEngine;
using WorldTile;

namespace Code.Infrastracture.Bridges
{
    /// <summary>
    /// Проектный мост: связывает генератор высот OWT с сервисом высот контроллера.
    /// Вешается на любой управляющий объект сцены с рельефом (например, WorldManager или SceneContext).
    /// </summary>
    public class OwtHeightBridge : MonoBehaviour
    {
        private void OnEnable()
        {
            // Подключаем глобальный расчет высот OWT к контроллеру
            GroundHeightService.SetProvider(GlobalElevation.GetHeight);
        }

        private void OnDisable()
        {
            // Очищаем при смене или выгрузке сцены
            GroundHeightService.ClearProvider();
        }
    }
}