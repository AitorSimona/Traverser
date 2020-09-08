using Unity.Mathematics;

namespace Unity.Kinematica
{
    internal struct HermitCurve
    {
        public static HermitCurve Create(float3 inPos, float3 inTangent, float3 outPos, float3 outTangent, float curvature)
        {
            float length = math.distance(inPos, outPos);

            HermitCurve curve = new HermitCurve()
            {
                p0 = inPos,
                p1 = outPos,
                m0 = math.normalizesafe(inTangent, float3.zero) * math.min(length, curvature),
                m1 = math.normalizesafe(outTangent, float3.zero) * math.min(length, curvature)
            };

            curve.subdivs = math.max((int)math.floor(length / SubdivLength), 3);
            curve.ComputeCurveLength();

            return curve;
        }

        static float SubdivLength => 1.0f;

        public float3 InPosition => p0;
        public float3 OutPosition => p1;
        public float3 InTangent => m0;
        public float3 OutTangent => m1;

        public float3 SegmentDirection => math.normalizesafe(p1 - p0, float3.zero);

        public float CurveLength => curveLength;

        public float3 EvaluatePosition(float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float h00 = 2.0f * t3 - 3.0f * t2 + 1.0f;
            float h10 = t3 - 2.0f * t2 + t;
            float h01 = -2.0f * t3 + 3.0f * t2;
            float h11 = t3 - t2;

            return p0 * h00 + m0 * h10 + p1 * h01 + m1 * h11;
        }

        public float3 EvaluateTangent(float t)
        {
            float t2 = t * t;

            float h00 = 6.0f * (t2 - t);
            float h10 = 3.0f * t2 - 4.0f * t + 1.0f;
            float h01 = 6.0f * (t - t2);
            float h11 = 3.0f * t2 - 2.0f * t;

            return p0 * h00 + m0 * h10 + p1 * h01 + m1 * h11;
        }

        public SplinePoint EvaluatePointAtDistance(float distance)
        {
            float t = DistanceToTime(distance);
            return new SplinePoint()
            {
                position = EvaluatePosition(t),
                tangent = EvaluateTangent(t),
            };
        }

        public float DistanceToTime(float distance)
        {
            float remainingDistance = distance;

            float dt = 1.0f / subdivs;
            float3 point = EvaluatePosition(0.0f);
            for (int i = 0; i < subdivs; ++i)
            {
                float3 nextPoint = EvaluatePosition((i + 1) * dt);
                float segmentLength = math.distance(point, nextPoint);

                if (segmentLength <= 0.0f)
                {
                    continue;
                }

                if (remainingDistance < segmentLength)
                {
                    return (i + remainingDistance / segmentLength) * dt;
                }

                point = nextPoint;
                remainingDistance -= segmentLength;
            }

            return 1.0f;
        }

        public void ComputeCurveLength()
        {
            curveLength = 0.0f;

            float dt = 1.0f / subdivs;
            float3 point = EvaluatePosition(0.0f);
            for (int i = 0; i < subdivs; ++i)
            {
                float3 nextPoint = EvaluatePosition((i + 1) * dt);
                curveLength += math.distance(point, nextPoint);
                point = nextPoint;
            }
        }

        float3 p0; // start position
        float3 p1; // end position
        float3 m0; // start tangent
        float3 m1; // end tangent

        int     subdivs;
        float   curveLength;
    }
}
