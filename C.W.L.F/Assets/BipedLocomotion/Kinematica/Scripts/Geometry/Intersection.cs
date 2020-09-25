using UnityEngine;

using Unity.Mathematics;

public static class Intersection
{
    /// <summary>
    /// Collects all colliders that are touching or are inside a sphere.
    /// </summary>
    /// <param name="position">Position in world space of the sphere.</param>
    /// <param name="radius">Radius of the sphere.</param>
    /// <param name="result">Array of colliders that the sphere overlaps with.</param>
    /// <param name="layerMask">Layer mask used to selectively ignore colliders.</param>
    /// <param name="ignore">Transform to be ignored in the result.</param>
    /// <returns>Number of colliders collected in the result.</returns>
    public static int OverlapSphere(float3 position, float radius, out Collider[] result, int layerMask = -1, Transform ignore = null)
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

        var colliders = new Collider[16];

        int OverlapSphereNonAlloc()
        {
            if (layerMask != -1)
            {
                return Physics.OverlapSphereNonAlloc(position, radius, colliders, layerMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                return Physics.OverlapSphereNonAlloc(position, radius, colliders);
            }
        }

        result = null;

        int numHits = OverlapSphereNonAlloc();

        if (numHits == 0)
        {
            return 0;
        }
        else if (numHits == 1)
        {
            Transform transform = colliders[0].transform;

            if (ignore != null && IsDescendant(ignore, transform))
            {
                return 0;
            }

            result = colliders;

            return 1;
        }
        else
        {
            int numValidHits = 0;

            for (int i = 0; i < numHits; i++)
            {
                Transform transform = colliders[i].transform;

                if (ignore != null && IsDescendant(ignore, transform))
                {
                    colliders[i] = colliders[--numHits];
                }
                else
                {
                    numValidHits++;
                }
            }

            result = colliders;

            return numValidHits;
        }
    }
}
