using System;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    public class ScaleTweener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Coroutine scaleTween = null;
        public Transition scaleTweenTransition;
        public Vector3 initialScale = new Vector3(1, 1, 1);
        public Vector3 hoverScale = new Vector3(1, 1, 1);
        public float lerpDuration = 0.3f;
        public Transform scaleTarget;
        private bool _enableOnHover = false;

        Transform target => scaleTarget ? scaleTarget : transform;
        
        public bool enableOnHover
        {
            get => _enableOnHover;
            set
            {
                // if we disable this while cursor is currently on hover,
                // we behave as if the cursor did leave.
                if (!value) OnPointerExit(null);

                _enableOnHover = value;
            }
        }

        private void Start()
        {
            target.localScale = initialScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enableOnHover) return;
            
            if (scaleTween != null)
            {
                StopCoroutine(scaleTween);
            }
            
            Vector3 startingScale = target.localScale;
            scaleTween = scaleTweenTransition.PlayOnceOnFixedUpdateWithDuration(
                this,
                state => target.localScale = Vector3.Lerp(startingScale, hoverScale, state.Snapshot()),
                onFinishState =>
                {
                    target.localScale = hoverScale;
                    scaleTween = null;
                },
                lerpDuration
            );
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (scaleTween != null)
            {
                StopCoroutine(scaleTween);
            }
        
            Vector3 startingScale = target.localScale;
            scaleTween = scaleTweenTransition.PlayOnceOnFixedUpdateWithDuration(
                this,
                state => target.localScale = Vector3.Lerp(startingScale, initialScale, state.Snapshot()),
                onFinishState =>
                {
                    target.localScale = hoverScale;
                    scaleTween = null;
                },
                lerpDuration
            );
        }
        
    }
}