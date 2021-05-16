using UnityEngine;
using ForLoopCowboyCommons.Geometry;
using ForLoopCowboyCommons.EditorHelpers;

public class PlayerMovementBehavior : MonoBehaviour
{
    // Publicly exposed settings

    [Tooltip("Movement speed going forward and backwards.")]
    public float MovementSpeed = 5f;

    [Tooltip("Movement speed going sideways.")]
    public float StrafeMovementSpeed = 5f;

    [Tooltip("Height the character can jump.")]
    public float JumpHeight = 1f;

    // Getters for encapsulated data
    private float LookSensitivityX { get => aimSettings.LookSensitivityX; }
    private float LookSensitivityY { get => aimSettings.LookSensitivityY; }

    public bool InvertY { get => aimSettings.InvertY; }
    public float MaxLookAngle { get => aimSettings.MaxLookAngle; }
    public float MinLookAngle { get => aimSettings.MinLookAngle; }

    // Encapsulating settings under specific classes
    [System.Serializable]
    public class AimSettings
    {
        [Range(1f, 10f)]
        public float AimSpeed = 1f;

        [SerializeField, Range(0f, 10f)]
        public float LookSensitivityX = 5f;

        public bool InvertX = false;

        [SerializeField, Range(0f, 10f)]
        public float LookSensitivityY = 5f;

        public bool InvertY = true;

        [Range(0.0001f, 90f)]
        public float MaxLookAngle = 80f;

        public float UpLowerBound { get => 360f - MaxLookAngle; }

        [Range(-0.0001f, -90f)]
        public float MinLookAngle = -60f;

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

        public bool enabled = false;

        [Tooltip("When checked, momentum settings will apply also while grounded.")]
        public bool enableWhileGrounded = false;

        [Tooltip("Transitions between not moving and moving.")]
        public Transition momentumBuildup;

        [Tooltip("Transitions between moving and not moving.")]
        public Transition momentumDropoff;

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
    public DebugInformation debugInfo;

    // Internal cached references

    private CharacterController controller;

    private Vector2 lookInput;
    private Vector2 moveInput;

    private PlayerControls controls;

    private Quaternion headRotation = Quaternion.identity;

    private Vector3 playerVelocity = Vector3.zero;

    private Transition.TransitionState momentumBuildup;
    private Transition.TransitionState momentumDropoff;

    private void OnEnable()
    {
        controls = new PlayerControls();
        controls.Default.Look.Enable();
        controls.Default.Move.Enable();
        controls.Default.Jump.Enable();

        controls.Default.Jump.performed += ctx => Jump(JumpHeight);
    }

    private void OnDisable()
    {
        controls.Default.Look.Disable();
        controls.Default.Move.Disable();
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
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

        // instantiate transition states
        momentumBuildup = momentumSettings.momentumBuildup.GetPlayableInstance();
        momentumDropoff = momentumSettings.momentumDropoff.GetPlayableInstance();

    }

    /// In Update(), read inputs and update current cache.
    private void FixedUpdate()
    {
        lookInput = controls.Default.Look.ReadValue<Vector2>();
        moveInput = controls.Default.Move.ReadValue<Vector2>();

        ProcessRotation();
        ProcessMovement();
    }

    private void ProcessMovement()
    {
        bool isGrounded = debugInfo.isGrounded = controller.isGrounded;
        float gravityValue = debugInfo.enabled ? debugInfo.debugGravity : Physics.gravity.y;

        if (isGrounded && playerVelocity.y < 0) playerVelocity.y = 0f;

        // update vertical velocity
        playerVelocity.y += gravityValue * Time.deltaTime;

        // update horizontal momentum
        ProcessMomentum();

        controller.Move(playerVelocity * Time.deltaTime);

    }

    // Recalculates X and Z axes to 
    private void ProcessMomentum()
    {
        if (!momentumSettings.enabled) return;

        bool doNotInterpolate = (!momentumSettings.enableWhileGrounded && controller.isGrounded);

        Vector3 moveWithNoUp = transform.rotation * new Vector3(moveInput.x * StrafeMovementSpeed, 0, moveInput.y * MovementSpeed);
        Vector3 curVelocityWithNoUp = new Vector3(playerVelocity.x, 0, playerVelocity.z);

        // default to current velocity
        Vector3 interpolatedMove = curVelocityWithNoUp;

        // pass input through directly
        if (doNotInterpolate) {
            interpolatedMove = moveWithNoUp;
        }

        // not pushing the directional && currently moving, momentum drop off until zero
        else if (
            Mathf.Approximately(moveWithNoUp.magnitude, 0f) &&
             (curVelocityWithNoUp.x != 0 ||
             curVelocityWithNoUp.z != 0)
            ) {
            
            momentumBuildup.ResetAnimation();
            interpolatedMove = Vector3.Lerp(curVelocityWithNoUp, Vector3.zero, momentumDropoff.Evaluate(Time.fixedDeltaTime));

        // pushing the directional and below the max speed on either axis, buildup using lerp
        } else if (
            moveWithNoUp.magnitude > 0 &&
            (Mathf.Abs(curVelocityWithNoUp.x) < StrafeMovementSpeed ||
             Mathf.Abs(curVelocityWithNoUp.z) < MovementSpeed)
        ) { 
            
            momentumDropoff.ResetAnimation();
            interpolatedMove = Vector3.Lerp(curVelocityWithNoUp, moveWithNoUp, momentumBuildup.Evaluate(Time.fixedDeltaTime));

        }


        playerVelocity.x = interpolatedMove.x;
        playerVelocity.z = interpolatedMove.z;

    }

    private void ProcessRotation()
    {
        transform.Rotate(0, lookInput.x * LookSensitivityX * Time.deltaTime * aimSettings.AimSpeed, 0);
        RotateHead();
    }

    private void RotateHead()
    {
        var newX = headRotation.eulerAngles.x + lookInput.y * LookSensitivityY * aimSettings.AimSpeed * Time.deltaTime * (InvertY ? -1 : 1);

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
        if (debugInfo.isGrounded) playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
    }

    /// Returns current rotation of the head.
    public Quaternion GetHeadRotation() { return headRotation; }

    // Debug text
    private void OnGUI()
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
        GUI.Label(new Rect(h_margin, 5 * margin + 4 * space, 200, height), $"Momentum dropoff value [ {momentumDropoff.Evaluate(0)} ]");

    }
}
