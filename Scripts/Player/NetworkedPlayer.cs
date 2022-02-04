using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Movement;
using KinematicCharacterController.Examples;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace forloopcowboy_unity_tools.Scripts.Player
{
    public class NetworkedPlayer : MonoBehaviour
    {
        public UnitManager.Side side;
        public new ExampleCharacterCamera camera;
        public NetworkedCharacterController player;
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

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            camera.SetFollowTransform(cameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            camera.IgnoredColliders.Clear();
            camera.IgnoredColliders.AddRange(player.GetComponentsInChildren<Collider>());

            inputSettings.EnableAll();
        }
        
        private void LateUpdate()
        {
            HandleCameraInput();
            player.PostInputUpdate(Time.deltaTime, camera.transform.forward);
        }
        
        private void Update()
        {
            HandleInputs();

            if (inputSettings.escape.WasPressedThisFrame())
            {
                
            }
        }


        public void HandleInputs()
        {
            var inputs = new NetworkedCharacterController.PlayerCharacterInputs();
            var moveInput = inputSettings.move.ValueThisFrame<Vector2>();
            var pressedJump = inputSettings.jump.WasPressedThisFrame();

            inputs.cameraRotation = camera.Transform.rotation;
            inputs.moveAxisForward = moveInput.y;
            inputs.moveAxisRight = moveInput.x;
            inputs.requestJump = pressedJump;

            player.SetInputs(ref inputs);
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
            camera.UpdateWithInput(Time.deltaTime, 0, lookInputVector);
            
        }
        
    }
}