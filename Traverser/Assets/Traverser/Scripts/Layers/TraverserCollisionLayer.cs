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

        public static int CastCapsuleCollisions(float3 startPosition, float3 endPosition, float radius, float3 normalizedDisplacement, ref RaycastHit[] raycastHits, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            return Physics.CapsuleCastNonAlloc(startPosition, endPosition, radius, normalizedDisplacement, raycastHits, 
                maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        }

        // --- Turn off controller ---
        public static void ConfigureController(bool active, ref MovementController controller)
        {
            controller.collisionEnabled = !active;
            controller.groundSnap = !active;
            controller.resolveGroundPenetration = !active;
            controller.gravityEnabled = !active;
        }

        // --------------------------------
    }

}
