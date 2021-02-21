using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
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


        public Animator animator;
        //private Quaternion initialRotation;
        //float3 initialPosition = float3.zero;
        //float3 currentPosition = float3.zero;
        // --------------------------------

        // --- Basic Methods ---
        private void Start()
        {
            //animator = GetComponent<Animator>();
            //initialRotation = skeleton.rotation;
        }

        private void LateUpdate()
        {

            if(!GetComponent<TraverserParkourAbility>().isAnimationON)
            {
                skeleton.transform.position = skeletonRef.transform.position;
            }
        // --- Move all the skeleton to the character's position ---
        //animator.SetTarget(AvatarTarget.Root, 0.0f);
        //animator.Update(0);
        //initialPosition = animator.targetPosition;
        //animator.SetTarget(AvatarTarget.Root, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        //animator.Update(0);
        //currentPosition = animator.targetPosition;

                //Debug.Log(currentPosition - initialPosition);


                //skeleton.transform.position = skeletonRef.transform.position;
                //skeleton.parent.transform.position = skeletonRef.transform.position;
                //skeleton.parent.localPosition = Vector3.zero;
                //skeleton.localPosition = skeletonRef.transform.localPosition;



                //skeleton.rotation =  
                //    transform.rotation * Quaternion.AngleAxis(90, Vector3.up)
                //    * Quaternion.AngleAxis(skeleton.localRotation.eulerAngles.x, Vector3.right)
                //    * Quaternion.AngleAxis(skeleton.localRotation.eulerAngles.z, Vector3.forward)
                //    ;

                //skeleton.rotation = transform.rotation * initialRotation;

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

        public bool MatchTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime)
        {
            // --- Adjusts game object's position and rotation to match givern position and rotation in the given time with current animation ---
            bool ret = true;

            if (!animator.isMatchingTarget)
            {
                //float normalizeTime = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1.0f);

                //if (normalizeTime > normalisedEndTime)
                //    ret = false;
                //else
                animator.MatchTarget(matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
            }

            if (math.distancesq(transform.position, matchPosition) < 1.0f)
            {
                animator.InterruptMatchTarget(false);
                //animator.InterruptMatchTarget();
                ret = false;
            }

            //ret = animator.isMatchingTarget;

            return ret;
        }

        public void SetRootMotion(bool rootMotion)
        {
            animator.applyRootMotion = rootMotion;
        }

        // --------------------------------

    }
}
