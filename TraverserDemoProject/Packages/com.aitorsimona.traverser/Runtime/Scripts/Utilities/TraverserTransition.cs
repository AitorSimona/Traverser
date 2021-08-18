using System;
using UnityEngine;

namespace Traverser
{
    public class TraverserTransition 
    {
        [Serializable]
        public struct TraverserTransitionData
        {
            [Tooltip("The name of the animator state to play in order to reach the contactTransform.")]
            public string transitionAnim;

            [Tooltip("The name of the animator state to play in order to reach the targetTransform after reaching the contactTransform.")]
            public string targetAnim;

            [Tooltip("The name of the aniamtor trigger to activate in order to play the transitionAnim.")]
            public string triggerTransitionAnim;

            [Tooltip("The name of the aniamtor trigger to activate in order to play the targetAnim.")]
            public string triggerTargetAnim;

            [Tooltip("The multiplier to apply to a direction specified by the user that offsets the contactTransform. Useful if you want, for example, to start the target animation before reaching the contact point.")]
            public float contactOffset;

            [Tooltip("The multiplier to apply to a direction specified by the user that offsets the targetTransform. Useful if you want, for example, to end the target animation further from the original target.")]
            public float targetOffset;

            [Tooltip("Indicates if the motion warper should apply warping in Y direction. Use if you want to cover any height. The warper will cover X and Z by default.")]
            public bool warpY;

            [Tooltip("Use if you want to force the target animation, thus skipping warp to contactTransform. Use if you only need a single animation transition.")]
            public bool forceTarget;
        }

        // --- Attributes ---

        // --- Indicates if transition handler is currently playing ---
        public bool isON { get => isTransitionAnimationON || isTargetAnimationON; }

        // --- Indicates if transition handler is currently warping ---
        public bool isWarping { get => isWarpOn; }

        // --------------------------------

        // --- Private Variables ---

        private TraverserAnimationController animationController;
        private TraverserCharacterController controller;

        // --- Indicates if we are playing the specified transitionAnimation ---
        private bool isTransitionAnimationON = false;

        // --- Indicates if we are playing the specified targetAnimation ---
        private bool isTargetAnimationON = false;

        // --- Indicates if transition handler is currently warping ---
        private bool isWarpOn = false;

        // --- Contains current transition information ---
        private TraverserTransitionData transitionData;

        // --- Transform in space we have to reach playing transitionAnimation ---
        private TraverserTransform contactTransform;

        // --- Transform in space we have to reach playing targetAnimation ---
        private TraverserTransform targetTransform;

        // --------------------------------

        // --- Basic Methods ---

        private void Initialize()
        {
            // --- Reset everything --- 
            isWarpOn = false;
            isTransitionAnimationON = false;
            isTargetAnimationON = false;
            transitionData.transitionAnim = "";
            transitionData.targetAnim = "";
            animationController.animator.ResetTrigger(transitionData.triggerTransitionAnim);
            animationController.animator.ResetTrigger(transitionData.triggerTargetAnim);
            transitionData.triggerTransitionAnim = "";
            transitionData.triggerTargetAnim = "";
            animationController.SetRootMotion(false);
            transitionData.forceTarget = false;
   
        }

        public TraverserTransition(TraverserAnimationController _animationController, ref TraverserCharacterController _controller)
        {
            animationController = _animationController;
            controller = _controller;
        }

        // --------------------------------

        // --- Utility Methods ---

