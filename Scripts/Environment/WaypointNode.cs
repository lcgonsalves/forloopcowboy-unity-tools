using forloopcowboy_unity_tools.Scripts.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Environment
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
        
        /// <param name="newNext"></param>
        /// <returns>The former next waypoint if one existed.</returns>
        [CanBeNull]
        public WaypointNode SetNext(WaypointNode newNext)
        {
            var former = next;
            next = newNext;
            return former;
        }

        public WaypointNode GetEnd()
        {
            var temp = next;
            while (temp.HasNext)
            {
                temp = temp.next;
            }

            return temp;
        }
        
        
        public bool HasNext => TryGetNext(out _);

        private void OnEnable()
        {
            if (configuration)
                gameObject.layer = configuration.Layer;
        }

        private void OnDrawGizmos()
        {
            // indicator is green if has next, but red if doesn't.
            Gizmos.color = TryGetNext(out _) ? new Color(0.13f, 0.42f, 0.2f) : Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(Vector3.up * 0.25f), 0.15f);

            Gizmos.color = new Color(0.79f, 1f, 0.03f);
            Gizmos.DrawSphere(transform.position, 0.25f);

            if (TryGetNext(out var next))
            {
                Gizmos.DrawLine(transform.position, next.transform.position);
            }
        }
    }
}