using System.Collections;
using UnityEngine;

namespace Code.Infrastracture.Logic
{
    public class LoadingCurtain : MonoBehaviour
    {
        public CanvasGroup Curtain;
        private float _deltaTime = 0.03f;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void Show()
        {
            gameObject.SetActive(true); 
            Curtain.alpha = 1;
        }

        public void Hide() => StartCoroutine((FadeIn()));

        private IEnumerator FadeIn()
        {
            while (Curtain.alpha > 0 )
            {
                Curtain.alpha -= _deltaTime;
                yield return new WaitForSeconds(_deltaTime);
            }
            gameObject.SetActive(false);
        }
    }
}
