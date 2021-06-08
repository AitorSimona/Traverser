using UnityEngine;

using Unity.Mathematics;

public static partial class ClosestPoint
{
    /// <summary>
    /// Calculates the closest contact point between the collider
    /// surface and the given line segment.
    /// </summary>
    /// <remarks>
    /// This function calculates the closest point between the collider
    /// surface and the given world space line segment.
    /// The closest point will be reported correctly even in the case
    /// where the line segment intersects the collider.
    /// This function returns <see cref="Missing.Nan"/> if the collider type
    /// is not supported or if the collision layer does not match.
    /// </remarks>
    /// <param name="startPosition">World space start position of the line segment we want to find the closest point for.</param>
    /// <param name="endPosition">World space end position of the line segment we want to find the closest point for.</param>
    /// <param name="collider">Collider to find the closest surface point on.</param>
    /// <param name="collisionLayers">Layer mask that denotes the expected collision layer.</param>
    /// <returns>Closest point in world space between the collider and the line segment or NaN.</returns>
    public static (float3, float3) FromLineSegment(float3 startPosition, float3 endPosition, Collider collider, int collisionLayers = -1)
    {
        if (collisionLayers > -1 && collider != null && collider.gameObject != null)
        {
            if (((1 << collider.gameObject.layer) & collisionLayers) == 0)
            {
                return (Missing.NaN, Missing.NaN);
            }
        }

        if (collider is BoxCollider)
        {
            return FromLineSegment(startPosition, endPosition, collider as BoxCollider);
        }
        else if (collider is SphereCollider)
        {
            return FromLineSegment(startPosition, endPosition, collider as SphereCollider);
        }
        else if (collider is CapsuleCollider)
        {
            return FromLineSegment(startPosition, endPosition, collider as CapsuleCollider);
        }
        else if (collider is CharacterController)
        {
            return FromLineSegment(startPosition, endPosition, collider as CharacterController);
        }

        return (Missing.NaN, Missing.NaN);
    }

    static (float3, float3) FromLineSegment(float3 startPosition, float3 endPosition, BoxCollider collider)
    {
        Transform transform = collider.transform;

        var obb = BoundingBox.FromBoxCollider(collider);

        var segment = endPosition - startPosition;
        var lineDirection = math.normalizesafe(segment);
        var lineCenter = startPosition + (lineDirection * (math.length(segment) * 0.5f));
        var center2Center = lineCenter - obb.transform.t;

        float3 x = transform.right;
        float3 y = transform.up;
        float3 z = transform.forward;

        var boxPoint = new float3(
            math.dot(center2Center, x),
                math.dot(center2Center, y),
                    math.dot(center2Center, z));

        var boxDirection = new float3(
            math.dot(lineDirection, x),
                math.dot(lineDirection, y),
                    math.dot(lineDirection, z));

        var extents = obb.size * 0.5f;

        if (boxDirection.x < 0.0f)
        {
            boxPoint.x = -boxPoint.x;
            boxDirection.x = -boxDirection.x;
        }

        if (boxDirection.y < 0f)
        {
            boxPoint.y = -boxPoint.y;
            boxDirection.y = -boxDirection.y;
        }

        if (boxDirection.z < 0f)
        {
            boxPoint.z = -boxPoint.z;
            boxDirection.z = -boxDirection.z;
        }

        float lineDistance = 0f;

        if (boxDirection.x > 0f)
        {
            if (boxDirection.y > 0f)
            {
                if (boxDirection.z > 0f)
                {
                    lineDistance = GetLineDistance(extents, boxPoint, boxDirection);
                }
                else
                {
                    lineDistance = GetLineDistance(extents, boxPoint, boxDirection, 0, 1);
                }
            }
            else
            {
                if (boxDirection.z > 0f)
                {
                    lineDistance = GetLineDistance(extents, boxPoint, boxDirection, 0, 2);
                }
                else
                {
                    lineDistance = GetLineDistance(extents, boxPoint, boxDirection, 0);
                }
            }
        }
        else
        {
            if (boxDirection.y > 0f)
            {
                if (boxDirection.z > 0f)
                {
                    lineDistance = GetLineDistance(extents, boxPoint, boxDirection, 1, 2);
                }
                else
                {
                    lineDistance = GetLineDistance(extents, boxPoint, boxDirection, 1);
                }
            }
            else
            {
                if (boxDirection.z > 0f)
                {
                    lineDistance = GetLineDistance(extents, boxPoint, boxDirection, 2);
                }
                else
                {
                    lineDistance = 0f;
                }
            }
        }

        var linePoint = lineCenter + (lineDirection * lineDistance);

        float lineExtent = math.length(segment) / 2.0f;
        if (lineDistance >= -lineExtent)
        {
            if (lineDistance > lineExtent)
            {
                linePoint = endPosition;
            }
        }
        else
        {
            linePoint = startPosition;
        }

        var colliderPoint = FromPosition(linePoint, collider);

        return (linePoint, colliderPoint);
    }

