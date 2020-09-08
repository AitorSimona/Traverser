using UnityEngine;
using Unity.Kinematica;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using System;
using UnityEngine.AI;
using Unity.SnapshotDebugger;

namespace Navigation
{
    [BurstCompile(CompileSynchronously = true)]
    public struct KinematicaJob : IJob
    {
        public MemoryRef<MotionSynthesizer> synthesizer;

        public PoseSet idleCandidates;

        public PoseSet locomotionCandidates;

        public Trajectory trajectory;

        public MemoryRef<NavigationPath> navigationPath;

        ref MotionSynthesizer Synthesizer => ref synthesizer.Ref;

        ref NavigationPath NavPath => ref navigationPath.Ref;

        public void Execute()
        {
            bool goalReached = true;

            if (navigationPath.IsValid)
            {
                if (!NavPath.IsBuilt)
                {
                    NavPath.Build();
                }

                goalReached = NavPath.GoalReached || !NavPath.UpdateAgentTransform(Synthesizer.WorldRootTransform);
            }
            
            if (goalReached)
            {
                Synthesizer.ClearTrajectory(trajectory);

                if (Synthesizer.MatchPose(idleCandidates, Synthesizer.Time, MatchOptions.DontMatchIfCandidateIsPlaying | MatchOptions.LoopSegment, 0.01f))
                {
                    return;
                }
            }
            else
            {
                NavPath.GenerateTrajectory(ref Synthesizer, ref trajectory);  
            }

            Synthesizer.MatchPoseAndTrajectory(locomotionCandidates, Synthesizer.Time, trajectory);
        }
    }

    [RequireComponent(typeof(Kinematica))]
    public class Navigation : SnapshotProvider
    {
        [Header("Prediction settings")]
        [Tooltip("Desired speed in meters per second for slow movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedSlow = 3.9f;

        [Tooltip("Desired speed in meters per second for fast movement.")]
        [Range(0.0f, 10.0f)]
        public float desiredSpeedFast = 5.5f;

        public Transform targetMarker;

        Kinematica kinematica;

        PoseSet idleCandidates;
        PoseSet locomotionCandidates;
        Trajectory trajectory;

        [Snapshot]
        NavigationPath navigationPath;

        float desiredLinearSpeed => InputUtility.IsPressingActionButton() ? desiredSpeedFast : desiredSpeedSlow;

        public override void OnEnable()
        {
            base.OnEnable();

            kinematica = GetComponent<Kinematica>();
            ref var synthesizer = ref kinematica.Synthesizer.Ref;

            idleCandidates = synthesizer.Query.Where("Idle", Locomotion.Default).And(Idle.Default);
            locomotionCandidates = synthesizer.Query.Where("Locomotion", Locomotion.Default).Except(Idle.Default);
            trajectory = synthesizer.CreateTrajectory(Allocator.Persistent);

            navigationPath = NavigationPath.CreateInvalid();

            synthesizer.PlayFirstSequence(idleCandidates);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            idleCandidates.Dispose();
            locomotionCandidates.Dispose();
            trajectory.Dispose();
            navigationPath.Dispose();
        }

        public override void OnEarlyUpdate(bool rewind)
        {
            base.OnEarlyUpdate(rewind);

            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    Vector3 targetPosition = hit.point;

                    float desiredSpeed = 1.5f;
                    float accelerationDistance = 0.1f;
                    float decelerationDistance = 1.0f;

                    NavMeshPath navMeshPath = new NavMeshPath();
                    if (NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, navMeshPath))
                    {
                        var navParams = new NavigationParams()
                        {
                            desiredSpeed = desiredSpeed,
                            maxSpeedAtRightAngle = 0.0f,
                            maximumAcceleration = NavigationParams.ComputeAccelerationToReachSpeed(desiredSpeed, accelerationDistance),
                            maximumDeceleration = NavigationParams.ComputeAccelerationToReachSpeed(desiredSpeed, decelerationDistance),
                            intermediateControlPointRadius = 1.0f,
                            finalControlPointRadius = 0.15f,
                            pathCurvature = 5.0f
                        };

                        float3[] points = Array.ConvertAll(navMeshPath.corners, pos => new float3(pos));

                        navigationPath.Dispose();
                        navigationPath = NavigationPath.Create(points, AffineTransform.CreateGlobal(transform), navParams, Allocator.Persistent);

                        targetMarker.gameObject.SetActive(true);
                        targetMarker.position = targetPosition;
                    }
                }
            }
        }

        void Update()
        {
            KinematicaJob job = new KinematicaJob()
            {
                synthesizer = kinematica.Synthesizer,
                idleCandidates = idleCandidates,
                locomotionCandidates = locomotionCandidates,
                trajectory = trajectory,
                navigationPath = navigationPath.IsValid ? new MemoryRef<NavigationPath>(ref navigationPath) : MemoryRef<NavigationPath>.Null,
            };
            kinematica.AddJobDependency(job.Schedule());
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 900, 20), "Click somewhere on the ground to move character toward that location.");
        }
    }
}
