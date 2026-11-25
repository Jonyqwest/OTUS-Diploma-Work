//using Core.Character.States;
//using UnityEngine;

//namespace Core.Character.Controllers
//{
//    [RequireComponent(typeof(Rigidbody))]
//    public class BallCharacterController : MonoBehaviour
//    {
//        [Header("Config")]
//        public ForceProfile forceProfile;
//        public BallCharacterConfig config;
//        public JumpProfile JumpVerticalProfile;
//        public JumpProfile JumpHorizontalProfile;

//        [Header("Physics")]
//        public Rigidbody rb;
//        public SphereCollider sphereCollider;

//        public enum CharacterState
//        {
//            Grounded,
//            Running,
//            Jumping,
//            Slide,
//            Flight,
//            Dart
//        }
//        public CharacterState currentState = CharacterState.Grounded;


//        JumpModule jumpMod;
//        GroundModule groundMod;

//        private int jumpCount = 0;
//        private bool isGrounded;

//        private Vector3 controlDirection;

//        private FlightState flightState;
//        private bool isFlyState;
//        private JumpProfile currentJumpProfile;

//        private Vector3 lockedMoveDirection = Vector3.forward; // фиксированное направление
//        private bool isDirectionLocked = false;

//        private Vector3 orientation = Vector3.forward; // ← ТЕКУЩАЯ ОРИЕНТАЦИЯ ПЕРСОНАЖА
//        private Vector3 lockedOrientation;
//        private bool wasFreeLookLastFrame = false;

//        private void Awake()
//        {
//            if (!rb) rb = GetComponent<Rigidbody>();
//            if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();

//            flightState = new FlightState(rb, config);

//            groundMod   =     new GroundModule();
//            jumpMod     =     new JumpModule(rb);

//            GetComponent<BallGroundSensor>()
//            .Bind(groundMod);
//        }

//        //private void FixedUpdate()
//        //{

            


//        //    //UpdateGrounded();



//        //    if (currentState == CharacterState.Flight)
//        //    {
//        //        flightState.Tick(controlDirection);
//        //    }
//        //}
         

//        public void ApplyMovement(MovementIntent intent, float dt)
//        {
//            // 1. Ориентация
//            UpdateOrientation(intent, dt);
             
//            //  перемещение прикладывается ОТНОСИТЕЛЬНО ОРИЕНТАЦИИ:
//                Vector3 worldForceDir = Quaternion.LookRotation(orientation) * intent.move;

//            // Горизонтальная скорость
//            Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
//            float horizontalSpeed = horizontalVel.magnitude;
//            float blendForceSpeed = 1 - Mathf.Clamp01(horizontalSpeed / config.moveWalke);

//             //─── СЛОЙ 1: Позиционирование(низкие скорости) ───
//            if (intent.move.magnitude > 0.01f)
//            {
//                Vector3 delta = worldForceDir * config.maxWalkeSpeed * Time.fixedDeltaTime * blendForceSpeed;
//                rb.MovePosition(rb.position + delta);
//            }

//            // ─── СЛОЙ 2: Физическое ускорение ───
//            if (intent.move.magnitude > 0.01f)
//            {
//                // Проекция текущей скорости НА направление силы ───
//                // Положительная = движемся примерно туда же, куда хотим ускориться
//                // Отрицательная = движемся против направления силы (разворот/торможение)
//                Vector3 worldForceDir_Z = Quaternion.LookRotation(orientation) * new Vector3(0,0,intent.move.normalized.z);
//                Vector3 worldForceDir_X = Quaternion.LookRotation(orientation) * new Vector3(intent.move.normalized.x, 0,0);
//                float speedAlongForce_Z = Vector3.Dot(horizontalVel, worldForceDir_Z);
//                float speedAlongForce_X = Vector3.Dot(horizontalVel, worldForceDir_X);

//                float forceMultiplierZ = 1f;
//                float forceMultiplierX = 1f;

//                if (speedAlongForce_Z > 0f) // Движемся В НАПРАВЛЕНИИ силы → разгон
//                {
//                    // Нормализуем проекцию относительно maxSpeed
//                    float normalizedProjectedSpeed = Mathf.Clamp01(speedAlongForce_Z / config.maxRunSpeed);
//                    // Ограничиваем силу ТОЛЬКО при разгоне в направлении движения
//                    forceMultiplierZ = forceProfile.StartCurve.Evaluate(normalizedProjectedSpeed);
//                }

