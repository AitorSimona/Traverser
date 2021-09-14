using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserLocomotionAbility : MonoBehaviour, TraverserAbility 
    {
        // --- Attributes ---
        [Header("Animation")]
        [Tooltip("A reference to the locomotion ability's dataset (scriptable object asset).")]
        public TraverserLocomotionData locomotionData;

        [Header("Physics")]
        [Tooltip("A reference to the locomotion ability's ragdoll profile. If not set, locomotion wil use the default profile.")]
        public TraverserRagdollProfile ragdollProfile;

        [Header("Movement settings")]

        [Tooltip("A reference to the character's camera to adapt movement.")]
        public Transform cameraTransform;

        [Tooltip("If below this value, do not update velocity as this would stop the character immediately.")]
        [Range(0.0f, 0.1f)]
        public float minimumInputIntensity = 0.1f;

        [Tooltip("Desired speed in meters per second for walk movement.")]
        [Range(0.0f, 1.5f)]
        public float walkSpeed = 1.0f;

        [Tooltip("Desired speed in meters per second for jog movement.")]
        [Range(0.0f, 4.0f)]
        public float jogSpeed = 3.9f;

        [Tooltip("Desired speed in meters per second for run movement.")]
        [Range(0.0f, 6.0f)]
        public float runSpeed = 5.5f;

        [Tooltip("How fast the character decreases movementSpeed. Smaller values make the character reach target movementSpeed slower.")]
        [Range(0.1f, 5.0f)]
        public float movementDecelerationRatio = 0.75f;

        [Tooltip("How fast the character reaches its desired jog movement speed in seconds. Smaller values make character reach jogSpeed slower")]
        [Range(0.1f, 5.0f)]
        public float jogSpeedTime = 0.75f;

        [Tooltip("How fast the character reaches its desired run speed (and decelerates to slow) in seconds. Smaller values make character reach runSpeed and decelerate to jogSpeed slower.")]
        [Range(0.1f, 5.0f)]
        public float runSpeedTime = 1.5f;


        [Header("Rotation settings")]
        [Tooltip("How fast the character rotates in degrees/s.")]
        public float rotationSpeed = 180.0f; 

        [Tooltip("How fast the character's rotation speed will increase with given input in degrees/s^2.")]
        public float rotationAcceleration = 15.0f; 

        [Tooltip("How fast the character achieves rotationAcceleration. Smaller values make the character reach target rotationAcceleration faster. Can't be 0.")]
        [Range(0.1f, 5.0f)]
        public float rotationAccelerationTime = 0.1f;

        [Tooltip("Speed will be multiplied by this value when turning around (heading above rotationSpeedToTurn and movement speed below movementSpeedNoTurn). Used for keeping character in place when turning around")]
        [Range(0.0f, 10.0f)]
        public float movementSpeedDampingOnTurn = 0.1f;

        [Tooltip("Character must be below this movement speed to trigger movementSpeedDampingOnTurn.")]
        [Range(0.0f, 10.0f)]
        public float movementSpeedNoTurn = 1.0f;

        [Tooltip("Character will trigger movementSpeedDampingOnTurn when its rotation is above this value.")]
        [Range(0.0f, 10.0f)]
        public float rotationSpeedToTurn = 1.0f;

        [Header("Jump settings")]
        [Tooltip("How fast the character begins jumping in m/s.")]
        public float initialJumpSpeed = 12.0f;

        [Tooltip("How fast the character ends up jumping in m/s.")]
        public float maxJumpSpeed = 15.0f;

        [Tooltip("How fast the character accelerates jump in m/s sq.")]
        public float jumpDeceleration = 2.0f;

        [Tooltip("How much time the character keeps maxJumpSpeed intact (afterwards jumpDeceleration will be applied).")]
        public float jumpTime = 0.25f;

        [Header("Contact settings")]
        [Tooltip("Maximum angle at which the character can contact with the environment in degrees. If above this number the collision won't be considered.")]
        public float contactAngleMax = 30.0f;

        [Tooltip("Maximum distance at which the character can contact with the environment in meters. If above this number the collision won't be considered.")]
        public float contactDistanceMax = 4.0f;

        [Header("Simulation settings")]
        [Tooltip("How many movement iterations per frame will the controller perform. More iterations are more expensive but provide greater predictive collision detection reach.")]
        [Range(0, 10)]
        public int iterations = 3;
        [Tooltip("How much will the controller displace the 2nd and following iterations respect the first one (speed increase). Increases prediction reach at the cost of precision (void space).")]
        [Range(1.0f, 10.0f)]
        public float stepping = 10.0f;

        [Header("Feet IK settings")]
        [Tooltip("Activates or deactivates foot IK placement for the locomotion ability.")]
        public bool fIKOn = true;

        [Tooltip("The maximum distance of the ray that enables foot IK, the bigger the ray the further we detect the ground.")]
        [Range(0.0f, 5.0f)]
        public float feetIKGroundDistance = 1.0f;

        [Tooltip("Modifies how fast we adjust pelvis to match feet positions.")]
        [Range(0.0f, 1.0f)]
        public float pelvisAdjustmentSpeed = 0.28f;

        [Tooltip("Modifies how fast we adjust feet to match IK transforms.")]
        [Range(0.0f, 1.0f)]
        public float feetAdjustmentSpeed = 0.5f;

        [Header("Debug")]
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Selecting the object may be required to show debug geometry.")]
        public bool debugDraw = false;


        // -------------------------------------------------

        // --- Private Variables ---

        private TraverserAbilityController abilityController;
        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;

        // --- Feet IK ---
        private TraverserTransform leftFootIKTransform, rightFootIKTransform;
        private float lastPelvisCorrectedY, lastRightFootPositionY, lastLeftFootPositionY;

        // --- Jump ---
        private float currentJumpSpeed = 0.0f;
        private float dampingTime = 0.15f;
        private bool wasJumping = false;
        private bool isApex = false;
        private float groundDistance = 0.1f;
        private float currentTime = 0.0f;

        // --- Ability-level state ---
        public enum LocomotionAbilityState
        {
            Moving,
            Falling,
            Landing,
            Jumping
        }

        // --- Character's target movement speed ---
        private float desiredLinearSpeed = 0.0f;
        
        // --- Stores current velocity in m/s ---
        private Vector3 currentVelocity = Vector3.zero;

        // --- Stores current rotation speed in degrees ---
        private float currentRotationSpeed = 0.0f; 

        // --- Stores the current time to reach max movement speed ---
        private float movementAccelerationTimer = 0.0f;

        // --- Sotes movementSpeedTimer's maximum value, has to be 1.0f ---
        private float movementAccelerationMaxTime = 1.0f;
        
        // --- Stores the current time to reach desired velocity (decelerating) ---
        private float movementDecelerationTimer = 1.0f;

        // --- Stores previous inpu intensity to decelerate character ---
        private float previousMovementIntensity = 0.0f;

        private LocomotionAbilityState state;


        private Vector3 previousGroundPosition = Vector3.zero;
        private Transform groundTransform;

        // -------------------------------------------------

        // --- Basic Methods ---
        public void Start()
        {
            state = LocomotionAbilityState.Moving;
            desiredLinearSpeed = jogSpeed;
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            leftFootIKTransform = TraverserTransform.Get(Vector3.zero, Quaternion.identity);
            rightFootIKTransform = TraverserTransform.Get(Vector3.zero, Quaternion.identity);
        }

        // -------------------------------------------------

        // --- Ability class methods ---

        private void LateUpdate()
        {
            if (!abilityController.isCurrent(this))
                return;

            // --- Adapt locomotion to moving ground (movable ledges for example) ---
            if (controller.current.ground != null
                && controller.current.ground.Equals(controller.previous.ground))
            {
                if (groundTransform == null || !groundTransform.Equals(controller.current.ground.transform))
                {
                    groundTransform = controller.current.ground.transform;
                    previousGroundPosition = groundTransform.position;
                }

                Vector3 delta = groundTransform.position - previousGroundPosition;
                controller.TeleportTo(transform.position + delta);

                previousGroundPosition = groundTransform.position;
            }
        }

        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserAbility ret = this;

            if (animationController.transition.isON)
            {
                animationController.transition.UpdateTransition();
            }

            return ret;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            TraverserAbility ret = this;

            if (!animationController.transition.isON)
            {
                if (state == LocomotionAbilityState.Landing)
                    state = LocomotionAbilityState.Moving;

                // --- We perform future movement to check for collisions, then rewind using snapshot debugger's capabilities ---
                TraverserAbility contactAbility = HandleMovementPrediction(deltaTime);

                // --- Another ability has been triggered ---
                if (contactAbility != null)
                    ret = contactAbility;
            }


            if(fIKOn)
            {
                FindFootIKTarget(animationController.rightFootPos, ref rightFootIKTransform);
                FindFootIKTarget(animationController.leftFootPos, ref leftFootIKTransform);
            }

            return ret;
        }

        TraverserAbility HandleMovementPrediction(float deltaTime)
        {
            Assert.IsTrue(controller != null); // just in case :)

            // --- Start recording (all current state values will be recorded) ---
            controller.Snapshot();

            TraverserAbility contactAbility = SimulatePrediction(ref controller, deltaTime);

            // --- Go back in time to the snapshot state, all current state variables recover their initial value ---
            controller.Rewind();

            return contactAbility;
        }

        TraverserAbility SimulatePrediction(ref TraverserCharacterController controller, float deltaTime)
        {
            bool attemptTransition = true;
            TraverserAbility contactAbility = null;

            // --- Preserve pre-simulation transform ---
            TraverserTransform tmp = TraverserTransform.Get(transform.position, transform.rotation);

            // --- Compute desired speed ---
            float speed = GetDesiredSpeed(deltaTime);

            // --- Speed damping, reduce speed when turning around ---
            if (currentRotationSpeed > rotationSpeedToTurn && speed < movementSpeedNoTurn)
                speed *= movementSpeedDampingOnTurn;

            // --- Compute desired velocity given camera transform ---
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0.0f;

            Vector2 inputDirection = abilityController.inputController.GetInputMovement();

            // --- Activate or update jump ---
            HandleJump(speed);
            speed = DampenSpeed(speed);

            // --- If stick is released, do not update velocity as this would stop the character immediately ---
            if (inputDirection.magnitude > minimumInputIntensity)
            {
                currentVelocity = inputDirection.x * camRight
                        + inputDirection.y * camForward;
            }

            // --- Compute desired displacement ---
            Vector3 finalDisplacement = currentVelocity.normalized * speed;
            finalDisplacement += transform.up * currentJumpSpeed;
            finalDisplacement *= deltaTime;

            // --- Update rotation ---
            UpdateRotation();

            // --- Rotate controller ---
            controller.Rotate(currentRotationSpeed * deltaTime);

            for (int i = 0; i < iterations; ++i)
            {
                // --- Step simulation if above first tick ---
                if (i == 0)
                    controller.stepping = 1.0f;
                else
                    controller.stepping = stepping;

                // --- Simulate movement ---
                controller.Move(finalDisplacement);
                controller.Tick(deltaTime);

                ref TraverserCharacterController.TraverserCollision collision = ref controller.current; // current state of the controller (after a tick is issued)

                // --- After performing movement, evaluate results and trigger a reaction, or let other abilities react ---

                // --- If a collision occurs, call each ability's onContact callback ---
                if (collision.isColliding && attemptTransition)
                {
                    ref TraverserTransform contactTransform = ref controller.contactTransform;

                    float angle = Vector3.SignedAngle(controller.contactNormal, -transform.forward, Vector3.up);

                    // --- If we are not close to the desired angle or contact point, do not handle contacts ---
                    if (Mathf.Abs(angle) > contactAngleMax || Mathf.Abs(Vector3.Distance(contactTransform.t, tmp.t)) > contactDistanceMax)
                    {
                        continue;
                    }

                    if (contactAbility == null)
                    {
                        foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                        {
                            // --- If any ability reacts to the collision, break ---
                            if (ability.IsAbilityEnabled() && ability.OnContact(ref contactTransform, deltaTime))
                            {
                                contactAbility = ability;
                                break;
                            }
                        }
                    }

                    attemptTransition = false; // make sure we do not react to another collision
                }
                 else if ((state == LocomotionAbilityState.Falling
                    && controller.CheckForwardCollision(transform.position + Vector3.up * controller.capsuleHeight * 0.9f, contactDistanceMax)) 
                    || controller.CheckForwardCollision(transform.position - Vector3.up * 0.25f, contactDistanceMax))
                {
                    // --- Check forward collision for possible ledge contact and inform abilities ---

                    // --- We are falling and we have found a forward collision ---             
                    if (contactAbility == null)
                    {
                        foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                        {
                            // --- If any ability reacts to the collision, break ---
                            if (ability.IsAbilityEnabled() && ability.OnContact(ref controller.contactTransform, deltaTime))
                            {
                                contactAbility = ability;
                                break;
                            }
                        }
                    }

                    //break;
                }
                else if (!collision.isGrounded 
                    || (collision.ground && controller.previous.ground && (collision.ground.GetInstanceID() != controller.previous.ground.GetInstanceID()))) // we are dropping/falling down and found ground
                {
                    // --- Let other abilities take control on drop ---
                    if (contactAbility == null)
                    {
                        foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                        {
                            // --- If any ability reacts to the drop, break ---
                            if (ability.IsAbilityEnabled() && ability.OnDrop(deltaTime))
                            {
                                contactAbility = ability;
                                break;
                            }
                        }
                    }
                }

                float distanceToGround = controller.GetYDistanceToGround(tmp.t + Vector3.up * 0.1f);

                // --- We are falling and the simulation has found a new ground, we may activate a landing transition ---             
                if (!controller.groundSnap && distanceToGround != -1.0f && contactAbility == null && collision.isGrounded && collision.ground 
                    && (controller.previous.ground == null || distanceToGround < 0.15f) )
                {
                    bool success = false;

                    // --- Activate a landing roll transition ---
                    if (state == LocomotionAbilityState.Falling
                        && distanceToGround > controller.capsuleHeight
                        && distanceToGround < controller.capsuleHeight * 1.25f)
                    {
                        animationController.animator.CrossFade(locomotionData.fallTransitionAnimation.animationStateName, locomotionData.fallTransitionAnimation.transitionDuration, 0);

                        TraverserTransform contactTransform = TraverserTransform.Get(transform.position, transform.rotation);
                        TraverserTransform targetTransform = TraverserTransform.Get(collision.ground.ClosestPoint(animationController.skeletonPos)
                            + transform.forward * locomotionData.fallToRollTransitionData.targetOffset + Vector3.up * controller.capsuleHeight / 2.0f, // roll animation offset
                            transform.rotation);

                        // --- We pass an existing transition trigger that won't do anything, we are already in falling animation ---
                        success = animationController.transition.StartTransition(ref locomotionData.fallToRollTransitionData, ref contactTransform, ref targetTransform);
                    }
                    else if ((state == LocomotionAbilityState.Moving
                        && distanceToGround > controller.capsuleHeight
                        && distanceToGround < controller.capsuleHeight * 1.25f)
                        || (distanceToGround < 0.15f && state == LocomotionAbilityState.Falling))
                    {
                        animationController.animator.CrossFade(locomotionData.fallTransitionAnimation.animationStateName, locomotionData.fallTransitionAnimation.transitionDuration, 0);

                        TraverserTransform contactTransform = TraverserTransform.Get(transform.position, transform.rotation);
                        TraverserTransform targetTransform = TraverserTransform.Get(collision.ground.ClosestPoint(animationController.skeletonPos)
                            + transform.forward * locomotionData.hardLandingTransitionData.targetOffset + Vector3.up * controller.capsuleHeight / 2.0f, // roll animation offset
                            transform.rotation);

                        // --- We pass an existing transition trigger that won't do anything, we are already in falling animation ---
                        success = animationController.transition.StartTransition(ref locomotionData.hardLandingTransitionData, ref contactTransform, ref targetTransform);
                    }

                    // --- Trigger a landing transition ---
                    if (success)
                    {
                        currentJumpSpeed = 0.0f;
                        wasJumping = false;
                        state = LocomotionAbilityState.Landing;
                        break;
                    }
                }
            }

            ResetRotation();

            return contactAbility;
        }

        public bool OnContact(ref TraverserTransform contactTransform, float deltaTime)
        {
            return false;
        }

        public bool OnDrop(float deltaTime)
        {
            return false;
        }

        public void ResetMovement()
        {
            controller.ResetController();
            movementDecelerationTimer = 0.0f;
            previousMovementIntensity = 0.0f;
            movementAccelerationTimer = 0.0f;
            currentVelocity = Vector3.zero;
            state = LocomotionAbilityState.Moving;
        }

        public void OnEnter()
        {
            currentJumpSpeed = 0.0f;
            wasJumping = false;

            FindFootIKTarget(animationController.rightFootPos, ref rightFootIKTransform);
            FindFootIKTarget(animationController.leftFootPos, ref leftFootIKTransform);

            // --- Update last IK positions to prevent working with outdated data ---

            lastRightFootPositionY = transform.InverseTransformPoint(rightFootIKTransform.t).y;
            lastLeftFootPositionY = transform.InverseTransformPoint(leftFootIKTransform.t).y;
            lastPelvisCorrectedY = animationController.skeletonPos.y;

            if(abilityController.ragdollController != null)
                abilityController.ragdollController.SetRagdollProfile(ref ragdollProfile);
        }

        public void OnExit()
        {
            if (abilityController.ragdollController != null)
                abilityController.ragdollController.SetDefaultRagdollProfile();
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        // -------------------------------------------------

        // --- Movement ---

        public void SetLocomotionState(LocomotionAbilityState newState)
        {
            state = newState;
        }

        public LocomotionAbilityState GetLocomotionState()
        {
            return state;
        }

        private float GetDesiredSpeed(float deltaTime)        
        {
            float desiredSpeed;
            float moveIntensity = GetDesiredMovementIntensity(deltaTime);

            // --- Sprinting, Accelerate/Decelerate to movementSpeedFast or movementSpeedSlow ---
            if (abilityController.inputController.GetInputButtonRun() && desiredLinearSpeed < runSpeed)
                desiredLinearSpeed += (runSpeed - jogSpeed) * deltaTime * runSpeedTime;
            else if (desiredLinearSpeed > jogSpeed && abilityController.inputController.GetMoveIntensity() > 0.0f)
                desiredLinearSpeed += (jogSpeed - runSpeed) * deltaTime * runSpeedTime;

            // --- Increase timer ---
            movementAccelerationTimer += jogSpeedTime*deltaTime;

            // --- Cap timer ---
            if (movementAccelerationTimer > movementAccelerationMaxTime)
                movementAccelerationTimer = movementAccelerationMaxTime;

            // --- Compute desired speed given input intensity and timer ---
            desiredSpeed = desiredLinearSpeed * movementAccelerationTimer * moveIntensity;

            // --- Reset/Decrease timer if input intensity changes ---
            if (desiredSpeed == 0.0f)
                movementAccelerationTimer = 0.0f;
            else if (moveIntensity < movementAccelerationTimer)
                movementAccelerationTimer = moveIntensity;

            return desiredSpeed;
        }

        private float GetDesiredMovementIntensity(float deltaTime)
        {
            // --- Compute desired movement intensity given input and timer ---
            float moveIntensity = abilityController.inputController.GetMoveIntensity();

            // --- Cap timer, reset previous movement intensity ---
            if (movementDecelerationTimer < 0.0f)
            {
                movementDecelerationTimer = 0.0f;
                previousMovementIntensity = 0.0f;
            }

            // --- Decrease timer if asked to decelerate, update previous movement intensity ---
            if (moveIntensity < previousMovementIntensity)
            {
                moveIntensity = movementDecelerationTimer;
                previousMovementIntensity = moveIntensity; 

                // --- We use movementDecelerationTime to decelerate, bigger value makes decelerating a longer task ---
                movementDecelerationTimer -= movementDecelerationRatio * deltaTime;
            }
            // --- Update timer if accelerating/constant acceleration ---
            else
            {
                movementDecelerationTimer = moveIntensity;
                previousMovementIntensity = moveIntensity;
            }

            return moveIntensity;
        }

        public void AccelerateRotation(float rotation_acceleration)
        {
            Mathf.Clamp(rotation_acceleration, -rotationAcceleration, rotationAcceleration);
            currentRotationSpeed += rotation_acceleration;
        }

        public void ResetRotation()
        {
            //currentVelocity = Vector3.zero;
            currentRotationSpeed = 0.0f;
        }

        public void UpdateRotation()
        {
            // --- Compute desired yaw rotation from forward to currentVelocity ---
            float rot = Vector3.SignedAngle(transform.forward, currentVelocity, Vector3.up);

            rot = Mathf.Clamp(rot, -rotationSpeed, rotationSpeed);

            // --- Update rotation speed ---
            AccelerateRotation(rot / rotationAccelerationTime);

            // --- Cap Rotation ---
            currentRotationSpeed = Mathf.Clamp(currentRotationSpeed, -rotationSpeed, rotationSpeed);
        }

        public void HandleJump(float speed)
        {
            // --- Enable jumping only if on the ground and in moving state ---
            if (!wasJumping && abilityController.inputController.GetInputButtonNorth() && state == LocomotionAbilityState.Moving
            && (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionData.locomotionONAnimation.animationStateName) 
               || animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionData.locomotionOFFAnimation.animationStateName)) 
            && !animationController.animator.IsInTransition(0))
            {
                state = LocomotionAbilityState.Jumping;
                wasJumping = true;
                fIKOn = false;
                isApex = false;
                currentTime = 0.0f;

                // --- Choose jump animation depending on speed ---
                if (speed < walkSpeed)
                    animationController.animator.CrossFade(locomotionData.jumpAnimation.animationStateName, locomotionData.jumpAnimation.transitionDuration);
                else
                    animationController.animator.CrossFade(locomotionData.jumpForwardAnimation.animationStateName, locomotionData.jumpForwardAnimation.transitionDuration);
            }

            // --- If on jumping state, update jump speed ---
            if ((state == LocomotionAbilityState.Jumping 
                || state == LocomotionAbilityState.Falling)
                && wasJumping)
            {
                bool jumpingAnim = animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionData.jumpAnimation.animationStateName) ||
                    animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionData.jumpForwardAnimation.animationStateName);


                // --- Let the animation play for some time before applying jump speed ---
                if (jumpingAnim 
                    && animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > dampingTime
                    && currentJumpSpeed == 0.0f)
                {
                    if (currentJumpSpeed == 0.0f)
                        currentJumpSpeed = initialJumpSpeed;

                }
                // --- Progressively decelerate jumping speed over time ---
                else if (currentJumpSpeed > 0.0f)
                {
                    if (currentTime < jumpTime && !isApex)
                        currentTime += Time.deltaTime; //currentJumpSpeed += jumpDeceleration * Time.deltaTime;
                    else
                    {
                        currentJumpSpeed -= jumpDeceleration * Time.deltaTime;
                        isApex = true;
                    }

                    // --- If jump speed is low enough, activate falling state ---
                    if (currentJumpSpeed <= Mathf.Abs(Physics.gravity.y))
                    {
                        state = LocomotionAbilityState.Falling;

                        if (controller.GetYDistanceToGround(transform.position) < groundDistance)
                            currentJumpSpeed = 0.0f;
                    }

                }

            }

            // --- Activate landing animation ---
            if (state == LocomotionAbilityState.Falling 
                && currentJumpSpeed <= 0.0f
                && wasJumping)
            {
                if (speed < walkSpeed)
                    animationController.animator.CrossFade(locomotionData.fallToLandAnimation.animationStateName, locomotionData.fallToLandAnimation.transitionDuration);
                else
                    animationController.animator.CrossFade(locomotionData.fallToRunAnimation.animationStateName, locomotionData.fallToRunAnimation.transitionDuration);

                fIKOn = true;
                state = LocomotionAbilityState.Moving;
                wasJumping = false;
                currentJumpSpeed = 0.0f;
            }
        }

        public float DampenSpeed(float speed)
        {
            // --- Dampen speed when certain animations are playing ---
            if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionData.fallToLandAnimation.animationStateName))
                speed = speed * 0.25f;
            else if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionData.jumpAnimation.animationStateName))
                speed = speed * 0.75f;
            else if (animationController.animator.GetCurrentAnimatorStateInfo(0).IsName(locomotionData.fallToRunAnimation.animationStateName))
                speed = speed * 0.75f;

            return speed;
        }

        // -------------------------------------------------

        // --- Events ---

        private void OnAnimatorIK(int layerIndex)
        {
            if (!abilityController.isCurrent(this) || !fIKOn)
            {
                return;
            }

            // --- Adjust pelvis height to allow better feet adjustment ---
            MatchPelvisToFeet();

            // --- Set right foot weights (sampling animation curve) and adjust IK target ---
            animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animationController.animator.GetFloat("IKRightFootWeight"));
            AdjustIkTarget(AvatarIKGoal.RightFoot, ref rightFootIKTransform, ref lastRightFootPositionY);

            // --- Set left foot weights (sampling animation curve) and adjust IK target ---
            animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            animationController.animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animationController.animator.GetFloat("IKLeftFootWeight"));
            AdjustIkTarget(AvatarIKGoal.LeftFoot, ref leftFootIKTransform, ref lastLeftFootPositionY);      
        }

        void AdjustIkTarget(AvatarIKGoal foot, ref TraverserTransform IKTransform, ref float lastFootPositionY)
        {
            // --- Get previous IK position ---
            Vector3 IkPosition = animationController.animator.GetIKPosition(foot);

            if (IKTransform.t != Vector3.zero)
            {
                // --- Transform to local coordinates ---
                IkPosition = transform.InverseTransformPoint(IkPosition);
                Vector3 modifiedIKPosition = transform.InverseTransformPoint(IKTransform.t);

                float targetYPosition = Mathf.Lerp(lastFootPositionY, modifiedIKPosition.y, feetAdjustmentSpeed);
                IkPosition.y += targetYPosition;

                lastFootPositionY = targetYPosition;

                // --- Transform back to world ---
                IkPosition = transform.TransformPoint(IkPosition);

                // --- Assign IK rotation ---
                animationController.animator.SetIKRotation(foot, IKTransform.q);
            }

            // --- Assign IK position ---
            animationController.animator.SetIKPosition(foot, IkPosition);
        }

        private void MatchPelvisToFeet()
        {
            if (rightFootIKTransform.t == Vector3.zero || leftFootIKTransform.t == Vector3.zero || lastPelvisCorrectedY == 0)
                lastPelvisCorrectedY = animationController.animator.bodyPosition.y;
            else
            {
                // --- Compute offsets between feet and its IK targets ---
                float leftFootYOffset = leftFootIKTransform.t.y - transform.position.y;
                float rightFootYOffset = rightFootIKTransform.t.y - transform.position.y;

                // --- Get the maximum offset between both feet and apply it to pelvis ---
                float totalOffset = (leftFootYOffset < rightFootYOffset) ? leftFootYOffset : rightFootYOffset;
                Vector3 pelvisCorrectedPosition = animationController.animator.bodyPosition + Vector3.up * totalOffset;

                // --- Apply user defined speed modifier ---
                pelvisCorrectedPosition.y = Mathf.Lerp(lastPelvisCorrectedY, pelvisCorrectedPosition.y, pelvisAdjustmentSpeed);

                // --- Modify body position ---
                animationController.animator.bodyPosition = pelvisCorrectedPosition;

                // --- Store last corrected body position ---
                lastPelvisCorrectedY = pelvisCorrectedPosition.y;
            }
        }

        private void FindFootIKTarget(Vector3 footPosition, ref TraverserTransform footCorrectedTransform)
        {
            // --- Shoot a ray downwards from a higher position respect the foot, update IK target transform ---
            RaycastHit hit;
            footPosition.y = transform.position.y + feetIKGroundDistance*0.5f;

            if(debugDraw)
                Debug.DrawLine(footPosition, footPosition + Vector3.down * feetIKGroundDistance, Color.yellow);

            if (Physics.Raycast(footPosition, Vector3.down, out hit, feetIKGroundDistance, controller.characterCollisionMask))
            {
                footCorrectedTransform.t = footPosition;
                footCorrectedTransform.t.y = hit.point.y;
                footCorrectedTransform.q = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
                return;
            }

            footCorrectedTransform.t = Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDraw || abilityController == null)
                return;

            Gizmos.color = Color.cyan;

            // --- Draw forward collision ray ---
            Gizmos.DrawRay(transform.position + Vector3.up * controller.capsuleHeight * 0.9f, transform.forward*contactDistanceMax);
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}
