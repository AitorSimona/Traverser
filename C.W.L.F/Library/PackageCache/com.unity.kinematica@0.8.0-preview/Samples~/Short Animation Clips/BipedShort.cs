using UnityEngine;
using Unity.SnapshotDebugger;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace ShortAnimationClips
{
    [BurstCompile(CompileSynchronously = true)]
    public struct KinematicaJob : IJob
    {
        public MemoryRef<MotionSynthesizer> synthesizer;

        public PoseSet idleCandidates;

        public PoseSet locomotionCandidates;

        public Trajectory trajectory;

        public bool idle;

        ref MotionSynthesizer Synthesizer => ref synthesizer.Ref;

        public void Execute()
        {
            if (idle && Synthesizer.MatchPose(idleCandidates, Synthesizer.Time, MatchOptions.DontMatchIfCandidateIsPlaying | MatchOptions.LoopSegment, 0.01f))
            {
                return;
            }

            Synthesizer.MatchPoseAndTrajectory(locomotionCandidates, Synthesizer.Time, trajectory, MatchOptions.None, 0.4f, 0.01f);
        }
    }

    [RequireComponent(typeof(Kinematica))]
    public class BipedShort : SnapshotProvider
    {
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

        Kinematica kinematica;

        PoseSet idleCandidates;
        PoseSet locomotionCandidates;
        Trajectory trajectory;

        [Snapshot]
        float moveIntensity = 0.0f;

        [Snapshot]
        float3 movementDirection = Missing.forward;

        float desiredLinearSpeed => InputUtility.IsPressingActionButton() ? desiredSpeedFast : desiredSpeedSlow;

        public override void OnEnable()
        {
            base.OnEnable();

            kinematica = GetComponent<Kinematica>();
            ref var synthesizer = ref kinematica.Synthesizer.Ref;

            idleCandidates = synthesizer.Query.Where("Idle", Locomotion.Default).And(Idle.Default);
            locomotionCandidates = synthesizer.Query.Where("Locomotion", Locomotion.Default).Except(Idle.Default);
            trajectory = synthesizer.CreateTrajectory(Allocator.Persistent);

            synthesizer.PlayFirstSequence(idleCandidates);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            idleCandidates.Dispose();
            locomotionCandidates.Dispose();
            trajectory.Dispose();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            Utility.GetInputMove(ref movementDirection, ref moveIntensity);
        }

        void Update()
        {
            ref var synthesizer = ref kinematica.Synthesizer.Ref;
            
            float factor = 0.5f + math.abs(math.dot(
                Missing.zaxis(synthesizer.WorldRootTransform.q),
                    movementDirection)) * 0.5f;

            float desiredSpeed = moveIntensity * desiredLinearSpeed * factor;

            TrajectoryPrediction.CreateFromDirection(ref kinematica.Synthesizer.Ref,
                movementDirection,
                desiredSpeed,
                trajectory,
                velocityPercentage,
                forwardPercentage).Generate();

            KinematicaJob job = new KinematicaJob()
            {
                synthesizer = kinematica.Synthesizer,
                idleCandidates = idleCandidates,
                locomotionCandidates = locomotionCandidates,
                trajectory = trajectory,
                idle = moveIntensity == 0.0f
            };

            kinematica.AddJobDependency(job.Schedule());
        }

        void OnGUI()
        {
            InputUtility.DisplayMissingInputs(InputUtility.ActionButtonInput | InputUtility.MoveInput | InputUtility.CameraInput);
        }
    }
}
