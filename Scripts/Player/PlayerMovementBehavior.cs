using System;
using System.Collections;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace forloopcowboy_unity_tools.Scripts.Player
{
    [RequireComponent(typeof(CharacterController), typeof(Chronos.Chronos))]
    [SelectionBase]
    public class PlayerMovementBehavior : MonoBehaviour
    {
        // debug information
        [SerializeField]
        protected DebugInformation debugInfo = new DebugInformation();

        // Publicly exposed settings

        [SerializeField, Tooltip("Movement speed going forward and backwards.")]
        private float movementSpeed = 5f;

        // getter modifies the value according to the sprint
        public float MovementSpeed { get => movementSpeed; }

        [Tooltip("Movement speed going sideways.")]
        public float StrafeMovementSpeed = 5f;

        [Tooltip("Height the character can jump.")]
        public float JumpHeight = 1f;

        [SerializeField, Tooltip("Multiplies the movement speed when sprinting"), Range(1f, 4.5f)]
        private float sprintMultiplier = 1.45f;

        [Tooltip("Amount of seconds it takes to go idle.")]
        public float GoIdleInSeconds = 2f;

        // sprint multiplier, accounting for activation rules
        // returns real multiplier only if player is pressing the sprint button,
        // the player character is grounded, and the player is moving forward.
        public float StatefulSprintMultiplier { get => (isSprinting && controller.isGrounded && moveInput.y > 0) ? sprintMultiplier : 1; }

        // Getters for encapsulated data
        private float LookSensitivityX { get => aimSettings.LookSensitivityX; }
        private float LookSensitivityY { get => aimSettings.LookSensitivityY; }

        public bool InvertY { get => aimSettings.InvertY; }
        public float MaxLookAngle { get => aimSettings.MaxLookAngle; }
        public float MinLookAngle { get => aimSettings.MinLookAngle; }

        [System.Serializable]
        public class InputSettings
        {
            public InputActionReference look;
            public InputActionReference move;
            public InputActionReference jump;
            public InputActionReference sprint;
            public InputActionReference reset;

        }

        public InputSettings inputSettings = new InputSettings();

        // Encapsulating settings under specific classes
        [System.Serializable]
        public class AimSettings
        {
            [Range(1f, 10f)]
            public float AimSpeed = 1f;

            [SerializeField, Range(0f, 10f)]
            public float LookSensitivityX = 5f;

            [SerializeField, Range(0f, 10f)]
            public float LookSensitivityY = 5f;

            public bool InvertY = true;

            [Range(0.0001f, 90f)]
            public float MaxLookAngle = 80f;

            public float UpLowerBound { get => 360f - MaxLookAngle; }

            [Range(-0.0001f, -90f)]
            public float MinLookAngle = -60f;

            [SerializeField, Header("Overloading Settings")]
            internal bool allowOverload = false;

            public bool AllowOverload
            {
                get => allowOverload;
                set
                {
                    allowOverload = value;
                    if (allowOverload && overloaded == null)
                    {
                        overloaded = GameObjectHelpers.CreateDeepCopy(this);
                    }

                }
            }

            public AimSettings overloaded = null;

        }

        public AimSettings aimSettings;

        // Defines advanced functionality

        [System.Serializable]
        public class AdvancedSettings
        {

            [Tooltip("This transform is what will be used to rotate the head. If no head transform is provided, a virtual transform will be instantiated to handle head movement. The rotation can be fetched using the method GetHeadRotation().")]
            public Transform Head = null;

        }

        [System.Serializable]
        public class MomentumSettings
        {

            [System.Serializable]
            public class MomentumConfiguration
            {
                public bool enabled = true;

                [Tooltip("Transitions between not moving and moving.")]
                public Transition momentumBuildup;

                [Tooltip("Transitions between moving and not moving.")]
                public Transition momentumDropoff;

            }

            public bool enabled = false;

            public MomentumConfiguration grounded;
            public bool airborneEnabled = true;

            [Range(0.00001f, 1f), Tooltip("How much velocity we add per airborne frame in the direction of movement. How much can we control the character in the air?")]
            public float airbornePushForce = 0.05f;

        }

        [System.Serializable]
        public class DebugInformation
        {

            [Tooltip("Renders GUI elements and shows other debug info in real time. Disable in prod or for performance checks.")]
            public bool enabled = true;

            public bool enableGravity = true;

            public float debugGravity = Physics.gravity.y;

            [ReadOnly]
            public bool isGrounded = false;

        }

        public MomentumSettings momentumSettings;

        public AdvancedSettings advancedSettings;

        // Internal cached references

        protected CharacterController controller;

        protected Vector2 lookInput;

        private Vector2 _moveInput = Vector2.zero;
        protected Vector2 moveInput
        {
            get => _moveInput;
            set
            {

                // invoke event if movement changed
                bool pushingDirectional = value.magnitude > 0.0001f;
                bool wasStoppedAndIsMoving = Mathf.Approximately(_moveInput.magnitude, 0f) && pushingDirectional;
                bool wasMovingAndIsNowStopped = _moveInput.magnitude > 0f && Mathf.Approximately(value.magnitude, 0f);

                if (wasMovingAndIsNowStopped || wasStoppedAndIsMoving) onMovingStateChange?.Invoke(pushingDirectional);
                if (pushingDirectional) isIdle = false;

                _moveInput = value;

            }
        }

        // local state
        protected bool isMoving = false;

        private Quaternion headRotation = Quaternion.identity;

        protected Vector3 playerVelocity = Vector3.zero;
        public Vector3 PlayerVelocity { get => playerVelocity; }

        private Transition.TransitionState groundedMomentumBuildup;
        private Transition.TransitionState groundedMomentumDropoff;

        private Transition.TransitionState airborneMomentumBuildup;
        private Transition.TransitionState airborneMomentumDropoff;

        private bool _isSprinting = false;
        public bool isSprinting
        {
            get => _isSprinting;
            private set
            {
                onSprintStateChange?.Invoke(value);
                _isSprinting = value;
            }
        }

        private bool _isIdle = false;
        public bool isIdle
        {
            get => _isIdle;
            private set
            {
                // trigger event only if there's a change
                if (value != _isIdle) onIdleStateChange?.Invoke(value);

                _isIdle = value;
            }
        }

        private bool _wasGroundedLastFrame = false;
        public bool isGrounded
        {
            get => controller.isGrounded;
            private set
            {
                // trigger event only if there's a change
                if (value != _wasGroundedLastFrame) onGroundedStateChange?.Invoke(value);

                _wasGroundedLastFrame = value;
            }
        }



        // EVENTS

        /// Event is invoked every time isSprinting is set, and passes this new value to the event.
        public event Action<bool> onSprintStateChange;

        /// Event is invoked every time isSprinting is set, and passes this new value to the event.
        public event Action<bool> onIdleStateChange;

        /// Event is invoked every time moveInput Vector2's magnitude changes from 0 to >0 or from >0 to 0,
        /// and it passes true if the vector's magnitude is non zero.
        public event Action<bool> onMovingStateChange;

        /// Event is invoked every time isSprinting is set, and passes this new value to the event.
        public event Action<bool> onGroundedStateChange;

        /// Event is invoked every time character jumps.
        public event Action onJump;

        private void OnEnable()
        {

            inputSettings.look.action.Enable();
            inputSettings.move.action.Enable();
            inputSettings.jump.action.Enable();
            inputSettings.sprint.action.Enable();
            inputSettings.reset?.action.Enable();

            inputSettings.jump.action.performed += ctx => Jump(JumpHeight);
            inputSettings.sprint.action.started += ctx => isSprinting = true;
            inputSettings.sprint.action.canceled += ctx => isSprinting = false;

            onMovingStateChange += currentlyMoving => isMoving = currentlyMoving;

            // instantiate object if needed
            aimSettings.AllowOverload = aimSettings.AllowOverload;
        }

        private void OnDisable()
        {
            inputSettings.look.action.Disable();
            inputSettings.move.action.Disable();
            inputSettings.jump.action.Disable();
            inputSettings.sprint.action.Disable();
            inputSettings.reset?.action.Disable();
            Cursor.lockState = CursorLockMode.None;
        }

        protected void Start()
        {

            // lock mouse
            Cursor.lockState = CursorLockMode.Locked;

            controller = GetComponent<CharacterController>();

            if (!controller)
            {
                Debug.LogWarning("The player movement behavior requires a character controller. One was instantiated, but it's recommended you attach one manually.");
                controller = gameObject.AddComponent<CharacterController>();
            }

            if (!GetComponentInChildren<Collider>())
            {
                Debug.LogError("This object or its children must have colliders attached in order to work.");
            }

            controller.enabled = true;

            // instantiate tweenTransition states
            groundedMomentumBuildup = momentumSettings.grounded.momentumBuildup.GetPlayableInstance();
            groundedMomentumDropoff = momentumSettings.grounded.momentumDropoff.GetPlayableInstance();

            // begin idle check coroutine
            StartCoroutine(IdleStateDetectorCoroutine());

        }

        protected void FixedUpdate()
        {
            lookInput = inputSettings.look.action.ReadValue<Vector2>();
            moveInput = inputSettings.move.action.ReadValue<Vector2>();

            // broadcast grounded state
            isGrounded = controller.isGrounded;

            ProcessRotation();
            ProcessMovement();
        }

        protected virtual void ProcessMovement()
        {
            // gravity
            bool isGrounded = debugInfo.isGrounded = controller.isGrounded;
            float gravityValue = debugInfo.enabled ? debugInfo.debugGravity : Physics.gravity.y;

            if (isGrounded && playerVelocity.y < 0) playerVelocity.y = 0f;

            // update vertical velocity
            playerVelocity.y += gravityValue * Time.deltaTime;

            // update horizontal momentum
            ProcessMomentum();

            controller.Move(playerVelocity * Time.deltaTime);

        }

        /// keeps track of the animation states
        private float g_buildup, a_buildup, g_dropoff, a_dropoff;

        // Recalculates X and Z axes to 
        private void ProcessMomentum()
        {

            if (!momentumSettings.enabled) return;

            Vector3 moveWithNoUp = transform.rotation * new Vector3(
                moveInput.x * StrafeMovementSpeed,
                0,
                moveInput.y * MovementSpeed * StatefulSprintMultiplier
            );
            Vector3 curVelocityWithNoUp = new Vector3(playerVelocity.x, 0, playerVelocity.z);
            Vector3 interpolatedMovement = moveWithNoUp; // default to current player impulse

            bool validForGround = controller.isGrounded && momentumSettings.grounded.enabled;
            bool validForAir = !controller.isGrounded && momentumSettings.airborneEnabled;
            bool valid = validForGround || validForAir;
            bool shouldDropoff = (curVelocityWithNoUp.x != 0 || curVelocityWithNoUp.z != 0);
            bool shouldBuildup = moveWithNoUp.magnitude > 0f && ((Mathf.Abs(curVelocityWithNoUp.x) <= StrafeMovementSpeed || Mathf.Abs(curVelocityWithNoUp.z) <= MovementSpeed * StatefulSprintMultiplier));
            bool jumping = playerVelocity.y > 0;

            // TODO: if move velocity (direction) is lower than the current velocity we dropoff to that velocity

            if (validForGround && shouldBuildup)
            {

                // reset dropoff transitions that could have been playing
                groundedMomentumDropoff.ResetAnimation();

                // sync both animations to smooth tweenTransition the curves (don't restart an animation if in the middle of it)
                g_buildup = groundedMomentumBuildup.Evaluate(Time.fixedDeltaTime);

                interpolatedMovement = Vector3.Lerp(curVelocityWithNoUp, moveWithNoUp, g_buildup);

            }

            else if (validForGround && shouldDropoff)
            {
                // reset dropoff transitions that could have been playing
                groundedMomentumBuildup.ResetAnimation();

                // sync both animations to smooth tweenTransition the curves (don't restart an animation if in the middle of it)
                g_dropoff = groundedMomentumDropoff.Evaluate(Time.fixedDeltaTime);

                // using moveWithNoUp in this instance means that we either drop off to zero or we drop off from a higher speed to max speed
                interpolatedMovement = Vector3.Lerp(curVelocityWithNoUp, moveWithNoUp, g_dropoff);

            }

            else if (validForAir)
            {

                int xPush = moveInput.x > 0 ? 1 : -1;
                int zPush = moveInput.y > 0 ? 1 : -1;

                // no pushing if not pushing stick
                if (Mathf.Approximately(moveInput.x, 0f)) xPush = 0;
                if (Mathf.Approximately(moveInput.y, 0f)) zPush = 0;

                int xLimiter = Mathf.Abs(curVelocityWithNoUp.x) < StrafeMovementSpeed ? 1 : 0;
                int zLimiter = Mathf.Abs(curVelocityWithNoUp.z) <= MovementSpeed * StatefulSprintMultiplier ? 1 : 0;

                // rotated push -> push but rotated to transform
                Vector3 rotatedPush = transform.rotation * new Vector3(
                    (xPush * xLimiter),
                    0,
                    (zPush * zLimiter)
                ).normalized * momentumSettings.airbornePushForce;

                // we want to push in the direction of the stick, but only a little bit
                interpolatedMovement = new Vector3(curVelocityWithNoUp.x + rotatedPush.x, 0, curVelocityWithNoUp.z + rotatedPush.z);
            }

            // set movement state
            playerVelocity.x = interpolatedMovement.x;
            playerVelocity.z = interpolatedMovement.z;

        }

        private void ProcessRotation()
        {
            var aimspd = aimSettings.allowOverload ? aimSettings.overloaded.AimSpeed : aimSettings.AimSpeed;

            transform.Rotate(0, lookInput.x * LookSensitivityX * Time.deltaTime * aimspd, 0);
            RotateHead();
        }

        private void RotateHead()
        {
            var aimspd = aimSettings.allowOverload ? aimSettings.overloaded.AimSpeed : aimSettings.AimSpeed;
            var newX = headRotation.eulerAngles.x + lookInput.y * LookSensitivityY * aimspd * Time.deltaTime * (InvertY ? -1 : 1);

            headRotation = Quaternion.Euler(
                Geometry.ClampAngle(newX, -MaxLookAngle, -MinLookAngle),
                headRotation.eulerAngles.y,
                headRotation.eulerAngles.z
            );

            if (advancedSettings.Head != null)
            {
                advancedSettings.Head.localRotation = headRotation;
            }
        }

        private void Jump(float jumpHeight)
        {
            var gravityValue = debugInfo.enabled ? debugInfo.debugGravity : Physics.gravity.y;
            if (debugInfo.isGrounded)
            {
                playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                onJump?.Invoke();
            }
        }

        /// Returns current rotation of the head.
        public Quaternion GetHeadRotation() { return headRotation; }

        /// Coroutine that runs idle in the background with the given frequency and if it detects
        /// that the player is non moving and continues to be non moving after the GoIdleInSeconds parameter
        /// has elapsed, the isIdle state is flipped to true.

        private IEnumerator IdleStateDetectorCoroutine(float probingFrequencyInterval = 0.3f)
        {
            // do forever
            while (true)
            {

                while (!isMoving)
                {

                    yield return new WaitForSecondsRealtime(GoIdleInSeconds);

                    // state may have changed, but we account for it with !
                    isIdle = !isMoving;

                }

                yield return new WaitForSecondsRealtime(probingFrequencyInterval);
            }
        }

        // Debug text
        protected void OnGUI()
        {
            if (!debugInfo.enabled) return;

            const int margin = 10;
            const int h_margin = 50;
            const int space = 4;
            const int height = 40;

            GUI.Label(new Rect(h_margin, margin, 200, height), $"Look input [ {lookInput.x}, {lookInput.y} ] ");
            GUI.Label(new Rect(h_margin, 2 * margin + space, 200, height), $"Move input [ {moveInput.x}, {moveInput.y} ] ");
            GUI.Label(new Rect(h_margin, 3 * margin + 2 * space, 600, height), $"Head rotation [ {headRotation.eulerAngles.x}, {headRotation.eulerAngles.y}, {headRotation.eulerAngles.z} ] ");
            GUI.Label(new Rect(h_margin, 4 * margin + 3 * space, 600, height), $"Grounded? [ {controller.isGrounded} ], Player Velocity: {playerVelocity} ");
            GUI.Label(new Rect(h_margin, 5 * margin + 4 * space, 600, height), $"Momentum dropoff value [ g: {g_dropoff} a: {a_dropoff} ]");
            GUI.Label(new Rect(h_margin, 6 * margin + 5 * space, 600, height), $"Momentum buildup value [ g: {g_buildup} a: {a_buildup} ]");
            GUI.Label(new Rect(h_margin, 7 * margin + 6 * space, 600, height), $"Sprinting [ {isSprinting} ]");
            GUI.Label(new Rect(h_margin, 8 * margin + 7 * space, 600, height), $"Moving [ {isMoving} ]");
            GUI.Label(new Rect(h_margin, 9 * margin + 8 * space, 600, height), $"Idle [ {isIdle} ]");

        }
    }
}
