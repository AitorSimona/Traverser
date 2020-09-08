using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    [Data("Navigation Path")]
    public struct NavigationPath : IDisposable, IDebugObject, IDebugDrawable, Serializable
    {
        AffineTransform startTransform;
        NativeArray<float3> controlPoints;
        Allocator allocator;
        NavigationParams navParams;

        Spline pathSpline;
        int nextControlPoint;
        BlittableBool isBuilt;

        public DebugIdentifier debugIdentifier { get; set; }

        [Output("Trajectory", OutputFlags.OnlyAcceptSelfNode)]
        public DebugIdentifier outputTrajectory;

        public static NavigationPath CreateInvalid()
        {
            return new NavigationPath()
            {
                allocator = Allocator.Invalid
            };
        }

        public static NavigationPath Create(float3[] controlPoints, AffineTransform startTransform, NavigationParams navParams, Allocator allocator)
        {
            return new NavigationPath()
            {
                startTransform = startTransform,
                controlPoints = new NativeArray<float3>(controlPoints, allocator),
                allocator = allocator,
                navParams = navParams,
                pathSpline = Spline.Create(controlPoints.Length, allocator),
                nextControlPoint = 0,
                isBuilt = false
            };
        }

        public bool IsValid => allocator != Allocator.Invalid;

        public void Build()
        {
            pathSpline.BuildSpline(startTransform.t, startTransform.Forward, controlPoints, float3.zero, navParams.pathCurvature);
            isBuilt = true;
        }

        public void Dispose()
        {
            if (IsValid)
            {
                controlPoints.Dispose();
                pathSpline.Dispose();
            }
        }

        public bool IsBuilt => isBuilt;

        public int NumControlPoints => controlPoints.Length;

        public bool GoalReached => nextControlPoint >= NumControlPoints;

        public int NextControlPoint => nextControlPoint;

        public NavigationParams NavParams => navParams;

        public bool UpdateAgentTransform(AffineTransform agentTransform)
        {
            if (GoalReached)
            {
                return false;
            }

            float3 nextControlPointPos = pathSpline.segments[nextControlPoint].OutPosition;
            float controlPointRadius = nextControlPoint >= NumControlPoints - 1 ? navParams.finalControlPointRadius : navParams.intermediateControlPointRadius;
            if (math.distancesq(agentTransform.t, nextControlPointPos) <= controlPointRadius * controlPointRadius)
            {
                ++nextControlPoint;
                if (GoalReached)
                {
                    return false;
                }
            }

            // use agent transform as first control point of the spline (starting from nextControlPoint, segments before are discarded)
            HermitCurve segment = pathSpline.segments[nextControlPoint];

            segment = HermitCurve.Create(
                agentTransform.t,
                agentTransform.Forward,
                segment.OutPosition,
                segment.OutTangent,
                navParams.pathCurvature);

            pathSpline.segments[nextControlPoint] = segment;

            return true;
        }

        public SplinePoint EvaluatePointAtDistance(float distance)
        {
            return pathSpline.EvaluatePointAtDistance(distance, nextControlPoint);
        }

        public void GenerateTrajectory(ref MotionSynthesizer synthesizer, ref Trajectory trajectory)
        {
            if (GoalReached)
            {
                return;
            }

            Assert.IsTrue(trajectory.Length > 0);
            if (trajectory.Length == 0)
            {
                return;
            }

            AffineTransform rootTransform = synthesizer.WorldRootTransform;

            float maxSpeedAtCorner = navParams.desiredSpeed;
            if (nextControlPoint < pathSpline.segments.Length - 1)
            {
                float3 curSegmentDir = pathSpline.segments[nextControlPoint].SegmentDirection;
                float3 nextSegmentDir = pathSpline.segments[nextControlPoint + 1].SegmentDirection;

                float alignment = math.max(math.dot(curSegmentDir, nextSegmentDir), 0.0f);
                maxSpeedAtCorner = math.lerp(navParams.maxSpeedAtRightAngle, navParams.desiredSpeed, alignment);
            }

            int halfTrajectoryLength = trajectory.Length / 2;

            float deltaTime = synthesizer.Binary.TimeHorizon / halfTrajectoryLength;
            float distance = 0.0f;

            float speed = math.length(synthesizer.CurrentVelocity);
            float remainingDistOnSpline = pathSpline.ComputeCurveLength(nextControlPoint);
            float remainingDistOnSegment = pathSpline.segments[nextControlPoint].CurveLength;

            for (int index = halfTrajectoryLength; index < trajectory.Length; ++index)
            {
                if (remainingDistOnSpline > 0.0f)
                {
                    // acceleration to reach desired speed
                    float acceleration = math.clamp((navParams.desiredSpeed - speed) / deltaTime,
                        -navParams.maximumDeceleration,
                        navParams.maximumAcceleration);

                    // decelerate if needed to reach maxSpeedAtCorner
                    float brakingDistance = 0.0f;
                    if (remainingDistOnSegment > 0.0f && speed > maxSpeedAtCorner)
                    {
                        brakingDistance = NavigationParams.ComputeDistanceToReachSpeed(speed, maxSpeedAtCorner, -navParams.maximumDeceleration);
                        if (remainingDistOnSegment <= brakingDistance)
                        {
                            acceleration = math.min(acceleration, NavigationParams.ComputeAccelerationToReachSpeed(speed, maxSpeedAtCorner, remainingDistOnSegment));
                        }
                    }

                    // decelerate if needed to stop when last control point is reached
                    brakingDistance = NavigationParams.ComputeDistanceToReachSpeed(speed, 0.0f, -navParams.maximumDeceleration);
                    if (remainingDistOnSpline <= brakingDistance)
                    {
                        acceleration = math.min(acceleration, NavigationParams.ComputeAccelerationToReachSpeed(speed, 0.0f, remainingDistOnSpline));
                    }

                    speed += acceleration * deltaTime;
                }
                else
                {
                    speed = 0.0f;
                }

                float moveDist = speed * deltaTime;
                remainingDistOnSegment -= moveDist;
                remainingDistOnSpline -= moveDist;
                distance += moveDist;

                AffineTransform point = EvaluatePointAtDistance(distance);
                trajectory[index] = rootTransform.inverseTimes(point);
            }

            synthesizer.DebugPushGroup();

            synthesizer.DebugWriteUnblittableObject(ref trajectory);
            outputTrajectory = trajectory.debugIdentifier;

            synthesizer.DebugWriteUnblittableObject(ref this);
        }

        public void DrawPath()
        {
            if (GoalReached)
            {
                return;
            }

            float pathLength = pathSpline.ComputeCurveLength(nextControlPoint);
            if (pathLength <= 0.0f)
            {
                return;
            }

            float stepDist = 0.2f;
            int lines = (int)math.floor(pathLength / stepDist);

            float3 position = pathSpline.EvaluatePointAtDistance(0.0f, nextControlPoint).position;
            for (int i = 0; i < lines; ++i)
            {
                float distance = ((i + 1) / (float)lines) * pathLength;
                float3 nextPosition = pathSpline.EvaluatePointAtDistance(distance, nextControlPoint).position;

                DebugDraw.DrawLine(position, nextPosition, Color.white);

                position = nextPosition;
            }

            for (int i = 0; i < pathSpline.segments.Length; ++i)
            {
                float radius = (i == pathSpline.segments.Length - 1) ? navParams.finalControlPointRadius : navParams.intermediateControlPointRadius;
                DebugDraw.DrawSphere(pathSpline.segments[i].OutPosition, quaternion.identity, radius, (nextControlPoint <= i) ? Color.red : Color.white);
            }
        }

        public void WriteToStream(Unity.SnapshotDebugger.Buffer buffer)
        {
            buffer.Write(startTransform);
            buffer.WriteNativeArray(controlPoints, allocator);
            buffer.WriteBlittable(navParams);
            pathSpline.WriteToStream(buffer);
            buffer.Write(nextControlPoint);
            buffer.WriteBlittable(isBuilt);
            buffer.WriteBlittable(outputTrajectory);
        }

        public void ReadFromStream(Unity.SnapshotDebugger.Buffer buffer)
        {
            startTransform = buffer.ReadAffineTransform();
            controlPoints = buffer.ReadNativeArray<float3>(out allocator);
            navParams = buffer.ReadBlittable<NavigationParams>();
            pathSpline.ReadFromStream(buffer);
            nextControlPoint = buffer.Read32();
            isBuilt = buffer.ReadBlittable<BlittableBool>();
            outputTrajectory = buffer.ReadBlittable<DebugIdentifier>();
        }

        public void Draw(Camera camera, ref MotionSynthesizer synthesizer, DebugMemory debugMemory, SamplingTime debugSamplingTime, ref DebugDrawOptions options)
        {
            DrawPath();
        }
    }
}
