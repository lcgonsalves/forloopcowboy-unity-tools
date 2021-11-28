using forloopcowboy_unity_tools.Scripts.Core;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace forloopcowboy_unity_tools.Scripts.Environment
{
    public class WaypointNode : MonoBehaviour
    {
        [SerializeField] private WaypointNode next;
        [FormerlySerializedAs("configuration")] [SerializeField] private WaypointSettings settings;

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

        [CanBeNull]
        public WaypointNode GetEnd()
        {
            var temp = next;

            while (temp is {HasNext: true})
            {
                if (temp.GetInstanceID() == this.GetInstanceID())
                {
                    temp = temp.next; // idk
                    break;
                }
                else temp = temp.next;
            }

            return temp;
        }
        
        
        public bool HasNext => TryGetNext(out _);

        private void OnEnable()
        {
            if (settings)
                gameObject.layer = settings.Layer;
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