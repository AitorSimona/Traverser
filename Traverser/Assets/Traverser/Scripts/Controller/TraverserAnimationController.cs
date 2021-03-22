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

        // --------------------------------

        // --- Private Variables ---

        private TraverserCharacterController controller;

        // --------------------------------

        private void Awake()
        {
            deltaPosition = new Vector3(0.0f, 0.0f, 0.0f);
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
                transform.position += deltaPosition * Time.deltaTime;
                deltaPosition = Vector3.zero;
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

        public bool MatchTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime, float validDistance)
        {
            bool ret = true;

            // --- Activate target matching if no matching is being run ---         
            Vector3 difference = matchPosition - transform.position;
            Vector3 direction = difference.normalized;
            //float normalizeTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
            deltaPosition = difference / animLength;
            deltaPosition.y = 0.0f;

            if (!animator.isMatchingTarget)
            {
                //animator.MatchTarget(matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
            }

            // --- If close enough to validDistance, end target matching ---
            if (math.distance(transform.position, matchPosition) < validDistance)
            {
                //animator.InterruptMatchTarget(false);
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
