using System;
using Core.Character.Capsule;
using Core.Character.Controllers;
using Core.Input.Interfaces;
using UnityEngine;

namespace Core.Character.Spectator
{
    [RequireComponent(typeof(InputRouter))]
    public class HoverSpectatorBody : MonoBehaviour, IControllable, IPossessable
    {
        [Header("Camera & Target")]
        [SerializeField] private Transform _cameraAnchor;
        public IPossessable Driver => this;
        public Transform CameraTarget => _cameraAnchor != null ? _cameraAnchor : transform;

        [Header("Movement Speed")]
        [SerializeField] private float _moveSpeed = 30f;
        [SerializeField] private float _verticalScrollSpeed = 20f;

        [Header("Altitude & Terrain Following")]
        [Tooltip("Желаемая высота полета над рельефом (в метрах)")]
        [SerializeField] private float _relativeAltitude = 40f;
        [SerializeField] private float _minRelativeAltitude = 5f;
        [SerializeField] private float _maxRelativeAltitude = 300f;
        [SerializeField] private float _altitudeSmoothTime = 0.25f;
        [SerializeField] private LayerMask _groundLayer = ~0;

        [Header("Idle Bobbing (Дыхание ветра)")]
        [SerializeField] private bool _enableBobbing = true;
        [SerializeField] private float _bobAmplitude = 0.3f;
        [SerializeField] private float _bobFrequency = 1.0f;

        [Header("Optimization")]
        [Tooltip("Интервал между проверками высоты при полете (в секундах)")]
        [SerializeField] private float _groundCheckInterval = 0.1f;
        [Tooltip("Минимальный сдвиг по горизонтали для запроса высоты (в метрах)")]
        [SerializeField] private float _minMoveDistanceToCheck = 0.5f;

        private float _groundCheckTimer = 0f;
        private Vector3 _lastCheckedPosition;
        private float _cachedGroundHeight = 0f;
        private float _currentAltitudeVelocity;
        private bool _isPossessed = false;

        // Кэшированные ссылки на физические компоненты для их подавления
        private Rigidbody _rb;
        private BallCharacterController _ballController;
        private bool _originalRbKinematic = false;
        private bool _originalRbGravity = true;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _ballController = GetComponent<BallCharacterController>();
        }

        public void OnPossessed()
        {
            _isPossessed = true;
            _groundCheckTimer = _groundCheckInterval;
            _lastCheckedPosition = transform.position;
            _cachedGroundHeight = GroundHeightService.GetHeight(transform.position, _groundLayer);

            // 1. Подавляем штатный физический контроллер персонажа
            if (_ballController != null)
            {
                _ballController.enabled = false;
            }

            // 2. Отключаем физическое влияние Rigidbody на полет камеры
            if (_rb != null)
            {
                _originalRbKinematic = _rb.isKinematic;
                _originalRbGravity = _rb.useGravity;

                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.useGravity = false;
                _rb.isKinematic = true;
            }
        }

        public void OnUnpossessed()
        {
            _isPossessed = false;

            // Восстанавливаем состояние физики (если управление возвращается шару)
            if (_rb != null)
            {
                _rb.isKinematic = _originalRbKinematic;
                _rb.useGravity = _originalRbGravity;
            }

            if (_ballController != null)
            {
                _ballController.enabled = true;
            }
        }

        public void ApplyMovement(ref MovementIntent intent, float dt)
        {
            if (!_isPossessed) return;

            // 1. Направление взгляда камеры в горизонтальной плоскости
            Camera mainCam = Camera.main;
            Vector3 camForward = mainCam != null ? mainCam.transform.forward : intent.viewDirection;
            Vector3 forward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;
            Vector3 right = mainCam != null 
                ? Vector3.ProjectOnPlane(mainCam.transform.right, Vector3.up).normalized 
                : Vector3.Cross(Vector3.up, forward).normalized;

            // Синхронизируем вращение спектатора с направлением взгляда
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // Горизонтальный инпут (WASD / Стик)
            Vector3 inputVector = intent.moveRun;
            Vector3 horizontalMove = (forward * inputVector.z + right * inputVector.x) * _moveSpeed * dt;

            // 2. Управление высотой полета (Колесико мыши + клавиши E / Q)
            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _relativeAltitude += scroll * _verticalScrollSpeed * 0.5f;
            }

            if (intent.jumpHeld || UnityEngine.Input.GetKey(KeyCode.E))
                _relativeAltitude += _verticalScrollSpeed * dt;
            if (intent.slide || UnityEngine.Input.GetKey(KeyCode.Q))
                _relativeAltitude -= _verticalScrollSpeed * dt;

            _relativeAltitude = Mathf.Clamp(_relativeAltitude, _minRelativeAltitude, _maxRelativeAltitude);

            // 3. Умный опрос рельефа
            _groundCheckTimer += dt;
            Vector3 currentPos = transform.position;
            float horizontalDistance = Vector2.Distance(
                new Vector2(currentPos.x, currentPos.z), 
                new Vector2(_lastCheckedPosition.x, _lastCheckedPosition.z)
            );

            if (_groundCheckTimer >= _groundCheckInterval || horizontalDistance >= _minMoveDistanceToCheck)
            {
                _groundCheckTimer = 0f;
                _lastCheckedPosition = currentPos;
                _cachedGroundHeight = GroundHeightService.GetHeight(currentPos, _groundLayer);
            }

            // 4. Расчет целевой высоты и сглаживание
            float targetY = _cachedGroundHeight + _relativeAltitude;

            if (_enableBobbing)
            {
                float bobOffset = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
                targetY += bobOffset;
            }

            float newY = Mathf.SmoothDamp(currentPos.y, targetY, ref _currentAltitudeVelocity, _altitudeSmoothTime);

            // 5. Применяем итоговую позицию
            transform.position = new Vector3(
                currentPos.x + horizontalMove.x,
                newY,
                currentPos.z + horizontalMove.z
            );
        }
    }
}