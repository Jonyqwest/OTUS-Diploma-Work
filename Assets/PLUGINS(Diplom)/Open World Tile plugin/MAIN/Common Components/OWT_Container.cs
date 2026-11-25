namespace WorldTile
{
    using UnityEngine;

    public enum ContainerState
    {
        Hidden,
        Static,
        Active
    }

    public class OWT_Container : MonoBehaviour
    {
        [SerializeField] public WorldPosition worldPosition;

        [Header("Distances (0 = use global default)")]
        [SerializeField] private float showDistance = 0;
        [SerializeField] private float activeDistance = 0;

        [Header("Physics Freeze")]
        [SerializeField] private bool freezeOnSpawn = true;
        [SerializeField] private float raycastDistance = 5f;
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private int raycastInterval = 10;

        public ContainerState State { get; private set; } = ContainerState.Hidden;
        public float ShowDistance => showDistance;
        public float ActiveDistance => activeDistance;

        private bool _initialPositionApplied;
        private bool _childrenFrozen;
        private bool _childrenUnfrozen;
        private int _frameSinceLastCheck;
        private Rigidbody[] _cachedBodies;

        private void Awake()
        {
            OWT_ContainerRegistry.Register(this);
        }

        private void OnDestroy()
        {
            OWT_ContainerRegistry.Unregister(this);
        }

        private void Update()
        {
            if (!_initialPositionApplied && TileStat.compensPos != Vector3.zero)
            {
                TeleportToWorldPosition();
                _initialPositionApplied = true;
            }

            if (_initialPositionApplied && freezeOnSpawn && !_childrenFrozen)
            {
                FreezeChildren();
                _childrenFrozen = true;
            }

            if (_childrenFrozen && !_childrenUnfrozen)
            {
                if (_frameSinceLastCheck++ >= raycastInterval)
                {
                    _frameSinceLastCheck = 0;

                    if (AreAllGrounded())
                    {
                        UnfreezeChildren();
                        _childrenUnfrozen = true;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.N))
                DebugTeleport();

            if (Input.GetKeyDown(KeyCode.U))
            {
                UnfreezeChildren();
                _childrenUnfrozen = true;
                Debug.Log($"[OWT_Container] Manual unfreeze via U key", this);
            }
        }

        public void ApplyState(ContainerState newState)
        {
            if (newState == State) return;
            State = newState;

            switch (newState)
            {
                case ContainerState.Hidden:
                    break;

                case ContainerState.Static:
                    gameObject.SetActive(true);
                    SetInteractive(false);
                    break;

                case ContainerState.Active:
                    gameObject.SetActive(true);
                    SetInteractive(true);
                    break;
            }
        }

        private bool AreAllGrounded()
        {
            if (_cachedBodies == null || _cachedBodies.Length == 0)
                return false;

            foreach (var rb in _cachedBodies)
            {
                if (rb == null) continue;

                if (Physics.Raycast(rb.position, Vector3.down, raycastDistance, groundMask))
                    continue;

                return false;
            }

            return true;
        }

        private void FreezeChildren()
        {
            _cachedBodies = GetComponentsInChildren<Rigidbody>(true);
            if (_cachedBodies.Length == 0) return;

            foreach (var rb in _cachedBodies)
                rb.isKinematic = true;

            Debug.Log($"[OWT_Container] Frozen {_cachedBodies.Length} Rigidbody(s)", this);
        }

        private void UnfreezeChildren()
        {
            if (_cachedBodies == null || _cachedBodies.Length == 0) return;

            foreach (var rb in _cachedBodies)
            {
                if (rb != null)
                    rb.isKinematic = false;
            }

            Debug.Log($"[OWT_Container] Unfrozen {_cachedBodies.Length} Rigidbody(s)", this);
        }

        public void DebugTeleport()
        {
            var unityPos = worldPosition.ToUnityPosition();
            var fromWP = new WorldPosition();
            fromWP.zoom = TileStat.BaseZoom;
            fromWP = fromWP.FromUnityPosition(unityPos, TileStat.BaseZoom);

            var deltaPos = transform.position - unityPos;

            Debug.Log($"[OWT_Container Debug N] " +
                $"name={name} " +
                $"current pos=({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})\n" +
                $"worldPosition= Tile({worldPosition.tileX}/{worldPosition.tileY}) " +
                $"offset({worldPosition.localOffset.x:F4}/{worldPosition.localOffset.y:F4}) " +
                $"H:{worldPosition.Height:F2} Z:{worldPosition.zoom}\n" +
                $"ToUnityPosition → ({unityPos.x:F2}, {unityPos.y:F2}, {unityPos.z:F2})\n" +
                $"compens={TileStat.compensPos}\n" +
                $"RoundTrip → Tile({fromWP.tileX}/{fromWP.tileY}) " +
                $"offset({fromWP.localOffset.x:F4}/{fromWP.localOffset.y:F4})\n" +
                $"delta current→target: ({deltaPos.x:F2}, {deltaPos.y:F2}, {deltaPos.z:F2})",
                this);

            transform.position = unityPos;
        }

        private void TeleportToWorldPosition()
        {
            transform.position = worldPosition.ToUnityPosition();
        }

        private void SetInteractive(bool on)
        {
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = on;
        }
    }
}
