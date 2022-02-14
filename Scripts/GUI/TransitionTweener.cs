using System;
using System.Collections.Generic;
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
        public bool enableTranslation = true;
        public Vector3 from;
        public Vector3 to;
        public bool useLocalPosition = false;
        public bool startFromCurrentPosition = true;

        [Header("Rotation")]
        public bool enableRotation = false;
        public Vector3 fromRotation;
        public Vector3 toRotation;
        public bool useLocalRotation = false;
        public bool startFromCurrentRotation = true;

        [Serializable]
        public class TransitionPreset
        {
            public string name = "Untitled Preset";
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

            private TransitionPreset(TransitionTweener tweener)
            {
                this.tweenTransition = tweener.tweenTransition;
                this.tweenDuration = tweener.tweenDuration;
                this.untweenTransition = tweener.untweenTransition;
                this.untweenDuration = tweener.untweenDuration;
                this.@from = tweener.@from;
                this.to = tweener.to;
                this.useLocalPosition = tweener.useLocalPosition;
                this.startFromCurrentPosition = tweener.startFromCurrentPosition;
                this.fromRotation = tweener.fromRotation;
                this.toRotation = tweener.toRotation;
                this.useLocalRotation = tweener.useLocalRotation;
                this.startFromCurrentRotation = tweener.startFromCurrentRotation;
            }

            public static TransitionPreset FromTweener(TransitionTweener tweener)
            {
                return new TransitionPreset(tweener);
            }
        }

        [SerializeField] private List<TransitionPreset> presets = new List<TransitionPreset>();

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

        [ContextMenu("Save settings to preset.")]
        public void SaveToPreset()
        {
            presets.Add(TransitionPreset.FromTweener(this));
        }

        [ContextMenu("Tween")]
        public void Tween()
        {
            Tween(true);
        }

        public void Tween(string presetName, bool interrupt)
        {
            var presetWithName = presets.Find(_ => _.name == presetName);
            if (presetWithName != null) Tween(presetWithName, interrupt);
        }
        
        public void Untween(string presetName, bool interrupt)
        {
            var presetWithName = presets.Find(_ => _.name == presetName);
            if (presetWithName != null) Untween(presetWithName, interrupt);
        }

        /// <summary>
        /// This overrides the current settings of the game object. If you want to keep
        /// these settings save them to a preset.
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="interrupt"></param>
        public void Tween(TransitionPreset preset, bool interrupt)
        {
            ApplyPreset(preset);
            Tween(interrupt);
        }
        
        /// <summary>
        /// This overrides the current settings of the game object. If you want to keep
        /// these settings save them to a preset.
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="interrupt"></param>
        public void Untween(TransitionPreset preset, bool interrupt)
        {
            ApplyPreset(preset);
            Untween(interrupt);
        }

        private void ApplyPreset(TransitionPreset preset)
        {
            this.tweenTransition = preset.tweenTransition;
            this.tweenDuration = preset.tweenDuration;
            this.untweenTransition = preset.untweenTransition;
            this.untweenDuration = preset.untweenDuration;
            this.@from = preset.@from;
            this.to = preset.to;
            this.useLocalPosition = preset.useLocalPosition;
            this.startFromCurrentPosition = preset.startFromCurrentPosition;
            this.fromRotation = preset.fromRotation;
            this.toRotation = preset.toRotation;
            this.useLocalRotation = preset.useLocalRotation;
            this.startFromCurrentRotation = preset.startFromCurrentRotation;
        }
        
        [ContextMenu("UnTween")]
        public void Untween()
        {
            Untween(true);
        }

        // perhaps not the most precise naming, but i like the sound of "slerp"
        public void SlerpTo(Vector3 destination, float duration, bool local = true, bool interrupt = true)
        {
            tweenDuration = duration;
            startFromCurrentPosition = true;
            useLocalPosition = local;
            to = destination;
            Tween(interrupt);
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

            currentTween = tweenTransition.PlayOnceOnFixedUpdateWithDuration(
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

            if (enableTranslation)
            {
                if (useLocalPosition) transform.localPosition = lerpedPosition;
                else transform.position = lerpedPosition;
            }

            if (enableRotation)
            {
                if (useLocalRotation) transform.localRotation = lerpedRotation;
                else transform.rotation = lerpedRotation;
            }
        }
    }
}