using UnityEngine;

using Unity.Mathematics;

public static partial class ClosestPoint
{
    /// <summary>
    /// Calculates the closest contact point on the collider surface
    /// with respect to the given position.
    /// </summary>
    /// <remarks>
    /// This function calculates the closest contact point on the collider
    /// surface with respect to the given world space position.
    /// The closest point will be reported correctly even in the case
    /// where the position is inside the collider.
    /// This function returns <see cref="Missing.Nan"/> if the collider type
    /// is not supported or if the collision layer does not match.
    /// </remarks>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="collider">Collider to find the closest surface point on.</param>
    /// <param name="collisionLayers">Layer mask that denotes the expected collision layer.</param>
    /// <returns>Closest contact point on the collider surface or NaN.</returns>
    public static float3 FromPosition(float3 position, Collider collider, int collisionLayers = -1)
    {
        if (collisionLayers > -1 && collider != null && collider.gameObject != null)
        {
            if (((1 << collider.gameObject.layer) & collisionLayers) == 0)
            {
                return Missing.NaN;
            }
        }

        if (collider is BoxCollider)
        {
            return FromPosition(position, collider as BoxCollider);
        }
        else if (collider is SphereCollider)
        {
            return FromPosition(position, collider as SphereCollider);
        }
        else if (collider is CapsuleCollider)
        {
            return FromPosition(position, collider as CapsuleCollider);
        }
        else if (collider is CharacterController)
        {
            return FromPosition(position, collider as CharacterController);
        }
        else
        {
            return Missing.NaN;
        }
    }

    /// <summary>
    /// Calculates the closest contact point on a box collider
    /// with respect to the given position.
    /// </summary>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="collider">Box collider to find the closest surface point on.</param>
    /// <returns>Closest contact point on the box collider.</returns>
    public static float3 FromPosition(float3 position, BoxCollider collider)
    {
        var obb = BoundingBox.FromBoxCollider(collider);

        var localPosition = obb.inverseTransformPoint(position);

        var half = obb.size * 0.5f;

        if (localPosition.x < -half.x || localPosition.x > half.x ||
            localPosition.y < -half.y || localPosition.y > half.y ||
            localPosition.z < -half.z || localPosition.z > half.z)
        {
            localPosition.x = math.clamp(localPosition.x, -half.x, half.x);
            localPosition.y = math.clamp(localPosition.y, -half.y, half.y);
            localPosition.z = math.clamp(localPosition.z, -half.z, half.z);
        }
        else
        {
            float x = half.x - Mathf.Abs(localPosition.x);
            float y = half.y - Mathf.Abs(localPosition.y);
            float z = half.z - Mathf.Abs(localPosition.z);

            if (x < y && x < z)
            {
                localPosition.x = (localPosition.x < 0.0f ? -half.x : half.x);
            }
            else if (y < x && y < z)
            {
                localPosition.y = (localPosition.y < 0.0f ? -half.y : half.y);
            }
            else
            {
                localPosition.z = (localPosition.z < 0.0f ? -half.z : half.z);
            }
        }

        return obb.transformPoint(localPosition);
    }

    /// <summary>
    /// Calculates the closest contact point on a sphere collider
    /// with respect to the given position.
    /// </summary>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="collider">Sphere collider to find the closest surface point on.</param>
    /// <returns>Closest contact point on the sphere collider.</returns>
    public static float3 FromPosition(float3 position, SphereCollider collider)
    {
        Transform transform = collider.transform;

        var center = Missing.Convert(transform.position + collider.center);

        var direction = math.normalizesafe(position - center);

        var localPosition = direction * (collider.radius * transform.localScale.x);

        return center + localPosition;
    }

