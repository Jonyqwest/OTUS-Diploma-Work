using UnityEngine;

namespace Code.Data
{
    public static class DataExtension
    {
        public static Vector3Data AsVector3Data(this Vector3 vector) => 
            new Vector3Data(vector.x, vector.y, vector.z);

        public static Vector3 AsUnityVector(this Vector3Data data) => 
            new Vector3(data.X, data.Y, data.Z);
    }
}