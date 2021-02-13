using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserAbilityController))]
    [RequireComponent(typeof(TraverserCharacterController))]

    public class TraverserLocomotionAbility : MonoBehaviour, TraverserAbility // SnapshotProvider is derived from MonoBehaviour, allows the use of kinematica's snapshot debugger
    {
        // --- Attributes ---
        [Header("Movement settings")]
        [Tooltip("Desired speed in meters per second for slow movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedSlow = 3.9f;

        [Tooltip("Desired speed in meters per second for fast movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedFast = 5.5f;

        //[Tooltip("Speed in meters per second at which the character is considered to be braking (assuming player release the stick).")]
        //[Range(0.0f, 10.0f)]
        //public float brakingSpeed = 0.4f;

        [Tooltip("How fast the character's speed will increase with given input in m/s^2")]
        public float maxMovementAcceleration = 0.1f;

        public float max_rot_speed = 10.0f; // in degrees / second

        public float max_rot_acceleration = 0.1f; // in degrees

        public float time_to_accel = 0.1f;

        public float time_to_target = 0.1f;

        //[Tooltip("How likely are we to deviate from current pose to idle, higher values make faster transitions to idle")]

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
        private float desiredLinearSpeed => TraverserInputLayer.capture.run ? desiredSpeedFast : desiredSpeedSlow;
        private Vector3 currentVelocity = Vector3.zero;
        private float current_rotation_speed = 0.0f; // degrees

        // -------------------------------------------------

        // --- Basic Methods ---
        public void OnEnable()
        {
            controller = GetComponent<TraverserCharacterController>();
            TraverserInputLayer.capture.movementDirection = Vector3.forward;
        }

        public void OnDisable()
        {


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

            TraverserAffineTransform tmp = TraverserAffineTransform.Create(transform.position, transform.rotation);

            Vector3 inputDirection;
            inputDirection.x = TraverserInputLayer.capture.stickHorizontal;
            inputDirection.y = 0.0f;
            inputDirection.z = TraverserInputLayer.capture.stickVertical;

            Vector3 acceleration = (inputDirection - currentVelocity) / time_to_accel;

            if(acceleration.magnitude > maxMovementAcceleration)
            {
                acceleration.Normalize();
                acceleration *= maxMovementAcceleration;
            }

            AccelerateMovement(acceleration);

            // --- Cap Velocity ---
            currentVelocity.x = Mathf.Clamp(currentVelocity.x, -desiredLinearSpeed, desiredLinearSpeed);
            currentVelocity.z = Mathf.Clamp(currentVelocity.z, -desiredLinearSpeed, desiredLinearSpeed);
            currentVelocity.y = Mathf.Clamp(currentVelocity.y, -desiredLinearSpeed, desiredLinearSpeed);

            float rot = Vector3.SignedAngle(transform.forward, currentVelocity, Vector3.up);

            if (rot > max_rot_speed)
                rot = max_rot_speed;

            AccelerateRotation(rot / time_to_target);

            // --- Cap Rotation ---
            current_rotation_speed = Mathf.Clamp(current_rotation_speed, -max_rot_speed, max_rot_speed);

            float speed = GetDesiredSpeed(deltaTime);

            //Debug.Log(speed);

            // ---If desired rotation is equal or bigger than the maximum allowed, increase rotation speed and decrease movement speed(we want to turn around)-- -
            //if (Mathf.Abs(current_rotation_speed) == max_rot_speed && speed < desiredLinearSpeed * 0.5f)
            //{
            //    current_rotation_speed *= turnMultiplier;
            //    speed *= speedTurnMultiplier;
            //    //previousSpeed *= speedTurnMultiplier;
            //    timetoMaxSpeed = 0.0f;
            //}

            controller.ForceRotate(current_rotation_speed * deltaTime);

            for (int i = 0; i < iterations; ++i)
            {
                Vector3 finalDisplacement = transform.forward*speed*deltaTime;

                if (i == 0)
                    controller.stepping = 1.0f;
                else
                    controller.stepping = stepping;

                controller.Move(finalDisplacement);
                controller.Tick(deltaTime);

                ref TraverserCharacterController.TraverserCollision collision = ref controller.current; // current state of the controller (after a tick is issued)

                // --- If a collision occurs, call each ability's onContact callback ---

                if (collision.isColliding && attemptTransition)
                {
                    float3 contactPoint = collision.colliderContactPoint;
                    contactPoint.y = controller.position.y;
                    float3 contactNormal = collision.colliderContactNormal;

                    // TODO: Check if works properly
                    float Qangle;
                    Vector3 Qaxis;
                    transform.rotation.ToAngleAxis(out Qangle, out Qaxis);
                    //Qaxis.x = 0.0f;
                    //Qaxis.y = 0.0f;
                    quaternion q = quaternion.identity/*math.mul(transform.rotation, Quaternion.FromToRotation(Qaxis, contactNormal))*/;

                    TraverserAffineTransform contactTransform = TraverserAffineTransform.Create(contactPoint, q);

                    //  TODO : Remove temporal debug object
                    GameObject.Find("dummy").transform.position = contactTransform.t;

                    float3 desired_direction = contactTransform.t - tmp.t;
                    float current_orientation = Mathf.Rad2Deg * Mathf.Atan2(gameObject.transform.forward.z, gameObject.transform.forward.x);
                    float target_orientation = current_orientation + Vector3.SignedAngle(TraverserInputLayer.capture.movementDirection, desired_direction, Vector3.up);
                    float angle = -Mathf.DeltaAngle(current_orientation, target_orientation);

                    // TODO: The angle should be computed according to the direction we are heading too (not always the smallest angle!!)
                    //Debug.Log(angle);
                    // --- If we are not close to the desired angle or contact point, do not handle contacts ---
                    if (Mathf.Abs(angle) < 30 || Mathf.Abs(math.distance(contactTransform.t, tmp.t)) > 4.0f)
                    {
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
                else if (!controller.isGrounded) // we are dropping/falling down
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

        float timetoMaxSpeed = 0.0f;
        float timerSpeed = 0.5f;

        //float previousSpeed = 0.0f;

        float GetDesiredSpeed(float deltaTime)        
        {
            float desiredSpeed = 0.0f;
            float moveIntensity = TraverserInputLayer.GetMoveIntensity();

            timetoMaxSpeed += timerSpeed*deltaTime;

            if (timetoMaxSpeed > 1.0f)
                timetoMaxSpeed = 1.0f;

            desiredSpeed = desiredLinearSpeed * timetoMaxSpeed * moveIntensity;

            if (desiredSpeed == 0.0f)
                timetoMaxSpeed = 0.0f;
            else if (moveIntensity < timetoMaxSpeed)
                timetoMaxSpeed = moveIntensity;

            return desiredSpeed;
        }

        public void AccelerateMovement(Vector3 acceleration)
        {
            if (acceleration.magnitude > maxMovementAcceleration)
                acceleration = acceleration.normalized * maxMovementAcceleration;

            currentVelocity += acceleration;
        }

        public void AccelerateRotation(float rotation_acceleration)
        {
            Mathf.Clamp(rotation_acceleration, -max_rot_acceleration, max_rot_acceleration);
            current_rotation_speed += rotation_acceleration;
        }

        public void ResetVelocityAndRotation()
        {
            currentVelocity = Vector3.zero;
            current_rotation_speed = 0.0f;
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}
