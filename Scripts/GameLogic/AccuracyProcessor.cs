using System;
using forloopcowboy_unity_tools.Scripts.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// <summary>
    /// Defines methods for scrambling a given position based on the predefined settings.
    /// </summary>
    [CreateAssetMenu(fileName = "Untitled Accuracy Settings", menuName = "Settings.../New Accuracy Settings", order = 0)]
    public class AccuracyProcessor : SerializedScriptableObject
    {
        public enum VectorDimensions { X, Y, Z }

        public float minimumDeviance, maximumDeviance;
        
        [Tooltip("0 means raw random value between min and max. 1 means always hitting minimumDeviance. 0.5 lerps between the random point and the minimum.")]
        public float biasTowardCentre = 0f;

        /// <summary>
        /// Wrapper that allows for modifying the deviance values without affecting asset.
        /// </summary>
        public class ScrambledTransform
        {
            public Transform transform;
            
            public float biasTowardCentre = 0f;
            public GameObject gameObject => transform.gameObject;
            
            
            /// <summary>
            /// Calculates scrambled position based on the range.
            /// If the character's weapon has range of 10 and it is 9 units away from target,
            /// the range value should be '0.9', which in this case will mean that the
            /// deviances will be cut by 10% (as character is closer to target so deviation shouldn't be so extreme).
            /// </summary>
            /// <param name="range">As a factor of the accessible range of the weapon. Will be clamped between 0 and 1.</param>
            /// <param name="newDimensionsToScramble">Which dimensions should be used for scrambling.</param>
            /// <returns></returns>
            public Vector3 GetScrambledPositionAtRange(float range, params VectorDimensions[] newDimensionsToScramble)
            {
                range = Mathf.Clamp01(range);
                return 
                    lastScrambledPosition = ScramblePosition(
                        transform.position, 
                        range * minimumDeviance, 
                        range * maximumDeviance, 
                            biasTowardCentre,
                        newDimensionsToScramble
                    );
            }

            public Vector3 lastScrambledPosition { get; private set;  } = Vector3.zero;

            public float minimumDeviance, maximumDeviance;
            public VectorDimensions[] dimensionsToScramble;

            internal ScrambledTransform(Transform transform, float biasTowardCentre, float minimumDeviance, float maximumDeviance, VectorDimensions[] dimensionsToScramble)
            {
                this.transform = transform;
                this.biasTowardCentre = biasTowardCentre;
                this.minimumDeviance = minimumDeviance;
                this.maximumDeviance = maximumDeviance;
                this.dimensionsToScramble = dimensionsToScramble;
            }
        }

        /// <summary>
        /// Based on an initial position, returns a vector that has been translated
        /// in a random direction, in a random amount between the minimum and maximum deviances
        /// in all of the specified directions.
        /// </summary>
        /// <param name="position">Position to scramble.</param>
        /// <param name="minimumDeviance">Least amount of deviation possible.</param>
        /// <param name="maximumDeviance">Highest amount of deviation possible.</param>
        /// <param name="biasTowardsMinimum">0 means raw random value between min and max. 1 means always hitting minimumDeviance. 0.5 lerps between the random point and the minimum.</param>
        /// <param name="dimensionsToScramble">Whether to translate the vector vertically, horizontally, or depthwise.</param>
        /// <returns>The scrambled vector.</returns>
        public static Vector3 ScramblePosition(
            Vector3 position,
            float minimumDeviance,
            float maximumDeviance,
            float biasTowardsMinimum,
            params VectorDimensions[] dimensionsToScramble
        ) {
            Assert.IsTrue(minimumDeviance >= 0, "minimumDeviance >= 0");
            Assert.IsTrue(maximumDeviance >= 0 && maximumDeviance > minimumDeviance, "maximumDeviance >= 0 && maximumDeviance > minimumDeviance");

            if (dimensionsToScramble.Length == 0)
                dimensionsToScramble = new[] {VectorDimensions.X, VectorDimensions.Y, VectorDimensions.Z};
            
            Vector3 result = new Vector3(position.x, position.y, position.z);

            foreach (var dimension in dimensionsToScramble)
            {
                int sign = RandomExtended.Boolean() ? -1 : 1;
                float value = Random.Range(minimumDeviance, maximumDeviance);
                value = Mathf.Lerp(value, minimumDeviance, biasTowardsMinimum);
                value *= sign;
                
                switch (dimension)
                {
                    case VectorDimensions.X:
                        result.x += value;
                        break;
                    case VectorDimensions.Y:
                        result.y += value;
                        break;
                    case VectorDimensions.Z:
                        result.z += value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }
        
        /// <summary>
        /// Returns a wrapper that allows for easier scrambling.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="dimensionsToScramble">If none is specified, scrambles on all dimensions.</param>
        /// <returns></returns>
        public ScrambledTransform Scramble(Transform transform, params VectorDimensions[] dimensionsToScramble)
        {
            return new ScrambledTransform(transform, biasTowardCentre, minimumDeviance, maximumDeviance, dimensionsToScramble);
        }

        /// <summary>
        /// Based on the current position of the transform, returns a vector that has been translated
        /// in a random direction, in a random amount between the minimum and maximum deviances
        /// in all of the specified directions.
        /// </summary>
        /// <param name="transform">Transform used to get initial position.</param>
        /// <param name="dimensionsToScramble">Whether to translate the vector vertically, horizontally, or depthwise.</param>
        /// <returns>The scrambled vector.</returns>
        public Vector3 GetScrambledPosition(Transform transform, params VectorDimensions[] dimensionsToScramble)
        {
            return ScramblePosition(transform.position, dimensionsToScramble);
        }

        /// <summary>
        /// Based on an initial position, returns a vector that has been translated
        /// in a random direction, in a random amount between the minimum and maximum deviances
        /// in all of the specified directions.
        /// </summary>
        /// <param name="position">Position to scramble.</param>
        /// <param name="dimensionsToScramble">Whether to translate the vector vertically, horizontally, or depthwise.</param>
        /// <returns>The scrambled vector.</returns>
        public Vector3 ScramblePosition(Vector3 position, params VectorDimensions[] dimensionsToScramble)
        {
            return ScramblePosition(
                position,
                minimumDeviance,
                maximumDeviance,
                biasTowardCentre,
                dimensionsToScramble
            );
        }
    }
    
}