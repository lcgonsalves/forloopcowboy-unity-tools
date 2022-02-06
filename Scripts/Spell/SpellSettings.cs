using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spell
{
    public abstract class SpellSettings : SerializedScriptableObject
    {
        /// <summary>Returns a new instance of the spell, initialized with the settings.</summary>
        public abstract INetworkSpell GetNewSpellInstance();

        /// <summary>Returns all prefabs that the spell wants to pool.</summary>
        public abstract IEnumerable<GameObject> GetPrefabsToBePooled();

        // Validators //

        protected bool IsNetworkedObject(GameObject prefab) => prefab && prefab.HasComponent<NetworkObject>();
        protected bool IsNetworkedTransform(GameObject prefab) => prefab && prefab.HasComponent<NetworkTransform>();
        protected bool IsNetworkedRigidbody(GameObject prefab) => prefab && prefab.HasComponent<NetworkRigidbody>();
        
    }

}