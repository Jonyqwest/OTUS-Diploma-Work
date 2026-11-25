using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OWT_PositionDetector : MonoBehaviour
{
    void Update()
    {
        OWT_PlayerPositionStatic.ChangePlayerWorldPosition(transform.position);
    }
}
