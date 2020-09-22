using System;

using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;

using UnityEngine;

using Buffer = Unity.SnapshotDebugger.Buffer;

namespace CWLF
{

    // --- Definition of ledge geometry, so we can adapt motion when colliding into it ---

    public partial class ClimbingAbility : SnapshotProvider, Ability
    {

        // --- Definition of a ledge's anchor, the point we are attached to --- 
        public struct LedgeAnchor
        {
            // --- Attributes that define an anchor point ---
            public int index;
            public float distance;

            // --- Construction ---
            public static LedgeAnchor Create()
            {
                return new LedgeAnchor();
            }

            // --- IO ---
            public void WriteToStream(Buffer buffer)
            {
                buffer.Write(index);
                buffer.Write(distance);
            }

            public void ReadFromStream(Buffer buffer)
            {
                index = buffer.Read32();
                distance = buffer.ReadSingle();
            }
        }

        public struct LedgeGeometry : IDisposable, Serializable
        {
            // --- Attribute that defines a ledge object ---
            public NativeArray<float3> vertices;

            // --- Construction / Destruction ---
            public static LedgeGeometry Create()
            {
                var result = new LedgeGeometry();
                result.vertices = new NativeArray<float3>(4, Allocator.Persistent); // custom alloc vertices
                return result;
            }

            public void Dispose()
            {
                vertices.Dispose(); // free memory
            }

            // --- Create ledge geometry from box collider ---
            public void Initialize(BoxCollider collider)
            {
                Transform transform = collider.transform;

                Vector3 center = collider.center;
                Vector3 size = collider.size;

                vertices[0] = transform.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);
                vertices[1] = transform.TransformPoint(center + new Vector3(size.x, size.y, size.z) * 0.5f);
                vertices[2] = transform.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);
                vertices[3] = transform.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);
            }

            // --- IO ---
            public void WriteToStream(Buffer buffer)
            {
                buffer.WriteNativeArray(vertices, Allocator.Persistent);
            }

            public void ReadFromStream(Buffer buffer)
            {
                vertices = buffer.ReadNativeArray<float3>(out _);
            }

            // --- Getters ---
            public int GetNextEdgeIndex(int index)
            {
                return (index + 5) % 4;
            }

            public int GetPreviousEdgeIndex(int index)
            {
                return (index + 3) % 4;
            }

            public float3 GetNormalizedEdge(int index)
            {
                float3 a = vertices[index];
                float3 b = vertices[GetNextEdgeIndex(index)];
                return math.normalize(b - a);
            }

            public float3 GetNormal(LedgeAnchor anchor)
            {
                return -math.cross(GetNormalizedEdge(anchor.index), Missing.up);
            }

            public float GetLength(int index)
            {
                float3 a = vertices[index];
                float3 b = vertices[GetNextEdgeIndex(index)];

                return math.length(b - a);
            }

            public float3 GetPosition(LedgeAnchor anchor)
            {
                return vertices[anchor.index] + GetNormalizedEdge(anchor.index) * anchor.distance;
            }

            public AffineTransform GetTransform(LedgeAnchor anchor)
            {
                float3 p = GetPosition(anchor);
                float3 edge = GetNormalizedEdge(anchor.index);
                float3 up = Missing.up;
                float3 n = GetNormal(anchor);

                return new AffineTransform(p, math.quaternion(math.float3x3(edge, up, n)));
            }

            // --- Anchor ---
            public LedgeAnchor GetAnchor(float3 position)
            {
                LedgeAnchor result = new LedgeAnchor();

                float3 closestPoint = ClosestPoint.FromPosition(position,vertices[0], vertices[1]);
                float minimumDistance = math.length(closestPoint - position);

                result.distance = math.length(closestPoint - vertices[0]);

                int numEdges = vertices.Length;

                for (int i = 1; i < numEdges; ++i)
                {
                    closestPoint = ClosestPoint.FromPosition(position, vertices[i], vertices[GetNextEdgeIndex(i)]);
                    float distance = math.length(closestPoint - position);

                    if (distance < minimumDistance)
                    {
                        minimumDistance = distance;
                        result.index = i;
                        result.distance = math.length(closestPoint - vertices[i]);
                    }
                }

                result.distance = math.clamp(result.distance, 0.0f, GetLength(result.index));

                return result;
            }

            public LedgeAnchor UpdateAnchor(LedgeAnchor anchor, float3 position)
            {
                int a = anchor.index;
                int b = GetNextEdgeIndex(anchor.index);

                float3 closestPoint = ClosestPoint.FromPosition(position, vertices[a], vertices[b]);

                LedgeAnchor result;

                float length = math.length(vertices[b] - vertices[a]);
                float distance = math.length(closestPoint - vertices[a]);

                if (distance > length)
                {
                    result.distance = distance - length;
                    result.index = GetNextEdgeIndex(anchor.index);

                    return result;
                }
                else if (distance < 0.0f)
                {
                    result.index = GetPreviousEdgeIndex(anchor.index);
                    result.distance = GetLength(result.index) + distance;

                    return result;
                }

                result.distance = distance;
                result.index = anchor.index;

                return result;
            }

            public LedgeAnchor UpdateAnchor(LedgeAnchor anchor, float displacement)
            {
                LedgeAnchor result;

                int a = anchor.index;
                int b = GetNextEdgeIndex(anchor.index);

                float length = math.length(vertices[b] - vertices[a]);
                float distance = anchor.distance + displacement;

                if (distance > length)
                {
                    result.distance = distance - length;
                    result.index = GetNextEdgeIndex(anchor.index);

                    return result;
                }
                else if (distance < 0.0f)
                {
                    result.index = GetPreviousEdgeIndex(anchor.index);
                    result.distance = GetLength(result.index) + distance;

                    return result;
                }

                result.distance = distance;
                result.index = anchor.index;

                return result;
            }

            // --- Debug ---
            public void DebugDraw()
            {
                for (int i = 0; i < 4; ++i)
                {
                    float3 v0 = vertices[i];
                    float3 v1 = vertices[GetNextEdgeIndex(i)];
                    float3 c = (v0 + v1) * 0.5f;
                    Debug.DrawLine(v0, v1, Color.green);
                    float3 edge = GetNormalizedEdge(i);
                    float3 n = -math.cross(edge, Missing.up);
                    Debug.DrawLine(c, c + n * 0.3f, Color.cyan);
                }
            }

            public void DebugDraw(ref LedgeAnchor state)
            {
                float3 position = GetPosition(state);
                float3 normal = GetNormal(state);

                Debug.DrawLine(position, position + normal * 0.3f, Color.red);
            }
        }
    }
}