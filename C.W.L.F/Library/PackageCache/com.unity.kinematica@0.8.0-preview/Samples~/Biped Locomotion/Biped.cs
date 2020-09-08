using UnityEngine;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.SnapshotDebugger;

namespace BipedLocomotion
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

            Synthesizer.MatchPoseAndTrajectory(locomotionCandidates, Synthesizer.Time, trajectory);
        }
    }

    [RequireComponent(typeof(Kinematica))]
    public class Biped : SnapshotProvider
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
        float3 movementDirection = Missing.forward;

        [Snapshot]
        float moveIntensity = 0.0f;

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
            float desiredSpeed = moveIntensity * desiredLinearSpeed;

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
