using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;

namespace SnappyLocomotion
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

            Synthesizer.MatchPoseAndTrajectory(locomotionCandidates, Synthesizer.Time, trajectory, MatchOptions.None, 0.7f);
        }
    }


    [RequireComponent(typeof(Kinematica))]
    public class SnappyLocomotion : SnapshotProvider
    {
        [Tooltip("How much character translation should match desired trajectory as opposed to the binary.")]
        [Range(0.0f, 1.0f)]
        public float snapTranslationFactor = 1.0f;

        [Tooltip("How much character rotation should match desired trajectory as opposed to the binary.")]
        [Range(0.0f, 1.0f)]
        public float snapRotationFactor = 1.0f;

        CharacterController controller;
        Kinematica kinematica;

        PoseSet idleCandidates;
        PoseSet locomotionCandidates;
        Trajectory trajectory;

        [Snapshot]
        float moveIntensity = 0.0f;

        [Snapshot]
        float3 movementDirection = Missing.forward;

        public override void OnEnable()
        {
            base.OnEnable();

            controller = GetComponent<CharacterController>();
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

        public virtual void OnAnimatorMove()
        {
            if (kinematica.Synthesizer.IsValid)
            {
                ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

                AffineTransform rootDelta = synthesizer.SteerRootMotion(trajectory, snapTranslationFactor, snapRotationFactor);

                float3 rootTranslation = transform.rotation * rootDelta.t;
                transform.rotation *= rootDelta.q;
                
    #if UNITY_EDITOR
                if (Unity.SnapshotDebugger.Debugger.instance.rewind)
                {
                    return;
                }
    #endif
                controller.Move(rootTranslation);
                synthesizer.SetWorldTransform(AffineTransform.Create(transform.position, transform.rotation), true);
            }
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            Utility.GetInputMove(ref movementDirection, ref moveIntensity);
        }

        void Update()
        {
            float desiredSpeed = moveIntensity * 3.9f;

            TrajectoryPrediction.CreateFromDirection(ref kinematica.Synthesizer.Ref,
                movementDirection,
                desiredSpeed,
                trajectory,
                0.2f,
                0.1f).Generate();

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
            InputUtility.DisplayMissingInputs(InputUtility.MoveInput);
        }
    }
}
