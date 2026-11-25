using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.MiniGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MiniGameView : MonoBehaviour
    {
        [Header("Canvas & Fading")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("UI Score")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _tallyBonusText;

        [Header("Continue UI")]
        [SerializeField] private GameObject _angelNotifier;
        [SerializeField] private GameObject _continuePanel;
        [SerializeField] private Button _continueButton;
        [Header("Demo Mode UI")]
        [SerializeField] private GameObject _takeControlPrompt; // "Нажмите [ любую Клавишу],  чтобы начать миниигру"

        [Header("Catcher Visuals (4 позиции)")]
        [SerializeField] private GameObject _catcherTopLeft;
        [SerializeField] private GameObject _catcherBottomLeft;
        [SerializeField] private GameObject _catcherTopRight;
        [SerializeField] private GameObject _catcherBottomRight;

        [Header("Chutes (Начало и конец лотков)")]
        [Tooltip("0: TopLeft, 1: BottomLeft, 2: TopRight, 3: BottomRight")]
        [SerializeField] private Transform[] _chuteStartPoints;
        [SerializeField] private Transform[] _chuteEndPoints;
        [SerializeField] private GameObject _ringPrefab;
        [SerializeField] private Transform _ringsContainer;

        [Header("Touch Buttons (Для мобилок/мышки)")]
        [SerializeField] private Button _btnTopLeft;
        [SerializeField] private Button _btnBottomLeft;
        [SerializeField] private Button _btnTopRight;
        [SerializeField] private Button _btnBottomRight;

        // События UI для Презентера
        public event Action<ChuteType> OnChuteClicked;
        public event Action OnContinueClicked;

        private readonly Dictionary<int, GameObject> _spawnedRings = new Dictionary<int, GameObject>();

        
        private void Awake()
        {
            Init();
        }

         
        public void SetScore(int score)
        {
            if (_scoreText)
                _scoreText.text = score.ToString("D3");
        }

        
        public void SetCatcherPosition(ChuteType pos)
        {
            if (_catcherTopLeft) _catcherTopLeft.SetActive(pos == ChuteType.TopLeft);
            if (_catcherBottomLeft) _catcherBottomLeft.SetActive(pos == ChuteType.BottomLeft);
            if (_catcherTopRight) _catcherTopRight.SetActive(pos == ChuteType.TopRight);
            if (_catcherBottomRight) _catcherBottomRight.SetActive(pos == ChuteType.BottomRight);
        }

        public void SetContinueActive(bool active)
        {
            if (_continuePanel) _continuePanel.SetActive(active);
            if (_angelNotifier) _angelNotifier.SetActive(active);
        }

        public void SpawnRing(int ringId)
        {
            if (_ringPrefab == null) return;
            Transform parent = _ringsContainer != null ? _ringsContainer : transform;
            GameObject ringGo = Instantiate(_ringPrefab, parent);
            _spawnedRings[ringId] = ringGo;
        }

        public void SetRingProgress(int ringId, ChuteType chute, float progress)
        {
            if (!_spawnedRings.TryGetValue(ringId, out GameObject go)) return;
            int idx = (int)chute;

            if (idx < _chuteStartPoints.Length && idx < _chuteEndPoints.Length)
            {
                Vector3 start = _chuteStartPoints[idx].position;
                Vector3 end = _chuteEndPoints[idx].position;
                go.transform.position = Vector3.Lerp(start, end, progress);
            }
        }

        public void DestroyRing(int ringId)
        {
            if (_spawnedRings.TryGetValue(ringId, out GameObject go))
            {
                Destroy(go);
                _spawnedRings.Remove(ringId);
            }
        }

        // --- ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ---

        public void FadeIn(float duration, Action onComplete = null) =>
            StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));

        public void FadeOut(float duration, Action onComplete = null) =>
            StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));

        public void PlayTallyAnimation(int totalScore, Action onComplete = null) =>
            StartCoroutine(TallyRoutine(totalScore, onComplete));

        private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        private IEnumerator TallyRoutine(int totalScore, Action onComplete)
        {
            if (_tallyBonusText != null && totalScore > 0)
            {
                _tallyBonusText.gameObject.SetActive(true);
                int currentTally = 0;
                int remainingScore = totalScore;
                float stepDelay = Mathf.Clamp(1.0f / totalScore, 0.02f, 0.08f);

                while (remainingScore > 0)
                {
                    remainingScore--;
                    currentTally++;
                    SetScore(remainingScore);
                    _tallyBonusText.text = $"+{currentTally} КОЛЕЦ!";
                    yield return new WaitForSeconds(stepDelay);
                }

                yield return new WaitForSeconds(0.4f);
            }

            onComplete?.Invoke();
        }
        
        public void SetTakeControlPromptActive(bool active)
        {
            if (_takeControlPrompt != null)
                _takeControlPrompt.SetActive(active);
        }
        
        private void Init()
        {
            if (_canvasGroup)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_continueButton)
                _continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());

            if (_btnTopLeft) _btnTopLeft.onClick.AddListener(() => OnChuteClicked?.Invoke(ChuteType.TopLeft));
            if (_btnBottomLeft) _btnBottomLeft.onClick.AddListener(() => OnChuteClicked?.Invoke(ChuteType.BottomLeft));
            if (_btnTopRight) _btnTopRight.onClick.AddListener(() => OnChuteClicked?.Invoke(ChuteType.TopRight));
            if (_btnBottomRight) _btnBottomRight.onClick.AddListener(() => OnChuteClicked?.Invoke(ChuteType.BottomRight));

            SetContinueActive(false);
            
            if (_tallyBonusText) _tallyBonusText.gameObject.SetActive(false);
            if (_angelNotifier) _angelNotifier.SetActive(false);
        }
    }
}