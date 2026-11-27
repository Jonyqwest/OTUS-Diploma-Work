namespace WorldTile
{ 
    using UnityEngine;
    using UnityEngine.UIElements;

    public class TileProgressUI : MonoBehaviour
    {
        private ProgressBar uiProgressBar;
        private Label progressLabel;
        private int lastPercent = int.MinValue;
        private bool isVisuallyReset;

        void OnEnable()
        {
            // A disabled/re-enabled UIDocument keeps its visual tree. Detach old
            // runtime elements so repeated enables cannot accumulate duplicates.
            uiProgressBar?.RemoveFromHierarchy();
            progressLabel?.RemoveFromHierarchy();

            // Получаем корень UI-документа
            var root = GetComponent<UIDocument>().rootVisualElement;

            // Создаём прогрессбар
            uiProgressBar = new ProgressBar
            {
                //title = " LOADIND WORLD..",
                value = 0,
                lowValue = 0,
                highValue = 100,
                 
            };

            progressLabel = new Label("0%")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleRight,
                    color = Color.white,
                    //fontSize = 8,
                    marginTop = 4,
                    //height = 10
                }
            };

            // Добавляем в UI
            root.Add(uiProgressBar);
            root.Add(progressLabel);
            lastPercent = 0;
            isVisuallyReset = false;
        }

        void OnDisable()
        {
            uiProgressBar?.RemoveFromHierarchy();
            progressLabel?.RemoveFromHierarchy();
            uiProgressBar = null;
            progressLabel = null;
            lastPercent = int.MinValue;
            isVisuallyReset = true;
        }

        /// <summary>
        /// Вызывается при обновлении прогресса (0–1)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            int percent = (int)(progress * 100f);

            // Completion used to assign "100%" and immediately replace it with an
            // empty string on every LateUpdate. Preserve the reset behaviour, but
            // perform it only once for a completed progress value.
            if (percent >= 100)
            {
                if (lastPercent == 100 && isVisuallyReset)
                    return;

                ResetProgress();
                lastPercent = 100;
                return;
            }

            if (percent == lastPercent && !isVisuallyReset)
                return;

            if (uiProgressBar == null || progressLabel == null)
                return;

            uiProgressBar.value = progress * 100f;
            progressLabel.text = $"{percent}%";
            lastPercent = percent;
            isVisuallyReset = false;
        }

        /// <summary>
        /// Сбросить прогресс
        /// </summary>
        public void ResetProgress()
        {
            if (uiProgressBar == null || progressLabel == null)
                return;

            if (isVisuallyReset)
            {
                lastPercent = int.MinValue;
                return;
            }

            uiProgressBar.value = 0;
            progressLabel.text = "";
            lastPercent = int.MinValue;
            isVisuallyReset = true;
        }
    }
}