    static (float3, float3) FromLineSegment(float3 startPosition, float3 endPosition, SphereCollider collider)
    {
        Transform transform = collider.transform;

        float3 position = transform.position;
        float3 center = collider.center;

        var radius = collider.radius * transform.lossyScale.x;

        var segment = endPosition - startPosition;
        var lineDirection = math.normalizesafe(segment);

        var delta = position + center - startPosition;

        float cosAngle = math.dot(delta, lineDirection);
        var projection = lineDirection * math.max(cosAngle, 0.0f);
        float length = math.min(math.length(projection), math.length(segment));

        var linePoint = startPosition + (lineDirection * length);

        delta = position + center - linePoint;

        var colliderPoint = linePoint +
            (math.normalizesafe(delta) *
                (math.length(delta) - radius));

        return (linePoint, colliderPoint);
    }

    static (float3, float3) FromLineSegment(float3 startPosition, float3 endPosition, CapsuleCollider collider)
    {
        Transform transform = collider.transform;

        float height = collider.height;
        float radius = collider.radius;

        float3 center = collider.center;

        var offset = float3.zero;

        offset[collider.direction] = (height / 2f) - radius;

        float3 capsuleStart = transform.TransformPoint(center - offset);
        float3 capsuleEnd = transform.TransformPoint(center + offset);

        var (linePoint, colliderPoint) =
            FromLineSegment(startPosition, endPosition,
                capsuleStart, capsuleEnd);

        var delta = linePoint - colliderPoint;

        colliderPoint +=
            math.normalizesafe(delta) *
                (radius * transform.lossyScale.x);

        return (linePoint, colliderPoint);
    }

    static (float3, float3) FromLineSegment(float3 startPosition, float3 endPosition, CharacterController controller)
    {
        Transform transform = controller.transform;

        float3 center = controller.center;

        var offset = float3.zero;

        offset[1] = (controller.height / 2f) - controller.radius;

        float3 capsuleStart = transform.TransformPoint(center - offset);
        float3 capsuleEnd = transform.TransformPoint(center + offset);

        var (linePoint, colliderPoint) =
            FromLineSegment(startPosition, endPosition,
                capsuleStart, capsuleEnd);

        var delta = colliderPoint - linePoint;

        colliderPoint = linePoint +
            (math.normalizesafe(delta) *
                (math.length(delta) - controller.radius));

        return (linePoint, colliderPoint);
    }

