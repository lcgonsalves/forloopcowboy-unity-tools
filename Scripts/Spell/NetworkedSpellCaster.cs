using System;
using System.Collections.Generic;
using System.Globalization;
using forloopcowboy_unity_tools.Scripts.Core.Networking;
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

        private Movement.KinematicCharacterController characterController;
        private NetworkVariable<Vector3> synchedCharacterVelocity;

        private NetworkStats networkStats;

        [Serializable]
        public struct InputSettings
        {
            public InputActionReference cast;
        }

        [ServerRpc]
        private void SynchronizeCharacterControllerVelocityServerRpc(Vector3 velocity)
        {
            synchedCharacterVelocity.Value = velocity;
        }

        private void Awake()
        {
            characterController = GetComponent<Movement.KinematicCharacterController>();
            networkStats = GetComponent<NetworkStats>();
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

        private void Update()
        {
            if (IsOwner && IsClient)
                SynchronizeCharacterControllerVelocityServerRpc(characterController.Motor.GetState().BaseVelocity);
        }

        private void HandleCastReleased(InputAction.CallbackContext _)
        {
            if (castPosition.TryGetComponent(out PreviewComponent previewComponent))
                previewComponent.Hide();

            CastSpellServerRpc(
                GetLagCompCastPosition(activeSpell, IsHost)    
            );
        }

        private void HandleCastPressed(InputAction.CallbackContext _)
        {
            if (activeSpell != null && castPosition.TryGetComponent(out PreviewComponent previewComponent))
            {
                var castSettings = new CastSettings();
                
                castSettings.direction = GetCastDirection(activeSpell);
                castSettings.position = GetLagCompCastPosition(activeSpell, true); // we assume host here because the preview is always local

                if (activeSpell != null) // rider wants a null check here.
                    previewComponent.SetAndShow(activeSpell.GetPreview(this, castSettings));
            }
        }

        /// <summary>
        /// Casts a spell and spawns it.
        /// </summary>
        [ServerRpc]
        private void CastSpellServerRpc(Vector3 lagCorrectedPosition)
        {
            if (activeSpell == null) return;
            
            var castSettings = new CastSettings();
            castSettings.direction = GetCastDirection(activeSpell);
            castSettings.position = lagCorrectedPosition;

            if (activeSpell != null && activeSpell.TryCast(this, castSettings, out var obj) && !obj.IsSpawned)
                obj.Spawn(destroyWithScene: true);
        }

        [ServerRpc]
        private void InitializeSpellServerRpc() => InitializeSpellLocal();

        void InitializeSpellLocal()
        {
            foreach (var spellSetting in spellSettings)
            {
                // register poolable objects
                spellSetting.RegisterPrefabsInPool();
                
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

        public Vector3 GetLagCompCastPosition(INetworkSpell spell, bool isHost)
        {
            switch (spell)
            {
                default:
                    float compensationTime = 0;
                    if (!isHost) compensationTime = networkStats.LastRTT * 1.02f;
                    return castPosition.position + synchedCharacterVelocity.Value * compensationTime;
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

        public override void OnDestroy()
        {
            base.OnDestroy();
            inputSettings.cast.action.started -= HandleCastPressed;
            inputSettings.cast.action.canceled -= HandleCastReleased;
        }
    }
}