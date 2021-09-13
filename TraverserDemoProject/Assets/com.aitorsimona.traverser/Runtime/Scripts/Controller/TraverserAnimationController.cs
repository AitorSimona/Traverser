using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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
            public float DirectionX;

            public int MoveID;
            public int SpeedID;
            public int HeadingID;
            public int DirectionXID;
        }

        [Serializable]
        public struct AnimationData
        {
            [Tooltip("The animation state name (Animator state not animation name!).")]
            public string animationStateName;

            [Tooltip("Time in seconds manual animation states transitions will take.")]
            public float transitionDuration;
        }
        
        [Header("Warping")]
        [Tooltip("The distance at which the character target has to be within for warping success.")]
        [Range(0.01f, 0.1f)]
        public float warpingValidDistance = 0.1f;

        [Header("Runtime rig")]
        public Rig spineRig;
        public Rig legsRig;
        public Rig armsRig;
        public GameObject hipsRigEffector;
        public GameObject spineRigEffector;
        public GameObject leftLegRigEffector;
        public GameObject rightLegRigEffector;
        public GameObject aimRigEffector;
        public Transform hipsRef;

        [Header("Debug")]
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Select the object to show debug geometry.")]
        public bool debugDraw = false;

        [Tooltip("The radius of the spheres used to debug draw contact and target transforms.")]
        [Range(0.1f, 1.0f)]
        public float contactDebugSphereRadius = 0.5f;

        // --- Animator and animation transition handler ---
        [HideInInspector]
        public Animator animator;
        public TraverserTransition transition;

        // --- IK properties ---
        public Vector3 skeletonPos { get => skeletonPosition; }
        public Vector3 leftFootPos { get => leftFootPosition; }
        public Vector3 rightFootPos { get => rightFootPosition; }
        public Vector3 leftHandPos { get => leftHandPosition; }
        public Vector3 rightHandPos { get => rightHandPosition; }

        // --- Runtime rig effectors transforms ---
        public TraverserTransform hipsEffectorOGTransform { get => hipsEffectorOriginalTransform; }
        public TraverserTransform spineEffectorOGTransform { get => spineEffectorOriginalTransform; }
        public TraverserTransform leftLegEffectorOGTransform { get => leftLegEffectorOriginalTransform; }
        public TraverserTransform rightLegEffectorOGTransform { get => rightLegEffectorOriginalTransform; }
        public TraverserTransform aimEffectorOGTransform { get => aimEffectorOriginalTransform; }

        // --------------------------------

        // --- Private Variables ---

        private TraverserCharacterController controller;

        // --- Motion that has to be warped in the current frame given timeToTarget, pre deltaTime ---
        private Vector3 currentdeltaPosition = Vector3.zero;

        // --- Rotation that has to be warped in the current frame given timeToTarget, pre deltaTime ---
        private Quaternion currentdeltaRotation;

        // --- If we have less than this time to reach the next point, teleport. Keep it small to avoid hiccups ---
        private float warperMinimumTime = 0.025f;

        // --- Used to store the bone's positions/rotations for use in the next frame ---
        private Vector3 skeletonPosition = Vector3.zero;
        private Vector3 leftFootPosition = Vector3.zero;
        private Vector3 rightFootPosition = Vector3.zero;
        private Vector3 leftHandPosition = Vector3.zero;
        private Vector3 rightHandPosition = Vector3.zero;

        // --- Runtime rig ---
        // --- Store runtime rig effector transforms ---
        private TraverserTransform hipsEffectorOriginalTransform;
        private TraverserTransform spineEffectorOriginalTransform;
        private TraverserTransform leftLegEffectorOriginalTransform;
        private TraverserTransform rightLegEffectorOriginalTransform;
        private TraverserTransform aimEffectorOriginalTransform;

        // --- Curve related private variables ---
        private Vector3[] points;
        private Vector3[] warpedPoints;
        private int steps = 11;
        private float stepping = 0.1f;
        private int currentPoint = 0;

        // --------------------------------

        private void Awake()
        {
            points = new Vector3[steps];
            warpedPoints = new Vector3[steps];
        }

        private void Start()
        {
            controller = GetComponent<TraverserCharacterController>();
            animator = GetComponent<Animator>();
            transition = new TraverserTransition(this, ref controller);

            animatorParameters = new Dictionary<string, int>();

            hipsEffectorOriginalTransform = TraverserTransform.Get(hipsRigEffector.transform.localPosition, hipsRigEffector.transform.localRotation);
            spineEffectorOriginalTransform = TraverserTransform.Get(spineRigEffector.transform.localPosition, spineRigEffector.transform.localRotation);
            leftLegEffectorOriginalTransform = TraverserTransform.Get(leftLegRigEffector.transform.localPosition, leftLegRigEffector.transform.localRotation);
            rightLegEffectorOriginalTransform = TraverserTransform.Get(rightLegRigEffector.transform.localPosition, rightLegRigEffector.transform.localRotation);
            aimEffectorOriginalTransform = TraverserTransform.Get(aimRigEffector.transform.localPosition, aimRigEffector.transform.localRotation);

            // --- Save all animator parameter hashes for future reference ---
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                animatorParameters.Add(param.name, param.nameHash);
            }
        }

        public void InitializeAnimatorParameters(ref AnimatorParameters parameters)
        {
            parameters.Move = false;
            parameters.Speed = 0.0f;
            parameters.Heading = 0.0f;
            parameters.DirectionX = 0.0f;
            parameters.MoveID = Animator.StringToHash("Move");
            parameters.SpeedID = Animator.StringToHash("Speed");
            parameters.HeadingID = Animator.StringToHash("Heading");
            parameters.DirectionXID = Animator.StringToHash("DirectionX");
        }

        public void UpdateAnimator(ref AnimatorParameters parameters)
        {
            // --- Update animator with the given parameter's values ---
            animator.SetBool(parameters.MoveID, parameters.Move);
            animator.SetFloat(parameters.SpeedID, parameters.Speed);
            animator.SetFloat(parameters.HeadingID, parameters.Heading);
            animator.SetFloat(parameters.DirectionXID, parameters.DirectionX);
        }

        public bool WarpToTarget(Vector3 matchPosition, Quaternion matchRotation, bool forceSuccess = false)
        {
            bool ret = true;

            // --- Compute delta position to be covered ---
            if (!transition.isTargetON)
            {
                Vector3 currentPosition = skeletonPosition;
                // --- Prevent transition animations from warping Y ---
                matchPosition.y = currentPosition.y;

                // --- A looped animation, one of the transition Animations, no root motion, use current velocity ---
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
                // --- Retrieve target animation's current normalized time ---
                float currentTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                // --- We might be transitioning from transition animation to target, if so get normalized time from next state (target) ---
                if (animator.IsInTransition(0) && !animator.GetCurrentAnimatorStateInfo(0).IsName(transition.targetAnimName))
                    currentTime = animator.GetNextAnimatorStateInfo(0).normalizedTime;

                // --- If we have already left target anim, force teleport ---
                else if (!animator.GetCurrentAnimatorStateInfo(0).IsName(transition.targetAnimName))
                    currentTime = 1.0f;

                // --- Update curve to a possibly changed destination ---
                OffsetCurve(matchPosition);
                matchPosition = warpedPoints[steps - 1];

                // --- If we are close enough to the next warp point, aim for the next one ---
                if (Vector3.Magnitude(warpedPoints[currentPoint] - skeletonPosition) < warpingValidDistance)
                {
                    currentPoint++;
                    currentPoint = Mathf.Clamp(currentPoint, 0, steps - 1);
                }

                // --- Compute required displacement to next point ---
                Vector3 nextPoint = warpedPoints[currentPoint];
                Vector3 desiredDisplacement = nextPoint - skeletonPosition;

                // --- Compute how much time we have to cover the desired displacement ---
                float expectedTime = (float)(currentPoint + 1) / steps;
                float timeAvailable = expectedTime - currentTime;

                // --- If we do not have enough time, teleport ourselves to next point, else update current delta ---
                if (timeAvailable <= warperMinimumTime)
                {
                    transform.position = nextPoint + (transform.position - skeletonPosition);
                    currentdeltaPosition = Vector3.zero;
                }
                else
                    currentdeltaPosition = desiredDisplacement / timeAvailable;

                // --- If close enough to match position, end warping ---
                if ((Vector3.Magnitude(matchPosition - skeletonPosition) < warpingValidDistance
                    && currentdeltaPosition.magnitude < warpingValidDistance)
                    || forceSuccess)
                {
                    currentdeltaPosition = Vector3.zero;
                    transform.rotation = matchRotation; // force final rotation
                    currentdeltaRotation = transform.rotation;
                    currentPoint = 0;
                    ret = false;
                }
            }

            return ret;
        }

        public void CreateCurve(Vector3 targetPosition, float initialStepping)
        {
            float currentStepping = initialStepping;

            Vector3 originalDisplacement = Vector3.zero;

            // --- We need to store the relative movement --- 
            for (int it = 0; it < steps; ++it)
            {
                GetPositionAtTime(currentStepping, out points[it], AvatarTarget.Body);

                if (it > 0)
                {
                    originalDisplacement.x += Mathf.Abs(points[it].x - points[it - 1].x);
                    originalDisplacement.y += Mathf.Abs(points[it].y - points[it - 1].y);
                    originalDisplacement.z += Mathf.Abs(points[it].z - points[it - 1].z);
                }

                currentStepping += stepping;
            }

            Vector3 previousdiff = points[1] - points[0];
            Vector3 backupDiff;

            for (int it = 1; it < steps - 1; ++it)
            {
                backupDiff = points[it + 1] - points[it];
                points[it] = points[it - 1] + previousdiff;
                previousdiff = backupDiff;
            }

            points[steps - 1] = points[steps - 2] + previousdiff;

            // --- Warp motion ---
            Vector3 warpTotalDisplacement = targetPosition - points[steps - 1];

            bool linearWarpX = false;
            bool linearWarpY = false;
            bool linearWarpZ = false;

            if (Mathf.Approximately(originalDisplacement.x, 0.0f))
                linearWarpX = true;
            if (Mathf.Approximately(originalDisplacement.y, 0.0f))
                linearWarpY = true;
            if (Mathf.Approximately(originalDisplacement.z, 0.0f))
                linearWarpZ = true;

            currentStepping = initialStepping;
            Vector3 accumulatedWeight = Vector3.zero;

            for (int it = 0; it < steps - 1; ++it)
            {

                Vector3 weight = points[it + 1] - points[it];
                weight.x = Mathf.Abs(weight.x);
                weight.y = Mathf.Abs(weight.y);
                weight.z = Mathf.Abs(weight.z);


                if (!linearWarpX)
                    weight.x /= originalDisplacement.x;
                else
                {
                    weight.x = warpTotalDisplacement.x * currentStepping;
                }

                if (!linearWarpY)
                    weight.y /= originalDisplacement.y;
                else
                {
                    weight.y = warpTotalDisplacement.y * currentStepping;
                }

                // --- Apply weighted warp ---
                if (!linearWarpZ)
                    weight.z /= originalDisplacement.z;
                else
                {
                    // --- Apply linear warp ---
                    weight.z = warpTotalDisplacement.z * currentStepping;
                }

                weight.Scale(warpTotalDisplacement);
                accumulatedWeight += weight;
                warpedPoints[it] = points[it] + accumulatedWeight;

                // linear warping, really good for straight transitions, bad for others
                //warpedPoints[it] = points[it] + (warpTotalDisplacement * currentStepping);

                currentStepping += stepping;
            }

            warpedPoints[steps - 1] = targetPosition;
        }

        public void OffsetCurve(Vector3 matchPosition)
        {
            Vector3 displacement = matchPosition - warpedPoints[steps - 1];
            float currentStepping = currentPoint * stepping;

            for (int it = currentPoint; it < steps; ++it)
            {
                warpedPoints[it] += displacement * (currentPoint * stepping);
                currentStepping += stepping;

            }
        }

        // --------------------------------

        // --- Utility Methods ---

        public void GetPositionAtTime(float normalizedTime, out Vector3 position, AvatarTarget target)
        {
            // --- Samples current animation at the given normalized time (0.0f - 1.0f) ---
            // --- Useful to know at what position will an animation end ---

            animator.applyRootMotion = true;
            animator.SetTarget(target, normalizedTime);
            animator.Update(0);
            position = animator.targetPosition;
            animator.applyRootMotion = false;
        }

        // --------------------------------

        // --- Events ---

        private void OnAnimatorMove()
        {
            if (transition.isON)
            {
                // --- Apply warping ---
                if (transition.isWarping)
                {
                    // --- Apply deltas ---
                    transform.position = Vector3.Lerp(transform.position, transform.position + currentdeltaPosition, Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, currentdeltaRotation, Time.deltaTime);
                }

                controller.targetHeading = 0.0f;
                controller.targetDisplacement = Vector3.zero;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            // --- Store bone positions to use in next frame ---
            skeletonPosition = animator.GetBoneTransform(HumanBodyBones.Hips).position;
            rightFootPosition = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            leftFootPosition = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            rightHandPosition = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            leftHandPosition = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
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
