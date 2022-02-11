using System;
using BehaviorDesigner.Runtime;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// <summary>
    /// Stores health and propagates it through
    /// server side events, which trigger client side events.
    /// Be sure to check which side you're on before subscribing to
    /// events, because one side's event will never trigger on the other.
    /// </summary>
    public class NetworkHealthComponent : NetworkBehaviour
    {
        public enum HealthChangeType
        {
            Heal,
            Damage
        }

        public struct HealthChange
        {
            public int delta;
            public HealthChangeType type;
        }

        [OdinSerialize, ShowInInspector]
        public int Current { get; private set; }
        
        [OdinSerialize, ShowInInspector]
        public int Max { get; private set; }
        
        [OdinSerialize, ShowInInspector]
        public int Min { get; private set; }

    }


}