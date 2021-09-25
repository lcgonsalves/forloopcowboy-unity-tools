using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    /// <summary>
    /// Encapsulates data for placing NPCs.
    /// </summary>
    [CreateAssetMenu]
    public class PlaceableUnit : ScriptableObject
    {
        // todo: add some validation to the UI to remind configurator that all needed components are added
        public GameObject prefab;
    }
}
