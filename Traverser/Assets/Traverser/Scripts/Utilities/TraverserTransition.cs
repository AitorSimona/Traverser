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

        private bool isTransitionON = false;
        private bool isAnimationON = false;
        MatchTargetWeightMask weightMask;

        public bool isON { get => isTransitionON || isAnimationON; }

        // --- Basic Methods ---

        public TraverserTransition(TraverserAnimationController _animationController, ref TraverserCharacterController _controller)
        {
            animationController = _animationController;
            controller = _controller;
            weightMask = new MatchTargetWeightMask(Vector3.one, 1.0f);
        }

        public void StartTransition()
        {
            if (!isTransitionON && !isAnimationON)
            {
                // --- If we are in a transition activate root motion and disable controller ---
                animationController.SetRootMotion(true);
                controller.ConfigureController(true);
                isTransitionON = true;
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
                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName("Vaulting")
                        && animationController.animator.GetNextAnimatorStateInfo(0).IsName("LocomotionON"))
                    {
                        // --- Get skeleton's current position and teleport controller ---
                        float3 newTransform = animationController.skeleton.transform.position;
                        newTransform.y = animationController.transform.position.y;
                        controller.TeleportTo(newTransform);

                        // --- If we are in a transition disable controller ---
                        controller.ConfigureController(isTransitionON);
                        isAnimationON = false;
                    }

                    ret = true;
                    return ret;
                }

                // --- Transition has been activated
                if (isTransitionON)
                {
                    //GameObject.Find("dummy2").transform.position = controller.contactTransform.t;
                    //GameObject.Find("dummy2").transform.rotation = controller.contactTransform.q;

                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName("JogTransition"))
                        isTransitionON = animationController.MatchTarget(controller.contactTransform.t, controller.contactTransform.q, AvatarTarget.Root, weightMask, 0.0f, 1.0f, 2.0f);

                    if (!isTransitionON)
                    {
                        isAnimationON = true;
                        controller.position = animationController.transform.position;
                        animationController.animator.SetTrigger("Vault");
                        ret = true;
                        return ret;
                    }

                    ret = true;
                    return ret;
                }

                isAnimationON = animationController.animator.GetCurrentAnimatorStateInfo(0).IsName("Vaulting");

                //Debug.Log(isAnimationON);

                if (isAnimationON)
                    ret = true;

            }

            return ret;
        }

        // -------------------------------------------------
    }
}
