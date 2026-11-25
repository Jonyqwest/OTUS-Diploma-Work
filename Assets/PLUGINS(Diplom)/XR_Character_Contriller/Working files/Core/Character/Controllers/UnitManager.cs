using System.Collections.Generic;
using Core.Character.Capsule;
using UnityEngine;

namespace Core.Character.Controllers
{
    /// <summary>
    /// Собирает всех юнитов в сцене и по Tab поочерёдно передаёт их контроллеру.
    /// Авто-сбор через FindObjectsOfType; порядок можно дополнительно задать вручную.
    /// </summary>
    public class UnitManager : MonoBehaviour
    {
        [Tooltip("Фасад контроллера. Пусто — ищется в детях.")]
        public ControllerFacade controller;

        [Header("Auto cycle (test)")]
        [Tooltip("Автоматически переключать юнитов каждые cycleInterval секунд.")]
        public bool autoCycle = true;
        [Tooltip("Интервал автоматической смены юнита (сек).")]
        public float cycleInterval = 10f;
        [SerializeField]
        private  List<AvatarFacade> _units = new();
        private int _index = -1;
        private float _cycleTimer;

        private void Awake()
        {
            if (controller == null) controller = GetComponentInChildren<ControllerFacade>();
        }

        private void Start()
        {
            RefreshUnits();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
                NextUnit();

            // По Escape переключаемся в режим обозревателя / возвращаемся к юниту
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && controller != null)
            {
                if (controller.IsObserverActive)
                {
                    // Если мы в обозревателе — возвращаемся к юниту
                    NextUnit();
                }
                else
                {
                    // Выходим из юнита в режим обозревателя
                    controller.ReturnToObserver(placeAboveCurrentUnit: true);
                }
            }

            if (autoCycle && cycleInterval > 0f && _units.Count > 0)
            {
                _cycleTimer += Time.deltaTime;
                if (_cycleTimer >= cycleInterval)
                {
                    _cycleTimer = 0f;
                    NextUnit();
                }
            }
        }

        /// <summary>
        /// Заново собирает всех активных юнитов в сцене.
        /// </summary>
        public void RefreshUnits()
        {
            _units.Clear();
            _units.AddRange(FindObjectsOfType<AvatarFacade>());
            _index = FindCurrentIndex();
        }

        /// <summary>
        /// Передаёт контроллеру следующего юнита (по кругу).
        /// </summary>
        public void NextUnit()
        {
            if (_units.Count == 0)
                RefreshUnits();

            if (_units.Count == 0 || controller == null)
                return;

            _index = FindCurrentIndex();
            int next = (_index + 1) % _units.Count;
            _index = next;
            controller.Possess(_units[next]);
            _cycleTimer = 0f;
        }

        /// <summary>
        /// Передаёт контроллеру юнита по индексу.
        /// </summary>
        public void Possess(int index)
        {
            if (index < 0 || index >= _units.Count || controller == null)
                return;

            _index = index;
            controller.Possess(_units[index]);
        }

        /// <summary>
        /// Индекс юнита, которым сейчас управляет контроллер. -1 — никем не управляем.
        /// </summary>
        private int FindCurrentIndex()
        {
            var currentDriver = controller != null ? controller.CurrentDriver : null;
            if (currentDriver == null)
                return -1;

            for (int i = 0; i < _units.Count; i++)
            {
                if (ReferenceEquals(_units[i].Driver, currentDriver))
                    return i;
            }
            return -1;
        }
    }
}
