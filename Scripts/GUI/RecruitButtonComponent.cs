using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    [RequireComponent(typeof(ScaleTweener))]
    public class RecruitButtonComponent : MonoBehaviour, IPointerClickHandler
    {
        public int cooldownInSeconds = 5;
        public ProgressBar progressBar;
        public TextMeshProUGUI timerText;
        
        public event Action OnCooldownOver;
        public event Action OnRecruitment;

        private Coroutine _timerCoroutine;
        private ScaleTweener _scaleTweener;
        private GameplayManager _gameplayManager;
        
        private void Start()
        {
            progressBar = this.GetOrElseAddComponent<ProgressBar>();
            _scaleTweener = this.GetOrElseAddComponent<ScaleTweener>();
            _gameplayManager = GameObject.FindObjectOfType<GameplayManager>();

            // disable timer text when it's done
            OnCooldownOver += () =>
            {
                if (timerText != null) timerText.gameObject.SetActive(false);
            };

            ResetTimer();
        }

        private void ResetTimer()
        {
            progressBar.current = -1;
            progressBar.max = cooldownInSeconds;
            
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            if (timerText != null) timerText.gameObject.SetActive(true);

            // every second increase the time
            _timerCoroutine = this.RunAsync(
                () =>
                {
                    progressBar.current = Mathf.Clamp(progressBar.current + 1, 0, progressBar.max);
                    if (progressBar.isFull) OnOnCooldownOver();

                    if (timerText != null)
                    {
                        // floor the division as 0.98 minutes is 0 minutes and x seconds (handled by the next variable)
                        var minutes = (int) (progressBar.unitsLeft / 60f);
                        var seconds = progressBar.unitsLeft % 60;
                        
                        var txt = $"{minutes}:{(seconds < 10 ? "0" : "")}{seconds}";
                        timerText.text = txt;
                    }
                },
                () => this == null,
                GameObjectHelpers.RoutineTypes.TimeInterval,
                1f
            );
        }

        private void Update()
        {
            // disable scale on hover while timer bar is not full
            _scaleTweener.enableOnHover = progressBar.isFull;
        }

        protected virtual void OnOnRecruitment() { OnRecruitment?.Invoke(); }
        protected virtual void OnOnCooldownOver() { OnCooldownOver?.Invoke(); }

        public void OnPointerClick(PointerEventData eventData)
        {
            // just reset counter and call event, if progress bar is full!
            if (progressBar.isFull)
            {
                progressBar.current = 0;
                OnOnRecruitment();
                ResetTimer();
                _gameplayManager.ShuffleCards();
            }
            
        }
    }
}