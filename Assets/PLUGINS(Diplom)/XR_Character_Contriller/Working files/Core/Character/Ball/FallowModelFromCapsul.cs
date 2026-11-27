using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallowModelFromCapsul : MonoBehaviour
{
   [SerializeField]
   private RigBall _rigBall;
   private Transform _transform => _rigBall?.BallCenter ?? transform;
   private Transform _owner => _rigBall.Owner;

   private void Update()
   {
      if (_owner)
      {
         _owner.position = _transform.position;
         _owner.rotation = _transform.rotation;
      }
   }
}
