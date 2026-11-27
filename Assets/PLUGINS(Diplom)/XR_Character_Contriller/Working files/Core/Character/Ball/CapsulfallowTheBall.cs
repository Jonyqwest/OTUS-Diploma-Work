using Core.Character.Controllers;
using UnityEngine;

namespace BallControler
{
    public class CapsulfallowTheBall : MonoBehaviour
    {
        [Tooltip("Контейнер Колайдеров.")]
        [SerializeField] 
        private Transform _colliderContainer;
        
        [Tooltip("Скорость плавного поворота к направлению движения (град/с).")]
        [SerializeField]
        private float _rotateSpeed = 100f;
     
       
        [Tooltip("не надо заполнять")]
        private Transform _ball;

        
        
        public BallCharacterController ballCharacterController;
        private Transform _transform => _colliderContainer?.transform ?? transform;
        
        /// <summary>
        /// Явно задаёт шар и его контроллер (вызывается юнитом после спавна).
        /// </summary>
        public void SetBall(BallCharacterController controller)
        {
            ballCharacterController = controller;
            var ballTransform = controller != null ? controller.transform : null;
            if (ballTransform)
                _ball = ballTransform.GetComponent<RigBall>().BallCenter;
        }

        private void TryResolveBall()
        {
            if (ballCharacterController == null && _ball != null)
            {
                ballCharacterController = _ball.GetComponent<BallCharacterController>();
                if (ballCharacterController == null)
                    Debug.LogWarning("[fallowTheBall] ball заполнен, но в нём нет BallCharacterController!", this);
            }
        }

        void Update()
        {
            TryResolveBall();
            if (_ball == null || ballCharacterController == null) return;

            // устанавливаем тело на шар
            _transform.position = _ball.position;

            // и Плавный поворот
            Vector3 newForward = Vector3.RotateTowards(
                _transform.forward,
                ballCharacterController.Orientation,
                _rotateSpeed * Time.deltaTime,
                0f
            );
            _transform.rotation = Quaternion.LookRotation(newForward, Vector3.up);
        }
    }
}
