using Code.CameraLogic;
using Code.Data;
using Code.Infrastracture;
using Code.Infrastracture.Services;
using Code.Infrastracture.Services.PersistentProgress;
using Code.Services.Inputs;
using UnityEngine;

public class HeroMove : MonoBehaviour, ISaveProgress
{
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private float _speed = 1f;
    
    private IInputService _inputService;
    private Camera _camera;
    private float _epsilon = 0.001f;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Start()
    {
        _inputService = AllServices.Container.Single<IInputService>();
        //_inputService = Game.InputService;  
    }

    public void SetCameraFolow(GameObject hero)
    {
    }

    private void Update()
    {
        Vector3 moveDirection = Vector3.zero;
        
        if (_inputService.Axis.sqrMagnitude > _epsilon)
        {
            var inputAxis3d = new Vector3(_inputService.Axis.x, 0, _inputService.Axis.y);
            moveDirection = _camera.transform.TransformDirection(inputAxis3d);
            moveDirection.y = 0;
            moveDirection.Normalize();
            transform.forward = moveDirection;
        }

        moveDirection += Physics.gravity;
        _characterController.Move(_speed * moveDirection * Time.deltaTime);
    }

    public void UpdateProgress(PlayerProgress progress)
    {
        if (progress.WorldData.Position == null)
            progress.WorldData.Position = new PositionOnLevel();
        progress.WorldData.Position.Position = transform.position.AsVector3Data();
    }

    public void LoadProgress(PlayerProgress progress)
    {
        if (progress.WorldData.Position?.Position != null)
        {
            var savedPosition = progress.WorldData.Position.Position;
            transform.position = savedPosition.AsUnityVector();
        }
    }
}