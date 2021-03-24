using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    public class TraverserTransition 
    {
        // --- Attributes ---
        private TraverserAnimationController animationController;
        private TraverserCharacterController controller;

        // -------------------------------------------------

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

        // --- Indicates how much importance we give to position and rotation warping ---
        MatchTargetWeightMask weightMask;

        // --- Indicates if transition handler is currently playing ---
        public bool isON { get => isTransitionAnimationON || isTargetAnimationON; }

        // --- Basic Methods ---

        private void Initialize()
        {
            // --- Reset everything ---
            isTransitionAnimationON = false;
            isTargetAnimationON = false;
            transitionAnimation = "";
            targetAnimation = "";
            triggerAnimation = "";
        }

        public TraverserTransition(TraverserAnimationController _animationController, ref TraverserCharacterController _controller)
        {
            animationController = _animationController;
            controller = _controller;
            weightMask = new MatchTargetWeightMask(Vector3.one, 1.0f);
        }

        public void StartTransition(string transitionAnim, string targetAnim, string triggerAnim)
        {
            if (!isTransitionAnimationON && !isTargetAnimationON)
            {
                // --- If we are in a transition activate root motion and disable controller ---
                //animationController.SetRootMotion(true);
                controller.ConfigureController(false);
                transitionAnimation = transitionAnim;
                targetAnimation = targetAnim;
                triggerAnimation = triggerAnim;
                animationController.animator.SetTrigger(triggerAnim);
                isTransitionAnimationON = true;
            }
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
                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation))
                    {
                        // --- Get skeleton's current position and teleport controller ---
                        float3 newTransform = animationController.skeleton.transform.position;
                        newTransform.y = animationController.transform.position.y;
                        controller.TeleportTo(newTransform);

                        // --- Reenable controller and give back control ---
                        controller.ConfigureController(true);
                        ret = false;
                    }
                    else
                        ret = true;
                }
                // --- Animator is not in transition and we can take control ---
                else
                {
                    // --- We are using transitionAnimation to reach contact location ---
                    if (isTransitionAnimationON)
                    {
                        //GameObject.Find("dummy2").transform.position = controller.contactTransform.t;
                        //GameObject.Find("dummy2").transform.rotation = controller.contactTransform.q;

                        // --- Use target matching (motion warping) to reach the contact transform as the transitionAnimation plays ---
                        if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(transitionAnimation))
                            isTransitionAnimationON = animationController.WarpToTarget(controller.contactTransform.t, controller.contactTransform.q, AvatarTarget.Root, weightMask, 0.0f, 1.0f, 2.0f);

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
                        Vector3 target = controller.contactTransform.t;
                        target += -controller.contactNormal*(controller.contactSize);

                        Debug.Log(controller.contactSize);
                        GameObject.Find("dummy2").transform.position = target;

                        // --- Use target matching (motion warping) to reach the target transform as the targetAnimation plays ---
                        if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation))
                            isTargetAnimationON = animationController.WarpToTarget(target, controller.contactTransform.q, AvatarTarget.Root, weightMask, 0.0f, 1.0f, controller.contactSize*0.1f);

                        ret = isTargetAnimationON;
                    }
                }

            }

            if (!ret)
                Initialize();

            return ret;
        }

        // -------------------------------------------------
    }
}
