using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]

    public class TraverserClimbingAbility : MonoBehaviour, TraverserAbility
    {
        // --- Attributes ---

        [Header("Animation")]
        [Tooltip("A reference to the locomotion ability's dataset (scriptable object asset).")]
        public TraverserClimbingData climbingData;

        [Header("Physics")]
        [Tooltip("A reference to the climbing ability's ragdoll profile. If not set, climbing wil use the default profile.")]
        public TraverserRagdollProfile ragdollProfile;
        [Tooltip("The profile used for free hang hops (ledge to ledge).")]
        public TraverserRagdollProfile freehangTransitionRagdollProfile;

        [Header("Ledge traversal settings")]
        [Tooltip("Desired speed in meters per second for ledge climbing.")]
        [Range(0.0f, 10.0f)]
        public float desiredLedgeSpeed = 0.5f;
        [Tooltip("Desired accleration in meters per second squared for ledge climbing.")]
        [Range(0.0f, 10.0f)]
        public float desiredLedgeAcceleration = 2.0f;
        [Tooltip("How fast the character will cover ledge corners.")]
        [Range(1.0f, 2.0f)]
        public float desiredCornerSpeed = 1.0f;
        [Tooltip("The closest the character will be able to get to a ledge corner, in meters.")]
        [Range(0.0f, 0.5f)]
        public float desiredCornerMaxDistance = 0.35f;
        [Tooltip("The maximum distance the character will be able to cover when climbing to a ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxClimbableHeight = 3.5f;

        [Header("Ledge to ledge settings")]
        [Tooltip("The maximum distance the character will be able to cover when jumping from ledge to ledge.")]
        [Range(0.0f, 5.0f)]
        public float maxLedgeDistance = 1.0f;
        [Tooltip("The minimum distance at which we trigger a ledge to ledge transition, below this we just move and corner.")]
        [Range(0.25f, 2.0f)]
        public float minLedgeDistance = 0.5f;
        [Tooltip("The size of the rays used to detect nearby ledges.")]
        [Range(1.0f, 3.0f)]
        public float nearbyLedgeRayLength = 2.0f;
        [Tooltip("The minimum distance at which we detect a nearby ledge, below this we just move and corner in the same ledge.")]
        public float nearbyLedgeMaxDistance = 0.3f;

        [Header("Freehang settings")]
        [Tooltip("How fast the character transitions into freehang.")]
        public float freeHangTransitionSpeed = 1.0f;

        [Header("Aim IK settings")]
        [Tooltip("How fast the head rotates towards the target direction. Used in ledge to ledge.")]
        public float headAimIKSpeed = 1.0f;
        [Tooltip("How fast the body rotates towards the target direction. Used in ledge to ledge.")]
        public float bodyAimIKSpeed = 1.0f;

        [Header("Feet IK settings")]
        [Tooltip("Activates or deactivates foot IK placement for the climbing ability.")]
        public bool fIKOn = true;
        [Tooltip("The character's foot length (size in meters).")]
        [Range(0.0f, 1.0f)]
        public float footLength = 1.0f;
        [Tooltip("The length of the ray used to detect surfaces to place the feet on.")]
        public float feetRayLength = 2.0f;
        [Tooltip("Modifies how fast we adjust the feet to match IK transforms.")]
        [Range(0.0f, 100.0f)]
        public float feetAdjustmentSpeed = 0.5f;

        [Header("Hands IK settings")]
        [Tooltip("Activates or deactivates hand IK placement for the climbing ability.")]
        public bool hIKOn = true;
        [Tooltip("The Y distance to correct in order to place the hands on the ledge.")]
        public float handIKYDistance = 1.0f;
        [Tooltip("The character's hand length (size in meters) / correction in forward direction.")]
        [Range(-1.0f, 1.0f)]
        public float handLength = 1.0f;
        [Tooltip("The length of the ray used to detect surfaces to place the hand on.")]
        public float handRayLength = 1.0f;
        [Tooltip("Modifies how fast we adjust the hands to match IK transforms.")]
        [Range(0.0f, 100.0f)]
        public float handsAdjustmentSpeed = 0.5f;
        [Tooltip("Modifies how fast we adjust the hands to match Aim IK rotation.")]
        [Range(0.0f, 1.0f)]
        public float handsAimIKIntensity = 0.25f;

        [Header("Debug")]
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Selecting the object may be required to show debug geometry.")]
        public bool debugDraw = false;
        [Tooltip("Indicates the point the character is aiming to, used to trigger a ledge to ledge transition.")]
        [Range(0.0f, 1.0f)]
        public float aimDebugSphereRadius = 0.25f;

        // --------------------------------

        // --- Ability-level state ---
        public enum ClimbingState
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

        // --- Private Variables ---

        private TraverserAbilityController abilityController;
        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserLocomotionAbility locomotionAbility;

        private ClimbingState state; 

        // --- Ledge movement ---
        private float currentLedgeSpeed = 0.0f;

        // --- Jump hangs ---
        private float maxDistance = 2.0f;
        private float maxYDifference = 0.5f;

        // --- Procedural corners ---
        private Vector3 previousHookNormal;
        private float cornerRotationLerpValue = 0.0f;
        private float cornerLerpInitialValue = 0.0f;
        private bool movingLeft = false;
        private float cornersBoundsOffset = 0.1f;

        // --- Mocing ledges ---
        private Vector3 movableLedgeDelta;

        // --- Character will be adjusted to movable ledge if target animation transition is above this normalized time ---
        private float ledgeAdjustmentLimitTime = 0.25f;

        // --- Ledge geometry will be changed to new one after this normalized time has passed in the target animation ---
        private float timeToLedgeToLedge = 0.35f;

        // --- Indicates if we have detected a valid ledge to ledge transition ---
        private bool ledgeDetected = false;

        // --- Offsets height of hanged transform to match a new position (upper or lower) --- 
        private float hangedTransformHeightRatio = 0.55f;

        // --- Nearby corners ---
        private bool leftIsMax = false;
        private Vector3 targetDirection = Vector3.zero;
        private bool onNearbyLedgeTransition = false;

        // --- Debug draw ---
        private Vector3 nearbyLedgeCollisionPosition = Vector3.zero;
        private Vector3 ltlRayOrigin = Vector3.zero;
        private Vector3 ltlRayDestination = Vector3.zero;
        // --- The position at which we are currently aiming to, determined by maxJumpRadius ---
        private Vector3 targetAimPosition;

        // --- Free hang ---
        private bool rightLegFreeHang = false;
        private bool leftLegFreeHang = false;
        private float freehangWeight = 0.0f;
        private float handIKYFreehang = -0.05f;
        private float handIKLengthFreehang = -0.02f;

        // --- IK ---
        private Vector3 previousLeftFootPosition = Vector3.zero;
        private Vector3 previousRightFootPosition = Vector3.zero;
        private Vector3 previousRightHandPosition = Vector3.zero;
        private Vector3 previousLeftHandPosition = Vector3.zero;

        private Quaternion previousRightHandRotation = Quaternion.identity;
        private Quaternion previousLeftHandRotation = Quaternion.identity;

        // --------------------------------

        // --- World interactable elements ---

        private TraverserLedgeObject.TraverserLedgeGeometry previousLedgeGeometry;
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

            state = ClimbingState.Suspended;

            previousLedgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            ledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            auxledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();
            ledgeHook = TraverserLedgeObject.TraverserLedgeHook.Create();
        }

        private void LateUpdate()
        {
            // --- Free hang, update weight ---
            // --- We update it even when in other abilities so the weight value is valid when jump hanging to ledges ---
            int hash;
            animationController.animatorParameters.TryGetValue("FreeHangWeight", out hash);

            if (hash != 0)
                animationController.animator.SetFloat(hash, freehangWeight);

            // --- Perform a transition to free hanging if legs cannot find a surface ---
            if (leftLegFreeHang && rightLegFreeHang 
                && state != ClimbingState.DropDown
                && state != ClimbingState.Dismount)
            {
                freehangWeight = Mathf.Lerp(freehangWeight, 1.0f, freeHangTransitionSpeed * Time.deltaTime);
            }
            else if (freehangWeight > 0.0f)
            {
                // --- Return to braced hanging ---
                freehangWeight = Mathf.Lerp(freehangWeight, 0.0f, freeHangTransitionSpeed * Time.deltaTime);

                if (freehangWeight < 0.05)
                    freehangWeight = 0.0f;
            }

            // --- Return if climbing is not the active ability ---
            if (!abilityController.isCurrent(this))
            {
                freehangWeight = 0.0f;
                return;
            }

            // --- Adapt to moving ledges ---

            if (animationController.transition.isON
                &&
                (!animationController.animator.IsInTransition(0)
                || animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > ledgeAdjustmentLimitTime)
                && !animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.pullUpTransitionData.targetAnim)
                && !animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.dropDownTransitionData.targetAnim))
            {
                // --- Update ledge if it moves, but do not teleport controller, warping will handle it ---
                ledgeGeometry.UpdateLedge();
                movableLedgeDelta = auxledgeGeometry.UpdateLedge();
            }
            else
            {
                // --- Update ledge if it moves, teleport controller to follow ledge ---
                auxledgeGeometry.UpdateLedge();
                movableLedgeDelta = ledgeGeometry.UpdateLedge();
                controller.TeleportTo(transform.position + movableLedgeDelta);
            }

            // --- Notify transition handler to adapt motion warping to new destination
            if (state != ClimbingState.Dismount)
            {
                animationController.transition.SetDestinationOffset(ref movableLedgeDelta);
            }

            // --- Free hang, adjust hips ---

            // --- Perform a transition to free hanging if legs cannot find a surface ---

            if (leftLegFreeHang && rightLegFreeHang)
                animationController.animator.GetBoneTransform(HumanBodyBones.Hips).position = animationController.skeletonPos;
            else if (freehangWeight > 0.0f)
            {
                // --- Return to braced hanging ---
                animationController.animator.GetBoneTransform(HumanBodyBones.Hips).position = animationController.skeletonPos;

                if (freehangWeight < 0.05)
                    freehangWeight = 0.0f;
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

            // --- If a ledge was detected, trigger Aim IK ---
            if (ledgeDetected)
            {
                animationController.spineRig.weight = Mathf.Lerp(animationController.spineRig.weight, 1.0f, bodyAimIKSpeed * Time.deltaTime);

                Vector2 leftStickInput = abilityController.inputController.GetInputMovement();
                float moveIntensity = abilityController.inputController.GetMoveIntensity();
                Vector3 aimDirection = transform.right * leftStickInput.x + transform.up * leftStickInput.y;
                aimDirection.Normalize();

                if(moveIntensity > 0.1f)
                    animationController.aimRigEffector.transform.localPosition = animationController.aimEffectorOGTransform.t + transform.InverseTransformDirection(aimDirection) * moveIntensity * headAimIKSpeed;
            }
            else
            {
                // --- Progressively deactivate Aim IK ---
                animationController.spineRig.weight = Mathf.Lerp(animationController.spineRig.weight, 0.0f, bodyAimIKSpeed * Time.deltaTime);

                if (animationController.spineRig.weight < 0.1f)
                    animationController.aimRigEffector.transform.localPosition = animationController.aimEffectorOGTransform.t;
            }

            return this;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            TraverserAbility ret = this;

            // --- If character is not falling ---
            if (!IsState(ClimbingState.Suspended))
            {
                // --- Handle current state ---
                switch (state)
                {
                    case ClimbingState.Mounting:
                        HandleMountingState();
                        break;        

                    case ClimbingState.Dismount:
                        HandleDismountState();
                        break;

                    case ClimbingState.PullUp:
                        HandlePullUpState();
                        break;

                    case ClimbingState.DropDown:
                        HandleDropDownState();
                        break;

                    case ClimbingState.LedgeToLedge:
                        HandleLedgeToLedgeState();
                        break;

                    case ClimbingState.JumpBack:
                        HandleJumpBackState();
                        break;

                    case ClimbingState.Climbing:
                        HandleClimbingState(deltaTime);
                        break;

                    default:
                        break;
                }

                // --- Let other abilities handle the situation ---
                if (IsState(ClimbingState.Suspended))
                    ret = null;
            }

            return ret;
        }

        public bool OnContact(ref TraverserTransform contactTransform, float deltaTime)
        {
            bool ret = false;

            //---If we make contact with a climbable surface and player issues climb order, mount ---         
            BoxCollider collider = controller.current.collider as BoxCollider;

            if (collider == null)
                return ret;

            if (abilityController.inputController.GetInputButtonEast()
                && IsState(ClimbingState.Suspended)
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
                    TraverserTransform hangedTransform = GetHangedSkeletonTransform(animationController.skeletonPos, ref auxledgeGeometry, ref ledgeHook);

                    // --- Offset contact transform ---
                    contactTransform.t -= transform.forward * climbingData.mountTransitionData.contactOffset;
                    hangedTransform.t -= transform.forward * climbingData.mountTransitionData.targetOffset;

                    // --- Require a transition ---
                    ret = animationController.transition.StartTransition(ref climbingData.mountTransitionData, ref contactTransform, ref hangedTransform);

                    // --- If transition start is successful, change state ---
                    if (ret)
                    {
                        previousLedgeGeometry = ledgeGeometry;
                        ledgeGeometry.Initialize(ref collider);
                        SetState(ClimbingState.Mounting);
                        controller.targetVelocity = Vector3.zero;
                    }
                }
            }
            else if ((locomotionAbility.GetLocomotionState() == TraverserLocomotionAbility.LocomotionAbilityState.Falling
                || locomotionAbility.GetLocomotionState() == TraverserLocomotionAbility.LocomotionAbilityState.Jumping)
                && (contactTransform.t - (controller.snapshot.position + Vector3.up* controller.capsuleHeight)).magnitude < maxDistance
                && collider.gameObject.GetComponent<TraverserClimbingObject>() != null)
            {
                // ---  We are falling and colliding against a ledge ---
                auxledgeGeometry.Initialize(ref collider);
                TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHook(contactTransform.t);

                TraverserTransform hangedTransform = GetHangedSkeletonTransform(animationController.skeletonPos, ref auxledgeGeometry, ref ledgeHook);
                Vector3 hookPos = auxledgeGeometry.GetPosition(ref auxHook);
                Vector3 difference = (hookPos - (controller.snapshot.position + Vector3.up * controller.capsuleHeight));

                bool collided = AdjustToFreehang(ref auxledgeGeometry, ref auxHook, ref hangedTransform.t, transform.forward);

                // --- Decide whether to trigger a short or large hang transition ---
                if (collided
                    && difference.magnitude > maxDistance / 2.0f
                    && difference.y < maxYDifference)
                {
                    animationController.animator.Play(climbingData.fallTransitionAnimation.animationStateName, 0);
                    ret = animationController.transition.StartTransition(ref climbingData.jumpHangTransitionData, ref contactTransform, ref hangedTransform);
                }
                else if (difference.y < maxYDifference
                    && (difference.magnitude <= maxDistance / 2.0f || !collided))
                {
                    animationController.animator.Play(climbingData.fallTransitionAnimation.animationStateName, 0);
                    ret = animationController.transition.StartTransition(ref climbingData.jumpHangShortTransitionData, ref contactTransform, ref hangedTransform);
                }

                // --- If transition start is successful, change state ---
                if (ret)
                {
                    previousLedgeGeometry = ledgeGeometry;
                    ledgeGeometry.Initialize(ref auxledgeGeometry);
                    SetState(ClimbingState.LedgeToLedge);
                    controller.targetVelocity = Vector3.zero;
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
                    Vector3 skeletonPosition = animationController.skeletonPos;
                    Vector3 hookPosition = auxledgeGeometry.GetPosition(ref auxHook);
                    hookPosition.y += (skeletonPosition.y - transform.position.y);
                    Quaternion hookRotation = Quaternion.LookRotation(-auxledgeGeometry.GetNormal(auxHook.index), transform.up);
                    TraverserTransform contactTransform = TraverserTransform.Get(hookPosition, hookRotation);
                    contactTransform.t -= transform.forward * climbingData.dropDownTransitionData.contactOffset;

                    // --- Get target transform ---
                    TraverserTransform hangedTransform = GetHangedSkeletonTransform(skeletonPosition, ref auxledgeGeometry, ref auxHook);

                    AdjustToFreehang(ref auxledgeGeometry, ref auxHook, ref hangedTransform.t, -transform.forward);

                    // --- If capsule collides against any relevant collider, do not start the dropDown transition ---
                    if (!Physics.CheckCapsule(hangedTransform.t, hangedTransform.t + Vector3.up * controller.capsuleHeight, controller.capsuleRadius, controller.characterCollisionMask))
                    {
                        hangedTransform.q = hookRotation;

                        // --- Require a transition ---
                        ret = animationController.transition.StartTransition(ref climbingData.dropDownTransitionData ,ref contactTransform, ref hangedTransform);

                        if (ret)
                        {
                            previousLedgeGeometry = ledgeGeometry;
                            ledgeGeometry.Initialize(ref auxledgeGeometry);
                            ledgeHook = auxHook;
                            controller.targetVelocity = Vector3.zero;
                            SetState(ClimbingState.DropDown);
                        }
                    }
                }
            }

            return ret;
        }

        public void OnEnter()
        {
            previousLeftFootPosition = animationController.leftFootPos;
            previousRightFootPosition = animationController.rightFootPos;
            previousLeftHandPosition = animationController.leftHandPos;
            previousRightHandPosition = animationController.rightHandPos;

            previousLeftHandRotation = animationController.animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation;
            previousRightHandRotation = animationController.animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;

            if (abilityController.ragdollController != null)
                abilityController.ragdollController.SetRagdollProfile(ref ragdollProfile);
        }

        public void OnExit()
        {
            ledgeGeometry.Reset();
            previousLedgeGeometry.Reset();

            // --- Turn off/on controller ---
            controller.ConfigureController(true);

            if (abilityController.ragdollController != null)
                abilityController.ragdollController.SetDefaultRagdollProfile();
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
                SetState(ClimbingState.Climbing); 
                controller.ConfigureController(false);
            }
        }

        void HandleDismountState()
        {
            if (!animationController.transition.isON)
            {
                SetState(ClimbingState.Suspended);
                locomotionAbility.ResetMovement();
                animationController.animator.CrossFade(climbingData.locomotionOnAnimation.animationStateName, climbingData.locomotionOnAnimation.transitionDuration, 0);
            }
        }

        void HandlePullUpState()
        {
            if (!animationController.transition.isON)
            {
                SetState(ClimbingState.Suspended);
                locomotionAbility.ResetMovement();
                animationController.animator.CrossFade(climbingData.locomotionOnAnimation.animationStateName, climbingData.locomotionOnAnimation.transitionDuration, 0);
            }
        }

        void HandleDropDownState()
        {
            if (!animationController.transition.isON)
            {
                SetState(ClimbingState.Climbing);
                TraverserTransform hangedTransform = GetHangedTransform(animationController.skeletonPos, ref ledgeGeometry, ref ledgeHook);
                controller.ConfigureController(false);
                animationController.animator.Play(climbingData.ledgeIdleAnimation.animationStateName);
                //Quaternion backupRot = animationController.hipsRef.rotation;
                transform.rotation = hangedTransform.q;
                //animationController.hipsRef.rotation = backupRot;
            }
        }

        void HandleLedgeToLedgeState()
        {
            // --- Initialize ledge once above timeToLedgeToLedge ---
            if (animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > timeToLedgeToLedge
                && animationController.transition.isTargetON
                && !animationController.animator.IsInTransition(0)
                && !ledgeGeometry.IsEqual(ref auxledgeGeometry))
            {
                previousLedgeGeometry = ledgeGeometry;
                ledgeGeometry.Initialize(ref auxledgeGeometry);
            }

            if (!animationController.transition.isON)
            {
                if (!ledgeGeometry.IsEqual(ref auxledgeGeometry))
                {
                    previousLedgeGeometry = ledgeGeometry;
                    ledgeGeometry.Initialize(ref auxledgeGeometry);
                }

                SetState(ClimbingState.Climbing);
                controller.ConfigureController(false);

                if (abilityController.ragdollController != null)
                    abilityController.ragdollController.SetRagdollProfile(ref ragdollProfile);
            }
        }

        void HandleJumpBackState()
        {
            if (!animationController.transition.isON)
            {
                SetState(ClimbingState.Suspended);
                locomotionAbility.ResetMovement();
                animationController.animator.Play(climbingData.fallLoopAnimation.animationStateName, 0);
                locomotionAbility.SetLocomotionState(TraverserLocomotionAbility.LocomotionAbilityState.Falling);
                transform.rotation = Quaternion.LookRotation(-ledgeGeometry.GetNormal(ledgeHook.index),Vector3.up);
            }
        }

        void HandleClimbingState(float deltaTime)
        {
            // --- Handle ledge climbing/movement direction ---
            bool canMove = UpdateClimbing(deltaTime);

            // --- Stop controller ---
            if (!canMove)
            {
                controller.targetVelocity = Vector3.zero;
                controller.targetDisplacement = Vector3.zero;
            }

            // --- Throw a ray downwards to check for ground ---
            RaycastHit hit;

            bool closeToDrop = Physics.Raycast(transform.position, Vector3.down, out hit ,maxClimbableHeight - controller.capsuleHeight, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);

            if(debugDraw)
                Debug.DrawRay(transform.position, Vector3.down * (maxClimbableHeight - controller.capsuleHeight), Color.yellow);

            // --- The position at which we perform a capsule check to prevent pulling up into a wall --- 
            Vector3 pullupPosition = ledgeGeometry.GetPosition(ref ledgeHook);
            pullupPosition += Vector3.up * controller.capsuleHeight * hangedTransformHeightRatio;
            pullupPosition += transform.forward * climbingData.pullUpTransitionData.targetOffset;

            // --- React to pull up ---
            if (abilityController.inputController.GetInputButtonNorth() 
                && abilityController.inputController.GetInputMovement().y > 0.5f &&
                state != ClimbingState.LedgeToLedge && !IsCapsuleColliding(ref pullupPosition)
                && freehangWeight == 0.0f)
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
                    SetState(ClimbingState.PullUp);
                    controller.targetDisplacement = Vector3.zero;
                }
            }
            else if (closeToDrop && abilityController.inputController.GetInputButtonEast() /*&& freehangWeight == 0.0f*/)
            {
                // --- React to dismount ---
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
                    SetState(ClimbingState.Dismount);
                    controller.targetDisplacement = Vector3.zero;
                }
            }
            else if (abilityController.inputController.GetInputButtonSouth()
                && freehangWeight == 0.0f)
            {
                // --- Trigger a jump back transition if required by player ---

                TraverserTransform contactTransform = TraverserTransform.Get(animationController.skeletonPos, transform.rotation);
                
                // --- Offset target transform ---
                TraverserTransform targetTransform = contactTransform;
                targetTransform.t.y += 0.5f;
                targetTransform.t -= transform.forward * climbingData.jumpBackTransitionData.targetOffset;

                bool success = animationController.transition.StartTransition(ref climbingData.jumpBackTransitionData, ref contactTransform, ref targetTransform);

                if (success)
                {
                    SetState(ClimbingState.JumpBack);
                    controller.targetDisplacement = Vector3.zero;
                }
            }
        }

        bool UpdateClimbing(float deltaTime)
        {
            bool canMove = false;

            Vector2 leftStickInput = abilityController.inputController.GetInputMovement();

            // --- Accelerate movement ---
            currentLedgeSpeed += (leftStickInput.x - currentLedgeSpeed) * desiredLedgeAcceleration * deltaTime;
            currentLedgeSpeed = Mathf.Clamp(currentLedgeSpeed, -1.0f, 1.0f);
            controller.targetVelocity.x = currentLedgeSpeed;

            // --- Store last given player direction ---
            if (leftStickInput.x != 0.0f)
            {
                movingLeft = leftStickInput.x < 0.0f;
                canMove = true;
            }

            // --- Given input and movement, compute target position ---
            Vector3 direction = (transform.right * leftStickInput.x) /*+ transform.forward - ledgeGeometry.GetNormal(ledgeHook.index)*/ /*ledgeGeometry.GetNormal(ledgeHook) - transform.right*/;
            direction.Normalize();

            if(debugDraw)
                Debug.DrawLine(ledgeGeometry.GetPosition(ref ledgeHook), ledgeGeometry.GetPosition(ref ledgeHook) + direction * 5.0f);

            Vector3 delta = direction * Mathf.Abs(leftStickInput.x) * desiredLedgeSpeed * Mathf.Abs(currentLedgeSpeed) * deltaTime;
            Vector3 targetPosition = transform.position + delta;

            // --- Update hook ---
            TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook = ledgeGeometry.UpdateHook(ref ledgeHook, targetPosition, movingLeft);

            // --- Handle movement on the same ledge, including corners ---
            if (!onNearbyLedgeTransition)
                canMove = OnLedgeMovement(ref desiredLedgeHook, targetPosition);

            // --- Handle nearby ledge player controlled transition ---
            OnNearbyLedgeMovement(ref desiredLedgeHook, targetPosition);

            // --- Handle ledge to ledge transition, if ledge is detected ---
            if (!onNearbyLedgeTransition && cornerRotationLerpValue <= cornerLerpInitialValue /*&& freehangWeight == 0.0f*/)
                OnLedgeToLedge();

            return canMove;
        }
        
        public bool OnLedgeMovement(ref TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook, Vector3 targetPosition)
        {
            bool canMove = false;

            // --- Update movement while far enough from the edge's corners ---
            if (!ledgeGeometry.IsOutOfBounds(ref desiredLedgeHook, desiredCornerMaxDistance - cornersBoundsOffset))
            {
                ledgeHook = desiredLedgeHook;
                controller.targetDisplacement = targetPosition - transform.position;
                cornerRotationLerpValue = cornerLerpInitialValue;
                targetDirection = ledgeGeometry.GetNormal(movingLeft ? ledgeGeometry.GetNextEdgeIndex(ledgeHook.index) : ledgeGeometry.GetPreviousEdgeIndex(ledgeHook.index));
                previousHookNormal = ledgeGeometry.GetNormal(ledgeHook.index);
                leftIsMax = movingLeft;
                canMove = true;
            }
            else         
            {
                // --- Throw a ray to check for a corner collision ---
                RaycastHit hit;
                Vector3 rayOrigin = ledgeGeometry.GetPosition(ref ledgeHook) - transform.forward*0.1f - Vector3.up * 0.1f;
                
                if(movingLeft)
                    rayOrigin += transform.right * -0.25f;
                else
                    rayOrigin += transform.right * 0.25f;

                if (debugDraw)
                    Debug.DrawLine(rayOrigin,  rayOrigin + transform.forward);

                bool collided = Physics.Raycast(rayOrigin, transform.forward, out hit, 1.0f, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);
                BoxCollider collider = hit.collider as BoxCollider;

                // --- Stop movement on collision
                if (collided && !ledgeGeometry.IsEqual(ref collider))
                {
                    canMove = false;
                }
                else if (movableLedgeDelta == Vector3.zero)
                {
                    ledgeDetected = false;
                    ledgeHook = desiredLedgeHook;
                    UpdateCornerMovement(targetPosition);
                    canMove = true;
                }

            }

            return canMove;
        }

        public void OnNearbyLedgeMovement(ref TraverserLedgeObject.TraverserLedgeHook desiredLedgeHook, Vector3 targetPosition)
        {
            // --- Check for ledge towards the character's movement direction ---
            RaycastHit hit;
            Vector3 direction = movingLeft ? -transform.right : transform.right;
            Vector3 castDirection = direction * 0.5f;
            Vector3 castOrigin = transform.position + Vector3.up * controller.capsuleHeight * 0.75f;
            castOrigin += (-transform.forward * controller.capsuleRadius) + castDirection;
            nearbyLedgeCollisionPosition = castOrigin;

            bool collided = Physics.Raycast(nearbyLedgeCollisionPosition, transform.forward, out hit, nearbyLedgeRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);
            BoxCollider hitCollider = hit.collider as BoxCollider;

            if (debugDraw)
                Debug.DrawLine(nearbyLedgeCollisionPosition, nearbyLedgeCollisionPosition + transform.forward * nearbyLedgeRayLength, Color.yellow);

            // --- Use another raycast perpendicular to the first for lateral ledges ---
            if ((collided && ledgeGeometry.IsEqual(ref hitCollider)) || !collided)
            {
                castOrigin -= castDirection;

                collided = Physics.Raycast(castOrigin, direction, out hit, nearbyLedgeRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);
                hitCollider = hit.collider as BoxCollider;

                if (debugDraw)
                    Debug.DrawLine(castOrigin, castOrigin + direction * nearbyLedgeRayLength, Color.yellow);
            }

            float distanceToCorner = ledgeHook.distance;
            float distanceToLedge = minLedgeDistance;

            // --- Compute distance to corner given movement direction ---
            if (collided)
            {
                if (movingLeft)
                    distanceToCorner = ledgeGeometry.GetLength(ledgeHook.index) - ledgeHook.distance;
                else
                    distanceToCorner = ledgeHook.distance;

                // --- Also compute distance to collided ledge ---
                ledgeGeometry.ClosestPointDistance(hit.point, ref ledgeHook, ref distanceToLedge);
            }

            // --- If another ledge is found nearby, trigger seamless player controlled transition ---
            if (collided 
                && !ledgeGeometry.IsEqual(ref hitCollider) 
                && abilityController.inputController.GetInputMovement().x != 0.0f
                && distanceToCorner <= desiredCornerMaxDistance
                && distanceToLedge < nearbyLedgeMaxDistance
                && movableLedgeDelta == Vector3.zero
                && hit.transform.GetComponent<TraverserClimbingObject>())
            {
                // --- Create geometry given new ledge ---
                previousHookNormal = ledgeGeometry.GetNormal(ledgeHook.index);
                previousLedgeGeometry = ledgeGeometry;
                ledgeGeometry.Initialize(ref hitCollider);

                // --- Get most relevant hook in the character's movement direction ---
                // --- We do not want the closest, but the most relevant !!! ---
                ledgeHook = ledgeGeometry.GetHook(transform.position + direction - transform.forward * 0.5f);

                // --- Adjust hook distance, which is far from the current position due to the trick above ---
                if (movingLeft)
                    ledgeHook.distance = 0.0f;
                else
                    ledgeHook.distance = ledgeGeometry.GetLength(ledgeHook.index);

                // --- Update control variables ---
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
                ledgeDetected = false;

                // --- Keep the hook at the same edge ---
                if (ledgeHook.index == desiredLedgeHook.index)
                    ledgeHook = desiredLedgeHook;

                UpdateCornerMovement(targetPosition);

                // --- End nearby transition after reaching targetDirection ---
                if (cornerRotationLerpValue > 1.0f)
                    onNearbyLedgeTransition = false;
            }
        }

        public void UpdateCornerMovement(Vector3 targetPosition)
        {
            controller.targetDisplacement = targetPosition - transform.position;

            // --- The more displacement, the greater the rotation change ---
            if (leftIsMax)
            {
                if (movingLeft)
                    cornerRotationLerpValue += controller.targetDisplacement.magnitude * desiredCornerSpeed;
                else
                    cornerRotationLerpValue -= controller.targetDisplacement.magnitude * desiredCornerSpeed;
            }
            else
            {
                if (movingLeft)
                    cornerRotationLerpValue -= controller.targetDisplacement.magnitude * desiredCornerSpeed;
                else
                    cornerRotationLerpValue += controller.targetDisplacement.magnitude * desiredCornerSpeed;
            }

            // --- Update rotation ---
            Vector3 lookDirection = Vector3.Lerp(previousHookNormal, targetDirection, cornerRotationLerpValue);
            transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * Quaternion.FromToRotation(transform.forward, lookDirection), 1.0f);
        }

        public void OnLedgeToLedge()
        {
            Vector2 leftStickInput = abilityController.inputController.GetInputMovement();
            float moveIntensity = abilityController.inputController.GetMoveIntensity();

            // --- Given input, compute target aim Position (used to trigger ledge to ledge transitions) ---
            Vector3 aimDirection = transform.right * leftStickInput.x + transform.up * leftStickInput.y;
            aimDirection.Normalize();

            Vector3 rayOrigin = transform.position + transform.up * controller.capsuleHeight * 0.75f
                + transform.forward * controller.capsuleRadius;

            targetAimPosition = rayOrigin + aimDirection * maxLedgeDistance * moveIntensity;

            // --- Check for ledge ---
            RaycastHit hit;

            bool collided = Physics.SphereCast(rayOrigin, aimDebugSphereRadius, aimDirection, out hit, 
                maxLedgeDistance * moveIntensity, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);
            float distanceToCorner = 0.0f;
            BoxCollider hitCollider = hit.collider as BoxCollider;

            // --- Compute distance from edge corner to new ledge hit point ---
            if (collided && !ledgeGeometry.IsEqual(ref hitCollider) && hit.transform.GetComponent<TraverserClimbingObject>())
            {
                ledgeGeometry.ClosestPointDistance(hit.point, ref ledgeHook, ref distanceToCorner);

                if(distanceToCorner > minLedgeDistance)
                    ledgeDetected = true;
            }
            else
            {
                ledgeDetected = false;
            }

            // --- Trigger a ledge to ledge transition if required by player ---
            if (abilityController.inputController.GetInputButtonWest()
                && moveIntensity > 0.1f && collided
                && distanceToCorner > minLedgeDistance) 
            {
                auxledgeGeometry.Initialize(ref hitCollider);

                if (!auxledgeGeometry.IsEqual(ref ledgeGeometry))
                {
                    // --- Adjust the aim position so we don't end in another ledge edge ---
                    Vector3 ledgePos = hit.collider.bounds.center - transform.forward * hit.collider.bounds.size.magnitude;
                    TraverserLedgeObject.TraverserLedgeHook auxHook = auxledgeGeometry.GetHookAt(ledgePos, 0.5f);
                    TraverserTransform contactTransform = TraverserTransform.Get(animationController.skeletonPos, transform.rotation);

                    Vector3 hookPosition = auxledgeGeometry.GetPosition(ref auxHook) - auxledgeGeometry.GetNormal(auxHook.index) * controller.capsuleRadius;
                    hookPosition.y -= controller.capsuleHeight * (1.0f - hangedTransformHeightRatio);

                    TraverserTransform hangedTransform = TraverserTransform.Get(hookPosition, Quaternion.LookRotation(auxledgeGeometry.GetNormal(auxHook.index), transform.up)); 

                    // Overriding rayOrigin and hit variables from above! 
                    rayOrigin = transform.position + Vector3.up * controller.capsuleHeight * 0.5f;
                    Vector3 probeDirection = hangedTransform.t - rayOrigin;

                    // --- Store positions for debug draw ---
                    ltlRayOrigin = rayOrigin;
                    ltlRayDestination = ltlRayOrigin + probeDirection;

                    // --- Throw a ray in target direction and if a collider is found prevent ledge to ledge ---
                    if (Physics.Raycast(ltlRayOrigin, probeDirection.normalized, out hit, probeDirection.magnitude,controller.characterCollisionMask,QueryTriggerInteraction.Ignore))
                    {
                        ledgePos = transform.position;
                        auxHook = auxledgeGeometry.GetHookAt(ledgePos, 0.35f);
                        hookPosition = auxledgeGeometry.GetPosition(ref auxHook) - auxledgeGeometry.GetNormal(auxHook.index) * controller.capsuleRadius;
                        hookPosition.y -= controller.capsuleHeight * (1.0f - hangedTransformHeightRatio);
                        hangedTransform = TraverserTransform.Get(hookPosition, Quaternion.LookRotation(auxledgeGeometry.GetNormal(auxHook.index), transform.up));

                        probeDirection = hangedTransform.t - rayOrigin;
                        ltlRayDestination = ltlRayOrigin + probeDirection;

                        // --- If there is a collision, try to jump to the previous edge, else prevent ledge to ledge ---
                        if (Physics.Raycast(ltlRayOrigin, probeDirection.normalized, out hit, probeDirection.magnitude, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
                            return;
                    }

                    // --- Offset warp point ---
                    AdjustToFreehang(ref auxledgeGeometry, ref auxHook, ref hangedTransform.t, transform.forward);

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

                        SetState(ClimbingState.LedgeToLedge);

                        if (!ledgeGeometry.IsInitialized())
                        {
                            previousLedgeGeometry = ledgeGeometry;
                            ledgeGeometry.Initialize(ref auxledgeGeometry);
                        }

                        ledgeHook = auxHook;
                        ledgeDetected = false;

                        if (abilityController.ragdollController != null && freehangWeight != 0.0f)
                            abilityController.ragdollController.SetRagdollProfile(ref freehangTransitionRagdollProfile);
                    }
                }
                
            }
        }

        // --------------------------------

        // --- Utilities ---    

        private bool AdjustToFreehang(ref TraverserLedgeObject.TraverserLedgeGeometry ledgeGeom, ref TraverserLedgeObject.TraverserLedgeHook ledgeHk, ref Vector3 position, Vector3 endDirection)
        {
            // --- Throw a ray to check for surface, and decide if we are performing a transition to free hang ---
            RaycastHit hit;
            bool collided = Physics.Raycast(position, endDirection, out hit, maxDistance * 0.5f, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);

            if (debugDraw)
                Debug.DrawLine(position, position + endDirection * maxDistance * 1.5f);

            if(!collided)
                position += ledgeGeom.GetNormal(ledgeHk.index) * 0.25f - Vector3.up * 0.3f;

            return collided;
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
            Vector3 hangedPosition = hookPosition - ledgeGeom.GetNormal(ledgeHk.index) * controller.capsuleRadius * 1.25f;
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

        public void SetState(ClimbingState newState)
        {
            state = newState;
        }

        public bool IsState(ClimbingState queryState)
        {
            return state == queryState;
        }

        // --------------------------------

        // --- Events ---

        private void OnAnimatorIK(int layerIndex)
        {
            if (!abilityController.isCurrent(this))
                return;

            // --- Ensure IK is not activated during a transition between two different abilities (locomotion - mount) ---
            if ((animationController.animator.IsInTransition(0) 
                && animationController.animator.GetNextAnimatorStateInfo(0).IsName(climbingData.mountTransitionData.targetAnim))
                || (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionAbility.locomotionData.locomotionONAnimation.animationStateName)
                || animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(climbingData.mountTransitionData.transitionAnim)))
                return;

            // --- Set weights to 0 and return if IK is off ---
            if (!fIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
                animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0.0f);
            }
            else
            {
                // --- Else, sample foot weight from the animator's parameters, which are set by the animations themselves through curves ---

                // --- Left foot ---
                float weight = animationController.animator.GetFloat("IKLeftFootWeight");
                AdjustFootIK(AvatarIKGoal.LeftFoot, ref previousLeftFootPosition, animationController.leftFootPos, ref leftLegFreeHang, weight);

                // --- Right foot ---
                weight = animationController.animator.GetFloat("IKRightFootWeight");
                AdjustFootIK(AvatarIKGoal.RightFoot, ref previousRightFootPosition, animationController.rightFootPos, ref rightLegFreeHang, weight);

            }

            // --- Set weights to 0 and return if IK is off ---
            if (!hIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.0f);
                animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.0f);
                animationController.animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.0f);
            }
            else
            {
                // --- Else, sample hand weight from the animator's parameters, which are set by the animations themselves through curves ---

                // --- Left hand ---
                float weight = animationController.animator.GetFloat("IKLeftHandWeight");
                AdjustHandIK(AvatarIKGoal.LeftHand, HumanBodyBones.LeftHand, ref previousLeftHandPosition, ref previousLeftHandRotation, animationController.leftHandPos, weight);

                // --- Right hand ---
                weight = animationController.animator.GetFloat("IKRightHandWeight");
                AdjustHandIK(AvatarIKGoal.RightHand, HumanBodyBones.RightHand, ref previousRightHandPosition, ref previousRightHandRotation, animationController.rightHandPos, weight);                
            }
        }

        private void AdjustFootIK(AvatarIKGoal ikGoal, ref Vector3 previousFootPosition, Vector3 bonePosition, ref bool freeHang, float weight)
        {        
            // --- Set weight ---
            animationController.animator.SetIKPositionWeight(ikGoal, weight);
            animationController.animator.SetIKRotationWeight(ikGoal, weight);

            // --- Initialize previous foot position ---
            if (previousFootPosition == Vector3.zero)
                previousFootPosition = animationController.animator.GetIKPosition(ikGoal);

            // --- Throw a ray to detect a surface, else activate free hang (requires both feet failing collision test) ---
            Vector3 footPosition;
            RaycastHit hit;
            Vector3 rayOrigin = bonePosition - transform.forward * 0.75f;
            //rayOrigin += transform.forward * freeHangForwardOffset * freehangWeight
            //    + Vector3.down * freeHangDownwardsOffset * freehangWeight;

            float speedModifier = feetAdjustmentSpeed * Time.deltaTime;

            if (movableLedgeDelta != Vector3.zero)
                speedModifier = 1.0f;

            if (debugDraw)
                Debug.DrawLine(rayOrigin, rayOrigin + transform.forward * feetRayLength);

            if (Physics.Raycast(rayOrigin, transform.forward, out hit, feetRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore))
            {
                footPosition = Vector3.Lerp(previousFootPosition, hit.point - transform.forward * footLength, speedModifier);
                freeHang = false;
            }
            else
            {
                footPosition = Vector3.Lerp(previousFootPosition, bonePosition, speedModifier);
                freeHang = true;
            }
          
            // --- Update previous foot position ---
            previousFootPosition = footPosition;

            // --- Adjust to free hang forward offset ---
            //footPosition += (-transform.forward * freeHangForwardOffset * freehangWeight)
            //    + Vector3.down * freeHangDownwardsOffset * freehangWeight;

            // --- Set IK position ---
            animationController.animator.SetIKPosition(ikGoal, footPosition);
            
            if (weight <= 0.0f)
                previousFootPosition = bonePosition; // Keep previous position at bone position when weight is 0 

        }

        private void AdjustHandIK(AvatarIKGoal ikGoal, HumanBodyBones bone, ref Vector3 previousHandPosition, ref Quaternion previousHandRotation, Vector3 bonePosition, float weight)
        {
            // --- Left hand ---
            if (weight > 0.0f)
            {
                // --- Set weights and get IK transform ---
                animationController.animator.SetIKPositionWeight(ikGoal, weight);
                animationController.animator.SetIKRotationWeight(ikGoal, weight);
                Vector3 IKPosition = animationController.animator.GetIKPosition(ikGoal);
                Quaternion IKRotation = animationController.animator.GetIKRotation(ikGoal);

                // --- Initialize previous position and rotation if at startup ---
                if (previousHandPosition == Vector3.zero)
                    previousHandPosition = IKPosition;

                if (previousHandRotation == Quaternion.identity)
                    previousHandRotation = IKRotation;

                // --- Compute hand position given ledge hook ---
                Vector3 forward = animationController.animator.GetBoneTransform(HumanBodyBones.Hips).forward;
                TraverserLedgeObject.TraverserLedgeHook hook = ledgeGeometry.GetHook(IKPosition - forward * 0.3f);
                Vector3 handPosition = ledgeGeometry.GetPosition(ref hook);
                Vector3 rayOrigin = bonePosition;
                rayOrigin.y = handPosition.y;

                // --- Offset ray to prevent null hits ---
                rayOrigin += -forward * handRayLength * 0.5f - Vector3.up * 0.1f;

                // --- Offset ray according to free hang forward offset ---
                //rayOrigin += forward * freeHangForwardOffset * freehangWeight
                //    + Vector3.down * freeHangDownwardsOffset * freehangWeight;
                Vector3 handDirection = forward;

                // --- Throw ray to detect surface ---
                RaycastHit hit;
                bool collided = Physics.Raycast(rayOrigin, forward, out hit, handRayLength, controller.characterCollisionMask, QueryTriggerInteraction.Ignore);
                BoxCollider collider = hit.collider as BoxCollider;

                if (debugDraw)
                    Debug.DrawLine(rayOrigin, rayOrigin + forward * handRayLength);

                // --- On collision, check if the surface is either the current or previous ledge geometry ---
                if (collided 
                    && (ledgeGeometry.IsEqual(ref collider) || previousLedgeGeometry.IsEqual(ref collider)))
                {
                    float backupY = handPosition.y;
                    handPosition = hit.point;
                    handPosition.y = backupY;
                    handDirection = -hit.normal;
                }

                // --- Update hand position according to user defined parameters ---
                handPosition -= forward * handLength;
                handPosition.y += handIKYDistance;

                // --- Adapt to free hang ---
                //if(!animationController.transition.isON)
                //{
                    handPosition -= forward * (handIKLengthFreehang * freehangWeight);
                    handPosition.y += handIKYFreehang * freehangWeight;
                //}

                // --- Aim IK, if a ledge was found, aim the hand towards the given target position ---
                Vector2 leftStickInput = abilityController.inputController.GetInputMovement();
                bool useAimIK = ikGoal == AvatarIKGoal.LeftHand ? leftStickInput.x < 0.0f : leftStickInput.x >= 0.0f;
                float speedModifier = handsAdjustmentSpeed * Time.deltaTime;

                if (!animationController.transition.isON && ledgeDetected && useAimIK)
                {
                    float moveIntensity = abilityController.inputController.GetMoveIntensity();
                    Vector3 aimDirection = transform.right * leftStickInput.x + transform.up * leftStickInput.y;
                    aimDirection.Normalize();

                    handPosition += aimDirection * moveIntensity;
                    handDirection = ((targetAimPosition + forward * 0.15f) - bonePosition).normalized;
                    speedModifier *= handsAimIKIntensity;
                }

                if (movableLedgeDelta != Vector3.zero)
                    speedModifier = 1.0f;

                // --- Using the computed position and rotation, smoothly transition to desired transform ---
                handPosition = Vector3.Lerp(previousHandPosition, handPosition, speedModifier);
                IKRotation = Quaternion.Slerp(previousHandRotation,
                    Quaternion.LookRotation(handDirection + Vector3.up * 1.5f), speedModifier);

                // --- Update previous hand position and rotation ---
                previousHandPosition = handPosition;
                previousHandRotation = IKRotation;

                // --- Adjust position according to free hang forward offset ---
                //handPosition -= forward * freeHangForwardOffset * freehangWeight
                //    + Vector3.down * freeHangDownwardsOffset * freehangWeight;
                

                // --- Set IK position and rotation ---
                animationController.animator.SetIKPosition(ikGoal, handPosition);
                animationController.animator.SetIKRotation(ikGoal, IKRotation);
            }
            else
            {
                // --- Keep previous position and rotation at bone position when weight is 0 ---
                previousHandPosition = bonePosition;
                previousHandRotation = animationController.animator.GetBoneTransform(bone).rotation;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!debugDraw || abilityController == null)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetAimPosition, aimDebugSphereRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(ltlRayOrigin, ltlRayDestination);
        }

        // -------------------------------------------------
    }
}

