using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Mathematics
{
    public static class Missing
    {
        // smallest such that 1.0+epsilon != 1.0
        internal const float epsilon = 1.192092896e-07f;

        internal static int roundToInt(float f)
        {
            return (int)math.floor(f + 0.5f);
        }

        public static float3 rotateVector(quaternion q, float3 v)
        {
            return math.mul(q, v);
        }

        public static float3 project(float3 a, float3 b)
        {
            return math.dot(a, b) * b;
        }

        internal static float sel(float x, float a, float b)
        {
            return x >= 0.0f ? a : b;
        }

        internal static quaternion mul(quaternion q, float f)
        {
            return new quaternion(
                q.value.x * f, q.value.y * f, q.value.z * f, q.value.w * f);
        }

        internal static float3 xaxis(quaternion q)
        {
            float s = 2.0f * q.value.w;
            float x2 = 2.0f * q.value.x;
            return new float3(
                x2 * q.value.x + s * q.value.w - 1.0f,
                x2 * q.value.y + s * q.value.z,
                x2 * q.value.z + s * -q.value.y);
        }

        internal static float3 yaxis(quaternion q)
        {
            float s = 2.0f * q.value.w;
            float y2 = 2.0f * q.value.y;
            return new float3(
                y2 * q.value.x + s * -q.value.z,
                y2 * q.value.y + s * q.value.w - 1.0f,
                y2 * q.value.z + s * q.value.x);
        }

        public static float3 zaxis(quaternion q)
        {
            // This should be fast than math.quaternion.forward().
            // Need to make sure this doesn't get translated to
            // float-by-float operations.
            float s = 2.0f * q.value.w;
            float z2 = 2.0f * q.value.z;
            return new float3(
                q.value.x * z2 + s * q.value.y,
                q.value.y * z2 + s * -q.value.x,
                q.value.z * z2 + s * q.value.w - 1.0f);
        }

        public static float recip(float value)
        {
            return 1.0f / value;
        }

        public static bool equalEps(float a, float b, float epsilon)
        {
            Assert.IsTrue(epsilon >= 0.0f);

            // If the following assert is triggered, it means that epsilon is so small compared to a or b that it is lower than the
            // float precision. To fix this assert either set epsilon to 0.0f or increase epsilon
            Assert.IsTrue(epsilon == 0.0f || epsilon >= math.min(math.abs(a), math.abs(b)) * 1e-8f);

            return math.abs(a - b) <= epsilon;
        }

        public static bool equalEps(float3 lhs, float3 rhs, float epsilon)
        {
            Assert.IsTrue(epsilon >= 0.0f);
            return
                equalEps(lhs.x, rhs.x, epsilon) &&
                equalEps(lhs.y, rhs.y, epsilon) &&
                equalEps(lhs.z, rhs.z, epsilon);
        }

        internal static bool equalEps(quaternion lhs, quaternion rhs, float epsilon)
        {
            return math.abs(dot(lhs, rhs)) > 1.0f - epsilon;
        }

        internal static bool equalEps(AffineTransform lhs, AffineTransform rhs, float epsilon)
        {
            return equalEps(lhs.q, rhs.q, epsilon) && equalEps(lhs.t, rhs.t, epsilon);
        }

        internal static float dot(quaternion lhs, quaternion rhs)
        {
            return math.dot(lhs.value, rhs.value);
        }

        internal static float3 log(quaternion q)
        {
            float3 v = new float3(q.value.x, q.value.y, q.value.z);
            float sinHalfAngle = math.length(v);
            if (sinHalfAngle < epsilon)
            {
                return zero;
            }
            else
            {
                float f = math.atan2(sinHalfAngle, q.value.w) / sinHalfAngle;
                return new float3(f * q.value.x, f * q.value.y, f * q.value.z);
            }
        }

        internal static quaternion negate(quaternion q)
        {
            return new quaternion(-q.value.x, -q.value.y, -q.value.z, -q.value.w);
        }

        public static quaternion conjugate(quaternion q)
        {
            return new quaternion(-q.value.x, -q.value.y, -q.value.z, q.value.w);
        }

        public static float3 axisAngle(quaternion q, out float angle)
        {
            float3 v = new float3(q.value.x, q.value.y, q.value.z);
            float sinHalfAngle = math.length(v);
            if (sinHalfAngle < epsilon)
            {
                angle = 0.0f;
                return new float3(1.0f, 0.0f, 0.0f);
            }
            else
            {
                angle = 2.0f * math.atan2(sinHalfAngle, q.value.w);
                return v * (1.0f / sinHalfAngle);
            }
        }

        internal static float angle(quaternion q)
        {
            return 2.0f * math.acos(math.clamp(q.value.w, -1.0f, 1.0f));
        }

        public static AffineTransform lerp(AffineTransform lhs, AffineTransform rhs, float theta)
        {
            return new AffineTransform(
                math.lerp(lhs.t, rhs.t, theta),
                math.slerp(lhs.q, rhs.q, theta));
        }

        public static float3 mul(float3 a, float3 b)
        {
            return new float3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static float3 recip(float3 v)
        {
            return new float3(1.0f / v.x, 1.0f / v.y, 1.0f / v.z);
        }

        internal static float3 normalize(float3 v, float3 normalizedDefault)
        {
            float length = math.length(v);

            if (length < epsilon)
            {
                return normalizedDefault;
            }
            else
            {
                return v * recip(length);
            }
        }

        internal static float squared(float value)
        {
            return value * value;
        }

        public static float3 zero
        {
            get
            {
                return new float3(0.0f);
            }
        }

        internal static float3 half
        {
            get
            {
                return new float3(0.5f);
            }
        }

        public static float3 right
        {
            get
            {
                return new float3(1.0f, 0.0f, 0.0f);
            }
        }

        public static float3 up
        {
            get
            {
                return new float3(0.0f, 1.0f, 0.0f);
            }
        }

        public static float3 forward
        {
            get
            {
                return new float3(0.0f, 0.0f, 1.0f);
            }
        }

        public static float3 NaN
        {
            get
            {
                return new float3(float.NaN, float.NaN, float.NaN);
            }
        }

        public static bool IsNaN(float3 value)
        {
            return
                float.IsNaN(value.x) ||
                float.IsNaN(value.y) ||
                float.IsNaN(value.y);
        }

        internal static float3 min
        {
            get
            {
                return new float3(float.MinValue, float.MinValue, float.MinValue);
            }
        }

        internal static float3 max
        {
            get
            {
                return new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            }
        }

        internal static float3 minimize(float3 lhs, float3 rhs)
        {
            return new float3(
                lhs.x < rhs.x ? lhs.x : rhs.x,
                lhs.y < rhs.y ? lhs.y : rhs.y,
                lhs.z < rhs.z ? lhs.z : rhs.z);
        }

        internal static float3 maximize(float3 lhs, float3 rhs)
        {
            return new float3(
                lhs.x > rhs.x ? lhs.x : rhs.x,
                lhs.y > rhs.y ? lhs.y : rhs.y,
                lhs.z > rhs.z ? lhs.z : rhs.z);
        }

        internal static float threshold(float lhs, float epsilon, float replacement)
        {
            return math.abs(lhs) <= epsilon ? replacement : lhs;
        }

        internal static float3 threshold(float3 lhs, float epsilon, float replacement)
        {
            return new float3(
                threshold(lhs.x, epsilon, replacement),
                threshold(lhs.y, epsilon, replacement),
                threshold(lhs.z, epsilon, replacement));
        }

        internal static float3 orthogonal(float3 v)
        {
            float3 vn = math.normalize(v);

            if (vn.z < 0.5f && vn.z > -0.5f)
            {
                return math.normalize(new float3(-vn.y, vn.x, 0));
            }
            else
            {
                return math.normalize(new float3(-vn.z, 0, vn.x));
            }
        }

        // Calculates quaternion required to rotate v1 into v2; v1 and v2 need not be normalised
        public static quaternion forRotation(float3 v1, float3 v2)
        {
            float k = math.sqrt(math.lengthsq(v1) * math.lengthsq(v2));

            float d = math.clamp(math.dot(v1, v2), -k, k);

            if (k < epsilon)
            {
                // Test for zero-length input, to avoid infinite loop.  Return identity (not much else to do)
                return quaternion.identity;
            }
            else if (math.abs(d + k) < epsilon * k)
            {
                // Test if v1 and v2 were antiparallel, to avoid singularity
                float3 m = orthogonal(v1);
                quaternion q1 = forRotation(m, v2);
                quaternion q2 = forRotation(v1, m);
                return math.mul(q1, q2);
            }
            else
            {
                // This means that xyz is k.sin(theta),
                // where a is the unit axis of rotation,
                // which equals 2k.sin(theta/2)cos(theta/2).
                float3 v = math.cross(v1, v2);

                // We then put 2kcos^2(theta/2) =
                // dot+k into the w part of the
                // quaternion and normalize.
                return math.normalize(new quaternion(v.x, v.y, v.z, d + k));
            }
        }

        public static int truncToInt(float value)
        {
            return (int)value;
        }

        public static Vector3 Convert(float3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static float3 Convert(Vector3 v)
        {
            return new float3(v.x, v.y, v.z);
        }

        internal static Quaternion Convert(quaternion q)
        {
            return new Quaternion(
                q.value.x, q.value.y,
                q.value.z, q.value.w);
        }

        public static quaternion Convert(Quaternion q)
        {
            return new quaternion(q.x, q.y, q.z, q.w);
        }

        public static AffineTransform Convert(Transform transform)
        {
            return new AffineTransform(transform.position, transform.rotation);
        }

        internal static Vector3 cartesianToSpherical(Vector3 cartesian)
        {
            float x = cartesian.x;
            float y = cartesian.y;
            float z = cartesian.z;

            float radius = Mathf.Sqrt(x * x + y * y + z * z);
            float theta = Mathf.Atan2(y, x);
            float phi = Mathf.Acos(z / radius);

            Assert.IsTrue(theta >= -Mathf.PI && theta <= Mathf.PI);
            Assert.IsTrue(phi >= 0.0f && phi <= Mathf.PI);

            return new Vector3(radius, theta, phi);
        }

        internal static Vector3 sphericalToCartesian(Vector3 spherical)
        {
            float radius = spherical.x;
            float theta = spherical.y;
            float phi = spherical.z;

            float x = Mathf.Cos(theta) * Mathf.Cos(phi) * radius;
            float y = Mathf.Sin(theta) * Mathf.Cos(phi) * radius;
            float z = Mathf.Sin(phi) * radius;

            return new Vector3(x, y, z);
        }

        internal static float mean(float min, float max)
        {
            return (min + max) * 0.5f;
        }

        internal static Vector3 mean(Vector3 min, Vector3 max)
        {
            return new Vector3(
                mean(min.x, max.x),
                mean(min.y, max.y),
                mean(min.z, max.z));
        }

        internal static float standardDeviation(float min, float max)
        {
            Assert.IsTrue(max >= min);
            float r = max - min;
            if (r <= Mathf.Epsilon)
                return 1.0f;
            return r * 0.5f;
        }

        internal static Vector3 standardDeviation(Vector3 min, Vector3 max)
        {
            return new Vector3(
                standardDeviation(min.x, max.x),
                standardDeviation(min.y, max.y),
                standardDeviation(min.z, max.z));
        }

        internal static float safeAngle(float3 lhs, float3 rhs)
        {
            float lengthLhs = math.length(lhs);
            float lengthRhs = math.length(rhs);

            if (lengthLhs < epsilon || lengthRhs < epsilon)
            {
                return 0.0f;
            }
            else
            {
                float3 normalizedLhs = lhs * recip(lengthLhs);
                float3 normalizedRhs = rhs * recip(lengthRhs);

                return math.acos(math.dot(normalizedLhs, normalizedRhs));
            }
        }

        internal static float inverseAbs(float x)
        {
            return 1.0f - math.min(1.0f, math.abs(x));
        }

        // theta is 'reference point', i.e. [-1;theta;+1]
        internal static float inverseAbsHat(float x, float theta)
        {
            Assert.IsTrue(x >= -1.0f && x <= 1.0f);
            Assert.IsTrue(theta >= -1.0f && theta <= 1.0f);

            if (math.abs(x - theta) <= 0.001f)
            {
                return 1.0f;
            }

            if (x < theta)
            {
                float z = (theta - x) / (theta + 1.0f);

                Assert.IsTrue(z >= 0.0f && z <= 1.0f);

                return inverseAbs(z);
            }
            else
            {
                float z = (x - theta) / (1.0f - theta);

                Assert.IsTrue(z >= 0.0f && z <= 1.0f);

                return inverseAbs(z);
            }
        }

        internal static void DisplayInverseAbs(float g)
        {
            var center = new Vector2(0.5f, 0.5f);
            var size = new Vector2(0.1f, 0.1f);

            var values = new float[100];

            for (int i = 0; i < values.Length; ++i)
            {
                float f = i / (float)values.Length;

                f = (f - 0.5f) * 2.0f;

                f = Missing.inverseAbsHat(f, g);

                values[i] = f;
            }

            var backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.25f);

            //DebugDraw.Begin();
            //DebugDraw.DrawGUIFunction(center, size, values, 0.0f, 1.0f, backgroundColor, Color.white);
            //DebugDraw.End();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct AffineTransform
    {
        public float3 t;
        public quaternion q;

        public AffineTransform(float3 t_, quaternion q_)
        {
            t = t_;
            q = q_;
        }

        public static AffineTransform Create(float3 t, quaternion q)
        {
            return new AffineTransform(t, q);
        }

        public static AffineTransform CreateGlobal(Transform transform)
        {
            return Create(transform.position, transform.rotation);
        }

        public static AffineTransform identity
        {
            get
            {
                return new AffineTransform(
                    new float3(0.0f, 0.0f, 0.0f),
                    quaternion.identity);
            }
        }

        public float3 transform(float3 p)
        {
            return Missing.rotateVector(q, p) + t;
        }

        public float3 transformDirection(float3 d)
        {
            return Missing.rotateVector(q, d);
        }

        public float3 inverseTransform(float3 p)
        {
            return inverse().transform(p);
        }

        public static AffineTransform operator*(AffineTransform lhs, AffineTransform rhs)
        {
            return new AffineTransform(
                lhs.transform(rhs.t), math.mul(lhs.q, rhs.q));
        }

        public static AffineTransform operator*(AffineTransform lhs, float scale)
        {
            return new AffineTransform(lhs.t * scale, lhs.q);
        }

        // Return the inverse of this transform
        // v'=R*v+T
        // v'-T=R*v
        // inverse(R)*(v' - T) = inverse(R)*R*v = v
        // v = -inverse(R)*T +inverse(R)*(v')
        public AffineTransform inverse()
        {
            quaternion inverseQ = Missing.conjugate(q);
            return new AffineTransform(
                Missing.rotateVector(inverseQ, -t), inverseQ);
        }

        // returns this.Inverse() * rhs
        public AffineTransform inverseTimes(AffineTransform rhs)
        {
            quaternion inverseQ = Missing.conjugate(q);
            return new AffineTransform(
                Missing.rotateVector(inverseQ, rhs.t - t),
                math.mul(inverseQ, rhs.q));
        }

        public AffineTransform alignHorizontally()
        {
            quaternion alignRotation = Missing.forRotation(transformDirection(Missing.up), Missing.up);

            return new AffineTransform(
                t, math.mul(alignRotation, q));
        }

        public float3 Forward => transformDirection(new float3(0.0f, 0.0f, 1.0f));
    }
}
