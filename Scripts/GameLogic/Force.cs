using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// <summary>
    /// Copyright 2019 Kleber de Oliveira Andrade
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining a copy
    /// of this software and associated documentation files (the "Software"), to deal
    /// in the Software without restriction, including without limitation the rights
    /// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    /// copies of the Software, and to permit persons to whom the Software is
    /// furnished to do so, subject to the following conditions:

    /// The above copyright notice and this permission notice shall be included in all
    /// copies or substantial portions of the Software.

    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    /// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    /// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    /// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    /// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    /// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    /// SOFTWARE.
    /// </summary>
    public class Force : MonoBehaviour
    {
        public enum ForceType { Repulsion = -1, None = 0, Attraction = 1 }
        public ForceType m_Type;
        public Transform m_Pivot;
        public float m_Radius;
        public float m_StopRadius;
        public float m_Force;
        public LayerMask m_Layers;

        [CanBeNull] public HashSet<int> m_AffectedObjectIDs;

        private void FixedUpdate()
        {
            var pivotPosition = m_Pivot.position;
            
            Collider[] colliders = Physics.OverlapSphere(pivotPosition, m_Radius, m_Layers);
            var updater = PhysicsUpdate(m_Force, m_StopRadius, m_Type);
            
            foreach (var collider in colliders)
            {
                // only pull objects that are listed in the set of affected objects
                if (m_AffectedObjectIDs != null && !m_AffectedObjectIDs.Contains(collider.GetInstanceID())) continue;

                updater(collider, pivotPosition);
            }
        }

        public struct Settings
        {
            public float forceMagnitude;
            public float stopRadius;

            public Settings(float stopRadius = 0.15f, float forceMagnitude = 10f)
            {
                this.stopRadius = stopRadius;
                this.forceMagnitude = forceMagnitude;
            }
        }

        /// <summary>
        /// Given the set parameters, performs a physics update
        /// on the given collider, pulling it to/pushing it out from a given
        /// pivot position..
        /// </summary>
        /// <param name="forceType">Whether to attract, repel, or none.</param>
        /// <param name="stopRadius">Radius at which the force should no longer take effect.</param>
        /// <param name="forceMagnitude">Amount of force to use.</param>
        /// <returns>A physics updater function, that returns true when the given collider had a force applied to itself (ForceType.None always returns false.).</returns>
        public static Func<Component, Vector3, bool> PhysicsUpdate(
            float forceMagnitude,
            float stopRadius,
            ForceType forceType = ForceType.Attraction
        ) {
            return (Component collider, Vector3 pivotPosition) =>
            {
                // no force is applied to disabled or destroyed objects
                if (collider.IsNull() || !collider.gameObject.activeInHierarchy) return false;
                
                float signal = (float) forceType;

                Rigidbody body = collider.GetComponent<Rigidbody>();
                if (body == null)
                    return false;

                Vector3 direction = pivotPosition - body.position;

                float distance = direction.magnitude;

                direction = direction.normalized;

                if (distance < stopRadius)
                    return false;

                float forceRate = (forceMagnitude / distance);

                body.AddForce(direction * (forceRate / body.mass) * signal);

                return forceType != ForceType.None;
            };
        }
        
        // Overload to pass settings directly
        public static Func<Collider, Vector3, bool> PhysicsUpdate(
            Settings settings,
            ForceType forceType = ForceType.Attraction
        ){ return PhysicsUpdate(settings.forceMagnitude, settings.stopRadius, forceType); }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.62f, 1f, 0.4f);
            Gizmos.DrawWireSphere(m_Pivot.position, m_Radius);
            
            Gizmos.color = new Color(1f, 0.33f, 0.24f);
            Gizmos.DrawWireSphere(m_Pivot.position, m_StopRadius);
        }
    }
}