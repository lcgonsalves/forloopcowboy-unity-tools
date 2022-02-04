using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    [Serializable]
    public class ClampedFloat
    {
        [SerializeField]
        private ClampedFloatSettings settings;

        public ClampedFloat(ClampedFloatSettings settings)
        {
            this.settings = settings;
        }
        
        public float CurrentValue { get; private set; }

        /// <summary>
        /// Decrements counter by rate and returns its clamped value.
        /// </summary>
        public float Decrement(float amount)
        {
            CurrentValue = Mathf.Clamp(
                CurrentValue - amount,
                settings.min,
                settings.max
            );

            return CurrentValue;
        }
        
        public float Increment(float amount)
        {
            CurrentValue = Mathf.Clamp(
                CurrentValue + amount,
                settings.min,
                settings.max
            );

            return CurrentValue;
        }
        
    }

    [Serializable]
    public struct ClampedFloatSettings
    {
        [ValidateInput("MinMaxSanityCheck")]
        public float min;
        
        [ValidateInput("MinMaxSanityCheck")]
        public float max;

        [ValidateInput("DefaultWithinBounds")]
        public float defaultValue;

        /// <summary>
        /// Makes sure that values can't be set beyond one another.
        /// </summary>
        private bool MinMaxSanityCheck(float _)
        {
            if (min > max) min = max;
            return true;
        }

        private bool DefaultWithinBounds(float defVal)
        {
            defaultValue = Mathf.Clamp(defVal, min, max);
            return true;
        }

    }
    
}