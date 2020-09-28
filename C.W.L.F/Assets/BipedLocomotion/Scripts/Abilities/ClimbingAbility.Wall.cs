using Unity.SnapshotDebugger;
using Unity.Mathematics;
using Unity.Kinematica;

using UnityEngine;

public partial class ClimbingAbility : SnapshotProvider, Ability
{
    // --- Definition of a wall's anchor, the point we are attached to (relative to the wall plane, note it is a 2D point) --- 
    public struct WallAnchor
    {
        // --- Attributes that define an anchor point ---
        public float x; // note these two values form a 2D position, expressed from 0 to 1, from top left vertex
        public float y;

        // representation of a wall (plane), point p is built by x and y
        // v - - - -
        // -       -
        // -   p   - x is 0.5, y 0.75, as seen from v (multiply by width and height and you get the 2D point)
        // - - - - -

        // -------------------------------------------------

        // --- Construction ---
        public static WallAnchor Create()
        {
            return new WallAnchor();
        }
  
        // -------------------------------------------------
    }

    // -------------------------------------------------

    // --- Definition of wall geometry, so we can adapt motion when colliding into it ---
    public struct WallGeometry
    {
        // --- Attributes that define a wall object ---
        public AffineTransform transform; // Ensure we remain on the same space
        public float3 scale; // global

        public float3 normal;
        public float3 center;
        public float3 size; // local

        // -------------------------------------------------

        // --- Basic Methods ---
        public static WallGeometry Create()
        {
            return new WallGeometry();
        }

        public void Initialize(BoxCollider collider, AffineTransform contactTransform)
        {
            // --- Create wall geometry from box collider ---
            transform = Convert(collider.transform);
            scale = collider.transform.lossyScale;
            center = collider.center;
            size = collider.size;

            Initialize(contactTransform);
        }

        public void Initialize(AffineTransform contactTransform)
        {
            // World space contact position to local canonical cube position
            float3 localPosition = WorldToLocal(contactTransform.t);

            // --- Find out the wall's closest normal ---
            normal = GetNormal(0);

            // --- Plane to point distance (plane built with normal and up), point is the contact transform ---
            float minimumDistance = math.abs(PlaneDistance(normal, Missing.up, localPosition));
            
            // The idea is to compute all minimum distances, between the planes and the different normals 
            // to know exactly in what direction respect the wall the contact has been made

            for (int i = 1; i < 4; ++i)
            {
                float3 n = GetNormal(i);

                float distance = math.abs(PlaneDistance(n, Missing.up, localPosition));

                if (distance < minimumDistance)
                {
                    normal = n;
                    minimumDistance = distance;
                }
            }
        }

        // -------------------------------------------------

        // --- Utilities ---
        public float3 transformPoint(float3 point)
        {
            // transforms point p from local to world
            return transform.transform(point * scale);
        }

        public float3 inverseTransformPoint(float3 point)
        {
            // transforms point p from world to local
            return transform.inverseTransform(point) / scale;
        }

        public float3 transformDirection(float3 direction)
        {
            // transforms direction from local to world
            return transform.transformDirection(direction);
        }

        public static AffineTransform Convert(Transform transform)
        {
            // Ensure we remain on the same space
            return new AffineTransform(transform.position, transform.rotation);
        }

        public static float PlaneDistance(float3 normal, float3 up, float3 position)
        {
            // --- Compute the distance between the given normal and the contact transform ---
            float3 orthogonal = math.cross(normal, up);

            // --- Point plane distance ---

            // u*V1 + v*V2, where u and v = 1.0f
            float3 vertex = normal + up + orthogonal; // direction pointing towards where a vertex is

            float d = -math.dot(normal, vertex);

            return math.dot(normal, position) + d;
        }

        public float3 WorldToLocal(float3 p)
        {
            return Missing.mul(inverseTransformPoint(p) - center, Missing.recip(size)) * 2.0f;
        }

