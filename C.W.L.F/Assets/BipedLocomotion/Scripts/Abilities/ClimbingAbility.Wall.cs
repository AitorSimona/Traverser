using Unity.SnapshotDebugger;
using Unity.Mathematics;
using Unity.Kinematica;

using UnityEngine;


namespace CWLF
{

    // --- Definition of wall geometry, so we can adapt motion when colliding into it ---

    public partial class ClimbingAbility : SnapshotProvider, Ability
    {
        public struct WallGeometry
        {
            // --- Attributes that define a wall object ---
            public AffineTransform transform; // Ensure we remain on the same space
            public float3 scale; // global

            public float3 normal;
            public float3 center;
            public float3 size; // local

            // --- Construction ---
            public static WallGeometry Create()
            {
                return new WallGeometry();
            }

            // --- Create wall geometry from box collider ---
            public void Initialize(BoxCollider collider, AffineTransform contactTransform)
            {
                transform = Convert(collider.transform);
                scale = collider.transform.lossyScale;
                center = collider.center;
                size = collider.size;

                //Initialize(contactTransform);
            }

            // --- Utility transformation methods ---
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

        }

    }
}
