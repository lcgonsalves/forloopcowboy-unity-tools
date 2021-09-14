using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.SquadUnit
{
    [CreateAssetMenu]
    public class Army : ScriptableObject, IHasLayer
    {
        public string key = "Untitled Army";

        [SerializeField] private string layerName = "Default";

        private LayerHelper layerConfiguration = new LayerHelper();

        public string LayerName => layerName;

        public int Layer => layerConfiguration.Layer(LayerName);
        public LayerMask LayerMask => layerConfiguration.LayerMaskFor(Layer);
    }
}
