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
    [ShowOdinSerializedPropertiesInInspector]
    public class NetworkHealthComponent : 
        NetworkBehaviour, 
        IHealth,     
        ISerializationCallbackReceiver,
        ISupportsPrefabSerialization
    {
        public static readonly int DEFAULT_STARTING_HEALTH = 100;
        public static readonly int DEFAULT_MAX_HEALTH = 100;

        public NetworkVariable<int> NetworkCurrent = new NetworkVariable<int>(DEFAULT_STARTING_HEALTH);
        
        [ShowInInspector]
        public int Current
        {
            get => NetworkCurrent.Value;
            private set
            {
                if (IsSpawned) SetCurrentValueServerRpc(value);
            }
        }

        [ShowInInspector]
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

        [Button(ButtonSizes.Large)]
        public void Damage(int amount, IDamageProvider damageSource) => Damage(amount);
        public void Damage(int amount)
        {
            if (Current <= 0) return;
            Current -= amount;
        }

        [Button(ButtonSizes.Large)]
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
        
        // serialization
        [SerializeField]
        [HideInInspector]
        private SerializationData serializationData;

        SerializationData ISupportsPrefabSerialization.SerializationData
        {
            get => this.serializationData;
            set => this.serializationData = value;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject((UnityEngine.Object) this, ref this.serializationData);
            this.OnAfterDeserialize();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.OnBeforeSerialize();
            UnitySerializationUtility.SerializeUnityObject((UnityEngine.Object) this, ref this.serializationData);
        }

        /// <summary>Invoked after deserialization has taken place.</summary>
        /// <footer><a href="https://www.google.com/search?q=Sirenix.OdinInspector.SerializedMonoBehaviour.OnAfterDeserialize">`SerializedMonoBehaviour.OnAfterDeserialize` on google.com</a></footer>
        protected virtual void OnAfterDeserialize()
        {
        }

        /// <summary>Invoked before serialization has taken place.</summary>
        /// <footer><a href="https://www.google.com/search?q=Sirenix.OdinInspector.SerializedMonoBehaviour.OnBeforeSerialize">`SerializedMonoBehaviour.OnBeforeSerialize` on google.com</a></footer>
        protected virtual void OnBeforeSerialize()
        {
        }

        [HideInTables]
        [OnInspectorGUI]
        [PropertyOrder(-2.147484E+09f)]
        private void InternalOnInspectorGUI() => EditorOnlyModeConfigUtility.InternalOnInspectorGUI((UnityEngine.Object) this);
        
    }


}