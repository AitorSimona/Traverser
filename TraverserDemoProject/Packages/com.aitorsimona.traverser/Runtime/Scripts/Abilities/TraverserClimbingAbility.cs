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

        [Tooltip("Desired speed for ledge corner traversal.")]
        public float ledgeCornerSpeed = 1.0f;

        [Tooltip("The closest the character will be able to get to a ledge corner, in meters.")]
        [Range(0.0f, 0.5f)]
        public float desiredCornerMinDistance;

        [Tooltip("The maximum distance the character will be able to cover when climbing to a ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxClimbableHeight;

        [Tooltip("The maximum distance the character will be able to cover when jumping from ledge to ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxJumpRadius = 1.0f;

        [Header("Feet IK settings")]
        [Tooltip("Activates or deactivates foot IK placement for the climbing ability.")]
        public bool fIKOn = true;
        [Tooltip("The character's foot length (size in meters).")]
        [Range(0.0f, 1.0f)]
        public float footLength = 1.0f;

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
            //CornerRight, 
            //CornerLeft, 
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
        private float cornerLerpInitialValue = 0.25f;
        private bool movingLeft = false;


        // --- Character will be adjusted to movable ledge if target animation transition is below this normalized time ---
        private float ledgeAdjustmentLimitTime = 0.25f;

        private float minLedgeDistance = 0.5f;

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

        // --------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            if (animationController.transition.isON)
                animationController.transition.UpdateTransition();

            return this;
        }

        private void LateUpdate()
        {
            // --- Keep character on ledge if it moves ---
            //if (!animationController.transition.isON)

            if (!abilityController.isCurrent(this))
                return;

            Vector3 delta;

            if (animationController.transition.isON 
                &&  
                (!animationController.animator.IsInTransition(0)
                || animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > ledgeAdjustmentLimitTime)
                && !animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.pullUpTransitionData.targetAnim))
            {
                ledgeGeometry.UpdateLedge();
                delta = auxledgeGeometry.UpdateLedge();
            }
            else
            {
                auxledgeGeometry.UpdateLedge();
                delta = ledgeGeometry.UpdateLedge();
                controller.TeleportTo(transform.position + delta);
            }



            //delta = delta.normalized * delta.magnitude;

            if (state != ClimbingAbilityState.Dismount)
            {
                animationController.transition.SetDestinationOffset(ref delta);
                GameObject.Find("dummy1").transform.position += delta;
            }

            // --- Draw ledge geometry ---
            if (debugDraw)
            {
                ledgeGeometry.DebugDraw();
                ledgeGeometry.DebugDraw(ref ledgeHook);
            }
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

                    ledgeGeometry.height = 0.0f; // reset

                    SetClimbingState(ClimbingState.None);

                    // --- Turn off/on controller ---
                    controller.ConfigureController(true);
                }

                // --- Keep character on ledge if it moves ---
                //transform.position += ledgeGeometry.UpdateLedge();
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
                auxledgeGeometry.Initialize(collider);
                TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHook(contactTransform.t);

                // --- Make sure we are not too close to the corner (We may trigger an unwanted transition afterwards) ---
                if (!collider.Equals(controller.current.ground as BoxCollider))
                {
                    // --- We want to reach the pre climb position and then activate the animation ---
                    ledgeGeometry.Initialize(collider);
                    TraverserTransform hangedTransform = GetHangedSkeletonTransform();

                    // --- Offset contact transform ---
                    contactTransform.t -= transform.forward * climbingData.mountTransitionData.contactOffset;
                    hangedTransform.t -= transform.forward * climbingData.mountTransitionData.targetOffset;

                    // --- Require a transition ---
                    ret = animationController.transition.StartTransition(ref climbingData.mountTransitionData, ref contactTransform, ref hangedTransform);

                    // --- If transition start is successful, change state ---
                    if (ret)
                    {
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
                auxledgeGeometry.Initialize(collider);

                if (ledgeGeometry.height == 0.0f)
                    ledgeGeometry.Initialize(ref auxledgeGeometry);

                TraverserTransform hangedTransform = GetHangedSkeletonTransform();

                // --- Require a transition ---
                //animationController.animator.CrossFade(climbingData.fallTransitionAnimation.animationStateName, climbingData.fallTransitionAnimation.transitionDuration, 0);

                Vector3 difference = (contactTransform.t - (transform.position + Vector3.up * controller.capsuleHeight));

                // --- Decide whether to trigger a short or large hang transition ---

                if (difference.magnitude > maxDistance / 2.0f
                    && difference.y < maxYDifference
                    && locomotionAbility.GetLocomotionState() == TraverserLocomotionAbility.LocomotionAbilityState.Falling)
                {
                    animationController.animator.Play(climbingData.fallTransitionAnimation.animationStateName, 0);
                    ret = animationController.transition.StartTransition(ref climbingData.jumpHangTransitionData, ref contactTransform, ref hangedTransform);
                }
                else if (difference.magnitude < maxDistance / 3.0f
                    && difference.y < maxYDifference)
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
                    ledgeGeometry.Initialize(collider);
                    ledgeHook = ledgeGeometry.GetHook(controller.previous.position);
                    Vector3 hookPosition = ledgeGeometry.GetPosition(ref ledgeHook);
                    hookPosition.y += (animationController.skeleton.position.y - transform.position.y);
                    Quaternion hookRotation = Quaternion.LookRotation(-ledgeGeometry.GetNormal(ref ledgeHook), transform.up);

                    TraverserTransform contactTransform = TraverserTransform.Get(hookPosition, hookRotation);
                    contactTransform.t -= transform.forward * climbingData.dropDownTransitionData.contactOffset;

                    // --- Get target transform ---
                    TraverserTransform hangedTransform = GetHangedSkeletonTransform();
         
                    // --- If capsule collides against any relevant collider, do not start the dropDown transition ---
                    if (!Physics.CheckCapsule(hangedTransform.t, hangedTransform.t + Vector3.up * controller.capsuleHeight, controller.capsuleRadius, controller.characterCollisionMask))
                    {
                        hangedTransform.q = hookRotation;

                        // --- Require a transition ---
                        ret = animationController.transition.StartTransition(ref climbingData.dropDownTransitionData ,ref contactTransform, ref hangedTransform);

                        if (ret)
                        {
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
            if (animationController.IsTransitionFinished())
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

                TraverserTransform hangedTransform = GetHangedTransform();

                controller.ConfigureController(false);
                //controller.TeleportTo(hangedTransform.t);
                transform.rotation = hangedTransform.q;
            }
        }

        void HandleLedgeToLedgeState()
        {
            if(animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.35f 
                && !animationController.animator.IsInTransition(0))
                ledgeGeometry.Initialize(ref auxledgeGeometry);

            if (!animationController.transition.isON)
            {
                SetState(ClimbingAbilityState.Climbing);
                SetClimbingState(ClimbingState.Idle);

                TraverserTransform hangedTransform = GetHangedTransform();

                controller.ConfigureController(false);
                controller.TeleportTo(hangedTransform.t);
                transform.rotation = hangedTransform.q;
            }
        }

        void HandleJumpBackState()
        {
            if (animationController.IsTransitionFinished())
            {
                SetState(ClimbingAbilityState.Suspended);
                //animationController.animator.CrossFade(climbingData.fallLoopAnimation.animationStateName, climbingData.fallLoopAnimation.transitionDuration, 0);
                animationController.animator.Play(climbingData.fallLoopAnimation.animationStateName, 0);

                Vector3 newTransform = animationController.skeleton.position;
                newTransform.y -= controller.capsuleHeight / 2.0f;
                controller.TeleportTo(newTransform);
                locomotionAbility.ResetLocomotion();
                locomotionAbility.SetLocomotionState(TraverserLocomotionAbility.LocomotionAbilityState.Falling);
                transform.rotation = Quaternion.LookRotation(-ledgeGeometry.GetNormal(ref ledgeHook),Vector3.up);
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

        Vector3 ledgeCollisionPosition = Vector3.zero;

        bool UpdateClimbing(float deltaTime)
        {
            bool ret = false;

            // --- Given input, compute target position ---
            Vector2 leftStickInput = abilityController.inputController.GetInputMovement();

            if (leftStickInput.x != 0.0f)
                movingLeft = leftStickInput.x < 0.0f;

            Vector3 direction = transform.right + transform.forward - ledgeGeometry.GetNormal(ref ledgeHook) /*ledgeGeometry.GetNormal(ledgeHook) - transform.right*/;

            Debug.DrawLine(ledgeGeometry.GetPosition(ref ledgeHook), ledgeGeometry.GetPosition(ref ledgeHook) + direction * 5.0f);


            Vector3 lookDirection = /*transform.forward +*/ ledgeGeometry.GetNormal(ref ledgeHook);
            lookDirection.Normalize();

            Debug.DrawLine(ledgeGeometry.GetPosition(ref ledgeHook), ledgeGeometry.GetPosition(ref ledgeHook) + lookDirection * 5.0f);


            Vector3 delta = direction * leftStickInput.x * desiredSpeedLedge * deltaTime;
            Vector3 targetPosition = transform.position + delta;

            // --- Given input, compute target aim Position (used to trigger ledge to ledge transitions) ---
            Vector3 aimDirection = transform.right * leftStickInput.x + transform.up * leftStickInput.y;
            aimDirection.Normalize();

            Vector3 rayOrigin = transform.position
                + transform.up * controller.capsuleHeight * 0.75f
                + transform.forward * controller.capsuleRadius;

            targetAimPosition = rayOrigin + aimDirection * maxJumpRadius * abilityController.inputController.GetMoveIntensity(); 

            // --- Update hook ---
            TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook = ledgeGeometry.UpdateHook(ref ledgeHook, targetPosition, movingLeft);

            // --- Check for ledge ---
            RaycastHit hit;

            Vector3 dire = movingLeft ? -transform.right : transform.right;
            bool collided = Physics.SphereCast(ledgeGeometry.GetPosition(ref ledgeHook), aimDebugSphereRadius,
                movingLeft ? -transform.right : transform.right
                , out hit, minLedgeDistance, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);

            ledgeCollisionPosition = ledgeGeometry.GetPosition(ref ledgeHook) + dire * minLedgeDistance;

            float distanceToCorner = 0.0f;

            if (collided)
            {
                ledgeGeometry.ClosestPointPlaneDistance(hit.point, ref ledgeHook, ref distanceToCorner);
                //Debug.Log(distanceToCorner);
            }

            // --- Update current hook if it is still on the ledge ---
            if (desiredLedgeHook.index == ledgeHook.index)
            {
                // --- Compute the position of a capsule equal to the character's at the end of the corner transition ---
                Vector3 position = transform.position;
                position += movingLeft ? -transform.right * 0.75f : transform.right * 0.75f;
                //position += transform.forward * 0.5f;


                // --- If it collides against any relevant collider, do not start the corner transition ---
                if (collided
                    && ledgeGeometry.IsOutOfBounds(ref ledgeHook, 0.25f)
                    && distanceToCorner > minLedgeDistance)
                {

                    controller.targetDisplacement = Vector3.zero;
                    ret = false;
                }
                else
                {
                    ret = true;
                    ledgeHook = desiredLedgeHook;
                    controller.targetDisplacement = targetPosition - transform.position;

                    // --- The more displacement, the greater the rotation change ---
                    cornerRotationLerpValue += controller.targetDisplacement.magnitude * ledgeCornerSpeed;

                    // --- Lerp towards the newly desired direction ---

                    if (previousHookNormal == Vector3.zero)
                        previousHookNormal = lookDirection;

                    lookDirection = Vector3.Lerp(previousHookNormal, lookDirection, cornerRotationLerpValue);
                    transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * Quaternion.FromToRotation(transform.forward, lookDirection), 1.0f);
                }
            }
            else
            {
                // --- Compute the position of a capsule equal to the character's at the end of the corner transition ---
                Vector3 position = transform.position;
                position += movingLeft ? -transform.right * 0.75f : transform.right * 0.75f;
                //position += transform.forward * 0.5f;

                // --- If it collides against any relevant collider, do not start the corner transition ---
                if (collided
                && ledgeGeometry.IsOutOfBounds(ref ledgeHook, 0.25f))
                {
                    if (distanceToCorner < minLedgeDistance
                        && hit.transform.GetComponent<TraverserClimbingObject>())
                    {
                        controller.targetDisplacement = targetPosition - transform.position;


                        cornerRotationLerpValue = cornerLerpInitialValue;
                        previousHookNormal = ledgeGeometry.GetNormal(ref ledgeHook);

                        ledgeGeometry.Initialize(hit.collider as BoxCollider);
                        ledgeHook = ledgeGeometry.GetHook(targetPosition);

                        ret = true;
                    }
                    else
                    {
                        controller.targetDisplacement = Vector3.zero;
                        ret = false;
                    }
                }
                else
                {
                    // ---  Store current hook normal and give an initial value to lerp value ---
                    cornerRotationLerpValue = cornerLerpInitialValue;
                    previousHookNormal = ledgeGeometry.GetNormal(ref ledgeHook);

                    ret = true;
                    ledgeHook = desiredLedgeHook;
                    controller.targetDisplacement = targetPosition - transform.position;
                }
            }

            float moveIntensity = abilityController.inputController.GetMoveIntensity();

            collided = Physics.SphereCast(rayOrigin, aimDebugSphereRadius, aimDirection, out hit, maxJumpRadius * moveIntensity, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);
            
            if (collided)
            {
                ledgeGeometry.ClosestPointDistance(hit.point, ref ledgeHook, ref distanceToCorner);
                //Debug.Log(distanceToCorner);
            }

            // --- Trigger a ledge to ledge transition if required by player ---
            if (moveIntensity > 0.1f && collided 
                && distanceToCorner > minLedgeDistance)
            {
                // --- Check if collided object is climbable ---
                if (hit.transform.GetComponent<TraverserClimbingObject>()
                    )
                {
                    // --- Trigger a transition and change state ---
                    auxledgeGeometry.Initialize(hit.collider as BoxCollider);

                    if (!auxledgeGeometry.IsEqual(ref ledgeGeometry))
                    {
                        // --- Adjust the aim position so we don't end in another ledge edge ---
                        TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHook(targetAimPosition - transform.forward * controller.capsuleRadius + aimDirection);

                        Vector3 matchPosition = auxledgeGeometry.GetPositionAtDistance(auxHook.index, 0.5f);
                        matchPosition -= Vector3.up * (controller.capsuleHeight - (animationController.skeleton.transform.position.y - transform.position.y));
                        matchPosition -= transform.forward * controller.capsuleRadius;

                        TraverserTransform contactTransform = TraverserTransform.Get(animationController.skeleton.transform.position, transform.rotation);
                        TraverserTransform targetTransform = TraverserTransform.Get(matchPosition, transform.rotation);

                        // --- Compute aim angle and decide which transition has to be triggered ---
                        float angle = Vector3.SignedAngle(transform.up, aimDirection, -transform.forward);

                        bool success;

                        if (Mathf.Abs(angle) < 45.0f)
                        {
                            success = animationController.transition.StartTransition(ref climbingData.HopUpTransitionData, ref contactTransform, ref targetTransform);
                        }
                        else if (angle >= 45.0f && angle <= 90.0f)
                        {
                            success = animationController.transition.StartTransition(ref climbingData.HopRightTransitionData, ref contactTransform, ref targetTransform);
                        }
                        else if (angle <= -45.0f && angle >= -90.0f)
                        {
                            success = animationController.transition.StartTransition(ref climbingData.HopLeftTransitionData, ref contactTransform, ref targetTransform);
                        }
                        else
                        {
                            success = animationController.transition.StartTransition(ref climbingData.HopDownTransitionData, ref contactTransform, ref targetTransform);
                        }

                        // --- Trigger ledge to ledge transition ---
                        if (success)
                        {
                            controller.targetDisplacement = Vector3.zero;

                            SetState(ClimbingAbilityState.LedgeToLedge);

                            if (ledgeGeometry.height == 0.0f)
                                ledgeGeometry.Initialize(ref auxledgeGeometry);

                            ledgeHook = auxledgeGeometry.GetHook(targetAimPosition - transform.forward * controller.capsuleRadius + aimDirection);

                            // --- Turn off/on controller ---
                            controller.ConfigureController(false);
                        }
                    }
                }
            }

            // --- Trigger a jump back transition if required by player ---
            if(abilityController.inputController.GetInputButtonSouth())
            {
                SetState(ClimbingAbilityState.JumpBack);

                // --- Turn off/on controller ---
                controller.ConfigureController(false);

                animationController.animator.CrossFade(climbingData.jumpBackAnimation.animationStateName, climbingData.jumpBackAnimation.transitionDuration, 0);
            }

            // --- Draw ledge geometry and hook ---
            //if (debugDraw)
            //{
            //    ledgeGeometry.DebugDraw();
            //    ledgeGeometry.DebugDraw(ref ledgeHook);
            //}

            return ret;
        }        

        // --------------------------------

        // --- Utilities ---    

        public void ResetClimbing()
        {
            movingLeft = false;
            previousHookNormal = ledgeGeometry.GetNormal(ref ledgeHook);
            cornerRotationLerpValue = 1.0f;
        }

        private bool IsCapsuleColliding(ref Vector3 start)
        {
            Vector3 end = start;
            end.y += controller.capsuleHeight;

            // --- Cast a capsule and return whether there has been a collision or not ---
            return Physics.CheckCapsule(start, end, controller.capsuleRadius, controller.characterCollisionMask);        
        }

        private TraverserTransform GetHangedTransform()
        {
            // --- Compute the character's position and rotation when hanging on a ledge ---
            ledgeHook = ledgeGeometry.GetHook(animationController.skeleton.position);
            Vector3 hookPosition = ledgeGeometry.GetPosition(ref ledgeHook);
            Quaternion hangedRotation = Quaternion.LookRotation(ledgeGeometry.GetNormal(ref ledgeHook), transform.up);
            Vector3 hangedPosition = hookPosition - ledgeGeometry.GetNormal(ref ledgeHook) * controller.capsuleRadius;
            hangedPosition.y = hookPosition.y - controller.capsuleHeight;
            return TraverserTransform.Get(hangedPosition, hangedRotation);
        }

        private TraverserTransform GetHangedSkeletonTransform()
        {
            // --- Compute the character's skeleton position and rotation when hanging on a ledge ---
            TraverserTransform skeletonTransform = GetHangedTransform();
            skeletonTransform.t.y += controller.capsuleHeight * 0.5f;
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

                    if (animationController.IsAdjustingSkeleton())
                    {
                        footPosition = animationController.leftFootPosition;
                        TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(footPosition);
                        footPosition.x = ledgeGeometry.GetPosition(ref hook).x;
                        footPosition.z = ledgeGeometry.GetPosition(ref hook).z;
                    }

                    RaycastHit hit;

                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.pullUpTransitionData.targetAnim)
                        && animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.3f)
                    {
                        if (Physics.Raycast(footPosition + Vector3.up - transform.forward*0.25f, Vector3.down, out hit, 2.0f, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point + Vector3.up *0.15f;
                        }

                        animationController.animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation);
                    }
                    else
                    {
                        //footPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.LeftFoot)));

                        if (Physics.Raycast(footPosition - transform.forward, transform.forward, out hit, 2.0f, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point;
                        }

                        footPosition -= transform.forward * footLength;
                        //footPosition.y = animationController.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;

                    }

                    Debug.DrawLine(footPosition, footPosition - transform.forward * 2.0f);
                    animationController.animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                }

                weight = animationController.animator.GetFloat("IKRightFootWeight");

                // --- Right foot ---
                if (weight > 0.0f)
                {
                    animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weight);
                    animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, weight);

                    Vector3 footPosition = animationController.animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                    if (animationController.IsAdjustingSkeleton())
                    {
                        footPosition = animationController.rightFootPosition;
                        TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(footPosition);
                        footPosition.x = ledgeGeometry.GetPosition(ref hook).x;
                        footPosition.z = ledgeGeometry.GetPosition(ref hook).z;
                    }

                    RaycastHit hit;

                    if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.pullUpTransitionData.targetAnim)
                        && animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.3f)
                    {
                        if (Physics.Raycast(footPosition + Vector3.up - transform.forward * 0.25f, Vector3.down, out hit, 2.0f, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point + Vector3.up * 0.15f;
                        }

                        animationController.animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation);
                    }
                    else
                    {
                        //footPosition = ledgeGeometry.GetPosition(ledgeGeometry.GetHook(animationController.animator.GetIKPosition(AvatarIKGoal.RightFoot)));

                        if (Physics.Raycast(footPosition - transform.forward, transform.forward, out hit, 2.0f, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                        {
                            footPosition = hit.point;
                        }

                        footPosition -= transform.forward * footLength;
                        //footPosition.y = animationController.animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y;
                    }

                    Debug.DrawLine(footPosition, footPosition - transform.forward * 2.0f);
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

                    if (animationController.IsAdjustingSkeleton())
                        IKPosition = animationController.leftHandPosition;

                    TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(IKPosition);

                    Vector3 handPosition = ledgeGeometry.GetPosition(ref hook);

                    handPosition -= transform.forward * handLength;
                    handPosition.y += handIKYDistance;

                    Debug.DrawLine(handPosition, handPosition - transform.forward * 2.0f);
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

                    if (animationController.IsAdjustingSkeleton())
                        IKPosition = animationController.rightHandPosition;

                    TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(IKPosition);


                    Vector3 handPosition = ledgeGeometry.GetPosition(ref hook);

                    handPosition -= transform.forward * handLength;
                    handPosition.y += handIKYDistance;

                    Debug.DrawLine(handPosition, handPosition - transform.forward * 2.0f);
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
            Gizmos.DrawWireSphere(ledgeCollisionPosition, aimDebugSphereRadius);
        }

        // -------------------------------------------------
    }
}

