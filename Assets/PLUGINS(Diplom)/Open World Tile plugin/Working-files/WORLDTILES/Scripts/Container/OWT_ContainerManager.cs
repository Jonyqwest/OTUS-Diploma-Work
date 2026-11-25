namespace WorldTile
{
    using UnityEngine;

    public class OWT_ContainerManager : MonoBehaviour
    {
        [Header("Global defaults")]
        [SerializeField] private float defaultShowDistance = 200f;
        [SerializeField] private float defaultActiveDistance = 50f;

        [Header("Performance")]
        [SerializeField] private float checkInterval = 0.3f;

        private float _nextCheckTime;

        private void OnEnable()
        {
            OWT_ContainerRegistry.Clear();
        }

        private void Update()
        {
            if (Time.time < _nextCheckTime) return;
            _nextCheckTime = Time.time + checkInterval;

            Vector3 playerPos = PlayerWorldStat.GetCurrentWorldPosition().ToUnityPosition();

            foreach (var container in OWT_ContainerRegistry.GetAll())
            {
                if (container == null) continue;

                float dist = Vector3.Distance(playerPos, container.worldPosition.ToUnityPosition());

                float showDist = container.ShowDistance > 0 ? container.ShowDistance : defaultShowDistance;
                float activeDist = container.ActiveDistance > 0 ? container.ActiveDistance : defaultActiveDistance;

                if (dist > showDist)
                    container.ApplyState(ContainerState.Hidden);
                else if (dist > activeDist)
                    container.ApplyState(ContainerState.Static);
                else
                    container.ApplyState(ContainerState.Active);
            }
        }
    }
}