    static (float3, float3) FromLineSegment(float3 startPosition1, float3 endPosition1, float3 startPosition2, float3 endPosition2)
    {
        var lineSegment1 = endPosition1 - startPosition1;
        float lineHalfLength1 = math.length(lineSegment1) * 0.5f;
        var lineDirection1 = math.normalizesafe(lineSegment1);
        var lineCenter1 = startPosition1 + (lineDirection1 * lineHalfLength1);
        float lineDistance1 = 0.0f;

        var lineSegment2 = endPosition2 - startPosition2;
        float lineHalfLength2 = math.length(lineSegment2) * 0.5f;
        var lineDirection2 = math.normalizesafe(lineSegment2);
        var lineCenter2 = startPosition2 + (lineDirection2 * lineHalfLength2);
        float lineDistance2 = 0.0f;

        var center2Center = lineCenter1 - lineCenter2;

        float line1DotLine2 = -math.dot(lineDirection1, lineDirection2);
        float dot1 = math.dot(center2Center, lineDirection1);
        float dot2 = -math.dot(center2Center, lineDirection2);
        float factor = math.abs(1.0f - line1DotLine2 * line1DotLine2);

        const float epsilon = 0.0001f;

        if (factor >= epsilon)
        {
            lineDistance1 = line1DotLine2 * dot2 - dot1;
            lineDistance2 = line1DotLine2 * dot1 - dot2;

            float extent0 = lineHalfLength1 * factor;
            float extentt1 = lineHalfLength2 * factor;

            if (lineDistance1 >= -extent0)
            {
                if (lineDistance1 <= extent0)
                {
                    if (lineDistance2 >= -extentt1)
                    {
                        if (lineDistance2 <= extentt1)
                        {
                            float reciprocal = 1.0f / factor;

                            lineDistance1 *= reciprocal;
                            lineDistance2 *= reciprocal;
                        }
                        else
                        {
                            lineDistance2 = lineHalfLength2;

                            float lineDistance = -(line1DotLine2 * lineDistance2 + dot1);
                            if (lineDistance < -lineHalfLength1)
                            {
                                lineDistance1 = -lineHalfLength1;
                            }
                            else if (lineDistance <= lineHalfLength1)
                            {
                                lineDistance1 = lineDistance;
                            }
                            else
                            {
                                lineDistance1 = lineHalfLength1;
                            }
                        }
                    }
                    else
                    {
                        lineDistance2 = -lineHalfLength2;

                        float lineDistance = -(line1DotLine2 * lineDistance2 + dot1);
                        if (lineDistance < -lineHalfLength1)
                        {
                            lineDistance1 = -lineHalfLength1;
                        }
                        else if (lineDistance <= lineHalfLength1)
                        {
                            lineDistance1 = lineDistance;
                        }
                        else
                        {
                            lineDistance1 = lineHalfLength1;
                        }
                    }
                }
                else
                {
                    if (lineDistance2 >= -extentt1)
                    {
                        if (lineDistance2 <= extentt1)
                        {
                            lineDistance1 = lineHalfLength1;

                            float lineDistance = -(line1DotLine2 * lineDistance1 + dot2);
                            if (lineDistance < -lineHalfLength2)
                            {
                                lineDistance2 = -lineHalfLength2;
                            }
                            else if (lineDistance <= lineHalfLength2)
                            {
                                lineDistance2 = lineDistance;
                            }
                            else
                            {
                                lineDistance2 = lineHalfLength2;
                            }
                        }
                        else
                        {
                            lineDistance2 = lineHalfLength2;

                            float lineDistance = -(line1DotLine2 * lineDistance2 + dot1);
                            if (lineDistance < -lineHalfLength1)
                            {
                                lineDistance1 = -lineHalfLength1;
                            }
                            else if (lineDistance <= lineHalfLength1)
                            {
                                lineDistance1 = lineDistance;
                            }
                            else
                            {
                                lineDistance1 = lineHalfLength1;

                                lineDistance = -(line1DotLine2 * lineDistance1 + dot2);
                                if (lineDistance < -lineHalfLength2)
                                {
                                    lineDistance2 = -lineHalfLength2;
                                }
                                else if (lineDistance <= lineHalfLength2)
                                {
                                    lineDistance2 = lineDistance;
                                }
                                else
                                {
                                    lineDistance2 = lineHalfLength2;
                                }
                            }
                        }
                    }
                    else
                    {
                        lineDistance2 = -lineHalfLength2;

                        float lineDistance = -(line1DotLine2 * lineDistance2 + dot1);
                        if (lineDistance < -lineHalfLength1)
                        {
                            lineDistance1 = -lineHalfLength1;
                        }
                        else if (lineDistance <= lineHalfLength1)
                        {
                            lineDistance1 = lineDistance;
                        }
                        else
                        {
                            lineDistance1 = lineHalfLength1;

                            lineDistance = -(line1DotLine2 * lineDistance1 + dot2);
                            if (lineDistance > lineHalfLength2)
                            {
                                lineDistance2 = lineHalfLength2;
                            }
                            else if (lineDistance >= -lineHalfLength2)
                            {
                                lineDistance2 = lineDistance;
                            }
                            else
                            {
                                lineDistance2 = -lineHalfLength2;
                            }
                        }
                    }
                }
            }
            else
            {
                if (lineDistance2 >= -extentt1)
                {
                    if (lineDistance2 <= extentt1)
                    {
                        lineDistance1 = -lineHalfLength1;

                        float lineDistance = -(line1DotLine2 * lineDistance1 + dot2);
                        if (lineDistance < -lineHalfLength2)
                        {
                            lineDistance2 = -lineHalfLength2;
                        }
                        else if (lineDistance <= lineHalfLength2)
                        {
                            lineDistance2 = lineDistance;
                        }
                        else
                        {
                            lineDistance2 = lineHalfLength2;
                        }
                    }
                    else
                    {
                        lineDistance2 = lineHalfLength2;

                        float lineDistance = -(line1DotLine2 * lineDistance2 + dot1);
                        if (lineDistance > lineHalfLength1)
                        {
                            lineDistance1 = lineHalfLength1;
                        }
                        else if (lineDistance >= -lineHalfLength1)
                        {
                            lineDistance1 = lineDistance;
                        }
                        else
                        {
                            lineDistance1 = -lineHalfLength1;

                            lineDistance = -(line1DotLine2 * lineDistance1 + dot2);
                            if (lineDistance < -lineHalfLength2)
                            {
                                lineDistance2 = -lineHalfLength2;
                            }
                            else if (lineDistance <= lineHalfLength2)
                            {
                                lineDistance2 = lineDistance;
                            }
                            else
                            {
                                lineDistance2 = lineHalfLength2;
                            }
                        }
                    }
                }
                else
                {
                    lineDistance2 = -lineHalfLength2;

                    float lineDistance = -(line1DotLine2 * lineDistance2 + dot1);
                    if (lineDistance > lineHalfLength1)
                    {
                        lineDistance1 = lineHalfLength1;
                    }
                    else if (lineDistance >= -lineHalfLength1)
                    {
                        lineDistance1 = lineDistance;
                    }
                    else
                    {
                        lineDistance1 = -lineHalfLength1;

                        lineDistance = -(line1DotLine2 * lineDistance1 + dot2);
                        if (lineDistance < -lineHalfLength2)
                        {
                            lineDistance2 = -lineHalfLength2;
                        }
                        else if (lineDistance <= lineHalfLength2)
                        {
                            lineDistance2 = lineDistance;
                        }
                        else
                        {
                            lineDistance2 = lineHalfLength2;
                        }
                    }
                }
            }
        }
        else
        {
            var extents = lineHalfLength1 + lineHalfLength2;
            var sign = (line1DotLine2 > 0.0f ? -1.0f : 1.0f);
            var average = (dot1 - sign * dot2) * 0.5f;
            var negativeAverage = -average;

            if (negativeAverage < -extents)
            {
                negativeAverage = -extents;
            }
            else if (negativeAverage > extents)
            {
                negativeAverage = extents;
            }

            lineDistance2 = -sign * negativeAverage * lineHalfLength2 / extents;
            lineDistance1 = negativeAverage + sign * lineDistance2;
        }

        var linePoint1 = lineCenter1 + (lineDirection1 * lineDistance1);
        var linePoint2 = lineCenter2 + (lineDirection2 * lineDistance2);

        return (linePoint1, linePoint2);
    }

