using System;
using System.Collections.Generic;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spell
{
    public class BallisticTrajectoryPreview : IPreview
    {
        // Change this if you want to be able to track velocity changes.
        public Func<Vector3> getStartingVelocity;
        
        public Vector3 startingVelocity;
        
        private int resolution;
        private float distanceTimeBetweenPoints;
        private LayerMask collidableLayers;
        
        private List<Vector3> points;

        public BallisticTrajectoryPreview(
            Vector3 startingVelocity,
            LayerMask collidableLayers,
            int resolution = 50,
            float distanceTimeBetweenPoints = 0.1f,
            Func<Vector3> startingVelocityGetter = null
        ) {
            this.resolution = resolution;
            this.distanceTimeBetweenPoints = distanceTimeBetweenPoints;
            this.startingVelocity = startingVelocity;
            this.collidableLayers = collidableLayers;
            this.points = new List<Vector3>(resolution);

            if (startingVelocityGetter == null)
                this.getStartingVelocity = () => this.startingVelocity;
            else this.getStartingVelocity = startingVelocityGetter;
        }

        public void Update(ref PreviewContext context)
        {
            if (!context.LineRenderer.enabled) context.LineRenderer.enabled = true;
            
            context.LineRenderer.positionCount = resolution;
            Vector3 startingPosition = context.PreviewComponent.transform.position;
            points.Clear();

            var velocity = getStartingVelocity();
            
            for (float t = 0; t < resolution; t += distanceTimeBetweenPoints)
            {
                Vector3 newPoint = startingPosition + t * velocity;
                newPoint.y = startingPosition.y + velocity.y * t + Physics.gravity.y/2f * t * t;
                points.Add(newPoint);

                if(Physics.CheckSphere(newPoint, .1f, collidableLayers))
                {
                    context.LineRenderer.positionCount = points.Count;
                    break;
                }
            }

            context.LineRenderer.SetPositions(points.ToArray());
        }

        // nothing to dispose
        public void Dispose(ref PreviewContext context) => Hide(ref context);

        public void Hide(ref PreviewContext context)
        {
            // just hide the line renderer.
            context.LineRenderer.enabled = false;
        }
    }
}