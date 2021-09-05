using System;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Player
{
    /// Builds upon the regular behavior by adding ledge grabbing
    /// And crouching.
    public class AdvancedPlayerMovementBehaviour : PlayerMovementBehavior
    {
        [System.Serializable]
        public class LedgeGrabSettings
        {

            [Tooltip("Which layer are the ledges defined?")]
            public LayerMask LedgeLayerMask;

            [Tooltip("Controls how we interpolate between self and the interpolated point P (see code). Both transitions must finish in order to complete the animation.")]
            public Transition transition;

            [Tooltip("Controls how we transition horizontally when grabbing a ledge. Both transitions must finish in order to complete the animation.")]
            public Transition horizontalInterpolation;

            [Tooltip("Scales animation time depending on the distance to the ledge. Longer distance = longer animations if the box is checked.")]
            public bool scaleWithDistance = true;

            [Range(0.0001f, 1f)]
            public float maxTimeScale = 1f;

            [SerializeField]
            internal float armLength;

            public Vector3 detectorCenter;

            public Vector3 detectorSize;

            internal BoxCollider ledgeTrigger;

            [System.Serializable]
            public class State
            {
                [ReadOnly] public bool ledgeIsWithinReach = false;

                [ReadOnly, Tooltip("The current value of the animation scale.")]
                public float animationScaleValue = 1f;
            }

            [SerializeField]
            internal State state;
        }

        public LedgeGrabSettings ledgeGrabSettings;

        /// local cached variables

        // position of the ledge we *can* grab in world coordinates. only valid if isLedgeWithinReach is set to true
        private Vector3 g_targetLedgeGrabPosition = Vector3.zero;

        // position horizontally equal to self, vertically at the height of the ledge, updated at the time a ledge is found.
        private Vector3 g_ledgeGrabInterpolationSource = Vector3.zero;

        private Vector3 g_transformWhenLedgeGrabStarted = Vector3.zero;

        private Transition.TransitionState grabAnim;
        private Transition.TransitionState horizontalLerpAnimation;

        private bool _isLedgeWithinReach = false;
        public bool isLedgeWithinReach
        {
            get => _isLedgeWithinReach;
            set
            {
                if (value != _isLedgeWithinReach) onLedgeWithinReachState?.Invoke(value);
                _isLedgeWithinReach = value;
                ledgeGrabSettings.state.ledgeIsWithinReach = value;
            }
        }

        public event Action<bool> onLedgeWithinReachState;

        new private void Start()
        {
            // perform default initialization
            base.Start();
            grabAnim = ledgeGrabSettings.transition.GetPlayableInstance();
            horizontalLerpAnimation = ledgeGrabSettings.horizontalInterpolation.GetPlayableInstance();

            // interpolation animation must have length equal or longer than grab anim
            if (ledgeGrabSettings.horizontalInterpolation.duration < ledgeGrabSettings.transition.duration)
            {
                Debug.LogWarning("Horizontal interpolation should be equal or longer than grab animation or there will be some collisions or floating.");
            }

            if (inputSettings.reset) inputSettings.reset.action.performed += ctx =>
            {
                controller.enabled = false;
                transform.position = Vector3.zero;
                controller.enabled = true;
            };

        }

        private void Update()
        {

            if (isLedgeWithinReach && inputSettings.jump.action.triggered && !currentlyGrabbingLedge)
            {
            
                controller.enabled = false; // must disable controller to force transform position.
                currentlyGrabbingLedge = true;

                grabAnim.ResetAnimation();
                horizontalLerpAnimation.ResetAnimation();

                // determine animation scaling
                if (ledgeGrabSettings.scaleWithDistance) 
                {

                    // if our distance is zero, we don't scale it. if our distance is >= the arm length, we scale by the max scale factor
                    AnimationCurve scaleFunction = AnimationCurve.Linear(ledgeGrabSettings.armLength * .5f, 1f, ledgeGrabSettings.armLength * 2, ledgeGrabSettings.maxTimeScale);
                    scaleFunction.postWrapMode = WrapMode.ClampForever;
                    scaleFunction.preWrapMode = WrapMode.ClampForever;

                    var distance = Vector3.Distance(g_targetLedgeGrabPosition, g_transformWhenLedgeGrabStarted);

                    // update animation instances accordingly
                    grabAnim.duration = ledgeGrabSettings.transition.duration / scaleFunction.Evaluate(distance);

                    // debug 
                    ledgeGrabSettings.state.animationScaleValue = ledgeGrabSettings.transition.duration / scaleFunction.Evaluate(distance);

                }
                else {
                    // simply reset to the prefab duration if scale with distance is off
                    grabAnim.duration = ledgeGrabSettings.transition.duration;
                    horizontalLerpAnimation.duration = ledgeGrabSettings.horizontalInterpolation.duration;
                    ledgeGrabSettings.state.animationScaleValue = 1f;
                }

            }
        }

        private bool currentlyGrabbingLedge = false;
        Vector3 destination;
        protected override void ProcessMovement()
        {

            // // box casts and sets some flags & shit
            if (!currentlyGrabbingLedge) DetectLedges();

            if (currentlyGrabbingLedge) ProcessLedgeGrab();
            else 
            {
                base.ProcessMovement();
            }

        }

        Vector3 intermediatePointP = Vector3.zero;
        Vector3 destinationPoint = Vector3.zero;

        protected void ProcessLedgeGrab()
        {

            // Update position of IK
            // FIXME: broken.
            // iKController.leftHand.positionRelativeToHead = new Vector3(
            //     L.x,
            //     ledgeRelativeToHead.y,
            //     ledgeRelativeToHead.z
            // );

            // iKController.rightHand.positionRelativeToHead = new Vector3(
            //     R.x,
            //     ledgeRelativeToHead.y,
            //     ledgeRelativeToHead.z
            // );


            // interpolate between A and B => find point P
            // interpolate between self and P
            // move to that point

            intermediatePointP = Vector3.Lerp(
                g_ledgeGrabInterpolationSource,
                g_targetLedgeGrabPosition,
                horizontalLerpAnimation.Evaluate(Time.fixedDeltaTime)
            );

            destinationPoint = Vector3.Lerp(
                g_transformWhenLedgeGrabStarted,
                intermediatePointP,
                grabAnim.Evaluate(Time.fixedDeltaTime)
            );

            transform.position = destinationPoint;

            // terminate ledge grab only when both animations are finished
            if (grabAnim.finished && horizontalLerpAnimation.finished)
            {
                // pressing space may have added velocity, so we reset
                playerVelocity = Vector3.zero;
                currentlyGrabbingLedge = false;
                controller.detectCollisions = true;
                controller.enabled = true;
            }

        }


        private void DetectLedges()
        {
            // detect ledge before any other fixed update behavior
            RaycastHit hit;
            var globalPos = transform.TransformPoint(ledgeGrabSettings.detectorCenter);

            if (
                Physics.BoxCast(
                    globalPos,
                    ledgeGrabSettings.detectorSize,
                    transform.rotation * Vector3.forward,
                    out hit,
                    transform.rotation,
                    ledgeGrabSettings.armLength,
                    ledgeGrabSettings.LedgeLayerMask
                )
            )
            {

                Transform ledge = hit.transform;
                Collider ledgeCollider = ledge.GetComponent<Collider>();

                // project our position on the edge of the top plane of the ledge
                Vector3 tempContactPoint = ledgeCollider.ClosestPointOnBounds(globalPos);
                tempContactPoint = new Vector3(tempContactPoint.x, ledgeCollider.bounds.max.y, tempContactPoint.z);

                g_targetLedgeGrabPosition = tempContactPoint;
                g_ledgeGrabInterpolationSource = new Vector3(transform.position.x, g_targetLedgeGrabPosition.y, transform.position.z);
                g_transformWhenLedgeGrabStarted = transform.position;
                isLedgeWithinReach = true;

            }
            else isLedgeWithinReach = false;
        }

        private void OnDrawGizmos()
        {

            if (!debugInfo.enabled) return;

            ExtendedDebug.DrawBoxCastBox(
                transform.TransformPoint(ledgeGrabSettings.detectorCenter),
                ledgeGrabSettings.detectorSize,
                transform.rotation,
                transform.rotation * Vector3.forward,
                ledgeGrabSettings.armLength,
                new Color(0.12345678f, 0.36273672f, 0.4632746328746f, 0.3444444444449f)
            );

            if (isLedgeWithinReach)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(
                    g_targetLedgeGrabPosition,
                    0.06273672f
                );
            }

            if (currentlyGrabbingLedge)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(
                    g_ledgeGrabInterpolationSource,
                    0.05f
                );

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(
                    intermediatePointP,
                    0.08f
                );
                Gizmos.DrawWireSphere(
                    destinationPoint,
                    0.08f
                );
            }

        }

        new protected void OnGUI() {
            if (!debugInfo.enabled) return;

            base.OnGUI();

            const int margin = 10;
            const int h_margin = 50;
            const int space = 4;
            const int height = 40;

            GUI.Label(new Rect(h_margin, 10 * margin + 9 * space, 600, height), $"Grab anim length [ {grabAnim.duration} ]  Grab anim length [ {horizontalLerpAnimation.duration} ]");

        }

    }
}
