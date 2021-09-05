using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.SquadUnit
{
    [CreateAssetMenu]
    public class Army : ScriptableObject, IHasLayer
    {
        public string key = "Untitled Army";

        [SerializeField] private LayerHelper layerConfiguration = new LayerHelper("Everything");
    
        public int Layer => layerConfiguration.Layer;
        public LayerMask LayerMask => layerConfiguration.LayerMask;
    }
}
