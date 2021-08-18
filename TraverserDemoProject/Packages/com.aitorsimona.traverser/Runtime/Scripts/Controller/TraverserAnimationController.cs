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
                    //transform.position = Vector3.Lerp(transform.position, transform.position + currentdeltaPosition, Time.deltaTime);

                    transform.position = Vector3.Lerp(transform.position, targetDelta, Time.deltaTime);

                    transform.rotation = Quaternion.Slerp(transform.rotation, currentdeltaRotation, Time.deltaTime);

                    // --- Update time and Y warping ---
                    //targetYWarp -= transform.position.y - previousY;
                    targetWarpTime -= Time.deltaTime;
                    //targetWarpTime = Mathf.Max(targetWarpTime, 0.1f);
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

        Vector3 targetDelta;
        Vector3 globalDelta;

        public bool WarpToTarget(Vector3 matchPosition, Quaternion matchRotation, bool warpY = true, bool forceSuccess = false)
        {
            bool ret = true;

            // --- Check whether we are in a transition or target animation ---
            bool loop = animator.GetCurrentAnimatorStateInfo(0).loop;

            // --- If a new warp has been issued, reset start variables ---
            if (previousMatchPosition != matchPosition)
                warpStart = true;

            // --- Compute bodyEndPosition and targetWarpTime once at the beginning of warping ---
            if (/*!loop && */warpStart)
            {
                warpStart = false;
                previousMatchPosition = matchPosition;

                // --- Compute current target animation's final position (root motion) ---
                GetPositionAtTime(1.0f, out bodyEndPosition);
                CreateCurve();
                
                // --- Compute warping time (time left until animation end) ---
                targetWarpTime = animator.GetCurrentAnimatorStateInfo(0).length  // total animation length
                    - (animator.GetCurrentAnimatorStateInfo(0).normalizedTime * animator.GetCurrentAnimatorStateInfo(0).length) // start transition time
                    ; // end transition time ?

                targetWarpTime = Mathf.Max(targetWarpTime, 0.001f);

                //// sample points list
                //float currentTime = 0.25f;
                //int previous = (int)(currentTime);
                //int next = Mathf.Min((int)(currentTime + 1.0f), steps - 1);
                //targetDelta = Vector3.Lerp(skeleton.position, points[next], currentTime - previous / (next - previous)); // can use inverse lerp or create map function

                globalDelta = matchPosition - points[steps - 1];
                Vector3 originalDelta = Vector3.zero/*points[steps - 1] - transform.position*/;
                Vector3 newDelta = matchPosition - transform.position;
                //newDelta /= targetWarpTime;


                for (int it = 0; it < steps; ++it)
                {
                    Vector3 weight = points[Mathf.Min(it + 1, steps - 1)] - points[it];
                    originalDelta.x += Mathf.Abs(weight.x);
                    originalDelta.y += Mathf.Abs(weight.y);
                    originalDelta.z += Mathf.Abs(weight.z);
                }


                //globalDelta /= steps;

                Vector3 previousPos = transform.position;

                for (int it = 0; it < steps; ++it)
                {


                    if(loop)
                        points[it] += globalDelta * (((float)it + 1) / (steps));
                    else
                    {
                        // Original weight
                        Vector3 weight = points[Mathf.Min(it + 1, steps - 1)] - points[it];
                        Vector3 originalWeight = weight;
                        //weight.x /= originalDelta.x;
                        //weight.y /= originalDelta.y;
                        //weight.z /= originalDelta.z;

                        // Now we should compute mapped weight (match - transform.pos)

                        weight.x = Mathf.InverseLerp(0.0f, Mathf.Abs(originalDelta.x), Mathf.Abs(weight.x));
                        weight.y = Mathf.InverseLerp(0.0f, Mathf.Abs(originalDelta.y), Mathf.Abs(weight.y));
                        weight.z = Mathf.InverseLerp(0.0f, Mathf.Abs(originalDelta.z), Mathf.Abs(weight.z));

                        //weight.x = weight.x * newDelta.x / steps;
                       //weight.y = weight.y * newDelta.y / steps;
                        //weight.z = weight.z * newDelta.z / steps;


                        //if (Mathf.Abs(originalDelta.x) > 0.1)
                        //    weight.x /= originalDelta.x;
                        //if (Mathf.Abs(originalDelta.y) > 0.1)
                        //    weight.y /= originalDelta.y;
                        //if (Mathf.Abs(originalDelta.z) > 0.1)
                        //    weight.z /= originalDelta.z;

                        // --- If the original animation does not have motion in one direction, let the warper fully cover that motion ---
                        //if (originalDelta.x < 0.05 && newDelta.x > 0.05)
                        //    weight.x = globalDelta.x / steps;
                        //if (originalDelta.y < 0.05 && newDelta.y > 0.05)
                        //    weight.y = globalDelta.y / steps;
                        //if (originalDelta.z < 0.05 && newDelta.z > 0.05)
                        //    weight.z = globalDelta.z / steps;

                        //if (Mathf.Abs(originalDelta.x) < 0.05 /*&& newDelta.x > 0.05 *//*|| weight.x < 0.05*/)
                        //    weight.x = globalDelta.x / steps;
                        //if (Mathf.Abs(originalDelta.y) < 0.05 /*&& newDelta.y > 0.05*/ /*|| weight.y < 0.05*/)
                        //    weight.y = globalDelta.y / steps;
                        //if (Mathf.Abs(originalDelta.z) < 0.05 /*&& newDelta.z > 0.05*/ /*|| weight.z < 0.05*/)
                        //    weight.z = globalDelta.z / steps;

                        // Now apply correct weight
                        //weight.Scale(newDelta);

                        //globalDelta -= weight;
                        weight.Scale(newDelta);
                        weight.Scale(originalWeight.normalized);

                        points[it] = previousPos + weight;
                        previousPos = points[it];
                    }

        
                }

                // --- Compute difference between root motion's target Y and our target Y ---   
                targetYWarp = matchPosition.y - bodyEndPosition.y;
            }

            // --- Compute delta position to be covered ---
            if (loop)
            {
                Vector3 currentPosition = transform.position;


                // --- Prevent transition animations from warping Y ---
                //matchPosition.y = currentPosition.y;

                // --- A looped animation, one of the transition Animations, no root motion ---
                Vector3 desiredDisplacement = matchPosition - currentPosition;
                Vector3 velocity = desiredDisplacement.normalized * Mathf.Max(controller.targetVelocity.magnitude, 1.0f);
                float time = desiredDisplacement.magnitude / velocity.magnitude;

                currentdeltaPosition = desiredDisplacement / time;
                currentdeltaRotation = Quaternion.SlerpUnclamped(transform.rotation, matchRotation, 1.0f / time);

                // ------------------------

                // sample points list
                float currentTime = Mathf.Min(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f); ;
                int previous = (int)(currentTime * (steps - 1));
                int next = previous + 1/*Mathf.Min((int)(currentTime * steps + 1.0f), steps - 1)*/;
                //targetDelta = Vector3.Lerp(transform.position, points[Mathf.Min(next, steps - 1)], Mathf.InverseLerp(((float)previous) / steps, ((float)next) / steps, currentTime)); // can use inverse lerp or create map function
                targetDelta = points[Mathf.Min(next, steps - 1)];

                // ------------------------

                //GameObject.Find("dummy1").transform.position = matchPosition;

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
                Vector3 currentPosition = transform.position;
                Vector3 validPosition = matchPosition;
                validPosition.y = currentPosition.y;

                // --- A targetAnimation, we want to take profit of the animation's motion ---
                Vector3 desiredDisplacement = validPosition - currentPosition;

                if (warpY)
                {
                    desiredDisplacement.y = targetYWarp;

                    // --- If remaining YWarp is small enough, end warping ---
                    //if (Mathf.Abs(targetYWarp) < warpingValidDistance)
                    //{
                    //    forceSuccess = true;
                    //}

                }
                //else
                //matchPosition.y = currentPosition.y;

                // ------------------------
                //GameObject.Find("dummy1").transform.position = matchPosition;

                // sample points list
                //float currentTime = Mathf.Min(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f); ;
                //int previous = (int)(currentTime * (steps - 1));
                //int next = Mathf.Min(previous + 1, steps - 1)/*Mathf.Min((int)(currentTime * steps + 1.0f), steps - 1)*/;
                //targetDelta = Vector3.Lerp(transform.position, points[next], (currentTime - ((float)previous / steps)) / ((next - previous) / steps)); // can use inverse lerp or create map function

                // sample points list
                float currentTime = Mathf.Min(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
                int previous = (int)(currentTime * (steps - 1));
                int next = previous + 1/*Mathf.Min((int)(currentTime * steps + 1.0f), steps - 1)*/;
                //targetDelta = Vector3.Lerp(transform.position, points[Mathf.Min(next, steps - 1)], Mathf.InverseLerp(((float)previous) / steps, ((float)next) / steps, currentTime)); // can use inverse lerp or create map function
                targetDelta = points[Mathf.Min(next, steps - 1)];
                // ------------------------


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

        Vector3[] points;

        int steps = 10;
        float stepping = 0.1f;

        public void CreateCurve()
        {

            float currentStepping = stepping;
            points = new Vector3[steps];

            // --- We need to store the relative movement --- 
            for(int it = 0; it < steps; ++it)
            {
                GetPositionAtTime(currentStepping, out points[it]);
                //points[it] -= skeletonRef.transform.position;
                currentStepping += stepping;
            }

            

            int a = 3;
        }

        public bool IsTransitionFinished()
        {
            return animator.GetNextAnimatorStateInfo(0).normalizedTime == 0.0f && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f;
        }

        public void GetPositionAtTime(float normalizedTime, out Vector3 position)
        {
            // --- Samples current animation at the given normalized time (0.0f - 1.0f) ---
            // --- Useful to know at what position will an animation end ---

            SetRootMotion(true);
            animator.SetTarget(AvatarTarget.Root, normalizedTime);
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
