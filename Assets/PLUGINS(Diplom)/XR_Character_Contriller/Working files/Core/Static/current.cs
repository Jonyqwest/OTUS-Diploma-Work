// using UnityEngine;
//
// public static class current 
// {
//     //PLAYER PARTS
//     public static GameObject player; //ссылка на обьект плеер
//    
//     public static Transform BALL;
//     public static Rigidbody BALLrb;
//     public static Transform BODY;
//     public static Transform BODY_POSITION;
//     public static Transform BODY_ROTATION;
//     public static Transform BODY_CENTER;
//
//     public static Transform HEART;
//     public static Transform CamBack; //позиция для камеры со спины
//     public static Transform CamFirstPlace; //позиция для камеры от первого лица
//
//     public static Transform CameraTarget;
//     public static Transform mainCam;
//     
//
//     public static Vector3 V3_orientalSpeed;
//     public static Vector3 V3_orientalForce;
//     public static Vector3 V3_perpendikularSpeed;
//
//
//     public static float speed;
//     public static float speedMagnitude;
//     public static float speedFrontMagnitude;
//     public static float speedUp;
//     //скорость для спидометра
//     public static float Speedometr;
//     public static float SpeedometrLast;
//     public static float SpeedometrDelta;
//     public static float SpeedometrUpdateTime = .5f;
//
//    
//
//
//
//     public static bool isJump;      //прыжок
//     public static bool isJump1;      //прыжок один раз 
//     public static bool isJump2;      //прыжок двойной
//     public static bool isTwoJump;   //двойной прыжок
//
//     public static bool isLanding;   //бегает на земле
//     public static bool isHold;      //если персонаж остановился
//     public static bool isPressBack; //нажата кнопка назад
//
//     
//
//
//     public static bool isPlane = false; //парим
//     public static bool isDart = false;  //рывок
//     public static bool isPike = false;  //выход из пике
//
//
//     public static int invertMove = 1;   //движение против ориентации
//
//     //бег
//     public static float runMaxSpeed = 15 * 15;  //квадрат скорости бега
//     public static float forceForward = 6000;    //сила бега прямо
//     public static float forceBack = 1200;       //сила тормоза
//     public static float forceRotate = 1000;     //сила разворота при беге
//     public static float maxRunAngle = 6;        //угол наклона при беге
//
//     //ускорение
//     public static float axelerateMaxSpeed = 25 * 25;//квадрат скорости бега при ускорении
//     public static float axelerateForce = 6000;      //сила ускорения
//     public static float axelerateMaxEnetgy = 100;   //максимальный запас энергии для ускорения
//     public static float axelerateRegeneration = 1;  //восстановление топлива для ускорения
//     public static float axelerateOil = 10;          //количество топлива для ускорения
//
//
//
//     //прыжок
//     public static float forceJump = 500;      //сила прыжка
//     public static float gravity = 1.4f;         //гравитация при парении
//
//     //полет
//     public static float flyMaxSpeed = 44 * 44;      //квадрат скорости
//     public static float flyTimeMaxSpeed = 35;       //время разгона до максимума
//     public static float forceFly = 800;             //мошность крыльев
//     public static float resistenceOfLanding = 0.4f; //сопротивление при приземлении
//     public static float maxAngle = 20;             
//
//     //рывок
//     public static float dartAcceleration = 5;       //ускорение рывка
//     public static float forceMaxDart = 200;         //максимальная сила рывка
//     public static float forceDart = 0;              //действующая сила рывка
//
//     public static float forcePike = 800;            //сила пикирования
//
//
//     public static int collectRingOnRoad;            //собранные кольца
//     public static int collectStarOnRoad;            //собранные звезды
//
//     //прочность
//     public static int stamina = 20;                 //выносливость
//     public static float mass = 60;                   //масса
//     public static float armor = 1;                  //броня
//     public static int damage = 0;                 //текущие разрушения
//
// }
