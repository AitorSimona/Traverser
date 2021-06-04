using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]
    [RequireComponent(typeof(TraverserLocomotionAbility))]

    public class TraverserParkourAbility : MonoBehaviour, TraverserAbility
    {
        // --- Attributes ---
        [Header("Feet IK settings")]
        [Tooltip("Activates or deactivates foot IK placement for the climbing ability.")]
        public bool fIKOn = true;
        [Tooltip("The maximum distance of the ray that enables foot IK, the bigger the ray the further we detect the ground.")]
        [Range(0.0f, 5.0f)]
        public float feetIKGroundDistance = 1.0f;
        [Tooltip("The character's foot height (size in Y, meters).")]
        [Range(0.0f, 1.0f)]
        public float footHeight = 1.0f;

        // -------------------------------------------------

        // --- Private Variables ---

        private TraverserAbilityController abilityController;
        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserLocomotionAbility locomotionAbility;

        // --- Ray for feet IK checks ---
        private Ray IKRay;

        // -------------------------------------------------

        // --- Basic Methods ---

        public void Start()
        {
            IKRay = new Ray();
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public void OnInputUpdate()
        {
            TraverserInputLayer.capture.UpdateParkour();
        }

        public TraverserAbility OnUpdate(float deltaTime)
        {
            return animationController.transition.UpdateTransition() ? this : null;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            if (animationController.transition.isON)
                return this;

            return null;
        }

        public bool OnContact(TraverserTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            if (TraverserInputLayer.capture.parkourButton && !animationController.transition.isON)
            {
                ref Collider collider = ref controller.current.collider;

                TraverserParkourObject parkourObject = collider.GetComponent<TraverserParkourObject>();

                if(parkourObject != null)
                    ret = RequestTransition(ref parkourObject, false);
            }

            return ret;
        }

        public bool OnDrop(float deltaTime)
        {
            bool ret = false;

            if (TraverserInputLayer.capture.parkourDropDownButton
                && controller.previous.isGrounded
                && controller.previous.ground != null
                && !animationController.transition.isON)
            {
                ref Collider collider = ref controller.current.ground;

                if (collider != null)
                {
                    TraverserParkourObject parkourObject = controller.previous.ground.GetComponent<TraverserParkourObject>();

                    if (parkourObject != null)
                        ret = RequestTransition(ref parkourObject, true);
                }
            }

            return ret;
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        bool RequestTransition(ref TraverserParkourObject parkourObject, bool isDrop) 
        {
            bool ret = false;

            // --- Contact transform --- 
            TraverserTransform contactTransform = controller.contactTransform;

            // --- Compute target transform at the other side of the collider ---
            Vector3 target = controller.contactTransform.t;
            target += -controller.contactNormal * controller.contactSize;
            TraverserTransform targetTransform = TraverserTransform.Get(target, controller.contactTransform.q);

            // --- Decide which transition to play depending on object type ---
            switch (parkourObject.type)
            {
                case TraverserParkourObject.TraverserParkourType.Wall:
                    ret = HandleWallTransition(ref contactTransform, ref targetTransform);
                    break;
                case TraverserParkourObject.TraverserParkourType.Table:
                    ret = HandleTableTransition(ref contactTransform, ref targetTransform);
                    break;
                case TraverserParkourObject.TraverserParkourType.Platform:
                    ret = HandlePlatformTransition(ref contactTransform, ref targetTransform, isDrop);
                    break;
                case TraverserParkourObject.TraverserParkourType.Ledge:
                    ret = HandleLedgeTransition(ref contactTransform, ref targetTransform);
                    break;
                case TraverserParkourObject.TraverserParkourType.Tunnel:
                    ret = HandleTunnelTransition(ref contactTransform, ref targetTransform);
                    break;

                default:
                    break;
            }       

            return ret;
        }

        private bool HandleWallTransition(ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            bool ret = false;

            // --- Get speed from controller ---
            //float speed = Vector3.Magnitude(controller.targetVelocity);

            // --- NON-WORKING SOLUTION ---


            // --- Compute direction of animation depending on wall's normal ---
            //Vector3 contactRight = Vector3.Cross(controller.contactNormal, parkourObject.transform.up);
            //bool right = Vector3.Dot(transform.forward, contactRight) > 0.0;

            //// --- Target should be contact + animationDirection*Speed ---
            //target = controller.contactTransform.t;

            //if (right)
            //{
            //    target += contactRight * 1.0f;
            //    targetTransform.q *= Quaternion.Euler(0.0f, 90.0f, 0.0f);
            //}
            //else
            //{
            //    target -= contactRight * 1.0f;
            //    targetTransform.q *= Quaternion.Euler(0.0f, -90.0f, 0.0f);
            //}

            //targetTransform.t = target;

            //// --- Check if we are jogging or running and play appropriate transition ---
            //if (speed <= locomotionAbility.movementSpeedSlow + 0.1 && speed >= walkSpeed)
            //{
            //    if(right)
            //        ret = animationController.transition.StartTransition("JogTransition", "WallRunRight", "JogTransitionTrigger", "WallRunRightTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
            //    else
            //        ret = animationController.transition.StartTransition("JogTransition", "WallRunLeft", "JogTransitionTrigger", "WallRunLeftTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
            //}
            //else if (speed > locomotionAbility.movementSpeedSlow + 0.1)
            //{
            //    if (right)
            //        ret = animationController.transition.StartTransition("RunTransition", "WallRunRight", "RunTransitionTrigger", "WallRunRightTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
            //    else
            //        ret = animationController.transition.StartTransition("RunTransition", "WallRunLeft", "RunTransitionTrigger", "WallRunLeftTrigger", 3.0f, 0.5f, ref controller.contactTransform, ref targetTransform);
            //}

            return ret;
        }

        private bool HandleTableTransition(ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            bool ret = false;

            // --- Get speed from controller ---
            float speed = Vector3.Magnitude(controller.targetVelocity);

            // --- Check if we are jogging or running and play appropriate transition ---
            if (speed <= locomotionAbility.jogSpeed + 0.1 && speed >= locomotionAbility.walkSpeed)
            {
                ret = StartTransition("JogTransition", "VaultTableJog", "JogTransitionTrigger",
                    "TableTrigger", 1.0f, 1.0f, ref contactTransform, ref targetTransform);
            }
            else if (speed > locomotionAbility.jogSpeed + 0.1)
            {
                ret = StartTransition("RunTransition", "VaultTableRun", "RunTransitionTrigger",
                    "TableTrigger", 1.5f, 1.0f, ref contactTransform, ref targetTransform);
            }

            return ret;
        }

        private bool HandlePlatformTransition(ref TraverserTransform contactTransform, ref TraverserTransform targetTransform, bool isDrop)
        {
            bool ret = false;

            // --- Get speed from controller ---
            float speed = Vector3.Magnitude(controller.targetVelocity);

            // --- If we are dropping our target should be the locomotion simulation's last iteration position ---
            targetTransform.t = controller.current.position;

            // --- If we are climbing instead, our target should be on the platform but a bit displaced forward ---
            if (!isDrop)
            {
                targetTransform.t = controller.contactTransform.t;
                targetTransform.t.y += (animationController.skeleton.transform.position.y - transform.position.y);
            }
            else
            {
                targetTransform.q = transform.rotation;
                targetTransform.t.y += (animationController.skeleton.transform.position.y - transform.position.y);
            }

            //targetTransform.t = target;

            // --- Check if we are walking, jogging or running and play appropriate transition ---
            if (speed <= locomotionAbility.walkSpeed)
            {
                if (!isDrop)
                {
                    ret = StartTransition("WalkTransition", "ClimbPlatformWalk", "WalkTransitionTrigger",
                        "PlatformClimbTrigger", 1.0f, 0.1f, ref contactTransform, ref targetTransform);
                }
                else
                {
                    contactTransform = targetTransform;
                    ret = StartTransition("WalkTransition", "DropPlatformWalk", "WalkTransitionTrigger",
                        "PlatformDropTrigger", 1.0f, 0.1f, ref contactTransform, ref targetTransform);
                }
            }
            else if (speed <= locomotionAbility.jogSpeed + 0.1 && speed >= locomotionAbility.walkSpeed)
            {
                if (!isDrop)
                {
                    ret = StartTransition("JogTransition", "ClimbPlatformJog", "JogTransitionTrigger",
                        "PlatformClimbTrigger", 1.0f, 0.1f, ref contactTransform, ref targetTransform);
                }
                else
                {
                    contactTransform = targetTransform;
                    ret = StartTransition("JogTransition", "DropPlatformJog", "JogTransitionTrigger",
                        "PlatformDropTrigger", 1.0f, 0.1f, ref contactTransform, ref targetTransform);
                }
            }
            else
            {
                if (!isDrop)
                {
                    ret = StartTransition("RunTransition", "ClimbPlatformRun", "RunTransitionTrigger",
                        "PlatformClimbTrigger", 1.0f, 0.1f, ref contactTransform, ref targetTransform);
                }
                else
                {
                    contactTransform = targetTransform;
                    ret = StartTransition("RunTransition", "DropPlatformRun", "RunTransitionTrigger",
                        "PlatformDropTrigger", 1.0f, 0.1f, ref contactTransform, ref targetTransform);
                }
            }

            return ret;
        }

        private bool HandleLedgeTransition(ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            bool ret = false;

            // --- Get speed from controller ---
            float speed = Vector3.Magnitude(controller.targetVelocity);

            // --- Check if we are walking, jogging or running and play appropriate transition ---
            if (speed <= locomotionAbility.walkSpeed)
            {
                ret = StartTransition("WalkTransition", "VaultLedgeWalk", "WalkTransitionTrigger",
                    "LedgeTrigger", 0.75f, 0.5f, ref contactTransform, ref targetTransform);
            }
            else if (speed <= locomotionAbility.jogSpeed + 0.1 && speed >= locomotionAbility.walkSpeed)
            {
                ret = StartTransition("JogTransition", "VaultLedgeJog", "JogTransitionTrigger",
                    "LedgeTrigger", 1.0f, 0.25f, ref contactTransform, ref targetTransform);
            }
            else
            {
                ret = StartTransition("RunTransition", "VaultLedgeRun", "RunTransitionTrigger",
                    "LedgeTrigger", 1.0f, 0.5f, ref contactTransform, ref targetTransform);
            }

            return ret;
        }

        private bool HandleTunnelTransition(ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            bool ret = false;

            // --- Get speed from controller ---
            float speed = Vector3.Magnitude(controller.targetVelocity);

            // --- End warp point should be at skeleton height ---
            targetTransform.t.y = controller.current.ground.ClosestPoint(targetTransform.t).y
                + (animationController.skeleton.transform.position.y - transform.position.y);

            // --- Check if we are jogging or running and play appropriate transition ---
            if (speed <= locomotionAbility.jogSpeed + 0.1 && speed >= locomotionAbility.walkSpeed)
            {
                ret = StartTransition("JogTransition", "SlideTunnelJog", "JogTransitionTrigger",
                    "TunnelTrigger", 1.75f, 0.5f, ref contactTransform, ref targetTransform);
            }
            else if (speed > locomotionAbility.jogSpeed + 0.1)
            {
                ret = StartTransition("RunTransition", "SlideTunnelRun", "RunTransitionTrigger",
                    "TunnelTrigger", 1.75f, 0.5f, ref contactTransform, ref targetTransform);
            }

            return ret;
        }

        private bool StartTransition(string transitionAnim, string targetAnim, string triggerTransitionAnim, string triggerTargetAnim, 
            float contactOffset, float targetOffset, ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            // --- Apply offsets to warp points ---
            contactTransform.t -= (transform.forward * contactOffset);
            targetTransform.t += (transform.forward * targetOffset);

            return animationController.transition.StartTransition(transitionAnim, targetAnim, triggerTransitionAnim, triggerTargetAnim, 
                ref contactTransform, ref targetTransform);
        }

        // -------------------------------------------------

        // --- Events ---

        private void OnAnimatorIK(int layerIndex)
        {
            if (!abilityController.isCurrent(this))
                return;

            // --- Set weights to 0 and return if IK is off ---
            if (!fIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
                return;
            }

            // --- Else, sample foot weight from the animator's parameters, which are set by the animations themselves through curves ---

            RaycastHit hit;

            // --- Left foot ---
            animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animationController.animator.GetFloat("IKLeftFootWeight"));

            IKRay.origin = animationController.animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up;
            IKRay.direction = Vector3.down;

            if (Physics.Raycast(IKRay, out hit, feetIKGroundDistance, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 footPosition = hit.point;
                footPosition.y += footHeight;
                animationController.animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
            }

            // --- Right foot ---
            animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animationController.animator.GetFloat("IKRightFootWeight"));
           
            IKRay.origin = animationController.animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up;
            IKRay.direction = Vector3.down;

            if (Physics.Raycast(IKRay, out hit, feetIKGroundDistance, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 footPosition = hit.point;
                footPosition.y += footHeight;
                animationController.animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
            }
        }

        // -------------------------------------------------
    }
}

