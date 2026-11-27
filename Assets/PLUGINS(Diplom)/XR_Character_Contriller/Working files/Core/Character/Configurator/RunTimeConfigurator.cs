using UnityEngine;

//[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class RunTimeConfigurator : MonoBehaviour
{
    [Header("Configs")]
    public BallCharacterConfig characterConfig;
    public СurveProfile forceProfile;
    public JumpProfile jumpHorizontalProfile;
    public JumpProfile jumpVerticalProfile;

    private Rigidbody rb;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ApplyRigidbodySettings();
    }


    public void ApplyRigidbodySettings()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        SetRunMode();

        RigidbodyConstraints constraints = RigidbodyConstraints.None;
        rb.constraints = constraints;
    }

    public void SetWalkMode()
    {
        rb.mass = characterConfig.mass;
        rb.linearDamping = characterConfig.walkeDrag;
        rb.angularDamping = characterConfig.angularDragWalke;
    }

    public void SetRunMode()
    {
        rb.mass = characterConfig.mass;
        rb.linearDamping = characterConfig.groundDrag;
        rb.angularDamping = characterConfig.angularDrag;
    }

    public void SetFlyMode()
    {
        rb.mass = characterConfig.massFly;
        rb.linearDamping = characterConfig.groundDrag;
        rb.angularDamping = characterConfig.angularDrag;
    }
}
