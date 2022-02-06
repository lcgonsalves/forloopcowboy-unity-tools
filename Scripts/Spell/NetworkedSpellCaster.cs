using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core.Networking.forloopcowboy_unity_tools.Scripts.Core.Networking;
using forloopcowboy_unity_tools.Scripts.Spell.Implementations;
using JetBrains.Annotations;
using Sirenix.Serialization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace forloopcowboy_unity_tools.Scripts.Spell
{
    public class NetworkedSpellCaster : NetworkBehaviour, ISpellCaster
    {
        // Settings
        
        [Tooltip("Where the spell is cast from.")] public Transform castPosition;
        [Tooltip("The spells the player starts with.")] public List<SpellSettings> spellSettings;
        public InputSettings inputSettings;
        
        // Internal state

        [CanBeNull] private INetworkSpell activeSpell = null;
        private List<INetworkSpell> spells = new List<INetworkSpell>();

        [Serializable]
        public struct InputSettings
        {
            public InputActionReference cast;
        }

        public void Start()
        {
            if (IsOwner && IsClient)
            {
                inputSettings.cast.action.Enable();
                inputSettings.cast.action.started += HandleCastPressed;
                inputSettings.cast.action.canceled += HandleCastReleased;

                // Keep spells in sync at the start
                InitializeSpellLocal();
                InitializeSpellServerRpc();
            }
        }

        private void HandleCastReleased(InputAction.CallbackContext _)
        {
            if (castPosition.TryGetComponent(out PreviewComponent previewComponent))
                previewComponent.Hide();
            
            CastSpellServerRpc();
        }

        private void HandleCastPressed(InputAction.CallbackContext _)
        {
            if (activeSpell != null && castPosition.TryGetComponent(out PreviewComponent previewComponent))
                previewComponent.SetAndShow(activeSpell.GetPreview(this));
        }

        /// <summary>
        /// Casts a spell and spawns it.
        /// </summary>
        [ServerRpc]
        private void CastSpellServerRpc()
        {
            if (activeSpell != null && activeSpell.TryCast(this, out var obj) && !obj.IsSpawned)
                obj.Spawn(destroyWithScene: true);
            else NetworkLog.LogInfoServer("Could not cast.");
        }

        [ServerRpc]
        private void InitializeSpellServerRpc() => InitializeSpellLocal();

        void InitializeSpellLocal()
        {
            foreach (var spellSetting in spellSettings)
            {
                // register poolable objects
                foreach (var prefab in spellSetting.GetPrefabsToBePooled())
                    NetworkObjectPool.Singleton.RegisterPrefab(prefab);
                    
                spells.Add(spellSetting.GetNewSpellInstance());
            }
            
            if (spellSettings.Count > 0) activeSpell = spells[0];
        }

        public void OnDisable()
        {
            if (IsOwner && IsClient)
            {
                inputSettings.cast.action.Disable();
            }
        }

        public Vector3 GetCastPosition(INetworkSpell spell)
        {
            switch (spell)
            {
                default:
                    return castPosition.position;
            }
        }

        public Vector3 GetCastDirection(INetworkSpell spell)
        {
            switch (spell)
            {
                default:
                    return castPosition.forward;
            }
        }

        public bool TryGetCastTarget(INetworkSpell spell, out Transform castTarget)
        {
            castTarget = null;
            return false;
        }
    }
}