//                if (speedAlongForce_X > 0f) // Движемся В НАПРАВЛЕНИИ силы → разгон
//                {
//                    // Нормализуем проекцию относительно maxSpeed
//                    float normalizedProjectedSpeed = Mathf.Clamp01(speedAlongForce_X / config.maxRunSpeed);
//                    // Ограничиваем силу ТОЛЬКО при разгоне в направлении движения
//                    forceMultiplierX = forceProfile.StartCurve.Evaluate(normalizedProjectedSpeed);
//                }



//                Vector3 force = worldForceDir_Z * config.acceleration * forceMultiplierZ
//                              + worldForceDir_X * config.acceleration * forceMultiplierX;

//                rb.AddForce(force, ForceMode.Acceleration);
//            }

//            Vector3 horizontalVel_end = new Vector3(rb.velocity.x, 0, rb.velocity.z);
//            float horizontalSpeed_end = horizontalVel_end.magnitude;

//            // 🔑 Ключ: разрешаем превышение на 10-15% ТОЛЬКО для манёвров
//            float maxAllowedSpeed = config.maxRunSpeed * 1.15f;

//            if (horizontalSpeed_end > maxAllowedSpeed)
//            {
//                // Плавно «стравливаем» избыток скорости
//                float excess = horizontalSpeed_end - maxAllowedSpeed;
//                Vector3 damping = -horizontalVel_end.normalized * excess * 0.8f; // 0.8 = плавность

//                rb.velocity += new Vector3(damping.x, 0, damping.z);
//            }

//                //гроунд
//                groundMod.Tick();
//            //Debug.Log("!!!groundState = " + groundMod.ContactType);
//            if (groundMod.State.justLanded)
//            { 
//                //Debug.Log("[BallCharacter] is justLanded");
//                jumpMod.OnGrounded();
//            }

//            //прыжок
//            if (intent.jumpPressed
//                & groundMod.State.isGrounded)
//            {
//                jumpMod.OnGrounded();
//                if (horizontalSpeed < config.moveStanding * config.moveStanding)
//                {
//                    //Debug.Log("прыжек Upp");
//                    currentJumpProfile = JumpVerticalProfile;
//                }
//                else { 
//                    //Debug.Log("прыжек Forvard");
//                    currentJumpProfile = JumpHorizontalProfile;
//                }
//                jumpMod.TryStartJump(intent, config.jumpForce, currentJumpProfile);
//            }
//            jumpMod.Tick(intent, dt);



//            //if (horizontalSpeed < config.moveWalke * config.moveWalke)
//            //{
//            //    //для малых скоростей перемещение = смещение
//            //    var x = intent.move.x;
//            //    Vector3 delta =
//            //        intent.move * config.maxWalkeSpeed * Time.fixedDeltaTime;

//            //    rb.MovePosition(rb.position + delta);
//            //    Debug.Log("Малая скорость");
//            //}
//            //else
//            //{
//            //    //для больших скоростей
//            //    Vector3 force =
//            //        intent.move * config.acceleration;

//            //    rb.AddForce(force, ForceMode.Acceleration);
//            //    Debug.Log("Большая скорость");
//            //}

//            //HandleJump(intent);
//        }

//        private void UpdateControlDirection(MovementIntent intent)
//        { 
//            // ─── Определяем целевое направление движения ───
//            Vector3 targetDirection;

//            if (intent.freeLook)
//            {
//                // 🔒 Free Look: фиксируем направление ПРИ ПЕРЕХОДЕ В РЕЖИМ
//                if (!isDirectionLocked)
//                {
//                    // Запоминаем текущее направление взгляда как направление движения
//                    lockedMoveDirection = intent.look.normalized;
//                    isDirectionLocked = true;
//                }
//                targetDirection = lockedMoveDirection;
//            }
//            else
//            {
//                // 🔓 Обычный режим: следуем за взглядом
//                isDirectionLocked = false;
//                targetDirection = intent.look.normalized;
//            }

//            // ─── ПЛАВНЫЙ ПОВОРОТ к целевому направлению ───
//            // Важно: даже в Free Look поворот должен быть плавным (иначе рывок при входе)
//            float turnSpeed = config.maxTurnSpeed;

//            // На высоких скоростях поворот МЕДЛЕННЕЕ (сохраняем физику)
//            float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
//            float speedFactor = 1f - Mathf.Clamp01(horizontalSpeed / config.maxFlySpeed);
//            turnSpeed *= Mathf.Lerp(config.minTurnSpeed, 1f, speedFactor);

//            controlDirection = Vector3.RotateTowards(
//                controlDirection,
//                targetDirection,
//                turnSpeed * Time.fixedDeltaTime,
//                0f
//            ).normalized;
//        }

