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
        private bool isTransitionON = false;

        // --- Indicates if we are playing the specified targetAnimation ---
        private bool isAnimationON = false;

        // --- The desired transition animation to play to reach the desired location ---
        private string transitionAnimation;

        // --- The desired animation to play on reaching the desired location ---
        private string targetAnimation;

        // --- The desired animator trigger to activate for transition / target animations ---
        private string triggerAnimation;

        MatchTargetWeightMask weightMask;

        public bool isON { get => isTransitionON || isAnimationON; }

        // --- Basic Methods ---

        private void Initialize()
        {
            isTransitionON = false;
            isAnimationON = false;
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
            if (!isTransitionON && !isAnimationON)
            {
                // --- If we are in a transition activate root motion and disable controller ---
                animationController.SetRootMotion(true);
                controller.ConfigureController(true);
                transitionAnimation = transitionAnim;
                targetAnimation = targetAnim;
                triggerAnimation = triggerAnim;
                animationController.animator.SetTrigger(triggerAnim);
                isTransitionON = true;
            }
        }

        public bool UpdateTransition()
        {
            bool ret = false;

            if (animationController != null)
            {
                //// --- Animator is transitioning from one animation to another ---
                if (animationController.animator.IsInTransition(0))
                {
                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation)
                       /* && animationController.animator.GetNextAnimatorStateInfo(0).IsName("LocomotionON")*/)
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

                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(transitionAnimation))
                        isTransitionON = animationController.MatchTarget(controller.contactTransform.t, controller.contactTransform.q, AvatarTarget.Root, weightMask, 0.0f, 1.0f, 2.0f);

                    if (!isTransitionON)
                    {
                        isAnimationON = true;
                        controller.position = animationController.transform.position;
                        animationController.animator.SetTrigger(triggerAnimation);
                        ret = true;
                        return ret;
                    }

                    ret = true;
                    return ret;
                }

                isAnimationON = animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation);

                //Debug.Log(isAnimationON);

                //if (isAnimationON)
                //{
                //    Vector3 target = controller.contactTransform.t;
                //    target += controller.transform.forward*controller.contactSize;

                //    GameObject.Find("dummy2").transform.position = target;


                //    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(targetAnimation))
                //        isAnimationON = animationController.MatchTarget(target, controller.contactTransform.q, AvatarTarget.Root, weightMask, 0.0f, 1.0f, 2.0f);

                //    if (!isAnimationON)
                //    {
                //        //isAnimationON = true;
                //        controller.position = animationController.transform.position;
                //        //animationController.animator.SetTrigger(triggerAnimation);
                //        ret = true;
                //        return ret;
                //    }

                //    ret = true;
                //    return ret;
                //}

                if (isAnimationON)
                    ret = true;
                

            }

            if (!ret)
                Initialize();

            return ret;
        }

        // -------------------------------------------------
    }
}
