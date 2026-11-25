// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;
//
// public class playerForceControler : MonoBehaviour
// {
//     public const float resistMove = 12;
//     public const float resistHold = 15;
//
//     public static bool isMoveAxelerate;
//
//     private GameObject player;
//     private Rigidbody rb;
//     private Camera cam;
//     private const float PI = 3.1415926535f;
//     private const float PIx180 = 57.295779514719953f;
//     private const float PI_180 = 0.017453292519444f;
//    
//     private bool isPlane = false;
//     private bool isJump = false;
//     private bool isDart = false;
//     private bool isPressAxelerate = false;
//
//     private Quaternion planeAngle;
//     private float angle; //угол наклона крыла
//     private float angle0; //угол наклона крыла изначально
//     private float flydrive = 0; //скорость двигателя
//     private Transform orientationPos;
//
//     //кривые для бега и полета
//     public AnimationCurve startCurve; //кривая разгона бега
//     public AnimationCurve rotateCurve; //кривая поворота при беге от скорости
//     public AnimationCurve frictionCurve; //кривая остаковки
//     public AnimationCurve stopCurve; //кривая торможения на земле
//     public AnimationCurve flyCurve; //кривая разгона бега
//
//     //joystic
//     //public FloatingJoystick joystickLeft;
//     //public FloatingJoystick joystickRight;
//
//     //крыло для вращения
//     private Transform body;
//     private Transform bodyRotation;
//     private Transform ball;
//
//
//     //controls
//     //private Controls controls;
//     private float moveLeftRight;
//     private float moveFrontBack;
//     private float flyLeftRight;
//     private float flyFrontBack;
//
//     //булевы определители что нажато
//     private bool isPressLeftArrow  = false;
//     private bool isPressRightArrow = false;
//
//
//     private void Awake()
//     {
//         //controls = new Controls();
//         ////jump
//         //controls.Main.Jump.performed += context => Jump();
//         //controls.Main.Jump.canceled += context => JumpDone();
//         ////dart
//         //controls.Main.Dive.performed += context => Dart();
//         //controls.Main.Dive.canceled += context => DartDone();
//         ////axelerate
//         //controls.Main.Axelerate.performed += context => Axelerate();
//         //controls.Main.Axelerate.canceled += context => AxelerateDone();
//     }
//
//
//     private void OnEnable()
//     {
//         //controls.Enable();
//
//         //btnJump.onJumpPessed += Jump;
//         //btnJump.onJumpDone += JumpDone;
//
//         ////btnDart.onDartPressed += Dart;
//         ////btnDart.onDartDone += DartDone;
//
//         //btnLeftArrow.onPessed += PressLeftArrow;
//         //btnLeftArrow.onDone += DropLeftArrow;
//
//         //btnRightArrow.onPessed += PressRightArrow;
//         //btnRightArrow.onDone += DropRightArrow;
//
//     }
//
//     private void OnDisable()
//     {
//         //controls.Disable();
//         //btnJump.onJumpPessed -= Jump;
//         //btnJump.onJumpDone -= JumpDone;
//
//         ////btnDart.onDartPressed -= Dart;
//         ////btnDart.onDartDone -= DartDone;
//
//         //btnLeftArrow.onPessed -= PressLeftArrow;
//         //btnLeftArrow.onDone -= DropLeftArrow;
//
//         //btnRightArrow.onPessed -= PressRightArrow;
//         //btnRightArrow.onDone -= DropRightArrow;
//     }
//
//
//     void Start()
//     {
//         //cam = Camera.main.GetComponent<StereoCamera>();
//
//         player = GameObject.Find("PLAYER");
//         ball = player.transform.Find("BALL");
//         body = player.transform.Find("BODY");
//         bodyRotation = current.BODY_CENTER;
//         rb = ball.GetComponent<Rigidbody>();
//  
//         orientationPos = GameObject.Find("BODY_ORIENTATION").transform;
//         orientationPos.localPosition = new Vector3(0, 0, 1);
//
//         planeAngle = bodyRotation.localRotation;
//         angle0 = planeAngle.z;
//
//     }
//
//
//     // FixUpdate is called once per frame
//     void FixedUpdate()
//     {
//         
//         //if (!recTrackStat.isControlActive) return;
//
//         if (rb.isKinematic)
//             rb.isKinematic = false;
//
//         if (isJump)
//         {
//             if (current.isPlane)
//             {
//                 Vector3 _vUp = new Vector3(0, 1, 0);
//                 rb.AddForce(_vUp * current.forceFly * 1.41f);
//                 if(current.speedFrontMagnitude > 1)
//                     rb.AddForce(current.V3_orientalSpeed * -current.forceFly * 0.3f);
//             }
//         };
//
//
//         if (!current.isPlane)
//         {
//             if (current.isHold)
//             {
//                 rb.drag = resistHold;
//             }
//             else
//             {
//                 if (current.speedUp < 0)
//                     rb.drag = 0.0001f;
//                 else
//                     rb.drag = resistMove;
//
//                 if(spiralStat.isTrigerSpiral)
//                     rb.drag = 0;
//             }
//
//             flydrive = Regressdriver(current.resistenceOfLanding);
//
//
//             MoveFrontBack();
//             MoveLeftRight();
//
//             MoveAxelerate();
//         }
//         else
//         {
//             isMoveAxelerate = false;
//             //btnFrontBackArrow.StopAxelerate();
//
//             FlyFrontBack();
//             FlyLeftRight();
//         }
//
//         if (current.isPlane & !current.isLanding)
//         {
//             if (current.speedUp < -current.gravity)
//             {
//                 rb.useGravity = false; //парим
//                 rb.drag = 0;
//                 rb.angularDrag = 0;
//             }
//             else
//             {
//                 rb.useGravity = true; //падаем
//             }
//         }
//         else
//         {
//             rb.useGravity = true; //падаем
//         }
//
//
//         if (current.isDart & !current.isLanding)
//         {
//             Vector3 _vDown = new Vector3(0, -1, 0);
//             rb.AddForce(_vDown * current.forceDart); //импульс вниз
//             rb.useGravity = true; //парим
//         }
//
//
//         if (current.isPike)
//         {
//             //Debug.Log("пикируем?");
//             current.isPlane = false;
//             Vector3 _vUp = new Vector3(0, 1, 0);
//
//             rb.AddForce(_vUp * current.forcePike * current.dartAcceleration * 2);
//             if (current.speed<200) rb.AddForce(current.V3_orientalSpeed * current.forcePike * current.dartAcceleration);
//             if (rb.velocity.y > 0)
//             {
//                 current.isDart = false;
//                 current.isPike = false;
//                 //current.isPlane = true;
//             }
//
//         }
//
//         if (spiralStat.isTrigerSpiral)
//         {
//             //rb.useGravity = false;  
//         }
//
//     }
//
//
//
//     public void Jump()
//     {
//         //if (!recTrackStat.isControlActive) return;
//
//         current.isLanding = false;
//         isDart = false;
//         //если прыжка до этого небыло
//         if (!isJump)
//         {
//             if ((current.isJump1)&(!current.isJump2)) current.isJump2 = true; //если первый прыжек еще был а второй еще не был
//             if (!current.isJump1) current.isJump1 = true; //если первый прыжек еще не сделан
//
//             if(!current.isPlane)
//                 current.isPlane = false;
//
//             if (current.isJump2)
//             {
//                 current.isTwoJump = true;
//                 current.isPlane = true;
//             }
//
//             isJump = true;
//             current.isJump = true;
//
//             
//                 rb.drag = 0;
//                 current.isJump = true;
//             
//
//             if (!current.isTwoJump) { 
//                 Vector3 _vUp = new Vector3(0, 1, 0);
//                 rb.AddForce(_vUp * current.forceJump, ForceMode.Impulse); //импульс вверх
//             }
//
//             if (current.isHold)
//             {
//                 rb.AddForce(current.V3_orientalSpeed * current.forceForward); //импульс вверх и вперед
//             }
//
//             Invoke("firstJumpTimer", 0.7f);
//         }
//     }
//
//
//     public void JumpDone()
//     {
//         isJump = false;
//         current.isJump = false;
//         current.isJump2 = false;
//     }
//
//
//
//     public void Dart()
//     {
//         //if (!recTrackStat.isControlActive) return;
//
//         if (current.speedMagnitude > current.runMaxSpeed) { current.isDart = true; }
//         else current.isLanding = true;
//         current.isPike = false;
//         current.forceDart = current.forceMaxDart;
//         rb.useGravity = true; //падаем
//         Vector3 _vDown = new Vector3(0, -1, 0);
//         //rb.AddForce(_vDown * current.forceDart * (current.speedMagnitude * 0.006f + 60)*2.8f); //импульс вниз
//     }
//
//     public void DartDone()
//     {
//         isDart = false;
//         current.forceDart = 0;
//         if(!current.isLanding)
//             current.isPike = true;
//     }
//
//     void Axelerate()
//     {
//         isPressAxelerate = true;
//     }
//     void AxelerateDone()
//     {
//         isPressAxelerate = false;
//     }
//
//     void MoveAxelerate()
//     {
//         //if (!recTrackStat.isControlActive) return;
//         ////если нажато ускорение
//         //if (btnFrontBackArrow.GetMoveValue() == 2 || isPressAxelerate)
//         //{
//         //    //если топливо есть
//         //    if (true)
//         //    {
//         //        //движемся с ускорением
//         //        isMoveAxelerate = true; 
//         //        rb.drag = 0;
//         //        float _forceAxelerate = forceCurve(current.axelerateMaxSpeed, "startCurve");
//         //        rb.AddForce(current.V3_orientalForce * (_forceAxelerate * current.axelerateForce));
//         //    }
//         //    //если топлива нет
//         //    else
//         //    {
//         //        isMoveAxelerate = false;
//         //    }
//         //}
//         //else
//         //    isMoveAxelerate =false;
//     }
//
//         void MoveFrontBack()
//     {
//         //moveFrontBack = btnFrontBackArrow.GetMoveValue();
//         //if (controls.Main.MoveFrontBack.ReadValue<float>() != 0) moveFrontBack = controls.Main.MoveFrontBack.ReadValue<float>();
//
//         if (moveFrontBack != 0) rb.drag = 0;
//
//         if (moveFrontBack > 0)
//         {
//             float _forceFromSpeed = forceCurve(current.runMaxSpeed, "startCurve");
//             rb.AddForce(current.V3_orientalForce * (_forceFromSpeed * current.forceForward + current.forceDart));
//         }
//         if (moveFrontBack < 0)
//         {
//             current.isPressBack = true;
//
//             float _forceFromSpeed = 1;
//             if (current.invertMove == -1) { _forceFromSpeed = forceCurve(20, "stopCurve"); }
//             rb.AddForce(-current.V3_orientalForce * _forceFromSpeed * current.forceBack);
//         }
//         else
//         {
//             current.isPressBack = false;
//         }
//     }
//
//
//     void MoveLeftRight()
//     {
//
//         moveLeftRight = 0;
//         if (isPressLeftArrow)
//             moveLeftRight = 1;
//         if (isPressRightArrow)
//             moveLeftRight = -1;
//         //if(joystickRight.Horizontal != 0 )
//         //    moveLeftRight = -joystickRight.Horizontal;
//         //if (controls.Main.MoveLeftRight.ReadValue<float>() != 0)
//             //moveLeftRight = controls.Main.MoveLeftRight.ReadValue<float>();
//
//         if (current.isHold)
//         {
//             Quaternion _r = Quaternion.AngleAxis(-moveLeftRight, Vector3.up);
//             body.transform.rotation *= _r;
//         }
//         else
//         {
//             Vector3 _forceLR = current.V3_perpendikularSpeed * moveLeftRight * spiralStat.SpiralInverce * current.forceRotate;// * (1.2f - forceCurve(current.runMaxSpeed, "rotate")) ;
//             rb.AddForce(_forceLR);
//         }
//
//         angle += (moveLeftRight - angle) * 0.1f;
//         planeAngle.z = angle0 + PI_180 * angle * current.maxRunAngle * (1 - forceCurve(current.runMaxSpeed, "rotate"));
//         bodyRotation.localRotation = planeAngle;
//     }
//
//
//
//
//
//
//     void FlyFrontBack()
//     { 
//         //flyFrontBack = joystickLeft.Vertical;
//         //if (controls.Main.MoveFrontBack.ReadValue<float>() != 0) flyFrontBack = controls.Main.MoveFrontBack.ReadValue<float>();
//
//         if (current.V3_orientalSpeed.y > -.9f)
//             flyFrontBack = 1;//летим всегда вперед
//         else
//         {
//             flyFrontBack = -1;
//         }
//
//         if (flyFrontBack > 0)
//         {
//             float _forceFromSpeed = forceCurve(current.flyMaxSpeed, "flyCurve");
//             float _forceFromTime = Progressdriver(current.flyTimeMaxSpeed, flyFrontBack);
//             rb.AddForce(current.V3_orientalSpeed * (_forceFromTime * _forceFromSpeed * current.forceFly + current.forceDart));
//         }
//         if (flyFrontBack < 0)
//         {
//             current.isPressBack = true;
//
//             float _forceFromSpeed = 1;
//             if (current.invertMove == -1) { _forceFromSpeed = forceCurve(20, "stopCurve"); }
//             rb.AddForce(-current.V3_orientalSpeed * _forceFromSpeed * current.forceFly);
//         }
//         else
//         {
//             current.isPressBack = false;
//         }
//
//     }
//
//
//     void FlyLeftRight()
//     {
//         flyLeftRight = 0;
//         if (isPressLeftArrow)
//             flyLeftRight = 1;
//         if (isPressRightArrow)
//             flyLeftRight = -1;
//         //flyLeftRight = -joystickRight.Horizontal;
//         //if (controls.Main.MoveLeftRight.ReadValue<float>() != 0) flyLeftRight = controls.Main.MoveLeftRight.ReadValue<float>();
//
//      
//         
//         if (current.V3_orientalSpeed.y > -.9f)
//             rb.AddForce(current.V3_perpendikularSpeed * flyLeftRight * current.forceFly);
//         else
//         {
//             Debug.Log("летим вниз вращаемся вокруг себя");
//             float pikeLeftRight = current.V3_orientalSpeed.y * current.V3_orientalSpeed.y;
//             rb.AddForce(current.V3_perpendikularSpeed * flyLeftRight * current.forceFly * pikeLeftRight* 2);
//
//             Quaternion _r = Quaternion.AngleAxis(-flyLeftRight, Vector3.up);
//             body.transform.rotation *= _r;
//         }
//         
//
//         angle += (flyLeftRight - angle) * 0.08f;
//
//         planeAngle.z = angle0 + PI_180 * angle * current.maxAngle * (1 - forceCurve(current.flyMaxSpeed, "rotate"));
//         bodyRotation.localRotation = planeAngle;
//     }
//
//
//
//
//
//
//
//
//    
//
//
//     private float forceCurve(float maxSpeed, string curveName)
//     {
//         //нормализуем Х (мин 0 скорость, макс скорость 1)
//         float _X = current.speedMagnitude / (maxSpeed + 0.001f);
//         if (current.speedMagnitude > maxSpeed) _X = 1;
//
//         //выбор кривой
//         float _curve = 0;
//         if (curveName == "startCurve") _curve = startCurve.Evaluate(_X);
//         if (curveName == "stopCurve") _curve = stopCurve.Evaluate(_X);
//         if (curveName == "runFriction") _curve = startCurve.Evaluate(_X);
//
//
//
//         if (curveName == "flyCurve") _curve = flyCurve.Evaluate(_X);
//         if (curveName == "rotate") _curve = rotateCurve.Evaluate(_X);
//
//
//
//         //нонмализуем Y
//         float _Y = 1 - _curve;
//         return _Y;
//     }
//
//
//
//     float Progressdriver(float time, float movedriver)
//     {
//         float _t = flydrive * time / (movedriver + 0.001f);
//         if ((flydrive > -1) & (flydrive < 1))
//         {
//             if (time > 0)
//             {
//                 flydrive += movedriver * Time.deltaTime / time;
//             }
//         }
//         return flydrive;
//     }
//
//
//     float Regressdriver(float resistence)
//     {
//         if (flydrive > 0)
//         {
//             flydrive -= Time.deltaTime * resistence;
//         }
//         else flydrive = 0;
//         return flydrive;
//     }
//
//     private void firstJumpTimer()
//     {
//         current.isJump1 = false;
//         current.isJump2 = false;
//     }
//
//     public void PressLeftArrow()
//     {
//         isPressLeftArrow = true;
//     }
//     public void DropLeftArrow()
//     {
//         isPressLeftArrow = false;
//     }
//
//
//     public void PressRightArrow()
//     {
//         isPressRightArrow = true;
//     }
//     public void DropRightArrow()
//     {
//         isPressRightArrow = false;
//     }
//
//
//
// }
