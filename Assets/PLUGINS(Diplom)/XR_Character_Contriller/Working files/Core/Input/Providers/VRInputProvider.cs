using System.Collections.Generic;
using Core.Character.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.InputSystem.InputDevice;

public class VRInputProvider : MonoBehaviour,  IInputProvider
{
    [Header("Head")]
    [SerializeField] 
    private Transform _header;
    [Header("Контроллеры")]
    [SerializeField] 
    private Transform _left;
    [SerializeField] 
    private Transform _right;
    [Header("Body")]
    [SerializeField]
    private Transform _body;

    private BallCharacterController _bodyСontroller;
    // private Transform _bobyRig;
    
    [SerializeField] private MovementPivotController movementPivot;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset _inputActionAsset;
    
    [Header("Jump detection")]
    public float minUpVelocity = 0.5f;          // м/с — начало прыжка
    public float maxIntentSampleTime = 1f;      // макс. длительность намерения
    public float minHeightDelta = 0.1f;        // минимальный прыжок
    public float maxHeightDelta = 0.3f;         // полный прыжок

    [Header("Intent mapping")]
    public float minHoldTime = 0.2f;
    public float maxHoldTime = 1f;

    private float _maxDistanceBetweenHeadBodyMagnitude = 2f;
    
    // состояние
    private float prevHeadY;
    private Vector3 prevHeadPosition;
    private float jumpStartY;
    private float maxHeadY;

    private float intentTimer;
    private float holdTimer;
    private float targetHoldTime;

    private bool samplingJump;
    private bool holdingJump;
    private bool _headJumpStart;
    private bool _headJumpHeld;
    private Vector3 lastBodyRigPosition;
    private Vector3 lastHeadLocalPosition;
    private Vector3 accumulateDelta;
    

    [Header("⚙️Руки над головой")]
    [Tooltip("Сколько метров выше головы должны быть руки для срабатывания")]
    public float threshold = -0.1f;
    [Tooltip("Если true: сработает 1 раз за поднятие. Если false: будет true всё время, пока руки подняты")]
    public bool triggerOncePerRaise = true;
    // Состояние детектора
    public bool IsHandsAboveHead { get; private set; }
    private bool wasAboveHead = false;
    private string _debugText;
    
    private InputAction _moveAction;
    private InputAction _selectAction;
    
    private void Awake()
    {
        if(_bodyСontroller is null) 
            _bodyСontroller = FindObjectOfType<BallCharacterController>();
        
        if(_body is null)
            _body = _bodyСontroller?.transform;
        
        // _bobyRig = _body.parent;
       
        if(_header is null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                _header = cam.transform;
        }

        if (movementPivot is null)
        {
            movementPivot = FindObjectOfType<MovementPivotController>();
        }

        // lastBodyRigPosition = _bobyRig.position;
        if (_header)
            lastHeadLocalPosition = _header.localPosition;
        
        if (_inputActionAsset && DeviceDetectUtility.VrIsAvailable())
        {
            // Найти Move action (Left thumbstick)
            _moveAction = _inputActionAsset.FindAction("Move");
            
            // Включить action
            if (_moveAction != null)
                _moveAction.Enable();
            
            // Найти Select  (Left)
            _selectAction = _inputActionAsset.FindAction("Select");
        }
    }

    /// <summary>
    /// Задаёт голову игрока извне (например, при рантайм-спавне юнитом).
    /// </summary>
    public void SetHead(Transform head)
    {
        _header = head;
        if (_header)
            lastHeadLocalPosition = _header.localPosition;
    }
    private void OnDestroy()
    {
        if (_moveAction != null)
            _moveAction.Disable();
    }
    
