using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    public class TraverserLedgeObject
    {
        // --- Definition of a ledge's hook, the point we are attached to --- 
        public struct TraverserLedgeHook
        {
            // --- Attributes that define a hook point ---
            public int index; // of the 4 lines/edges that compose a ledge, which one is the hook on
            public float distance; // where in the given ledge line is the hook

            // representation of a ledge (top view)
            // - - - - -
            // -       -
            // -       - 
            // - - - p - p is at 0.75*distance and at the 2nd line (index)

            // -------------------------------------------------

            // --- Construction ---
            public static TraverserLedgeHook Create()
            {
                TraverserLedgeHook hook = new TraverserLedgeHook();
                hook.index = 0;
                hook.distance = 0;
                return hook;
            }

            // -------------------------------------------------
        }

        // -------------------------------------------------

        // --- Definition of ledge geometry, so we can adapt motion to its bounds ---
        public struct TraverserLedgeGeometry
        {
            // --- Attributes ---
            public float3[] vertices; // Array of 4 vertices that build the ledge's edges

            // --- Basic Methods ---
            public static TraverserLedgeGeometry Create()
            {
                TraverserLedgeGeometry ledge = new TraverserLedgeGeometry();
                ledge.vertices = new float3[4]; 
                return ledge;
            }

            public void Initialize(BoxCollider collider)
            {
                // --- Create ledge geometry from box collider ---
                Transform transform = collider.transform;

                Vector3 center = collider.center;
                Vector3 size = collider.size;
              
                vertices[0] = transform.TransformPoint(center + GetVector3(-size.x, size.y, size.z) * 0.5f);
                vertices[1] = transform.TransformPoint(center + GetVector3(size.x, size.y, size.z) * 0.5f);
                vertices[2] = transform.TransformPoint(center + GetVector3(size.x, size.y, -size.z) * 0.5f);
                vertices[3] = transform.TransformPoint(center + GetVector3(-size.x, size.y, -size.z) * 0.5f);
            }

            // -------------------------------------------------

            // --- Utilities ---
            Vector3 GetVector3(float x, float y, float z)
            {
                Vector3 tmp;
                tmp.x = x;
                tmp.y = y;
                tmp.z = z;
                return tmp;
            }

            float3 ClosestPoint(float3 position, float3 pointP1, float3 pointP2)
            {
                float3 edge = pointP2 - pointP1;

                // --- Project position-P1 vector on current edge ---
                float3 projection = math.project(position - pointP1, math.normalizesafe(edge));

                // --- Find intersection point, which is also the closest point to given position ---
                float3 closestPoint = pointP1 + projection;

                if (math.dot(projection, edge) < 0.0f)
                    closestPoint = pointP1;
                else if (math.lengthsq(projection) > math.lengthsq(edge))
                    closestPoint = pointP2;


                return closestPoint;
            }

            public float ClosestPointDistance(float3 position, TraverserLedgeHook hook)
            {
                // --- Get current edge's vertices and compute distance to position ---
                float3 pointP1 = vertices[hook.index];
                float3 pointP2 = vertices[GetNextEdgeIndex(hook.index)];
                float distanceP1 = math.length(pointP1 - position);
                float distanceP2 = math.length(pointP2 - position);

                // --- Return the smallest distance ---
                if (distanceP1 < distanceP2)
                    return distanceP1;
                else
                    return distanceP2;
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
                // --- Get unitary vector from edge ---
                float3 pointP1 = vertices[index];
                float3 pointP2 = vertices[GetNextEdgeIndex(index)];
                return math.normalize(pointP2 - pointP1);
            }

            public float3 GetNormal(TraverserLedgeHook hook)
            {
                // --- Returns the normal of the given hook's current edge ---
                return -math.cross(GetNormalizedEdge(hook.index), Vector3.up);
            }

            public float GetLength(int index)
            {
                // --- Get length of ledge edge given by index ---
                float3 a = vertices[index];
                float3 b = vertices[GetNextEdgeIndex(index)];

                return math.length(b - a);
            }

            public float3 GetPosition(TraverserLedgeHook hook)
            {
                // --- Get 3D position from given ledge 1D hook point ---
                return vertices[hook.index] + GetNormalizedEdge(hook.index) * hook.distance;
            }

            //public float GetDistanceToClosestVertex(float3 position, float3 forward, ref bool left)
            //{
            //    float distance = vertices[0].x - position.x;
            //    float minimumDistance = 1.0f;

            //    float3 abs_forward = 0.0f;
            //    abs_forward.x = Mathf.Abs(forward.x);
            //    abs_forward.z = Mathf.Abs(forward.z);

            //    // --- We use the forward/normal given to determine under which direction we should compute distance --- 

            //    if (abs_forward.Equals(Vector3.forward))
            //    {
            //        minimumDistance = math.abs(math.length(vertices[0].x - position.x));
            //        distance = vertices[0].x - position.x;

            //        if (minimumDistance > math.abs(math.length(vertices[1].x - position.x)))
            //        {
            //            minimumDistance = math.abs(math.length(vertices[1].x - position.x));
            //            distance = vertices[1].x - position.x;
            //        }

            //        if (minimumDistance > math.abs(math.length(vertices[2].x - position.x)))
            //        {
            //            minimumDistance = math.abs(math.length(vertices[2].x - position.x));
            //            distance = vertices[2].x - position.x;
            //        }

            //        if (minimumDistance > math.abs(math.length(vertices[3].x - position.x)))
            //        {
            //            minimumDistance = math.abs(math.length(vertices[3].x - position.x));
            //            distance = vertices[3].x - position.x;
            //        }

            //        // --- We are facing z, our left is the negative X ---
            //        if (forward.z > 0.0f)
            //            left = distance > 0.0f ? false : true;
            //        // --- We are facing -z, our left is the positive X ---
            //        else
            //            left = distance > 0.0f ? true : false;

            //    }
            //    else if(abs_forward.Equals(Vector3.right))
            //    {
            //        minimumDistance = math.abs(math.length(vertices[0].z - position.z));
            //        distance = vertices[0].z - position.z;

            //        if (minimumDistance > math.abs(math.length(vertices[1].z - position.z)))
            //        {
            //            minimumDistance = math.abs(math.length(vertices[1].z - position.z));
            //            distance = vertices[1].z - position.z;
            //        }

            //        if (minimumDistance > math.abs(math.length(vertices[2].z - position.z)))
            //        {
            //            minimumDistance = math.abs(math.length(vertices[2].z - position.z));
            //            distance = vertices[2].z - position.z;
            //        }

            //        if (minimumDistance > math.abs(math.length(vertices[3].z - position.z)))
            //        {
            //            minimumDistance = math.abs(math.length(vertices[3].z - position.z));
            //            distance = vertices[3].z - position.z;
            //        }

            //        // --- We are facing X, our left is the negative X ---
            //        if (forward.x > 0.0f)
            //            left = distance > 0.0f ? true : false;
            //        // --- We are facing -z, our left is the positive X ---
            //        else
            //            left = distance > 0.0f ? false : true;

            //    }

            //    return minimumDistance;
            //}

            public TraverserAffineTransform GetTransform(TraverserLedgeHook anchor)
            {
                // --- Get transform out of a 2D ledge anchor --- 
                float3 p = GetPosition(anchor);
                float3 edge = GetNormalizedEdge(anchor.index);
                float3 up = Vector3.up;
                float3 n = GetNormal(anchor);

                return new TraverserAffineTransform(p, math.quaternion(math.float3x3(edge, up, n)));
            }

            public TraverserAffineTransform GetTransformGivenNormal(TraverserLedgeHook anchor, float3 normal)
            {
                // --- Get transform out of a 2D ledge anchor --- 
                float3 p = GetPosition(anchor);
                float3 edge = GetNormalizedEdge(anchor.index);
                float3 up = Vector3.up;

                return new TraverserAffineTransform(p, math.quaternion(math.float3x3(edge, up, normal)));
            }
            // -------------------------------------------------

            // --- Ledge Hook ---

            public TraverserLedgeHook GetHook(float3 position)
            {
                // --- Given a 3d position/ root motion transform, return the closer anchor point ---
                TraverserLedgeHook result;
                result.index = 0;
                result.distance = 0;

                float3 closestPoint = ClosestPoint(position, vertices[0], vertices[1]);
                float minimumDistance = math.length(closestPoint - position);

                result.distance = math.length(closestPoint - vertices[0]);

                int numEdges = vertices.Length;

                for (int i = 1; i < numEdges; ++i)
                {
                    closestPoint = ClosestPoint(position, vertices[i], vertices[GetNextEdgeIndex(i)]);
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

            public TraverserLedgeHook UpdateHook(TraverserLedgeHook hook, float3 position)
            {
                // --- Get current and next edge index ---
                int a = hook.index;
                int b = GetNextEdgeIndex(hook.index);

                // --- Use vector rejection to get the closest point in current edge from given position ---
                float3 closestPoint = ClosestPoint(position, vertices[a], vertices[b]);

                TraverserLedgeHook result;

                // --- Compute edge length and distance from edge's pointP1 (start) and closestPoint ---
                float length = math.length(vertices[b] - vertices[a]);
                float distance = math.length(closestPoint - vertices[a]);

                // --- If over edge length, move anchor to next edge ---
                if (distance > length)
                {
                    result.distance = distance - length;
                    result.index = GetNextEdgeIndex(hook.index);

                    return result;
                }
                // --- If below edge start, move anchor to previous edge ---
                else if (distance < 0.0f)
                {
                    result.index = GetPreviousEdgeIndex(hook.index);
                    result.distance = GetLength(result.index) + distance;

                    return result;
                }

                // --- Update properties ---
                result.distance = distance;
                result.index = hook.index;

                return result;
            }

            // -------------------------------------------------

            // --- Debug ---
            public void DebugDraw()
            {
                // --- Iterate vertices and draw ledge edges ---
                for (int i = 0; i < 4; ++i)
                {
                    float3 v0 = vertices[i];
                    float3 v1 = vertices[GetNextEdgeIndex(i)];
                    float3 c = (v0 + v1) * 0.5f;

                    if(i == 0)
                        Debug.DrawLine(v0, v1, Color.blue);
                    else
                        Debug.DrawLine(v0, v1, Color.green);

                    float3 edge = GetNormalizedEdge(i);
                    float3 n = -math.cross(edge, Vector3.up);
                    Debug.DrawLine(c, c + n * 0.3f, Color.cyan);
                }
            }

            public void DebugDraw(ref TraverserLedgeHook state)
            {
                // --- Draw hook ---
                float3 position = GetPosition(state);
                float3 normal = GetNormal(state);

                Debug.DrawLine(position, position + normal * 0.3f, Color.red);
            }

            // -------------------------------------------------
        }

        // -------------------------------------------------
    }
}
