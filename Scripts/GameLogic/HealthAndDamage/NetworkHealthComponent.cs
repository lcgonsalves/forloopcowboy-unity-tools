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
    public class NetworkHealthComponent : NetworkBehaviour, IHealth
    {
        public static readonly int DEFAULT_STARTING_HEALTH = 100;
        public static readonly int DEFAULT_MAX_HEALTH = 100;

        public NetworkVariable<int> NetworkCurrent = new NetworkVariable<int>(DEFAULT_STARTING_HEALTH);
        
        [OdinSerialize, ShowInInspector]
        public int Current
        {
            get => NetworkCurrent.Value;
            private set
            {
                if (IsSpawned) SetCurrentValueServerRpc(value);
            }
        }

        [OdinSerialize, ShowInInspector]
        public int Max { get; private set; }

        public HealthSettings settings;

        /// <summary>
        /// Applies settings to current state.
        /// </summary>
        public void Initialize()
        {
            Current = settings ? settings.Current : DEFAULT_STARTING_HEALTH;
            Max     = settings ? settings.Max     : DEFAULT_MAX_HEALTH;
        }

        public void Damage(int amount)
        {
            if (Current <= 0) return;
            Current -= amount;
        }

        public void Heal(int amount, bool revive = true)
        {
            if (Current <= 0 && !revive) return;
            Current += amount;
        }

        private void OnServerInitialized()
        {
            Initialize();
        }

        public bool IsAlive => Current > 0;
        public bool IsDead => !IsAlive;

        [ServerRpc(RequireOwnership = false)]
        public void SetCurrentValueServerRpc(int newHealthValue)
        {
            NetworkCurrent.Value = Mathf.Clamp(newHealthValue, 0, Max);
        }

        private void Awake() => Initialize();
        
    }


}