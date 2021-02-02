using UnityEngine;

using Unity.Mathematics;

internal abstract class CollisionShape
{
    internal struct Contact
    {
        public CollisionShape shape;

        public float3 startPosition;

        public float3 endPosition;

        public Collider contactCollider;

        public float3 contactOrigin;

        public float3 contactPoint;

        public float3 contactNormal;

        public float contactDistance;

        public bool contactPenetration;

        public RaycastHit contact;

        public static Contact Create(float3 startPosition, float3 endPositions)
        {
            return new Contact
            {
                startPosition = startPosition,
                endPosition = endPositions
            };
        }
    }

    public abstract float Radius
    {
        get;
    }

    public abstract Collider Collider
    {
        get;
    }

    public abstract Contact this[int index]
    {
        get;
    }

    public abstract float3 Center(float3 position);

    public abstract int CollisionCastAll(float3 position, float3 direction, float distance, int layerMask);

    public abstract float3 FromPosition(float3 position, float3 origin);

    public abstract void DebugDraw(float3 position, quaternion rotation, Color color);
}
