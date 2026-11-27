using System.Collections.Generic;
using UnityEngine;


public class PCInputProvider : MonoBehaviour, IInputProvider
{
    
    [Header("Dependencies")]
    [Tooltip("Основная камера игрока. Если не назначена — будет искать камеру с тегом 'MainCamera'")]
   
    public Transform movementPivot; //← Назначьте в инспекторе так надежнее!

    [System.Obsolete]
    private void Awake()
    {
        // Автоматический поиск, если не назначена в инспекторе
        if (!movementPivot)
        {
            movementPivot = FindObjectOfType<MovementPivotController>().transform;
            if (!movementPivot)
                Debug.LogWarning("[PCInputProvider] В системе камер Не найден MovementPivot! Он указывает направление в сложной системе камер, по нему выбирается направление вперед.");
        }
    }

    //🧠 ФОРМИРУЕМ НАМЕРЕНИЯ
    public MovementIntent ReadIntent()
    {
        MovementIntent intent = new MovementIntent();

        //1. перемещение
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0f, v).normalized;
        
        intent.walkMode = Input.GetKey(KeyCode.LeftShift);
        if (intent.walkMode)
            intent.moveWalk = move;
        else
            intent.moveRun = move;

        // 2. Взгляд = направление камеры
        Vector3 fwd = movementPivot.forward;
        fwd.y = 0;
        intent.viewDirection = fwd.normalized;// куда смотрит MovementPivot там и перед
      

        // 3. Free Look = ПКМ
        intent.freeLook = Input.GetKey(KeyCode.Mouse2);// ПКМ = свободный взгляд
        // intent.freeLook = Input.GetKey(KeyCode.Numlock);

        intent.SetjumpPressed(Input.GetKeyDown(KeyCode.Space)); 
        intent.jumpHeld = Input.GetKey(KeyCode.Space);

        intent.slide = Input.GetKey(KeyCode.LeftControl);
        //intent.boostPressed = Input.GetKey(KeyCode.LeftShift);

        return intent;
    }
}
