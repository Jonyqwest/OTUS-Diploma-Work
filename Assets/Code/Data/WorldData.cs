using System;
using UnityEngine;

namespace Code.Data
{
    [Serializable]
    public class WorldData
    {
        public PositionOnLevel Position;
       
    }

    public class PositionOnLevel
    {
        public Vector3Data Position;
        public string Level;
    }
}