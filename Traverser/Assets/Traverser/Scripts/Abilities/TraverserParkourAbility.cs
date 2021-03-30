using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]
    [RequireComponent(typeof(TraverserLocomotionAbility))]

    public class TraverserParkourAbility : MonoBehaviour, TraverserAbility
    {
        // --- Attributes ---
        [Header("Transition settings")]
        [Tooltip("Distance in meters for performing movement validity checks.")]
        [Range(0.0f, 1.0f)]
        public float contactThreshold;

        [Tooltip("Maximum linear error for transition poses.")]
        [Range(0.0f, 1.0f)]
        public float maximumLinearError;

        [Tooltip("Maximum angular error for transition poses.")]
        [Range(0.0f, 180.0f)]
        public float maximumAngularError;

        // -------------------------------------------------

        // --- Private Variables ---

        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserLocomotionAbility locomotionAbility;

        // -------------------------------------------------

        // --- Basic Methods ---

        public void OnEnable()
        {
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserInputLayer.capture.UpdateParkour();
            return animationController.transition.UpdateTransition() ? this : null;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            if (animationController.transition.isON)
                return this;

            return null;
        }

        public TraverserAbility OnPostUpdate(float deltaTime)
        {
            //if (animationController.transition.isON && !animationController.animator.applyRootMotion)
              //  animationController.SetRootMotion(true);

            return null;
        }

        public bool OnContact(TraverserAffineTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            TraverserInputLayer.capture.UpdateParkour();

            if (TraverserInputLayer.capture.parkourButton && !animationController.transition.isON)
            {
                ref Collider collider = ref controller.current.collider;

                TraverserParkourObject parkourObject = collider.GetComponent<TraverserParkourObject>();

                if(parkourObject != null)
                    ret = RequestTransition(ref parkourObject, false);
            }

            return ret;
        }

        bool RequestTransition(ref TraverserParkourObject parkourObject, bool isDrop) 
        {
            bool ret = false;

            if (parkourObject)
            {
                // --- Get speed from controller ---
                float speed = math.length(controller.targetVelocity);
                float walkSpeed = 1.5f; // TODO: Create walk speed

                // --- Compute target transform at the other side of the collider ---
                Vector3 target = controller.contactTransform.t;
                target += -controller.contactNormal * controller.contactSize;
                TraverserAffineTransform targetTransform = TraverserAffineTransform.Create(target, controller.contactTransform.q);

                // --- Decide which transition to play depending on object type ---
                switch (parkourObject.type)
                {
                    case TraverserParkourObject.TraverserParkourType.Wall:

                        // --- Compute direction of animation depending on wall's normal ---
                        Vector3 contactRight = Vector3.Cross(controller.contactNormal, parkourObject.transform.up);
                        bool right = Vector3.Dot(transform.forward, contactRight) > 0.0;

                        // --- Target should be contact + animationDirection*Speed ---
                        target = controller.contactTransform.t;

                        if (right)
                        {
                            target += contactRight * 1.0f;
                            targetTransform.q *= Quaternion.Euler(0.0f, 90.0f, 0.0f);
                        }
                        else
                        {
                            target -= contactRight * 1.0f;
                            targetTransform.q *= Quaternion.Euler(0.0f, -90.0f, 0.0f);
                        }

                        targetTransform.t = target;

                        // --- Check if we are jogging or running and play appropriate transition ---
                        if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= walkSpeed)
                        {
                            if(right)
                                ret = animationController.transition.StartTransition("JogTransition", "WallRunRight", "JogTransitionTrigger", "WallRunRightTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("JogTransition", "WallRunLeft", "JogTransitionTrigger", "WallRunLeftTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }
                        else if (speed > locomotionAbility.movementSpeedSlow + 0.1)
                        {
                            if (right)
                                ret = animationController.transition.StartTransition("RunTransition", "WallRunRight", "RunTransitionTrigger", "WallRunRightTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("RunTransition", "WallRunLeft", "RunTransitionTrigger", "WallRunLeftTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        }

                        break;
                    case TraverserParkourObject.TraverserParkourType.Table:

                        // --- Check if we are jogging or running and play appropriate transition ---
                        if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= walkSpeed)
                            ret = animationController.transition.StartTransition("JogTransition", "VaultTableJog", "JogTransitionTrigger", "TableTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        else if(speed > locomotionAbility.movementSpeedSlow + 0.1)
                            ret = animationController.transition.StartTransition("RunTransition", "VaultTableRun", "RunTransitionTrigger", "TableTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);

                        break;
                    case TraverserParkourObject.TraverserParkourType.Platform:

                        // --- If we are dropping our target should be the locomotion simulation's last iteration position ---
                        target = controller.current.position;

                        // --- If we are climbing instead, our target should be on the platform but a bit displaced forward ---
                        if (!isDrop)
                        {
                            target = controller.contactTransform.t;
                            target += -controller.contactNormal * 0.5f;
                        }
                        else
                            targetTransform.q = transform.rotation;

                        targetTransform.t = target;

                        // --- Check if we are walking, jogging or running and play appropriate transition ---
                        if (speed <= walkSpeed)
                        {
                            if(!isDrop)
                                ret = animationController.transition.StartTransition("WalkTransition", "ClimbPlatformWalk", "WalkTransitionTrigger", "PlatformClimbTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("WalkTransition", "DropPlatformWalk", "WalkTransitionTrigger", "PlatformDropTrigger", 1.5f, 0.5f, ref targetTransform, ref targetTransform);
                        }
                        else if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= walkSpeed)
                        {
                            if (!isDrop)
                                ret = animationController.transition.StartTransition("JogTransition", "ClimbPlatformJog", "JogTransitionTrigger", "PlatformClimbTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("JogTransition", "DropPlatformJog", "JogTransitionTrigger", "PlatformDropTrigger", 1.5f, 0.5f, ref targetTransform, ref targetTransform);
                        }
                        else
                        {
                            if (!isDrop)
                                ret = animationController.transition.StartTransition("RunTransition", "ClimbPlatformRun", "RunTransitionTrigger", "PlatformClimbTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                            else
                                ret = animationController.transition.StartTransition("RunTransition", "DropPlatformRun", "RunTransitionTrigger", "PlatformDropTrigger", 1.5f, 0.5f, ref targetTransform, ref targetTransform);
                        }

                        break;
                    case TraverserParkourObject.TraverserParkourType.Ledge:

                        // --- Check if we are walking, jogging or running and play appropriate transition ---
                        if (speed <= walkSpeed)
                            ret = animationController.transition.StartTransition("WalkTransition", "VaultLedgeWalk", "WalkTransitionTrigger", "LedgeTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        else if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= walkSpeed)
                            ret = animationController.transition.StartTransition("JogTransition", "VaultLedgeJog", "JogTransitionTrigger", "LedgeTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        else
                            ret = animationController.transition.StartTransition("RunTransition", "VaultLedgeRun", "RunTransitionTrigger", "LedgeTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);

                        break;
                    case TraverserParkourObject.TraverserParkourType.Tunnel:

                        // --- Check if we are jogging or running and play appropriate transition ---
                        if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= walkSpeed) 
                            ret = animationController.transition.StartTransition("JogTransition", "SlideTunnelJog", "JogTransitionTrigger", "TunnelTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
                        else if (speed > locomotionAbility.movementSpeedSlow + 0.1)
                            ret = animationController.transition.StartTransition("RunTransition", "SlideTunnelRun", "RunTransitionTrigger", "TunnelTrigger", 2.0f, 0.5f, ref controller.contactTransform, ref targetTransform);

                        break;
                    default:
                        break;
                }
            }

            return ret;
        }

        public bool OnDrop(float deltaTime)
        {
            bool ret = false;

            TraverserInputLayer.capture.UpdateParkour();

            if (TraverserInputLayer.capture.parkourDropDownButton 
                && controller.previous.isGrounded 
                && controller.previous.ground != null
                && !animationController.transition.isON)
            {
                ref Collider collider = ref controller.current.ground;

                if (collider != null)
                {
                    TraverserParkourObject parkourObject = controller.previous.ground.GetComponent<TraverserParkourObject>();
                    ret = RequestTransition(ref parkourObject, true);
                }             
            }

            return ret;
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        // -------------------------------------------------
    }
}

