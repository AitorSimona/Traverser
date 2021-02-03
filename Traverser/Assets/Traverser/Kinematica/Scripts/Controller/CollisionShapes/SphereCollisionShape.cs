using UnityEngine;

using Unity.Mathematics;

internal class SphereCollisionShape : CollisionShape
{
    public SphereCollisionShape(SphereCollider collider)
    {
        this.collider = collider;
    }

    protected SphereCollider collider;

    public override float Radius
    {
        get { return collider.radius; }
    }

    public override float3 Center(float3 position)
    {
        return position + Missing.Convert(collider.center);
    }

    public override Collider Collider
    {
        get { return collider; }
    }

    const float epsilon = 0.001f;

    float3 startPosition;

    float3 direction;
    float distance;
    int layerMask;

    RaycastHit[] raycastHits = new RaycastHit[15];

    public override int CollisionCastAll(float3 position, float3 direction, float distance, int layerMask)
    {
        this.direction = direction;
        this.distance = distance;
        this.layerMask = layerMask;

        startPosition = Center(position);

        int numContacts = Physics.SphereCastNonAlloc(
            startPosition, Radius, direction, raycastHits,
                distance + epsilon, layerMask, QueryTriggerInteraction.Ignore);

        return numContacts;
    }

    public override Contact this[int index]
    {
        get
        {
            var contact = Contact.Create(
                startPosition, float3.zero);

            contact.shape = null;

            Transform currentTransform = raycastHits[index].collider.transform;
            if (currentTransform == collider.transform)
            {
                return contact;
            }

            while (currentTransform != null)
            {
                if (currentTransform == collider.transform)
                {
                    return contact;
                }

                currentTransform = currentTransform.parent;
            }

            contact.contact = raycastHits[index];
            contact.contactOrigin = contact.startPosition;
            contact.contactCollider = raycastHits[index].collider;
            contact.contactPoint = raycastHits[index].point;
            contact.contactNormal = raycastHits[index].normal;

            contact.contactDistance = raycastHits[index].distance;

            if (raycastHits[index].distance == 0f)
            {
                var colliderPoint = ClosestPoint.FromPosition(contact.startPosition, contact.contactCollider, layerMask);

                if (Missing.IsNaN(colliderPoint))
                {
                    return contact;
                }

                float3 delta = colliderPoint - contact.startPosition;
                if (math.abs(math.length(delta) - Radius) < epsilon)
                {
                    float dot = math.dot(math.normalizesafe(delta), direction);
                    if (dot <= -0.8f)
                    {
                        return contact;
                    }
                }

                contact.contactOrigin = contact.startPosition;
                contact.contactPoint = colliderPoint;

                contact.contactDistance = math.length(delta) - Radius;
                contact.contactPenetration = (contact.contactDistance < 0f);

                RaycastHit raycastHit;
                if (Raycast.ClosestHit(contact.startPosition, math.normalizesafe(delta),
                    out raycastHit, Mathf.Max(contact.contactDistance + Radius, Radius + 0.01f)))
                {
                    contact.contactNormal = raycastHit.normal;
                }
                else if (contact.contactDistance < epsilon)
                {
                    contact.contactNormal = math.normalizesafe(contact.startPosition - colliderPoint);
                }
            }

            if (!Missing.equalEps(direction, -Missing.up, epsilon))
            {
                RaycastHit raycastHit;
                if (Raycast.ClosestHit(contact.contactPoint - (direction * distance),
                    direction, out raycastHit, distance + Radius, -1, collider.transform))
                {
                    contact.contactNormal = raycastHit.normal;
                }
            }

            contact.shape = this;

            return contact;
        }
    }

    public override float3 FromPosition(float3 position, float3 origin)
    {
        return ClosestPoint.FromPosition(origin, Center(position), Radius);
    }

    public override void DebugDraw(float3 position, quaternion rotation, Color color)
    {
    }
}
