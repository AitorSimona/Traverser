﻿using UnityEngine;

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
            public Vector3[] vertices; // Array of 4 vertices that build the ledge's edges
            public float height;

            // --- Basic Methods ---
            public static TraverserLedgeGeometry Create()
            {
                TraverserLedgeGeometry ledge = new TraverserLedgeGeometry();
                ledge.vertices = new Vector3[4];
                ledge.height = 0.0f;
                return ledge;
            }

            public void Initialize(BoxCollider collider)
            {
                // --- Create ledge geometry from box collider ---
                Transform transform = collider.transform;
                height = collider.bounds.size.y;

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

            Vector3 ClosestPoint(Vector3 position, Vector3 pointP1, Vector3 pointP2)
            {
                Vector3 edge = pointP2 - pointP1;

                // --- Project position-P1 vector on current edge ---
                Vector3 projection = Vector3.Project(position - pointP1, Vector3.Normalize(edge));

                // --- Find intersection point, which is also the closest point to given position ---
                Vector3 closestPoint = pointP1 + projection;

                if (Vector3.Dot(projection, edge) < 0.0f)
                    closestPoint = pointP1;
                else if (Vector3.SqrMagnitude(projection) > Vector3.SqrMagnitude(edge))
                    closestPoint = pointP2;


                return closestPoint;
            }

            public bool ClosestPointDistance(Vector3 position, TraverserLedgeHook hook, ref float distance)
            {
                // --- Returns true if left vertex is the closest, false otherwise ---

                // --- Get current edge's vertices and compute distance to position ---
                Vector3 pointP1 = vertices[hook.index];
                Vector3 pointP2 = vertices[GetNextEdgeIndex(hook.index)];

                Vector3 edge = pointP2 - pointP1;

                // --- Project position-P1 vector on current edge ---
                Vector3 projection = Vector3.Project(position - pointP1, Vector3.Normalize(edge));
                // --- Find intersection point, which is also the closest point to given position ---
                Vector3 closestPoint = pointP1 + projection;

                float distanceP1 = Vector3.SqrMagnitude(pointP1 - closestPoint);
                float distanceP2 = Vector3.SqrMagnitude(pointP2 - closestPoint);

                // --- Return the smallest distance ---
                if (distanceP1 < distanceP2)
                {
                    distance = distanceP1;
                    return false; // right is the closest
                }
                else
                {
                    distance = distanceP2;
                    return true; // left is the closest
                }
            }

            public int GetNextEdgeIndex(int index)
            {
                return (index + 5) % 4;
            }

            public int GetPreviousEdgeIndex(int index)
            {
                return (index + 3) % 4;
            }

            public Vector3 GetNormalizedEdge(int index)
            {
                // --- Get unitary vector from edge ---
                Vector3 pointP1 = vertices[index];
                Vector3 pointP2 = vertices[GetNextEdgeIndex(index)];
                return Vector3.Normalize(pointP2 - pointP1);
            }

            public Vector3 GetNormal(TraverserLedgeHook hook)
            {
                // --- Returns the normal of the given hook's current edge ---
                return -Vector3.Cross(GetNormalizedEdge(hook.index), Vector3.up).normalized;
            }

            public float GetLength(int index)
            {
                // --- Get length of ledge edge given by index ---
                Vector3 a = vertices[index];
                Vector3 b = vertices[GetNextEdgeIndex(index)];

                return Vector3.Magnitude(b - a);
            }

            public Vector3 GetPosition(TraverserLedgeHook hook)
            {
                // --- Get 3D position from given ledge 1D hook point ---
                return vertices[hook.index] + GetNormalizedEdge(hook.index) * hook.distance;
            }

            public Vector3 GetPositionAtDistance(int index, float distance)
            {
                // --- Get 3D position from given ledge 1D hook point ---
                return vertices[index] + GetNormalizedEdge(index) * distance;
            }

            // -------------------------------------------------

            // --- Ledge Hook ---

            public TraverserLedgeHook GetHook(Vector3 position)
            {
                // --- Given a 3d position/ root motion transform, return the closer anchor point ---
                TraverserLedgeHook result;
                result.index = 0;
                result.distance = 0;

                Vector3 closestPoint = ClosestPoint(position, vertices[0], vertices[1]);
                float minimumDistance = Vector3.Magnitude(closestPoint - position);

                result.distance = Vector3.Magnitude(closestPoint - vertices[0]);

                int numEdges = vertices.Length;

                for (int i = 1; i < numEdges; ++i)
                {
                    closestPoint = ClosestPoint(position, vertices[i], vertices[GetNextEdgeIndex(i)]);
                    float distance = Vector3.Magnitude(closestPoint - position);

                    if (distance < minimumDistance)
                    {
                        minimumDistance = distance;
                        result.index = i;
                        result.distance = Vector3.Magnitude(closestPoint - vertices[i]);
                    }
                }

                result.distance = Mathf.Clamp(result.distance, 0.0f, GetLength(result.index));

                return result;
            }

            public TraverserLedgeHook UpdateHook(TraverserLedgeHook hook, Vector3 position, float cornerMinDistance)
            {
                // --- Get current and next edge index ---
                int a = hook.index;
                int b = GetNextEdgeIndex(hook.index);

                // --- Use vector rejection to get the closest point in current edge from given position ---
                Vector3 closestPoint = ClosestPoint(position, vertices[a], vertices[b]);

                TraverserLedgeHook result;

                // --- Compute edge length and distance from edge's pointP1 (start) and closestPoint ---
                float length = Vector3.Magnitude(vertices[b] - vertices[a]);
                float distance = Vector3.Magnitude(closestPoint - vertices[a]);

                // --- If over edge length, move anchor to next edge ---
                if (distance > length - cornerMinDistance)
                {
                    result.distance = distance - length;
                    result.index = GetNextEdgeIndex(hook.index);

                    return result;
                }
                // --- If below edge start, move anchor to previous edge ---
                else if (distance < cornerMinDistance)
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
                    Vector3 v0 = vertices[i];
                    Vector3 v1 = vertices[GetNextEdgeIndex(i)];
                    Vector3 c = (v0 + v1) * 0.5f;

                    if(i == 0)
                        Debug.DrawLine(v0, v1, Color.blue);
                    else
                        Debug.DrawLine(v0, v1, Color.green);

                    Vector3 edge = GetNormalizedEdge(i);
                    Vector3 n = -Vector3.Cross(edge, Vector3.up);
                    Debug.DrawLine(c, c + n * 0.3f, Color.cyan);
                }
            }

            public void DebugDraw(ref TraverserLedgeHook hook)
            {
                // --- Draw hook ---
                Vector3 position = GetPosition(hook);
                Vector3 normal = GetNormal(hook);

                Debug.DrawLine(position, position + normal * 0.3f, Color.red);
            }

            // -------------------------------------------------
        }

        // -------------------------------------------------
    }
}
