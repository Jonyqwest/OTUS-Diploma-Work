using UnityEngine;

public class BumpSensor : MonoBehaviour
{
    public enum SensorType { Front, Left, Right }
    public SensorType sensorType;
    public GroundModule ground;

    bool isTouchingGround;


    void OnTriggerEnter(Collider other)
    {
        if (IsGround(other))
        {
            isTouchingGround = true;
            NotifyGround();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsGround(other))
        {
            Debug.Log($"[BumpSensor-{sensorType}] OnTriggerExit: {other.name}");
            isTouchingGround = false;
            NotifyGround();
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (IsGround(other) && !isTouchingGround)
        {
            Debug.Log($"[BumpSensor-{sensorType}] OnTriggerStay: {other.name}");
            isTouchingGround = true;
            NotifyGround();
        }
    }

bool IsGround(Collider other)
    {
        // Игнорируем персонажа - у него есть Rigidbody
        if (other.GetComponent<Rigidbody>() != null)
        {
            return false;
        }
        // Проверяем слой Default (слой 2)
        bool isDefault = other.gameObject.layer == 2; // Default = 2
        Debug.Log($"[BumpSensor-{sensorType}] layer Default check: {isDefault}");
        
        if (!isDefault)
        {
            Debug.Log($"[BumpSensor-{sensorType}] REJECTED: wrong layer");
            return false;
        }
        
        Debug.Log($"[BumpSensor-{sensorType}] ACCEPTED!");
       
   
        Debug.Log("555555555");
        // Проверяем слой Default
        bool isDefaults = other.gameObject.layer == LayerMask.NameToLayer("Default");
        Debug.Log($"[BumpSensor-{sensorType}] layer Default check: {isDefaults}");
        
        return isDefaults;
        return true;
    }

    void NotifyGround()
    {
        if (ground == null) 
        {
            Debug.LogError($"[BumpSensor-{sensorType}] ground is NULL!");
            return;
        }

        Debug.Log($"[BumpSensor-{sensorType}] NotifyGround: isTouching={isTouchingGround}");
        
        switch (sensorType)
        {
            case SensorType.Front:
                ground.SetFrontDetected(isTouchingGround);
                break;
            case SensorType.Left:
                ground.SetLeftDetected(isTouchingGround);
                break;
            case SensorType.Right:
                ground.SetRightDetected(isTouchingGround);
                break;
        }
    }
}