    //🧠 ФОРМИРУЕМ НАМЕРЕНИЯ
    public MovementIntent ReadIntent()
    {
        //VR TEST
        //GameObject.Find("декорации").SetActive(false);
        _debugText = "start debug - ";
        MovementIntent intent = new MovementIntent();
        
        float dt = Time.deltaTime;
        if (_header is null || _body is null)
        {
            _debugText += "n/[VRInputProvider] No head or body!";
            Debug.LogError("[VRInputProvider] No head or body!");
            return intent;
        }
        
        
        if (movementPivot is null)
        {
            Debug.LogError("[VRInputProvider] No movement pivot!");
            return intent;
        }
        
        //Направление камеры
        Vector3 fwd = movementPivot.transform.forward;
        fwd.y = 0;
        intent.viewDirection = fwd.normalized;// куда смотрит MovementPivot там и перед
        
        //Режим ходьбы
        if (_selectAction.IsPressed())
            intent.walkMode = true;
        
        //Перемещение
        Vector3 walkDeltaWorld = Vector3.zero;
        walkDeltaWorld = _header.position - _body.position;
        walkDeltaWorld.y = 0;
        if (walkDeltaWorld.sqrMagnitude > 0.0001f)
        {
            //Переводим в пространство ориентации
            Vector3 moveRelative = VectorUtility.WorldToOrientationSpace(walkDeltaWorld, fwd);
            moveRelative.y = 0;
            if (moveRelative.sqrMagnitude < 1)
                intent.moveWalk = moveRelative;
            else
                intent.moveWalk = moveRelative.normalized;
        }
        else
            intent.moveWalk = Vector3.zero;

        
        
        // Компенсация смещения
        // --- вычисляем какое было смещение рига-тела за кадр ---
        // var deltaBodyPosition = _bobyRig.position - lastBodyRigPosition;
        
        // var deltaBodyPosition = BallStaticOffset.Value;
        var deltaBodyPosition = Vector3.zero;
        deltaBodyPosition.y = 0;
        _debugText = "deltaBodyPosition = " + deltaBodyPosition;
        // ставим голову на место двигая родителя противоположно смещению
        
        //очень плохо делать это сдесь: но когда я придумаю куда вынести персчет головы я переделаю
        _header.parent.position -= deltaBodyPosition; 
        
        //плохо очень плохо, но давай тут плавно подвигать голову к телу если тело очень далеко
        // if ((_head.position - _body.position).magnitude > _maxDistanceBetweenHeadBodyMagnitude)
        //     _head.position = Vector3.Lerp(_head.position, _body.position, 0.01f);
        // --- moveRun: thumbstick ---
        // float h = Input.GetAxisRaw("Horizontal");
        // float v = Input.GetAxisRaw("Vertical");
        // Vector3 stickMove = new Vector3(h, 0f, v).normalized;
        // intent.moveRun = stickMove;
        
        // --- viewDirection: head forward ---
        // Vector3 fwd = _head.forward;
        // fwd.y = 0;
        // intent.viewDirection = fwd.normalized;

        // --- jump ---
        float headY = _header.localPosition.y - lastHeadLocalPosition.y;
        float velocityY = (headY - prevHeadY) / dt;

        _headJumpStart = false;
        _headJumpHeld = false;

        // -------------------------------
        // 1️⃣ старт прыжка
        // -------------------------------
        if (!samplingJump && !holdingJump && velocityY > minUpVelocity)
        {
            samplingJump = true;
            intentTimer = 0f;

            jumpStartY = headY;
            maxHeadY = headY;
            
            _headJumpStart = true;
        }

        // -------------------------------
        // 2️⃣ замер амплитуды
        // -------------------------------
        if (samplingJump)
        {
            intentTimer += dt;
            maxHeadY = Mathf.Max(maxHeadY, headY);

            bool headStoppedRising = velocityY <= 0f;
            bool timeExpired = intentTimer >= maxIntentSampleTime;

            if (headStoppedRising || timeExpired)
            {
                float heightDelta = maxHeadY - jumpStartY;

                if (heightDelta >= minHeightDelta)
                {
                    float t = Mathf.InverseLerp(
                        minHeightDelta,
                        maxHeightDelta,
                        heightDelta
                    );

                    targetHoldTime = Mathf.Lerp(
                        minHoldTime,
                        maxHoldTime,
                        t
                    );

                    holdingJump = true;
                    holdTimer = 0f;
                }

                samplingJump = false;
            }
        }

        // -------------------------------
        // 3️⃣ удержание намерения
        // -------------------------------
        if (holdingJump)
        {
            holdTimer += dt;
            _headJumpHeld = true;

            if (holdTimer >= targetHoldTime)
            {
                holdingJump = false;
            }
        }

        prevHeadY = headY;
        prevHeadPosition = _header.position;
        //////
        
        if(_headJumpStart || Input.GetKeyDown(KeyCode.Space))
            intent.SetjumpPressed(true);

        if (_headJumpHeld || Input.GetKey(KeyCode.Space))
            intent.jumpHeld = true;

        //Бег
        if (_moveAction != null)
        {
             Vector2 stickInput = _moveAction.ReadValue<Vector2>();
             if (stickInput.magnitude > 0.01)
             { 
                 if(intent.walkMode) 
                     intent.moveWalk += new Vector3(stickInput.x, 0f, stickInput.y);
                 else
                    intent.moveRun = new Vector3(stickInput.x, 0f, stickInput.y);
             }
             
        }
        // Чтение thumbstick через Gamepad
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 leftStick = gamepad.leftStick.ReadValue();
            if (leftStick.magnitude > 0.1f)
            {
                // Движение
                // intent.moveRun = new Vector3(leftStick.x, 0f, leftStick.y);
                // _debugText = "VR геймпад moveRun = "+intent.moveRun;
                // Тест - скрыть декорации
                //GameObject.Find("декорации").SetActive(false);
            }
        }
        
