using UnityEngine;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.SnapshotDebugger;
using UnityEngine.Assertions;

namespace CWLF
{
    // --- Locomotion pose prediction job (MT) ---
    [BurstCompile(CompileSynchronously = true)] //Burst is primarily designed to work efficiently with the Job system.
    public struct LocomotionJob : IJob // multhithreaded code, pose prediction will be executed faster
    {
        // --- Attributes ---
        public MemoryRef<MotionSynthesizer> synthesizer;

        public PoseSet idlePoses;

        public PoseSet locomotionPoses;

        public Trajectory trajectory; // struct that holds a trajectory (array of transforms)

        public bool idle;

        public float minTrajectoryDeviation;

        public float responsiveness; // how easily the character reacts to input orders 

        public float maxPoseDeviation;

        ref MotionSynthesizer Synthesizer => ref synthesizer.Ref; // reference to the synthesizer

        // -------------------------------------------------
        public void Execute()
        {
            if (idle && Synthesizer.MatchPose(idlePoses, Synthesizer.Time, MatchOptions.DontMatchIfCandidateIsPlaying | MatchOptions.LoopSegment, maxPoseDeviation))
            {
                return;
            }

            Synthesizer.MatchPoseAndTrajectory(locomotionPoses, Synthesizer.Time, trajectory, MatchOptions.None, responsiveness, minTrajectoryDeviation);
        }
    }

