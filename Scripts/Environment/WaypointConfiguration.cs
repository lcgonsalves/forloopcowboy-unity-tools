using ForLoopCowboyCommons.EditorHelpers;
using UnityEngine;

namespace ForLoopCowboyCommons.Environment
{
    [CreateAssetMenu(fileName = "New Waypoint Configuration", menuName = "Waypoint Config", order = 0)]
    public class WaypointConfiguration : ScriptableObject, IHasLayer
    {
        private static readonly string lm = "Waypoint";
        
        [ReadOnly, SerializeField] private string layerMask = lm;
        
        private LayerHelper lh = new LayerHelper(lm);
        
        public int Layer => lh.Layer;
        public LayerMask LayerMask => lh.LayerMask;
    }
}