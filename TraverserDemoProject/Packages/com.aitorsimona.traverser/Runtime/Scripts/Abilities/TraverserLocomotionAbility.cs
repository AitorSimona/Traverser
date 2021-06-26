using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserLocomotionAbility : MonoBehaviour, TraverserAbility 
    {
        // --- Attributes ---
        [Header("Movement settings")]

        [Tooltip("A reference to the character's camera to adapt movement.")]
        public Transform camera;

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
        [Tooltip("The character's foot height (size in Y, meters).")]
        [Range(0.0f, 1.0f)]
        public float footHeight = 1.0f;


        // -------------------------------------------------

        // --- Private Variables ---

        private TraverserAbilityController abilityController;
        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;

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

        // --- Ray for feet IK checks ---
        private Ray IKRay;

        // -------------------------------------------------

        // --- Basic Methods ---
        public void Start()
        {
            IKRay = new Ray();
            desiredLinearSpeed = jogSpeed;
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            TraverserInputLayer.capture.movementDirection = Vector3.zero;
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public void OnInputUpdate()
        {
            TraverserInputLayer.capture.UpdateLocomotion();
        }

        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserAbility ret = this;

            return ret;
        }

        public TraverserAbility OnFixedUpdate(float deltaTime)
        {
            TraverserAbility ret = this;

            // --- We perform future movement to check for collisions, then rewind using snapshot debugger's capabilities ---
            TraverserAbility contactAbility = HandleMovementPrediction(deltaTime);

            // --- Another ability has been triggered ---
            if (contactAbility != null)
                ret = contactAbility;

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
            Vector3 camForward = camera.forward;
            Vector3 camRight = camera.right;
            camForward.y = 0.0f;

            Vector3 inputDirection;
            inputDirection.x = TraverserInputLayer.capture.leftStickHorizontal;
            inputDirection.y = 0.0f;
            inputDirection.z = TraverserInputLayer.capture.leftStickVertical;

            // --- If stick is released, do not update velocity as this would stop the character immediately ---
            if (inputDirection.magnitude > minimumInputIntensity)
            {
                currentVelocity = inputDirection.x * camRight
                        + inputDirection.z * camForward;
            }

            // --- Compute desired displacement ---
            Vector3 finalDisplacement = currentVelocity.normalized * speed;
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

                // --- If a collision occurs, call each ability's onContact callback ---

                if (collision.isColliding && attemptTransition)
                {
                    ref TraverserTransform contactTransform = ref controller.contactTransform /*TraverserAffineTransform.Create(contactPoint, q)*/;

                    float angle = Vector3.SignedAngle(controller.contactNormal, -transform.forward, Vector3.up);

                    // --- If we are not close to the desired angle or contact point, do not handle contacts ---
                    if (Mathf.Abs(angle) > contactAngleMax || Mathf.Abs(Vector3.Distance(contactTransform.t, tmp.t)) > contactDistanceMax)
                    {
                        //Debug.Log(Mathf.Abs(angle));
                        continue;
                    }

                    if (contactAbility == null)
                    {
                        foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                        {
                            // --- If any ability reacts to the collision, break ---
                            if (ability.IsAbilityEnabled() && ability.OnContact(contactTransform, deltaTime))
                            {
                                contactAbility = ability;
                                break;
                            }
                        }
                    }

                    attemptTransition = false; // make sure we do not react to another collision
                }
                else if (!collision.isGrounded 
                    || (collision.ground && controller.previous.ground 
                        && (collision.ground.GetInstanceID() != controller.previous.ground.GetInstanceID()))) // we are dropping/falling down and found ground
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

                // TODO: Add case for falling without finding new ground 
            }

            ResetRotation();

            return contactAbility;
        }

        public bool OnContact(TraverserTransform contactTransform, float deltaTime)
        {
            return false;
        }

        public bool OnDrop(float deltaTime)
        {
            return false;
        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        // -------------------------------------------------

        // --- Movement ---

        public void ResetLocomotion()
        {
            movementDecelerationTimer = 0.0f;
            previousMovementIntensity = 0.0f;
            movementAccelerationTimer = 0.0f;
            controller.targetVelocity = Vector3.zero;
        }

        float GetDesiredSpeed(float deltaTime)        
        {
            float desiredSpeed = 0.0f;
            float moveIntensity = GetDesiredMovementIntensity(deltaTime);

            // --- Sprinting, Accelerate/Decelerate to movementSpeedFast or movementSpeedSlow ---
            if (TraverserInputLayer.capture.run && desiredLinearSpeed < runSpeed)
                desiredLinearSpeed += (runSpeed - jogSpeed) * deltaTime * runSpeedTime;
            else if (desiredLinearSpeed > jogSpeed && TraverserInputLayer.GetMoveIntensity() > 0.0f)
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

        float GetDesiredMovementIntensity(float deltaTime)
        {
            // --- Compute desired movement intensity given input and timer ---
            float moveIntensity = TraverserInputLayer.GetMoveIntensity();


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

        // -------------------------------------------------

        // --- Events ---

        private void OnAnimatorIK(int layerIndex)
        {
            if (!abilityController.isCurrent(this))
                return;

            // --- Set weights to 0 and return if IK is off ---
            if(!fIKOn)
            {
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0.0f);
                animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0.0f);
                animationController.animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0.0f);
                return;
            }

            // --- Else, sample foot weight from the animator's parameters, which are set by the animations themselves through curves ---

            RaycastHit hit;

            // --- Left foot ---
            animationController.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animationController.animator.GetFloat("IKLeftFootWeight"));
            animationController.animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animationController.animator.GetFloat("IKLeftFootWeight"));

            IKRay.origin = animationController.animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up;
            IKRay.direction = Vector3.down;           

            if (Physics.Raycast(IKRay, out hit, feetIKGroundDistance, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 footPosition = hit.point;
                footPosition.y += footHeight;
                animationController.animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                animationController.animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * animationController.animator.GetIKRotation(AvatarIKGoal.LeftFoot));
            }

            // --- Right foot ---
            animationController.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animationController.animator.GetFloat("IKRightFootWeight"));
            animationController.animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animationController.animator.GetFloat("IKRightFootWeight"));

            IKRay.origin = animationController.animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up;
            IKRay.direction = Vector3.down;

            if (Physics.Raycast(IKRay, out hit, feetIKGroundDistance, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 footPosition = hit.point;
                footPosition.y += footHeight;
;
                animationController.animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                animationController.animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * animationController.animator.GetIKRotation(AvatarIKGoal.RightFoot));
            }
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}