    static (float3, float3) FromLineSegment(float3 startPosition, float3 endPosition, float3 position, float radius)
    {
        var segment = endPosition - startPosition;
        var lineDirection = math.normalizesafe(segment);

        var delta = position - startPosition;

        var cosAngle = math.dot(delta, lineDirection);
        var projection = lineDirection * math.max(cosAngle, 0.0f);
        var length = math.min(math.length(projection), math.length(segment));

        var linePoint = startPosition + (lineDirection * length);

        delta = position - linePoint;

        var colliderPoint = linePoint +
            (math.normalizesafe(delta) *
                (math.length(delta) - radius));

        return (linePoint, colliderPoint);
    }

    static float GetLineDistance(float3 boxExtents, float3 boxPoint, float3 boxDirection)
    {
        var extents = boxPoint - boxExtents;

        if (boxDirection.y * extents.x >= boxDirection.x * extents.y)
        {
            if (boxDirection.z * extents.x >= boxDirection.x * extents.z)
            {
                return GetLineDistance(boxExtents, boxPoint, boxDirection, extents, 0, 1, 2);
            }
            else
            {
                return GetLineDistance(boxExtents, boxPoint, boxDirection, extents, 2, 0, 1);
            }
        }
        else
        {
            if (boxDirection.z * extents.y >= boxDirection.y * extents.z)
            {
                return GetLineDistance(boxExtents, boxPoint, boxDirection, extents, 1, 2, 0);
            }
            else
            {
                return GetLineDistance(boxExtents, boxPoint, boxDirection, extents, 2, 0, 1);
            }
        }
    }