        // lastBodyRigPosition = _bobyRig.position;
        lastHeadLocalPosition = _header.localPosition;


        LefRighrController();
        
        return intent;
    }
    
    
    
    
    
    
    
    
    void LefRighrController()
    {
        if (_left is null || _right is null || _header is null) return;

        float headY = _header.position.y;
        float leftY = _left.position.y;
        float rightY = _right.position.y;

        bool nowAbove = (leftY > headY + threshold) && (rightY > headY + threshold);

        // Защита от дребезга + логика однократного/постоянного триггера
        if (triggerOncePerRaise)
        {
            if (nowAbove && !wasAboveHead)
            {
                OnHandsRaised();
            }
        }
        
        IsHandsAboveHead = nowAbove;
        wasAboveHead = nowAbove;

        // 🔍 Визуальная отладка (видно в Scene View)
#if UNITY_EDITOR
        Color col = nowAbove ? Color.green : Color.red;
        Debug.DrawLine(_header.position, _left.position, col);
        Debug.DrawLine(_header.position, _right.position, col);
#endif
    }

    void OnHandsRaised()
    {
        Debug.Log("🙌 ОБЕ РУКИ ВЫШЕ ГОЛОВЫ!");
        // ▼ ВАШ КОД ЗДЕСЬ ▼
        // SpawnObject();
        // PlayAnimation();
        // TriggerEvent?.Invoke();
    }
    // Добавить метод для проверки разных CommonUsage
    private Vector2 TryReadThumbstick(XRNode node, InputFeatureUsage<Vector2> usage)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.TryGetFeatureValue(usage, out Vector2 value))
            return value;
        return Vector2.zero;
    }
    
    //public string GetDebugInfo()
    //{
        // var sb = new System.Text.StringBuilder();
        // sb.AppendLine("=== ------ ===");
        //  
        // // Найти все Vector2 feature values
        // var device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        // var characteristics = device.characteristics;
        // // Попробовать стандартные Vector2 usages
        // Vector2[] testValues = new Vector2[4];
        // device.TryGetFeatureValue(CommonUsages.primary2DAxis, out testValues[0]);
        // device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out testValues[1]);
        // // и т.д.
        // foreach (var value in testValues)
        // {
        //     sb.AppendLine(value.ToString());
        // }
        // return sb.ToString();
    //}

    private void OnGUI()
    {
        // //Показывать только если что-то происходит
        // var style = new GUIStyle(GUI.skin.label);
        // style.fontSize = 24;
        // style.normal.textColor = Color.red;
        //
        // GUI.Label(new Rect(400, 400, 1000, 1000),
        //     _debugText+
        //     "\n"+
        //     GetDebugInfo(),
        //     style
        // );
    }
}
