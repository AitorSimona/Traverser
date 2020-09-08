using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using Buffer = Unity.SnapshotDebugger.Buffer;

namespace Unity.Kinematica
{
    public struct SplinePoint
    {
        public float3 position;
        public float3 tangent;

        public static implicit operator AffineTransform(SplinePoint point)
        {
            float3 forward = point.tangent;
            forward.y = 0.0f;
            forward = math.normalizesafe(forward, new float3(0.0f, 0.0f, 1.0f));

            return new AffineTransform(point.position, quaternion.LookRotation(forward, new float3(0.0f, 1.0f, 0.0f)));
        }
    }

    internal struct Spline : Serializable, IDisposable
    {
        public NativeArray<HermitCurve> segments;
        public Allocator allocator;

        public static Spline Create(int numControlPoints, Allocator allocator)
        {
            int pointCount = numControlPoints + 1;
            int segmentCount = pointCount - 1;

            return new Spline()
            {
                segments = new NativeArray<HermitCurve>(segmentCount, allocator),
                allocator = allocator
            };
        }

        public void BuildSpline(float3 inPosition, float3 inTangent, NativeArray<float3> controlPoints, float3 outTangent, float curvature)
        {
            int pointCount = controlPoints.Length + 1;
            int segmentCount = pointCount - 1;

            segments[0] = HermitCurve.Create(
                inPosition,
                inTangent,
                controlPoints[0],
                segmentCount == 1 ? outTangent : (controlPoints[1] - inPosition),
                curvature);

            for (int i = 1; i < segmentCount; ++i)
            {
                float3 previousPoint = i == 1 ? inPosition : controlPoints[i - 2];

                float3 segmentInTangent = controlPoints[i] - previousPoint;
                float3 segmentOutTangent = i == (segmentCount - 1) ? outTangent : (controlPoints[i + 1] - controlPoints[i - 1]);

                segments[i] = HermitCurve.Create(
                    controlPoints[i - 1],
                    segmentInTangent,
                    controlPoints[i],
                    segmentOutTangent,
                    curvature);
            }
        }

        public void Dispose()
        {
            segments.Dispose();
        }

        public float ComputeCurveLength(int startSegment = 0)
        {
            float length = 0.0f;

            for (int i = startSegment; i < segments.Length; ++i)
            {
                length += segments[i].CurveLength;
            }

            return length;
        }

        public SplinePoint EvaluatePointAtDistance(float distance, int startSegment = 0)
        {
            float remainingDistance = distance;

            for (int i = startSegment; i < segments.Length; ++i)
            {
                if (remainingDistance <= segments[i].CurveLength)
                {
                    SplinePoint point = segments[i].EvaluatePointAtDistance(remainingDistance);
                    return point;
                }
                remainingDistance -= segments[i].CurveLength;
            }

            return new SplinePoint()
            {
                position = segments[segments.Length - 1].OutPosition,
                tangent = segments[segments.Length - 1].OutTangent
            };
        }

        public void WriteToStream(Unity.SnapshotDebugger.Buffer buffer)
        {
            buffer.WriteNativeArray(segments, allocator);
        }

        public void ReadFromStream(Unity.SnapshotDebugger.Buffer buffer)
        {
            segments = buffer.ReadNativeArray<HermitCurve>(out allocator);
        }
    }
}