    static float GetLineDistance(float3 boxExtents, float3 boxPoint, float3 boxDirection, float3 extentToPoint, int i0, int i1, int i2)
    {
        var extents = float3.zero;

        extents[i1] = boxPoint[i1] + boxExtents[i1];
        extents[i2] = boxPoint[i2] + boxExtents[i2];

        if (boxDirection[i0] * extents[i1] >= boxDirection[i1] * extentToPoint[i0])
        {
            if (boxDirection[i0] * extents[i2] >= boxDirection[i2] * extentToPoint[i0])
            {
                var reciprocal = 1.0f / boxDirection[i0];

                return -extentToPoint[i0] * reciprocal;
            }
            else
            {
                var lengthSquared =
                    boxDirection[i0] * boxDirection[i0] +
                        boxDirection[i2] * boxDirection[i2];

                var temp = lengthSquared * extents[i1] -
                    boxDirection[i1] * (boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i2] * extents[i2]);

                if (temp <= 2.0f * lengthSquared * boxExtents[i1])
                {
                    var percentage = temp / lengthSquared;
                    lengthSquared += boxDirection[i1] * boxDirection[i1];
                    temp = extents[i1] - percentage;

                    var delta = boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i1] * temp + boxDirection[i2] * extents[i2];

                    return -delta / lengthSquared;
                }
                else
                {
                    lengthSquared += boxDirection[i1] * boxDirection[i1];

                    var delta = boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i1] * extentToPoint[i1] + boxDirection[i2] * extents[i2];

                    return -delta / lengthSquared;
                }
            }
        }
        else
        {
            if (boxDirection[i0] * extents[i2] >= boxDirection[i2] * extentToPoint[i0])
            {
                var lengthSquared =
                    boxDirection[i0] * boxDirection[i0] +
                        boxDirection[i1] * boxDirection[i1];

                var temp = lengthSquared * extents[i2] -
                    boxDirection[i2] * (boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i1] * extents[i1]);

                if (temp <= 2.0f * lengthSquared * boxExtents[i2])
                {
                    var percentage = temp / lengthSquared;
                    lengthSquared += boxDirection[i2] * boxDirection[i2];
                    temp = extents[i2] - percentage;

                    var delta = boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i1] * extents[i1] + boxDirection[i2] * temp;

                    return -delta / lengthSquared;
                }
                else
                {
                    lengthSquared += boxDirection[i2] * boxDirection[i2];

                    var delta = boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i1] * extents[i1] + boxDirection[i2] * extentToPoint[i2];

                    return -delta / lengthSquared;
                }
            }
            else
            {
                var delta = 0.0f;

                var lengthSquared = boxDirection[i0] * boxDirection[i0] +
                    boxDirection[i2] * boxDirection[i2];

                var temp = lengthSquared * extents[i1] -
                    boxDirection[i1] * (boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i2] * extents[i2]);

                if (temp >= 0f)
                {
                    if (temp <= 2f * lengthSquared * boxExtents[i1])
                    {
                        var percentage = temp / lengthSquared;
                        lengthSquared += boxDirection[i1] * boxDirection[i1];
                        temp = extents[i1] - percentage;

                        delta = boxDirection[i0] * extentToPoint[i0] +
                            boxDirection[i1] * temp + boxDirection[i2] * extents[i2];

                        return -delta / lengthSquared;
                    }
                    else
                    {
                        lengthSquared += boxDirection[i1] * boxDirection[i1];

                        delta = boxDirection[i0] * extentToPoint[i0] +
                            boxDirection[i1] * extentToPoint[i1] +
                                boxDirection[i2] * extents[i2];

                        return -delta / lengthSquared;
                    }
                }

                lengthSquared =
                    boxDirection[i0] * boxDirection[i0] +
                        boxDirection[i1] * boxDirection[i1];

                temp = lengthSquared * extents[i2] -
                    boxDirection[i2] * (boxDirection[i0] * extentToPoint[i0] +
                        boxDirection[i1] * extents[i1]);

                if (temp >= 0.0f)
                {
                    if (temp <= 2.0f * lengthSquared * boxExtents[i2])
                    {
                        var percentage = temp / lengthSquared;
                        lengthSquared += boxDirection[i2] * boxDirection[i2];
                        temp = extents[i2] - percentage;

                        delta = boxDirection[i0] * extentToPoint[i0] +
                            boxDirection[i1] * extents[i1] +
                                boxDirection[i2] * temp;

                        return -delta / lengthSquared;
                    }
                    else
                    {
                        lengthSquared += boxDirection[i2] * boxDirection[i2];

                        delta = boxDirection[i0] * extentToPoint[i0] +
                            boxDirection[i1] * extents[i1] +
                                boxDirection[i2] * extentToPoint[i2];

                        return -delta / lengthSquared;
                    }
                }

                lengthSquared += boxDirection[i2] * boxDirection[i2];

                delta = boxDirection[i0] * extentToPoint[i0] +
                    boxDirection[i1] * extents[i1] +
                        boxDirection[i2] * extents[i2];

                return -delta / lengthSquared;
            }
        }
    }

    static float GetLineDistance(float3 boxExtents, float3 boxPoint, float3 boxDirection, int i0, int i1)
    {
        var extentToPoint0 = boxPoint[i0] - boxExtents[i0];
        var extentToPoint1 = boxPoint[i1] - boxExtents[i1];

        var product0 = boxDirection[i1] * extentToPoint0;
        var product1 = boxDirection[i0] * extentToPoint1;

        if (product0 >= product1)
        {
            boxPoint[i0] = boxExtents[i0];

            extentToPoint1 = boxPoint[i1] + boxExtents[i1];

            var delta = product0 - boxDirection[i0] * extentToPoint1;

            if (delta >= 0.0f)
            {
                var reciprocal = 1.0f / (boxDirection[i0] *
                    boxDirection[i0] + boxDirection[i1] * boxDirection[i1]);

                return -(boxDirection[i0] * extentToPoint0 +
                    boxDirection[i1] * extentToPoint1) * reciprocal;
            }
            else
            {
                var reciprocal = 1.0f / boxDirection[i0];

                return -extentToPoint0 * reciprocal;
            }
        }
        else
        {
            boxPoint[i1] = boxExtents[i1];

            extentToPoint0 = boxPoint[i0] + boxExtents[i0];

            var delta = product1 - boxDirection[i1] * extentToPoint0;

            if (delta >= 0.0f)
            {
                var reciprocal = 1.0f / (boxDirection[i0] *
                    boxDirection[i0] + boxDirection[i1] * boxDirection[i1]);

                return -(boxDirection[i0] * extentToPoint0 +
                    boxDirection[i1] * extentToPoint1) * reciprocal;
            }
            else
            {
                var reciprocal = 1.0f / boxDirection[i1];

                return -extentToPoint1 * reciprocal;
            }
        }
    }

    static float GetLineDistance(float3 boxExtents, float3 boxPoint, float3 boxDirection, int i0)
    {
        return (boxExtents[i0] - boxPoint[i0]) / boxDirection[i0];
    }
}
