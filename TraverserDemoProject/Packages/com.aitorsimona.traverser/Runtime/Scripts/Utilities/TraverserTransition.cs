using UnityEngine;

namespace Traverser
{
    public class TraverserTransition 
    {
        // --- Attributes ---
        private TraverserAnimationController animationController;
        private TraverserCharacterController controller;

        // --------------------------------

        // --- Indicates if transition handler is currently playing ---
        public bool isON { get => isTransitionAnimationON || isTargetAnimationON; }

        // --- Indicates if transition handler is currently warping ---
        public bool isWarping { get => isWarpOn; }

        // --- Private Variables ---

        // --- Indicates if we are playing the specified transitionAnimation ---
        private bool isTransitionAnimationON = false;

        // --- Indicates if we are playing the specified targetAnimation ---
        private bool isTargetAnimationON = false;

        // --- The desired transition animation to play to reach the desired location ---
        private string transitionAnimation;

        // --- The desired animation to play on reaching the desired location ---
        private string targetAnimation;

        // --- The desired animator trigger to activate for transition animations ---
        private string triggerTransitionAnim;

        // --- The desired animator trigger to activate for target animations ---
        private string triggerTargetAnim;

        // --- Transform in space we have to reach playing transitionAnimation ---
        private TraverserTransform contactTransform;

        // --- Transform in space we have to reach playing targetAnimation ---
        private TraverserTransform targetTransform;

        // --- Indicates if transition handler is currently warping ---
        private bool isWarpOn = false;

        // --- Whether or not the warper will cover Y distances ---
        private bool warpY = true;

        private bool forceTargetAnimation = false;

        // --------------------------------

        // --- Basic Methods ---

        private void Initialize()
        {
            // --- Reset everything --- 
            isWarpOn = false;
            isTransitionAnimationON = false;
            isTargetAnimationON = false;
            transitionAnimation = "";
            animationController.animator.ResetTrigger(triggerTransitionAnim);
            animationController.animator.ResetTrigger(triggerTargetAnim);
            targetAnimation = "";
            triggerTargetAnim = "";
            animationController.SetRootMotion(false);
            forceTargetAnimation = false;
    
        }

        public TraverserTransition(TraverserAnimationController _animationController, ref TraverserCharacterController _controller)
        {
            animationController = _animationController;
            controller = _controller;
        }

        // --------------------------------

        // --- Utility Methods ---

        public bool StartTransition(string transitionAnim, string targetAnim, string triggerTransitionAnim, string triggerTargetAnim,
            ref TraverserTransform contactTransform, ref TraverserTransform targetTransform, bool warpY = true, bool forceTarget = false)
        {
            // --- TransitionAnim and targetAnim must exists as states in the animator ---
            // --- TriggerAnim must exist as trigger in transitions between current state to transitionAnim to targetAnim ---

            if(!animationController.animator.HasState(0, Animator.StringToHash(transitionAnim))
                || !animationController.animator.HasState(0, Animator.StringToHash(targetAnim))
                || !animationController.animatorParameters.ContainsKey(triggerTransitionAnim)
                || !animationController.animatorParameters.ContainsKey(triggerTargetAnim))
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
                transitionAnimation = transitionAnim;
                targetAnimation = targetAnim;
                this.triggerTargetAnim = triggerTargetAnim;
                this.triggerTransitionAnim = triggerTransitionAnim;

                animationController.animator.SetTrigger(triggerTransitionAnim);

                isTransitionAnimationON = true;
                this.targetTransform = targetTransform;
                this.contactTransform = contactTransform;
                this.warpY = warpY;
                this.forceTargetAnimation = forceTarget;

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
                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation)
                        && isTargetAnimationON)
                    {
                        // TODO: We may end up here before warping is complete, one mistake may be not using the exit transition time's
                        // in warping computation
                        animationController.ResetWarper();

                        // --- Get skeleton's current position and teleport controller ---
                        Vector3 newTransform = animationController.skeleton.transform.position;
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
                        if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(transitionAnimation))
                            isTransitionAnimationON = animationController.WarpToTarget(contactTransform.t, contactTransform.q, true, forceTargetAnimation);

                        // --- When we reach the contact point, activate targetAnimation ---
                        if (!isTransitionAnimationON)
                        {
                            
                            isTransitionAnimationON = false;
                            isTargetAnimationON = true;
                            isWarpOn = true;
                            controller.TeleportTo(animationController.transform.position);
                            animationController.animator.SetTrigger(triggerTargetAnim);
                        }

                        ret = true;
                    }
                    // --- We have activated targetAnimation and must keep transition on until end of play ---
                    else if (isTargetAnimationON)
                    {
                        // --- Use motion warping to reach the target transform as the targetAnimation plays ---
                        if (isWarpOn && animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation))
                            isWarpOn = animationController.WarpToTarget(targetTransform.t, targetTransform.q, warpY);

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

        // --------------------------------
    }
}
