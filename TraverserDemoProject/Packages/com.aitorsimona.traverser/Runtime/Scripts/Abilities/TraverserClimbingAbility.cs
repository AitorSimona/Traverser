using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserClimbingAbility : MonoBehaviour, TraverserAbility
    {
        // --- Attributes ---

        [Header("Animation")]
        [Tooltip("A reference to the locomotion ability's dataset (scriptable object asset).")]
        public TraverserClimbingData climbingData;

        [Header("Ledge traversal settings")]
        [Tooltip("Desired speed in meters per second for ledge climbing.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedLedge;

        [Tooltip("The closest the character will be able to get to a ledge corner, in meters.")]
        [Range(0.0f, 0.5f)]
        public float desiredCornerMinDistance;

        [Tooltip("The maximum distance the character will be able to cover when climbing to a ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxClimbableHeight;

        [Tooltip("The maximum distance the character will be able to cover when jumping from ledge to ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxJumpRadius = 1.0f;

        [Tooltip("The minimum distance at which we trigger a ledge to ledge transition, below this we just move and corner.")]
        public float minLedgeDistance = 0.5f;

        [Header("Feet IK settings")]
        [Tooltip("Activates or deactivates foot IK placement for the climbing ability.")]
        public bool fIKOn = true;
        [Tooltip("The character's foot length (size in meters).")]
        [Range(0.0f, 1.0f)]
        public float footLength = 1.0f;
        [Tooltip("The length of the ray used to detect surfaces to place the feet on.")]
        public float feetRayLength = 2.0f;

        [Header("Hands IK settings")]
        [Tooltip("Activates or deactivates hand IK placement for the climbing ability.")]
        public bool hIKOn = true;
        [Tooltip("The Y distance to correct in order to place the hands on the ledge.")]
        public float handIKYDistance = 1.0f;
        [Tooltip("The character's hand length (size in meters) / correction in forward direction.")]
        [Range(-1.0f, 1.0f)]
        public float handLength = 1.0f;

        [Header("Debug")]
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Selecting the object may be required to show debug geometry.")]
        public bool debugDraw = false;

        [Tooltip("Indicates the point the character is aiming to, used to trigger a ledge to ledge transition.")]
        [Range(0.0f, 1.0f)]
        public float aimDebugSphereRadius = 0.25f;

        // --------------------------------

        // --- Ability-level state ---
        public enum ClimbingAbilityState
        {
            Suspended,
            Mounting,
            Dismount,
            PullUp,
            DropDown,
            LedgeToLedge,
            JumpBack,
            Climbing
        }

        // --------------------------------

        // --- Climbing-level state (directions) ---
        public enum ClimbingState
        {
            Idle,
            Up,
            Down,
            Left,
            Right,
            UpRight,
            DownRight,
            UpLeft,
            DownLeft,
            None
        }

        // --------------------------------

        // --- Private Variables ---

        private TraverserAbilityController abilityController;
        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserLocomotionAbility locomotionAbility;

        private ClimbingAbilityState state; // Actual Climbing movement/state
        private ClimbingState climbingState; // Actual Climbing direction

        // --- The position at which we are currently aiming to, determined by maxJumpRadius ---
        private Vector3 targetAimPosition;

        // --- Jump hangs ---
        private float maxDistance = 3.0f;
        private float maxYDifference = 0.25f;

        // --- Procedural corners ---
        private Vector3 previousHookNormal;
        private float cornerRotationLerpValue = 0.0f;
        private float cornerLerpInitialValue = 0.0f;
        private bool movingLeft = false;

        // --- Character will be adjusted to movable ledge if target animation transition is below this normalized time ---
        private float ledgeAdjustmentLimitTime = 0.25f;

        // --- Ledge geometry will be changed to new one after this normalized time has passed in the target animation ---
        private float timeToLedgeToLedge = 0.35f;

        // --- Offsets height of hanged transform to match a new position (upper or lower) --- 
        private float hangedTransformHeightRatio = 0.6f;

        Vector3 nearbyLedgeCollisionPosition = Vector3.zero;
        bool leftIsMax = false;
        Vector3 targetDirection = Vector3.zero;
        bool onNearbyLedgeTransition = false;

        Vector3 ltlRayOrigin = Vector3.zero;
        Vector3 ltlRayDestination = Vector3.zero;

        // --------------------------------

        // --- World interactable elements ---

        private TraverserLedgeObject.TraverserLedgeGeometry ledgeGeometry;
        private TraverserLedgeObject.TraverserLedgeGeometry auxledgeGeometry;
        private TraverserLedgeObject.TraverserLedgeHook ledgeHook;

        // --------------------------------

        // --- Basic Methods ---
        public void Start()
        {
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            locomotionAbility = GetComponent<TraverserLocomotionAbility>();

            state = ClimbingAbilityState.Suspended;
            climbingState = ClimbingState.None;

            ledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            auxledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            ledgeHook = TraverserLedgeObject.TraverserLedgeHook.Create();
        }

        private void LateUpdate()
        {
            // --- Keep character on ledge if it moves ---
            if (!abilityController.isCurrent(this))
                return;

            Vector3 delta;

            if (animationController.transition.isON
                &&
                (!animationController.animator.IsInTransition(0)
                || animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > ledgeAdjustmentLimitTime)
                && !animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.pullUpTransitionData.targetAnim)
                && !animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.dropDownTransitionData.targetAnim))
            {
                // --- Update ledge if it moves, but do not teleport controller, warping will handle it ---
                ledgeGeometry.UpdateLedge();
                delta = auxledgeGeometry.UpdateLedge();
            }
            else
            {
                // --- Update ledge if it moves, teleport controller to follow ledge ---
                auxledgeGeometry.UpdateLedge();
                delta = ledgeGeometry.UpdateLedge();
                controller.TeleportTo(transform.position + delta);
            }

            // --- Notify transition handler to adapt motion warping to new destination
            if (state != ClimbingAbilityState.Dismount)
            {
                animationController.transition.SetDestinationOffset(ref delta);
            }

            // --- Draw ledge geometry ---
            if (debugDraw)
            {
                ledgeGeometry.DebugDraw();
                ledgeGeometry.DebugDraw(ref ledgeHook);
            }
        }

        // --------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            if (animationController.transition.isON)
                animationController.transition.UpdateTransition();

            return this;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            TraverserAbility ret = this;


            // --- If character is not falling ---
            if (!IsState(ClimbingAbilityState.Suspended))
            {
                // --- Handle current state ---
                switch (state)
                {
                    case ClimbingAbilityState.Mounting:
                        HandleMountingState();
                        break;        

                    case ClimbingAbilityState.Dismount:
                        HandleDismountState();
                        break;

                    case ClimbingAbilityState.PullUp:
                        HandlePullUpState();
                        break;

                    case ClimbingAbilityState.DropDown:
                        HandleDropDownState();
                        break;

                    case ClimbingAbilityState.LedgeToLedge:
                        HandleLedgeToLedgeState();
                        break;

                    case ClimbingAbilityState.JumpBack:
                        HandleJumpBackState();
                        break;

                    case ClimbingAbilityState.Climbing:
                        HandleClimbingState(deltaTime);
                        break;

                    default:
                        break;
                }

                // --- Let other abilities handle the situation ---
                if (IsState(ClimbingAbilityState.Suspended))
                {
                    ret = null;

                    ledgeGeometry.Reset();

                    SetClimbingState(ClimbingState.None);

                    // --- Turn off/on controller ---
                    controller.ConfigureController(true);
                }
            }

            return ret;
        }

        public bool OnContact(TraverserTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            //---If we make contact with a climbable surface and player issues climb order, mount ---         
            BoxCollider collider = controller.current.collider as BoxCollider;

            if (collider == null)
                return ret;

            if (abilityController.inputController.GetInputButtonEast()
                && IsState(ClimbingAbilityState.Suspended)
                && controller.isGrounded             // --- Ensure we are not falling ---
                && collider.gameObject.GetComponent<TraverserClimbingObject>() != null)
            {
                // --- Fill auxiliary ledge to prevent mounting too close to a corner ---
                auxledgeGeometry.Initialize(ref collider);
                TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHook(contactTransform.t);

                // --- Make sure we don't mount our current ground ---
                if (!collider.Equals(controller.current.ground as BoxCollider))
                {
                    // --- We want to reach the pre climb position and then activate the animation ---
                    TraverserTransform hangedTransform = GetHangedSkeletonTransform(animationController.GetSkeletonPosition(), ref auxledgeGeometry, ref ledgeHook);

                    // --- Offset contact transform ---
                    contactTransform.t -= transform.forward * climbingData.mountTransitionData.contactOffset;
                    hangedTransform.t -= transform.forward * climbingData.mountTransitionData.targetOffset;

                    // --- Require a transition ---
                    ret = animationController.transition.StartTransition(ref climbingData.mountTransitionData, ref contactTransform, ref hangedTransform);

                    // --- If transition start is successful, change state ---
                    if (ret)
                    {
                        ledgeGeometry.Initialize(ref collider);

                        SetState(ClimbingAbilityState.Mounting);

                        // --- Turn off/on controller ---
                        controller.ConfigureController(false);
                    }
                }
            }
            else if ((locomotionAbility.GetLocomotionState() == TraverserLocomotionAbility.LocomotionAbilityState.Falling
                || locomotionAbility.GetLocomotionState() == TraverserLocomotionAbility.LocomotionAbilityState.Jumping)
                && (contactTransform.t - (transform.position + Vector3.up* controller.capsuleHeight)).magnitude < maxDistance
                && collider.gameObject.GetComponent<TraverserClimbingObject>() != null)
            {
                // ---  We are falling and colliding against a ledge
                auxledgeGeometry.Initialize(ref collider);

                if (!ledgeGeometry.IsInitialized())
                    ledgeGeometry.Initialize(ref auxledgeGeometry);

                TraverserTransform hangedTransform = GetHangedSkeletonTransform(animationController.GetSkeletonPosition(), ref auxledgeGeometry, ref ledgeHook);

                // --- Require a transition ---
                //animationController.animator.CrossFade(climbingData.fallTransitionAnimation.animationStateName, climbingData.fallTransitionAnimation.transitionDuration, 0);

                Vector3 difference = (contactTransform.t - (transform.position + Vector3.up * controller.capsuleHeight));

                // --- Decide whether to trigger a short or large hang transition ---

                if (difference.magnitude > maxDistance / 4.0f
                    && difference.y < maxYDifference
                    && locomotionAbility.GetLocomotionState() == TraverserLocomotionAbility.LocomotionAbilityState.Falling)
                {
                    animationController.animator.Play(climbingData.fallTransitionAnimation.animationStateName, 0);
                    ret = animationController.transition.StartTransition(ref climbingData.jumpHangTransitionData, ref contactTransform, ref hangedTransform);
                }
                else if (difference.y < maxYDifference
                    && difference.magnitude <= maxDistance / 4.0f)
                {
                    animationController.animator.Play(climbingData.fallTransitionAnimation.animationStateName, 0);
                    ret = animationController.transition.StartTransition(ref climbingData.jumpHangShortTransitionData, ref contactTransform, ref hangedTransform);
                }
                // --- If transition start is successful, change state ---
                if (ret)
                {
                    SetState(ClimbingAbilityState.LedgeToLedge);
                    locomotionAbility.ResetLocomotion();

                    // --- Turn off/on controller ---
                    controller.ConfigureController(false);
                }
                
            }

            return ret;
        }

        public bool OnDrop(float deltaTime)
        {
            bool ret = false;

            if (abilityController.inputController.GetInputButtonEast())
            {
                BoxCollider collider = controller.previous.ground as BoxCollider;

                // --- If collider exists and it is a climbable object ---
                if (collider && collider.GetComponent<TraverserClimbingObject>())
                {
                    // --- Initialize ledge and compute contact transform ---
                    auxledgeGeometry.Initialize(ref collider);
                    TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHook(controller.previous.position);

                    Vector3 skeletonPosition = animationController.GetSkeletonPosition();

                    Vector3 hookPosition = auxledgeGeometry.GetPosition(ref auxHook);
                    hookPosition.y += (skeletonPosition.y - transform.position.y);
                    Quaternion hookRotation = Quaternion.LookRotation(-auxledgeGeometry.GetNormal(auxHook.index), transform.up);

                    TraverserTransform contactTransform = TraverserTransform.Get(hookPosition, hookRotation);
                    contactTransform.t -= transform.forward * climbingData.dropDownTransitionData.contactOffset;

                    // --- Get target transform ---
                    TraverserTransform hangedTransform = GetHangedSkeletonTransform(skeletonPosition, ref auxledgeGeometry, ref auxHook);
         
                    // --- If capsule collides against any relevant collider, do not start the dropDown transition ---
                    if (!Physics.CheckCapsule(hangedTransform.t, hangedTransform.t + Vector3.up * controller.capsuleHeight, controller.capsuleRadius, controller.characterCollisionMask))
                    {
                        hangedTransform.q = hookRotation;

                        // --- Require a transition ---
                        ret = animationController.transition.StartTransition(ref climbingData.dropDownTransitionData ,ref contactTransform, ref hangedTransform);

                        if (ret)
                        {
                            ledgeGeometry.Initialize(ref auxledgeGeometry);
                            ledgeHook = auxHook;

                            SetState(ClimbingAbilityState.DropDown);
                            // --- Turn off/on controller ---
                            controller.ConfigureController(false);
                        }
                    }
                }
            }

            return ret;
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        // --- Climbing states wrappers ---
        void HandleMountingState()
        {
            // --- If transition is over and we were mounting change state to ledge idle ---
            if (!animationController.transition.isON)
            {
                SetState(ClimbingAbilityState.Climbing); 
                SetClimbingState(ClimbingState.Idle);

                //TraverserTransform hangedTransform = GetHangedTransform();

                controller.ConfigureController(false);
                //controller.TeleportTo(hangedTransform.t);
                //transform.rotation = hangedTransform.q;
            }
        }

        void HandleDismountState()
        {
            if (!animationController.transition.isON)
            {
                //animationController.AdjustSkeleton();
                SetState(ClimbingAbilityState.Suspended);

                // --- Get skeleton's current position and teleport controller ---
                //Vector3 newTransform = animationController.skeleton.position;
                //newTransform.y -= controller.capsuleHeight / 2.0f;
                //controller.TeleportTo(newTransform);
                locomotionAbility.ResetLocomotion();

                animationController.animator.CrossFade(climbingData.locomotionOnAnimation.animationStateName, climbingData.locomotionOnAnimation.transitionDuration, 0);
            }
        }

        void HandlePullUpState()
        {
            if (!animationController.transition.isON)
            {
                //animationController.AdjustSkeleton();
                SetState(ClimbingAbilityState.Suspended);

                // --- Get skeleton's current position and teleport controller ---
                //Vector3 newTransform = animationController.skeleton.position;
                //newTransform.y -= controller.capsuleHeight / 2.0f;
                //controller.TeleportTo(newTransform);
                locomotionAbility.ResetLocomotion();
                animationController.animator.CrossFade(climbingData.locomotionOnAnimation.animationStateName, climbingData.locomotionOnAnimation.transitionDuration, 0);
            }
        }

        void HandleDropDownState()
        {
            if (!animationController.transition.isON)
            {
                SetState(ClimbingAbilityState.Climbing);
                SetClimbingState(ClimbingState.Idle);
                animationController.animator.CrossFade(climbingData.ledgeIdleAnimation.animationStateName, climbingData.ledgeIdleAnimation.transitionDuration, 0);

                ResetClimbing();

                TraverserTransform hangedTransform = GetHangedTransform(animationController.GetSkeletonPosition(), ref ledgeGeometry, ref ledgeHook);

                controller.ConfigureController(false);
                //controller.TeleportTo(hangedTransform.t);
                transform.rotation = hangedTransform.q;
            }
        }

        void HandleLedgeToLedgeState()
        {
            if (animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > timeToLedgeToLedge
                && animationController.transition.isTargetON
                && !animationController.animator.IsInTransition(0)
                && !ledgeGeometry.IsEqual(ref auxledgeGeometry))
            {
                ledgeGeometry.Initialize(ref auxledgeGeometry);
            }

            if (!animationController.transition.isON)
            {

                if (!ledgeGeometry.IsEqual(ref auxledgeGeometry))
                    ledgeGeometry.Initialize(ref auxledgeGeometry);

                SetState(ClimbingAbilityState.Climbing);
                SetClimbingState(ClimbingState.Idle);

                //TraverserTransform hangedTransform = GetHangedTransform();

                controller.ConfigureController(false);
                //controller.TeleportTo(hangedTransform.t);
                //transform.rotation = hangedTransform.q;
            }
        }

        void HandleJumpBackState()
        {
            if (!animationController.transition.isON)
            {
                SetState(ClimbingAbilityState.Suspended);
                //animationController.animator.CrossFade(climbingData.fallLoopAnimation.animationStateName, climbingData.fallLoopAnimation.transitionDuration, 0);
                animationController.animator.Play(climbingData.fallLoopAnimation.animationStateName, 0);

                //Vector3 newTransform = animationController.skeleton.position;
                //newTransform.y -= controller.capsuleHeight / 2.0f;
                //controller.TeleportTo(newTransform);
                locomotionAbility.ResetLocomotion();
                locomotionAbility.SetLocomotionState(TraverserLocomotionAbility.LocomotionAbilityState.Falling);
                transform.rotation = Quaternion.LookRotation(-ledgeGeometry.GetNormal(ledgeHook.index),Vector3.up);
            }
        }

        void HandleClimbingState(float deltaTime)
        {
            bool canMove = UpdateClimbing(deltaTime);

            ClimbingState desiredState = GetDesiredClimbingState();

            if (!canMove)
                desiredState = ClimbingState.Idle;

            // --- Handle ledge climbing/movement direction ---
            if (!IsClimbingState(desiredState))
            {
                if (desiredState == ClimbingState.Idle)
                    animationController.animator.CrossFade(climbingData.ledgeIdleAnimation.animationStateName, climbingData.ledgeIdleAnimation.transitionDuration, 0);
                else if (desiredState == ClimbingState.Right)
                    animationController.animator.CrossFade(climbingData.ledgeRightAnimation.animationStateName, climbingData.ledgeRightAnimation.transitionDuration, 0);
                else if (desiredState == ClimbingState.Left)
                    animationController.animator.CrossFade(climbingData.ledgeLeftAnimation.animationStateName, climbingData.ledgeLeftAnimation.transitionDuration, 0);

                SetClimbingState(desiredState);
            }

            RaycastHit hit;

            bool closeToDrop = Physics.Raycast(transform.position, Vector3.down, out hit ,maxClimbableHeight - controller.capsuleHeight, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);

            if(debugDraw)
                Debug.DrawRay(transform.position, Vector3.down * (maxClimbableHeight - controller.capsuleHeight), Color.yellow);

            // --- The position at which we perform a capsule check to prevent pulling up into a wall --- 
            Vector3 pullupPosition = ledgeGeometry.GetPosition(ref ledgeHook);
            pullupPosition += Vector3.up * controller.capsuleHeight / 2.0f;
            pullupPosition += transform.forward * climbingData.pullUpTransitionData.targetOffset;

            // --- React to pull up/dismount ---
            if (abilityController.inputController.GetInputButtonNorth() 
                && abilityController.inputController.GetInputMovement().y > 0.5f &&
                state != ClimbingAbilityState.LedgeToLedge && !IsCapsuleColliding(ref pullupPosition))
            {
                bool ret;

                TraverserTransform contactTransform = TraverserTransform.Get(transform.position, transform.rotation);

                // --- Offset target transform ---
                TraverserTransform targetTransform = contactTransform;
                targetTransform.t = pullupPosition;

                // --- Require a transition ---
                ret = animationController.transition.StartTransition(ref climbingData.pullUpTransitionData, ref contactTransform, ref targetTransform);

                // --- If transition start is successful, change state ---
                if (ret)
                {
                    SetState(ClimbingAbilityState.PullUp);
                    controller.targetDisplacement = Vector3.zero;
                }
            }
            else if (closeToDrop && abilityController.inputController.GetInputButtonEast())
            {
                bool ret;

                TraverserTransform contactTransform = TraverserTransform.Get(transform.position, transform.rotation);

                // --- Offset target transform ---
                TraverserTransform targetTransform = contactTransform;
                targetTransform.t.y = hit.point.y + controller.capsuleHeight/2.0f;
                targetTransform.t -= transform.forward * climbingData.dismountTransitionData.targetOffset;
                
                // --- Require a transition ---
                ret = animationController.transition.StartTransition(ref climbingData.dismountTransitionData, ref contactTransform, ref targetTransform);

                // --- If transition start is successful, change state ---
                if (ret)
                {
                    SetState(ClimbingAbilityState.Dismount);
                    controller.targetDisplacement = Vector3.zero;
                }
            }
        }

        bool UpdateClimbing(float deltaTime)
        {
            bool ret = false;

            Vector2 leftStickInput = abilityController.inputController.GetInputMovement();

            // --- Store last given player direction ---
            if (leftStickInput.x != 0.0f)
            {
                movingLeft = leftStickInput.x < 0.0f;
                ret = true;
            }

            // --- Given input, compute target position ---
            Vector3 direction = (transform.right * leftStickInput.x) /*+ transform.forward - ledgeGeometry.GetNormal(ledgeHook.index)*/ /*ledgeGeometry.GetNormal(ledgeHook) - transform.right*/;
            direction.Normalize();

            if(debugDraw)
                Debug.DrawLine(ledgeGeometry.GetPosition(ref ledgeHook), ledgeGeometry.GetPosition(ref ledgeHook) + direction * 5.0f);

            Vector3 delta = direction * Mathf.Abs(leftStickInput.x) * desiredSpeedLedge * deltaTime;
            Vector3 targetPosition = transform.position + delta;

            // --- Update hook ---
            TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook = ledgeGeometry.UpdateHook(ref ledgeHook, targetPosition, movingLeft);

            // --- Handle movement on the same ledge, including corners ---
            if (!onNearbyLedgeTransition)
                OnLedgeMovement(ref desiredLedgeHook, targetPosition);

            // --- Handle nearby ledge player controlled transition ---
            OnNearbyLedgeMovement(ref desiredLedgeHook, targetPosition);

            // --- Handle ledge to ledge transition, if ledge is detected ---
            if (!onNearbyLedgeTransition && cornerRotationLerpValue == cornerLerpInitialValue)
                OnLedgeToLedge();

            // --- Trigger a jump back transition if required by player ---
            if (abilityController.inputController.GetInputButtonSouth())
            {
                TraverserTransform contactTransform = TraverserTransform.Get(animationController.GetSkeletonPosition(), transform.rotation);
                // --- Offset target transform ---
                TraverserTransform targetTransform = contactTransform;
                targetTransform.t.y += 0.5f;
                targetTransform.t -= transform.forward * climbingData.jumpBackTransitionData.targetOffset;

                bool success = animationController.transition.StartTransition(ref climbingData.jumpBackTransitionData, ref contactTransform, ref targetTransform);

                if (success)
                {
                    SetState(ClimbingAbilityState.JumpBack);
                    controller.targetDisplacement = Vector3.zero;
                    // --- Turn off/on controller ---
                    controller.ConfigureController(false);
                }        
            }

            return ret;
        }
        
        public void OnLedgeMovement(ref TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook, Vector3 targetPosition)
        {
            if (!ledgeGeometry.IsOutOfBounds(ref ledgeHook, 0.25f))
            {
                ledgeHook = desiredLedgeHook;
                controller.targetDisplacement = targetPosition - transform.position;
                cornerRotationLerpValue = cornerLerpInitialValue;
                targetDirection = ledgeGeometry.GetNormal(movingLeft ? ledgeGeometry.GetNextEdgeIndex(ledgeHook.index) : ledgeGeometry.GetPreviousEdgeIndex(ledgeHook.index));
                previousHookNormal = ledgeGeometry.GetNormal(ledgeHook.index);
                leftIsMax = movingLeft;
            }
            else
            {
                ledgeHook = desiredLedgeHook;
                controller.targetDisplacement = targetPosition - transform.position;

                // --- The more displacement, the greater the rotation change ---
                if (leftIsMax)
                {
                    if (movingLeft)
                        cornerRotationLerpValue += controller.targetDisplacement.magnitude;
                    else
                        cornerRotationLerpValue -= controller.targetDisplacement.magnitude;
                }
                else
                {
                    if (movingLeft)
                        cornerRotationLerpValue -= controller.targetDisplacement.magnitude;
                    else
                        cornerRotationLerpValue += controller.targetDisplacement.magnitude;
                }

                Vector3 lookDirection = Vector3.Lerp(previousHookNormal, targetDirection, cornerRotationLerpValue);
                transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * Quaternion.FromToRotation(transform.forward, lookDirection), 1.0f);
            }
        }

        public void OnNearbyLedgeMovement(ref TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook, Vector3 targetPosition)
        {
            // --- Check for ledge ---
            RaycastHit hit;
            Vector3 dire = movingLeft ? -transform.right : transform.right;

            Vector3 castOrigin = transform.position + Vector3.up * controller.capsuleHeight * 0.75f;
            castOrigin += ledgeGeometry.GetPosition(ref ledgeHook) - castOrigin;

            nearbyLedgeCollisionPosition = castOrigin /*+ transform.forward * aimDebugSphereRadius*/;

            bool collided = Physics.SphereCast(nearbyLedgeCollisionPosition, aimDebugSphereRadius, dire
                , out hit, minLedgeDistance, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);

            nearbyLedgeCollisionPosition += dire * minLedgeDistance;

            float distanceToCorner = 0.0f;

            BoxCollider hitCollider = hit.collider as BoxCollider;

            if (collided 
                && ledgeGeometry.IsEqual(ref hitCollider) 
                && abilityController.inputController.GetInputMovement().x != 0.0f
                && hit.transform.GetComponent<TraverserClimbingObject>())
            {
                ledgeGeometry.ClosestPointPlaneDistance(hit.point, ref ledgeHook, ref distanceToCorner);
                previousHookNormal = ledgeGeometry.GetNormal(ledgeHook.index);
                ledgeGeometry.Initialize(ref hitCollider);
                ledgeHook = ledgeGeometry.GetHook(transform.position);
                targetDirection = ledgeGeometry.GetNormal(ledgeHook.index);

                if(cornerRotationLerpValue >= 1.0f || cornerRotationLerpValue == 0.0f)
                    cornerRotationLerpValue = cornerLerpInitialValue;
                else
                    cornerRotationLerpValue = 1.0f - cornerRotationLerpValue;


                onNearbyLedgeTransition = true;
                leftIsMax = movingLeft;
            }
            else if(onNearbyLedgeTransition)
            {
                if (ledgeHook.index == desiredLedgeHook.index)
                    ledgeHook = desiredLedgeHook;

                controller.targetDisplacement = targetPosition - transform.position;

                // --- The more displacement, the greater the rotation change ---
                if (leftIsMax)
                {
                    if (movingLeft)
                        cornerRotationLerpValue += controller.targetDisplacement.magnitude;
                    else
                        cornerRotationLerpValue -= controller.targetDisplacement.magnitude;
                }
                else
                {
                    if (movingLeft)
                        cornerRotationLerpValue -= controller.targetDisplacement.magnitude;
                    else
                        cornerRotationLerpValue += controller.targetDisplacement.magnitude;
                }

                Vector3 lookDirection = Vector3.Lerp(previousHookNormal, targetDirection, cornerRotationLerpValue);
                transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * Quaternion.FromToRotation(transform.forward, lookDirection), 1.0f);

                if (cornerRotationLerpValue > 1.0f)
                    onNearbyLedgeTransition = false;
            }

        }

        public void OnLedgeToLedge()
        {
            Vector2 leftStickInput = abilityController.inputController.GetInputMovement();

            // --- Given input, compute target aim Position (used to trigger ledge to ledge transitions) ---
            Vector3 aimDirection = transform.right * leftStickInput.x + transform.up * leftStickInput.y;
            aimDirection.Normalize();

            Vector3 rayOrigin = transform.position
                + transform.up * controller.capsuleHeight * 0.75f
                + transform.forward * controller.capsuleRadius;

            targetAimPosition = rayOrigin + aimDirection * maxJumpRadius * abilityController.inputController.GetMoveIntensity();

            // --- Check for ledge ---
            RaycastHit hit;

            float moveIntensity = abilityController.inputController.GetMoveIntensity();
            bool collided = Physics.SphereCast(rayOrigin, aimDebugSphereRadius, aimDirection, out hit, maxJumpRadius * moveIntensity, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);
            float distanceToCorner = 0.0f;

            if (collided)
                ledgeGeometry.ClosestPointDistance(hit.point, ref ledgeHook, ref distanceToCorner);

            // --- Trigger a ledge to ledge transition if required by player ---
            if (moveIntensity > 0.1f && collided
                && distanceToCorner > minLedgeDistance
                && hit.transform.GetComponent<TraverserClimbingObject>()) 
            {
                BoxCollider hitCollider = hit.collider as BoxCollider;
                auxledgeGeometry.Initialize(ref hitCollider);

                if (!auxledgeGeometry.IsEqual(ref ledgeGeometry))
                {
                    // --- Adjust the aim position so we don't end in another ledge edge ---
                    Vector3 ledgePos = hit.collider.bounds.center - transform.forward * hit.collider.bounds.size.magnitude;
                    TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHookAt(ledgePos, 0.35f);
                    TraverserTransform contactTransform = TraverserTransform.Get(animationController.GetSkeletonPosition(), transform.rotation);

                    Vector3 hookPosition = auxledgeGeometry.GetPosition(ref auxHook) - auxledgeGeometry.GetNormal(auxHook.index) * controller.capsuleRadius;
                    hookPosition.y -= controller.capsuleHeight * (1.0f - hangedTransformHeightRatio);

                    TraverserTransform hangedTransform = TraverserTransform.Get(hookPosition, Quaternion.LookRotation(auxledgeGeometry.GetNormal(auxHook.index), transform.up)); 

                    // Overriding rayOrigin and hit variables from above! 
                    rayOrigin = transform.position + Vector3.up * controller.capsuleHeight * 0.5f;
                    Vector3 probeDirection = hangedTransform.t - rayOrigin;

                    ltlRayOrigin = rayOrigin;
                    ltlRayDestination = ltlRayOrigin + probeDirection;

                    // --- Throw a ray in target direction and if a collider is found prevent ledge to ledge ---
                    if (Physics.Raycast(ltlRayOrigin, probeDirection.normalized, out hit, probeDirection.magnitude,controller.characterCollisionMask,QueryTriggerInteraction.Ignore))
                    {
                        ledgePos = transform.position;
                        auxHook = auxledgeGeometry.GetHookAt(ledgePos, 0.35f);
                        hookPosition = auxledgeGeometry.GetPosition(ref auxHook) - auxledgeGeometry.GetNormal(auxHook.index) * controller.capsuleRadius;
                        hookPosition.y -= controller.capsuleHeight * 0.25f;
                        hangedTransform = TraverserTransform.Get(hookPosition, Quaternion.LookRotation(auxledgeGeometry.GetNormal(auxHook.index), transform.up));

                        probeDirection = hangedTransform.t - rayOrigin;
                        ltlRayDestination = ltlRayOrigin + probeDirection;

                        // --- If there is a collision, try to jump to the previous edge, else prevent ledge to ledge ---
                        if (Physics.Raycast(ltlRayOrigin, probeDirection.normalized, out hit, probeDirection.magnitude, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                            return;
                    }

                    // --- Compute aim angle and decide which transition has to be triggered ---
                    float angle = Vector3.SignedAngle(transform.up, (hookPosition - transform.position + Vector3.up * controller.capsuleHeight).normalized, -transform.forward);

                    bool success;

                    if (Mathf.Abs(angle) < 15.0f && leftStickInput.y > 0.0f)
                    {
                        success = animationController.transition.StartTransition(ref climbingData.HopUpTransitionData, ref contactTransform, ref hangedTransform);
                    }
                    else if (angle >= 15.0f && angle <= 135.0f)
                    {
                        success = animationController.transition.StartTransition(ref climbingData.HopRightTransitionData, ref contactTransform, ref hangedTransform);
                    }
                    else if (angle <= -15.0f && angle >= -135.0f)
                    {
                        success = animationController.transition.StartTransition(ref climbingData.HopLeftTransitionData, ref contactTransform, ref hangedTransform);
                    }
                    else
                    {
                        success = animationController.transition.StartTransition(ref climbingData.HopDownTransitionData, ref contactTransform, ref hangedTransform);
                    }

                    // --- Trigger ledge to ledge transition ---
                    if (success)
                    {
                        controller.targetDisplacement = Vector3.zero;

                        // Could be used to trigger a faster transition, useful for more responsiveness 
                        //animationController.animator.Play(climbingData.HopUpTransitionData.transitionAnim);

                        SetState(ClimbingAbilityState.LedgeToLedge);

                        if (!ledgeGeometry.IsInitialized())
                            ledgeGeometry.Initialize(ref auxledgeGeometry);

                        ledgeHook = auxHook;

                        // --- Turn off/on controller ---
                        controller.ConfigureController(false);
                    }
                }
                
            }
        }

        // --------------------------------

        // --- Utilities ---    

        public void ResetClimbing()
        {
            movingLeft = false;
            previousHookNormal = ledgeGeometry.GetNormal(ledgeHook.index);
            cornerRotationLerpValue = 1.0f;
        }

        private bool IsCapsuleColliding(ref Vector3 start)
        {
            Vector3 end = start;
            end.y += controller.capsuleHeight;

            // --- Cast a capsule and return whether there has been a collision or not ---
            return Physics.CheckCapsule(start, end, controller.capsuleRadius, controller.characterCollisionMask);        
        }

        private TraverserTransform GetHangedTransform(Vector3 fromPosition, ref TraverserLedgeObject.TraverserLedgeGeometry ledgeGeom, ref TraverserLedgeObject.TraverserLedgeHook ledgeHk)
        {
            // --- Compute the character's position and rotation when hanging on a ledge ---
            ledgeHk = ledgeGeom.GetHook(fromPosition);
            Vector3 hookPosition = ledgeGeom.GetPosition(ref ledgeHk);
            Quaternion hangedRotation = Quaternion.LookRotation(ledgeGeom.GetNormal(ledgeHk.index), transform.up);
            Vector3 hangedPosition = hookPosition - ledgeGeom.GetNormal(ledgeHk.index) * controller.capsuleRadius;
            hangedPosition.y = hookPosition.y - controller.capsuleHeight;
            return TraverserTransform.Get(hangedPosition, hangedRotation);
        }

        private TraverserTransform GetHangedSkeletonTransform(Vector3 fromPosition, ref TraverserLedgeObject.TraverserLedgeGeometry ledgeGeom, ref TraverserLedgeObject.TraverserLedgeHook ledgeHk)
        {
            // --- Compute the character's skeleton position and rotation when hanging on a ledge ---
            TraverserTransform skeletonTransform = GetHangedTransform(fromPosition, ref ledgeGeom, ref ledgeHk);
            skeletonTransform.t.y += controller.capsuleHeight * hangedTransformHeightRatio;
            return skeletonTransform;
        }

        public void SetState(ClimbingAbilityState newState)
        {
            state = newState;
        }

        public bool IsState(ClimbingAbilityState queryState)
        {
            return state == queryState;
        }

        public void SetClimbingState(ClimbingState climbingState)
        {
            this.climbingState = climbingState;
        }

        public bool IsClimbingState(ClimbingState climbingState)
        {
            return this.climbingState == climbingState;
        }
        
        ClimbingState GetDesiredClimbingState() 
        {
            Vector2 stickInput = abilityController.inputController.GetInputMovement();

            // --- Use ledge definition to determine how close we are to the edges of the wall, also at which side the vertex is (bool left) ---
            //float distance = 0.0f;
            //bool left = ledgeGeometry.ClosestPointDistance(transform.position, ref ledgeHook, ref distance);

            if (stickInput.x > 0.5f)
            {
                //if (!left && distance <= desiredCornerMinDistance)
                //    return ClimbingState.CornerRight;

                return ClimbingState.Right;
            }

            else if (stickInput.x < -0.5f)
            {
                //if (left && distance <= desiredCornerMinDistance)
                //    return ClimbingState.CornerLeft;

                return ClimbingState.Left;
            }
            

            return ClimbingState.Idle;
        }

        // --------------------------------

        // --- Events ---

        private void OnAnimatorIK(int layerIndex)
        {
            if (!abilityController.isCurrent(this) /*|| climbingState == ClimbingState.None*/)
                return;

            // --- Ensure IK is not activated during a transition between two different abilities (locomotion - mount) ---
            if ((!animationController.transition.isTargetON 
                && animationController.transition.targetAnimName == climbingData.mountTransitionData.targetAnim)
                ||
                (animationController.transition.targetAnimName == climbingData.mountTransitionData.targetAnim 
                &&animationController.animator.IsInTransition(0)))
                return;

            // --- Set weights to 0 and return if IK is off ---
            if (!fIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);
            }
            else
            {
                // --- Else, sample foot weight from the animator's parameters, which are set by the animations themselves through curves ---

                // --- Left foot ---
                float weight = animationController.animator.GetFloat("IKLeftFootWeight");

                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, weight);
                    animationController.animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, weight);

                    Vector3 footPosition = animationController.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

                    //if (animationController.IsAdjustingSkeleton())
                    //{
                    //    footPosition = animationController.leftFootPosition;
                    //    TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(footPosition);
                    //    footPosition.x = ledgeGeometry.GetPosition(ref hook).x;
                    //    footPosition.z = ledgeGeometry.GetPosition(ref hook).z;
                    //}

                    RaycastHit hit;

                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.pullUpTransitionData.targetAnim)
                        && animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.3f)
                    {
                        if (Physics.Raycast(footPosition + Vector3.up - transform.forward*0.25f, Vector3.down, out hit, feetRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point + Vector3.up *0.15f;
                        }

                        animationController.animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation);
                    }
                    else
                    {
                        //footPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.LeftFoot)));

                        if (Physics.Raycast(footPosition, transform.forward, out hit, feetRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point;
                        }

                        footPosition -= transform.forward * footLength;
                        //footPosition.y = animationController.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;

                    }

                    Debug.DrawLine(footPosition, footPosition + transform.forward * feetRayLength);
                    animationController.animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                }

                weight = animationController.animator.GetFloat("IKRightFootWeight");

                // --- Right foot ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weight);
                    animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, weight);

                    Vector3 footPosition = animationController.animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                    //if (animationController.IsAdjustingSkeleton())
                    //{
                    //    footPosition = animationController.rightFootPosition;
                    //    TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(footPosition);
                    //    footPosition.x = ledgeGeometry.GetPosition(ref hook).x;
                    //    footPosition.z = ledgeGeometry.GetPosition(ref hook).z;
                    //}

                    RaycastHit hit;

                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.pullUpTransitionData.targetAnim)
                        && animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.3f)
                    {
                        if (Physics.Raycast(footPosition + Vector3.up - transform.forward * 0.25f, Vector3.down, out hit, feetRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point + Vector3.up * 0.15f;
                        }

                        animationController.animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation);
                    }
                    else
                    {
                        //footPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.RightFoot)));

                        if (Physics.Raycast(footPosition, transform.forward, out hit, feetRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point;
                        }

                        footPosition -= transform.forward * footLength;
                        //footPosition.y = animationController.animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y;
                    }

                    Debug.DrawLine(footPosition, footPosition + transform.forward * feetRayLength);
                    animationController.animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                }
            }

            // --- Set weights to 0 and return if IK is off ---
            if (!hIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);
            }
            else
            {
                // --- Else, sample hand weight from the animator's parameters, which are set by the animations themselves through curves ---
                float weight = animationController.animator.GetFloat("IKLeftHandWeight");

                // --- Left hand ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
                    animationController.animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);

                    Vector3 IKPosition = animationController.animator.GetIKPosition(AvatarIKGoal.LeftHand);

                    //if (animationController.IsAdjustingSkeleton())
                    //    IKPosition = animationController.leftHandPosition;

                    TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(IKPosition);

                    Vector3 handPosition = ledgeGeometry.GetPosition(ref hook);

                    handPosition -= transform.forward * handLength;
                    handPosition.y += handIKYDistance;

                    //Debug.DrawLine(handPosition, handPosition + transform.forward * 2.0f);
                    animationController.animator.SetIKPosition(AvatarIKGoal.LeftHand, handPosition);
                    animationController.animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.FromToRotation(transform.forward, transform.forward + Vector3.up*1.5f) * transform.rotation);
                }

                weight = animationController.animator.GetFloat("IKRightHandWeight");

                // --- Right hand ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
                    animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);

                    Vector3 IKPosition = animationController.animator.GetIKPosition(AvatarIKGoal.RightHand);

                    //if (animationController.IsAdjustingSkeleton())
                    //    IKPosition = animationController.rightHandPosition;

                    TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(IKPosition);


                    Vector3 handPosition = ledgeGeometry.GetPosition(ref hook);

                    handPosition -= transform.forward * handLength;
                    handPosition.y += handIKYDistance;

                    //Debug.DrawLine(handPosition, handPosition - transform.forward * 2.0f);
                    animationController.animator.SetIKPosition(AvatarIKGoal.RightHand, handPosition);
                    animationController.animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.FromToRotation(transform.forward, transform.forward + Vector3.up*1.5f) * transform.rotation);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!debugDraw || abilityController == null)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetAimPosition, aimDebugSphereRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(nearbyLedgeCollisionPosition, aimDebugSphereRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(ltlRayOrigin, ltlRayDestination);
        }

        // -------------------------------------------------
    }
}

