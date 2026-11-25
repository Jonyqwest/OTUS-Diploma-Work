using System.Collections.Generic;
using Core.Character.Capsule;
using Core.Character.Controllers;
using Core.Input.Interfaces;
using UnityEngine;
using Core.Camera;

public class AvatarFacade : MonoBehaviour, IControllable, IViewModeSwitchable
{
    // public Transform Model3D => model3D;
    // [SerializeField] private Transform model3D;
    
    [Header("Camera Mode Preference")]
    [Tooltip("Текущий сохраненный режим камеры (1-е или 3-е лицо). Запоминается при переключениях на клавишу V.")]
    [SerializeField] private CameraViewMode _currentViewMode = CameraViewMode.ThirdPerson;
    public CameraViewMode CurrentViewMode
    {
        get => _currentViewMode;
        set => _currentViewMode = value;
    }
    public string DriverPrefabPath => _driverPrefabPath;
    private string _driverPrefabPath = "Prefab/PhisicsDriver";

    public Transform CameraTarget => _cameraTarget;
    private Transform _cameraTarget;
    public IPossessable Driver => _driver;
    private IPossessable _driver;

    [Header("Layer Settings")]
    [Tooltip("Слой юнита и его драйвера (например Hero). При значении 0 (Default) используется слой самого объекта.")]
    [Layer] [SerializeField] private int _unitLayer = 0;
    public int UnitLayer => _unitLayer;

    [Header("Physics Settings")]
    [Tooltip("Конфиг физики шара (ScriptableObject). Если не назначен — используется дефолтный конфиг из префаба драйвера.")]
    [SerializeField] private BallCharacterConfig _characterConfig;
    public BallCharacterConfig CharacterConfig => _characterConfig;

    [Header("Visual Settings")]
    [Tooltip("Показывать ли дебаг-капсулу физического драйвера (PhisicsDriver)")]
    [SerializeField] private bool _showDriverCapsule = false;

    [Tooltip("Рендереры/меши, которые скрываются при виде от 1-го лица (например: голова, шлем, тело)")]
    [SerializeField] private List<Renderer> _hideInFirstPerson = new List<Renderer>();

    [Tooltip("Объекты, которые скрываются при виде от 3-го лица (например: руки или оружие 1-го лица)")]
    [SerializeField] private List<GameObject> _hideInThirdPerson = new List<GameObject>();

    private AvatarConstructor _avatarConstructor;
    private AvatarVisuals _visuals;
    private Transform _capsuleTransform;

    public AvatarVisuals Visuals => _visuals;

    private void Awake()
    {
        _avatarConstructor = new AvatarConstructor(this);
        var ballController = _avatarConstructor.CreateDriver();
        _driver = ballController;
        _cameraTarget = _driver.CameraTarget;

        if (_characterConfig != null && ballController != null)
        {
            ballController.SetConfig(_characterConfig);
        }

        // Применяем слой и отключаем самостолкновения внутри юнита (устраняет дерганье)
        AvatarCollisionResolver.Setup(gameObject, _avatarConstructor.BallDriver, _unitLayer);

        _visuals = new AvatarVisuals(
            _avatarConstructor.BallDriver,
            _showDriverCapsule,
            _hideInFirstPerson,
            _hideInThirdPerson
        );
    }

    public void SetFirstPersonView() => _visuals?.SetFirstPersonView();
    public void SetThirdPersonView() => _visuals?.SetThirdPersonView();

    
    public void SetDriverCapsuleVisible(bool isVisible)
    {
        _showDriverCapsule = isVisible;
        _visuals?.SetDriverCapsuleVisible(isVisible);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && _visuals != null)
        {
            _visuals.SetDriverCapsuleVisible(_showDriverCapsule);
        }

        if (Application.isPlaying && _driver is BallCharacterController controller && _characterConfig != null)
        {
            controller.SetConfig(_characterConfig);
        }

        if (Application.isPlaying && _avatarConstructor != null)
        {
            AvatarCollisionResolver.Setup(gameObject, _avatarConstructor.BallDriver, _unitLayer);
        }
    }
#endif
}