    // -------------------------------------------------

    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(MovementController))]

    public class LocomotionAbility : SnapshotProvider, Ability // SnapshotProvider is derived from MonoBehaviour, allows the use of kinematica's snapshot debugger
    {
        // --- Attributes ---
        [Header("Prediction settings")]
        [Tooltip("Desired speed in meters per second for slow movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedSlow = 3.9f;

        [Tooltip("Desired speed in meters per second for fast movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedFast = 5.5f;

        [Tooltip("How fast or slow the target velocity is supposed to be reached.")]
        [Range(0.0f, 1.0f)]
        public float velocityPercentage = 1.0f;

        [Tooltip("How fast or slow the desired forward direction is supposed to be reached.")]
        [Range(0.0f, 1.0f)]
        public float forwardPercentage = 1.0f;

        [Tooltip("Relative weighting for pose and trajectory matching.")]
        [Range(0.0f, 1.0f)]
        public float responsiveness = 0.45f;

        [Tooltip("Speed in meters per second at which the character is considered to be braking (assuming player release the stick).")]
        [Range(0.0f, 10.0f)]
        public float brakingSpeed = 0.4f;

        [Header("Motion correction")]
        [Tooltip("How much root motion distance should be corrected to match desired trajectory.")]
        [Range(0.0f, 1.0f)]
        public float correctTranslationPercentage = 0.0f;

        [Tooltip("How much root motion rotation should be corrected to match desired trajectory.")]
        [Range(0.0f, 1.0f)]
        public float correctRotationPercentage = 1.0f;

        [Tooltip("Minimum character move speed (m/s) before root motion correction is applied.")]
        [Range(0.0f, 10.0f)]
        public float correctMotionStartSpeed = 2.0f;

        [Tooltip("Character move speed (m/s) at which root motion correction is fully effective.")]
        [Range(0.0f, 10.0f)]
        public float correctMotionEndSpeed = 3.0f;

        [Tooltip("How likely are we to deviate from current pose to idle, higher values make faster transitions to idle")]
        [Range(-1.0f, 1.0f)]
        public float maxPoseDeviation = 0.01f;

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

        // --- World interactable elements ---
        [Snapshot]
        LedgeObject.LedgeGeometry ledgeGeometry;

        Kinematica kinematica;
        MovementController controller;

        PoseSet idleCandidates;
        PoseSet locomotionCandidates;
        Trajectory trajectory;

        [Snapshot]
        bool isBraking = false;

        bool preFreedrop = true;

        // TODO: Remove from here
        float desiredLinearSpeed => InputLayer.capture.run ? desiredSpeedFast : desiredSpeedSlow;

        float distance_to_fall = 3.0f; // initialized to maxFallPredictionDistance

        // -------------------------------------------------

        // --- Basic Methods ---
        public override void OnEnable()
        {
            base.OnEnable();
            kinematica = GetComponent<Kinematica>();
            controller = GetComponent<MovementController>();
            ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

            InputLayer.capture.movementDirection = Missing.forward;
            InputLayer.capture.moveIntensity = 0.0f;

            // --- Initialize arrays ---
            idleCandidates = synthesizer.Query.Where("Idle", Locomotion.Default).And(Idle.Default);
            locomotionCandidates = synthesizer.Query.Where("Locomotion", Locomotion.Default).Except(Idle.Default);
            trajectory = synthesizer.CreateTrajectory(Allocator.Persistent);

            ledgeGeometry = LedgeObject.LedgeGeometry.Create();

            // --- Play default animation ---
            synthesizer.PlayFirstSequence(idleCandidates);

            preFreedrop = freedrop;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            // --- Delete arrays ---
            idleCandidates.Dispose();
            locomotionCandidates.Dispose();
            trajectory.Dispose();

            ledgeGeometry.Dispose();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            if (!rewind) // if we are not using snapshot debugger to rewind
                InputLayer.capture.UpdateLocomotion();

            if (InputLayer.capture.run)
                freedrop = true;
            else
                freedrop = preFreedrop;
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public Ability OnUpdate(float deltaTime)
        {
            Ability ret = this;
            ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

            // --- Ensure stop animation is properly played, that we do not change to other animations ---
            float minTrajectoryDeviation = HandleBraking();

            // --- We perform future movement to check for collisions, then rewind using snapshot debugger's capabilities ---
            Ability contactAbility = HandleMovementPrediction(ref synthesizer, GetDesiredSpeed(ref synthesizer), deltaTime);

            // --- Set up a job so pose prediction runs faster (multithreading) ---
            LocomotionJob job = new LocomotionJob()
            {
                synthesizer = kinematica.Synthesizer,
                idlePoses = idleCandidates,
                locomotionPoses = locomotionCandidates,
                trajectory = trajectory,
                idle = /*InputLayer.capture.moveIntensity*/ GetDesiredSpeed(ref synthesizer) == 0.0f,
                minTrajectoryDeviation = minTrajectoryDeviation,
                responsiveness = responsiveness,
                maxPoseDeviation = this.maxPoseDeviation
            };

            // --- Finally tell kinematica to wait for this job to finish before executing other stuff ---
            kinematica.AddJobDependency(job.Schedule());

            // --- Another ability has been triggered ---
            if (contactAbility != null)
                ret = contactAbility;


            return ret;
        }

        public Ability OnPostUpdate(float deltaTime)
        {
            if (!freedrop)
            {
                // --- Prevent character from falling from its current ground ---
                // --- TODO: Clean this, temporal placement ---
                if (!freedrop && controller.current.ground)
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
                            ledgeGeometry.Initialize(collider);
                        }
                    }


                    ledgeGeometry.DebugDraw(); // TODO: Temporal
                }

                LimitTransform();
            }

            return null;
        }

        float HandleBraking()         // TODO: Remove from here
        {
            // --- Kinematica comment ---

            // If character is braking, we set a strong deviation threshold on Trajectory Heuristic to be conservative (candidate would need to be a LOT better to be picked)
            // because then we want the character to pick a stop clip in the library and stick to it even if Kinematica can jump to a better clip (cost wise) in the middle 
            // of that stop animation. Indeed stop animations have very subtle foot steps (to reposition to idle stance) that would be squeezed by blend/jumping from clip to clip.
            // Moreover, playing a stop clip from start to end will make sure we will reach a valid transition point to idle.

            bool hasReachedEndOfSegment;
            MotionSynthesizer synthesizer = kinematica.Synthesizer.Ref;
            Binary.TypeIndex type = KinematicaLayer.GetCurrentAnimationInfo(ref synthesizer, out hasReachedEndOfSegment);

            float minTrajectoryDeviation = 0.03f; // default threshold

            if (type == synthesizer.Binary.GetTypeIndex<Locomotion>())
            {
                if (isBraking)
                {
                    minTrajectoryDeviation = 0.3f; // high threshold to let stop animation finish
                }
            }
            else if (hasReachedEndOfSegment)
            {

                minTrajectoryDeviation = 0.0f; // we are not playing a locomotion segment and we reach the end of that segment, we must force a transition, otherwise character will freeze in the last position
            }

            return minTrajectoryDeviation;
        }

        Ability HandleMovementPrediction(ref MotionSynthesizer synthesizer, float desiredSpeed, float deltaTime)
        {
            // --- Create final trajectory from given parameters ---
            TrajectoryPrediction prediction = TrajectoryPrediction.CreateFromDirection(ref kinematica.Synthesizer.Ref,
               InputLayer.capture.movementDirection,
               desiredSpeed,
               trajectory,
               velocityPercentage,
               forwardPercentage);

            Assert.IsTrue(controller != null); // just in case :)

            // --- Start recording (all Snapshot marked values will be recorded) ---
            controller.Snapshot();

            Ability contactAbility = SimulatePrediction(ref synthesizer, ref prediction, ref controller, deltaTime);

            // --- Go back in time to the snapshot state, all snapshot-marked variables recover their initial value ---
            controller.Rewind();

            return contactAbility;
        }
        
        Ability SimulatePrediction(ref MotionSynthesizer synthesizer, ref TrajectoryPrediction prediction, ref MovementController controller, float deltaTime)
        {
            AffineTransform transform = prediction.Transform;
            AffineTransform worldRootTransform = synthesizer.WorldRootTransform;

            float inverseSampleRate = Missing.recip(synthesizer.Binary.SampleRate); // recip is just the inverse (1/x)

            bool attemptTransition = true;
            Ability contactAbility = null;

            AffineTransform tmp = synthesizer.WorldRootTransform;

            // --- We keep pushing new transforms until trajectory prediction limit or collision ---
            while (prediction.Push(transform))
            {
                // --- Get new transform and perform future movement ---
                transform = prediction.Advance;

                controller.MoveTo(worldRootTransform.transform(transform.t)); // apply movement
                controller.Tick(inverseSampleRate); // update controller

                ref MovementController.Closure closure = ref controller.current; // current state of the controller (after a tick is issued)

                // --- If a collision occurs, call each ability's onContact callback ---
                if (closure.isColliding && attemptTransition)
                {
                    distance_to_fall = maxFallPredictionDistance; // reset distance once no fall is predicted

                    float3 contactPoint = closure.colliderContactPoint;
                    contactPoint.y = controller.Position.y;
                    float3 contactNormal = closure.colliderContactNormal;
                    quaternion q = math.mul(transform.q, Missing.forRotation(Missing.zaxis(transform.q), contactNormal));

                    AffineTransform contactTransform = new AffineTransform(contactPoint, q);

                    //  TODO : Remove temporal debug object
                    //GameObject.Find("dummy").transform.position = contactTransform.t;

                    float3 desired_direction = contactTransform.t - tmp.t;
                    float current_orientation = Mathf.Rad2Deg * Mathf.Atan2(gameObject.transform.forward.z, gameObject.transform.forward.x);
                    float target_orientation = current_orientation + Vector3.SignedAngle(InputLayer.capture.movementDirection, desired_direction, Vector3.up);
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
                        foreach (Ability ability in GetComponents(typeof(Ability)))
                        {
                            // --- If any ability reacts to the collision, break ---
                            if (ability.OnContact(ref synthesizer, contactTransform, deltaTime))
                            {
                                contactAbility = ability;
                                break;
                            }
                        }
                    }

                    attemptTransition = false; // make sure we do not react to another collision
                }
                else if (!closure.isGrounded) // we are dropping/falling down
                {
                    // --- Let other abilities take control on drop ---
                    if (contactAbility == null)
                    {
                        foreach (Ability ability in GetComponents(typeof(Ability)))
                        {
                            // --- If any ability reacts to the drop, break ---
                            if (ability.OnDrop(ref synthesizer, deltaTime))
                            {
                                contactAbility = ability;
                                break;
                            }
                        }
                    }

                    if (!freedrop)
                    {
                        // --- Compute distance to fall point ---
                        Vector3 futurepos;
                        futurepos.x = controller.current.position.x;
                        futurepos.y = controller.current.position.y;
                        futurepos.z = controller.current.position.z;
                        distance_to_fall = Mathf.Abs((futurepos - gameObject.transform.position).magnitude) - brakeDistance;
                        break;
                    }
                }
                else
                {
                    distance_to_fall = maxFallPredictionDistance; // reset distance once no fall is predicted
                }

                transform.t = worldRootTransform.inverseTransform(controller.Position);
                prediction.Transform = transform;
            }

            return contactAbility;
        }

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {
            return false;
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            return false;
        }

        public void OnAbilityAnimatorMove() // called by ability controller at OnAnimatorMove()
        {
            if (kinematica.Synthesizer.IsValid)
            {
                ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

                // --- Influence motion using locomotion ability attributes ---
                AffineTransform rootMotion = synthesizer.SteerRootMotion(trajectory, correctTranslationPercentage, correctRotationPercentage, correctMotionStartSpeed, correctMotionEndSpeed);
                AffineTransform rootTransform = AffineTransform.Create(transform.position, transform.rotation) * rootMotion;
                synthesizer.SetWorldTransform(AffineTransform.Create(rootTransform.t, rootTransform.q), true);
            }
        }

        // -------------------------------------------------

        // --- Utilities ---

        float GetDesiredSpeed(ref MotionSynthesizer synthesizer)        
        {
            float desiredSpeed = 0.0f;

            // --- If we are idle ---
            if (InputLayer.capture.moveIntensity == 0.0f)
            {
                if (!isBraking && math.length(synthesizer.CurrentVelocity) < brakingSpeed)
                    isBraking = true;
            }
            else
            {
                isBraking = false;
                desiredSpeed = InputLayer.capture.moveIntensity * desiredLinearSpeed;
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
            if (ledgeGeometry.vertices[0].Equals(float3.zero) || freedrop)
                return;

            Vector3 pos = gameObject.transform.position;
            ledgeGeometry.LimitTransform(ref pos, 0.1f);
            gameObject.transform.position = pos;
            AffineTransform worldRootTransform = AffineTransform.Create(pos, kinematica.Synthesizer.Ref.WorldRootTransform.q);
            kinematica.Synthesizer.Ref.SetWorldTransform(worldRootTransform, true);
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}