        public bool StartTransition(ref TraverserTransitionData transitionDataset ,ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            // --- TransitionAnim and targetAnim must exists as states in the animator ---
            // --- TriggerAnim must exist as trigger in transitions between current state to transitionAnim to targetAnim ---

            if(!animationController.animator.HasState(0, Animator.StringToHash(transitionDataset.transitionAnim))
                || !animationController.animator.HasState(0, Animator.StringToHash(transitionDataset.targetAnim))
                || !animationController.animatorParameters.ContainsKey(transitionDataset.triggerTransitionAnim)
                || !animationController.animatorParameters.ContainsKey(transitionDataset.triggerTargetAnim))
            {
                Debug.LogWarning("TraverserTransition - StartTransition - Could not find one or more animation states/parameters in the given animator.");
                return false;
            }
            else if (!isTransitionAnimationON && !isTargetAnimationON && !animationController.animator.IsInTransition(0))
            {
                // --- If we are in a transition activate root motion and disable controller ---
                //animationController.SetRootMotion(true);
                controller.ConfigureController(false);
                isWarpOn = true;
                transitionData.transitionAnim = transitionDataset.transitionAnim;
                transitionData.targetAnim = transitionDataset.targetAnim;
                transitionData.triggerTargetAnim = transitionDataset.triggerTargetAnim;
                transitionData.triggerTransitionAnim = transitionDataset.triggerTransitionAnim;
                animationController.animator.SetTrigger(transitionDataset.triggerTransitionAnim);

                isTransitionAnimationON = true;
                this.targetTransform = targetTransform;
                this.contactTransform = contactTransform;
                transitionData.warpY = transitionDataset.warpY;
                transitionData.forceTarget = transitionDataset.forceTarget;

                return true;
            }

            return false;
        }

        public bool UpdateTransition()
        {
            bool ret = false;

            if (animationController != null)
            {

                // --- Animator is transitioning from one animation to another ---
                if (animationController.animator.IsInTransition(0))
                {
                    // --- Target animation has finished playing, we can give control back to the controller ---
                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(transitionData.targetAnim)
                        && isTargetAnimationON)
                    {
                        // TODO: We may end up here before warping is complete, one mistake may be not using the exit transition time's
                        // in warping computation
                        animationController.ResetWarper();

                        // --- Get skeleton's current position and teleport controller ---
                        Vector3 newTransform = targetTransform.t/*animationController.skeleton.transform.position*/;
                        newTransform.y -= controller.capsuleHeight/2.0f;
                        controller.TeleportTo(newTransform);

                        // --- Reenable controller and give back control ---
                        controller.ConfigureController(true);
                        ret = false;
                    }
                    else
                    {
                        if (isWarpOn && !isTargetAnimationON)                               
                            animationController.WarpToTarget(contactTransform.t, contactTransform.q);
                        //else if (isWarpOn)
                        //    animationController.WarpToTarget(targetTransform.t, targetTransform.q, targetValidDistance);

                        ret = true;
                    }
                }
                // --- Animator is not in transition and we can take control ---
                else
                {
                    // --- We are using transitionAnimation to reach contact location ---
                    if (isTransitionAnimationON)
                    {
                        // --- Use target matching (motion warping) to reach the contact transform as the transitionAnimation plays ---
                        if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(transitionData.transitionAnim))
                            isTransitionAnimationON = animationController.WarpToTarget(contactTransform.t, contactTransform.q, true, transitionData.forceTarget);

                        // --- When we reach the contact point, activate targetAnimation ---
                        if (!isTransitionAnimationON)
                        {
                            
                            isTransitionAnimationON = false;
                            isTargetAnimationON = true;
                            isWarpOn = true;
                            controller.TeleportTo(animationController.transform.position);
                            animationController.animator.SetTrigger(transitionData.triggerTargetAnim);
                        }

                        ret = true;
                    }
                    // --- We have activated targetAnimation and must keep transition on until end of play ---
                    else if (isTargetAnimationON)
                    {
                        // --- Use motion warping to reach the target transform as the targetAnimation plays ---
                        if (isWarpOn && animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(transitionData.targetAnim))
                            isWarpOn = animationController.WarpToTarget(targetTransform.t, targetTransform.q, transitionData.warpY);

                        // --- If current state does not have a valid exit transition, return control ---
                        if (!isWarpOn && animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f)
                        {
                            // --- Get skeleton's current position and teleport controller ---
                            Vector3 newTransform = animationController.skeleton.transform.position;
                            newTransform.y -= controller.capsuleHeight / 2.0f;
                            controller.TeleportTo(newTransform);

                            // --- Reenable controller and give back control ---
                            controller.ConfigureController(true);
                            ret = false;
                        }
                        else
                            ret = true;
                    }
                }

            }

            if (!ret)
                Initialize();

            return ret;
        }

        public void DebugDraw(float contactDebugSphereRadius)
        {
            // --- Draw transition contact and target point ---
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(contactTransform.t, contactDebugSphereRadius);
            Gizmos.DrawWireSphere(targetTransform.t, contactDebugSphereRadius);
        }

        // --------------------------------
    }
}
