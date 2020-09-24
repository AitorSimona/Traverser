using UnityEngine;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.SnapshotDebugger;

namespace CWLF
{
    [BurstCompile(CompileSynchronously = true)] //Burst is primarily designed to work efficiently with the Job system.
    public struct LocomotionJob : IJob // multhithreaded code, pose prediction will be executed faster
    {
        public MemoryRef<MotionSynthesizer> synthesizer;

        public PoseSet idleCandidates;

        public PoseSet locomotionCandidates;

        public Trajectory trajectory; // struct that holds a trajectory (array of transforms)

        public bool idle;

        ref MotionSynthesizer Synthesizer => ref synthesizer.Ref; // reference to the synthesizer

        public void Execute()
        {
            if (idle && Synthesizer.MatchPose(idleCandidates, Synthesizer.Time, MatchOptions.DontMatchIfCandidateIsPlaying | MatchOptions.LoopSegment, 0.01f))
            {
                return;
            }

            Synthesizer.MatchPoseAndTrajectory(locomotionCandidates, Synthesizer.Time, trajectory);
        }
    }

    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(MovementController))]
    public class LocomotionAbility : SnapshotProvider, Ability // SnapshotProvider is derived from MonoBehaviour, allows the use of kinematica's snapshot debugger
    {
        // --- Inspector variables ---
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

        // --- Internal variables ---
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

        // --- Ability class methods ---
        public Ability OnUpdate(float deltaTime)
        {
            return this;
        }

        public bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime)
        {
            return false;
        }

        public bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime)
        {
            return false;
        }
    }

}
