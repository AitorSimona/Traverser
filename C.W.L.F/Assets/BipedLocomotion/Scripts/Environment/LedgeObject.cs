using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;

using Buffer = Unity.SnapshotDebugger.Buffer;

namespace CWLF
{
    public class LedgeObject : SnapshotProvider
    {
        // --- Definition of a ledge's anchor, the point we are attached to --- 
        public struct LedgeAnchor
        {
            // --- Attributes that define an anchor point ---
            public int index; // of the 4 lines/edges that compose a ledge, which one is the anchor on
            public float distance; // where in the given ledge line is the anchor

            // representation of a ledge (top view)
            // - - - - -
            // -       -
            // -       - 
            // - - - p - p is at 0.75*distance and at the 2nd line (index)

            // -------------------------------------------------

            // --- Construction ---
            public static LedgeAnchor Create()
            {
                return new LedgeAnchor();
            }

            // -------------------------------------------------
        }

        // -------------------------------------------------

        // --- Definition of ledge geometry, so we can adapt motion when colliding into it ---
        public struct LedgeGeometry : IDisposable, Serializable
        {
            // --- Attributes ---
            public NativeArray<float3> vertices; // Attribute that defines a ledge object

            // --- Basic Methods ---
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

            public void Initialize(BoxCollider collider)
            {
                // --- Create ledge geometry from box collider ---
                Transform transform = collider.transform;

                Vector3 center = collider.center;
                Vector3 size = collider.size;

                vertices[0] = transform.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);
                vertices[1] = transform.TransformPoint(center + new Vector3(size.x, size.y, size.z) * 0.5f);
                vertices[2] = transform.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);
                vertices[3] = transform.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);
            }

            // -------------------------------------------------

            // --- IO ---
            public void WriteToStream(Buffer buffer)
            {
                buffer.WriteNativeArray(vertices, Allocator.Persistent);
            }

            public void ReadFromStream(Buffer buffer)
            {
                vertices = buffer.ReadNativeArray<float3>(out _);
            }

            // -------------------------------------------------

            // --- Utilities ---

            public void LimitTransform(ref Vector3 position, float offset)
            {
                if (position.x < vertices[0].x + offset)
                    position.x = vertices[0].x + offset;
                if (position.z > vertices[0].z - offset)
                    position.z = vertices[0].z - offset;

                if (position.x > vertices[2].x - offset)
                    position.x = vertices[2].x - offset;
                if (position.z < vertices[2].z + offset)
                    position.z = vertices[2].z + offset;

                //position.y = vertices[0].y;
            }

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
                // --- Get unitary vector in the direction of edge given by line ---
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
                // --- Get length of ledge edge given by index ---
                float3 a = vertices[index];
                float3 b = vertices[GetNextEdgeIndex(index)];

                return math.length(b - a);
            }


            public float3 GetPosition(LedgeAnchor anchor)
            {
                // --- Get 3D position from given ledge 2D anchor point ---
                return vertices[anchor.index] + GetNormalizedEdge(anchor.index) * anchor.distance;
            }

            public float GetDistanceToClosestVertex(float3 position, float3 forward)
            {
                float minimumDistance = math.abs(math.length(vertices[0].x - position.x));

                forward.x = Mathf.Abs(forward.x);
                forward.z = Mathf.Abs(forward.z);

                // --- We use the forward/normal given to determine under which direction we should compute distance --- 

                if (forward.Equals(Missing.forward))
                {
                    if (minimumDistance > math.abs(math.length(vertices[1].x - position.x)))
                        minimumDistance = math.abs(math.length(vertices[1].x - position.x));

                    if (minimumDistance > math.abs(math.length(vertices[2].x - position.x)))
                        minimumDistance = math.abs(math.length(vertices[2].x - position.x));

                    if (minimumDistance > math.abs(math.length(vertices[3].x - position.x)))
                        minimumDistance = math.abs(math.length(vertices[3].x - position.x));
                }
                else if(forward.Equals(Missing.right))
                {
                    minimumDistance = math.abs(math.length(vertices[0].z - position.z));

                    if (minimumDistance > math.abs(math.length(vertices[1].z - position.z)))
                        minimumDistance = math.abs(math.length(vertices[1].z - position.z));
                                                                           
                    if (minimumDistance > math.abs(math.length(vertices[2].z - position.z)))
                        minimumDistance = math.abs(math.length(vertices[2].z - position.z));
                                                                           
                    if (minimumDistance > math.abs(math.length(vertices[3].z - position.z)))
                        minimumDistance = math.abs(math.length(vertices[3].z - position.z));
                }

                return minimumDistance;
            }

            public AffineTransform GetTransform(LedgeAnchor anchor)
            {
                // --- Get transform out of a 2D ledge anchor --- 
                float3 p = GetPosition(anchor);
                float3 edge = GetNormalizedEdge(anchor.index);
                float3 up = Missing.up;
                float3 n = GetNormal(anchor);

                return new AffineTransform(p, math.quaternion(math.float3x3(edge, up, n)));
            }

            public AffineTransform GetTransformGivenNormal(LedgeAnchor anchor, float3 normal)
            {
                // --- Get transform out of a 2D ledge anchor --- 
                float3 p = GetPosition(anchor);
                float3 edge = GetNormalizedEdge(anchor.index);
                float3 up = Missing.up;
                //float3 n = GetNormal(anchor);

                return new AffineTransform(p, math.quaternion(math.float3x3(edge, up, normal)));
            }
            // -------------------------------------------------

            // --- Anchor ---
            public LedgeAnchor GetAnchor(float3 position)
            {
                // --- Given a 3d position/ root motion transform, return the closer anchor point ---
                LedgeAnchor result = new LedgeAnchor();

                float3 closestPoint = ClosestPoint.FromPosition(position, vertices[0], vertices[1]);
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
                // --- Displace ledge anchor point by given displacement ---
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

            // -------------------------------------------------

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

            // -------------------------------------------------
        }

        // -------------------------------------------------
    }
}
