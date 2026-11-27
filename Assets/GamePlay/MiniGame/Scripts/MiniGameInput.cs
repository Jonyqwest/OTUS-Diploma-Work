using System;
using UnityEngine;

namespace GamePlay.MiniGame
{
    public class MiniGameInput
    {
        public event Action<ChuteType> OnChuteSelected;
        public event Action OnContinuePressed;

        public void UpdateInput()
        {
            CheckKeyboard();
            CheckGamepad();
            CheckContinue();
        }

        private void CheckKeyboard()
        {
            // Прямые кнопки на 4 лотка (удобно на клавиатуре)
            if (Input.GetKeyDown(KeyCode.Q)) OnChuteSelected?.Invoke(ChuteType.TopLeft);
            else if (Input.GetKeyDown(KeyCode.A)) OnChuteSelected?.Invoke(ChuteType.BottomLeft);
            else if (Input.GetKeyDown(KeyCode.E)) OnChuteSelected?.Invoke(ChuteType.TopRight);
            else if (Input.GetKeyDown(KeyCode.D)) OnChuteSelected?.Invoke(ChuteType.BottomRight);

            // Стрелки с диагоналями
            else if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKeyDown(KeyCode.UpArrow)) OnChuteSelected?.Invoke(ChuteType.TopLeft);
            else if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKeyDown(KeyCode.DownArrow)) OnChuteSelected?.Invoke(ChuteType.BottomLeft);
            else if (Input.GetKey(KeyCode.RightArrow) && Input.GetKeyDown(KeyCode.UpArrow)) OnChuteSelected?.Invoke(ChuteType.TopRight);
            else if (Input.GetKey(KeyCode.RightArrow) && Input.GetKeyDown(KeyCode.DownArrow)) OnChuteSelected?.Invoke(ChuteType.BottomRight);
        }

        private void CheckGamepad()
        {
            // Опрос осей D-Pad / Стика
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (horizontal < -0.5f && vertical > 0.5f) OnChuteSelected?.Invoke(ChuteType.TopLeft);
            else if (horizontal < -0.5f && vertical < -0.5f) OnChuteSelected?.Invoke(ChuteType.BottomLeft);
            else if (horizontal > 0.5f && vertical > 0.5f) OnChuteSelected?.Invoke(ChuteType.TopRight);
            else if (horizontal > 0.5f && vertical < -0.5f) OnChuteSelected?.Invoke(ChuteType.BottomRight);
        }

        private void CheckContinue()
        {
            // Пробел, Enter или кнопка геймпада (кнопка 0 = 'A' на Xbox / 'X' на PS)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                OnContinuePressed?.Invoke();
            }
        }
    }
}