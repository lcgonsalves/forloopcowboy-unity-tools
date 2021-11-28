using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class AccuracyProcessorComponent : SerializedMonoBehaviour
    {
        [CanBeNull] public AccuracyProcessor accuracyProcessor;
        
        public Vector3 lastRandomizedPosition = Vector3.zero;

        [Button]
        public void Randomize(List<AccuracyProcessor.VectorDimensions> dimensionsList)
        {
            if (!accuracyProcessor)
            {
                Debug.LogWarning("No accuracy processor present in component.");
                return;
            }
            lastRandomizedPosition = accuracyProcessor.GetScrambledPosition(transform, dimensionsList.ToArray());
        }

        private void OnDrawGizmosSelected()
        {
            if (accuracyProcessor)
            {
                Gizmos.color = new Color(1f, 0f, 0.83f);
                Gizmos.DrawSphere(transform.position, accuracyProcessor.minimumDeviance);
                Gizmos.DrawWireSphere(transform.position, accuracyProcessor.maximumDeviance);
            }
            
            Gizmos.color = new Color(0.62f, 1f, 0.4f);
            Gizmos.DrawSphere(lastRandomizedPosition, 0.1f);
        }
    }
}