using UnityEngine;

namespace Core.Character.Capsule
{
    /// <summary>
    /// Отвечает за настройку физических слоев и устранение внутренних конфликтов коллайдеров:
    /// 1. Назначает выбранный слой аватару и физическому драйверу.
    /// 2. Отключает взаимные столкновения между коллайдерами аватара и драйвера (устраняет дерганье и скачки).
    /// </summary>
    public static class AvatarCollisionResolver
    {
        /// <summary>
        /// Выполняет полную настройку слоев и игнорирование внутренних коллизий для конкретного юнита.
        /// </summary>
        public static void Setup(GameObject avatarObject, GameObject driverObject, int targetLayer)
        {
            if (avatarObject == null) return;

            // 1. Применяем слой (если targetLayer валиден)
            int layerToApply = (targetLayer >= 0 && targetLayer <= 31) ? targetLayer : avatarObject.layer;
            SetLayerRecursively(avatarObject.transform, layerToApply);

            if (driverObject != null)
            {
                SetLayerRecursively(driverObject.transform, layerToApply);
            }

            // 2. Отключаем столкновения между частями этого конкретного юнита
            ResolveInternalCollisions(avatarObject, driverObject);
        }

        /// <summary>
        /// Рекурсивно выставляет слой объекту и всем его дочерним элементам.
        /// </summary>
        public static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null) return;
            root.gameObject.layer = layer;

            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        /// <summary>
        /// Отключает физические коллизии между коллайдерами аватара и драйвера,
        /// а также между коллайдерами внутри самого аватара.
        /// Разные юниты при этом продолжают полноценно сталкиваться друг с другом!
        /// </summary>
        public static void ResolveInternalCollisions(GameObject avatarObject, GameObject driverObject)
        {
            if (avatarObject == null) return;

            Collider[] avatarColliders = avatarObject.GetComponentsInChildren<Collider>(true);

            // А) Игнорируем столкновения между коллайдерами самого аватара
            for (int i = 0; i < avatarColliders.Length; i++)
            {
                if (avatarColliders[i] == null) continue;

                for (int j = i + 1; j < avatarColliders.Length; j++)
                {
                    if (avatarColliders[j] == null) continue;
                    Physics.IgnoreCollision(avatarColliders[i], avatarColliders[j], true);
                }
            }

            if (driverObject == null) return;

            Collider[] driverColliders = driverObject.GetComponentsInChildren<Collider>(true);

            // Б) Игнорируем столкновения каждого коллайдера аватара с каждым коллайдером драйвера
            for (int i = 0; i < avatarColliders.Length; i++)
            {
                if (avatarColliders[i] == null) continue;

                for (int j = 0; j < driverColliders.Length; j++)
                {
                    if (driverColliders[j] == null) continue;
                    Physics.IgnoreCollision(avatarColliders[i], driverColliders[j], true);
                }
            }
        }
    }
}
