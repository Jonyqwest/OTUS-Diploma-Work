using System.Collections;
using Core.Camera;
using Core.Character.Capsule;
using Core.Character.Observer;
using UnityEngine;

public class CameraFasade : MonoBehaviour
{
    public Transform playerHead;
    public CameraRig rig;
    public CamConfigurator camConfigurator;
    
    CameraSystem cam;
    private CameraType _cameraType;
    DesktopCameraDevice desktop;
    OpenXRCameraDevice vrDevice;

    public bool IsThirdPerson => isThirdPerson;
    bool isThirdPerson;
    private bool _isInitialized = false;
    private Coroutine _transitionCoroutine;
    private IControllable _currentUnit;

    void Awake()
    {
        EnsureInitialized();
    }

    void Start()
    {
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (_isInitialized && cam != null) return;
        _isInitialized = true;

        if (rig == null)
        {
            Debug.LogError("[CameraFasade] rig не назначен!");
            return;
        }

        cam = new CameraSystem(rig);

        // ШАГ 1: Создаем и регистрируем устройство ввода (Desktop или VR)
        if (DeviceDetectUtility.VrIsAvailable())
        {
            _cameraType = CameraType.Vr;
            vrDevice = new OpenXRCameraDevice(rig.CameraVR.transform);
            cam.SetDevice(vrDevice);
            rig.MovementPivot.GetComponent<MovementPivotController>().SetDevice(vrDevice);
            Debug.Log("[CameraFasade] Using VR Camera Device: " + rig.CameraVR.name);
        }
        else
        {
            _cameraType = CameraType.Desktop;
            desktop = new DesktopCameraDevice();
            cam.SetDevice(desktop);
            rig.MovementPivot.GetComponent<MovementPivotController>().SetDevice(desktop);
            Debug.Log("[CameraFasade] Using Desktop Camera Device: " + rig.CameraMono.name);
        }

        // ШАГ 2: Передаем конфигурацию настроек
        cam.InitConfig(camConfigurator);

        // ШАГ 3: Теперь включаем режим камеры (все настройки и девайсы уже готовы!)
        if (_cameraType == CameraType.Vr)
        {
            SetViewMode(CameraViewMode.FirstPerson);
        }
        else
        {
            SetViewMode(CameraViewMode.ThirdPerson);
        }

        CheckCamerasEnabled(_cameraType);
    }
    
    /// <summary>
    /// Перенацеливает камеру на юнит с поддержкой кинематографичного перелета и запоминанием режима.
    /// </summary>
    public void SwitchUnitTarget(IControllable unit, float flightDuration = 0.8f)
    {
        EnsureInitialized();
        if (unit == null || cam == null) return;

        _currentUnit = unit;
        Transform target = unit.CameraTarget;
        CameraViewMode savedMode = unit.CurrentViewMode;

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        _transitionCoroutine = StartCoroutine(TransitionToTargetCoroutine(target, savedMode, flightDuration));
    }

    private IEnumerator TransitionToTargetCoroutine(Transform target, CameraViewMode finalMode, float flightDuration)
    {
        playerHead = target;

        // Включаем 3-е лицо для красивого подлета сверху/сзади
        SetViewMode(CameraViewMode.ThirdPerson);

        if (finalMode == CameraViewMode.FirstPerson)
        {
            yield return new WaitForSeconds(flightDuration);

            // Мягко ныряем в вид от первого лица
            SetViewMode(CameraViewMode.FirstPerson);
        }

        _transitionCoroutine = null;
    }

    /// <summary>
    /// Прямая установка режима камеры.
    /// </summary>
    public void SetViewMode(CameraViewMode mode)
    {
        EnsureInitialized();
        isThirdPerson = (mode == CameraViewMode.ThirdPerson);

        ICameraMode camMode = isThirdPerson ? (ICameraMode)new ThirdPersonMode() : new FirstPersonMode();
        cam.SetMode(camMode, playerHead);

        if (_currentUnit != null)
        {
            _currentUnit.CurrentViewMode = mode;
        }
    }

    public void SetTarget(Transform newTarget, bool smooth = false, float duration = 1f)
    {
        EnsureInitialized();
        if (newTarget == null || cam == null) return;

        playerHead = newTarget;
        ICameraMode nextMode = isThirdPerson ? (ICameraMode)new ThirdPersonMode() : new FirstPersonMode();
        cam.SetMode(nextMode, playerHead);
    }

    void Update()
    {
        if (cam == null) return;

        cam.Tick(Time.deltaTime); 
        
        // Переключение вида по клавише V
        if (Input.GetKeyDown(KeyCode.V))
        {
            CameraViewMode newMode = isThirdPerson ? CameraViewMode.FirstPerson : CameraViewMode.ThirdPerson;
            SetViewMode(newMode);
        }
    }

    private void CheckCamerasEnabled(CameraType type)
    {
        if (type == CameraType.Desktop)
        {
            if (rig.CameraMono != null) rig.CameraMono.gameObject.SetActive(true);
            if (rig.CameraVR != null && rig.CameraVR.transform.parent != null && rig.CameraVR.transform.parent.parent != null)
                rig.CameraVR.transform.parent.parent.gameObject.SetActive(false);
            if (rig.CameraStereoL != null) rig.CameraStereoL.gameObject.SetActive(false);
            if (rig.CameraStereoR != null) rig.CameraStereoR.gameObject.SetActive(false);
        }
        if (type == CameraType.Vr)
        {
            if (rig.CameraMono != null) rig.CameraMono.gameObject.SetActive(false);
            if (rig.CameraVR != null && rig.CameraVR.transform.parent != null && rig.CameraVR.transform.parent.parent != null)
                rig.CameraVR.transform.parent.parent.gameObject.SetActive(true);
            if (rig.CameraStereoL != null) rig.CameraStereoL.gameObject.SetActive(false);
            if (rig.CameraStereoR != null) rig.CameraStereoR.gameObject.SetActive(false);
        }
        rig.Arm.gameObject.SetActive(true);
    }
}