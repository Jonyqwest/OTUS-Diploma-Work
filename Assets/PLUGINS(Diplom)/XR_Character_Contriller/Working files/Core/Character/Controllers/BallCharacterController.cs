using Core.Character.States;
using Core.Input.Interfaces;
using UnityEngine;

namespace Core.Character.Controllers
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallCharacterController : MonoBehaviour, IPossessable
    {
        //[Header("runtime.characterConfig")]
        public RunTimeConfigurator runtime;
        private CharacterContext _context;

        [Header("Facade / Camera")]
        [Tooltip("Точка слежения камеры (голова/центр). Если не назначена — камера следит за самим шаром.")]
        public Transform cameraAnchor;

        public Transform CameraTarget => cameraAnchor != null ? cameraAnchor : transform;

        public void OnPossessed()
        {
            enabled = true;
        }

        public void OnUnpossessed()
        {
            enabled = false;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        public enum CharacterState
        {
            Grounded,
            Running,
            Jumping,
            Slide,
            Flight,
            Dart
        }
        public CharacterState CurrentState = CharacterState.Grounded;

        public Vector3 Orientation = Vector3.forward;   // ← ТЕКУЩАЯ ОРИЕНТАЦИЯ ПЕРСОНАЖА
        private SpeedStruct _speed;                     // ← СКОРОСТЬ
        private struct SpeedStruct
        {
            public Vector3 speedVector;                                 
            public Vector3 speedHorVector;                               
            public float SpeedMagnitude => speedVector.magnitude;  
            public float SpeedHorSqrMagnitude => speedHorVector.sqrMagnitude;  
            public float SpeedHorMagnitude => speedHorVector.magnitude;  
        }

        [Header("Physics")] 
        // private Transform _rig;
        private Rigidbody _rb;
        private Vector3 _lastPosition;
        private SphereCollider _sphereCollider;

        private Vector3 _lockedOrientation;
        private bool _wasFreeLookLastFrame = false;

        private JumpModule _jumpMod;
        private GroundModule _groundMod;

        private int _jumpCount = 0;
        private bool _isGrounded;

        private Vector3 _controlDirection;

        private FlightState _flightState;
        private bool _isFlyState;
        private JumpProfile _currentJumpProfile;

        private Vector3 _lockedMoveDirection = Vector3.forward;  
        private bool _isDirectionLocked = false;
        private float _walkSpeed => runtime.characterConfig.maxWalkSpeed;

        
        private void Awake()
        {
            // _rig = transform.parent;

            _context = new CharacterContext(transform);

            if (_rb is null)
            {
                _rb = _context.Ball.GetComponent<Rigidbody>();
            } 
            _rb.centerOfMass = Vector3.zero; // Центр масс строго в центре сферы!
            

            _lastPosition = _context.Ball.position;
            
            if (!_sphereCollider) _sphereCollider = GetComponent<SphereCollider>();

            if (!runtime) runtime = GetComponent<RunTimeConfigurator>();
            if (!runtime) {
                Debug.LogError("RunTime.characterConfigurator не найден!"); enabled = false; return;
            }
       
            
            _flightState = new FlightState(_rb, runtime.characterConfig);
            _groundMod   = new GroundModule();
            _jumpMod     = new JumpModule(_rb);

            var groundSensor = GetComponent<BallGroundSensor>();
            if (groundSensor != null) groundSensor.Bind(_groundMod);
        }

        /// <summary>
        /// Задаёт конфиг извне (для рантайм-спавна PhysicsDriver).
        /// Пересоздаёт FlightState (он кэширует конфиг) и применяет настройки Rigidbody.
        /// </summary>
        public void SetConfig(BallCharacterConfig config)
        {
            if (config == null || runtime == null) return;
            runtime.characterConfig = config;
            _flightState = new FlightState(_rb, config);
            runtime.ApplyRigidbodySettings();
        }

        /// <summary>
        /// Привязывает внешние сенсоры капсулы и перебиндивает GroundModule.
        /// Вызывается после рантайм-спавна, когда Awake уже отработал.
        /// </summary>
        public void SetGroundSensors(Transform front, Transform left, Transform right)
        {
            var sensor = GetComponent<BallGroundSensor>();
            if (sensor == null) return;
            sensor.frontSensor = front;
            sensor.leftSensor = left;
            sensor.rightSensor = right;
            sensor.Bind(_groundMod);
        }


        //// 🔑 Разруливает каждый кадр 
        public void ApplyMovement(ref MovementIntent intent, float dt)
        { 
            // 0. Скорость
            CalculateSpeed();

            // 1. Ориентация
            UpdateOrientation(intent, dt);

            // 2. Перемещение
            RunMovement(intent, dt);

            // 3. Остановка
            StopMovement(intent, dt);
            
            // 4. Прыжек
            Jump(ref intent, dt);
        }
        
        
        private void StopMovement(MovementIntent intent, float dt)
        {
            if (intent.moveRun.sqrMagnitude > 0.0001f && _speed.SpeedHorMagnitude <= runtime.characterConfig.maxWalkSpeed) return;
            
            if (_speed.SpeedHorMagnitude < _walkSpeed)
            {
                runtime.SetWalkMode();
                //при шаге останавливаемся если отпущены кнопки перемешения
                if (intent.moveWalk.magnitude < 0.01f){
                    _rb.linearVelocity = new Vector3(0, _speed.speedVector.y, 0);
                }
            }
            else
            { 
                //при беге останавливаемся если не жмем вперед
                if (intent.moveRun.z > 0) return;
                
                _speed.speedHorVector = Vector3.Lerp(_speed.speedHorVector, _speed.speedHorVector.normalized * _walkSpeed, dt * 4f); // 4f = скорость "успокоения"
                _rb.linearVelocity = new Vector3(_speed.speedHorVector.x, _speed.speedVector.y, _speed.speedHorVector.z);
                _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, dt * 10f);
            }
        }


        public void RunMovement(MovementIntent intent, float dt)
        {
            //ограничиваем по скорости
            if(_speed.SpeedHorMagnitude > runtime.characterConfig.maxRunSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * (runtime.characterConfig.maxRunSpeed);

            
            //─── СЛОЙ 1: Позиционирование(низкие скорости) ───
            if (intent.moveWalk.magnitude > 0.01f)
            {
                runtime.SetWalkMode();
                Vector3 worldMoveDir = Quaternion.LookRotation(Orientation) * intent.moveWalk;
                float blendForceSpeed;
                
                if (intent.walkMode)
                    blendForceSpeed = 1;
                else
                    blendForceSpeed = 1 - Mathf.Clamp01(_speed.SpeedHorMagnitude / runtime.characterConfig.moveWalke);
                
                Vector3 deltaWW = worldMoveDir * (runtime.characterConfig.maxWalkSpeed * dt * blendForceSpeed);
                //смещаем тело
                _rb.MovePosition(_rb.position + deltaWW);
                
                //для VR смещаем голову
                if (DeviceDetectUtility.VrIsAvailable()  & !intent.walkMode)
                {
                    bool isRun = intent.moveRun.sqrMagnitude > 0.0001f || _speed.SpeedHorSqrMagnitude > runtime.characterConfig.maxWalkSpeed;
                    //если не бежим голова главная
                    if(!isRun)
                        deltaWW = _context.Ball.position - _lastPosition;
                    // иначе голова следует за телом
                    deltaWW.y = 0;
                    _context.GetHead().parent.position += -deltaWW;
                }
            }

            // ─── СЛОЙ 2: Физическое ускорение ───
            if (intent.moveRun.magnitude > 0.01f)
            {
                runtime.SetRunMode();
                // Проекция текущей скорости НА направление силы ───
                // Положительная = движемся примерно туда же, куда хотим ускориться
                // Отрицательная = движемся против направления силы (разворот/торможение)
                float intert_Z = intent.moveRun.z;
                
                if (intert_Z > 0.5f &
                    (intent.moveRun.x > 0.5f || intent.moveRun.x < -0.5f) &
                    _speed.SpeedHorMagnitude > runtime.characterConfig.maxWalkSpeed)
                {
                     intert_Z = 0.6f;
                }
                
                Vector3 worldForceDir_Front = Quaternion.LookRotation(Orientation) * new Vector3(0, 0, intert_Z);
                Vector3 worldForceDir_LefrRight = Quaternion.LookRotation(Orientation) * new Vector3(intent.moveRun.x, 0, 0);
                float speedAlongForce_Front = Vector3.Dot(_speed.speedHorVector, worldForceDir_Front);
                float speedAlongForce_X = Vector3.Dot(_speed.speedHorVector, worldForceDir_LefrRight);

                float forceMultiplierZ = 1f;
                float forceMultiplierX = 1f;

                if (speedAlongForce_Front > 0.5f) // Движемся В НАПРАВЛЕНИИ силы → разгон
                {
                    // Нормализуем проекцию относительно maxSpeed
                    float normalizedProjectedSpeed = Mathf.Clamp01(speedAlongForce_Front / runtime.characterConfig.maxRunSpeed);
                    // Ограничиваем силу ТОЛЬКО при разгоне в направлении движения
                    forceMultiplierZ = runtime.forceProfile.StartCurve.Evaluate(normalizedProjectedSpeed);
                }

                if (speedAlongForce_X > 0.5f) // Движемся В ПЕРПЕНДИКУЛЯРНО силы
                {
                    // Нормализуем проекцию относительно maxSpeed
                    float normalizedProjectedSpeed = Mathf.Clamp01(speedAlongForce_X / runtime.characterConfig.maxRunSpeed);
                    // Ограничиваем силу ТОЛЬКО при разгоне в направлении движения
                    forceMultiplierX = runtime.forceProfile.StartCurve.Evaluate(normalizedProjectedSpeed);
                }

                Vector3 force = worldForceDir_Front * runtime.characterConfig.acceleration * forceMultiplierZ
                                + worldForceDir_LefrRight * runtime.characterConfig.acceleration * forceMultiplierX;
                _rb.AddForce(force, ForceMode.Acceleration);
            }
            _lastPosition = _context.Ball.position;
        }

        private void CalculateSpeed()
        {
            _speed.speedVector = _rb.linearVelocity;
            _speed.speedHorVector = new Vector3(_speed.speedVector.x, 0, _speed.speedVector.z);
        }

        public void Jump(ref MovementIntent intent, float dt)
        {
            //проверяем гроунд
            _groundMod.Tick();
            if (_groundMod.State.justLanded)
            {
                _jumpMod.OnGrounded();
            }

            //прыжок - используем bump detection
            if (intent.JumpPressedDequeue()
                & _groundMod.CanJump())
            { 
                _jumpMod.OnGrounded();
                if (_speed.SpeedHorMagnitude < runtime.characterConfig.moveWalke)
                {
                    //Debug.Log("прыжек Upp");
                    _currentJumpProfile = runtime.jumpVerticalProfile;
                }
                else
                {
                    //Debug.Log("прыжек Forvard");
                    _currentJumpProfile = runtime.jumpHorizontalProfile;
                }
                _jumpMod.TryStartJump(intent, runtime.characterConfig.jumpForce, _currentJumpProfile);
            }
            _jumpMod.Tick(intent, dt);

        }
        
        

        //private void UpdateControlDirection(MovementIntent intent)
        //{ 
        //    // ─── Определяем целевое направление движения ───
        //    Vector3 targetDirection;

        //    if (intent.freeLook)
        //    {
        //        // 🔒 Free Look: фиксируем направление ПРИ ПЕРЕХОДЕ В РЕЖИМ
        //        if (!isDirectionLocked)
        //        {
        //            // Запоминаем текущее направление взгляда как направление движения
        //            lockedMoveDirection = intent.look.normalized;
        //            isDirectionLocked = true;
        //        }
        //        targetDirection = lockedMoveDirection;
        //    }
        //    else
        //    {
        //        // 🔓 Обычный режим: следуем за взглядом
        //        isDirectionLocked = false;
        //        targetDirection = intent.look.normalized;
        //    }

        //    // ─── ПЛАВНЫЙ ПОВОРОТ к целевому направлению ───
        //    // Важно: даже в Free Look поворот должен быть плавным (иначе рывок при входе)
        //    float turnSpeed = runtime.characterConfig.maxTurnSpeed;

        //    // На высоких скоростях поворот МЕДЛЕННЕЕ (сохраняем физику)
        //    float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        //    float speedFactor = 1f - Mathf.Clamp01(horizontalSpeed / runtime.characterConfig.maxFlySpeed);
        //    turnSpeed *= Mathf.Lerp(runtime.characterConfig.minTurnSpeed, 1f, speedFactor);

        //    controlDirection = Vector3.RotateTowards(
        //        controlDirection,
        //        targetDirection,
        //        turnSpeed * Time.fixedDeltaTime,
        //        0f
        //    ).normalized;
        //}

        private void HandleJump(MovementIntent intent)
        {
           
            //if (!intent.jump) return;
            
            //if (intent.jump)
            //{
            //    TryJumpOrFlight();
            //}
        }

        //void TryStartJump(MovementIntent intent)
        //{
        //    if (jumpPhase != JumpPhase.Grounded)
        //        return;

        //    if (!intent.jumpPressed)
        //        return;

        //    jumpPhase = JumpPhase.Rising;
        //    jumpTime = 0f;
        //    jumpStartY = rb.position.y;

        //    // 🔒 фиксируем направление
        //    lockedAirDirection = rb.velocity;
        //    lockedAirDirection.y = 0f;

        //    rb.velocity = new Vector3(
        //        lockedAirDirection.x,
        //        0f,
        //        lockedAirDirection.z
        //    );
        //}

        private void TryJumpOrFlight()
        {
            if (_isGrounded)
            {
                // Первый прыжок
                _jumpCount = 1;
                _rb.AddForce(Vector3.up * runtime.characterConfig.jumpForce, ForceMode.VelocityChange);
                CurrentState = CharacterState.Jumping;
                return;
            }

            if (
                !_isGrounded &&
                _jumpCount == 1)
            {
                // Второй прыжок → вход в полёт
                _jumpCount = 2;
                EnterFlightState();
            }
        }

        private void EnterFlightState()
        {
            CurrentState = CharacterState.Flight;

            _rb.linearDamping = runtime.characterConfig.flightDrag;

            // вертикальную скорость НЕ трогаем
            // это важно для ощущения прыжка с обрыва
        }

        



        private void UpdateOrientation(MovementIntent intent, float dt)
        {
            //горизонтальная скорость
            Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);//_speed.speedHorVector;
            float horizontalSpeed = horizontalVelocity.sqrMagnitude;//_speed.SpeedHorMagnitude
            
            float speedRatio = Mathf.Clamp01(horizontalSpeed / runtime.characterConfig.maxFlySpeed);//для пробы берем максимальную скорость полета

            //защита от нулевого вектора
            if (Orientation.sqrMagnitude < 0.0001f)
                Orientation = Vector3.forward;
            
            // ─── РЕЖИМ 1: на земле   ───
            if (_groundMod.IsGrounded)
            {
                // Ориентация = взгляд (мгновенный поворот)
                // ПК: мышь, VR: голова, Мобилка: свайп
                Orientation = Vector3.RotateTowards(
                    Orientation,
                    intent.viewDirection,
                    runtime.characterConfig.maxTurnSpeed * dt, // быстро!
                    0f
                ).normalized;
                //Debug.Log("мы на земле! intent.viewDirection=movementPivot: " + intent.viewDirection);
                //Debug.Log("Orientation: " + Orientation+ " viewDirection: "+ intent.viewDirection + " runtime.characterConfig.maxTurnSpeed * dt: "+ runtime.characterConfig.maxTurnSpeed * dt);
                //Debug.Log("мы на земле! runtime.characterConfig.maxTurnSpeed: " + runtime.characterConfig.maxTurnSpeed);

            }
            // ─── РЕЖИМ 2: в полете   ───
            else
            {
                //// Ориентация = направление скорости (инерция доминирует)
                //Vector3 velocityDir = horizontalVelocity.normalized;

                //// НО: если игрок даёт команду (ввод != 0) — плавно поворачиваем К взгляду
                //if (intent.move.sqrMagnitude > 0.01f && !intent.freeLook)
                //{
                //    // Целевая ориентация = смесь скорости и взгляда
                //    // Чем выше скорость — тем сильнее инерция (медленнее поворот)
                //    float inertiaFactor = Mathf.Lerp(0.2f, 1f, speedRatio); // 0.2 = на макс. скорости почти не поворачиваемся

                //    Vector3 targetOrientation = Vector3.Slerp(
                //        velocityDir,
                //        intent.viewDirection,
                //        inertiaFactor * 0.5f // 50% влияния взгляда даже на скорости
                //    ).normalized;

                //    // Поворот ограничен физикой (медленно!)
                //    float turnSpeed = runtime.characterConfig.maxTurnSpeed;//* (1f - speedRatio * 0.5f);
                //    Orientation = Vector3.RotateTowards(
                //        Orientation,
                //        targetOrientation,
                //        turnSpeed * dt,
                //        0f
                //    ).normalized;
                //}
                //else
                //{
                //    //Без ввода — ориентация = скорость (чистая инерция)
                //    Orientation = velocityDir;
                //}
            }

            //// ─── FREE LOOK: фиксируем ориентацию независимо от всего ───
            //if (intent.freeLook)
            //{
            //    // При входе в freeLook — запоминаем текущую ориентацию
            //    if (!wasFreeLookLastFrame)
            //        lockedOrientation = Orientation;

            //    Orientation = lockedOrientation;
            //}

            //wasFreeLookLastFrame = intent.freeLook;
        }

    }


}
