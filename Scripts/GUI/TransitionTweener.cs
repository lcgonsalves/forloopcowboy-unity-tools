using System;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.HUD
{
    public class TransitionTweener : MonoBehaviour
    {
        public Transition tweenTransition;
        public float tweenDuration;
        
        public Transition untweenTransition;
        public float untweenDuration;
        
        [Header("Translation")]
        public Vector3 from;
        public Vector3 to;
        public bool useLocalPosition = false;
        public bool startFromCurrentPosition = true;

        [Header("Rotation")]
        public Vector3 fromRotation;
        public Vector3 toRotation;
        public bool useLocalRotation = false;
        public bool startFromCurrentRotation = true;

        public bool runOnStart = false;

        private void Start()
        {
            if (runOnStart)
                Tween();
        }

        private Coroutine currentTween = null;

        [ContextMenu("Apply Local (FROM)")]
        public void ApplyCurrentLocalTransformFrom()
        {
            from = transform.localPosition;
            fromRotation = transform.localRotation.eulerAngles;
        }
        
        [ContextMenu("Apply Global (FROM)")]
        public void ApplyCurrentGlobalTransformFrom()
        {
            from = transform.position;
            fromRotation = transform.rotation.eulerAngles;
        }
        
        [ContextMenu("Apply Local (TO)")]
        public void ApplyCurrentLocalTransformTo()
        {
            to = transform.localPosition;
            toRotation = transform.localRotation.eulerAngles;
        }
        
        [ContextMenu("Apply Global (TO)")]
        public void ApplyCurrentGlobalTransformTo()
        {
            to = transform.position;
            toRotation = transform.rotation.eulerAngles;
        }


        [ContextMenu("Tween")]
        public void Tween()
        {
            Tween(true);
        }
        
        [ContextMenu("UnTween")]
        public void Untween()
        {
            Untween(true);
        }
        
        public void Tween(bool interrupt)
        {
            if (currentTween != null && interrupt) StopCoroutine(currentTween);
            
            Vector3 startingPosition;
            Quaternion startingRotation;
            
            if (!startFromCurrentPosition)
            {
                if (useLocalPosition) transform.localPosition = from;
                else transform.position = from;
            }

            if (!startFromCurrentRotation)
            {
                if (useLocalRotation) transform.localRotation = Quaternion.Euler(fromRotation);
                else transform.rotation = Quaternion.Euler(fromRotation);
            }

            startingPosition = useLocalPosition ? transform.localPosition : transform.position;
            startingRotation = useLocalRotation ? transform.localRotation : transform.rotation;

            currentTween = tweenTransition.PlayOnceWithDuration(
                this,
                _ => TweenTransition(_, startingPosition, startingRotation, to, Quaternion.Euler(toRotation)),
                _ => TweenTransition(_, startingPosition, startingRotation, to, Quaternion.Euler(toRotation)),
                tweenDuration
            );
        }

        public void Untween(bool interrupt)
        {
            if (currentTween != null && interrupt) StopCoroutine(currentTween);
            
            Vector3 startingPosition;
            Quaternion startingRotation;
            
            if (!startFromCurrentPosition)
            {
                if (useLocalPosition) transform.localPosition = to;
                else transform.position = to;
            }

            if (!startFromCurrentRotation)
            {
                if (useLocalRotation) transform.localRotation = Quaternion.Euler(toRotation);
                else transform.rotation = Quaternion.Euler(toRotation);
            }

            startingPosition = useLocalPosition ? transform.localPosition : transform.position;
            startingRotation = useLocalRotation ? transform.localRotation : transform.rotation;

            currentTween = untweenTransition.PlayOnceWithDuration(
                this,
                _ => TweenTransition(_, startingPosition, startingRotation, from, Quaternion.Euler(fromRotation)),
                _ => TweenTransition(_, startingPosition, startingRotation, from, Quaternion.Euler(fromRotation)),
                untweenDuration
            );
        }

        private void TweenTransition(Transition.TransitionState state, Vector3 startingPos, Quaternion startingRot, Vector3 endPos, Quaternion endRot)
        {
            Vector3 lerpedPosition = Vector3.Lerp(startingPos, endPos, state.Snapshot());
            Quaternion lerpedRotation = Quaternion.Lerp(startingRot, endRot, state.Snapshot());

            if (useLocalPosition) transform.localPosition = lerpedPosition;
            else transform.position = lerpedPosition;

            if (useLocalRotation) transform.localRotation = lerpedRotation;
            else transform.rotation = lerpedRotation;
        }
    }
}