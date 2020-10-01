using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --- Wrapper for ability-collisions interactions ---

namespace CWLF
{

    public static class CollisionLayer
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
