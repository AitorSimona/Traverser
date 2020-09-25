using UnityEngine;

using Unity.Mathematics;

public static class Raycast
{
    /// <summary>
    /// Casts a ray against colliders in the scene and reports the closest hit.
    /// </summary>
    /// <remarks>
    /// Casts a ray against all colliders in the scene and reports the closest hit.
    /// <para>
    /// The optional layer mask allows to to filter out colliders.
    /// </para>
    /// <para>
    /// Optionally, a specific transform can be filtered out. This is used
    /// to prevent self-collisions with the shape initiating the query.
    /// </para>
    /// </remarks>
    /// <param name="origin">The starting point of the ray in world space.</param>
    /// <param name="direction">The normalized direction of the ray in world space.</param>
    /// <param name="result">Result of the ray cast. This will always report the closest hit.</param>
    /// <param name="maxDistance">The max distance for the ray cast.</param>
    /// <param name="layerMask">Layer mask used to selectively ignore colliders.</param>
    /// <param name="ignore">Transform to be ignored as a result.</param>
    /// <returns>True if the ray intersects with a collider, otherwise false.</returns>
    public static bool ClosestHit(float3 origin, float3 direction, out RaycastHit result, float maxDistance = 1000.0f, int layerMask = -1, Transform ignore = null)
    {
        bool IsDescendant(Transform parent, Transform descendant)
        {
            if (parent == null)
            {
                return false;
            }

            Transform descendantParent = descendant;

            while (descendantParent != null)
            {
                if (descendantParent == parent)
                {
                    return true;
                }

                descendantParent = descendantParent.parent;
            }

            return false;
        }

        var raycastHits = new RaycastHit[16];

        int RaycastNonAlloc()
        {
            if (layerMask != -1)
            {
                return Physics.RaycastNonAlloc(
                    origin, direction, raycastHits,
                        maxDistance, layerMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                return Physics.RaycastNonAlloc(
                    origin, direction, raycastHits, maxDistance);
            }
        }

        if (ignore == null && layerMask != -1)
        {
            return Physics.Raycast(
                origin, direction, out result,
                    maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        }

        result = new RaycastHit();

        int numHits = RaycastNonAlloc();

        if (numHits == 0)
        {
            return false;
        }
        else if (numHits == 1)
        {
            Transform transform = raycastHits[0].collider.transform;

            if (ignore != null && IsDescendant(ignore, transform))
            {
                return false;
            }

            result = raycastHits[0];

            return true;
        }
        else
        {
            for (int i = 0; i < numHits; i++)
            {
                Transform transform = raycastHits[i].collider.transform;

                if (ignore != null && IsDescendant(ignore, transform))
                {
                    continue;
                }

                if (result.collider == null || raycastHits[i].distance < result.distance)
                {
                    result = raycastHits[i];
                }
            }

            if (result.collider != null)
            {
                return true;
            }
        }

        return false;
    }
}