    /// <summary>
    /// Calculates the closest contact point on a capsule collider
    /// with respect to the given position.
    /// </summary>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="collider">Capsule collider to find the closest surface point on.</param>
    /// <returns>Closest contact point on the capsule collider.</returns>
    public static float3 FromPosition(float3 position, CapsuleCollider collider)
    {
        var closestPoint = Missing.zero;

        Transform transform = collider.transform;

        var localPoint = transform.InverseTransformPoint(position);

        var halfHeight = collider.height * 0.5f;
        var center = Missing.Convert(collider.center);
        var radius = collider.radius;

        if (collider.direction == 0)
        {
            var endOffset = Missing.right * (halfHeight - radius);

            if (localPoint.x > center.x + halfHeight - radius)
            {
                closestPoint = FromPosition(
                    localPoint, center + endOffset, radius);
            }
            else if (localPoint.x < center.x - halfHeight + radius)
            {
                closestPoint = FromPosition(
                    localPoint, center - endOffset, radius);
            }
            else
            {
                closestPoint = FromPosition(
                    localPoint, center - endOffset,
                        center + endOffset);

                closestPoint = FromPosition(
                    localPoint, closestPoint, radius);
            }

            return transform.TransformPoint(closestPoint);
        }
        else if (collider.direction == 1)
        {
            var endOffset = Missing.up * (halfHeight - radius);

            if (localPoint.y > center.y + halfHeight - radius)
            {
                closestPoint = FromPosition(
                    localPoint, center + endOffset, radius);
            }
            else if (localPoint.y < center.y - halfHeight + radius)
            {
                closestPoint = FromPosition(
                    localPoint, center - endOffset, radius);
            }
            else
            {
                closestPoint = FromPosition(
                    localPoint, center - endOffset, center + endOffset);

                closestPoint = FromPosition(
                    localPoint, closestPoint, radius);
            }

            return transform.TransformPoint(closestPoint);
        }
        else
        {
            var endOffset = Missing.forward * (halfHeight - radius);

            if (localPoint.z > center.z + halfHeight - radius)
            {
                closestPoint = FromPosition(
                    localPoint, center + endOffset, radius);
            }
            else if (localPoint.z < center.z - halfHeight + radius)
            {
                closestPoint = FromPosition(
                    localPoint, center - endOffset, radius);
            }
            else
            {
                closestPoint = FromPosition(
                    localPoint, center - endOffset,
                        center + endOffset);

                closestPoint = FromPosition(
                    localPoint, closestPoint, radius);
            }

            return transform.TransformPoint(closestPoint);
        }
    }

    /// <summary>
    /// Calculates the closest contact point on a character controller
    /// with respect to the given position.
    /// </summary>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="controller">Character controller to find the closest surface point on.</param>
    /// <returns>Closest contact point on the character controller.</returns>
    public static float3 FromPosition(float3 position, CharacterController controller)
    {
        var closestPoint = Missing.zero;

        Transform transform = controller.transform;

        var localPoint =
            position - Missing.Convert(
                transform.position);

        var height = controller.height;
        var radius = controller.radius;
        var center = Missing.Convert(controller.center);

        var endOffset = Missing.up * ((height * 0.5f) - radius);

        if (localPoint.y > height - radius)
        {
            closestPoint = FromPosition(
                localPoint, center + endOffset, radius);
        }
        else if (localPoint.y < controller.radius)
        {
            closestPoint = FromPosition(
                localPoint, center - endOffset, radius);
        }
        else
        {
            closestPoint = FromPosition(
                localPoint, center - endOffset,
                    center + endOffset);

            closestPoint = FromPosition(
                localPoint, closestPoint, radius);
        }

        return closestPoint + Missing.Convert(transform.position);
    }

    /// <summary>
    /// Calculates the closest point on a sphere with respect to the given position.
    /// </summary>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="collider">Sphere collider to find the closest surface point on.</param>
    /// <returns>Closest point on the sphere.</returns>
    public static float3 FromPosition(float3 position, float3 spherePosition, float radius)
    {
        var direction = math.normalizesafe(position - spherePosition);

        var localPosition = direction * radius;

        return spherePosition + localPosition;
    }

    /// <summary>
    /// Calculates the closest point on a line segment with respect to the given position.
    /// </summary>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="startPosition">World space start position of the line segment.</param>
    /// <param name="endPosition">World space end position of the line segment.</param>
    /// <returns>Closest point on the line segment.</returns>
    public static float3 FromPosition(float3 position, float3 startPosition, float3 endPosition)
    {
        var segment = endPosition - startPosition;

        var projection = Missing.project(
            position - startPosition,
                math.normalizesafe(segment));

        var closestPoint = projection + startPosition;

        if (math.dot(projection, segment) < 0.0f)
        {
            closestPoint = startPosition;
        }
        else if (math.lengthsq(projection) > math.lengthsq(segment))
        {
            closestPoint = endPosition;
        }

        return closestPoint;
    }

    /// <summary>
    /// Calculates the closest point on a cylinder with respect to the given position.
    /// </summary>
    /// <param name="position">World space position that we want to find the closest point for.</param>
    /// <param name="startPosition">World space start position of the cylinder.</param>
    /// <param name="endPosition">World space end position of the cylinder.</param>
    /// <param name="radius">Radius in meters of the cylinder.</param>
    /// <returns>Closest point on the cylinder.</returns>
    public static float3 FromPosition(float3 position, float3 startPosition, float3 endPosition, float radius)
    {
        var segment = endPosition - startPosition;

        var projection = Missing.project(
            position - startPosition,
                math.normalizesafe(segment));

        var closestPoint = projection + startPosition;

        if (math.dot(projection, segment) < 0f)
        {
            closestPoint = startPosition;
        }
        else if (math.lengthsq(projection) > math.lengthsq(segment))
        {
            closestPoint = endPosition;
        }

        closestPoint += math.normalizesafe(position - closestPoint) * radius;

        return closestPoint;
    }
}
