using UnityEngine;

public class BallGroundSensor : MonoBehaviour
{
    GroundModule ground;

    [Header("Bump Sensors")]
    public Transform frontSensor;
    public Transform leftSensor;
    public Transform rightSensor;

    public void Bind(GroundModule groundModule)
    {
        ground = groundModule;
        ground.frontSensor = frontSensor;
        ground.leftSensor = leftSensor;
        ground.rightSensor = rightSensor;
        
        // Автоматически добавляем BumpSensor если его нет
        if (frontSensor) SetupSensor(frontSensor, BumpSensor.SensorType.Front);
        if (leftSensor) SetupSensor(leftSensor, BumpSensor.SensorType.Left);
        if (rightSensor) SetupSensor(rightSensor, BumpSensor.SensorType.Right);
    }

    void SetupSensor(Transform sensor, BumpSensor.SensorType type)
    {
        // Настраиваем коллайдер как триггер
        Collider col = sensor.GetComponent<Collider>();
        if (col) 
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError($"[BallGroundSensor] NO Collider on {sensor.name}!");
        }
        
        // Добавляем BumpSensor если его нет
        BumpSensor bump = sensor.GetComponent<BumpSensor>();
        if (bump == null)
            bump = sensor.gameObject.AddComponent<BumpSensor>();
        
        // Настраиваем
        bump.sensorType = type;
        bump.ground = ground;
    }

    void OnCollisionEnter(Collision collision)
    {
        ground?.NotifyCollisionEnter(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        ground?.NotifyCollisionStay(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        ground?.NotifyCollisionExit(collision);
    }
}