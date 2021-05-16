using System;
using UnityEngine;

/// Defines a transition.
[CreateAssetMenu]
public class Transition : ScriptableObject
{
    public float duration = 1f;

    public float amplitude = 1f;

    [Tooltip("Instant transitions return every evaluate call at the end of the curve and only fire onEvaluation events.")]
    public bool instant = false;

    // Transition is defined by being evaluatable between 0 and 1 always (enforced by custom editor).
    public AnimationCurve transition = AnimationCurve.Linear(0, 0, 1, 1);

    [System.Serializable]
    public class TransitionState
    {

        /// return true if transition is finished
        public bool finished { get; private set; }

        /// Returns true if the transition has started and has not finished
        public bool isPlaying { get; private set; }

        public event Action<float> onFinish;
        public event Action<float> onStart;
        public event Action<float> onEvaluation;

        public float currentAnimationTime { get; private set; }

        // Internal constructor and local references
        private readonly float duration;
        private readonly AnimationCurve transition;

        private readonly bool isInstant;

        protected internal TransitionState(in float duration, in AnimationCurve curve, bool instant)
        {
            this.duration = duration;
            this.transition = curve;
            this.isInstant = instant;

            onFinish += _ => finished = true; isPlaying = false;
        }

        /// Resets animation time back to zero.
        /// Called at event on finish if loop is checked.
        public void ResetAnimation()
        {
            currentAnimationTime = 0f;
            finished = false;
        }

        /// Evaluates transition with a specific increment
        public float Evaluate(float increment)
        {
            float output;

            // Instant transitions evaluate at the end.
            if (isInstant) {
                output = Transition.Instant(transition);
                onEvaluation?.Invoke(output);
                return output;
            }

            bool justStarted = currentAnimationTime == 0;

            increment = increment < 0 ? Time.deltaTime : increment;
            currentAnimationTime = Mathf.Min(duration, currentAnimationTime + increment);

            output = transition.Evaluate(currentAnimationTime / duration);

            if (justStarted)
            {
                output = transition.Evaluate(0);
                onStart?.Invoke(output);
            }
            else if (currentAnimationTime >= duration) onFinish?.Invoke(output);
            else onEvaluation?.Invoke(output);

            return output;
        }

        /// Evaluates transition at its current state using increment (defaults to DeltaTime).
        /// Use ResetAnimation() in order to reset the transition.
        /// Returns floating point value between zero and amplitude.
        public float Evaluate() { return Evaluate(-1); }

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

}
