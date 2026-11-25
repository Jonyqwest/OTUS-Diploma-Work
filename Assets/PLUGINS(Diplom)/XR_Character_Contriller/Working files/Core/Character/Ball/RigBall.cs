using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigBall : MonoBehaviour
{
    public Transform Owner;
    
    public Transform BallCapsul;
    public Transform BallHead;
    public Transform BallCenter;
    public Transform BallFoot;
    public float BallRadius => transform.localScale.y * 0.5f;
}
