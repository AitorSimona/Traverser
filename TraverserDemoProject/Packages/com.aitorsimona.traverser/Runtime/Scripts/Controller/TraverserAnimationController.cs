using System;
using System.Collections.Generic;
using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserAnimationController : MonoBehaviour
    {
        // --- Attributes ---

        // --- Stores animator parameters for future reference ---
        public Dictionary<string, int> animatorParameters;

        public struct AnimatorParameters
        {
            public bool Move;
            public float Speed;
            public float Heading;

            public int MoveID;
            public int SpeedID;
            public int HeadingID;
        }

        [Serializable]
        public struct AnimationData
        {
            [Tooltip("The animation state name (Animator state not animation name!).")]
            public string animationStateName;

            [Tooltip("Time in seconds manual animation states transitions will take.")]
            public float transitionDuration;
        }

        [Header("Animation")]
        [Tooltip("Reference to the skeleton's parent. The controller positions the skeleton at the skeletonRef's position. Used to kill animation's root motion.")]
        public Transform skeleton;

        [Tooltip("Reference to the skeleton's reference position. A transform that follows the controller's object motion, with an offset to the bone position (f.ex hips).")]
        public Transform skeletonRef;

        [Header("Warping")]
        [Tooltip("The distance at which the character has to be within for warping success.")]
        [Range(0.1f, 0.3f)]
        public float warpingValidDistance = 0.1f;

        [Tooltip("The bigger the speed the more influence the warper will have over motion.")]
        [Range(1.0f, 10.0f)]
        public float warpingSpeed = 3.0f;

        [Header("Debug")]
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Select the object to show debug geometry.")]
        public bool debugDraw = false;

        [Tooltip("The radius of the spheres used to debug draw contact and target transforms.")]
        [Range(0.1f, 1.0f)]
        public float contactDebugSphereRadius = 0.5f;

        // --- Animator and animation transition handler ---
        public Animator animator;
        public TraverserTransition transition;

        // --------------------------------

        // --- Use in case you do not want the skeleton to be adjusted to the reference in a custom transition ---
        [HideInInspector]
        public bool fakeTransition = false;

        // --- Private Variables ---

        private TraverserCharacterController controller;

        // --- Motion that has to be warped in the current frame given timeToTarget, pre deltaTime ---
        private Vector3 currentdeltaPosition;

        // --- Rotation that has to be warped in the current frame given timeToTarget, pre deltaTime ---
        private Quaternion currentdeltaRotation;

        // --- Simple way of making sure to only compute bodyEndPosition and targetWarpTime once  (at the beginning of target animation warping) ---
        private bool warpStart = true;
        private Vector3 previousMatchPosition;

        // --- The position at which the skeleton will arrive at the end of the current animation (root motion) --- 
        private Vector3 bodyEndPosition;

        // --- Time left until animation end transition, the available time to apply warping ---
        private float targetWarpTime;

        // --- The distance in Y we need to warp --- 
        private float targetYWarp;


        // --------------------------------

        private void Awake()
        {
            currentdeltaPosition = Vector3.zero;

            controller = GetComponent<TraverserCharacterController>();
        }

        private void Start()
        {
            transition = new TraverserTransition(this, ref controller);
            animatorParameters = new Dictionary<string, int>();

            // --- Save all animator parameter hashes for future reference ---
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                animatorParameters.Add(param.name, param.nameHash);
            }
        }

        // --- Basic Methods ---

        private void LateUpdate()
        {
            // --- Ensure the skeleton does not get separated from the controller when not in a transition (forcing in-place animation since root motion is being baked into some animations) ---
            if (!transition.isON && !fakeTransition)
                skeleton.transform.position = skeletonRef.transform.position;
            else
            {
                // --- Apply warping ---
                if (transition.isWarping)
                {
                    float previousY = transform.position.y;
                    transform.position = Vector3.Lerp(transform.position, transform.position + currentdeltaPosition, Time.deltaTime * warpingSpeed);
                    transform.rotation = Quaternion.Slerp(transform.rotation, currentdeltaRotation, Time.deltaTime);

                    // --- Update time and Y warping ---
                    targetYWarp -= transform.position.y - previousY;
                    targetWarpTime -= Time.deltaTime;
                    targetWarpTime = Mathf.Max(targetWarpTime, 0.1f);
                }

         
                controller.targetHeading = 0.0f;
                controller.targetDisplacement = Vector3.zero;
            }
        }

        // --------------------------------

        // --- Utility Methods ---

        public void InitializeAnimatorParameters(ref AnimatorParameters parameters)
        {
            parameters.Move = false;
            parameters.Speed = 0.0f;
            parameters.Heading = 0.0f;
            parameters.MoveID = Animator.StringToHash("Move");
            parameters.SpeedID = Animator.StringToHash("Speed");
            parameters.HeadingID = Animator.StringToHash("Heading");
        }

        public void UpdateAnimator(ref AnimatorParameters parameters)
        {
            // --- Update animator with the given parameter's values ---
            animator.SetBool(parameters.MoveID, parameters.Move);
            animator.SetFloat(parameters.SpeedID, parameters.Speed);
            animator.SetFloat(parameters.HeadingID, parameters.Heading);
        }

        public bool WarpToTarget(Vector3 matchPosition, Quaternion matchRotation, bool warpY = true, bool forceSuccess = false)
        {
            bool ret = true;

            // --- Check whether we are in a transition or target animation ---
            bool loop = animator.GetCurrentAnimatorStateInfo(0).loop;

            // --- If a new warp has been issued, reset start variables ---
            if (previousMatchPosition != matchPosition)
                warpStart = true;

            // --- Compute bodyEndPosition and targetWarpTime once at the beginning of warping ---
            if (!loop && warpStart)
            {
                warpStart = false;
                previousMatchPosition = matchPosition;

                // --- Compute current target animation's final position (root motion) ---
                GetPositionAtTime(1.0f, out bodyEndPosition);

                // --- Compute warping time (time left until animation end) ---
                targetWarpTime = animator.GetCurrentAnimatorStateInfo(0).length  // total animation length
                    - (animator.GetCurrentAnimatorStateInfo(0).normalizedTime * animator.GetCurrentAnimatorStateInfo(0).length) // start transition time
                    ; // end transition time ?

                targetWarpTime = Mathf.Max(targetWarpTime, 0.001f);

                // --- Compute difference between root motion's target Y and our target Y ---   
                targetYWarp = matchPosition.y - bodyEndPosition.y;
            }

            // --- Compute delta position to be covered ---
            if (loop)
            {
                Vector3 currentPosition = skeleton.transform.position;
                // --- Prevent transition animations from warping Y ---
                matchPosition.y = currentPosition.y;

                // --- A looped animation, one of the transition Animations, no root motion ---
                Vector3 desiredDisplacement = matchPosition - currentPosition;
                Vector3 velocity = desiredDisplacement.normalized * Mathf.Max(controller.targetVelocity.magnitude, 1.0f);
                float time = desiredDisplacement.magnitude / velocity.magnitude;

                currentdeltaPosition = desiredDisplacement / time;
                currentdeltaRotation = Quaternion.SlerpUnclamped(transform.rotation, matchRotation, 1.0f / time);

                if (Vector3.Magnitude(matchPosition - currentPosition) < warpingValidDistance
                    || forceSuccess)
                {
                    currentdeltaPosition = Vector3.zero;
                    transform.rotation = matchRotation; // force final rotation
                    currentdeltaRotation = transform.rotation;                    
                    ret = false;
                }
            }
            else
            {
                // --- In our target animation, we cover the Y distance ---
                Vector3 currentPosition = skeleton.transform.position;
                Vector3 validPosition = matchPosition;
                validPosition.y = currentPosition.y;

                // --- A targetAnimation, we want to take profit of the animation's motion ---
                Vector3 desiredDisplacement = validPosition - currentPosition;

                if (warpY)
                {
                    desiredDisplacement.y = targetYWarp;

                    // --- If remaining YWarp is small enough, end warping ---
                    if (Mathf.Abs(targetYWarp) < warpingValidDistance)
                    {
                        desiredDisplacement.y = matchPosition.y - currentPosition.y;
                        //forceSuccess = true;
                    }

                }
                else
                    matchPosition.y = currentPosition.y;

                currentdeltaPosition = desiredDisplacement / targetWarpTime;             
                currentdeltaRotation = Quaternion.SlerpUnclamped(transform.rotation, matchRotation, 1.0f / targetWarpTime);
          
                if (Vector3.Magnitude(matchPosition - currentPosition) < warpingValidDistance
                    || forceSuccess)
                {
                    currentdeltaPosition = Vector3.zero;
                    transform.rotation = matchRotation; // force final rotation
                    currentdeltaRotation = transform.rotation;
                    ret = false;
                }
            }

            return ret;
        }

        public void ResetWarper()
        {
            currentdeltaPosition = Vector3.zero;
            currentdeltaRotation = transform.rotation;
            targetWarpTime = 0.001f;
            bodyEndPosition = transform.position;
        }

        public void SetRootMotion(bool rootMotion)
        {
            animator.applyRootMotion = rootMotion;
        }

        public bool IsTransitionFinished()
        {
            // ALERT!: Call only if your state has linked transitions
            return !animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f;
        }

        public void GetPositionAtTime(float normalizedTime, out Vector3 position)
        {
            // --- Samples current animation at the given normalized time (0.0f - 1.0f) ---
            // --- Useful to know at what position will an animation end ---

            SetRootMotion(true);
            animator.SetTarget(AvatarTarget.Body, normalizedTime);
            animator.Update(0);
            position = animator.targetPosition;
            SetRootMotion(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDraw || controller == null)
                return;

            // --- Draw transition contact and target point ---
            transition.DebugDraw(contactDebugSphereRadius);
        }

        // --------------------------------

    }
}
