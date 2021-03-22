using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    public partial class TraverserCharacterController : MonoBehaviour
    {

        // --- Wrapper to define a controller's state collision situation and movement information ---
        public struct TraverserCollision
        {
            // --- Attributes ---

            // --- The collider we just made contact with ---
            public Collider collider;

            // --- States if we are currently colliding ---
            public bool isColliding;

            // --- The point at which we collided with the current collider ---
            public float3 colliderContactPoint;

            // --- The current collider's normal direction ---
            public float3 colliderContactNormal;

            // --- Transform of the current ground, the object below the character ---
            public Collider ground;

            // --- States if the controller is grounded ---
            public bool isGrounded;

            // --- The controller's position (simulation) ---
            public float3 position;

            // --- The controller's velocity (simulation) ---
            public float3 velocity;

            // --- The controller's current kinematic based displacement ---
            public float3 kinematicDisplacement;

            // --- The controller's current dynamics based displacement ---
            public float3 dynamicsDisplacement;

            // --------------------------------

            // --- Basic methods ---

            internal static TraverserCollision Create()
            {
                return new TraverserCollision()
                {
                    position = float3.zero,
                    velocity = float3.zero,
                    collider = null,
                    isColliding = false,
                    colliderContactNormal = float3.zero,
                    colliderContactPoint = float3.zero,
                    ground = null,
                    isGrounded = false,
                    kinematicDisplacement = Vector3.zero,
                    dynamicsDisplacement = Vector3.zero,
                };
            }

            internal void Reset()
            {
                position = float3.zero;
                velocity = float3.zero;
                collider = null;
                isColliding = false;
                colliderContactNormal = float3.zero;
                colliderContactPoint = float3.zero;
                ground = null;
                isGrounded = false;
                kinematicDisplacement = float3.zero;
                dynamicsDisplacement = float3.zero;
            }

            internal void CopyFrom(ref TraverserCollision copyCollision)
            {
                collider = copyCollision.collider;
                isColliding = copyCollision.isColliding;
                colliderContactPoint = copyCollision.colliderContactPoint;
                colliderContactNormal = copyCollision.colliderContactNormal;
                ground = copyCollision.ground;
                position = copyCollision.position;
                velocity = copyCollision.velocity;
                isGrounded = copyCollision.isGrounded;
                kinematicDisplacement = copyCollision.kinematicDisplacement;
                dynamicsDisplacement = copyCollision.dynamicsDisplacement;
            }

            // --------------------------------
        }

        // --- Wrapper to define a controller's state, including previous and current collision situations ---
        public struct TraverserState
        {
            // --- Attributes ---

            // --- The state's previous collision situation --- 
            public TraverserCollision previousCollision;

            // --- The state's actual collision situation ---
            public TraverserCollision currentCollision;

            // --- The state's desired absolute displacement ---
            public float3 desiredDisplacement;

            // --------------------------------

            // --- Basic methods ---

            internal static TraverserState Create()
            {
                return new TraverserState()
                {
                    previousCollision = TraverserCollision.Create(),
                    currentCollision = TraverserCollision.Create(),
                    desiredDisplacement = float3.zero,
                };
            }

            internal void CopyFrom(ref TraverserState copyState)
            {
                previousCollision.CopyFrom(ref copyState.previousCollision);
                currentCollision.CopyFrom(ref copyState.currentCollision);
                desiredDisplacement = copyState.desiredDisplacement;
            }

            // --------------------------------
        }

        // --------------------------------

        // --- Private Variables ---

        // --- Unity's character controller ---
        private CharacterController characterController;

        // --- Actual controller state ---
        private TraverserState state;

        // --- State snapshot to save current state before movement simulation ---
        private TraverserState snapshotState;

        // --- To store simulation's last known position ---
        private float3 lastPosition;

        // --- The last contact position and rotation extracted from last collision ---
        private TraverserAffineTransform lastContactTransform = TraverserAffineTransform.Create(float3.zero, quaternion.identity);

        // --- Array of colliders for ground probing ---
        private Collider[] hitColliders = new Collider[3];

        // --- Ray for groundSnap checks ---
        private Ray groundRay = new Ray();

        // --- Array of raycast hits for groundSnap checks ---
        private RaycastHit[] groundRayHits = new RaycastHit[1];

        // --- Arrays of positions for geometry debugging ---
        private List<float3> probePositions;
        private List<float3> capsulePositions;
        private List<float3> planePositions;

        // --- Simulation counter (keep track of current iteration) ---
        private int simulationCounter = 0;

        // --------------------------------

        // --- Basic methods ---

        // Start is called before the first frame update
        void Start()
        {
            // --- Initialize controller ---
            state = TraverserState.Create();
            snapshotState = TraverserState.Create();
            characterController = GetComponent<CharacterController>();
            current.position = transform.position;
            targetPosition = transform.position;
            targetVelocity = float3.zero;
            targetHeading = 0.0f;

            // --- Initialize debug lists (consider commenting in build, with debugDraw set to false) ---
            probePositions = new List<float3>(3);
            planePositions = new List<float3>(3);
            capsulePositions = new List<float3>(3);
        }

        // --------------------------------

        // --- Collisions ---

        void CheckGroundCollision()
        {
            int colliderIndex = TraverserCollisionLayer.CastGroundProbe(position, groundProbeRadius, ref hitColliders, TraverserCollisionLayer.EnvironmentCollisionMask);

            if (colliderIndex != -1)
            {       
                current.ground = hitColliders[colliderIndex];
                current.isGrounded = true;
                //Debug.Log("Ground is:");
                //Debug.Log(current.ground.gameObject.name);
            }

            // --- Add cast position to debug draw lists ---
            if (debugDraw)
            {
                if (probePositions.Count == simulationCounter)
                    probePositions.Add(position);
                else
                    probePositions[simulationCounter] = position;

                if (planePositions.Count == simulationCounter)
                    planePositions.Add(characterController.bounds.min + Vector3.forward * characterController.radius + Vector3.right * characterController.radius + Vector3.up * groundProbeRadius);
                else
                    planePositions[simulationCounter] = characterController.bounds.min + Vector3.forward * characterController.radius + Vector3.right * characterController.radius + Vector3.up * groundProbeRadius;
            }

            // --- Prevent drop/fall, snap to ground ---

            if (groundSnap)
            {
                groundRay.origin = characterController.transform.position;
                groundRay.direction = -Vector3.up;

                if (state.previousCollision.ground != null 
                    && Physics.RaycastNonAlloc(groundRay, groundRayHits, groundSnapRayDistance, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore) == 0)
                {
                    //Debug.Log("KEPT ON BOUNDS");
                    characterController.enabled = false;
                    float3 correctedPosition = state.previousCollision.position;
                    transform.position = correctedPosition;
                    characterController.enabled = true;
                    state.currentCollision.position = transform.position;
                    state.currentCollision.velocity = characterController.velocity / stepping;
                }

                // --- Draw casted ray ---
                if (debugDraw)
                    Debug.DrawRay(groundRay.origin, groundRay.direction * groundSnapRayDistance);
            }
        }

        // --------------------------------

        // --- Events ---

        void OnDrawGizmosSelected()
        {
            if (!debugDraw || characterController == null)
                return;

            // --- Draw sphere at current ground probe position ---
            for (int i = 0; i < probePositions.Count; ++i)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(probePositions[i], groundProbeRadius);
            }
         
            // --- Draw ground collision height limit (if below limit this ground won't be detected in regular collisions) ---
            float3 planeScale = Vector3.one;
            planeScale.y = 0.05f;

            Gizmos.color = Color.yellow;

            for (int i = 0; i < planePositions.Count; ++i)
            {
                Gizmos.DrawWireCube(planePositions[i], Vector3.one * planeScale);
            }

            // --- Draw capsule at last simulation position ---      
            if (capsuleDebugMesh != null)
            {
                Gizmos.color = Color.red;

                for (int i = 0; i < capsulePositions.Count; ++i)
                {
                    Gizmos.DrawWireMesh(capsuleDebugMesh, 0, capsulePositions[i], Quaternion.identity, capsuleDebugMeshScale);
                }
            }

            //Gizmos.DrawSphere(current.colliderContactPoint, 0.5f);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // --- Update current collision information with hit's information ---

            // --- If below ground probe limit, consider it only as ground, not regular collision ---
            float heightLimit = characterController.bounds.min.y + groundProbeRadius;

            if (hit.point.y > heightLimit) // if the slope is steep we may activate a collision against current ground
            {
                // --- Make sure we do not activate a collision against our current ground ---
                if (state.previousCollision.ground != null && hit.collider.Equals(state.previousCollision.ground))
                    return;

                //Debug.Log("Collided with:");
                //Debug.Log(hit.gameObject.name);
                

                state.previousCollision.CopyFrom(ref state.currentCollision);
                state.currentCollision.colliderContactPoint = hit.point;
                contactSize = hit.collider.bounds.extents.magnitude;
                contactTransform.t = hit.point;
                contactTransform.q = math.mul(transform.rotation, Quaternion.FromToRotation(-transform.forward, hit.normal));
                state.currentCollision.colliderContactNormal = hit.normal;
                state.currentCollision.collider = hit.collider;
                state.currentCollision.isColliding = true;
            }
        }

        // --------------------------------
    }
}