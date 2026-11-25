using UnityEngine;

namespace Code.CameraLogic
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _following;
        [SerializeField] private float _rotationAngleX;
        [SerializeField] private float _distance;
        [SerializeField] private float _offsetY;
        
        private void LateUpdate()
        {
            if(_following is null) return;

            Quaternion rotation = Quaternion.Euler(_rotationAngleX, 0, 0);
            Vector3 position = rotation * new Vector3(0, 0, _distance) + GetFollowingPointPosition();

            transform.position = position;
            transform.rotation = rotation;
        }
        
        public void SetFollower(GameObject following) => _following = following.transform;

        private Vector3 GetFollowingPointPosition()
        {
            Vector3 followingPosition = _following.position;
            followingPosition.y += _offsetY;
            
            return followingPosition;
        }
    }
}