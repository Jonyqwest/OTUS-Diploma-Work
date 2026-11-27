using Core.Character.Capsule;
using Core.Input.Interfaces;
using UnityEngine;

namespace Core.Character.Controllers
{
    /// <summary>
    /// Главный Фасад Контроллера.
    /// Единственная точка входа для внешних систем: принимает юнит (IUnitFasade),
    /// перенацеливает ввод на его драйвер (сферу) и камеру на его голову.
    /// Роутер ввода живёт в spherePrefab рядом с контроллером (один контроллер — один роутер).
    /// </summary>
    public class ControllerFacade : MonoBehaviour
    {
        [Tooltip("Фасад камеры. Пусто — ищется в детях.")]
        public CameraFasade cameraFasade;
        
        [Tooltip("Обьект для кправления с IControllable (если пусто управляем Обзором)")]
        public GameObject ControllableGameObject; 

        private GameObject _lastControllableGameObject;
        private IControllable ControllableTarget;
        
        public bool IsObserverActive => _unit != null && _unit == _observerUnit;
        private IControllable _observerUnit;
        private IControllable _unit;
        private InputRouter _router;

        public IPossessable CurrentDriver => _unit != null ? _unit.Driver : null;

        private void Awake()
        {
            
            if (cameraFasade == null) cameraFasade = GetComponentInChildren<CameraFasade>();

            
            _observerUnit = GetComponentInChildren<Core.Character.Observer.HoverObserverBody>();
            // Если цель для управления не задана управляем Обзором (наш Ball_Spectator Обзорвер)!
            if (ControllableGameObject == null)
                ControllableTarget = GetComponentInChildren<IControllable>();
            else
                ControllableTarget = ControllableGameObject.GetComponent<IControllable>();
            ApplyTarget(ControllableTarget);
            
        }


        private void Update()
        {
            // Поддержка смены юнита прямо через Инспектор во время игры (Play Mode)
            if (ControllableGameObject != _lastControllableGameObject)
            {
                ControllableTarget = ControllableGameObject.GetComponentInChildren<IControllable>();
                ApplyTarget(ControllableTarget);
                _lastControllableGameObject = ControllableGameObject;
            }
        }

        private void ApplyTarget(IControllable Controllable)
        {
            if (Controllable is IControllable unit)
                BindUnit(unit);
            else
                Unbind();
        }

        /// <summary>
        /// Передаёт юнит под управление: ввод → драйвер юнита, камера → голова юнита.
        /// </summary>
        public void BindUnit(IControllable unit)
        {
            if (unit == null)
            {
                Unbind();
                return;
            }

            if (ReferenceEquals(_unit, unit) && _router != null)
                return;

            Unbind();

            _unit = unit;
            var driver = unit.Driver as Component;
            if (driver != null)
                _router = driver.GetComponent<InputRouter>();

            if (_router != null && unit.Driver != null)
                _router.SetTarget(unit.Driver);

            if (cameraFasade != null)
                cameraFasade.SwitchUnitTarget(unit);
        }

        /// <summary>
        /// Снимает управление с текущего юнита.
        /// </summary>
        public void Unbind()
        {
            if (_router != null)
            {
                _router.SetTarget(null);
                _router = null;
            }

            _unit = null;
        }

        /// <summary>
        /// Передаёт юнит под управление.
        /// </summary>
        public void Possess(IControllable unit)
        {
            BindUnit(unit);
        }

        /// <summary>
        /// Снимает управление с текущего юнита.
        /// </summary>
        public void Unpossess()
        {
            Unbind();
        }
        
        /// <summary>
        /// Возвращает управление режиму обозревателя (HoverObserverBody),
        /// опционально подтягивая его позицию над текущим юнитом.
        /// </summary>
        public void ReturnToObserver(bool placeAboveCurrentUnit = true)
        {
            if (_observerUnit == null)
                _observerUnit = GetComponentInChildren<Core.Character.Observer.HoverObserverBody>();

            if (_observerUnit == null)
            {
                Debug.LogWarning("[ControllerFacade] HoverObserverBody не найден в детях контроллера!");
                return;
            }

            // Если мы уже в режиме обозревателя — ничего делать не нужно
            if (_unit == _observerUnit) return;

            // Опционально: перед переключением позиционируем спектатора сзади и сверху относительно взгляда камеры, сохраняя фокус на персонаже
            if (placeAboveCurrentUnit && _unit != null && _observerUnit is Component observerComp)
            {
                Transform currentTargetTransform = _unit.CameraTarget;
                if (currentTargetTransform != null)
                {
                    Vector3 currentPos = currentTargetTransform.position;

                    // Получаем реальное направление взгляда активной камеры прямо сейчас
                    Vector3 viewForward = Vector3.forward;
                    if (cameraFasade != null && cameraFasade.rig != null)
                    {
                        if (cameraFasade.rig.CameraMono != null && cameraFasade.rig.CameraMono.isActiveAndEnabled)
                            viewForward = cameraFasade.rig.CameraMono.transform.forward;
                        else if (cameraFasade.rig.headPivot != null)
                            viewForward = cameraFasade.rig.headPivot.forward;
                    }

                    // Проецируем взгляд камеры на горизонт
                    Vector3 horizontalView = Vector3.ProjectOnPlane(viewForward, Vector3.up).normalized;
                    if (horizontalView.sqrMagnitude < 0.001f)
                        horizontalView = Vector3.forward;

                    // Отступаем назад относительно взгляда игрока и поднимаемся вверх
                    Vector3 spectatorPos = currentPos - (horizontalView * 15f) + (Vector3.up * 18f);
                    observerComp.transform.position = spectatorPos;

                    // 🎬 Кинематографичный фокус: смотрим строго в ту же точку на персонаже!
                    Vector3 lookAtTarget = currentPos - spectatorPos;
                    if (lookAtTarget.sqrMagnitude > 0.001f)
                        observerComp.transform.rotation = Quaternion.LookRotation(lookAtTarget);
                }
            }

            BindUnit(_observerUnit);
        }
        
    }
}
