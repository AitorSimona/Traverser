using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    public class TraverserTransition 
    {
        // --- Attributes ---
        private TraverserAnimationController animationController;
        private TraverserCharacterController controller;

        // --------------------------------

        // --- Indicates if we are playing the specified transitionAnimation ---
        private bool isTransitionAnimationON = false;

        // --- Indicates if we are playing the specified targetAnimation ---
        private bool isTargetAnimationON = false;

        // --- The desired transition animation to play to reach the desired location ---
        private string transitionAnimation;

        // --- The desired animation to play on reaching the desired location ---
        private string targetAnimation;

        // --- The desired animator trigger to activate for transition / target animations ---
        private string triggerAnimation;

        // --- Distance at which motion warping will be considered complete, for transitionAnimation warping ---
        private float transitionValidDistance = 0.0f;

        // --- Distance at which motion warping will be considered complete, for targetAnimation warping ---
        private float targetValidDistance = 0.0f;

        // --- Indicates how much importance we give to position and rotation warping ---
        MatchTargetWeightMask weightMask; 

        // --- Transform in space we have to reach playing transitionAnimation ---
        private TraverserAffineTransform contactTransform;

        // --- Transform in space we have to reach playing targetAnimation ---
        private TraverserAffineTransform targetTransform;


        // --- Indicates if transition handler is currently playing ---
        public bool isON { get => isTransitionAnimationON || isTargetAnimationON; }

        // --------------------------------

        // --- Basic Methods ---

        private void Initialize()
        {
            // --- Reset everything ---            
            isTransitionAnimationON = false;
            isTargetAnimationON = false;
            transitionAnimation = "";
            targetAnimation = "";
            triggerAnimation = "";
            animationController.SetRootMotion(false);
        }

        public TraverserTransition(TraverserAnimationController _animationController, ref TraverserCharacterController _controller)
        {
            animationController = _animationController;
            controller = _controller;
            weightMask = new MatchTargetWeightMask(Vector3.one, 1.0f);
        }

        // --------------------------------

        // --- Utility Methods ---

        public bool StartTransition(string transitionAnim, string targetAnim, string triggerTransitionAnim, string triggerTargetAnim, float transitionValidDistance, float targetValidDistance, ref TraverserAffineTransform contactTransform, ref TraverserAffineTransform targetTransform)
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

            if (!isTransitionAnimationON && !isTargetAnimationON && !animationController.animator.IsInTransition(0))
            {
                // --- If we are in a transition activate root motion and disable controller ---
                //animationController.SetRootMotion(true);
                controller.ConfigureController(false);
                transitionAnimation = transitionAnim;
                targetAnimation = targetAnim;
                triggerAnimation = triggerTargetAnim;
                animationController.animator.SetTrigger(triggerTransitionAnim);
                isTransitionAnimationON = true;
                this.transitionValidDistance = transitionValidDistance;
                this.targetValidDistance = targetValidDistance;
                this.targetTransform = targetTransform;
                this.contactTransform = contactTransform;
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
                        // --- Get skeleton's current position and teleport controller ---
                        float3 newTransform = animationController.skeleton.transform.position;
                        newTransform.y = targetTransform.t.y;
                        controller.TeleportTo(newTransform);

                        // --- Reenable controller and give back control ---
                        controller.ConfigureController(true);
                        ret = false;
                    }
                    else
                    {
                        if(!isTargetAnimationON)
                            animationController.WarpToTarget(contactTransform.t, contactTransform.q, AvatarTarget.Root, weightMask, transitionValidDistance);
                        else
                            animationController.WarpToTarget(targetTransform.t, targetTransform.q, AvatarTarget.Root, weightMask, transitionValidDistance);

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
                            isTransitionAnimationON = animationController.WarpToTarget(contactTransform.t, contactTransform.q, AvatarTarget.Root, weightMask, transitionValidDistance);

                        // --- When we reach the contact point, activate targetAnimation ---
                        if (!isTransitionAnimationON)
                        {
                            isTargetAnimationON = true;
                            controller.TeleportTo(animationController.transform.position);
                            animationController.animator.SetTrigger(triggerAnimation);
                        }

                        ret = true;
                    }
                    // --- We have activated targetAnimation and must keep transition on until end of play ---
                    else if (isTargetAnimationON)
                    {
                        // --- Use motion warping to reach the target transform as the targetAnimation plays ---
                        if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation))
                            animationController.WarpToTarget(targetTransform.t, targetTransform.q, AvatarTarget.Root, weightMask, targetValidDistance);

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
