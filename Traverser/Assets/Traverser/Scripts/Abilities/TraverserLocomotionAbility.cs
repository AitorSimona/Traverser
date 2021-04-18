using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserLocomotionAbility : MonoBehaviour, TraverserAbility 
    {
        // --- Attributes ---
        [Header("Movement settings")]
        [Tooltip("Desired speed in meters per second for slow movement.")]
        [Range(0.0f, 10.0f)]
        public float movementSpeedSlow = 3.9f;

        [Tooltip("Desired speed in meters per second for fast movement.")]
        [Range(0.0f, 10.0f)]
        public float movementSpeedFast = 5.5f;

        [Tooltip("Character won't move if below this speed.")]
        [Range(0.0f, 10.0f)]
        public float movementSpeedMin = 0.1f;

        [Tooltip("How fast the character's speed will increase with given input in m/s^2.")]
        public float movementAcceleration = 50.0f;

        [Tooltip("How fast the character achieves movementAcceleration. Smaller values make the character reach target movementAcceleration faster.")]
        public float movementAccelerationTime = 0.1f;

        [Tooltip("How fast the character decreases movementSpeed. Smaller values make the character reach target movementSpeed slower.")]
        public float movementDecelerationTime = 0.75f;

        [Tooltip("How much current speed impacts deceleration. Smaller values will make a running character decelerate slower.")]
        public float movementDecelerationRatio = 0.5f;

        [Tooltip("How fast the character reaches its desired slow movement speed in seconds. Smaller values make character reach movementSpeedSlow slower")]
        public float movementSpeedSlowTime = 0.75f;

        [Tooltip("How fast the character reaches its desired fast speed (and decelerates to slow) in seconds. Smaller values make character reach movementSpeedFast and decelerate to movementSpeedSlow slower.")]
        public float movementSpeedFastTime = 1.5f;


        [Header("Rotation settings")]
        [Tooltip("How fast the character rotates in degrees/s.")]
        public float rotationSpeed = 180.0f; 

        [Tooltip("How fast the character's rotation speed will increase with given input in degrees/s^2.")]
        public float rotationAcceleration = 15.0f; 

        [Tooltip("How fast the character achieves rotationAcceleration. Smaller values make the character reach target rotationAcceleration faster.")]
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

        // -------------------------------------------------

        // --- Private Variables ---

        private TraverserCharacterController controller;

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

        // -------------------------------------------------

        // --- Basic Methods ---
        public void OnEnable()
        {
            desiredLinearSpeed = movementSpeedSlow;
            controller = GetComponent<TraverserCharacterController>();
            TraverserInputLayer.capture.movementDirection = Vector3.zero;
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserInputLayer.capture.UpdateLocomotion();

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

        public TraverserAbility OnPostUpdate(float deltaTime)
        {

            return null;
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
            TraverserAffineTransform tmp = TraverserAffineTransform.Create(transform.position, transform.rotation);

            // --- Update current velocity and rotation ---
            UpdateMovement();
            UpdateRotation();

            // --- Compute desired speed ---
            float speed = GetDesiredSpeed(deltaTime);

            // --- Don't move if below minimum speed ---
            if (speed < movementSpeedMin)
                speed = 0.0f;

            // --- Rotate controller ---
            controller.ForceRotate(currentRotationSpeed * deltaTime);

            // --- Speed damping, reduce speed when turning around ---
            if (currentRotationSpeed > rotationSpeedToTurn && speed < movementSpeedNoTurn)
                speed *= movementSpeedDampingOnTurn; 

            // --- Compute desired displacement ---
            Vector3 finalDisplacement = transform.forward * speed * deltaTime;

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

                if(controller.collidedLedge)
                {
                    controller.collidedLedge = false;

                    if (contactAbility == null)
                    {
                        foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                        {

                            // --- If any ability reacts to the collision, break ---
                            if (ability.IsAbilityEnabled() && ability.OnContact(TraverserAffineTransform.Create(controller.capsuleHits[0].point, 
                                Quaternion.identity), deltaTime))
                            {
                                contactAbility = ability;
                                break;
                            }
                        }
                    }
                }

                else if (collision.isColliding && attemptTransition)
                {
                    ref TraverserAffineTransform contactTransform = ref controller.contactTransform /*TraverserAffineTransform.Create(contactPoint, q)*/;

                    float angle = Vector3.SignedAngle(controller.contactNormal, -transform.forward, Vector3.up);

                    // --- If we are not close to the desired angle or contact point, do not handle contacts ---
                    if (Mathf.Abs(angle) > contactAngleMax || Mathf.Abs(math.distance(contactTransform.t, tmp.t)) > contactDistanceMax)
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

            ResetVelocityAndRotation();

            return contactAbility;
        }

        public bool OnContact(TraverserAffineTransform contactTransform, float deltaTime)
        {
            return false;
        }

        public bool OnDrop(float deltaTime)
        {
            return false;
        }

        public void OnAbilityAnimatorMove() // called by ability controller at OnAnimatorMove()
        {

        }

        public bool IsAbilityEnabled()
        {
            return isActiveAndEnabled;
        }

        // -------------------------------------------------

        // --- Movement ---

        float GetDesiredSpeed(float deltaTime)        
        {
            float desiredSpeed = 0.0f;
            float moveIntensity = GetDesiredMovementIntensity(deltaTime);

            // --- Sprinting, Accelerate/Decelerate to movementSpeedFast or movementSpeedSlow ---
            if (TraverserInputLayer.capture.run && desiredLinearSpeed < movementSpeedFast)
                desiredLinearSpeed += (movementSpeedFast - movementSpeedSlow) * deltaTime * movementSpeedFastTime;
            else if (desiredLinearSpeed > movementSpeedSlow && TraverserInputLayer.GetMoveIntensity() > 0.0f)
                desiredLinearSpeed += (movementSpeedSlow - movementSpeedFast) * deltaTime * movementSpeedFastTime;

            // --- Increase timer ---
            movementAccelerationTimer += movementSpeedSlowTime*deltaTime;

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
                previousMovementIntensity = moveIntensity + 0.01f; 
                // --- We use movementDecelerationTime and our previous speed to decelerate, bigger previous speed makes decelerating a longer task ---
                movementDecelerationTimer -= (movementDecelerationTime - previousMovementIntensity*movementDecelerationRatio) * deltaTime;
            }
            // --- Update timer if accelerating/constant acceleration ---
            else
            {
                movementDecelerationTimer = moveIntensity;
                previousMovementIntensity = moveIntensity;
            }

            return moveIntensity;
        }

        public void AccelerateMovement(Vector3 acceleration)
        {
            if (acceleration.magnitude > movementAcceleration)
                acceleration = acceleration.normalized * movementAcceleration;

            currentVelocity += acceleration;
        }

        public void AccelerateRotation(float rotation_acceleration)
        {
            Mathf.Clamp(rotation_acceleration, -rotationAcceleration, rotationAcceleration);
            currentRotationSpeed += rotation_acceleration;
        }

        public void ResetVelocityAndRotation()
        {
            currentVelocity = Vector3.zero;
            currentRotationSpeed = 0.0f;
        }

        public void UpdateMovement()
        {
            // --- Gather current input in Vector3 format ---
            Vector3 inputDirection;
            inputDirection.x = TraverserInputLayer.capture.stickHorizontal;
            inputDirection.y = 0.0f;
            inputDirection.z = TraverserInputLayer.capture.stickVertical;

            // --- Compute desired acceleration given input, current velocity, and time to accelerate ---
            Vector3 acceleration = (inputDirection*desiredLinearSpeed - currentVelocity) / movementAccelerationTime;

            // --- Cap acceleration ---
            if (acceleration.magnitude > movementAcceleration)
            {
                acceleration.Normalize();
                acceleration *= movementAcceleration;
            }

            // --- Update velocity ---
            AccelerateMovement(acceleration);

            // --- Cap Velocity ---
            currentVelocity.x = Mathf.Clamp(currentVelocity.x, -desiredLinearSpeed, desiredLinearSpeed);
            currentVelocity.z = Mathf.Clamp(currentVelocity.z, -desiredLinearSpeed, desiredLinearSpeed);
            currentVelocity.y = Mathf.Clamp(currentVelocity.y, -desiredLinearSpeed, desiredLinearSpeed);
        }

        public void UpdateRotation()
        {
            // --- Compute desired yaw rotation from forward to currentVelocity ---
            float rot = Vector3.SignedAngle(transform.forward, currentVelocity, Vector3.up);

            if (rot > rotationSpeed)
                rot = rotationSpeed;

            // --- Update rotation speed ---
            AccelerateRotation(rot / rotationAccelerationTime);

            // --- Cap Rotation ---
            currentRotationSpeed = Mathf.Clamp(currentRotationSpeed, -rotationSpeed, rotationSpeed);
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}
