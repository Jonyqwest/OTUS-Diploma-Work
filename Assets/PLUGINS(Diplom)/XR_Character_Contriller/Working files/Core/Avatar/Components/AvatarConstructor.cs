using System;
using BallControler;
using Core.Character.Controllers;
using Core.Input.Interfaces;
using UnityEngine;

namespace Core.Character.Capsule
{
    /// <summary>
    /// Внешний юнит (пустой GameObject) → контроллер.
    /// Спавнит невидимую физическую сферу (PhysicsDriver.prefab) как своего ребёнка,
    /// связывает её с капсулой (сенсоры, конфиг). Синхронизацию капсулы за сферой
    /// делает fallowTheBall на самой капсуле.
    /// </summary>
    public class AvatarConstructor 
    {
        
        public AvatarConstructor(AvatarFacade avatarFacade)
        {
            _avatarFacade =  avatarFacade;
            
            //Получили префаб
            var path = avatarFacade.DriverPrefabPath;
            driverPrefab = Resources.Load<GameObject>(path);
            //Получили позицию
            _position =  avatarFacade.transform.position;
            //получаем родителя (попытка обернуть аватар и драйвер)
            _perentColliderContainer = avatarFacade.transform.parent;
            //Получили модель
            //_model = avatarFacade.Model3D;
            //Получение Юнита
            _owner = avatarFacade.transform;
            
            //------создаем драйвер--------//
            
            CreateDriver();
            
             
            //Получили CameraTarget из драйвера
            // CameraTarget = _ballDriver.GetComponent<RigBall>().BallHead;
            
           
            
            
            //avatarFacade.SetDriverAndCameraTarget(_driver, CameraTarget);//под вопросом (вернули цель и драйвер в фасад)
            
            //avatarFacade.InitModelRotate(_rigBall.BallCapsul);//под вопросом (вернули капсулу)
        }

        
        private Vector3 _position;
        
        private GameObject driverPrefab;

        private Transform ColliderContainer;
        private Transform _model;

        public Transform frontSensor;
        public Transform leftSensor;
        public Transform rightSensor;

        public BallCharacterConfig config;

        public Transform FootPosition => _rigBall?.BallFoot ?? null;

        public Transform CameraTarget => _rigBall?.BallHead;
        // public Transform CameraTarget => _avatarFacade.transform;
        //public Transform CameraTarget => ResolveHeadAnchor();
       
        private GameObject _ballDriver;
        private RigBall _rigBall;
        private BallCharacterController _driver;

        public GameObject BallDriver => _ballDriver;
        public RigBall RigBall => _rigBall;
        public IPossessable Driver => _driver;
        private Transform _perentColliderContainer;

        private Transform _owner;
        private AvatarFacade _avatarFacade;


        private Transform ResolveHeadAnchor()
        {
            Transform root = _model != null ? _model : _owner;
            if (root == null) return null;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "HeadAnchor")
                    return t;
            }
            return root;
        }

        public BallCharacterController CreateDriver()
        {
            if (_driver != null) return _driver; //если драйвер уже есть
            
            if (driverPrefab == null)
            {
                Debug.LogError("[UnitFasade] spherePrefab не назначен!");
                //enabled = false;
                return null;
            }

            var rigBAll = driverPrefab.GetComponent<RigBall>();
            if (rigBAll == null)
            {
                Debug.LogError("[UnitFasade] RigBall не найден!");
                //enabled = false;
                return null;
            }
            float ballRadius = rigBAll.BallRadius;
            
            _ballDriver = GameObject.Instantiate(driverPrefab, _position+ Vector3.up * ballRadius, Quaternion.identity);
            _ballDriver.name = $"PhysicsDriver ";
            _ballDriver.transform.parent = _perentColliderContainer;
            _rigBall = _ballDriver.GetComponent<RigBall>();
            _rigBall.Owner = _avatarFacade.transform;
            _driver = _ballDriver.GetComponent<BallCharacterController>();
            
            
            if (_driver == null)
            {
                Debug.LogError("[UnitFasade] в spherePrefab нет BallCharacterController!", _ballDriver);
                //enabled = false;
            }
            
            return _driver;
        }

    }
}
