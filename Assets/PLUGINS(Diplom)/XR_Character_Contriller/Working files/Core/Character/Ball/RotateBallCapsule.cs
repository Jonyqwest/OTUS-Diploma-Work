using Core.Character.Controllers;
using UnityEngine;

public class RotateBallCapsule : MonoBehaviour
{
    [SerializeField] 
    private BallCharacterController _ballController;

    [Tooltip("Скорость поворота в градусах в секунду")]
    [SerializeField] 
    private float _rotateSpeed = 720f;

    private void Awake()
    {
        // Если ссылка не задана в инспекторе, ищем в родителях (в корне PhisicsDriver)
        if (_ballController == null)
            _ballController = GetComponentInParent<BallCharacterController>();

        if (_ballController == null)
            Debug.LogError("[RotateBallCapsule] BallCharacterController не найден ни в инспекторе, ни в родительских объектах!", this);
    }

    private void Update()
    {
        if (_ballController == null) 
            return;

        // 1. Получаем АКТУАЛЬНУЮ ориентацию шара в каждом кадре
        Vector3 targetDirection = _ballController.Orientation;

        // 2. Обнуляем наклон по вертикали, чтобы капсула крутилась только вокруг оси Y
        targetDirection.y = 0f;

        // 3. Защита от нулевого вектора
        if (targetDirection.sqrMagnitude < 0.001f)
            return;

        // 4. Целевое вращение
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);

        // 5. Плавный поворот с учетом скорости _rotateSpeed
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _rotateSpeed * Time.deltaTime
        );
    }
}