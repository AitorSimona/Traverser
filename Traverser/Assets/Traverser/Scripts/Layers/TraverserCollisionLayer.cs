using Unity.Mathematics;
using UnityEngine;

// --- Wrapper for ability-collisions interactions ---

namespace Traverser
{
    public static class TraverserCollisionLayer
    {
        // --- Attributes ---
        private static int ms_EnvironmentCollisionMask = -1;

        // --- All object layers our character can collide with ---
        public static int EnvironmentCollisionMask
        {
            get
            {
                if (ms_EnvironmentCollisionMask < 0)
                {
                    string[] layerNames = new string[] { "Default", "Wall", "Ledge", "Platform", "Table" };
                    ms_EnvironmentCollisionMask = LayerMask.GetMask(layerNames);
                }

                return ms_EnvironmentCollisionMask;
            }
        }

        // --------------------------------

        // --- Utilities ---

        // --- Return whether given capsule collider is colliding with any object in given layers ---
        public static bool IsCharacterCapsuleColliding(Vector3 rootPosition, ref CapsuleCollider capsule)
        {
            Vector3 capsuleCenter = rootPosition + capsule.center;
            Vector3 capsuleOffset = Vector3.up * (capsule.height * 0.5f - capsule.radius);

            return Physics.CheckCapsule(capsuleCenter - capsuleOffset, capsuleCenter + capsuleOffset, capsule.radius - 0.1f, EnvironmentCollisionMask);
        }

        // --- Return the collider we are grounded to (index of hitColliders), -1 if no ground found --
        public static int CastGroundProbe(float3 position, float groundProbeRadius, ref Collider[] hitColliders, int layerMask)
        {
            // --- Ground collision check ---
            int chosenGround = -1;
            float3 colliderPosition;
            int numColliders = Physics.OverlapSphereNonAlloc(position, groundProbeRadius, hitColliders, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore);

            if (numColliders > 0)
            {
                float minDistance = groundProbeRadius * 2.0f;

                for (int i = 0; i < numColliders; ++i)
                {
                    colliderPosition = hitColliders[i].ClosestPoint(position);

                    if (math.distance(colliderPosition.y, position.y) < minDistance)
                        chosenGround = i;
                }
            }

            return chosenGround;
        }

        // --- Turn off controller ---
        //public static void ConfigureController(bool active, ref MovementController controller)
        //{
        //    controller.collisionEnabled = !active;
        //    controller.groundSnap = !active;
        //    controller.resolveGroundPenetration = !active;
        //    controller.gravityEnabled = !active;
        //}

        // --------------------------------
    }

}
