using UnityEngine;


[CreateAssetMenu(menuName = "Character/Force Profile")]
public class СurveProfile : ScriptableObject
{
    [Header("Curve")]
    public AnimationCurve StartCurve;
    public AnimationCurve RotateCurve;
    public AnimationCurve FrictionCurve;
    public AnimationCurve StopCurve;
    public AnimationCurve FlyCurve;
    public AnimationCurve BlendForceCurve;



}


