using Core.Input.Interfaces;
using UnityEngine;

public class InputRouter : MonoBehaviour
{
    [Header("PC Input")]
    public PCInputProvider pcInputProvider;
    
    [Header("VR Input")]
    public VRInputProvider vrInputProvider;

    private IInputProvider inputProvider;
    private MovementIntent intent;
    private IPossessable currentTarget;
    
    private void Awake()
    {
        inputProvider = pcInputProvider;
        
        // Автоматически подключаем VR Provider если есть
        if (DeviceDetectUtility.VrIsAvailable() && vrInputProvider != null)
        {
            Debug.Log("[InputRouter] Set VRInputProvider");
            inputProvider = vrInputProvider;
        }
    }

    public void SetTarget(IPossessable target)
    {
        if (currentTarget != null)
        {
            currentTarget.OnUnpossessed();
        }

        currentTarget = target;

        if (currentTarget != null)
        {
            currentTarget.OnPossessed();
        }
    }

    public IPossessable GetCurrentTarget() => currentTarget;

    //читаем тут
    private void Update()
    {
        if (inputProvider != null)
            intent.Update(inputProvider.ReadIntent());
    }

    //применяем тут
    private void FixedUpdate()
    {
        if (currentTarget != null)
            currentTarget.ApplyMovement(ref intent, Time.fixedDeltaTime);
    }
}
