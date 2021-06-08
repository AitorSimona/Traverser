using UnityEngine;

using Unity.Mathematics;
using Unity.Kinematica;

internal class CapsuleCollisionShape : CollisionShape
{
    public CapsuleCollisionShape(CapsuleCollider collider)
    {
        this.collider = collider;
    }

    protected CapsuleCollider collider;

    public override float Radius
    {
        get { return collider.radius; }
    }

    public override Collider Collider
    {
        get { return collider; }
    }

    public float Height
    {
        get { return collider.height; }
    }

    public override float3 Center(float3 position)
    {
        return position + Missing.Convert(collider.center);
    }

    public float3 Top(float3 position)
    {
        return Center(position) + Missing.up * (Height * 0.5f - Radius);
    }

    public float3 Bottom(float3 position, float offset = 0.0f)
    {
        return Center(position) - Missing.up * (Height * 0.5f - Radius + offset);
    }

    const float epsilon = 0.001f;

    float3 startPosition;
    float3 endPosition;

    int layerMask;

    RaycastHit[] raycastHits = new RaycastHit[15];

    public override int CollisionCastAll(float3 position, float3 direction, float distance, int layerMask)
    {
        this.layerMask = layerMask;

        startPosition = Bottom(position);
        endPosition = Top(position);

        int numContacts = Physics.CapsuleCastNonAlloc(
            startPosition, endPosition, Radius,
                direction, raycastHits, distance + epsilon,
                    layerMask, QueryTriggerInteraction.Ignore);

        return numContacts;
    }

    public override Contact this[int index]
    {
        get
        {
            var contact = Contact.Create(
                startPosition, endPosition);

            contact.shape = null;

            Transform currentTransform = raycastHits[index].collider.transform;

            while (currentTransform != null)
            {
                if (currentTransform == collider.transform)
                {
                    return contact;
                }

                currentTransform = currentTransform.parent;
            }

            contact.contact = raycastHits[index];
            contact.contactCollider = raycastHits[index].collider;
            contact.contactPoint = raycastHits[index].point;
            contact.contactNormal = raycastHits[index].normal;
            contact.contactDistance = raycastHits[index].distance;

            if (raycastHits[index].distance == 0f)
            {
                var (linePoint, colliderPoint) =
                    ClosestPoint.FromLineSegment(
                        contact.startPosition, contact.endPosition,
                            contact.contactCollider, layerMask);

                if (Missing.IsNaN(colliderPoint))
                {
                    return contact;
                }

                var delta = colliderPoint - linePoint;

                contact.contactOrigin = linePoint;
                contact.contactPoint = colliderPoint;

                contact.contactDistance = math.length(delta) - Radius;
                contact.contactPenetration = (contact.contactDistance < 0f);

                RaycastHit raycastHit;
                if (Raycast.ClosestHit(
                    linePoint, math.normalizesafe(delta), out raycastHit,
                        math.max(contact.contactDistance + Radius, Radius + 0.01f)))
                {
                    contact.contactNormal = raycastHit.normal;
                }
                else if (contact.contactDistance < epsilon)
                {
                    contact.contactNormal =
                        math.normalizesafe(linePoint - colliderPoint);
                }
            }
            else
            {
                contact.contactOrigin = ClosestPoint.FromPosition(
                    contact.contactPoint, contact.startPosition, contact.endPosition);
            }

            contact.contactNormal = math.normalizesafe(contact.contactOrigin - contact.contactPoint);

            contact.shape = this;

            return contact;
        }
    }

    public override float3 FromPosition(float3 position, float3 origin)
    {
        return ClosestPoint.FromPosition(origin, Bottom(position), Top(position), Radius);
    }

    public override void DebugDraw(float3 position, quaternion rotation, Color color)
    {
        //DebugExtensions.DebugDrawCollider(position, rotation, collider, color);
        //DebugExtensions.DrawTransform(position, rotation, 0.3f);
    }
}
