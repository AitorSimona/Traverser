using UnityEngine;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.SnapshotDebugger;

namespace CWLF
{
    [BurstCompile(CompileSynchronously = true)] //Burst is primarily designed to work efficiently with the Job system.
    public struct KinematicaJob : IJob // multhithreaded code, pose prediction will be executed faster
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


    [RequireComponent(typeof(Kinematica))]
    public class Biped : SnapshotProvider // Derived from MonoBehaviour, allows the use of kinematica's snapshot debugger
    {
        // --- Inspector variables ---
        [Header("Prediction settings")]
        [Tooltip("Desired speed in meters per second for slow movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedSlow = 3.0f;

        [Tooltip("Desired speed in meters per second for fast movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedFast = 5.5f;

        [Tooltip("How fast or slow the target velocity is supposed to be reached.")]
        [Range(0.0f, 1.0f)]
        public float velocityPercentage = 1.0f;

        [Tooltip("How fast or slow the desired forward direction is supposed to be reached.")]
        [Range(0.0f, 1.0f)]
        public float forwardPercentage = 1.0f;

        // --- Internal variables ---
        Kinematica kinematica;

        PoseSet idleCandidates;
        PoseSet locomotionCandidates;
        Trajectory trajectory;

        [Snapshot]
        float3 movementDirection = Missing.forward;

        [Snapshot]
        float moveIntensity = 0.0f;

        float desiredLinearSpeed => InputUtility.IsPressingActionButton() ? desiredSpeedFast : desiredSpeedSlow;

        // --- Methods ---
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

            Utility.GetInputMove(ref movementDirection, ref moveIntensity);
        }

        void Update()
        {
            float desiredSpeed = moveIntensity * desiredLinearSpeed;

            // --- Create final trajectory from given parameters ---
            TrajectoryPrediction.CreateFromDirection(ref kinematica.Synthesizer.Ref,
                movementDirection,
                desiredSpeed,
                trajectory,
                velocityPercentage,
                forwardPercentage).Generate();

            // --- Set up a job so pose prediction runs faster (multithreading) ---
            KinematicaJob job = new KinematicaJob()
            {
                synthesizer = kinematica.Synthesizer,
                idleCandidates = idleCandidates,
                locomotionCandidates = locomotionCandidates,
                trajectory = trajectory,
                idle = moveIntensity == 0.0f
            };

            // --- Finally tell kinematica to wait for this job to finish before executing other stuff ---
            kinematica.AddJobDependency(job.Schedule());
        }

        private void OnGUI()
        {
            InputUtility.DisplayMissingInputs(InputUtility.ActionButtonInput | InputUtility.MoveInput | InputUtility.CameraInput);
        }
    }
}