using Core.Character.Controllers;
using UnityEngine;
using UnityEngine.UI;

public class BoostIndicator : MonoBehaviour
{
    public Image fillImage;
    private BallCharacterController controller;

    public void Init(BallCharacterController c) => controller = c;

    void Update()
    {
        //// Пример: fill = текущее значение / макс
        //float fill = controller.boostMeter / controller.config.boostDuration;
        //fillImage.fillAmount = fill;

        //// Можно менять цвет или анимацию при нажатой кнопке
        //if (controller.isBoostReady)
        //    fillImage.color = Color.cyan;
        //else
        //    fillImage.color = Color.blue;
    }
}
