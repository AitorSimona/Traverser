using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;

public struct BoundingBox
{
    public AffineTransform transform;
    public float3 size;

    public float3 transformPoint(float3 position)
    {
        return transform.transform(position);
    }

    public float3 inverseTransformPoint(float3 position)
    {
        return transform.inverseTransform(position);
    }

    public bool Contains(float3 position, float radius)
    {
        float3 p = transform.inverseTransform(position);
        float3 halfSize = (size * 0.5f) + new float3(radius);

        if (p.x < -halfSize.x) return false;
        if (p.x > halfSize.x) return false;
        if (p.y < -halfSize.y) return false;
        if (p.y > halfSize.y) return false;
        if (p.z < -halfSize.z) return false;
        if (p.z > halfSize.z) return false;

        return true;
    }

    public static BoundingBox FromBoxCollider(BoxCollider collider)
    {
        AffineTransform baseTransform = Missing.Convert(collider.transform);

        float3 transformScale = collider.transform.lossyScale;
        float3 colliderScale = collider.size;

        float3 center = baseTransform.transform(
            collider.center * transformScale);

        return new BoundingBox
        {
            transform = new AffineTransform(center, baseTransform.q),
            size = transformScale * colliderScale
        };
    }

    public static bool SphereIntersect(float3 position, float radius, NativeArray<BoundingBox> obbs)
    {
        for (int i = 0; i < obbs.Length; ++i)
        {
            if (obbs[i].Contains(position, radius))
            {
                return true;
            }
        }

        return false;
    }

    //public unsafe void DebugDraw(Color color)
    //{
    //    float3* vertices = stackalloc float3[8];

    //    float3 extents = size * 0.5f;

    //    vertices[0] = transformPoint(new float3(-extents.x, extents.y, extents.z));
    //    vertices[1] = transformPoint(new float3(extents.x, extents.y, extents.z));
    //    vertices[2] = transformPoint(new float3(extents.x, extents.y, -extents.z));
    //    vertices[3] = transformPoint(new float3(-extents.x, extents.y, -extents.z));

    //    vertices[4] = transformPoint(new float3(-extents.x, -extents.y, extents.z));
    //    vertices[5] = transformPoint(new float3(extents.x, -extents.y, extents.z));
    //    vertices[6] = transformPoint(new float3(extents.x, -extents.y, -extents.z));
    //    vertices[7] = transformPoint(new float3(-extents.x, -extents.y, -extents.z));

    //    Debug.DrawLine(vertices[0], vertices[1], color);
    //    Debug.DrawLine(vertices[1], vertices[2], color);
    //    Debug.DrawLine(vertices[2], vertices[3], color);
    //    Debug.DrawLine(vertices[3], vertices[0], color);

    //    Debug.DrawLine(vertices[4], vertices[5], color);
    //    Debug.DrawLine(vertices[5], vertices[6], color);
    //    Debug.DrawLine(vertices[6], vertices[7], color);
    //    Debug.DrawLine(vertices[7], vertices[4], color);

    //    Debug.DrawLine(vertices[0], vertices[4], color);
    //    Debug.DrawLine(vertices[1], vertices[5], color);
    //    Debug.DrawLine(vertices[2], vertices[6], color);
    //    Debug.DrawLine(vertices[3], vertices[7], color);
    //}
}
