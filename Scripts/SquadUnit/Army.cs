using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.SquadUnit
{
    [CreateAssetMenu]
    public class Army : ScriptableObject, IHasLayer
    {
        public string key = "Untitled Army";

        [SerializeField] private string layerName = "Default";
        
        public string LayerName => layerName;

        public int Layer => LayerHelper.Layer(LayerName);
        public LayerMask LayerMask => LayerHelper.LayerMaskFor(Layer);
    }
}
