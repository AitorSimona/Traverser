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

        [Header("Animation")]
        [Tooltip("A reference to the parkour ability's dataset (scriptable object asset).")]
        public TraverserParkourData parkourData;

        //[Header("Feet IK settings")]
        //[Tooltip("Activates or deactivates foot IK placement for the climbing ability.")]
        //public bool fIKOn = true;
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
        //private Ray IKRay;

        // --- Default delta/epsilon used for parkour speed checks ---
        private float epsilon = 0.001f;
        private float maxYDifference = 0.25f;

        // -------------------------------------------------

        // --- Basic Methods ---

        public void Start()
        {
            Debug.Assert(parkourData);
            //IKRay = new Ray();
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();
        }

        // -------------------------------------------------

        // --- Ability class methods ---

        public TraverserAbility OnUpdate(float deltaTime)
        {
            if (animationController.transition.isON)
                animationController.transition.UpdateTransition();

            return this;
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

            if (abilityController.inputController.GetInputButtonSouth() && !animationController.transition.isON)
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

            if (abilityController.inputController.GetInputButtonEast()
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

        public void OnEnter()
        {

        }

        public void OnExit()
        {

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
                case TraverserParkourObject.TraverserParkourType.LedgeToLedge:

                    // --- We don't want to trigger a ledge to ledge transition when below a wall ---
                    if (contactTransform.t.y - transform.position.y < maxYDifference)
                    {
                        ret = HandleLedgeToLedgeTransition(ref contactTransform, ref targetTransform);

                        // --- If we are jumping back to a regular ledge/wall, notify locomotion ---
                        if (parkourObject.gameObject.GetComponent<TraverserClimbingObject>())
                        {
                            controller.groundSnap = false;
                        }
                    }

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
            if (speed <= locomotionAbility.jogSpeed + epsilon && speed >= locomotionAbility.walkSpeed)
                ret = StartTransition(ref parkourData.vaultTableJogTransitionData, ref contactTransform, ref targetTransform);
            else if (speed > locomotionAbility.jogSpeed + epsilon)
                ret = StartTransition(ref parkourData.vaultTableRunTransitionData ,ref contactTransform, ref targetTransform);

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
                targetTransform.t.y += (animationController.GetSkeletonPosition().y - transform.position.y);
            }
            else
            {
                targetTransform.q = transform.rotation;
                targetTransform.t.y += (animationController.GetSkeletonPosition().y - transform.position.y);
            }

            //targetTransform.t = target;

            // --- Check if we are walking, jogging or running and play appropriate transition ---
            if (speed <= locomotionAbility.walkSpeed)
            {
                if (!isDrop)
                    ret = StartTransition(ref parkourData.climbPlatformWalkTransitionData ,ref contactTransform, ref targetTransform);
                else
                {
                    contactTransform = targetTransform;
                    ret = StartTransition(ref parkourData.dropPlatformWalkTransitionData ,ref contactTransform, ref targetTransform);
                }
            }
            else if (speed <= locomotionAbility.jogSpeed + epsilon && speed >= locomotionAbility.walkSpeed)
            {
                if (!isDrop)
                    ret = StartTransition(ref parkourData.climbPlatformJogTransitionData ,ref contactTransform, ref targetTransform);
                else
                {
                    contactTransform = targetTransform;
                    ret = StartTransition(ref parkourData.dropPlatformJogTransitionData ,ref contactTransform, ref targetTransform);
                }
            }
            else
            {
                if (!isDrop)
                    ret = StartTransition(ref parkourData.climbPlatformRunTransitionData ,ref contactTransform, ref targetTransform);
                else
                {
                    contactTransform = targetTransform;
                    ret = StartTransition(ref parkourData.dropPlatformRunTransitionData ,ref contactTransform, ref targetTransform);
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
                ret = StartTransition(ref parkourData.vaultLedgeWalkTransitionData ,ref contactTransform, ref targetTransform);
            else if (speed <= locomotionAbility.jogSpeed + epsilon && speed >= locomotionAbility.walkSpeed)
                ret = StartTransition(ref parkourData.vaultLedgeJogTransitionData ,ref contactTransform, ref targetTransform);
            else
                ret = StartTransition(ref parkourData.vaultLedgeRunTransitionData ,ref contactTransform, ref targetTransform);

            return ret;
        }

        private bool HandleTunnelTransition(ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            bool ret = false;

            // --- Get speed from controller ---
            float speed = Vector3.Magnitude(controller.targetVelocity);

            // --- End warp point should be at skeleton height ---
            targetTransform.t.y = controller.current.ground.ClosestPoint(targetTransform.t).y
                + (animationController.GetSkeletonPosition().y - transform.position.y);

            // --- Check if we are jogging or running and play appropriate transition ---
            if (speed <= locomotionAbility.jogSpeed + epsilon && speed >= locomotionAbility.walkSpeed)
                ret = StartTransition(ref parkourData.slideTunnelJogTransitionData, ref contactTransform, ref targetTransform);
            else if (speed > locomotionAbility.jogSpeed + epsilon)
                ret = StartTransition(ref parkourData.slideTunnelRunTransitionData, ref contactTransform, ref targetTransform);

            return ret;
        }

        private bool HandleLedgeToLedgeTransition(ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            bool ret = false;

            // --- Get speed from controller ---
            //targetTransform.t -= (targetTransform.t - contactTransform.t)*0.5f;
            targetTransform.t = contactTransform.t;
            targetTransform.t.y += controller.capsuleHeight * 0.55f;
            contactTransform.t = animationController.GetSkeletonPosition();

            ret = StartTransition(ref parkourData.ledgeToLedgeTransitionData, ref contactTransform, ref targetTransform);

            if (ret)
            {
                //locomotionAbility.SetLocomotionState(TraverserLocomotionAbility.LocomotionAbilityState.Ledge);
                controller.groundSnap = true;
            }

            return ret;
        }

        private bool StartTransition(ref TraverserTransition.TraverserTransitionData transitionData, ref TraverserTransform contactTransform, ref TraverserTransform targetTransform)
        {
            // --- Apply offsets to warp points ---
            contactTransform.t -= (transform.forward * transitionData.contactOffset);
            targetTransform.t += (transform.forward * transitionData.targetOffset);
          

            return animationController.transition.StartTransition(ref transitionData, ref contactTransform, ref targetTransform);
        }

        // -------------------------------------------------

        // --- Events ---

        private void OnAnimatorIK(int layerIndex)
        {
            if (!abilityController.isCurrent(this))
                return;

            //// --- Set weights to 0 and return if IK is off ---
            //if (!fIKOn)
            //{
            //    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
            //    animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
            //    return;
            //}

            //// --- Else, sample foot weight from the animator's parameters, which are set by the animations themselves through curves ---

            //RaycastHit hit;

            //// --- Left foot ---
            //animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animationController.animator.GetFloat("IKLeftFootWeight"));

            //IKRay.origin = animationController.animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up;
            //IKRay.direction = Vector3.down;

            //if (Physics.Raycast(IKRay, out hit, feetIKGroundDistance, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
            //{
            //    Vector3 footPosition = hit.point;
            //    footPosition.y += footHeight;
            //    animationController.animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
            //}

            //// --- Right foot ---
            //animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animationController.animator.GetFloat("IKRightFootWeight"));
           
            //IKRay.origin = animationController.animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up;
            //IKRay.direction = Vector3.down;

            //if (Physics.Raycast(IKRay, out hit, feetIKGroundDistance, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
            //{
            //    Vector3 footPosition = hit.point;
            //    footPosition.y += footHeight;
            //    animationController.animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
            //}
        }

        // -------------------------------------------------
    }
}

