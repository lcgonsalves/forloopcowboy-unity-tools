using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace forloopcowboy_unity_tools.Scripts.Environment
{
    [CreateAssetMenu(fileName = "New Waypoint Configuration", menuName = "Settings/New Waypoint Settings...", order = 0)]
    public class WaypointSettings : ScriptableObject, IHasLayer
    {
        private static readonly string lm = "Waypoint";
        
        [FormerlySerializedAs("layerMask")] [SerializeField] private string layerName = lm;
        
        private LayerHelper lh = new LayerHelper();

        public string LayerName => layerName;
        public int Layer => lh.Layer(LayerName);
        public LayerMask LayerMask => lh.LayerMaskFor(Layer);
    }
}