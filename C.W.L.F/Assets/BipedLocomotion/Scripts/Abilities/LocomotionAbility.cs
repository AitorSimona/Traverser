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

        ref MotionSynthesizer Synthesizer => ref synthesizer.Ref; // reference to the synthesizer

        // -------------------------------------------------
        public void Execute()
        {
            if (idle && Synthesizer.MatchPose(idlePoses, Synthesizer.Time, MatchOptions.DontMatchIfCandidateIsPlaying | MatchOptions.LoopSegment, 0.01f))
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

        // -------------------------------------------------

        Kinematica kinematica;

        PoseSet idleCandidates;
        PoseSet locomotionCandidates;
        Trajectory trajectory;

        [Snapshot]
        float3 movementDirection = Missing.forward;

        [Snapshot]
        float moveIntensity = 0.0f;

        [Snapshot]
        bool run;

        [Snapshot]
        bool isBraking = false;

        [Snapshot]
        float3 rootVelocity = float3.zero;

        float desiredLinearSpeed => run ? desiredSpeedFast : desiredSpeedSlow;

        // -------------------------------------------------

        // --- Info about current animation, to help on braking ---
        struct SamplingTimeInfo
        {
            public bool isLocomotion;
            public bool hasReachedEndOfSegment;
        }

        // -------------------------------------------------

        // --- Basic Methods ---
        public override void OnEnable()
        {
            base.OnEnable();
            kinematica = GetComponent<Kinematica>();
            ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

            // --- Initialize arrays ---
            idleCandidates = synthesizer.Query.Where("Idle", Locomotion.Default).And(Idle.Default);
            locomotionCandidates = synthesizer.Query.Where("Locomotion", Locomotion.Default).Except(Idle.Default);
            trajectory = synthesizer.CreateTrajectory(Allocator.Persistent);

            // --- Play default animation ---
            synthesizer.PlayFirstSequence(idleCandidates);

            rootVelocity = synthesizer.CurrentVelocity;

        }

        public override void OnDisable()
        {
            base.OnDisable();

            // --- Delete arrays ---
            idleCandidates.Dispose();
            locomotionCandidates.Dispose();
            trajectory.Dispose();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            if (!rewind) // if we are not using snapshot debugger to rewind
            {
                Utility.GetInputMove(ref movementDirection, ref moveIntensity);
                run = Input.GetButton("A Button");
            }
        }

        // -------------------------------------------------

        // --- Ability class methods ---
        public Ability OnUpdate(float deltaTime)
        {
            Ability ret = this;
            ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

            float desiredSpeed = GetDesiredSpeed(ref synthesizer);

            // --- Ensure stop animation is properly played, that we do not change to other animations ---
            float minTrajectoryDeviation = HandleBraking();

            // --- We perform future movement to check for collisions, then rewind using snapshot debugger's capabilities ---
            Ability contactAbility = HandleMovementPrediction(ref synthesizer, desiredSpeed, deltaTime);

            // --- Set up a job so pose prediction runs faster (multithreading) ---
            LocomotionJob job = new LocomotionJob()
            {
                synthesizer = kinematica.Synthesizer,
                idlePoses = idleCandidates,
                locomotionPoses = locomotionCandidates,
                trajectory = trajectory,
                idle = moveIntensity == 0.0f,
                minTrajectoryDeviation = minTrajectoryDeviation,
                responsiveness = responsiveness
            };

            // --- Finally tell kinematica to wait for this job to finish before executing other stuff ---
            kinematica.AddJobDependency(job.Schedule());

            // --- Another ability has been triggered ---
            if (contactAbility != null)
                ret = contactAbility;

            return ret;
        }

        float HandleBraking()
        {
            // --- Kinematica comment ---

            // If character is braking, we set a strong deviation threshold on Trajectory Heuristic to be conservative (candidate would need to be a LOT better to be picked)
            // because then we want the character to pick a stop clip in the library and stick to it even if Kinematica can jump to a better clip (cost wise) in the middle 
            // of that stop animation. Indeed stop animations have very subtle foot steps (to reposition to idle stance) that would be squeezed by blend/jumping from clip to clip.
            // Moreover, playing a stop clip from start to end will make sure we will reach a valid transition point to idle.
           
            SamplingTimeInfo samplingTimeInfo = GetSamplingTimeInfo();

            float minTrajectoryDeviation = 0.03f; // default threshold

            if (samplingTimeInfo.isLocomotion)
            {
                if (isBraking)
                {
                    minTrajectoryDeviation = 0.25f; // high threshold to let stop animation finish
                }
            }
            else if (samplingTimeInfo.hasReachedEndOfSegment)
            {
                minTrajectoryDeviation = 0.0f; // we are not playing a locomotion segment and we reach the end of that segment, we must force a transition, otherwise character will freeze in the last position
            }

            return minTrajectoryDeviation;
        }

        Ability HandleMovementPrediction(ref MotionSynthesizer synthesizer, float desiredSpeed, float deltaTime)
        {
            // --- Create final trajectory from given parameters ---
            TrajectoryPrediction prediction = TrajectoryPrediction.CreateFromDirection(ref kinematica.Synthesizer.Ref,
               movementDirection,
               desiredSpeed,
               trajectory,
               velocityPercentage,
               forwardPercentage);

            MovementController controller = GetComponent<MovementController>();

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
                    float3 contactPoint = closure.colliderContactPoint;
                    contactPoint.y = controller.Position.y;
                    float3 contactNormal = closure.colliderContactNormal;
                    quaternion q = math.mul(transform.q, Missing.forRotation(Missing.zaxis(transform.q),contactNormal));

                    AffineTransform contactTransform = new AffineTransform(contactPoint, q);

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
            Kinematica kinematica = GetComponent<Kinematica>();

            if (kinematica.Synthesizer.IsValid)
            {
                ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

                // --- Influence motion using locomotion ability attributes ---
                AffineTransform rootMotion = synthesizer.SteerRootMotion(trajectory, correctTranslationPercentage, correctRotationPercentage, correctMotionStartSpeed, correctMotionEndSpeed);
                AffineTransform rootTransform = AffineTransform.Create(transform.position, transform.rotation) * rootMotion;
                synthesizer.SetWorldTransform(AffineTransform.Create(rootTransform.t, rootTransform.q), true);

                if (synthesizer.deltaTime >= 0.0f)
                {
                    rootVelocity = rootMotion.t / synthesizer.deltaTime;
                }
            }
        }

        // -------------------------------------------------

        // --- Utilities ---
        SamplingTimeInfo GetSamplingTimeInfo()
        {
            // --- Find out if current animation is a locomotive one and if it has ended (for braking) ---

            SamplingTimeInfo samplingTimeInfo = new SamplingTimeInfo()
            {
                isLocomotion = false,
                hasReachedEndOfSegment = false
            };

            Kinematica kinematica = GetComponent<Kinematica>();
            ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;
            ref Binary binary = ref synthesizer.Binary;

            SamplingTime samplingTime = synthesizer.Time;

            ref Binary.Segment segment = ref binary.GetSegment(samplingTime.timeIndex.segmentIndex);
            ref Binary.Tag tag = ref binary.GetTag(segment.tagIndex);
            ref Binary.Trait trait = ref binary.GetTrait(tag.traitIndex);

            if (trait.typeIndex == binary.GetTypeIndex<Locomotion>())
            {
                samplingTimeInfo.isLocomotion = true;
            }
            else if (samplingTime.timeIndex.frameIndex >= segment.destination.numFrames - 1)
            {
                samplingTimeInfo.hasReachedEndOfSegment = true;
            }

            return samplingTimeInfo;
        }

        float GetDesiredSpeed(ref MotionSynthesizer synthesizer)
        {
            float desiredSpeed = 0.0f;

            // --- If we are idle ---
            if (moveIntensity == 0.0f)
            {
                if (!isBraking && math.length(synthesizer.CurrentVelocity) < brakingSpeed)
                    isBraking = true;
            }
            else
            {
                isBraking = false;
                desiredSpeed = moveIntensity * desiredLinearSpeed;
            }

            return desiredSpeed;
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}