        public static float3 GetNormal(int index)
        {
            // simple getter of 4 normal directions
            float3[] normals = new float3[]
            {
                 Missing.right,
                 -Missing.right,
                 Missing.forward,
                 -Missing.forward
            };

            return normals[index];
        }

        public float3 GetNormalLocalSpace()
        {
            return normal;
        }

        public float3 GetNormalWorldSpace()
        {
            // local to world
            return transformDirection(GetNormalLocalSpace());
        }

        public float3 GetOrthogonalLocalSpace()
        {
            // return the perpendicular vector to the normal/up
            return math.cross(normal, Missing.up);
        }

        public float3 GetOrthogonalWorldSpace()
        {
            // local to world
            return transformDirection(GetOrthogonalLocalSpace());
        }

        public float3 GetBaseVertex()
        {
            // u*V1 + v*V2, where u and v = 1.0f
            return normal + Missing.up + GetOrthogonalLocalSpace();
        }

        public static float3 GetNextVertexCCW(float3 normal, float3 vertex)
        {
            // https://math.stackexchange.com/questions/304700
            return normal + math.cross(normal, vertex);
        }

        public float GetWidth()
        {
            // get world width
            float3 orthogonal = GetOrthogonalLocalSpace();

            return math.dot(orthogonal, size) * math.dot(orthogonal, scale);
        }

        public float GetHeight()
        {
            // get world height
            return size.y * scale.y;
        }

        public float GetHeight(ref WallAnchor anchor)
        {
            return GetHeight() * (1.0f - anchor.y);
        }

        public float3 GetPosition(WallAnchor anchor)
        {
            // --- Find the 3d position of our 2D anchor point ---

            float3 orthogonal = GetOrthogonalLocalSpace();
            float3 vertex = GetBaseVertex();

            float3 vertex0World = transformPoint(center + Missing.mul(size, vertex) * 0.5f);
            float3 orthogonalWorld = transformDirection(orthogonal);

            float xLocal = GetWidth() * anchor.x;
            float yLocal = GetHeight() * anchor.y;

            return vertex0World - (orthogonalWorld * xLocal) - (Missing.up * yLocal);
        }

        // -------------------------------------------------

        // --- Anchor ---
        public WallAnchor GetAnchor(float3 position)
        {
            // --- Given a 3d position/ root motion transform, return the closer anchor point ---
            float3 localPosition = WorldToLocal(position);

            float distance = math.abs(PlaneDistance(normal, Missing.up, localPosition));

            localPosition -= normal * distance;

            WallAnchor result;

            result.x = 1.0f - math.saturate(((math.dot(GetOrthogonalLocalSpace(), localPosition) + 1.0f) * 0.5f));
            result.y = 1.0f - math.saturate((localPosition.y + 1.0f) * 0.5f);

            return result;
        }

        // MYTODO: Check if this function is used in newer version, else eliminate it
        public WallAnchor UpdateAnchor(WallAnchor anchor, float2 xy)
        {
            WallAnchor result;

            result.x = math.saturate(anchor.x - xy.x / GetWidth()); // saturate = clamp (0.0f-1.0f)
            result.y = math.saturate(anchor.y - xy.y / GetHeight());

            return result;
        }

        // -------------------------------------------------

        // --- Debug ---
        public void DebugDraw()
        {
            float3 vertex = GetBaseVertex();

            for (int i = 0; i < 4; ++i)
            {
                float3 nextVertex = GetNextVertexCCW(normal, vertex);

                float3 v0 = transformPoint(center + Missing.mul(size, vertex) * 0.5f);
                float3 v1 = transformPoint(center + Missing.mul(size, nextVertex) * 0.5f);

                Debug.DrawLine(v0, v1, Color.green);

                vertex = nextVertex;
            }
        }

        public void DebugDraw(ref WallAnchor state)
        {
            float3 position = GetPosition(state);
            float3 normal = GetNormalWorldSpace();

            Debug.DrawLine(position, position + normal * 0.3f, Color.red);
        }

        // -------------------------------------------------
    }

    // -------------------------------------------------
}

