using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserAnimationController : MonoBehaviour
    {
        // --- Attributes ---

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

        // --- Animator and animation transition handler ---
        public Animator animator;
        public TraverserTransition transition;

        private Vector3 deltaPosition;
        private Vector3 currentdeltaPosition;
        private Quaternion deltaRotation;

        // --------------------------------

        // --- Private Variables ---

        private TraverserCharacterController controller;

        // --------------------------------

        private void Awake()
        {
            deltaPosition = Vector3.zero;
            currentdeltaPosition = Vector3.zero;

            controller = GetComponent<TraverserCharacterController>();
            transition = new TraverserTransition(this, ref controller);       
        }

        // --- Basic Methods ---
        private void Start()
        {

        }

        private void LateUpdate()
        {
            if (!transition.isON)
                skeleton.transform.position = skeletonRef.transform.position;
            else
            {
                transform.position += currentdeltaPosition * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, deltaRotation, Time.deltaTime);
                currentdeltaPosition = Vector3.zero;
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

        /// <summary>
        /// Adjusts game object's position and rotation to match givern position and rotation in the given time with current animation
        /// </summary>
        /// <param name="matchPosition"> The desired position to reach with target matching.</param>
        /// <param name="matchRotation"> The desired rotation to reach with target matching.</param>
        /// <param name="target"> Which bone should reach the desired transform.</param>
        /// <param name="weightMask"> How much importance should match position and rotation have in target matching.</param>
        /// <param name="normalisedStartTime"> At which animation timeframe should we start matching .</param>
        /// <param name="normalisedEndTime"> At which animation timeframe should we end matching.</param>
        /// <param name="validDistance"> If close enough to validDistance (meters), end target matching.</param>

        public bool WarpToTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime, float validDistance)
        {
            bool ret = true;

            // --- Warp position and rotation to match target transform ---  
            
            // --- Compute distance to match position and velocity ---
            Vector3 difference = matchPosition - transform.position;
            Vector3 velocity = difference - deltaPosition;

            // --- If velocity is zero, which means we just started warping, set it to one ---
            if (deltaPosition == Vector3.zero)
                velocity = Vector3.one;

            // --- Compute time to reach match position ---
            float timeToTarget = difference.magnitude / velocity.magnitude;
            //Debug.Log(timeToTarget);

            // --- Finally compute the motion that has to be warped ---
            deltaPosition = difference;
            currentdeltaPosition = difference * timeToTarget;
            deltaPosition.y = 0.0f;
            currentdeltaPosition.y = 0.0f;

            deltaRotation = matchRotation;

            // --- If close enough to validDistance, end warp ---
            if (math.distance(transform.position, matchPosition) < validDistance)
            {
                deltaPosition = Vector3.zero;
                ret = false;
            }

            return ret;
        }

        public void SetRootMotion(bool rootMotion)
        {
            animator.applyRootMotion = rootMotion;
        }

        // --------------------------------

    }
}
