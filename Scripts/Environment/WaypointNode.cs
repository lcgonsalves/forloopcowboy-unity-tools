using System;
using UnityEngine;

namespace ForLoopCowboyCommons.Environment
{
    public class WaypointNode : MonoBehaviour
    {
        [SerializeField] private WaypointNode next;
        [SerializeField] private WaypointConfiguration configuration;

        public bool TryGetNext(out WaypointNode nextWaypoint)
        {
            if (next == null)
            {
                nextWaypoint = null;
                return false;
            }
            else
            {
                nextWaypoint = next;
                return true;
            }
        }

        public bool HasNext => TryGetNext(out _);

        private void OnEnable()
        {
            gameObject.layer = configuration.Layer;
        }

        private void OnDrawGizmos()
        {
            // indicator is green if has next, but red if doesn't.
            Gizmos.color = TryGetNext(out _) ? new Color(0.13f, 0.42f, 0.2f) : Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(Vector3.up * 0.25f), 0.15f);

            Gizmos.color = new Color(0.79f, 1f, 0.03f);
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
    }
}