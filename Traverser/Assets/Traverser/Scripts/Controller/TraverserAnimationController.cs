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

        [Header("Animation")]
        [Tooltip("Reference to the skeleton's parent. The controller positions the skeleton at the skeletonRef's position. Used to kill animation's root motion.")]
        public Transform skeleton;
        [Tooltip("Reference to the skeleton's reference position. A transform that follows the controller's object motion, with an offset to the bone position (f.ex hips).")]
        public Transform skeletonRef;

        [Header("Warping")]
        [Tooltip("The distance at which the character has to be within for warping success.")]
        [Range(0.1f, 0.3f)]
        public float warpingValidDistance = 0.1f;

        [Header("Debug")]
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Select the object to show debug geometry.")]
        public bool debugDraw = false;

        [Tooltip("The radius of the spheres used to debug draw.")]
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

        // --- Used to store last matchPosition, debug draw purposes ---
        private Vector3 lastWarpPosition;

        // --- Rotation that has to be warped in the current frame given timeToTarget, pre deltaTime ---
        private Quaternion currentdeltaRotation;


        // --------------------------------

        private void Awake()
        {
            currentdeltaPosition = Vector3.zero;
            lastWarpPosition = Vector3.zero;

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
            // --- Ensure the skeleton does not get separated from the controller (forcing in-place animation) ---
            if (!transition.isON && !fakeTransition)
                skeleton.transform.position = skeletonRef.transform.position;
            else
            {
                // --- Apply warping ---
                if (transition.isWarping)
                {
                    transform.position = Vector3.Lerp(transform.position, transform.position + currentdeltaPosition, Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, currentdeltaRotation, Time.deltaTime);
                }

                controller.targetHeading = 0.0f;
                controller.targetDisplacement = Vector3.zero;
                //currentdeltaPosition = Vector3.zero;
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


        public bool WarpToTarget(Vector3 matchPosition, Quaternion matchRotation, float validDistance)
        {
            bool ret = true;

            // --- Warp position and rotation to match matchPosition and matchRotation ---  
            lastWarpPosition = matchPosition;

            // --- Check whether we are in a transition or target animation ---
            bool loop = animator.GetCurrentAnimatorStateInfo(0).loop;

            // --- Compute delta position to be covered ---
            if (loop)
            {
                Vector3 currentPosition = transform.position;
                matchPosition.y = currentPosition.y;

                // --- Compute distance to match position and velocity ---
                Vector3 validPosition = matchPosition - (transform.forward * validDistance);

                // --- A looped animation, one of the transition Animations, no root motion ---
                Vector3 desiredDisplacement = validPosition - currentPosition;
                Vector3 velocity = desiredDisplacement.normalized * controller.targetVelocity.magnitude;
                float time = desiredDisplacement.magnitude / velocity.magnitude;

                currentdeltaPosition = desiredDisplacement / time;
                currentdeltaRotation = Quaternion.SlerpUnclamped(transform.rotation, matchRotation, 1.0f / time);

                if (Vector3.Distance(currentPosition, validPosition) < warpingValidDistance)
                {
                    currentdeltaPosition = Vector3.zero;
                    transform.rotation = matchRotation; // force final rotation
                    currentdeltaRotation = transform.rotation;
                    
                    ret = false;
                }
            }
            else
            {
                Vector3 currentPosition = skeleton.transform.position;
                matchPosition.y = currentPosition.y;

                // --- Compute distance to match position and velocity ---
                Vector3 validPosition = matchPosition + (transform.forward * validDistance);

                // --- A targetAnimation, we want to take profit of the animation's motion ---
                Vector3 desiredDisplacement = validPosition - currentPosition;

                // For maximum time left precision we should take into account the exit transition time too 
                float time = animator.GetCurrentAnimatorStateInfo(0).length - 
                    (animator.GetCurrentAnimatorStateInfo(0).normalizedTime*animator.GetCurrentAnimatorStateInfo(0).length);
                
                currentdeltaPosition = desiredDisplacement / time;
                currentdeltaRotation = Quaternion.SlerpUnclamped(transform.rotation, matchRotation, 1.0f / time);

                if (Vector3.Distance(currentPosition, validPosition) < warpingValidDistance)
                {
                    currentdeltaPosition = Vector3.zero;
                    transform.rotation = matchRotation; // force final rotation
                    currentdeltaRotation = transform.rotation;
                    ret = false;
                }
            }

            return ret;
        }

        public void SetRootMotion(bool rootMotion)
        {
            animator.applyRootMotion = rootMotion;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDraw || controller == null)
                return;

            // --- Draw transition contact and target point ---
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastWarpPosition, contactDebugSphereRadius);
        }

        // --------------------------------

    }
}
