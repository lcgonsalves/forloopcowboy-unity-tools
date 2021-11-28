using System;
using System.Collections.Generic;
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
    [CreateAssetMenu(fileName = "Untitled Accuracy Settings", menuName = "Settings/New Accuracy Settings...", order = 0)]
    public class AccuracyProcessor : SerializedScriptableObject
    {
        public enum VectorDimensions { X, Y, Z }

        public float minimumDeviance, maximumDeviance;

        /// <summary>
        /// Wrapper that allows for modifying the deviance values without affecting asset.
        /// </summary>
        public class ScrambledTransform
        {
            public Transform transform;
            public GameObject gameObject => transform.gameObject;

            public Vector3 position =>
                ScramblePosition(transform.position, minimumDeviance, maximumDeviance, dimensionsToScramble);

            /// <summary> Scrambles position only for the selected dimensions </summary>
            public Vector3 GetScrambledPosition(params VectorDimensions[] newDimensionsToScramble) =>
                ScramblePosition(transform.position, minimumDeviance, maximumDeviance, newDimensionsToScramble);

            public float minimumDeviance, maximumDeviance;
            public VectorDimensions[] dimensionsToScramble;
            
            internal ScrambledTransform(Transform transform, float minimumDeviance, float maximumDeviance, VectorDimensions[] dimensionsToScramble)
            {
                this.transform = transform;
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
        /// <param name="dimensionsToScramble">Whether to translate the vector vertically, horizontally, or depthwise.</param>
        /// <returns>The scrambled vector.</returns>
        public static Vector3 ScramblePosition(
            Vector3 position,
            float minimumDeviance,
            float maximumDeviance,
            params VectorDimensions[] dimensionsToScramble
        ) {
            Assert.IsTrue(minimumDeviance >= 0, "minimumDeviance >= 0");
            Assert.IsTrue(maximumDeviance >= 0 && maximumDeviance > minimumDeviance, "maximumDeviance >= 0 && maximumDeviance > minimumDeviance");
            
            Vector3 result = new Vector3(position.x, position.y, position.z);

            foreach (var dimension in dimensionsToScramble)
            {
                int sign = RandomExtended.Boolean() ? -1 : 1;
                float value = sign * Random.Range(minimumDeviance, maximumDeviance);
                
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
        /// <param name="dimensionsToScramble"></param>
        /// <returns></returns>
        public ScrambledTransform Scramble(Transform transform, params VectorDimensions[] dimensionsToScramble)
        {
            return transform.GetScrambled(minimumDeviance, maximumDeviance, dimensionsToScramble);
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
                dimensionsToScramble
            );
        }
    }

    public static class TransformAccuracyExtension
    {
        /// <summary>
        /// Returns scrambled transform that scrambles on all dimensions.
        /// </summary>
        public static AccuracyProcessor.ScrambledTransform GetScrambled(
            this Transform t,
            float minimumDeviance,
            float maximumDeviance
        )
        {
            return t.GetScrambled(
                minimumDeviance, 
                maximumDeviance, 
                AccuracyProcessor.VectorDimensions.X,
                AccuracyProcessor.VectorDimensions.Y, 
                AccuracyProcessor.VectorDimensions.Z
            );
        }
        
        public static AccuracyProcessor.ScrambledTransform GetScrambled(
            this Transform t,
            float minimumDeviance,
            float maximumDeviance,
            params AccuracyProcessor.VectorDimensions[] dimensionsToScramble
        )
        {
            return new AccuracyProcessor.ScrambledTransform(t, minimumDeviance, maximumDeviance, dimensionsToScramble);
        }
        }
}