using System.Collections.Generic;
using UnityEngine;

namespace Core.Character.Capsule
{
    /// <summary>
    /// Управляет визуальной частью аватара:
    /// 1. Скрытие/отображение мешей при виде от 1-го и 3-го лица.
    /// 2. Скрытие/отображение дебаг-капсулы физического драйвера (PhysicsDriver).
    /// </summary>
    public class AvatarVisuals
    {
        private GameObject _driverObject;
        private readonly List<Renderer> _hideInFirstPerson;
        private readonly List<GameObject> _hideInThirdPerson;
        private bool _isFirstPerson = false;
        public bool IsFirstPerson => _isFirstPerson;

        public AvatarVisuals(
            GameObject driverObject,
            bool showDriverCapsule,
            List<Renderer> hideInFirstPerson,
            List<GameObject> hideInThirdPerson)
        {
            _driverObject = driverObject;
            _hideInFirstPerson = hideInFirstPerson ?? new List<Renderer>();
            _hideInThirdPerson = hideInThirdPerson ?? new List<GameObject>();

            SetDriverCapsuleVisible(showDriverCapsule);
        }

        /// <summary>
        /// Привязывает или обновляет ссылку на объект физического драйвера.
        /// </summary>
        public void SetDriver(GameObject driverObject, bool showDriverCapsule)
        {
            _driverObject = driverObject;
            SetDriverCapsuleVisible(showDriverCapsule);
        }

        /// <summary>
        /// Показывает или скрывает рендереры дебаг-капсулы физического драйвера.
        /// </summary>
        public void SetDriverCapsuleVisible(bool isVisible)
        {
            if (_driverObject == null) return;

            var renderers = _driverObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = isVisible;
                }
            }
        }

        /// <summary>
        /// Переключение в режим от 1-го лица:
        /// Скрывает части тела (голову/шлем), включает элементы 1-го лица (руки/прицел).
        /// </summary>
        public void SetFirstPersonView()
        {
            _isFirstPerson = true;

            for (int i = 0; i < _hideInFirstPerson.Count; i++)
            {
                if (_hideInFirstPerson[i] != null)
                    _hideInFirstPerson[i].enabled = false;
            }

            for (int i = 0; i < _hideInThirdPerson.Count; i++)
            {
                if (_hideInThirdPerson[i] != null)
                    _hideInThirdPerson[i].SetActive(true);
            }
        }

        /// <summary>
        /// Переключение в режим от 3-го лица:
        /// Восстанавливает видимость частей тела, скрывает элементы 1-го лица.
        /// </summary>
        public void SetThirdPersonView()
        {
            _isFirstPerson = false;

            for (int i = 0; i < _hideInFirstPerson.Count; i++)
            {
                if (_hideInFirstPerson[i] != null)
                    _hideInFirstPerson[i].enabled = true;
            }

            for (int i = 0; i < _hideInThirdPerson.Count; i++)
            {
                if (_hideInThirdPerson[i] != null)
                    _hideInThirdPerson[i].SetActive(false);
            }
        }
    }
}
