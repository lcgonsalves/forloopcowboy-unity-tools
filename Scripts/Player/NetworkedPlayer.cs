using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace forloopcowboy_unity_tools.Scripts.Player
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedPlayer : NetworkBehaviour
    {
        public UnitManager.Side side;
        public new PlayerCameraController cameraController;
        [FormerlySerializedAs("player")]
        public Movement.KinematicCharacterController characterController;
        public Transform cameraFollowPoint;

        [ShowInInspector]
        public HealthComponent healthComponent => _healthComponent == null ? _healthComponent = GetComponent<HealthComponent>() : _healthComponent;
        private HealthComponent _healthComponent;

        [System.Serializable]
        public class InputSettings
        {
            public InputActionReference look;
            public InputActionReference move;
            public InputActionReference jump;
            public InputActionReference sprint;
            public InputActionReference reset;
            public InputActionReference escape;

            public void EnableAll()
            {
                if (look != null) look.action.Enable();
                if (move != null) move.action.Enable();
                if (jump != null) jump.action.Enable();
                if (sprint != null) sprint.action.Enable();
                if (reset != null) reset.action.Enable();
                if (escape != null) escape.action.Enable();
            }
            
            public void DisableAll()
            {
                if (look != null) look.action.Disable();
                if (move != null) move.action.Disable();
                if (jump != null) jump.action.Disable();
                if (sprint != null) sprint.action.Disable();
                if (reset != null) reset.action.Disable();
                if (escape != null) escape.action.Disable();
            }
            
        }

        public InputSettings inputSettings;

        private void OnDisable()
        {
            if (IsOwner && IsClient)
            {
                inputSettings.DisableAll();
                inputSettings.escape.action.performed -= ToggleCursorLockState;
            }
        }
        
        private void Start()
        {
            if (IsOwner && IsClient)
            {
                inputSettings.EnableAll();
                inputSettings.escape.action.performed += ToggleCursorLockState;
                
                Cursor.lockState = CursorLockMode.Locked;

                // Tell camera to follow transform
                cameraController.SetFollowTransform(cameraFollowPoint);

                // Ignore the character's collider(s) for camera obstruction checks
                cameraController.IgnoredColliders.Clear();
                cameraController.IgnoredColliders.AddRange(characterController.GetComponentsInChildren<Collider>());
            }
            else
            {
                characterController.Motor.enabled = false;
            }
        }
        
        private void LateUpdate()
        {
            if (IsOwner && IsClient)
            {
                HandleCameraInput();
                characterController.PostInputUpdate(Time.deltaTime, cameraController.transform.forward);
            }
        }
        
        private void Update()
        {
            if (IsOwner && IsClient)
                HandleInputs();
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

            characterController.SetInputs(ref inputs);
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
            
        }
        
        private static void ToggleCursorLockState(InputAction.CallbackContext _)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else if (Cursor.lockState == CursorLockMode.None)
                Cursor.lockState = CursorLockMode.Locked;
        }

    }
}