//        private void HandleJump(MovementIntent intent)
//        {
           
//            //if (!intent.jump) return;
            
//            //if (intent.jump)
//            //{
//            //    TryJumpOrFlight();
//            //}
//        }

//        //void TryStartJump(MovementIntent intent)
//        //{
//        //    if (jumpPhase != JumpPhase.Grounded)
//        //        return;

//        //    if (!intent.jumpPressed)
//        //        return;

//        //    jumpPhase = JumpPhase.Rising;
//        //    jumpTime = 0f;
//        //    jumpStartY = rb.position.y;

//        //    // 🔒 фиксируем направление
//        //    lockedAirDirection = rb.velocity;
//        //    lockedAirDirection.y = 0f;

//        //    rb.velocity = new Vector3(
//        //        lockedAirDirection.x,
//        //        0f,
//        //        lockedAirDirection.z
//        //    );
//        //}

//        private void TryJumpOrFlight()
//        {
//            if (isGrounded)
//            {
//                // Первый прыжок
//                jumpCount = 1;
//                rb.AddForce(Vector3.up * config.jumpForce, ForceMode.VelocityChange);
//                currentState = CharacterState.Jumping;
//                return;
//            }

//            if (
//                !isGrounded &&
//                jumpCount == 1)
//            {
//                // Второй прыжок → вход в полёт
//                jumpCount = 2;
//                EnterFlightState();
//            }
//        }

//        private void EnterFlightState()
//        {
//            currentState = CharacterState.Flight;

//            rb.drag = config.flightDrag;

//            // вертикальную скорость НЕ трогаем
//            // это важно для ощущения прыжка с обрыва
//        }

//        //public void UpdateGrounded()
//        //{
//        //    isGrounded = Physics.Raycast(
//        //        rb.position,
//        //        Vector3.down,
//        //        config.groundCheckDistance
//        //        //,config.groundMask
//        //    );

//        //    if (isGrounded)
//        //        jumpCount = 0;
//        //}



//        private void UpdateOrientation(MovementIntent intent, float dt)
//        {
//            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
//            float horizontalSpeed = horizontalVelocity.sqrMagnitude;
//            float speedRatio = Mathf.Clamp01(horizontalSpeed / config.maxFlySpeed);//для пробы берем максимальную скорость полета

//            // ─── РЕЖИМ 1: на земле   ───
//            if (groundMod.IsGrounded)
//            {
//                // Ориентация = взгляд (мгновенный поворот)
//                // ПК: мышь, VR: голова, Мобилка: свайп
//                orientation = Vector3.RotateTowards(
//                    orientation,
//                    intent.viewDirection,
//                    config.maxTurnSpeed * 3f * dt, // быстро!
//                    0f
//                ).normalized;
//            }
//            // ─── РЕЖИМ 2: в полете   ───
//            else
//            {
//                // Ориентация = направление скорости (инерция доминирует)
//                Vector3 velocityDir = horizontalVelocity.normalized;

//                // НО: если игрок даёт команду (ввод != 0) — плавно поворачиваем К взгляду
//                if (intent.move.sqrMagnitude > 0.01f && !intent.freeLook)
//                {
//                    // Целевая ориентация = смесь скорости и взгляда
//                    // Чем выше скорость — тем сильнее инерция (медленнее поворот)
//                    float inertiaFactor = Mathf.Lerp(0.2f, 1f, speedRatio); // 0.2 = на макс. скорости почти не поворачиваемся

//                    Vector3 targetOrientation = Vector3.Slerp(
//                        velocityDir,
//                        intent.viewDirection,
//                        inertiaFactor * 0.5f // 50% влияния взгляда даже на скорости
//                    ).normalized;

//                    // Поворот ограничен физикой (медленно!)
//                    float turnSpeed = config.maxTurnSpeed;//* (1f - speedRatio * 0.5f);
//                    orientation = Vector3.RotateTowards(
//                        orientation,
//                        targetOrientation,
//                        turnSpeed * dt,
//                        0f
//                    ).normalized;
//                }
//                else
//                {
//                    // Без ввода — ориентация = скорость (чистая инерция)
//                    orientation = velocityDir;
//                }
//            }

//            // ─── FREE LOOK: фиксируем ориентацию независимо от всего ───
//            if (intent.freeLook)
//            {
//                // При входе в freeLook — запоминаем текущую ориентацию
//                if (!wasFreeLookLastFrame)
//                    lockedOrientation = orientation;

//                orientation = lockedOrientation;
//            }

//            wasFreeLookLastFrame = intent.freeLook;
//        }
//    }


//}
