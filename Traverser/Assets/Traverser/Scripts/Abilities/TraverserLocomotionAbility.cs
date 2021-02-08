using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
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

        [Tooltip("Speed in meters per second at which the character is considered to be braking (assuming player release the stick).")]
        [Range(0.0f, 10.0f)]
        public float brakingSpeed = 0.4f;

        [Header("Simulation settings")]
        [Tooltip("How many movement iterations per frame will the controller perform. More iterations are more expensive but provide greater predictive collision detection reach.")]
        [Range(0, 10)]
        public int iterations = 3;
        [Tooltip("How much will the controller displace the 2nd and following iterations respect the first one (speed increase). Increases prediction reach at the cost of precision (void space).")]
        [Range(1.0f, 10.0f)]
        public float stepping = 10.0f;

        [Header("Fall limitation")]
        [Tooltip("Disable to prevent character from falling off obstacles")]
        public bool freedrop = true;

        [Tooltip("[freedrop disabled only] Offsets distance left until falling down point, bigger values make the character brake sooner")]
        [Range(0.0f, 1.0f)]
        public float brakeDistance = 0.5f;

        [Tooltip("[freedrop disabled only] Modifies at what distance from falling point the character begins to break")]
        [Range(0.0f, 5.0f)]
        public float maxFallPredictionDistance = 3.0f;   // reset distance once no fall is predicted

        [Tooltip("[freedrop disabled only] Modifies at what distance from falling point the character stops")]
        [Range(0.0f, 1.0f)]
        public float minFallPredictionDistance = 0.5f;


        // -------------------------------------------------

        // --- Private Variables ---

        //TraverserLedgeObject.TraverserLedgeGeometry ledgeGeometry;
        private TraverserCharacterController controller;
        private bool isBraking = false;
        private bool preFreedrop = true;
        private float desiredLinearSpeed => TraverserInputLayer.capture.run ? desiredSpeedFast : desiredSpeedSlow;
        private float distance_to_fall = 3.0f; // initialized to maxFallPredictionDistance

        // -------------------------------------------------

        // --- Basic Methods ---
        public void OnEnable()
        {
            controller = GetComponent<TraverserCharacterController>();
            TraverserInputLayer.capture.movementDirection = Vector3.forward;

            // --- Initialize arrays ---
            //ledgeGeometry = TraverserLedgeObject.TraverserLedgeGeometry.Create();

            // --- Play default animation ---

            preFreedrop = freedrop;
            distance_to_fall = maxFallPredictionDistance;
        }

        public void OnDisable()
        {
            // --- Delete arrays ---
            //ledgeGeometry.Dispose();
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserInputLayer.capture.UpdateLocomotion();

            //if (TraverserInputLayer.capture.run)
            //    freedrop = true;
            //else
            //    freedrop = preFreedrop;

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
            if (!freedrop)
            {
                // --- Prevent character from falling from its current ground ---
                // --- TODO: Clean this, temporal placement ---
                if (!freedrop && controller.current.ground != null)
                {
                    BoxCollider collider = controller.current.ground.gameObject.GetComponent<BoxCollider>() as BoxCollider;

                    if (collider != null)
                    {
                        // TODO: Expose this
                        if (collider.gameObject.layer == LayerMask.NameToLayer("Wall")
                            || collider.gameObject.layer == LayerMask.NameToLayer("Default")
                            || collider.gameObject.layer == LayerMask.NameToLayer("Platform"))
                        {
                            // TODO: Prevent this from happening all the time
                            //ledgeGeometry.Initialize(collider);
                        }
                    }

                    //ledgeGeometry.DebugDraw(); // TODO: Temporal
                }

                LimitTransform();
            }

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
            //AffineTransform transform = prediction.Transform;
            //AffineTransform worldRootTransform = synthesizer.WorldRootTransform;

            //float inverseSampleRate = Missing.recip(synthesizer.Binary.SampleRate); // recip is just the inverse (1/x)

            //bool attemptTransition = true;

            TraverserAbility contactAbility = null;

            for (int i = 0; i < iterations; ++i)
            {
                Vector3 inputDirection;
                inputDirection.x = TraverserInputLayer.capture.stickHorizontal;
                inputDirection.y = 0.0f;
                inputDirection.z = TraverserInputLayer.capture.stickVertical;

                Vector3 finalPosition = inputDirection.normalized * GetDesiredSpeed() * deltaTime;

                if (i == 0)
                    controller.stepping = 1.0f;
                else
                    controller.stepping = stepping;

                controller.Move(finalPosition);
                controller.Tick(deltaTime);
            }

            // --- TEMPORAL DEBUG UTILITY ---
            float3 dummyPos = controller.position;
            dummyPos.y += controller.capsuleHeight / 2;
            GameObject.Find("Capsule").transform.position = dummyPos;

            //controller.MoveTo(finalPosition); // apply movement
            //controller.Tick(deltaTime); // update controller

            //AffineTransform tmp = synthesizer.WorldRootTransform;

            //// --- We keep pushing new transforms until trajectory prediction limit or collision ---
            //while (prediction.Push(transform))
            //{
            //    // --- Get new transform and perform future movement ---
            //    transform = prediction.Advance;

            //    //controller.Move((worldRootTransform.transform(transform.t) - controller.Position) * inverseSampleRate); // apply movement
            //    //TODO controller.Tick(inverseSampleRate); // update controller


            //    ref TraverserCharacterController.TraverserCollision collision = ref controller.current; // current state of the controller (after a tick is issued)

            //    // --- If a collision occurs, call each ability's onContact callback ---

            //    if (collision.isColliding && attemptTransition)
            //    {
            //        distance_to_fall = maxFallPredictionDistance; // reset distance once no fall is predicted

            //        float3 contactPoint = collision.colliderContactPoint;
            //        contactPoint.y = controller.Position.y;
            //        float3 contactNormal = collision.colliderContactNormal;
            //        quaternion q = math.mul(transform.q, Missing.forRotation(Missing.zaxis(transform.q), contactNormal));

            //        AffineTransform contactTransform = new AffineTransform(contactPoint, q);

            //        //  TODO : Remove temporal debug object
            //        //GameObject.Find("dummy").transform.position = contactTransform.t;

            //        float3 desired_direction = contactTransform.t - tmp.t;
            //        float current_orientation = Mathf.Rad2Deg * Mathf.Atan2(gameObject.transform.forward.z, gameObject.transform.forward.x);
            //        float target_orientation = current_orientation + Vector3.SignedAngle(TraverserInputLayer.capture.movementDirection, desired_direction, Vector3.up);
            //        float angle = -Mathf.DeltaAngle(current_orientation, target_orientation);

            //        // TODO: The angle should be computed according to the direction we are heading too (not always the smallest angle!!)
            //        //Debug.Log(angle);
            //        // --- If we are not close to the desired angle or contact point, do not handle contacts ---
            //        if (Mathf.Abs(angle) < 30 || Mathf.Abs(math.distance(contactTransform.t, tmp.t)) > 4.0f)
            //        {
            //            continue;
            //        }

            //        if (contactAbility == null)
            //        {
            //            foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
            //            {
            //                SnapshotProvider component = ability as SnapshotProvider;

            //                // --- If any ability reacts to the collision, break ---
            //                if (component.enabled && ability.OnContact(ref synthesizer, contactTransform, deltaTime))
            //                {
            //                    contactAbility = ability;
            //                    break;
            //                }
            //            }
            //        }

            //        attemptTransition = false; // make sure we do not react to another collision
            //    }
            //    else if (!controller.isGrounded) // we are dropping/falling down
            //    {
            //        // --- Let other abilities take control on drop ---
            //        if (contactAbility == null)
            //        {
            //            foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
            //            {
            //                SnapshotProvider component = ability as SnapshotProvider;

            //                // --- If any ability reacts to the drop, break ---
            //                if (component.enabled && ability.OnDrop(ref synthesizer, deltaTime))
            //                {
            //                    contactAbility = ability;
            //                    break;
            //                }
            //            }
            //        }

            //        if (!freedrop)
            //        {
            //            // --- Compute distance to fall point ---
            //            Vector3 futurepos;
            //            futurepos.x = controller.current.position.x;
            //            futurepos.y = controller.current.position.y;
            //            futurepos.z = controller.current.position.z;
            //            distance_to_fall = Mathf.Abs((futurepos - gameObject.transform.position).magnitude) - brakeDistance;
            //            break;
            //        }
            //    }
            //    else
            //    {
            //        distance_to_fall = maxFallPredictionDistance; // reset distance once no fall is predicted
            //    }

            //    transform.t = worldRootTransform.inverseTransform(controller.Position);
            //    prediction.Transform = transform;
            //}

            return contactAbility;
        }

        public bool OnContact(Transform contactTransform, float deltaTime)
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

        // --- Utilities ---

        float GetDesiredSpeed()        
        {
            float desiredSpeed = 0.0f;

            float moveIntensity = TraverserInputLayer.GetMoveIntensity();

            // --- If we are idle ---
            if (Mathf.Approximately(moveIntensity, 0.0f))
            {
                if (!isBraking && math.length(controller.velocity) < brakingSpeed)
                    isBraking = true;
            }
            else
            {
                isBraking = false;
                desiredSpeed = moveIntensity * desiredLinearSpeed;
            }

            // --- Manually brake the character when about to fall ---
            if (!freedrop)
            {
                if (distance_to_fall < minFallPredictionDistance)
                    desiredSpeed = 0.0f;
                else if (distance_to_fall < maxFallPredictionDistance)
                {
                    desiredSpeed *= distance_to_fall / maxFallPredictionDistance;
                }
            }

            return desiredSpeed;
        }

        public void LimitTransform()
        {
            //if (ledgeGeometry.vertices[0].Equals(float3.zero) || freedrop)
            //    return;

            //Vector3 pos = gameObject.transform.position;
            //ledgeGeometry.LimitTransform(ref pos, 0.1f);
            //gameObject.transform.position = pos;
            //AffineTransform worldRootTransform = AffineTransform.Create(pos, kinematica.Synthesizer.Ref.WorldRootTransform.q);
            //kinematica.Synthesizer.Ref.SetWorldTransform(worldRootTransform, true);
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}
