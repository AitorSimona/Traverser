using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace Unity.Kinematica
{
    internal static class DebugExtensions
    {
        public static void DrawTransform(float3 position, float scale, Color color)
        {
            float3 x = Missing.right * scale * 0.5f;
            float3 y = Missing.up * scale * 0.5f;
            float3 z = Missing.forward * scale * 0.5f;

            Debug.DrawLine(position - x, position + x, color);
            Debug.DrawLine(position - y, position + y, color);
            Debug.DrawLine(position - z, position + z, color);
        }

        public static void DrawTransform(float3 position, quaternion rotation, float scale)
        {
            Debug.DrawLine(position, position + Missing.xaxis(rotation) * scale, Color.red);
            Debug.DrawLine(position, position + Missing.yaxis(rotation) * scale, Color.green);
            Debug.DrawLine(position, position + Missing.zaxis(rotation) * scale, Color.blue);
        }

        public static void DebugDraw(this BoxCollider collider, Color color)
        {
            Transform transform = collider.transform;

            Vector3 center = collider.center;
            Vector3 size = collider.size;

            Vector3[] vertices = new Vector3[8];

            vertices[0] = transform.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);
            vertices[1] = transform.TransformPoint(center + new Vector3(size.x, size.y, size.z) * 0.5f);
            vertices[2] = transform.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);
            vertices[3] = transform.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);

            vertices[4] = transform.TransformPoint(center + new Vector3(-size.x, -size.y, size.z) * 0.5f);
            vertices[5] = transform.TransformPoint(center + new Vector3(size.x, -size.y, size.z) * 0.5f);
            vertices[6] = transform.TransformPoint(center + new Vector3(size.x, -size.y, -size.z) * 0.5f);
            vertices[7] = transform.TransformPoint(center + new Vector3(-size.x, -size.y, -size.z) * 0.5f);

            Debug.DrawLine(vertices[0], vertices[1], color);
            Debug.DrawLine(vertices[1], vertices[2], color);
            Debug.DrawLine(vertices[2], vertices[3], color);
            Debug.DrawLine(vertices[3], vertices[0], color);

            Debug.DrawLine(vertices[4], vertices[5], color);
            Debug.DrawLine(vertices[5], vertices[6], color);
            Debug.DrawLine(vertices[6], vertices[7], color);
            Debug.DrawLine(vertices[7], vertices[4], color);

            Debug.DrawLine(vertices[0], vertices[4], color);
            Debug.DrawLine(vertices[1], vertices[5], color);
            Debug.DrawLine(vertices[2], vertices[6], color);
            Debug.DrawLine(vertices[3], vertices[7], color);
        }

        public static void DebugDrawCollider(float3 position, quaternion rotation, CapsuleCollider collider, Color color)
        {
            float height = collider.height;
            float radius = collider.radius;

            float3 center = position + Missing.Convert(collider.center);

            float3 bottomSphereCenter = center - Missing.yaxis(rotation) * (height * 0.5f - radius);
            float3 topSphereCenter = center + Missing.yaxis(rotation) * (height * 0.5f - radius);

            DebugDrawCircle(bottomSphereCenter, rotation, color, radius);
            DebugDrawCircle(topSphereCenter, rotation, color, radius);

            float3 right = math.mul(rotation, new float3(radius, 0.0f, 0.0f));
            float3 left = math.mul(rotation, new float3(-radius, 0.0f, 0.0f));
            float3 front = math.mul(rotation, new float3(0.0f, 0.0f, radius));
            float3 back = math.mul(rotation, new float3(0.0f, 0.0f, -radius));

            Debug.DrawLine(bottomSphereCenter + right, topSphereCenter + right, color);
            Debug.DrawLine(bottomSphereCenter + left, topSphereCenter + left, color);
            Debug.DrawLine(bottomSphereCenter + front, topSphereCenter + front, color);
            Debug.DrawLine(bottomSphereCenter + back, topSphereCenter + back, color);

            quaternion qx1 = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), Mathf.PI * 0.5f);
            quaternion qx2 = quaternion.AxisAngle(new float3(-1.0f, 0.0f, 0.0f), Mathf.PI * 0.5f);
            quaternion qz1 = quaternion.AxisAngle(new float3(0.0f, 0.0f, 1.0f), Mathf.PI * 0.5f);
            quaternion qz2 = quaternion.AxisAngle(new float3(0.0f, 0.0f, -1.0f), Mathf.PI * 0.5f);

            quaternion q1 = math.mul(rotation, qx1);
            quaternion q2 = math.mul(rotation, qx2);

            DebugDrawArc(bottomSphereCenter, q1, Mathf.PI, color, radius);
            DebugDrawArc(topSphereCenter, q2, Mathf.PI, color, radius);
            DebugDrawArc(bottomSphereCenter, math.mul(q1, qz1), Mathf.PI, color, radius);
            DebugDrawArc(topSphereCenter, math.mul(q2, qz2), Mathf.PI, color, radius);
        }

        public static void DebugDrawArc(float3 position, quaternion rotation, float arcLength, Color color, float radius = 1.0f)
        {
            float TwoPi = Mathf.PI * 2.0f;
            float angle = 0.0f;

            float3 previousPoint = position +
                math.mul(rotation, new float3(math.cos(angle),
                0.0f, math.sin(angle)) * radius);

            int numSegments = Missing.truncToInt(arcLength * 32 / TwoPi);
            for (int i = 1; i <= numSegments; ++i)
            {
                angle = arcLength / numSegments * i;

                float3 point = position + math.mul(rotation,
                    new float3(math.cos(angle), 0.0f, math.sin(angle)) * radius);

                Debug.DrawLine(previousPoint, point, color);

                previousPoint = point;
            }
        }

        public static void DebugDrawCircle(float3 position, quaternion rotation, Color color, float radius = 1.0f)
        {
            DebugDrawArc(position, rotation, Mathf.PI * 2.0f, color, radius);
        }

        public static void DebugDrawTrajectory(AffineTransform worldRootTransform, NativeSlice<AffineTransform> trajectory, float sampleRate, Color baseColor, Color linesAndContourColor, bool drawArrows = true)
        {
            linesAndContourColor.a = 0.9f;

            int trajectoryLength = trajectory.Length;

            var previousRootTransform = worldRootTransform * trajectory[0];

            for (int i = 1; i < trajectoryLength; ++i)
            {
                var currentRootTransform =
                    worldRootTransform * trajectory[i];

                Unity.Kinematica.DebugDraw.DrawLine(previousRootTransform.t, currentRootTransform.t, baseColor);

                previousRootTransform = currentRootTransform;
            }

            void DrawArrow(AffineTransform transform)
            {
                Unity.Kinematica.DebugDraw.DrawArrow(
                    worldRootTransform * transform,
                    baseColor, linesAndContourColor, 0.1f);
            }

            if (drawArrows)
            {
                int stepSize = Missing.truncToInt(sampleRate / 3);
                int numSteps = trajectoryLength / stepSize;
                for (int i = 0; i < numSteps; ++i)
                {
                    DrawArrow(trajectory[i * stepSize]);
                }

                DrawArrow(trajectory[trajectoryLength - 1]);
            }
        }
    }
}
