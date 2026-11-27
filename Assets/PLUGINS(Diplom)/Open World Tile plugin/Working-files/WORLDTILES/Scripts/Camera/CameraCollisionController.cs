using UnityEngine;

/// <summary>
/// Умный контроллер камеры: плавно следует за целью, 
/// корректирует столкновения (inner/outer spherecast),
/// смотрит на цель и автоматически сокращает избыточную дистанцию.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraCollisionController : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform followTarget;
    public Vector3 followOffset = new Vector3(0, 2f, -4f);
    public float followSmooth = 10f;

    [Header("Collision Settings")]
    public LayerMask collisionMask;
    public float innerRadius = 0.3f;        // внутренняя сфера (жёсткий стоп)
    public float outerRadius = 0.8f;        // внешняя сфера (предупреждение)
    public float surfaceOffset = 0.05f;     // отступ от поверхности

    [Header("Prediction Settings")]
    public float predictiveStrength = 0.6f; // сила "ухода" от препятствия
    public float recoverSpeed = 2f;         // скорость восстановления offset

    [Header("Distance Control")]
    public float maxDistance = 6f;          // максимально допустимая дистанция
    public float returnSpeed = 5f;          // скорость возврата камеры при отставании

    private Vector3 desiredPosition;
    private Vector3 correctedPosition;
    private Vector3 currentOffset;
    private Vector3 defaultOffset;

    void Start()
    {
        if (!followTarget)
        {
            Debug.LogWarning($"{nameof(CameraCollisionController)}: нет цели для слежения!");
            enabled = false;
            return;
        }

        defaultOffset = followOffset;
        currentOffset = followOffset;
    }

    void LateUpdate()
    {
        if (!followTarget) return;

        Vector3 targetPos = followTarget.position;

        // 1️⃣ желаемая позиция
        desiredPosition = targetPos + currentOffset;

        // 2️⃣ Проверка приближения к препятствиям
        Vector3 direction = desiredPosition - targetPos;
        float distance = direction.magnitude;

        if (Physics.SphereCast(targetPos, outerRadius, direction.normalized,
            out RaycastHit preHit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float t = Mathf.InverseLerp(distance, 0f, preHit.distance);
            currentOffset = Vector3.Lerp(currentOffset, defaultOffset * Mathf.Lerp(1f, predictiveStrength, t), Time.deltaTime * followSmooth);
        }
        else
        {
            currentOffset = Vector3.Lerp(currentOffset, defaultOffset, Time.deltaTime * recoverSpeed);
        }

        // 3️⃣ обновлённая позиция после предсказания
        desiredPosition = targetPos + currentOffset;
        direction = desiredPosition - targetPos;
        distance = direction.magnitude;

        // 4️⃣ Внутренний SphereCast — жёсткий стоп
        correctedPosition = desiredPosition;
        if (Physics.SphereCast(targetPos, innerRadius, direction.normalized,
            out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            correctedPosition = hit.point - direction.normalized * (innerRadius + surfaceOffset);
        }

        // 5️⃣ Проверка расстояния — если камера слишком далеко, плавно возвращаем
        float currentDistance = Vector3.Distance(correctedPosition, targetPos);
        if (currentDistance > maxDistance)
        {
            Vector3 moveDir = (targetPos - correctedPosition).normalized;
            correctedPosition += moveDir * (currentDistance - maxDistance) * Time.deltaTime * returnSpeed;
        }

        // 6️⃣ Плавное движение камеры
        transform.position = Vector3.Lerp(transform.position, correctedPosition, Time.deltaTime * followSmooth);

        //// 7️⃣ Камера всегда смотрит на цель
        //transform.rotation = Quaternion.Lerp(
        //    transform.rotation,
        //    Quaternion.LookRotation(targetPos - transform.position, Vector3.up),
        //    Time.deltaTime * followSmooth * 2f
        //);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!followTarget) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(followTarget.position, outerRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(followTarget.position, innerRadius);

        Gizmos.color = Color.cyan;
        Vector3 dir = followTarget.position + followOffset - followTarget.position;
        Gizmos.DrawLine(followTarget.position, followTarget.position + dir);
    }
#endif
}
