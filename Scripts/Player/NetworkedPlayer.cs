using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.GUI;
using forloopcowboy_unity_tools.Scripts.Spell;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace forloopcowboy_unity_tools.Scripts.Player
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedPlayer : NetworkBehaviour
    {
        public enum SimulationMode
        {
            Kinematic,
            Physics
        }

        public NetworkVariable<SimulationMode> simulationMode;

        public UnitManager.Side side;
        public PlayerCameraController cameraController;
        public float autoDespawnPreviousCharacterDelay = 3f;

        // synchronized reference
        public NetworkVariable<NetworkObjectReference> syncedCharacterReference;

        [CanBeNull]
        public NetworkObject CharacterReference
        {
            get
            {
                if (syncedCharacterReference.Value.TryGet(out var reference))
                {
                    if (
                        CharacterController == null ||
                        CharacterRigidbody == null ||
                        HealthComponent == null ||
                        SpellCaster == null ||
                        CharacterNetworkTransform == null
                    )
                    {
                        CharacterController = reference.GetComponent<Movement.KinematicCharacterController>();
                        CharacterRigidbody = reference.GetComponent<Rigidbody>();
                        HealthComponent = reference.GetComponent<NetworkHealthComponent>();
                        SpellCaster = reference.GetComponent<NetworkedSpellCaster>();
                        CharacterNetworkTransform = reference.GetComponent<ClientNetworkTransform>();
                    }
                    
                    return reference;
                }
                
                return null;
            }
            private set
            {
                if (value != null)
                {
                    CharacterController = value.GetComponent<Movement.KinematicCharacterController>();
                    CharacterRigidbody = value.GetComponent<Rigidbody>();
                    HealthComponent = value.GetComponent<NetworkHealthComponent>();
                    SpellCaster = value.GetComponent<NetworkedSpellCaster>();
                    CharacterNetworkTransform = value.GetComponent<ClientNetworkTransform>();
                }
                else
                {
                    CharacterController = null;
                    CharacterRigidbody = null;
                    HealthComponent = null;
                    SpellCaster = null;
                    CharacterNetworkTransform = null;
                }

                if (IsServer)
                    syncedCharacterReference.Value = value;
            }
        }

        public bool HasCharacter => CharacterReference != null;
        
        public Movement.KinematicCharacterController CharacterController { get; private set; }
        public Rigidbody CharacterRigidbody { get; private set; }
        public NetworkHealthComponent HealthComponent { get; private set; }
        public NetworkedSpellCaster SpellCaster { get; private set; }
        
        public ClientNetworkTransform CharacterNetworkTransform { get; private set; }
        
        public Transform cameraFollowPoint;
        public float throwAngle;

        // public NetworkVariable<Quaternion> synchedCameraFollowPointRotation;

        /// <summary> Can be used for emitting spells, bullets, etc. Its direction is synched with the camera direction.</summary>
        [FormerlySerializedAs("emitterTransform")]
        [Tooltip("Its direction is synced with the camera direction.")]
        [CanBeNull] public Transform castPoint;

        [System.Serializable]
        public class InputSettings
        {
            public InputActionReference look;
            public InputActionReference move;
            public InputActionReference jump;
            public InputActionReference sprint;
            public InputActionReference reset;
            public InputActionReference escape;
            public InputActionReference respawn;
            public InputActionReference castSpell;

            public void EnableAll()
            {
                if (look != null) look.action.Enable();
                if (move != null) move.action.Enable();
                if (jump != null) jump.action.Enable();
                if (sprint != null) sprint.action.Enable();
                if (reset != null) reset.action.Enable();
                if (escape != null) escape.action.Enable();
                if (respawn != null) respawn.action.Enable();
                if (castSpell != null) castSpell.action.Enable();
            }
            
            public void DisableAll()
            {
                if (look != null) look.action.Disable();
                if (move != null) move.action.Disable();
                if (jump != null) jump.action.Disable();
                if (sprint != null) sprint.action.Disable();
                if (reset != null) reset.action.Disable();
                if (escape != null) escape.action.Disable();
                if (respawn != null) respawn.action.Disable();
                if (castSpell != null) castSpell.action.Disable();
            }
            
        }

        public InputSettings inputSettings;

        /// <summary>
        /// Assigns a new puppet to this player controller, if owner.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AssignNewCharacterServerRpc(NetworkObjectReference characterReference) =>
            AssignCharacterInternal(characterReference);

        /// <summary>
        /// Assigns a new puppet to this player controller, if owner.
        /// </summary>
        [ClientRpc]
        public void AssignNewCharacterClientRpc(NetworkObjectReference characterReference) =>
            AssignCharacterInternal(characterReference);

        private void AssignCharacterInternal(NetworkObjectReference characterReference)
        {
            if (characterReference.TryGet(out var reference) && reference.OwnerClientId == OwnerClientId)
            {
                var previousCharacterReference = CharacterReference;
                CharacterReference = reference;

                castPoint = reference.transform.FindWithName("CastPoint");
                cameraFollowPoint = reference.transform.FindWithName("CameraFollowPoint");

                var characterController = CharacterController;
                
                if (IsOwner)
                {
                    if (previousCharacterReference != null)
                    {
                        // clear previous character's spell casting event
                        var prevSpellCaster = previousCharacterReference.GetComponent<NetworkedSpellCaster>();

                        inputSettings.castSpell.action.started -= prevSpellCaster.HandleCastPressed;
                        inputSettings.castSpell.action.canceled -= prevSpellCaster.HandleCastReleased;
                    }
                    
                    // assign cast actions to new spell caster
                    inputSettings.castSpell.action.started += SpellCaster.HandleCastPressed;
                    inputSettings.castSpell.action.canceled += SpellCaster.HandleCastReleased;

                    cameraController.SetFollowTransform(cameraFollowPoint);

                    // Ignore the character's collider(s) for camera obstruction checks
                    cameraController.IgnoredColliders.Clear();
                    cameraController.IgnoredColliders.AddRange(characterController.GetComponentsInChildren<Collider>());

                    characterController.Motor.CharacterController = characterController;
                }

                // track health of this new character
                // todo: have a special health bar for the owner!
                NetworkHealthTracker.AssociateReactiveUpdateAndTrack(HealthComponent, reference.transform);

                foreach (var otherHealthComponent in FindObjectsOfType<NetworkHealthComponent>())
                {
                    if (otherHealthComponent == HealthComponent) continue;
                    NetworkHealthTracker.AssociateReactiveUpdateAndTrack(otherHealthComponent, otherHealthComponent.transform);
                }

                characterController.Motor.enabled = IsOwner;
            }
            else
                NetworkLog.LogErrorServer(
                    "Attempting to set character reference that is either null, or not owned by the player master. Make sure ownership is set properly.");
        }

        private void OnDisable()
        {
            if (IsOwner && IsClient)
            {
                inputSettings.DisableAll();
                inputSettings.escape.action.performed -= ToggleCursorLockState;
                inputSettings.respawn.action.performed -= HandleRespawnButtonPress;
            }
        }

        private void Start()
        {
            HandleSimulationModeChange();

            if (!HasCharacter)
            {
                // server rpc that will update this character reference asynchronously
                NetworkGameManager.GetOrCreateCharacterForPlayer(this);
            }
            
            if (IsOwner && IsClient)
            {
                inputSettings.EnableAll();
                inputSettings.escape.action.performed += ToggleCursorLockState;
                inputSettings.respawn.action.performed += HandleRespawnButtonPress;
                
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (HasCharacter)
            {
                CharacterController.Motor.enabled = false;
            }
        }

        public void HandleRespawnButtonPress(InputAction.CallbackContext ctx)
        {
            if (!HasCharacter || HealthComponent.IsDead)
            {
                DestroyPreviousCharacterServerRpc();
                
                // server rpc that will update this character reference asynchronously
                NetworkGameManager.CreateNewCharacterForPlayer(this);
            }
        }

        [ServerRpc]
        private void DestroyPreviousCharacterServerRpc()
        {
            if (HasCharacter)
            {
                Destroy(CharacterReference!.gameObject, autoDespawnPreviousCharacterDelay);
            }
        }

        private void HandleSimulationModeChange()
        {
            if (!HasCharacter) return;

            var currentSimulationMode = simulationMode.Value;
            
            if (HealthComponent.IsDead && IsOwner && currentSimulationMode != SimulationMode.Physics)
            {
                SetSimulationModeServerRpc(SimulationMode.Physics);
                currentSimulationMode = SimulationMode.Physics; // perform change locally to handle change immediately
            }
            else if (HealthComponent.IsAlive && IsOwner && currentSimulationMode != SimulationMode.Kinematic)
            {
                SetSimulationModeServerRpc(SimulationMode.Kinematic);
                currentSimulationMode = SimulationMode.Kinematic; // perform change locally to handle change immediately
            }
            
            switch (currentSimulationMode)
            {
                case SimulationMode.Kinematic:
                    CharacterController.Motor.enabled = true;
                    // do not touch kinematic rigidbody if this is not the owner
                    if (IsServer && !CharacterRigidbody.isKinematic) CharacterRigidbody.isKinematic = true;
                    // switch to client-authoritative model if owner
                    CharacterNetworkTransform.CanCommitToTransform = IsOwner;

                    break;
                
                case SimulationMode.Physics:
                    CharacterController.Motor.enabled = false;
                    // do not touch kinematic rigidbody if this is not the owner
                    if (IsServer && CharacterRigidbody.isKinematic) CharacterRigidbody.isKinematic = false;
                    // switch to server-authoritative model to simulate physics
                    CharacterNetworkTransform.CanCommitToTransform = IsServer;
                    
                    break;
            }
        }

        [ServerRpc]
        private void SetSimulationModeServerRpc(SimulationMode newSimulationMove) => 
            simulationMode.Value = newSimulationMove;

        private void LateUpdate()
        {
            if (IsOwner && IsClient)
            {
                HandleCameraInput();
                
                if (HasCharacter && simulationMode.Value == SimulationMode.Kinematic)
                    CharacterController.PostInputUpdate(Time.deltaTime, cameraController.transform.forward);
            }
        }
        
        private void Update()
        {
            HandleSimulationModeChange();

            if (!HasCharacter) return;

            if (IsOwner && IsClient)
            {
                
                if (simulationMode.Value == SimulationMode.Kinematic)
                    HandleInputs();
            }
            else
            {
                CharacterController.Motor.enabled = false;
            }
        }
        

        public void HandleInputs()
        {
            var inputs = new Movement.KinematicCharacterController.PlayerCharacterInputs();
            var moveInput = inputSettings.move.ValueThisFrame<Vector2>();
            var pressedJump = inputSettings.jump.WasPressedThisFrame();

            inputs.cameraRotation = cameraController.Transform.rotation;
            inputs.moveAxisForward = moveInput.y;
            inputs.moveAxisRight = moveInput.x;
            inputs.requestJump = pressedJump;

            if (HasCharacter)
                CharacterController.SetInputs(ref inputs);
        }

        public void HandleCameraInput()
        {
            var lookInput = inputSettings.look.ValueThisFrame<Vector2>();

            // Create the look input vector for the camera
            float mouseLookAxisUp = lookInput.y;
            float mouseLookAxisRight = lookInput.x;
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // Apply inputs to the camera
            cameraController.UpdateWithInput(Time.deltaTime, 0, lookInputVector);

            // Make sure emitter is positioned in line with camera's aim point
            if (castPoint != null)
            {
                // todo: replace projected point with raycast collision for more accurate?
                var camTransform = cameraController.transform;
                var projectedPoint = camTransform.position + (camTransform.forward * 55f);
                var correctedDirection = Quaternion.AngleAxis(throwAngle, transform.TransformDirection(Vector3.right)) *
                                         (projectedPoint - castPoint.position).normalized;

                castPoint.rotation = Quaternion.LookRotation(correctedDirection);
            }
        }


        private static void ToggleCursorLockState(InputAction.CallbackContext _)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else if (Cursor.lockState == CursorLockMode.None)
                Cursor.lockState = CursorLockMode.Locked;
        }

        public void AssignNewCharacter(NetworkObject character)
        {
            AssignNewCharacterServerRpc(character);
            AssignNewCharacterClientRpc(character);
        }
    }
}