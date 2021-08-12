using UnityEngine;

// --- Wrapper for ability-collisions interactions ---

namespace Traverser
{
    public static class TraverserCollisionLayer
    {
        // --- Attributes ---
        //private static int ms_EnvironmentCollisionMask = -1;

        // --- All object layers our character can collide with ---

        // TODO : Remove unused layers
        //public static int EnvironmentCollisionMask
        //{
        //    get
        //    {
        //        if (ms_EnvironmentCollisionMask < 0)
        //        {
        //            string[] layerNames = new string[] { "Default" };
        //            ms_EnvironmentCollisionMask = LayerMask.GetMask(layerNames);
        //        }

        //        return ms_EnvironmentCollisionMask;
        //    }
        //}

        // --------------------------------

        // --- Utilities ---

        // --- Return whether given capsule collider is colliding with any object in given layers ---
        //public static bool IsCharacterCapsuleColliding(Vector3 rootPosition, Vector3 center, float height, float radius)
        //{
        //    Vector3 capsuleCenter = rootPosition + center;
        //    Vector3 capsuleOffset = Vector3.up * (height * 0.5f - radius);

        //    return Physics.CheckCapsule(capsuleCenter - capsuleOffset, capsuleCenter + capsuleOffset, radius - 0.1f, EnvironmentCollisionMask);
        //}

        // --- Return the collider we are grounded to (index of hitColliders), -1 if no ground found --
        public static int CastGroundProbe(Vector3 position, float groundProbeRadius, ref Collider[] hitColliders, int layerMask)
        {
            // --- Ground collision check ---
            int chosenGround = -1;
            Vector3 colliderPosition;
            int numColliders = Physics.OverlapSphereNonAlloc(position, groundProbeRadius, hitColliders, layerMask, QueryTriggerInteraction.Ignore);

            if (numColliders > 0)
            {
                float minDistance = groundProbeRadius * 2.0f;

                for (int i = 0; i < numColliders; ++i)
                {
                    colliderPosition = hitColliders[i].ClosestPoint(position);

                    if (Mathf.Abs(colliderPosition.y - position.y) < minDistance)
                        chosenGround = i;
                }
            }

            return chosenGround;
        }

        // --------------------------------
    }

}
