using System;
using System.Collections;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    /// Defines a transition.
    [CreateAssetMenu]
    public class Transition : ScriptableObject
    {
        public float duration = 1f;

        public float amplitude = 1f;

        [Tooltip("Instant transitions return every evaluate call at the end of the curve and only fire onEvaluation events.")]
        public bool instant = false;

        // Transition is defined by being evaluatable between 0 and 1 always (enforced by custom editor).
        public AnimationCurve transition = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public Transition(float duration, float amplitude) {
            this.duration = duration;
            this.amplitude = amplitude;
        }

        [System.Serializable]
        public class TransitionState
        {

            /// return true if transition is finished
            public bool finished { get; private set; }

            /// Returns true if the transition has started and has not finished
            public bool isPlaying { get; private set; }

            /// Initialized to from transition scriptable object, but individual instances can be altered.
            public float duration;

            public event Action<float> onFinish;
            public event Action<float> onStart;
            public event Action<float> onEvaluation;

            public float currentAnimationTime { get; private set; }
            public float currentAnimationValue { get; private set; }

            // Internal constructor and local references
            private readonly AnimationCurve transition;

            private readonly bool isInstant;

            protected internal TransitionState(in float duration, in AnimationCurve curve, bool instant)
            {
                this.duration = duration;
                this.transition = curve;
                this.isInstant = instant;
                this.currentAnimationTime = 0f;
                this.currentAnimationValue = Snapshot();

                onFinish += _ => { finished = true; isPlaying = false; };
            }

            public void Stop() { finished = true; }

            /// Resets animation time back to zero.
            /// Called at event on finish if loop is checked.
            public void ResetAnimation()
            {
                currentAnimationTime = 0f;
                isPlaying = false;
                finished = false;
            }

            /// Returns animation evaluated at current frame.
            public float Snapshot()
            {
                return currentAnimationValue;
            }

            /// Evaluates transition with a specific increment
            public float Evaluate(float increment)
            {
                float output;
                isPlaying = true;

                // Instant transitions evaluate at the end.
                if (isInstant || finished) {
                    output = Transition.Instant(transition);
                    onEvaluation?.Invoke(output);
                    return output;
                }

                bool justStarted = currentAnimationTime == 0f;

                currentAnimationTime = Mathf.Min(duration, currentAnimationTime + increment);

                output = transition.Evaluate(currentAnimationTime / duration);

                if (justStarted)
                {
                    output = transition.Evaluate(0);
                    onStart?.Invoke(output);
                }
                else if (currentAnimationTime >= duration) onFinish?.Invoke(output);
                else onEvaluation?.Invoke(output);

                // cache last value for reusages on snapshot
                currentAnimationValue = output;

                return output;
            }

            /// Evaluates transition at its current state using increment (defaults to DeltaTime).
            /// Use ResetAnimation() in order to reset the transition.
            /// Returns floating point value between zero and amplitude.
            public float Evaluate() { return Evaluate(Time.deltaTime); }

        }

        /// Returns the transition evaluated at its end 
        public static float Instant(AnimationCurve transition) {
            return transition.Evaluate(1);
        }

        /// Returns the transition evaluated in the beginning
        public static float Peek(AnimationCurve transition) {
            return transition.Evaluate(0);
        }
        public TransitionState GetPlayableInstance() { return new TransitionState(duration, transition, instant); }
        public TransitionState GetPlayableInstance(float overrideDuration) { return new TransitionState(overrideDuration, transition, instant); }

        /// Simple linear transition from [0, 0] => [1, 1]
        public static Transition Linear { get => new Transition(1, 1); }

        /// Plays animation once, calling the updating function at each pass.
        /// Updating function receives as a parameter the current state of the animation, between 0 and 1.
        /// TransitionState passed is reusable - it is the same instance each time, after being reevaluated.
        /// Callbacks can refer to this last value by calling Snapshot() on the state as this uses
        /// a cache to make it a bit more efficient in repeat usages.
        public IEnumerator PlayOnceWithUpdater(Action<TransitionState> updatingFunction, Action<TransitionState> onFinish, float updateInterval = 0f)
        {
            var transitionInstance = GetPlayableInstance();
            transitionInstance.onFinish += _ => onFinish(transitionInstance);

            while (!transitionInstance.finished)
            {
                var interval = Mathf.Max(updateInterval, Time.fixedDeltaTime);
                transitionInstance.Evaluate(interval);
                updatingFunction(transitionInstance);
                if (interval == Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
                else new WaitForSeconds(interval);
            }
        }

        public Coroutine PlayOnce(
            MonoBehaviour self,
            Action<TransitionState> updatingFunction, 
            Action<TransitionState> onFinish,
            float updateInterval = 0f
        ){
            return self.StartCoroutine(PlayOnceWithUpdater(updatingFunction, onFinish, updateInterval));
        }

        public Coroutine PlayOnce(
            MonoBehaviour self,
            Action<TransitionState> updatingFunction, 
            float updateInterval = 0f
        ){
            return PlayOnce(self, updatingFunction, _ => {}, updateInterval); 
        }

    }
}
